using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;

public class OnClickInvoke : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private Button targetButton;
    
    [Header("Input Settings")]
    [SerializeField] private ControllerAction controllerAction;
    
    [Header("Action Type")]
    [SerializeField] private ActionType actionType = ActionType.Cancel;
    
    public enum ActionType
    {
        Cancel,
        Submit,
        Back,
        Menu,
        Confirm,
        Custom
    }
    
    private InputAction currentAction;

    private void Awake()
    {
        // ถ้ายังไม่มี Button ให้พยายามค้นหาจาก Component
        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();
        }
        
        // ถ้ายังไม่มี ControllerAction ให้สร้างใหม่
        if (controllerAction == null)
        {
            controllerAction = new ControllerAction();
        }
        
        // กำหนด InputAction ตาม ActionType ที่เลือก
        SetupInputAction();
    }

    private void SetupInputAction()
    {
        switch (actionType)
        {
            case ActionType.Cancel:
                currentAction = controllerAction.UIController.Cancel;
                break;
            case ActionType.Submit:
                currentAction = controllerAction.UIController.Submit;
                break;
            case ActionType.Back:
                currentAction = controllerAction.UIController.Cancel; // หรือสร้าง Back action ใหม่
                break;
            case ActionType.Menu:
                // currentAction = controllerAction.UIController.Menu;
                currentAction = controllerAction.UIController.Cancel; // fallback
                break;
            case ActionType.Confirm:
                currentAction = controllerAction.UIController.Submit;
                break;
            case ActionType.Custom:
                // สำหรับ Custom action สามารถเพิ่มเติมได้
                currentAction = controllerAction.UIController.Cancel; // fallback
                break;
        }
    }

    private void OnEnable()
    {
        if (currentAction == null) return;
        
        // เปิดใช้งาน Input Actions
        controllerAction.UIController.Enable();

        // ลงทะเบียน Input Action
        currentAction.performed += OnActionPerformed;
    }

    private void OnDisable()
    {
        if (currentAction == null) return;
        
        // ยกเลิกการลงทะเบียน Input Action
        currentAction.performed -= OnActionPerformed;

        // ปิดการใช้งาน Input Actions
        controllerAction.UIController.Disable();
    }
    
    private void OnActionPerformed(InputAction.CallbackContext context)
    {
        // ตรวจสอบว่าปุ่มยังใช้งานได้อยู่หรือไม่
        if (targetButton != null && targetButton.interactable && targetButton.gameObject.activeInHierarchy)
        {
            // เรียกใช้ onClick event ของปุ่ม
            targetButton.onClick.Invoke();
        }
    }
    
    // Method สำหรับเปลี่ยน Action Type แบบ Runtime
    public void SetActionType(ActionType newActionType)
    {
        // ยกเลิกการลงทะเบียน Action เดิม
        if (currentAction != null)
        {
            currentAction.performed -= OnActionPerformed;
        }
        
        // เปลี่ยน Action Type
        actionType = newActionType;
        SetupInputAction();
        
        // ลงทะเบียน Action ใหม่
        if (currentAction != null && enabled)
        {
            currentAction.performed += OnActionPerformed;
        }
    }
    
    // Method สำหรับเปลี่ยนปุ่มเป้าหมาย
    public void SetTargetButton(Button newButton)
    {
        targetButton = newButton;
    }
}