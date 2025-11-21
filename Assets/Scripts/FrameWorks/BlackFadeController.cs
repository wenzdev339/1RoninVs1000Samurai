using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BlackFadeController : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private GameObject blackFadeObject;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;
    
    [Header("Initial Behavior")]
    [SerializeField] private bool fadeInOnStart = true;

    private void Awake()
    {
        // ตรวจสอบว่าต้องการ Fade In หรือไม่
        if (fadeInOnStart)
        {
            StartCoroutine(FadeInSequence());
        }
        else
        {
            // ถ้าไม่ Fade In ให้ปิด Fade Object ทิ้ง
            blackFadeObject.SetActive(false);
        }
    }

    private IEnumerator FadeInSequence()
    {
        // 1. Set Active True
        blackFadeObject.SetActive(true);

        // 2. Set Opacity เป็น 100% (Alpha = 1)
        if (fadeImage == null)
        {
            fadeImage = blackFadeObject.GetComponent<Image>();
        }

        Color color = fadeImage.color;
        color.a = 1f; // 100% Opacity
        fadeImage.color = color;

        // 3. Fade จาก 100% -> 0%
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            
            color.a = alpha;
            fadeImage.color = color;
            
            yield return null;
        }

        // 4. ตรวจสอบให้แน่ใจว่า Alpha = 0
        color.a = 0f;
        fadeImage.color = color;

        // 5. Set Active False
        blackFadeObject.SetActive(false);
    }

    // ฟังก์ชันเสริม: เรียก Fade In ด้วยตัวเอง
    public void FadeIn(float duration = -1f)
    {
        float actualDuration = duration > 0 ? duration : fadeDuration;
        StartCoroutine(FadeInSequence(actualDuration));
    }

    private IEnumerator FadeInSequence(float duration)
    {
        blackFadeObject.SetActive(true);

        if (fadeImage == null)
        {
            fadeImage = blackFadeObject.GetComponent<Image>();
        }

        Color color = fadeImage.color;
        color.a = 1f;
        fadeImage.color = color;

        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            
            color.a = alpha;
            fadeImage.color = color;
            
            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;
        blackFadeObject.SetActive(false);
    }

    // ฟังก์ชันเสริม: เรียก Fade Out
    public void FadeOut(float duration = -1f)
    {
        float actualDuration = duration > 0 ? duration : fadeDuration;
        StartCoroutine(FadeOutSequence(actualDuration));
    }

    private IEnumerator FadeOutSequence(float duration)
    {
        blackFadeObject.SetActive(true);

        if (fadeImage == null)
        {
            fadeImage = blackFadeObject.GetComponent<Image>();
        }

        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;

        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            
            color.a = alpha;
            fadeImage.color = color;
            
            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
    }
}