using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class ButtonImageInputController : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    // Static Collection
    private static List<ButtonImageInputController> allButtons = new List<ButtonImageInputController>();
    private static ButtonImageInputController currentSelectedButton = null;
    
    [Header("UI Elements")]
    [SerializeField] private Button button;              
    
    [Header("Controller Input Icons")]
    [SerializeField] private Image mouseKeyboardIcon;
    [SerializeField] private Image xboxControllerIcon;
    [SerializeField] private Image psControllerIcon;
    [SerializeField] private Image genericGamepadIcon;
    
    [Header("Volume Slider Setup")]
    [SerializeField] private bool isVolumeSlider = false;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI volumeText;

    [Header("Volume Control Icons - Mouse/Keyboard")]
    [SerializeField] private Image mkDecrease;
    [SerializeField] private Image mkIncrease;

    [Header("Volume Control Icons - Xbox Controller")]
    [SerializeField] private Image xboxDecrease;
    [SerializeField] private Image xboxIncrease;

    [Header("Volume Control Icons - PlayStation Controller")]
    [SerializeField] private Image psDecrease;
    [SerializeField] private Image psIncrease;

    [Header("Volume Control Icons - Generic Gamepad")]
    [SerializeField] private Image genericDecrease;
    [SerializeField] private Image genericIncrease;
    
    [Header("Volume Adjustment Settings")]
    [SerializeField] private float volumeAdjustStep = 0.1f;

    // อ้างอิงไปยัง InputDeviceDetector
    private static InputDeviceDetector inputDetector;
    private InputDeviceDetector.InputDeviceType currentDevice;
    
    // ตัวแปรสำหรับการทำงาน - ใช้ UNSCALED TIME เพื่อทำงานได้ขณะ pause
    private bool isSelected = false;
    private float lastInputTime = 0f;
    private const float INPUT_DELAY = 0.15f;
    
    // ตัวแปรสำหรับการเลื่อนค้าง - ใช้ UNSCALED TIME
    private float keyRepeatDelay = 0.35f;
    private float keyRepeatRate = 0.1f;
    private float keyDownTime = 0f;
    private bool keyIsRepeating = false;
    private int horizontalInput = 0;
    
    // เพิ่มตัวแปรเก็บค่า Volume สำหรับป้องกันการรีเซ็ต
    private bool hasLoadedValues = false;
    private float cachedMusicValue = -1f;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
            
        SetAllIconsActiveState(false);
    }
    
    private void OnEnable()
    {
        if (!allButtons.Contains(this))
        {
            allButtons.Add(this);
        }
        
        if (inputDetector == null)
        {
            inputDetector = FindObjectOfType<InputDeviceDetector>();
        }
        
        if (inputDetector != null)
        {
            InputDeviceDetector.OnDeviceChanged -= HandleInputDeviceChanged;
            InputDeviceDetector.OnDeviceChanged += HandleInputDeviceChanged;
            currentDevice = inputDetector.GetCurrentDevice();
        }
        
        if (isVolumeSlider && volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            volumeSlider.onValueChanged.AddListener(OnSliderValueChanged);
            
            if (!hasLoadedValues && volumeSlider != null)
            {
                cachedMusicValue = volumeSlider.value;
                hasLoadedValues = true;
                UpdateVolumeText();
            }
        }
    }

    private void OnDisable()
    {
        allButtons.Remove(this);
        
        if (currentSelectedButton == this)
        {
            currentSelectedButton = null;
        }
        
        if (inputDetector != null)
        {
            InputDeviceDetector.OnDeviceChanged -= HandleInputDeviceChanged;
        }
        
        if (isVolumeSlider && volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
        
        isSelected = false;
        keyIsRepeating = false;
    }

    private void Update()
    {
        // ทำงานเฉพาะเมื่อถูกเลือกอยู่เท่านั้น
        if (!isSelected || !IsCurrentlySelected())
            return;

        // กรณีเป็น Volume Slider
        if (isVolumeSlider && volumeSlider != null)
        {
            HandleVolumeSliderInput();
        }
        
        // ดักจับ input สำหรับการกดค้าง
        CheckKeyHoldInput();
    }
    
    // ฟังก์ชันนี้ใช้ UNSCALED TIME เพื่อทำงานได้ขณะ pause
    private void CheckKeyHoldInput()
    {
        if (Keyboard.current != null)
        {
            bool leftKeyPressed = Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed;
            bool rightKeyPressed = Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed;
            
            bool leftKeyJustPressed = Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame;
            bool rightKeyJustPressed = Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame;
            
            bool keysReleased = !leftKeyPressed && !rightKeyPressed;
            
            if (keysReleased)
            {
                keyIsRepeating = false;
                horizontalInput = 0;
            }
            else if (leftKeyJustPressed)
            {
                horizontalInput = -1;
                keyDownTime = Time.unscaledTime; // UNSCALED TIME
                keyIsRepeating = false;
            }
            else if (rightKeyJustPressed)
            {
                horizontalInput = 1;
                keyDownTime = Time.unscaledTime; // UNSCALED TIME
                keyIsRepeating = false;
            }
            else if (leftKeyPressed && !keyIsRepeating)
            {
                horizontalInput = -1;
                if (Time.unscaledTime - keyDownTime > keyRepeatDelay) // UNSCALED TIME
                {
                    keyIsRepeating = true;
                    lastInputTime = Time.unscaledTime - keyRepeatRate; // UNSCALED TIME
                }
            }
            else if (rightKeyPressed && !keyIsRepeating)
            {
                horizontalInput = 1;
                if (Time.unscaledTime - keyDownTime > keyRepeatDelay) // UNSCALED TIME
                {
                    keyIsRepeating = true;
                    lastInputTime = Time.unscaledTime - keyRepeatRate; // UNSCALED TIME
                }
            }
            
            // ตรวจสอบว่ากำลังกดค้างและควรเพิ่ม/ลดค่าแล้วหรือไม่
            if (keyIsRepeating && horizontalInput != 0)
            {
                if (Time.unscaledTime - lastInputTime >= keyRepeatRate) // UNSCALED TIME
                {
                    lastInputTime = Time.unscaledTime; // UNSCALED TIME
                    
                    // เรียกฟังก์ชันปรับ Volume
                    if (isVolumeSlider && volumeSlider != null)
                    {
                        AdjustVolumeSlider(horizontalInput);
                    }
                }
            }
        }
        
        // ตรวจจับคอนโทรลเลอร์
        if (Gamepad.current != null)
        {
            Vector2 dpadValue = Gamepad.current.dpad.ReadValue();
            Vector2 leftStickValue = Gamepad.current.leftStick.ReadValue();
            
            bool dpadLeftPressed = dpadValue.x < -0.5f;
            bool dpadRightPressed = dpadValue.x > 0.5f;
            bool stickLeftPressed = leftStickValue.x < -0.5f;
            bool stickRightPressed = leftStickValue.x > 0.5f;
            
            bool leftPressed = dpadLeftPressed || stickLeftPressed;
            bool rightPressed = dpadRightPressed || stickRightPressed;
            
            bool keysReleased = !leftPressed && !rightPressed;
            
            if (keysReleased)
            {
                keyIsRepeating = false;
                horizontalInput = 0;
            }
            else if (leftPressed && horizontalInput != -1)
            {
                horizontalInput = -1;
                keyDownTime = Time.unscaledTime; // UNSCALED TIME
                keyIsRepeating = false;
                
                if (isVolumeSlider && volumeSlider != null)
                {
                    AdjustVolumeSlider(-1);
                }
            }
            else if (rightPressed && horizontalInput != 1)
            {
                horizontalInput = 1;
                keyDownTime = Time.unscaledTime; // UNSCALED TIME
                keyIsRepeating = false;
                
                if (isVolumeSlider && volumeSlider != null)
                {
                    AdjustVolumeSlider(1);
                }
            }
            else if (leftPressed && !keyIsRepeating)
            {
                if (Time.unscaledTime - keyDownTime > keyRepeatDelay) // UNSCALED TIME
                {
                    keyIsRepeating = true;
                    lastInputTime = Time.unscaledTime - keyRepeatRate; // UNSCALED TIME
                }
            }
            else if (rightPressed && !keyIsRepeating)
            {
                if (Time.unscaledTime - keyDownTime > keyRepeatDelay) // UNSCALED TIME
                {
                    keyIsRepeating = true;
                    lastInputTime = Time.unscaledTime - keyRepeatRate; // UNSCALED TIME
                }
            }
            
            if (keyIsRepeating && horizontalInput != 0)
            {
                if (Time.unscaledTime - lastInputTime >= keyRepeatRate) // UNSCALED TIME
                {
                    lastInputTime = Time.unscaledTime; // UNSCALED TIME
                    
                    if (isVolumeSlider && volumeSlider != null)
                    {
                        AdjustVolumeSlider(horizontalInput);
                    }
                }
            }
        }
    }

    // ฟังก์ชันจัดการ Input สำหรับ Volume Slider - ใช้ UNSCALED TIME
    private void HandleVolumeSliderInput()
    {
        if (Time.unscaledTime - lastInputTime < INPUT_DELAY) // UNSCALED TIME
            return;

        float horizontalInputValue = 0f;

        // ตรวจจับอินพุตจากคีย์บอร์ด
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            {
                horizontalInputValue = -1f;
                lastInputTime = Time.unscaledTime; // UNSCALED TIME
                AdjustVolumeSlider(-1);
            }
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            {
                horizontalInputValue = 1f;
                lastInputTime = Time.unscaledTime; // UNSCALED TIME
                AdjustVolumeSlider(1);
            }
        }

        // ตรวจจับอินพุตจากคอนโทรลเลอร์
        if (Gamepad.current != null && horizontalInputValue == 0f)
        {
            Vector2 dpadValue = Gamepad.current.dpad.ReadValue();
            Vector2 leftStickValue = Gamepad.current.leftStick.ReadValue();

            if (dpadValue.x < -0.5f || leftStickValue.x < -0.5f)
            {
                horizontalInputValue = -1f;
                lastInputTime = Time.unscaledTime; // UNSCALED TIME
                AdjustVolumeSlider(-1);
            }
            else if (dpadValue.x > 0.5f || leftStickValue.x > 0.5f)
            {
                horizontalInputValue = 1f;
                lastInputTime = Time.unscaledTime; // UNSCALED TIME
                AdjustVolumeSlider(1);
            }
        }
    }

    // ฟังก์ชันปรับค่า Volume Slider
    private void AdjustVolumeSlider(int direction)
    {
        if (volumeSlider == null)
            return;

        float newValue = volumeSlider.value + (direction * volumeAdjustStep);
        newValue = Mathf.Clamp(newValue, volumeSlider.minValue, volumeSlider.maxValue);
        volumeSlider.value = newValue;
        
        // บันทึกค่าที่เปลี่ยนแปลง
        cachedMusicValue = newValue;
        UpdateVolumeText();
    }

    private void HideIcons()
    {
        SetAllIconsActiveState(false);
    }

    private void HandleInputDeviceChanged(InputDeviceDetector.InputDeviceType newDevice)
    {
        currentDevice = newDevice;
        
        if (isSelected && IsCurrentlySelected())
        {
            UpdateIconsForCurrentDevice();
        }
    }

    private void UpdateIconsForCurrentDevice()
    {
        SetAllIconsActiveState(false);
        
        if (!isVolumeSlider)
        {
            SetCurrentDeviceIconActive(true);
        }
        else
        {
            SetCurrentVolumeControlIconsActive(true);
            SetCurrentDeviceIconActive(true);
            UpdateVolumeText();
        }
    }

    private void SetAllIconsActiveState(bool active)
    {
        if (this == null || !gameObject) return;
        
        if (mouseKeyboardIcon != null) mouseKeyboardIcon.gameObject.SetActive(active);
        if (xboxControllerIcon != null) xboxControllerIcon.gameObject.SetActive(active);
        if (psControllerIcon != null) psControllerIcon.gameObject.SetActive(active);
        if (genericGamepadIcon != null) genericGamepadIcon.gameObject.SetActive(active);
        
        if (isVolumeSlider)
        {
            if (mkDecrease != null) mkDecrease.gameObject.SetActive(active);
            if (mkIncrease != null) mkIncrease.gameObject.SetActive(active);
            if (xboxDecrease != null) xboxDecrease.gameObject.SetActive(active);
            if (xboxIncrease != null) xboxIncrease.gameObject.SetActive(active);
            if (psDecrease != null) psDecrease.gameObject.SetActive(active);
            if (psIncrease != null) psIncrease.gameObject.SetActive(active);
            if (genericDecrease != null) genericDecrease.gameObject.SetActive(active);
            if (genericIncrease != null) genericIncrease.gameObject.SetActive(active);
        }
    }

    private void SetCurrentDeviceIconActive(bool active)
    {
        switch (currentDevice)
        {
            case InputDeviceDetector.InputDeviceType.MouseKeyboard:
                if (mouseKeyboardIcon != null) mouseKeyboardIcon.gameObject.SetActive(active);
                break;
            case InputDeviceDetector.InputDeviceType.XboxController:
                if (xboxControllerIcon != null) xboxControllerIcon.gameObject.SetActive(active);
                break;
            case InputDeviceDetector.InputDeviceType.PlayStationController:
                if (psControllerIcon != null) psControllerIcon.gameObject.SetActive(active);
                break;
            case InputDeviceDetector.InputDeviceType.OtherGamepad:
                if (genericGamepadIcon != null) genericGamepadIcon.gameObject.SetActive(active);
                break;
        }
    }

    private void SetCurrentVolumeControlIconsActive(bool active)
    {
        switch (currentDevice)
        {
            case InputDeviceDetector.InputDeviceType.MouseKeyboard:
                if (mkDecrease != null) mkDecrease.gameObject.SetActive(active);
                if (mkIncrease != null) mkIncrease.gameObject.SetActive(active);
                break;
            case InputDeviceDetector.InputDeviceType.XboxController:
                if (xboxDecrease != null) xboxDecrease.gameObject.SetActive(active);
                if (xboxIncrease != null) xboxIncrease.gameObject.SetActive(active);
                break;
            case InputDeviceDetector.InputDeviceType.PlayStationController:
                if (psDecrease != null) psDecrease.gameObject.SetActive(active);
                if (psIncrease != null) psIncrease.gameObject.SetActive(active);
                break;
            case InputDeviceDetector.InputDeviceType.OtherGamepad:
                if (genericDecrease != null) genericDecrease.gameObject.SetActive(active);
                if (genericIncrease != null) genericIncrease.gameObject.SetActive(active);
                break;
        }
    }

    private bool IsCurrentlySelected()
    {
        return gameObject != null && gameObject.activeInHierarchy && 
               EventSystem.current != null && 
               EventSystem.current.currentSelectedGameObject == gameObject;
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        keyIsRepeating = false;
        currentSelectedButton = this;
        
        foreach (var button in allButtons)
        {
            if (button != this && button != null)
            {
                button.HideIcons();
            }
        }
        
        UpdateIconsForCurrentDevice();
        
        if (isVolumeSlider && volumeSlider != null && hasLoadedValues && cachedMusicValue >= 0)
        {
            volumeSlider.SetValueWithoutNotify(cachedMusicValue);
            UpdateVolumeText();
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        keyIsRepeating = false;
        HideIcons();
        
        if (currentSelectedButton == this)
        {
            currentSelectedButton = null;
        }
        
        if (isVolumeSlider && volumeSlider != null)
        {
            cachedMusicValue = volumeSlider.value;
        }
    }

    private void OnSliderValueChanged(float value)
    {
        cachedMusicValue = value;
        UpdateVolumeText();
    }
    
    private void UpdateVolumeText()
    {
        if (volumeText != null && volumeSlider != null)
        {
            int vol = Mathf.RoundToInt(volumeSlider.value * 100);
            volumeText.text = vol.ToString();
        }
    }
    
    public void ForceUpdateIcons()
    {
        if (IsCurrentlySelected())
        {
            UpdateIconsForCurrentDevice();
        }
    }

    private void ValidateReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (isVolumeSlider)
        {
            if (volumeSlider == null)
                Debug.LogWarning($"Volume Slider is missing on {gameObject.name}");
            
            if (volumeText == null)
                Debug.LogWarning($"Volume Text is missing on {gameObject.name}");
        }
    }

    private void Start()
    {
        ValidateReferences();
        
        if (isVolumeSlider && volumeSlider != null && !hasLoadedValues)
        {
            cachedMusicValue = volumeSlider.value;
            hasLoadedValues = true;
            UpdateVolumeText();
        }
    }
}