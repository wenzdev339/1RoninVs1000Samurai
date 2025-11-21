using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// เลือก parentButton ใน EventSystem
public class ButtonSliderBarController : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Button parentButton;

    public void OnPointerDown(PointerEventData eventData)
    {
        EventSystem.current.SetSelectedGameObject(parentButton.gameObject);
    }
}
