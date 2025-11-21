using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class AnimationSceneLoader : MonoBehaviour
{
    // อ้างอิง Animator
    [SerializeField] private Animator animator;
    // ชื่อฉากที่ต้องการโหลด
    [SerializeField] private string sceneName;
    // อ้างอิง Image สำหรับทำ Fade
    [SerializeField] private Image fadeImage; 
    // พื้นหลัง Fade
    [SerializeField] private GameObject fadeBG;
    // ระยะเวลาการ Fade
    [SerializeField] private float fadeDuration = 1f;
    // ตัวแปรควบคุมการ Fade
    [SerializeField] private bool isFading = false;

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    void Update()
    {
        // ตรวจสอบว่า Animation กำลังเล่นอยู่หรือไม่
        if (animator != null && IsAnimationFinished() && !isFading)
        {
            StartCoroutine(FadeOutAndLoadScene());
        }
    }

    private bool IsAnimationFinished()
    {
        // ตรวจสอบว่าการเล่น Animation ได้เสร็จสมบูรณ์
        return animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1 &&
               !animator.IsInTransition(0);
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        isFading = true;
        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene(sceneName);
    }

    public IEnumerator FadeOut()
    {
        fadeBG.SetActive(true);
        Color color = fadeImage.color;
        float elapsedTime = 0;

        while (elapsedTime < fadeDuration)
        {
            color.a = Mathf.Lerp(0, 1, elapsedTime / fadeDuration);
            fadeImage.color = color;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        color.a = 1;
    }
}