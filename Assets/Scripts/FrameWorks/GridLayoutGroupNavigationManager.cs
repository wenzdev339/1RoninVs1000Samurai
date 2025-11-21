using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GridLayoutGroupNavigationManager : MonoBehaviour
{
    [Header("Button Navigation")]
    [SerializeField] private List<Button> predefinedButtons = new List<Button>(); // ปุ่มที่ล็อกไว้ล่วงหน้า
    [SerializeField] private Transform dynamicButtonContainer; // Container สำหรับปุ่มที่สร้างแบบไดนามิก
    [SerializeField] private GridLayoutGroup gridLayoutGroup; // GridLayoutGroup component

    [Header("Input Actions")]
    [SerializeField] private ControllerAction controllerAction; // Input Action สำหรับ UIController

    [Header("Grid Settings")]
    [SerializeField] private int columnsCount = 3; // จำนวนคอลัมน์ใน Grid (ถ้าไม่ได้ใช้ GridLayoutGroup)
    [SerializeField] private bool autoDetectColumns = true; // ตรวจจับจำนวนคอลัมน์อัตโนมัติจาก GridLayoutGroup
    [SerializeField] private bool wrapHorizontal = true; // วนกลับไปด้านตรงข้ามเมื่อเลื่อนซ้าย-ขวาถึงขอบ
    [SerializeField] private bool wrapVertical = true; // วนกลับไปด้านตรงข้ามเมื่อเลื่อนขึ้น-ลงถึงขอบ

    private List<Button> allButtons = new List<Button>(); // รวมปุ่มทั้งหมด (ล็อกไว้ + ไดนามิก)
    private int currentButtonIndex = 0;

    [Header("Auto Navigation")]
    [SerializeField] private float repeatDelay = 0.3f; // เวลาเริ่มต้นก่อนเลื่อนครั้งแรก
    [SerializeField] private float repeatRate = 0.1f; // ความเร็วในการเลื่อนเมื่อกดค้าง

    private float nextMoveTime = 0f; // เวลาที่สามารถเลื่อนครั้งถัดไปได้
    private bool isNavigatingUp = false;
    private bool isNavigatingDown = false;
    private bool isNavigatingLeft = false;
    private bool isNavigatingRight = false;

    private void Awake()
    {
        // ถ้ายังไม่มี ControllerAction ให้พยายามค้นหา
        if (controllerAction == null)
        {
            controllerAction = new ControllerAction();
        }

        // ตรวจจับ GridLayoutGroup อัตโนมัติถ้าไม่ได้กำหนด
        if (gridLayoutGroup == null && dynamicButtonContainer != null)
        {
            gridLayoutGroup = dynamicButtonContainer.GetComponent<GridLayoutGroup>();
        }
    }

    private void OnEnable()
    {
        // เปิดใช้งาน Input Actions
        controllerAction.UIController.Enable();

        // ลงทะเบียน Input Actions
        controllerAction.UIController.Up.performed += OnUpPerformed;
        controllerAction.UIController.Down.performed += OnDownPerformed;
        controllerAction.UIController.Left.performed += OnLeftPerformed;
        controllerAction.UIController.Right.performed += OnRightPerformed;
        
        controllerAction.UIController.Up.canceled += OnUpCanceled;
        controllerAction.UIController.Down.canceled += OnDownCanceled;
        controllerAction.UIController.Left.canceled += OnLeftCanceled;
        controllerAction.UIController.Right.canceled += OnRightCanceled;

        RefreshDynamicButtons();
        
        // ✅ เลือกปุ่มอันดับ 1 เมื่อเปิด UI
        SelectFirstActiveButton();
    }

    private void OnDisable()
    {
        // ยกเลิกการลงทะเบียน Input Actions
        controllerAction.UIController.Up.performed -= OnUpPerformed;
        controllerAction.UIController.Down.performed -= OnDownPerformed;
        controllerAction.UIController.Left.performed -= OnLeftPerformed;
        controllerAction.UIController.Right.performed -= OnRightPerformed;
        
        controllerAction.UIController.Up.canceled -= OnUpCanceled;
        controllerAction.UIController.Down.canceled -= OnDownCanceled;
        controllerAction.UIController.Left.canceled -= OnLeftCanceled;
        controllerAction.UIController.Right.canceled -= OnRightCanceled;

        // ปิดการใช้งาน Input Actions
        controllerAction.UIController.Disable();
    }

    private void Update()
    {
        RefreshDynamicButtons();
        HandleGridNavigation();
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

    private void OnLeftPerformed(InputAction.CallbackContext context)
    {
        isNavigatingLeft = true;
        NavigateLeft();
    }

    private void OnRightPerformed(InputAction.CallbackContext context)
    {
        isNavigatingRight = true;
        NavigateRight();
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

    private void OnLeftCanceled(InputAction.CallbackContext context)
    {
        isNavigatingLeft = false;
        nextMoveTime = 0f;
    }

    private void OnRightCanceled(InputAction.CallbackContext context)
    {
        isNavigatingRight = false;
        nextMoveTime = 0f;
    }

    private void HandleGridNavigation()
    {
        if (!IsThisUIActive())
        {
            return; // ถ้าไม่ใช่ UI ที่ Active ให้หยุดควบคุม
        }

        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count == 0) return;

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
        else if (isNavigatingLeft && Time.unscaledTime >= nextMoveTime)
        {
            NavigateLeft();
            nextMoveTime = Time.unscaledTime + repeatRate;
        }
        else if (isNavigatingRight && Time.unscaledTime >= nextMoveTime)
        {
            NavigateRight();
            nextMoveTime = Time.unscaledTime + repeatRate;
        }

        // ถ้าไม่มีปุ่มถูก Select อยู่ ให้ Select ปุ่มปัจจุบัน
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            SelectCurrentActiveButton();
        }
    }

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

        return IsTopCanvas(thisCanvas);
    }

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

    private int GetColumnsCount()
    {
        if (autoDetectColumns && gridLayoutGroup != null)
        {
            // คำนวณจำนวนคอลัมน์จาก GridLayoutGroup
            RectTransform rectTransform = gridLayoutGroup.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                float availableWidth = rectTransform.rect.width - gridLayoutGroup.padding.left - gridLayoutGroup.padding.right;
                float cellWidth = gridLayoutGroup.cellSize.x + gridLayoutGroup.spacing.x;
                
                if (cellWidth > 0)
                {
                    int calculatedColumns = Mathf.FloorToInt((availableWidth + gridLayoutGroup.spacing.x) / cellWidth);
                    return Mathf.Max(1, calculatedColumns);
                }
            }
        }
        
        return Mathf.Max(1, columnsCount);
    }

    private (int row, int column) GetGridPosition(int buttonIndex)
    {
        int columns = GetColumnsCount();
        int row = buttonIndex / columns;
        int column = buttonIndex % columns;
        return (row, column);
    }

    private int GetButtonIndex(int row, int column)
    {
        int columns = GetColumnsCount();
        return row * columns + column;
    }

    /// <summary>
    /// นำทางไปทางขวา
    /// </summary>
    private void NavigateRight()
    {
        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count == 0) return;

        int currentActiveIndex = GetCurrentActiveButtonIndex();
        Button currentButton = activeButtons[currentActiveIndex];
        int currentIndex = allButtons.IndexOf(currentButton);
        
        var (currentRow, currentColumn) = GetGridPosition(currentIndex);
        int columns = GetColumnsCount();
        
        // คำนวณตำแหน่งปุ่มถัดไป
        int nextColumn = currentColumn + 1;
        int nextRow = currentRow;
        
        // ถ้าเลื่อนเกินขอบขวา
        if (nextColumn >= columns)
        {
            if (wrapHorizontal)
            {
                nextColumn = 0; // วนกลับไปด้านซ้าย
            }
            else
            {
                return; // ไม่ทำอะไร
            }
        }
        
        int targetIndex = GetButtonIndex(nextRow, nextColumn);
        
        // หาปุ่มที่เปิดใช้งานที่ตำแหน่งเป้าหมายหรือใกล้เคียงที่สุด
        Button targetButton = FindNearestActiveButton(targetIndex, activeButtons);
        if (targetButton != null)
        {
            currentButtonIndex = allButtons.IndexOf(targetButton);
            SelectButton(currentButtonIndex);
            nextMoveTime = Time.unscaledTime + repeatDelay;
        }
    }

    private void NavigateLeft()
    {
        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count == 0) return;

        int currentActiveIndex = GetCurrentActiveButtonIndex();
        Button currentButton = activeButtons[currentActiveIndex];
        int currentIndex = allButtons.IndexOf(currentButton);
        
        var (currentRow, currentColumn) = GetGridPosition(currentIndex);
        int columns = GetColumnsCount();
        
        // คำนวณตำแหน่งปุ่มถัดไป
        int nextColumn = currentColumn - 1;
        int nextRow = currentRow;
        
        // ถ้าเลื่อนเกินขอบซ้าย
        if (nextColumn < 0)
        {
            if (wrapHorizontal)
            {
                nextColumn = columns - 1; // วนกลับไปด้านขวา
            }
            else
            {
                return; // ไม่ทำอะไร
            }
        }
        
        int targetIndex = GetButtonIndex(nextRow, nextColumn);
        
        // หาปุ่มที่เปิดใช้งานที่ตำแหน่งเป้าหมายหรือใกล้เคียงที่สุด
        Button targetButton = FindNearestActiveButton(targetIndex, activeButtons);
        if (targetButton != null)
        {
            currentButtonIndex = allButtons.IndexOf(targetButton);
            SelectButton(currentButtonIndex);
            nextMoveTime = Time.unscaledTime + repeatDelay;
        }
    }

    private void NavigateDown()
    {
        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count == 0) return;

        int currentActiveIndex = GetCurrentActiveButtonIndex();
        Button currentButton = activeButtons[currentActiveIndex];
        int currentIndex = allButtons.IndexOf(currentButton);
        
        var (currentRow, currentColumn) = GetGridPosition(currentIndex);
        int columns = GetColumnsCount();
        int totalRows = Mathf.CeilToInt((float)allButtons.Count / columns);
        
        // คำนวณตำแหน่งปุ่มถัดไป
        int nextRow = currentRow + 1;
        int nextColumn = currentColumn;
        
        // ถ้าเลื่อนเกินขอบล่าง
        if (nextRow >= totalRows)
        {
            if (wrapVertical)
            {
                nextRow = 0; // วนกลับไปแถวบนสุด
            }
            else
            {
                return; // ไม่ทำอะไร
            }
        }
        
        int targetIndex = GetButtonIndex(nextRow, nextColumn);
        
        // หาปุ่มที่เปิดใช้งานที่ตำแหน่งเป้าหมายหรือใกล้เคียงที่สุด
        Button targetButton = FindNearestActiveButton(targetIndex, activeButtons);
        if (targetButton != null)
        {
            currentButtonIndex = allButtons.IndexOf(targetButton);
            SelectButton(currentButtonIndex);
            nextMoveTime = Time.unscaledTime + repeatDelay;
        }
    }

    private void NavigateUp()
    {
        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count == 0) return;

        int currentActiveIndex = GetCurrentActiveButtonIndex();
        Button currentButton = activeButtons[currentActiveIndex];
        int currentIndex = allButtons.IndexOf(currentButton);
        
        var (currentRow, currentColumn) = GetGridPosition(currentIndex);
        int columns = GetColumnsCount();
        int totalRows = Mathf.CeilToInt((float)allButtons.Count / columns);
        
        // คำนวณตำแหน่งปุ่มถัดไป
        int nextRow = currentRow - 1;
        int nextColumn = currentColumn;
        
        // ถ้าเลื่อนเกินขอบบน
        if (nextRow < 0)
        {
            if (wrapVertical)
            {
                nextRow = totalRows - 1; // วนกลับไปแถวล่างสุด
            }
            else
            {
                return; // ไม่ทำอะไร
            }
        }
        
        int targetIndex = GetButtonIndex(nextRow, nextColumn);
        
        // หาปุ่มที่เปิดใช้งานที่ตำแหน่งเป้าหมายหรือใกล้เคียงที่สุด
        Button targetButton = FindNearestActiveButton(targetIndex, activeButtons);
        if (targetButton != null)
        {
            currentButtonIndex = allButtons.IndexOf(targetButton);
            SelectButton(currentButtonIndex);
            nextMoveTime = Time.unscaledTime + repeatDelay;
        }
    }

    private Button FindNearestActiveButton(int targetIndex, List<Button> activeButtons)
    {
        // ถ้าตำแหน่งเป้าหมายอยู่นอกขอบเขต ให้หาปุ่มที่ใกล้เคียงที่สุด
        targetIndex = Mathf.Clamp(targetIndex, 0, allButtons.Count - 1);
        
        // ถ้าปุ่มที่ตำแหน่งเป้าหมาย active อยู่ ใช้เลย
        if (targetIndex < allButtons.Count && IsButtonActive(allButtons[targetIndex]))
        {
            return allButtons[targetIndex];
        }
        
        // หาปุ่มที่ active ที่มี index ใกล้เคียงที่สุด
        Button nearestButton = null;
        int nearestDistance = int.MaxValue;
        
        foreach (Button activeButton in activeButtons)
        {
            int activeIndex = allButtons.IndexOf(activeButton);
            int distance = Mathf.Abs(activeIndex - targetIndex);
            
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestButton = activeButton;
            }
        }
        
        return nearestButton;
    }

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
        
        return true;
    }

    private List<Button> GetActiveButtons()
    {
        List<Button> activeButtons = new List<Button>();
        foreach (Button button in allButtons)
        {
            if (IsButtonActive(button))
            {
                activeButtons.Add(button);
            }
        }
        return activeButtons;
    }

    private int GetCurrentActiveButtonIndex()
    {
        List<Button> activeButtons = GetActiveButtons();
        if (activeButtons.Count == 0) return 0;

        // ถ้าปุ่มปัจจุบันยัง active อยู่
        if (currentButtonIndex >= 0 && currentButtonIndex < allButtons.Count && IsButtonActive(allButtons[currentButtonIndex]))
        {
            Button currentButton = allButtons[currentButtonIndex];
            int activeIndex = activeButtons.IndexOf(currentButton);
            return activeIndex >= 0 ? activeIndex : 0;
        }

        // ถ้าปุ่มปัจจุบันไม่ active ให้ return 0
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

    /// เมธอดสำหรับการตั้งค่าปุ่มเริ่มต้น (เรียกจากภายนอก)
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

    public void SetInitialButtonByGridPosition(int row, int column)
    {
        int buttonIndex = GetButtonIndex(row, column);
        SetInitialButton(buttonIndex);
    }

    public int GetActiveButtonCount()
    {
        return GetActiveButtons().Count;
    }

    public void SetColumnsCount(int columns)
    {
        columnsCount = Mathf.Max(1, columns);
        autoDetectColumns = false;
    }

    public void SetWrapHorizontal(bool wrap)
    {
        wrapHorizontal = wrap;
    }

    public void SetWrapVertical(bool wrap)
    {
        wrapVertical = wrap;
    }
}