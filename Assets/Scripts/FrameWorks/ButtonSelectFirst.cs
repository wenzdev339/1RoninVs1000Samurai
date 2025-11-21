using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSelectFirst : MonoBehaviour
{
    public void SetFirstSelected(GameObject objectToSelect)
    {
        EventSystem.current.SetSelectedGameObject(objectToSelect);
        
        // เลือกเพื่อให้ UI Highlight ทำงาน
        Selectable selectable = objectToSelect.GetComponent<Selectable>();
        if (selectable != null)
        {
            selectable.Select();
        }
    }
}
