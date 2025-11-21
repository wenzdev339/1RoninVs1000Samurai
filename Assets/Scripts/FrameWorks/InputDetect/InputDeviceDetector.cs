using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// ระบบตรวจจับและจัดการอุปกรณ์อินพุตที่ผู้เล่นใช้งานอยู่ในปัจจุบัน
/// สามารถแยกประเภทของ Gamepad ได้ (Xbox, PlayStation)
/// และจัดการการแสดงผลของ cursor ตามอุปกรณ์อินพุต
/// </summary>
public class InputDeviceDetector : MonoBehaviour
{
    // ชนิดของอุปกรณ์อินพุต
    public enum InputDeviceType
    {
        MouseKeyboard,
        XboxController,
        PlayStationController,
        OtherGamepad,
        All
    }

    // สถานะอุปกรณ์อินพุตปัจจุบัน
    [SerializeField] private InputDeviceType currentDevice = InputDeviceType.MouseKeyboard;
    
    // เหตุการณ์ที่เกิดขึ้นเมื่อมีการเปลี่ยนอุปกรณ์อินพุต
    public static event Action<InputDeviceType> OnDeviceChanged;
    
    // อ้างอิงถึง Input Action ที่ใช้
    private ControllerAction controllerAction;
    
    // ตัวแปรสำหรับติดตามการเปลี่ยนแปลง
    private Vector2 lastMousePosition;
    
    [Header("Debug Info")]
    [SerializeField] private string currentGamepadName = "";
    [SerializeField] private Gamepad currentActiveGamepad;

    [Header("Cursor Management")]
    [SerializeField] private bool manageCursor = true;
    [SerializeField] private bool cursorVisible = true;

    [SerializeField] private float deviceSwitchDelay = 1.0f; // หน่วงเวลา 1 วินาที
    private float lastDeviceSwitchTime = 0f;
    [SerializeField] private float deviceCheckInterval = 0.5f; // ตรวจสอบทุก 0.5 วินาที
    private float lastDeviceCheckTime = 0f;
    private InputDeviceType lastActiveInputType = InputDeviceType.MouseKeyboard;
    [SerializeField] private bool requireMultipleInputsToSwitch = true; // ต้องมีการ input หลายครั้งจึงจะเปลี่ยน
    private Dictionary<InputDeviceType, float> deviceActivityTimers = new Dictionary<InputDeviceType, float>();
    private const float ACTIVITY_THRESHOLD = 0.5f; // ต้องมีการใช้งานต่อเนื่องเกิน 0.5 วินาที
    
private void Awake()
{
    controllerAction = new ControllerAction();
    
    // ติดตั้ง callback สำหรับตรวจจับอุปกรณ์
    InputSystem.onActionChange += OnActionChange;
    
    // รับชนิดของอุปกรณ์เริ่มต้น
    if (Gamepad.current != null)
    {
        currentGamepadName = Gamepad.current.name;
        currentActiveGamepad = Gamepad.current;
        currentDevice = DetermineGamepadType(Gamepad.current);
    }
    else
    {
        currentDevice = InputDeviceType.MouseKeyboard;
    }
    
    // เรียกใช้เหตุการณ์เริ่มต้น
    OnDeviceChanged?.Invoke(currentDevice);
    
    // ตั้งค่า cursor เริ่มต้น
    UpdateCursorVisibility(currentDevice);
    
    // เริ่มต้น deviceActivityTimers
    deviceActivityTimers[InputDeviceType.MouseKeyboard] = 0f;
    deviceActivityTimers[InputDeviceType.XboxController] = 0f;
    deviceActivityTimers[InputDeviceType.PlayStationController] = 0f;
    deviceActivityTimers[InputDeviceType.OtherGamepad] = 0f;
    
    // เพิ่ม: บันทึกตำแหน่งเมาส์เริ่มต้น (ป้องกัน null)
    lastMousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
}
    
    private void OnEnable()
    {
        controllerAction.Enable();
        
        // บันทึกตำแหน่งเมาส์เริ่มต้น
        lastMousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        
        // ลงทะเบียนรับการแจ้งเตือนเมื่อมีอุปกรณ์เชื่อมต่อหรือตัดการเชื่อมต่อ
        InputSystem.onDeviceChange += OnDeviceChange;
    }
    
    private void OnDisable()
    {
        controllerAction.Disable();
        InputSystem.onActionChange -= OnActionChange;
        InputSystem.onDeviceChange -= OnDeviceChange;
    }
    
    private void Update()
    {
        if (Time.time - lastDeviceCheckTime > deviceCheckInterval)
        {
            DetectInputDevice();
        }
    }
    
    /// <summary>
    /// ตรวจจับการเชื่อมต่อหรือตัดการเชื่อมต่ออุปกรณ์
    /// </summary>
    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Gamepad gamepad && (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected))
        {
            // ตั้งค่า gamepad ที่เพิ่งเชื่อมต่อเป็น gamepad ปัจจุบัน
            currentGamepadName = gamepad.name;
            currentActiveGamepad = gamepad;
            InputDeviceType gamepadType = DetermineGamepadType(gamepad);
            
            // เปลี่ยนอุปกรณ์เมื่อมีการเชื่อมต่อ gamepad ใหม่
            SwitchToDevice(gamepadType);
        }
        else if (device is Gamepad disconnectedGamepad && (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected))
        {
            // ตรวจสอบว่า gamepad ที่ถูกถอดเป็น gamepad ที่กำลังใช้งานอยู่หรือไม่
            if (currentActiveGamepad == disconnectedGamepad)
            {
                // หา gamepad ที่เหลืออยู่
                var gamepads = Gamepad.all;
                if (gamepads.Count > 0)
                {
                    // เลือก gamepad แรกที่พบ
                    currentActiveGamepad = gamepads[0];
                    currentGamepadName = currentActiveGamepad.name;
                    InputDeviceType gamepadType = DetermineGamepadType(currentActiveGamepad);
                    SwitchToDevice(gamepadType);
                }
                else
                {
                    // ถ้าไม่มี gamepad เหลืออยู่ ให้กลับไปใช้ mouse/keyboard
                    currentActiveGamepad = null;
                    currentGamepadName = "";
                    SwitchToDevice(InputDeviceType.MouseKeyboard);
                }
            }
        }
    }
    
    /// <summary>
    /// ตรวจจับอุปกรณ์อินพุตที่กำลังใช้งานอยู่และเปลี่ยนทันทีเมื่อพบการใช้งาน
    /// </summary>
    private void DetectInputDevice()
    {
        // บันทึกอุปกรณ์ที่ใช้งานอยู่ก่อนหน้า
        InputDeviceType previousDevice = currentDevice;
        
        // ตรวจสอบการใช้งานของทุกอุปกรณ์
        bool mouseKeyboardActive = IsMouseKeyboardActive();
        
        // ลดตัวจับเวลาของทุกอุปกรณ์
        foreach (var key in deviceActivityTimers.Keys.ToList())
        {
            deviceActivityTimers[key] = Mathf.Max(0, deviceActivityTimers[key] - deviceCheckInterval);
        }
        
        // เพิ่มตัวจับเวลาสำหรับอุปกรณ์ที่ใช้งานอยู่
        if (mouseKeyboardActive)
        {
            deviceActivityTimers[InputDeviceType.MouseKeyboard] = ACTIVITY_THRESHOLD;
        }
        
        // ตรวจสอบการใช้งาน Gamepad แต่ละชิ้น
        Dictionary<Gamepad, bool> gamepadActivity = new Dictionary<Gamepad, bool>();
        foreach (var gamepad in Gamepad.all)
        {
            bool isActive = IsGamepadActive(gamepad);
            gamepadActivity[gamepad] = isActive;
            
            if (isActive)
            {
                InputDeviceType gamepadType = DetermineGamepadType(gamepad);
                deviceActivityTimers[gamepadType] = ACTIVITY_THRESHOLD;
            }
        }
        
        // ตัดสินใจว่าควรใช้อุปกรณ์ใด โดยพิจารณาจากอุปกรณ์ที่มีการใช้งานล่าสุดและมีการใช้งานต่อเนื่อง
        InputDeviceType deviceToUse = currentDevice; // เริ่มต้นด้วยอุปกรณ์ปัจจุบัน
        float highestActivity = deviceActivityTimers[currentDevice]; // เริ่มต้นด้วยค่ากิจกรรมของอุปกรณ์ปัจจุบัน
        
        // หาอุปกรณ์ที่มีการใช้งานมากที่สุด
        foreach (var pair in deviceActivityTimers)
        {
            if (pair.Value > highestActivity)
            {
                highestActivity = pair.Value;
                deviceToUse = pair.Key;
            }
        }
        
        // ใช้เกณฑ์ขั้นต่ำสำหรับการเปลี่ยนอุปกรณ์
        if (highestActivity >= ACTIVITY_THRESHOLD && deviceToUse != currentDevice)
        {
            // ตรวจสอบการหน่วงเวลา
            if (Time.time - lastDeviceSwitchTime >= deviceSwitchDelay)
            {
                // หากเป็น gamepad ต้องอัพเดต currentActiveGamepad ด้วย
                if (IsAnyGamepad(deviceToUse))
                {
                    foreach (var gamepad in Gamepad.all)
                    {
                        if (DetermineGamepadType(gamepad) == deviceToUse && gamepadActivity[gamepad])
                        {
                            currentActiveGamepad = gamepad;
                            currentGamepadName = gamepad.name;
                            break;
                        }
                    }
                }
                
                // เปลี่ยนอุปกรณ์
                SwitchToDevice(deviceToUse);
                lastDeviceSwitchTime = Time.time;
                // Debug.Log($"Switched to most active device: {deviceToUse}");
            }
        }
    }

    /// <summary>
    /// ตรวจสอบว่าอุปกรณ์ปัจจุบันเป็น gamepad ชนิดใดชนิดหนึ่งหรือไม่
    /// </summary>
    private bool IsAnyGamepad(InputDeviceType deviceType)
    {
        return deviceType == InputDeviceType.XboxController || 
               deviceType == InputDeviceType.PlayStationController || 
               deviceType == InputDeviceType.OtherGamepad;
    }
    
    /// <summary>
    /// ตรวจสอบชนิดของ Gamepad
    /// </summary>
    private InputDeviceType DetermineGamepadType(Gamepad gamepad)
    {
        if (gamepad == null) return InputDeviceType.MouseKeyboard;
        
        string deviceName = gamepad.name.ToLower();
        
        // ตรวจสอบว่าเป็น PlayStation Controller
        if (deviceName.Contains("playstation") || 
            deviceName.Contains("ps4") || 
            deviceName.Contains("ps5") || 
            deviceName.Contains("dualshock") || 
            deviceName.Contains("dualsense") ||
            deviceName.Contains("wireless controller"))
        {
            return InputDeviceType.PlayStationController;
        }
        // ตรวจสอบว่าเป็น Xbox Controller
        else if (deviceName.Contains("xbox") || 
                 deviceName.Contains("microsoft") || 
                 deviceName.Contains("xinput"))
        {
            return InputDeviceType.XboxController;
        }
        // Gamepad อื่นๆ
        else
        {
            return InputDeviceType.OtherGamepad;
        }
    }
    
    /// <summary>
    /// ตรวจสอบการใช้งาน
    /// </summary>
    private bool IsGamepadActive(Gamepad gamepad)
    {
        if (gamepad == null || !gamepad.enabled)
        {
            return false;
        }
        
        // เพิ่ม threshold ให้สูงขึ้น
        float stickThreshold = 0.25f; // เพิ่มจาก 0.1f
        
        // ตรวจสอบการใช้งาน analog sticks
        bool leftStickActive = gamepad.leftStick.ReadValue().magnitude > stickThreshold;
        bool rightStickActive = gamepad.rightStick.ReadValue().magnitude > stickThreshold;
        
        // เพิ่ม threshold สำหรับ triggers
        bool leftTriggerActive = gamepad.leftTrigger.ReadValue() > 0.25f;
        bool rightTriggerActive = gamepad.rightTrigger.ReadValue() > 0.25f;
        
        // ตรวจสอบเฉพาะปุ่มสำคัญ
        bool significantButtonPressed = 
            gamepad.buttonSouth.isPressed || 
            gamepad.buttonNorth.isPressed || 
            gamepad.buttonEast.isPressed || 
            gamepad.buttonWest.isPressed ||
            gamepad.leftShoulder.isPressed ||
            gamepad.rightShoulder.isPressed ||
            gamepad.startButton.isPressed ||
            gamepad.selectButton.isPressed;
        
        return leftStickActive || rightStickActive || leftTriggerActive || rightTriggerActive || significantButtonPressed;
    }

private bool IsMouseKeyboardActive()
{
    // เพิ่ม threshold การเคลื่อนไหวของเมาส์
    float mouseMovementThreshold = 3.0f;
    
    // ตรวจสอบการเคลื่อนไหวของเมาส์
    if (Mouse.current != null)
    {
        Vector2 currentMousePos = Mouse.current.position.ReadValue();
        
        // ตรวจจับการเคลื่อนไหวของเมาส์ที่ชัดเจน
        if (Vector2.Distance(currentMousePos, lastMousePosition) > mouseMovementThreshold)
        {
            lastMousePosition = currentMousePos;
            return true;
        }
        
        // ตรวจจับการคลิกเมาส์
        if (Mouse.current.leftButton.wasPressedThisFrame || 
            Mouse.current.rightButton.wasPressedThisFrame || 
            Mouse.current.middleButton.wasPressedThisFrame)
        {
            return true;
        }
    }
    
    // ตรวจสอบการกดปุ่มบนคีย์บอร์ด - ตรวจสอบเฉพาะ Keyboard.current
    if (Keyboard.current != null)
    {
        foreach (var key in Keyboard.current.allKeys)
        {
            // ตรวจสอบเฉพาะ key เท่านั้น
            if (key != null && key.wasPressedThisFrame)
            {
                return true;
            }
        }
    }
    
    return false;
}

    
    /// <summary>
    /// ได้รับเรียกเมื่อมีการเปลี่ยนแปลงการกระทำ input
    /// </summary>
private void OnActionChange(object obj, InputActionChange change)
{
    if (change == InputActionChange.ActionPerformed)
    {
        var action = obj as InputAction;
        if (action != null && action.activeControl != null)
        {
            var device = action.activeControl.device;
            
            if (device is Gamepad gamepad)
            {
                InputDeviceType gamepadType = DetermineGamepadType(gamepad);
                deviceActivityTimers[gamepadType] = ACTIVITY_THRESHOLD;
            }
            else if (device is Mouse || device is Keyboard)
            {
                deviceActivityTimers[InputDeviceType.MouseKeyboard] = ACTIVITY_THRESHOLD;
            }
        }
    }
}
    
    /// <summary>
    /// เปลี่ยนไปใช้อุปกรณ์อินพุตที่ระบุ
    /// </summary>
    private void SwitchToDevice(InputDeviceType deviceType)
    {
        // บันทึกเวลาล่าสุดที่เปลี่ยนอุปกรณ์
        lastDeviceSwitchTime = Time.time;
        
        currentDevice = deviceType;
        
        // อัปเดต cursor ตามอุปกรณ์ใหม่
        if (manageCursor)
        {
            UpdateCursorVisibility(deviceType);
        }
        
        // แจ้งเตือนผู้ฟัง
        OnDeviceChanged?.Invoke(currentDevice);
    }
    
    /// <summary>
    /// อัปเดตการแสดงผลเคอร์เซอร์ตามอุปกรณ์อินพุต
    /// </summary>
    private void UpdateCursorVisibility(InputDeviceType deviceType)
    {
        if (deviceType == InputDeviceType.MouseKeyboard)
        {
            // แสดงเคอร์เซอร์เมื่อใช้ Mouse/Keyboard
            ShowCursor();
        }
        else
        {
            // ซ่อนเคอร์เซอร์เมื่อใช้ Gamepad
            HideCursor();
        }
    }
    
    /// <summary>
    /// แสดงเคอร์เซอร์
    /// </summary>
    public void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        cursorVisible = true;
    }
    
    /// <summary>
    /// ซ่อนเคอร์เซอร์
    /// </summary>
    public void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        cursorVisible = false;
    }
    
    /// <summary>
    /// เปิด/ปิดการจัดการเคอร์เซอร์อัตโนมัติ
    /// </summary>
    public void SetCursorManagement(bool enable)
    {
        manageCursor = enable;
        
        // อัปเดตเคอร์เซอร์ทันทีตามสถานะปัจจุบันหากเปิดใช้งาน
        if (enable)
        {
            UpdateCursorVisibility(currentDevice);
        }
    }
    
    /// <summary>
    /// รับสถานะการแสดงผลเคอร์เซอร์ปัจจุบัน
    /// </summary>
    public bool IsCursorVisible()
    {
        return cursorVisible;
    }
    
    /// <summary>
    /// รับชนิดของอุปกรณ์อินพุตปัจจุบัน
    /// </summary>
    public InputDeviceType GetCurrentDevice()
    {
        return currentDevice;
    }
    
    /// <summary>
    /// รับ Gamepad ที่กำลังใช้งานอยู่
    /// </summary>
    public Gamepad GetActiveGamepad()
    {
        return currentActiveGamepad;
    }
    
    /// <summary>
    /// ตรวจสอบว่าใช้ Mouse/Keyboard หรือไม่
    /// </summary>
    public bool IsUsingMouseKeyboard()
    {
        return currentDevice == InputDeviceType.MouseKeyboard;
    }
    
    /// <summary>
    /// ตรวจสอบว่าใช้ Gamepad หรือไม่ (ทุกประเภท)
    /// </summary>
    public bool IsUsingGamepad()
    {
        return IsAnyGamepad(currentDevice);
    }
    
    /// <summary>
    /// ตรวจสอบว่าใช้ Xbox Controller หรือไม่
    /// </summary>
    public bool IsUsingXboxController()
    {
        return currentDevice == InputDeviceType.XboxController;
    }
    
    /// <summary>
    /// ตรวจสอบว่าใช้ PlayStation Controller หรือไม่
    /// </summary>
    public bool IsUsingPlayStationController()
    {
        return currentDevice == InputDeviceType.PlayStationController;
    }
    
    /// <summary>
    /// ตรวจสอบว่าใช้ Gamepad อื่นๆ หรือไม่
    /// </summary>
    public bool IsUsingOtherGamepad()
    {
        return currentDevice == InputDeviceType.OtherGamepad;
    }
    
    /// <summary>
    /// รับชื่อของ Gamepad ปัจจุบัน
    /// </summary>
    public string GetCurrentGamepadName()
    {
        return currentGamepadName;
    }
}