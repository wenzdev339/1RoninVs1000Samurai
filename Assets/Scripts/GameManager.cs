using UnityEngine;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI SumscoreText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Countdown System")]
    [SerializeField] private GameObject countdownCanvas;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float countdownTime = 3f;

    [Header("HP UI System")]
    [SerializeField] private GameObject hpPrefab;
    [SerializeField] private Transform hpContainer;
    [SerializeField] private int maxHealth = 5;

    [Header("Player Reference")]
    [SerializeField] private PlayerMovement player;

    [Header("Game Settings")]
    [SerializeField] private float gameTime = 20f;

    private int score = 0;
    private float currentTime;
    private bool isGameOver = false;
    private bool isGameStarted = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Show countdown canvas at start
        if (countdownCanvas != null)
            countdownCanvas.SetActive(true);
    }

    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerMovement>();

        // Disable player movement during countdown
        if (player != null)
            player.enabled = false;

        currentTime = gameTime;
        InitializeHealthUI();
        UpdateScoreUI();
        UpdateTimerUI();

        // Start countdown coroutine
        StartCoroutine(CountdownCoroutine());
    }

    private IEnumerator CountdownCoroutine()
    {
        float countdown = countdownTime;

        while (countdown > 0)
        {
            // Update countdown text
            if (countdownText != null)
            {
                countdownText.text = Mathf.Ceil(countdown).ToString();
            }

            yield return null;
            countdown -= Time.deltaTime;
        }

        // Show "GO!" or "0"
        if (countdownText != null)
        {
            countdownText.text = "0";
        }

        yield return new WaitForSeconds(0.2f);

        // Hide countdown canvas
        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);

        // Enable player movement
        if (player != null)
            player.enabled = true;

        isGameStarted = true;
    }

    private void Update()
    {
        if (!isGameOver && isGameStarted)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerUI();

            if (currentTime <= 0)
            {
                currentTime = 0;
                GameOver();
            }
        }
    }

    private void InitializeHealthUI()
    {
        if (hpPrefab == null || hpContainer == null)
        {
            Debug.LogError("HP Prefab or HP Container is not assigned!");
            return;
        }

        // Clear existing HP icons
        foreach (Transform child in hpContainer)
        {
            Destroy(child.gameObject);
        }

        // Create HP icons
        for (int i = 0; i < maxHealth; i++)
        {
            Instantiate(hpPrefab, hpContainer);
        }
    }

    public void RemoveHealth()
    {
        if (hpContainer == null || hpContainer.childCount <= 0)
            return;

        // Destroy the last HP icon
        Transform lastHP = hpContainer.GetChild(hpContainer.childCount - 1);
        Destroy(lastHP.gameObject);

        // Check if no HP left
        if (hpContainer.childCount <= 0)
        {
            GameOver();
        }
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateScoreUI();
    }

    public void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "" + score;
            SumscoreText.text = "" + score;
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(currentTime);
            timerText.text = "" + seconds;
        }
    }

    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public int GetRemainingHP()
    {
        return hpContainer != null ? hpContainer.childCount : 0;
    }
}