using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// คอมโพเนนต์สำหรับแสดงผลและปรับเปลี่ยน UI ตามอุปกรณ์อินพุตที่ใช้งาน
/// รองรับการแสดงผลที่แตกต่างกันสำหรับ Xbox และ PlayStation Controller
/// </summary>
public class InputDeviceUIIndicator : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("GameObject ที่แสดงตัวช่วย UI สำหรับ Mouse/Keyboard")]
    [SerializeField] private GameObject mouseKeyboardUI;
    
    [Tooltip("GameObject ที่แสดงตัวช่วย UI สำหรับ Xbox Controller")]
    [SerializeField] private GameObject xboxControllerUI;
    
    [Tooltip("GameObject ที่แสดงตัวช่วย UI สำหรับ PlayStation Controller")]
    [SerializeField] private GameObject playstationControllerUI;
    
    [Tooltip("GameObject ที่แสดงตัวช่วย UI สำหรับ Gamepad อื่นๆ")]
    [SerializeField] private GameObject otherGamepadUI;
    
    [Tooltip("(ถ้ามี) TextMeshProUGUI สำหรับแสดงชนิดของอุปกรณ์ปัจจุบัน")]
    [SerializeField] private TextMeshProUGUI deviceTextIndicator;
    
    [Tooltip("(ถ้ามี) Image สำหรับแสดงไอคอนของอุปกรณ์")]
    [SerializeField] private Image deviceIconImage;
    
    [Header("Icon References")]
    [Tooltip("ไอคอนสำหรับ Mouse/Keyboard")]
    [SerializeField] private Sprite mouseKeyboardIcon;
    
    [Tooltip("ไอคอนสำหรับ Xbox Controller")]
    [SerializeField] private Sprite xboxControllerIcon;
    
    [Tooltip("ไอคอนสำหรับ PlayStation Controller")]
    [SerializeField] private Sprite psControllerIcon;
    
    [Tooltip("ไอคอนสำหรับ Gamepad อื่นๆ")]
    [SerializeField] private Sprite otherGamepadIcon;
    
    // ตัวแปรเก็บสถานะปัจจุบัน
    private InputDeviceDetector.InputDeviceType currentDeviceType;
    
    private void Start()
    {
        // สมัครรับการแจ้งเตือนเมื่อมีการเปลี่ยนอุปกรณ์
        InputDeviceDetector.OnDeviceChanged += HandleDeviceChanged;
        
        // ค้นหา InputDeviceDetector ในซีน
        InputDeviceDetector deviceDetector = FindObjectOfType<InputDeviceDetector>();
        
        if (deviceDetector != null)
        {
            // อัปเดต UI ตามอุปกรณ์ปัจจุบัน
            HandleDeviceChanged(deviceDetector.GetCurrentDevice());
        }
        else
        {
            Debug.LogWarning("InputDeviceDetector not found in scene. UI will not update automatically.");
            // ตั้งค่าเริ่มต้นเป็น Mouse/Keyboard
            UpdateUI(InputDeviceDetector.InputDeviceType.MouseKeyboard);
        }
    }
    
    private void OnDestroy()
    {
        // ยกเลิกการสมัครรับการแจ้งเตือน
        InputDeviceDetector.OnDeviceChanged -= HandleDeviceChanged;
    }
    
    /// <summary>
    /// รับการแจ้งเตือนเมื่อมีการเปลี่ยนอุปกรณ์อินพุต
    /// </summary>
    private void HandleDeviceChanged(InputDeviceDetector.InputDeviceType deviceType)
    {
        currentDeviceType = deviceType;
        UpdateUI(deviceType);
    }
    
    /// <summary>
    /// อัปเดต UI ตามชนิดของอุปกรณ์อินพุต
    /// </summary>
    private void UpdateUI(InputDeviceDetector.InputDeviceType deviceType)
    {
        // แสดง/ซ่อน UI ตามอุปกรณ์
        if (mouseKeyboardUI != null)
        {
            mouseKeyboardUI.SetActive(deviceType == InputDeviceDetector.InputDeviceType.MouseKeyboard);
        }
        
        if (xboxControllerUI != null)
        {
            xboxControllerUI.SetActive(deviceType == InputDeviceDetector.InputDeviceType.XboxController);
        }
        
        if (playstationControllerUI != null)
        {
            playstationControllerUI.SetActive(deviceType == InputDeviceDetector.InputDeviceType.PlayStationController);
        }
        
        if (otherGamepadUI != null)
        {
            otherGamepadUI.SetActive(deviceType == InputDeviceDetector.InputDeviceType.OtherGamepad);
        }
        
        // อัปเดตข้อความแสดงชนิดของอุปกรณ์ (ถ้ามี)
        if (deviceTextIndicator != null)
        {
            string deviceName = GetDeviceDisplayName(deviceType);
            deviceTextIndicator.text = deviceName;
        }
        
        // อัปเดตไอคอนของอุปกรณ์ (ถ้ามี)
        if (deviceIconImage != null)
        {
            deviceIconImage.sprite = GetDeviceIcon(deviceType);
            
            // รีเซ็ตขนาดของ Image ถ้ามีการเปลี่ยนแปลงสไปรต์
            if (deviceIconImage.sprite != null)
            {
                deviceIconImage.SetNativeSize();
            }
        }
    }
    
    /// <summary>
    /// รับชื่อที่แสดงผลของอุปกรณ์
    /// </summary>
    private string GetDeviceDisplayName(InputDeviceDetector.InputDeviceType deviceType)
    {
        switch (deviceType)
        {
            case InputDeviceDetector.InputDeviceType.MouseKeyboard:
                return "Mouse & Keyboard";
                
            case InputDeviceDetector.InputDeviceType.XboxController:
                return "Xbox Controller";
                
            case InputDeviceDetector.InputDeviceType.PlayStationController:
                return "PlayStation Controller";
                
            case InputDeviceDetector.InputDeviceType.OtherGamepad:
                return "Gamepad";
                
            default:
                return "Unknown Device";
        }
    }
    
    /// <summary>
    /// รับไอคอนของอุปกรณ์
    /// </summary>
    private Sprite GetDeviceIcon(InputDeviceDetector.InputDeviceType deviceType)
    {
        switch (deviceType)
        {
            case InputDeviceDetector.InputDeviceType.MouseKeyboard:
                return mouseKeyboardIcon;
                
            case InputDeviceDetector.InputDeviceType.XboxController:
                return xboxControllerIcon;
                
            case InputDeviceDetector.InputDeviceType.PlayStationController:
                return psControllerIcon;
                
            case InputDeviceDetector.InputDeviceType.OtherGamepad:
                return otherGamepadIcon;
                
            default:
                return mouseKeyboardIcon; // ใช้ไอคอนเริ่มต้นเป็น Mouse/Keyboard
        }
    }
    
    
    /// <summary>
    /// รับประเภทอุปกรณ์ปัจจุบัน
    /// </summary>
    public InputDeviceDetector.InputDeviceType GetCurrentDeviceType()
    {
        return currentDeviceType;
    }
}