using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class VerticalNavigationManager : MonoBehaviour
{
    [Header("Button Navigation")]
    [SerializeField] private List<Button> predefinedButtons = new List<Button>(); // ปุ่มที่ล็อกไว้ล่วงหน้า
    [SerializeField] private Transform dynamicButtonContainer; // Container สำหรับปุ่มที่สร้างแบบไดนามิก

    [Header("Input Actions")]
    [SerializeField] private ControllerAction controllerAction; // Input Action สำหรับ UIController

    private List<Button> allButtons = new List<Button>(); // รวมปุ่มทั้งหมด (ล็อกไว้ + ไดนามิก)
    private int currentButtonIndex = 0;

    [Header("Auto Navigation")]
    [SerializeField] private float repeatDelay = 0.3f; // เวลาเริ่มต้นก่อนเลื่อนครั้งแรก
    [SerializeField] private float repeatRate = 0.1f; // ความเร็วในการเลื่อนเมื่อกดค้าง

    private float nextMoveTime = 0f; // เวลาที่สามารถเลื่อนครั้งถัดไปได้
    private bool isNavigatingUp = false;
    private bool isNavigatingDown = false;

    private void Awake()
    {
        // ถ้ายังไม่มี ControllerAction ให้พยายามค้นหา
        if (controllerAction == null)
        {
            controllerAction = new ControllerAction();
        }
    }

    private void OnEnable()
    {
        // เปิดใช้งาน Input Actions
        controllerAction.UIController.Enable();

        // ลงทะเบียน Input Actions
        controllerAction.UIController.Up.performed += OnUpPerformed;
        controllerAction.UIController.Down.performed += OnDownPerformed;
        
        controllerAction.UIController.Up.canceled += OnUpCanceled;
        controllerAction.UIController.Down.canceled += OnDownCanceled;

        RefreshDynamicButtons();
        
        // ✅ เลือกปุ่มอันดับ 1 เมื่อเปิด UI
        SelectFirstActiveButton();
    }

    private void OnDisable()
    {
        // ยกเลิกการลงทะเบียน Input Actions
        controllerAction.UIController.Up.performed -= OnUpPerformed;
        controllerAction.UIController.Down.performed -= OnDownPerformed;
        
        controllerAction.UIController.Up.canceled -= OnUpCanceled;
        controllerAction.UIController.Down.canceled -= OnDownCanceled;

        // ปิดการใช้งาน Input Actions
        controllerAction.UIController.Disable();
    }

    private void Update()
    {
        RefreshDynamicButtons();
        HandleVerticalNavigation();
        EnsureSelectedButton();
    }

    private void OnUpPerformed(InputAction.CallbackContext context)
    {
        isNavigatingUp = true;
        NavigateUp();
    }

    private void OnDownPerformed(InputAction.CallbackContext context)
    {
        isNavigatingDown = true;
        NavigateDown();
    }

    private void OnUpCanceled(InputAction.CallbackContext context)
    {
        isNavigatingUp = false;
        nextMoveTime = 0f;
    }

    private void OnDownCanceled(InputAction.CallbackContext context)
    {
        isNavigatingDown = false;
        nextMoveTime = 0f;
    }

    private void HandleVerticalNavigation()
    {
        if (!IsThisUIActive())
        {
            return; // ถ้าไม่ใช่ UI ที่ Active ให้หยุดควบคุม
        }

        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count == 0) return;

        // การนำทางแบบค้างปุ่ม ✅ ใช้ unscaledTime แทน Time.time
        if (isNavigatingDown && Time.unscaledTime >= nextMoveTime)
        {
            NavigateDown();
            nextMoveTime = Time.unscaledTime + repeatRate;
        }
        else if (isNavigatingUp && Time.unscaledTime >= nextMoveTime)
        {
            NavigateUp();
            nextMoveTime = Time.unscaledTime + repeatRate;
        }

        // ถ้าไม่มีปุ่มถูก Select อยู่ ให้ Select ปุ่มปัจจุบัน
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            SelectCurrentActiveButton();
        }
    }

    /// <summary>
    /// ✅ เช็คว่า UI นี้เป็นเจ้าของปุ่มที่ถูก Select อยู่หรือไม่ และเป็น Canvas ที่อยู่บนสุด
    /// </summary>
    private bool IsThisUIActive()
    {
        // ถ้าไม่มี GameObject ที่ถูก Select อยู่เลย
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            return false;
        }

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        
        // เช็คว่าปุ่มที่ถูก Select อยู่ใน allButtons หรือไม่
        Button selectedButton = selectedObject.GetComponent<Button>();
        if (selectedButton == null || !allButtons.Contains(selectedButton))
        {
            return false;
        }

        // ✅ เช็คว่า Canvas ของ UI นี้เป็น Canvas ที่อยู่บนสุดหรือไม่
        Canvas thisCanvas = GetComponentInParent<Canvas>();
        Canvas selectedCanvas = selectedObject.GetComponentInParent<Canvas>();

        if (thisCanvas == null || selectedCanvas == null)
        {
            return false;
        }

        // ถ้าไม่ใช่ Canvas เดียวกัน ให้เปรียบเทียบ sortingOrder
        if (thisCanvas != selectedCanvas)
        {
            return false;
        }

        // ✅ เช็คว่า Canvas นี้เป็น Top Canvas (sortingOrder สูงสุด)
        return IsTopCanvas(thisCanvas);
    }

    /// <summary>
    /// ✅ เช็คว่า Canvas นี้เป็น Canvas ที่มี sortingOrder สูงสุดและ Active อยู่หรือไม่
    /// </summary>
    private bool IsTopCanvas(Canvas targetCanvas)
    {
        // หา Canvas ทั้งหมดที่ Active อยู่
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        
        int maxSortingOrder = int.MinValue;
        
        // หา sortingOrder สูงสุดของ Canvas ที่ Active
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.gameObject.activeInHierarchy && canvas.sortingOrder > maxSortingOrder)
            {
                maxSortingOrder = canvas.sortingOrder;
            }
        }

        // ถ้า Canvas นี้มี sortingOrder เท่ากับสูงสุด แสดงว่าเป็น Top Canvas
        return targetCanvas.sortingOrder >= maxSortingOrder;
    }

    private void NavigateDown()
    {
        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count == 0) return;

        // หา index ของปุ่มที่เปิดใช้งานถัดไปจากปุ่มปัจจุบัน
        int currentActiveIndex = GetCurrentActiveButtonIndex();
        int nextActiveIndex = (currentActiveIndex + 1) % activeButtons.Count;
        
        // อัปเดต currentButtonIndex ให้ตรงกับปุ่มใหม่
        currentButtonIndex = allButtons.IndexOf(activeButtons[nextActiveIndex]);
        SelectButton(currentButtonIndex);
        nextMoveTime = Time.unscaledTime + repeatDelay;
    }

    private void NavigateUp()
    {
        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count == 0) return;

        // หา index ของปุ่มที่เปิดใช้งานก่อนหน้าจากปุ่มปัจจุบัน
        int currentActiveIndex = GetCurrentActiveButtonIndex();
        int prevActiveIndex = (currentActiveIndex - 1 + activeButtons.Count) % activeButtons.Count;
        
        // อัปเดต currentButtonIndex ให้ตรงกับปุ่มใหม่
        currentButtonIndex = allButtons.IndexOf(activeButtons[prevActiveIndex]);
        SelectButton(currentButtonIndex);
        nextMoveTime = Time.unscaledTime + repeatDelay;
    }

    /// <summary>
    /// ตรวจสอบว่าปุ่มยังมีอยู่และเปิดใช้งานหรือไม่ (Enhanced version)
    /// </summary>
    private bool IsButtonActive(Button button)
    {
        // ตรวจสอบว่าปุ่มไม่เป็น null
        if (button == null) 
            return false;
        
        // ตรวจสอบว่า GameObject ยังมีอยู่
        if (button.gameObject == null) 
            return false;
        
        // ตรวจสอบว่า GameObject ยัง active ใน hierarchy
        if (!button.gameObject.activeInHierarchy) 
            return false;
        
        // ตรวจสอบว่า Button component ยัง enabled
        if (!button.enabled) 
            return false;
        
        // ตรวจสอบว่าปุ่มยัง interactable
        if (!button.interactable) 
            return false;
        
        // ตรวจสอบเพิ่มเติม: ว่า CanvasGroup ไม่บล็อกการทำงาน (ถ้ามี)
        CanvasGroup canvasGroup = button.GetComponentInParent<CanvasGroup>();
        if (canvasGroup != null && (!canvasGroup.interactable || canvasGroup.alpha <= 0f))
            return false;

        return true;
    }

    /// <summary>
    /// รับรายชื่อปุ่มที่เปิดใช้งานทั้งหมด
    /// </summary>
    private List<Button> GetActiveButtons()
    {
        List<Button> activeButtons = new List<Button>();
        
        // ตรวจสอบทุกปุ่มใน allButtons
        for (int i = 0; i < allButtons.Count; i++)
        {
            Button button = allButtons[i];
            if (IsButtonActive(button))
            {
                activeButtons.Add(button);
            }
        }
        
        return activeButtons;
    }

    /// <summary>
    /// หา index ของปุ่มปัจจุบันในรายการปุ่มที่เปิดใช้งาน
    /// </summary>
    private int GetCurrentActiveButtonIndex()
    {
        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count == 0) return 0;

        // หาปุ่มปัจจุบันในรายการปุ่มที่เปิดใช้งาน
        if (currentButtonIndex >= 0 && currentButtonIndex < allButtons.Count)
        {
            Button currentButton = allButtons[currentButtonIndex];
            
            // ตรวจสอบว่าปุ่มปัจจุบันยัง active อยู่
            if (IsButtonActive(currentButton))
            {
                int activeIndex = activeButtons.IndexOf(currentButton);
                if (activeIndex != -1)
                {
                    return activeIndex;
                }
            }
        }

        // ถ้าไม่พบปุ่มปัจจุบันในรายการที่เปิดใช้งาน ให้เลือกปุ่มแรกที่เปิดใช้งาน
        return 0;
    }

    private void SelectFirstActiveButton()
    {
        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count == 0)
        {
            EventSystem.current.SetSelectedGameObject(null);
            currentButtonIndex = -1;
            return;
        }

        // เลือกปุ่มแรกที่ active
        Button firstButton = activeButtons[0];
        currentButtonIndex = allButtons.IndexOf(firstButton);
        EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
    }

    /// <summary>
    /// เลือกปุ่มที่เปิดใช้งานปัจจุบัน
    /// </summary>
    private void SelectCurrentActiveButton()
    {
        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count == 0) 
        {
            // ถ้าไม่มีปุ่มที่ active เหลืออยู่
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        // ถ้าปุ่มปัจจุบันยังเปิดใช้งานอยู่
        if (currentButtonIndex >= 0 && currentButtonIndex < allButtons.Count && IsButtonActive(allButtons[currentButtonIndex]))
        {
            SelectButton(currentButtonIndex);
        }
        else
        {
            // หาปุ่มที่เปิดใช้งานแรกและเลือก
            Button firstActiveButton = activeButtons[0];
            currentButtonIndex = allButtons.IndexOf(firstActiveButton);
            SelectButton(currentButtonIndex);
        }
    }

    private void CollectAllButtons()
    {
        allButtons.Clear();

        // เพิ่มปุ่มที่ล็อกไว้ล่วงหน้า (กรองเฉพาะที่ไม่ใช่ null)
        foreach (Button button in predefinedButtons)
        {
            if (button != null)
            {
                allButtons.Add(button);
            }
        }

        // เพิ่มปุ่มที่สร้างแบบไดนามิก
        if (dynamicButtonContainer != null)
        {
            foreach (Transform child in dynamicButtonContainer)
            {
                if (child != null)
                {
                    Button button = child.GetComponent<Button>();
                    if (button != null)
                    {
                        allButtons.Add(button);
                    }
                }
            }
        }

        // ป้องกัน index เกินขอบเขต
        if (currentButtonIndex >= allButtons.Count)
        {
            currentButtonIndex = Mathf.Max(0, allButtons.Count - 1);
        }

        // ผูก onClick กับเมธอด OnButtonClicked (ลบ listener เก่าก่อน)
        foreach (Button button in allButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(() => OnButtonClicked(button));
                button.onClick.AddListener(() => OnButtonClicked(button));
            }
        }
    }

    private void OnButtonClicked(Button clickedButton)
    {
        // ตรวจสอบว่าปุ่มที่คลิกยัง active อยู่
        if (!IsButtonActive(clickedButton))
            return;

        // หา index ของปุ่มที่ถูกคลิกจาก allButtons
        int index = allButtons.IndexOf(clickedButton);
        if (index != -1)
        {
            currentButtonIndex = index;
            SelectButton(index);
        }
    }

    private void SelectButton(int index)
    {
        if (index >= 0 && index < allButtons.Count && IsButtonActive(allButtons[index]))
        {
            EventSystem.current.SetSelectedGameObject(allButtons[index].gameObject);
        }
    }

    private void EnsureSelectedButton()
    {
        // ✅ ถ้า UI นี้ไม่ใช่เจ้าของปุ่มที่ถูก Select อยู่ ไม่ต้องทำอะไร
        if (!IsThisUIActive())
        {
            return;
        }

        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count == 0) 
        {
            EventSystem.current.SetSelectedGameObject(null);
            currentButtonIndex = -1; // รีเซ็ต index เมื่อไม่มีปุ่มที่ active
            return;
        }

        // ตรวจสอบว่าปุ่มปัจจุบันยังเปิดใช้งานอยู่หรือไม่
        if (currentButtonIndex >= 0 && currentButtonIndex < allButtons.Count)
        {
            Button currentButton = allButtons[currentButtonIndex];
            if (IsButtonActive(currentButton))
            {
                // ปุ่มปัจจุบันยัง active อยู่ ตรวจสอบว่าถูก select อยู่หรือไม่
                if (EventSystem.current.currentSelectedGameObject != currentButton.gameObject)
                {
                    EventSystem.current.SetSelectedGameObject(currentButton.gameObject);
                }
            }
            else
            {
                // ถ้าปุ่มปัจจุบันไม่เปิดใช้งาน ให้หาปุ่มที่เปิดใช้งานที่ใกล้เคียงที่สุด
                FindAndSelectNearestActiveButton();
            }
        }
        else
        {
            // ถ้า index ไม่ถูกต้อง ให้เลือกปุ่มที่เปิดใช้งานแรก
            Button firstActiveButton = activeButtons[0];
            currentButtonIndex = allButtons.IndexOf(firstActiveButton);
            EventSystem.current.SetSelectedGameObject(firstActiveButton.gameObject);
        }
    }

    /// <summary>
    /// หาและเลือกปุ่มที่เปิดใช้งานที่ใกล้เคียงที่สุดกับตำแหน่งปัจจุบัน
    /// </summary>
    private void FindAndSelectNearestActiveButton()
    {
        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count == 0) 
        {
            currentButtonIndex = -1;
            return;
        }

        // หาปุ่มที่เปิดใช้งานที่มี index ใกล้เคียงกับ currentButtonIndex ที่สุด
        Button nearestButton = activeButtons[0];
        int nearestDistance = int.MaxValue;

        foreach (Button activeButton in activeButtons)
        {
            int activeButtonIndex = allButtons.IndexOf(activeButton);
            int distance = Mathf.Abs(activeButtonIndex - currentButtonIndex);
            
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestButton = activeButton;
            }
        }

        // อัปเดต currentButtonIndex และเลือกปุ่ม
        currentButtonIndex = allButtons.IndexOf(nearestButton);
        EventSystem.current.SetSelectedGameObject(nearestButton.gameObject);
    }

    /// <summary>
    /// รีเฟรชรายการปุ่มแบบไดนามิก และอัปเดตการเลือก
    /// </summary>
    public void RefreshDynamicButtons()
    {
        // อัปเดตรายชื่อปุ่มทั้งหมด
        CollectAllButtons();

        // ตรวจสอบและเลือกปุ่มที่เปิดใช้งาน
        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count > 0)
        {
            // ถ้าปุ่มปัจจุบันยังเปิดใช้งานอยู่ ให้คงไว้
            if (currentButtonIndex >= 0 && currentButtonIndex < allButtons.Count && IsButtonActive(allButtons[currentButtonIndex]))
            {
                SelectButton(currentButtonIndex);
            }
            else
            {
                // หาปุ่มที่เปิดใช้งานที่ใกล้เคียงที่สุด
                FindAndSelectNearestActiveButton();
            }
        }
        else
        {
            // ถ้าไม่มีปุ่มเหลือให้ยกเลิกการเลือก
            EventSystem.current.SetSelectedGameObject(null);
            currentButtonIndex = -1;
        }
    }

    /// <summary>
    /// เมธอดสำหรับการตั้งค่าปุ่มเริ่มต้น (เรียกจากภายนอก)
    /// </summary>
    public void SetInitialButton(int buttonIndex)
    {
        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count == 0) return;

        if (buttonIndex >= 0 && buttonIndex < allButtons.Count && IsButtonActive(allButtons[buttonIndex]))
        {
            currentButtonIndex = buttonIndex;
            SelectButton(buttonIndex);
        }
    }

    /// <summary>
    /// ตรวจสอบจำนวนปุ่มที่ active อยู่ (สำหรับ debug)
    /// </summary>
    public int GetActiveButtonCount()
    {
        return GetActiveButtons().Count;
    }
}