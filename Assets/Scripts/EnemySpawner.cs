using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private int initialSpawnCount = 300;
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private int spawnPerWave = 80;
    [SerializeField] private float minSpawnDistance = 12f;
    [SerializeField] private float maxSpawnDistance = 18f;

    [Header("Dynamic Spawn Settings")]
    [SerializeField] private int maxActiveEnemies = 8000;
    [SerializeField] private float densityCheckRadius = 12f;
    [SerializeField] private int minEnemiesInRadius = 1000;
    [SerializeField] private float dashSpawnCheckInterval = 0.1f;
    [SerializeField] private int dashSpawnAmount = 20;
    [SerializeField] private float dashSpawnOutsideCameraBuffer = 2f;

    [Header("Performance Settings")]
    [SerializeField] private int maxSimultaneousAttackers = 50;

    private static int currentAttackerCount = 0;
    private float attackCountResetInterval = 0.5f;
    private float attackCountResetTimer;

    private float spawnTimer;
    private float dashCheckTimer;
    private PlayerMovement playerMovement;
    private Camera mainCamera;
    
    private Transform enemiesParent;
    private int currentEnemyCount;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
            
        enemiesParent = new GameObject("Enemies").transform;
    }

    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }

        mainCamera = Camera.main;

        SpawnWave(initialSpawnCount);
    }

    private void Update()
    {
        if (player == null) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval && currentEnemyCount < maxActiveEnemies)
        {
            SpawnWave(spawnPerWave);
            spawnTimer = 0f;
        }

        attackCountResetTimer += Time.deltaTime;
        if (attackCountResetTimer >= attackCountResetInterval)
        {
            currentAttackerCount = 0;
            attackCountResetTimer = 0f;
        }

        if (playerMovement != null && playerMovement.IsDashing)
        {
            dashCheckTimer += Time.deltaTime;
            if (dashCheckTimer >= dashSpawnCheckInterval)
            {
                CheckAndSpawnAroundPlayer();
                dashCheckTimer = 0f;
            }
        }
    }

    private void CheckAndSpawnAroundPlayer()
    {
        if (player == null || currentEnemyCount >= maxActiveEnemies) return;

        int nearbyCount = CountNearbyEnemies();
        
        if (nearbyCount < minEnemiesInRadius)
        {
            int spawnCount = Mathf.Min(dashSpawnAmount, maxActiveEnemies - currentEnemyCount);
            for (int i = 0; i < spawnCount; i++)
            {
                SpawnEnemyOutsideCamera();
            }
        }
    }

    private int CountNearbyEnemies()
    {
        int count = 0;
        float sqrRadius = densityCheckRadius * densityCheckRadius;
        Vector2 playerPos = player.position;

        foreach (Transform enemy in enemiesParent)
        {
            if (enemy.gameObject.activeInHierarchy)
            {
                float sqrDist = ((Vector2)enemy.position - playerPos).sqrMagnitude;
                if (sqrDist <= sqrRadius)
                {
                    count++;
                }
            }
        }
        
        return count;
    }

    private void SpawnWave(int count)
    {
        int actualSpawnCount = Mathf.Min(count, maxActiveEnemies - currentEnemyCount);
        for (int i = 0; i < actualSpawnCount; i++)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (player == null) return;

        Vector2 spawnPosition = GetSpawnPosition();
        
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, enemiesParent);
        currentEnemyCount++;
    }

    private Vector2 GetSpawnPosition()
    {
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = UnityEngine.Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        return (Vector2)player.position + offset;
    }

    private void SpawnEnemyOutsideCamera()
    {
        if (player == null || mainCamera == null) return;

        Vector2 spawnPosition = GetSpawnPositionOutsideCamera();
        
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, enemiesParent);
        currentEnemyCount++;
    }

    private Vector2 GetSpawnPositionOutsideCamera()
    {
        // คำนวณขนาดกล้อง
        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        
        // เพิ่ม buffer ให้ spawn นอกกล้อง
        float spawnDistanceFromEdge = dashSpawnOutsideCameraBuffer;
        
        // สุ่มด้านที่จะ spawn (0=บน, 1=ล่าง, 2=ซ้าย, 3=ขวา)
        int side = UnityEngine.Random.Range(0, 4);
        
        Vector2 cameraPos = mainCamera.transform.position;
        Vector2 spawnPos = Vector2.zero;
        
        switch (side)
        {
            case 0: // บน
                spawnPos = new Vector2(
                    UnityEngine.Random.Range(cameraPos.x - cameraWidth/2f, cameraPos.x + cameraWidth/2f),
                    cameraPos.y + cameraHeight/2f + spawnDistanceFromEdge
                );
                break;
            case 1: // ล่าง
                spawnPos = new Vector2(
                    UnityEngine.Random.Range(cameraPos.x - cameraWidth/2f, cameraPos.x + cameraWidth/2f),
                    cameraPos.y - cameraHeight/2f - spawnDistanceFromEdge
                );
                break;
            case 2: // ซ้าย
                spawnPos = new Vector2(
                    cameraPos.x - cameraWidth/2f - spawnDistanceFromEdge,
                    UnityEngine.Random.Range(cameraPos.y - cameraHeight/2f, cameraPos.y + cameraHeight/2f)
                );
                break;
            case 3: // ขวา
                spawnPos = new Vector2(
                    cameraPos.x + cameraWidth/2f + spawnDistanceFromEdge,
                    UnityEngine.Random.Range(cameraPos.y - cameraHeight/2f, cameraPos.y + cameraHeight/2f)
                );
                break;
        }
        
        return spawnPos;
    }

    public void OnEnemyDestroyed()
    {
        currentEnemyCount--;
    }

    public static bool CanAttack()
    {
        if (Instance == null) return true;
        return currentAttackerCount < Instance.maxSimultaneousAttackers;
    }

    public static void IncrementAttackerCount()
    {
        currentAttackerCount++;
    }
}