using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ScrollRectAutoScroll : MonoBehaviour
{
    [Header("Scroll UI")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewportRect;
    
    [Header("Input Actions")]
    [SerializeField] private ControllerAction controllerAction; // Input Action สำหรับ UIController

    [Header("Scroll UI Mode")]
    [SerializeField] private VerticalLayoutGroup verticalLayoutGroup;

    private EventSystem eventSystem;
    private bool isMouseScrolling = false;
    private bool isMouseClicking = false;  // เพิ่มตัวแปรสำหรับตรวจจับการคลิกเมาส์
    private bool isNavigatingUp = false;
    private bool isNavigatingDown = false;
    private float lastMouseClickTime = 0f;
    private const float MOUSE_CLICK_TIMEOUT = 0.5f;  // เวลาหลังจากคลิกที่จะถือว่าไม่ได้ใช้เมาส์แล้ว

    private void Awake()
    {
        eventSystem = EventSystem.current;

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

        // ลงทะเบียน Input Action สำหรับการเลื่อน
        controllerAction.UIController.Up.performed += OnUpPerformed;
        controllerAction.UIController.Down.performed += OnDownPerformed;
        
        controllerAction.UIController.Up.canceled += OnUpCanceled;
        controllerAction.UIController.Down.canceled += OnDownCanceled;
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

    private void OnUpPerformed(InputAction.CallbackContext context)
    {
        isNavigatingUp = true;
        isMouseScrolling = false;
        isMouseClicking = false;
    }

    private void OnDownPerformed(InputAction.CallbackContext context)
    {
        isNavigatingDown = true;
        isMouseScrolling = false;
        isMouseClicking = false;
    }

    private void OnUpCanceled(InputAction.CallbackContext context)
    {
        isNavigatingUp = false;
    }

    private void OnDownCanceled(InputAction.CallbackContext context)
    {
        isNavigatingDown = false;
    }

    private void Update()
    {
        HandleInputDetection();

        // ทำการ scroll อัตโนมัติเฉพาะเมื่อไม่ได้ใช้เมาส์และมีอ็อบเจกต์ที่ถูกเลือก
        if (!isMouseClicking && !isMouseScrolling && eventSystem.currentSelectedGameObject != null && scrollRect.gameObject.activeInHierarchy)
        {
            HandleScrollToSelected();
        }
    }

    private void HandleInputDetection()
    {
        // ตรวจสอบการเลื่อนเมาส์
        if (Input.mouseScrollDelta.y != 0)
        {
            isMouseScrolling = true;
            isMouseClicking = false;
        }

        // ตรวจจับการคลิกเมาส์
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            isMouseClicking = true;
            lastMouseClickTime = Time.unscaledTime; // ✅ เปลี่ยนเป็น unscaledTime
        }

        // ตรวจสอบการใช้คีย์บอร์ด
        if (Input.GetAxis("Vertical") != 0 || (Input.anyKeyDown && !Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2)))
        {
            isMouseScrolling = false;
            isMouseClicking = false;
        }

        // ตรวจสอบ timeout หลังจากคลิกเมาส์
        if (isMouseClicking && Time.unscaledTime - lastMouseClickTime > MOUSE_CLICK_TIMEOUT) // ✅ เปลี่ยนเป็น unscaledTime
        {
            isMouseClicking = false;
        }
    }

    private void HandleScrollToSelected()
    {
        GameObject selectObject = eventSystem.currentSelectedGameObject;
        RectTransform selectedRect = selectObject.GetComponent<RectTransform>();

        if (selectedRect != null && selectedRect.IsChildOf(scrollRect.content))
        {
            int selectedIndex = selectedRect.GetSiblingIndex();
            int totalItems = scrollRect.content.childCount;
            ScrollToObject(selectObject, selectedIndex, totalItems);
        }
    }

    public void ScrollToObject(GameObject selectedObject, int selectedIndex, int totalItems)
    {
        if (!scrollRect.gameObject.activeInHierarchy || !scrollRect.content.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Cannot scroll because ScrollRect or Content is inactive.");
            return;
        }

        RectTransform selectedRect = selectedObject.GetComponent<RectTransform>();
        Vector3 selectedLocalPos = scrollRect.content.InverseTransformPoint(selectedRect.position);

        float selectedHeight = selectedRect.rect.height;
        float scrollHeight = scrollRect.content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;
        float viewableArea = scrollHeight - viewportHeight;
        float scrollLocalPosY = Mathf.Abs(selectedLocalPos.y);

        float layoutSpacing = verticalLayoutGroup.spacing;
        float viewportTop = scrollRect.viewport.rect.yMax;
        float viewportBottom = scrollRect.viewport.rect.yMin;
        Vector3 viewportLocalPos = scrollRect.viewport.InverseTransformPoint(selectedRect.position);

        float targetPos = scrollRect.verticalNormalizedPosition;

        if (viewportLocalPos.y > viewportTop - (selectedHeight / 2))
        {
            targetPos = Mathf.Clamp(scrollLocalPosY - (scrollLocalPosY / viewportHeight) - selectedHeight - layoutSpacing, 0, viewableArea);
        }
        else if (viewportLocalPos.y < viewportBottom + (selectedHeight / 2))
        {
            targetPos = selectedIndex != totalItems - 1 ? Mathf.Clamp(scrollLocalPosY - viewportHeight + selectedHeight / 2 + layoutSpacing, 0, viewableArea) : viewableArea;
        }

        if (scrollRect.verticalNormalizedPosition != targetPos)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothScrollCoroutine(targetPos));
        }
    }

    private IEnumerator SmoothScrollCoroutine(float targetPos)
    {
        RectTransform rectTransform = scrollRect.content;
        float scrollSensitivity = scrollRect.scrollSensitivity;
        float currentPos = rectTransform.anchoredPosition.y;

        while (Mathf.Abs(currentPos - targetPos) > 0.01f)
        {
            currentPos = Mathf.Lerp(currentPos, targetPos, Time.unscaledDeltaTime * scrollSensitivity); // ✅ เปลี่ยนเป็น unscaledDeltaTime
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, currentPos);
            yield return null;
        }
    }
}