using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro; // ถ้าใช้ TextMeshPro

public class AttractScreenController : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip[] videoClips;
    private int currentVideoIndex = 0;

    [Header("UI Settings")]
    [SerializeField] private GameObject pressAnyButtonText;
    [SerializeField] private float textBlinkSpeed = 0.5f;

    [Header("Fade Settings")]
    [SerializeField] private GameObject fadeImage; // Image ที่จะ Fade
    [SerializeField] private float fadeDuration = 1f; // เวลาในการ Fade (วินาที)
    private Image fadeImageComponent;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu_Scene";
    [SerializeField] private float inputDelay = 1f;

    private bool canPressButton = false;
    private bool isTransitioning = false; // ป้องกันกดซ้ำ
    private CanvasGroup textCanvasGroup;

    void Start()
    {
        SetupVideoPlayer();
        SetupPressAnyButtonText();
        SetupFadeImage();
        
        PlayNextVideo();
        StartCoroutine(EnableInputAfterDelay());
    }

    void SetupVideoPlayer()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    void SetupPressAnyButtonText()
    {
        if (pressAnyButtonText != null)
        {
            textCanvasGroup = pressAnyButtonText.GetComponent<CanvasGroup>();
            if (textCanvasGroup == null)
            {
                textCanvasGroup = pressAnyButtonText.AddComponent<CanvasGroup>();
            }
            
            StartCoroutine(BlinkText());
        }
    }

    void SetupFadeImage()
    {
        if (fadeImage != null)
        {
            // ซ่อน Fade Image ตอนเริ่มต้น
            fadeImage.SetActive(false);
            
            // เก็บ Image Component
            fadeImageComponent = fadeImage.GetComponent<Image>();
            if (fadeImageComponent == null)
            {
                Debug.LogError("Fade_Image ต้องมี Image Component!");
            }
            else
            {
                // ตั้งค่า Alpha เป็น 0 (โปร่งใส)
                Color color = fadeImageComponent.color;
                color.a = 0f;
                fadeImageComponent.color = color;
            }
        }
        else
        {
            Debug.LogWarning("ไม่ได้ใส่ Fade_Image ใน Inspector!");
        }
    }

    void PlayNextVideo()
    {
        if (videoClips.Length == 0)
        {
            Debug.LogError("ไม่มี Video Clips ใน Array!");
            return;
        }

        videoPlayer.clip = videoClips[currentVideoIndex];
        videoPlayer.Play();

        currentVideoIndex = (currentVideoIndex + 1) % videoClips.Length;
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        PlayNextVideo();
    }

    IEnumerator EnableInputAfterDelay()
    {
        yield return new WaitForSeconds(inputDelay);
        canPressButton = true;
    }

    IEnumerator BlinkText()
    {
        while (true)
        {
            float t = 0;
            while (t < textBlinkSpeed)
            {
                t += Time.deltaTime;
                textCanvasGroup.alpha = Mathf.Lerp(1f, 0.2f, t / textBlinkSpeed);
                yield return null;
            }

            t = 0;
            while (t < textBlinkSpeed)
            {
                t += Time.deltaTime;
                textCanvasGroup.alpha = Mathf.Lerp(0.2f, 1f, t / textBlinkSpeed);
                yield return null;
            }
        }
    }

    void Update()
    {
        if (!canPressButton || isTransitioning) return;

        // ตรวจสอบการกดปุ่มใดๆ
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            StartCoroutine(FadeOutAndLoadScene());
        }

        // รองรับ Gamepad/Controller
        if (Input.GetJoystickNames().Length > 0)
        {
            if (Input.GetButtonDown("Submit") || Input.GetButtonDown("Jump"))
            {
                StartCoroutine(FadeOutAndLoadScene());
            }
        }
    }

    IEnumerator FadeOutAndLoadScene()
    {
        isTransitioning = true;
        
        // เปิด Fade Image
        if (fadeImage != null)
        {
            fadeImage.SetActive(true);
        }

        // Fade Out (Alpha 0 -> 1)
        float elapsedTime = 0f;
        Color color = fadeImageComponent.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            color.a = alpha;
            fadeImageComponent.color = color;
            yield return null;
        }

        // ให้แน่ใจว่า Alpha เป็น 1 (ทึบ)
        color.a = 1f;
        fadeImageComponent.color = color;

        // โหลด Scene
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
        StopAllCoroutines();
    }
}