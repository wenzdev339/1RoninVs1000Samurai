using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIInteract : MonoBehaviour
{
    public void Interact(CanvasGroup Canvas)
    {
        Canvas.interactable = true;
        Canvas.blocksRaycasts = true;
    }

    public void NoneInteract(CanvasGroup Canvas)
    {
        Canvas.interactable = false;
        Canvas.blocksRaycasts = false;
    }
}
