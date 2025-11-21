using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ระบบที่จัดการการเปลี่ยนแปลง UI ตามชนิดของอุปกรณ์อินพุต
/// อ้างอิงข้อมูลอุปกรณ์อินพุตจาก InputDeviceDetector
/// </summary>
public class UIInputSwitcher : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("อ้างอิงถึง InputDeviceDetector เพื่อรับข้อมูลอินพุตปัจจุบัน")]
    [SerializeField] private InputDeviceDetector inputDetector;

    [Header("UI Groups")]
    [Tooltip("กลุ่ม UI ที่แสดงเฉพาะเมื่อใช้ Mouse/Keyboard")]
    [SerializeField] private List<GameObject> mouseKeyboardUIElements = new List<GameObject>();
    
    [Tooltip("กลุ่ม UI ที่แสดงเฉพาะเมื่อใช้ Xbox Controller")]
    [SerializeField] private List<GameObject> xboxUIElements = new List<GameObject>();
    
    [Tooltip("กลุ่ม UI ที่แสดงเฉพาะเมื่อใช้ PlayStation Controller")]
    [SerializeField] private List<GameObject> playstationUIElements = new List<GameObject>();
    
    [Tooltip("กลุ่ม UI ที่แสดงเฉพาะเมื่อใช้ Gamepad อื่นๆ")]
    [SerializeField] private List<GameObject> otherGamepadUIElements = new List<GameObject>();

    [Header("Button Prompts")]
    [Tooltip("รายการปุ่มที่ต้องเปลี่ยนไอคอนตามอุปกรณ์อินพุต")]
    [SerializeField] private List<ButtonPrompt> buttonPrompts = new List<ButtonPrompt>();

    [Header("Settings")]
    [Tooltip("เปิด/ปิดการเปลี่ยนแปลง UI อัตโนมัติ")]
    [SerializeField] private bool autoSwitchUI = true;

        [Tooltip("Tag สำหรับ UI ของ Mouse/Keyboard")]
    [SerializeField] private string mouseKeyboardTag = "MouseKeyboardUI";
    
    [Tooltip("Tag สำหรับ UI ของ Xbox Controller")]
    [SerializeField] private string xboxTag = "XboxUI";
    
    [Tooltip("Tag สำหรับ UI ของ PlayStation Controller")]
    [SerializeField] private string playstationTag = "PlayStationUI";
    
    [Tooltip("Tag สำหรับ UI ของ Gamepad อื่นๆ")]
    [SerializeField] private string otherGamepadTag = "OtherGamepadUI";

    [Header("Tag Settings")]
    [Tooltip("เปิด/ปิดการค้นหา GameObject จาก Tag โดยอัตโนมัติ")]
    [SerializeField] private bool findElementsByTag = true;

       // บันทึกสถานะ active ที่ต้นฉบับของแต่ละ UI element
    private Dictionary<GameObject, bool> originalActiveState = new Dictionary<GameObject, bool>();


    // สถานะอุปกรณ์อินพุตปัจจุบัน
    private InputDeviceDetector.InputDeviceType currentInputDevice;

    // เพื่อใช้ในการตรวจสอบการเปลี่ยนแปลงในหน้า Inspector
    private bool previousAutoSwitchUI;

    private void Awake()
    {
        if (inputDetector == null)
        {
            // พยายามค้นหา InputDeviceDetector ในฉาก
            inputDetector = FindObjectOfType<InputDeviceDetector>();
            
            if (inputDetector == null)
            {
                Debug.LogError("UIInputSwitcher: ไม่พบ InputDeviceDetector ในฉาก กรุณากำหนดอ้างอิงในหน้า Inspector");
                enabled = false;
                return;
            }
        }
                // ค้นหา UI Elements จาก Tag
        if (findElementsByTag)
        {
            FindUIElementsByTag();
        }

        previousAutoSwitchUI = autoSwitchUI;
    }

    private void OnEnable()
    {
        // ลงทะเบียนรับเหตุการณ์จาก InputDeviceDetector
        InputDeviceDetector.OnDeviceChanged += HandleInputDeviceChanged;
        
        // อัปเดต UI ตามอุปกรณ์อินพุตปัจจุบัน
        if (inputDetector != null)
        {
            currentInputDevice = inputDetector.GetCurrentDevice();
            UpdateUIForInputDevice(currentInputDevice);
        }
    }

    private void OnDisable()
    {
        // ยกเลิกการลงทะเบียนเมื่อปิดการใช้งาน
        InputDeviceDetector.OnDeviceChanged -= HandleInputDeviceChanged;
    }

    private void Update()
    {
        // ตรวจสอบการเปลี่ยนแปลงการตั้งค่าในหน้า Inspector
        if (previousAutoSwitchUI != autoSwitchUI)
        {
            previousAutoSwitchUI = autoSwitchUI;
            
            if (autoSwitchUI)
            {
                // อัปเดต UI ทันทีเมื่อเปิดใช้งาน
                UpdateUIForInputDevice(currentInputDevice);
            }
        }
    }
    
       /// <summary>
    /// ค้นหา UI Elements จาก Tag และเพิ่มเข้าไปใน List ที่เกี่ยวข้อง
    /// จะค้นหา GameObject ทั้งหมดในฉาก รวมถึงที่ถูกปิดการแสดงผล (SetActive = false)
    /// </summary>
    private void FindUIElementsByTag()
    {
        // Debug.Log("UIInputSwitcher: เริ่มค้นหา UI Elements จาก Tag...");
        
        // ค้นหา GameObject จาก Tag สำหรับ Mouse/Keyboard UI
        if (!string.IsNullOrEmpty(mouseKeyboardTag))
        {
            FindAndAddToList(mouseKeyboardTag, mouseKeyboardUIElements);
        }
        
        // ค้นหา GameObject จาก Tag สำหรับ Xbox UI
        if (!string.IsNullOrEmpty(xboxTag))
        {
            FindAndAddToList(xboxTag, xboxUIElements);
        }
        
        // ค้นหา GameObject จาก Tag สำหรับ PlayStation UI
        if (!string.IsNullOrEmpty(playstationTag))
        {
            FindAndAddToList(playstationTag, playstationUIElements);
        }
    }

        private void FindAndAddToList(string tag, List<GameObject> list)
    {
        // เริ่มต้นไม่พบ GameObject ใดๆ
        bool foundAny = false;
        
        try
        {
            // ค้นหา root GameObjects ใน scene ทั้งหมด (รวมทั้ง active และ inactive)
            GameObject[] allRootGameObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            
            foreach (GameObject root in allRootGameObjects)
            {
                // ค้นหา GameObjects ที่มี tag ตามที่กำหนดใน hierarchy นี้ (รวมทั้ง active และ inactive)
                Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true); // 'true' เพื่อค้นหาทั้ง active และ inactive
                
                foreach (Transform transform in allTransforms)
                {
                    if (transform.CompareTag(tag))
                    {
                        GameObject obj = transform.gameObject;
                        foundAny = true;
                        
                        // ตรวจสอบว่า GameObject มีอยู่ใน List แล้วหรือไม่
                        if (!list.Contains(obj))
                        {
                            // บันทึกสถานะ active ดั้งเดิมก่อนเพิ่มเข้า List
                            if (!originalActiveState.ContainsKey(obj))
                            {
                                originalActiveState.Add(obj, obj.activeSelf);
                                // Debug.Log($"UIInputSwitcher: บันทึกสถานะเริ่มต้นของ '{obj.name}': {obj.activeSelf}");
                            }
                            
                            list.Add(obj);
                            // Debug.Log($"UIInputSwitcher: เพิ่ม '{obj.name}' เข้าไปใน List สถานะ active: {obj.activeSelf}");
                        }
                    }
                }
            }
            
            // ถ้าไม่พบ GameObject ใดๆ ให้แสดงคำเตือน
            if (!foundAny)
            {
                // Debug.LogWarning($"UIInputSwitcher: ไม่พบ GameObject ที่มี Tag '{tag}'");
            }
        }
        catch (Exception e)
        {
            // Debug.LogError($"UIInputSwitcher: เกิดข้อผิดพลาดในการค้นหา Tag '{tag}': {e.Message}");
        }
    }

    /// <summary>
    /// จัดการเมื่อมีการเปลี่ยนแปลงอุปกรณ์อินพุต
    /// </summary>
    private void HandleInputDeviceChanged(InputDeviceDetector.InputDeviceType newDevice)
    {
        currentInputDevice = newDevice;
        
        if (autoSwitchUI)
        {
            UpdateUIForInputDevice(newDevice);
        }
    }

    /// <summary>
    /// อัปเดต UI ตามชนิดของอุปกรณ์อินพุต
    /// </summary>
    private void UpdateUIForInputDevice(InputDeviceDetector.InputDeviceType deviceType)
    {
        // อัปเดตการแสดงผลกลุ่ม UI
        SetUIGroupVisibility(mouseKeyboardUIElements, deviceType == InputDeviceDetector.InputDeviceType.MouseKeyboard);
        SetUIGroupVisibility(xboxUIElements, deviceType == InputDeviceDetector.InputDeviceType.XboxController);
        SetUIGroupVisibility(playstationUIElements, deviceType == InputDeviceDetector.InputDeviceType.PlayStationController);
        SetUIGroupVisibility(otherGamepadUIElements, deviceType == InputDeviceDetector.InputDeviceType.OtherGamepad);
        
        // อัปเดตไอคอนปุ่มกด
        UpdateButtonPrompts(deviceType);
        
        // Debug.Log($"UI อัปเดตตามอุปกรณ์อินพุต: {deviceType}");
    }

    /// <summary>
    /// กำหนดการมองเห็นของกลุ่ม UI
    /// </summary>
    private void SetUIGroupVisibility(List<GameObject> uiElements, bool isVisible)
    {
        foreach (var element in uiElements)
        {
            if (element != null)
            {
                element.SetActive(isVisible);
            }
        }
    }

    /// <summary>
    /// อัปเดตไอคอนปุ่มกดตามอุปกรณ์อินพุต
    /// </summary>
    private void UpdateButtonPrompts(InputDeviceDetector.InputDeviceType deviceType)
    {
        foreach (var prompt in buttonPrompts)
        {
            if (prompt != null)
            {
                prompt.UpdateForInputDevice(deviceType);
            }
        }
    }

    /// <summary>
    /// เปิด/ปิดการเปลี่ยนแปลง UI อัตโนมัติ
    /// </summary>
    public void SetAutoSwitchUI(bool enable)
    {
        autoSwitchUI = enable;
        previousAutoSwitchUI = enable;
        
        if (enable)
        {
            // อัปเดต UI ทันทีถ้าเปิดใช้งาน
            UpdateUIForInputDevice(currentInputDevice);
        }
    }

    /// <summary>
    /// บังคับให้อัปเดต UI ตามอุปกรณ์อินพุตปัจจุบัน
    /// </summary>
    public void ForceUpdateUI()
    {
        if (inputDetector != null)
        {
            currentInputDevice = inputDetector.GetCurrentDevice();
            UpdateUIForInputDevice(currentInputDevice);
        }
    }

    /// <summary>
    /// บังคับให้แสดง UI สำหรับอุปกรณ์อินพุตที่ระบุ
    /// </summary>
    public void ForceShowUIForDevice(InputDeviceDetector.InputDeviceType deviceType)
    {
        UpdateUIForInputDevice(deviceType);
    }

    /// <summary>
    /// บังคับให้ค้นหา UI Elements จาก Tag ใหม่
    /// </summary>
    public void ReFindElementsByTag()
    {
        // ล้าง List เก่าก่อน
        mouseKeyboardUIElements.Clear();
        xboxUIElements.Clear();
        playstationUIElements.Clear();
        otherGamepadUIElements.Clear();
        originalActiveState.Clear();
        
        // ค้นหาใหม่
        FindUIElementsByTag();
        
        // อัปเดต UI ทันที
        if (autoSwitchUI && inputDetector != null)
        {
            UpdateUIForInputDevice(inputDetector.GetCurrentDevice());
        }
    }
}