using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// คลาสที่จัดการไอคอนปุ่มกดที่เปลี่ยนไปตามอุปกรณ์อินพุต
/// </summary>

[Serializable]
public class ButtonPrompt
{
    [Tooltip("ชื่อของปุ่ม/คำสั่ง (เพื่อการอ้างอิง)")]
    public string actionName;
    
    [Tooltip("คอมโพเนนต์ Image หรือ TextMeshProUGUI ที่ต้องการเปลี่ยนไอคอน")]
    public Component targetComponent;
    
    [Header("Icons")]
    [Tooltip("ไอคอนสำหรับคีย์บอร์ด/เมาส์")]
    public Sprite keyboardMouseIcon;
    
    [Tooltip("ไอคอนสำหรับ Xbox Controller")]
    public Sprite xboxIcon;
    
    [Tooltip("ไอคอนสำหรับ PlayStation Controller")]
    public Sprite playstationIcon;
    
    [Tooltip("ไอคอนสำหรับ Gamepad อื่นๆ")]
    public Sprite otherGamepadIcon;
    
    [Header("Texts")]
    [Tooltip("ข้อความสำหรับคีย์บอร์ด/เมาส์")]
    public string keyboardMouseText = "คลิก";
    
    [Tooltip("ข้อความสำหรับ Xbox Controller")]
    public string xboxText = "A";
    
    [Tooltip("ข้อความสำหรับ PlayStation Controller")]
    public string playstationText = "×";
    
    [Tooltip("ข้อความสำหรับ Gamepad อื่นๆ")]
    public string otherGamepadText = "กด";

    /// <summary>
    /// อัปเดตไอคอนหรือข้อความตามชนิดของอุปกรณ์อินพุต
    /// </summary>
    public void UpdateForInputDevice(InputDeviceDetector.InputDeviceType deviceType)
    {
        if (targetComponent == null) return;
        
        // ถ้าเป็น Image
        if (targetComponent is Image imageComponent)
        {
            Sprite iconToUse = GetSpriteForDevice(deviceType);
            if (iconToUse != null)
            {
                imageComponent.sprite = iconToUse;
            }
        }
        // ถ้าเป็น TextMeshProUGUI
        else if (targetComponent is TextMeshProUGUI textComponent)
        {
            string textToUse = GetTextForDevice(deviceType);
            textComponent.text = textToUse;
        }
        else
        {
            Debug.LogWarning($"ButtonPrompt: คอมโพเนนต์ {targetComponent.GetType().Name} ไม่รองรับ ต้องเป็น Image หรือ TextMeshProUGUI เท่านั้น");
        }
    }
    
    /// <summary>
    /// รับไอคอนตามชนิดของอุปกรณ์อินพุต
    /// </summary>
    private Sprite GetSpriteForDevice(InputDeviceDetector.InputDeviceType deviceType)
    {
        switch (deviceType)
        {
            case InputDeviceDetector.InputDeviceType.MouseKeyboard:
                return keyboardMouseIcon;
            case InputDeviceDetector.InputDeviceType.XboxController:
                return xboxIcon;
            case InputDeviceDetector.InputDeviceType.PlayStationController:
                return playstationIcon;
            case InputDeviceDetector.InputDeviceType.OtherGamepad:
                return otherGamepadIcon;
            default:
                return keyboardMouseIcon;
        }
    }
    
    /// <summary>
    /// รับข้อความตามชนิดของอุปกรณ์อินพุต
    /// </summary>
    private string GetTextForDevice(InputDeviceDetector.InputDeviceType deviceType)
    {
        switch (deviceType)
        {
            case InputDeviceDetector.InputDeviceType.MouseKeyboard:
                return keyboardMouseText;
            case InputDeviceDetector.InputDeviceType.XboxController:
                return xboxText;
            case InputDeviceDetector.InputDeviceType.PlayStationController:
                return playstationText;
            case InputDeviceDetector.InputDeviceType.OtherGamepad:
                return otherGamepadText;
            default:
                return keyboardMouseText;
        }
    }
}

/// <summary>
/// คลาสสำหรับแสดงข้อมูลอุปกรณ์อินพุตปัจจุบันบน UI
/// </summary>
public class InputDeviceDisplay : MonoBehaviour
{
    [SerializeField] private InputDeviceDetector inputDetector;
    [SerializeField] private TextMeshProUGUI deviceNameText;
    [SerializeField] private TextMeshProUGUI deviceTypeText;
    [SerializeField] private Image deviceIcon;
    
    [Header("Device Icons")]
    [SerializeField] private Sprite keyboardMouseIcon;
    [SerializeField] private Sprite xboxIcon;
    [SerializeField] private Sprite playstationIcon;
    [SerializeField] private Sprite genericGamepadIcon;
    
    private void Awake()
    {
        if (inputDetector == null)
        {
            inputDetector = FindObjectOfType<InputDeviceDetector>();
        }
    }
    
    private void OnEnable()
    {
        if (inputDetector != null)
        {
            InputDeviceDetector.OnDeviceChanged += UpdateDeviceDisplay;
            UpdateDeviceDisplay(inputDetector.GetCurrentDevice());
        }
    }
    
    private void OnDisable()
    {
        InputDeviceDetector.OnDeviceChanged -= UpdateDeviceDisplay;
    }
    
    private void UpdateDeviceDisplay(InputDeviceDetector.InputDeviceType deviceType)
    {
        // อัปเดตไอคอน
        if (deviceIcon != null)
        {
            deviceIcon.sprite = GetDeviceIcon(deviceType);
        }
        
        // อัปเดตข้อความประเภทอุปกรณ์
        if (deviceTypeText != null)
        {
            string deviceTypeName = GetDeviceTypeName(deviceType);
            deviceTypeText.text = deviceTypeName;
        }
        
        // อัปเดตชื่ออุปกรณ์
        if (deviceNameText != null && inputDetector != null)
        {
            string deviceName = deviceType == InputDeviceDetector.InputDeviceType.MouseKeyboard 
                ? "คีย์บอร์ดและเมาส์" 
                : inputDetector.GetCurrentGamepadName();
            
            deviceNameText.text = deviceName;
        }
    }
    
    private Sprite GetDeviceIcon(InputDeviceDetector.InputDeviceType deviceType)
    {
        switch (deviceType)
        {
            case InputDeviceDetector.InputDeviceType.MouseKeyboard:
                return keyboardMouseIcon;
            case InputDeviceDetector.InputDeviceType.XboxController:
                return xboxIcon;
            case InputDeviceDetector.InputDeviceType.PlayStationController:
                return playstationIcon;
            case InputDeviceDetector.InputDeviceType.OtherGamepad:
                return genericGamepadIcon;
            default:
                return keyboardMouseIcon;
        }
    }
    
    private string GetDeviceTypeName(InputDeviceDetector.InputDeviceType deviceType)
    {
        switch (deviceType)
        {
            case InputDeviceDetector.InputDeviceType.MouseKeyboard:
                return "คีย์บอร์ด & เมาส์";
            case InputDeviceDetector.InputDeviceType.XboxController:
                return "Xbox Controller";
            case InputDeviceDetector.InputDeviceType.PlayStationController:
                return "PlayStation Controller";
            case InputDeviceDetector.InputDeviceType.OtherGamepad:
                return "Gamepad";
            default:
                return "ไม่ทราบอุปกรณ์";
        }
    }
}