using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeed = 250f;
    [SerializeField] private float dashDuration = 0.15f;

    [Header("Combat Settings")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float invincibilityDuration = 0.5f;
    [SerializeField] private AudioSource dashAudioSource;

    [Header("Dash Settings")]
    [SerializeField] private float dashPathRadius = 3f;
    [SerializeField] private float dashEndRadius = 4f;

    [Header("Launch Settings")]
    [SerializeField] private float launchForce = 35f;
    [SerializeField] private float launchUpwardMultiplier = 2f;
    [SerializeField] private float spinSpeed = 720f;
    [SerializeField] private float launchDuration = 0.8f;
    [SerializeField] private float fadeStartTime = 0.5f;
    [SerializeField] private float minScale = 0.3f;

    [Header("Spatial Dash Settings")]
    [SerializeField] private float dashCheckInterval = 0.02f;
    
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer katanaSprite;
    [SerializeField] private CameraShake cameraShake;

    private PlayerController playerController;
    private Camera mainCamera;
    
    private Vector2 moveInput;
    private Vector2 mousePosition;
    private Vector2 dashDirection;
    
    private bool isDashing = false;
    private float dashTimer;
    private float dashCheckTimer = 0f;
    
    private int currentHealth;
    private bool isInvincible = false;
    private float invincibilityTimer;

    private List<Vector2> dashPathPoints = new List<Vector2>();
    private HashSet<Enemy> launchedEnemies = new HashSet<Enemy>();

    public bool IsDashing => isDashing;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        playerController = new PlayerController();
        mainCamera = Camera.main;
        
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (katanaSprite == null) katanaSprite = transform.Find("Katana")?.GetComponent<SpriteRenderer>();

        currentHealth = maxHealth;
        
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.gravityScale = 0f;
        rb.drag = 0f;
    }

    private void OnEnable()
    {
        playerController.Enable();
        
        playerController.Player.Move.performed += OnMove;
        playerController.Player.Move.canceled += OnMove;
        
        playerController.Player.MousePosition.performed += OnMousePosition;
        
        playerController.Player.Dash.performed += OnDash;
    }

    private void OnDisable()
    {
        playerController.Disable();
        
        playerController.Player.Move.performed -= OnMove;
        playerController.Player.Move.canceled -= OnMove;
        
        playerController.Player.MousePosition.performed -= OnMousePosition;
        
        playerController.Player.Dash.performed -= OnDash;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMousePosition(InputAction.CallbackContext context)
    {
        mousePosition = context.ReadValue<Vector2>();
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (!isDashing && currentHealth > 0)
        {
            StartDash();
        }
    }

    private void Update()
    {
        bool isMoving = moveInput.magnitude > 0.1f && !isDashing;
        
        if (animator != null)
        {
            animator.SetBool("IsMove", isMoving);
        }

        if (isDashing)
        {
            FlipSpriteTowardsDash();
            
            dashTimer -= Time.deltaTime;
            dashCheckTimer += Time.deltaTime;
            
            if (dashCheckTimer >= dashCheckInterval)
            {
                CheckDashCollisionSpatial();
                dashCheckTimer = 0f;
            }
            
            if (dashTimer <= 0)
            {
                EndDash();
            }
        }
        else
        {
            FlipSpriteTowardsMouse();
        }

        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0)
            {
                isInvincible = false;
                SetSpriteAlpha(1f);
            }
            else
            {
                float alpha = Mathf.PingPong(Time.time * 10f, 1f);
                SetSpriteAlpha(alpha);
            }
        }
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            rb.velocity = dashDirection * dashSpeed;
        }
        else
        {
            rb.velocity = moveInput * moveSpeed;
        }
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCheckTimer = 0f;
        dashPathPoints.Clear();
        launchedEnemies.Clear();

        Vector3 worldMousePosition = mainCamera.ScreenToWorldPoint(mousePosition);
        worldMousePosition.z = 0;
        
        dashDirection = (worldMousePosition - transform.position).normalized;
        FlipSpriteTowardsDash();

        if (dashAudioSource != null)
        {
            dashAudioSource.Play();
        }

        if (cameraShake != null)
        {
            cameraShake.ShakeCamera();
        }
    }

    private void CheckDashCollisionSpatial()
    {
        dashPathPoints.Add(transform.position);

        // เช็คทั้ง Dash Path และ Dash End Radius พร้อมกัน
        float maxRadius = Mathf.Max(dashPathRadius, dashEndRadius);
        List<Enemy> nearbyEnemies = GetNearbyEnemiesSpatial(transform.position, maxRadius);
        
        foreach (Enemy enemy in nearbyEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                continue;

            // ข้ามศัตรูที่ Launch ไปแล้ว
            if (launchedEnemies.Contains(enemy))
                continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            
            // ตรวจสอบว่าอยู่ในระยะใดระยะหนึ่ง
            if (distance <= dashPathRadius || distance <= dashEndRadius)
            {
                LaunchEnemy(enemy, transform.position);
                launchedEnemies.Add(enemy);
            }
        }
    }

    private void EndDash()
    {
        isDashing = false;
        rb.velocity = Vector2.zero;

        // เช็คครั้งสุดท้ายก่อนจบ Dash เพื่อไม่ให้มีศัตรูหลุด
        CheckDashCollisionSpatial();

        if (cameraShake != null)
        {
            cameraShake.ShakeCamera();
        }

        dashPathPoints.Clear();
        launchedEnemies.Clear();
    }

    private void LaunchEnemy(Enemy enemy, Vector2 playerPosition)
    {
        if (enemy == null) return;

        // เพิ่มคะแนนทันที
        GameManager.Instance?.AddScore(1);

        // แจ้ง Spawner
        EnemySpawner.Instance?.OnEnemyDestroyed();

        // ปิด Enemy script
        enemy.enabled = false;

        // ปิด Colliders ทั้งหมด
        Collider2D[] colliders = enemy.GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // Setup Rigidbody
        Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
        if (enemyRb != null)
        {
            enemyRb.bodyType = RigidbodyType2D.Dynamic;
            enemyRb.gravityScale = 0f;
            enemyRb.drag = 0f;
            enemyRb.angularDrag = 0f;
            enemyRb.constraints = RigidbodyConstraints2D.None;
            enemyRb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            enemyRb.interpolation = RigidbodyInterpolation2D.None;

            // คำนวณทิศทางการกระเด็น
            Vector2 launchDirection = ((Vector2)enemy.transform.position - playerPosition).normalized;
            launchDirection += Vector2.up * launchUpwardMultiplier;
            launchDirection.Normalize();

            // ยิงขึ้นฟ้า
            enemyRb.velocity = launchDirection * launchForce;
            enemyRb.angularVelocity = Random.Range(-spinSpeed, spinSpeed);
        }

        // เริ่ม Fade และทำลาย
        StartCoroutine(FadeAndDestroy(enemy.gameObject));
    }

    private System.Collections.IEnumerator FadeAndDestroy(GameObject enemyObject)
    {
        if (enemyObject == null) yield break;

        SpriteRenderer[] sprites = enemyObject.GetComponentsInChildren<SpriteRenderer>();
        Vector3 originalScale = enemyObject.transform.localScale;
        Rigidbody2D enemyRb = enemyObject.GetComponent<Rigidbody2D>();

        float elapsedTime = 0f;
        bool fadeStarted = false;

        while (elapsedTime < launchDuration && enemyObject != null)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / launchDuration;

            // ลดความเร็วค่อยๆ
            if (enemyRb != null)
            {
                enemyRb.velocity *= 0.96f;
                enemyRb.velocity += Vector2.up * (1f - normalizedTime) * Time.deltaTime * 8f;
            }

            // เริ่ม Fade
            if (!fadeStarted && elapsedTime >= fadeStartTime)
            {
                fadeStarted = true;
            }

            if (fadeStarted)
            {
                float fadeProgress = (elapsedTime - fadeStartTime) / (launchDuration - fadeStartTime);
                float alpha = Mathf.Lerp(1f, 0f, fadeProgress);
                float scale = Mathf.Lerp(1f, minScale, fadeProgress);

                // Fade sprites
                foreach (var sprite in sprites)
                {
                    if (sprite != null)
                    {
                        Color color = sprite.color;
                        color.a = alpha;
                        sprite.color = color;
                    }
                }

                // Scale down
                if (enemyObject != null)
                {
                    enemyObject.transform.localScale = originalScale * scale;
                }
            }

            yield return null;
        }

        // ทำลายศัตรู
        if (enemyObject != null)
        {
            Destroy(enemyObject);
        }
    }

    private List<Enemy> GetNearbyEnemiesSpatial(Vector2 position, float radius)
    {
        if (EnemyManager.Instance != null)
        {
            return EnemyManager.Instance.GetNearbyEnemies(position, radius);
        }
        
        return new List<Enemy>();
    }

    private void FlipSpriteTowardsMouse()
    {
        Vector3 worldMousePosition = mainCamera.ScreenToWorldPoint(mousePosition);
        worldMousePosition.z = 0;

        bool shouldFlip = worldMousePosition.x < transform.position.x;
        
        if (spriteRenderer != null)
            spriteRenderer.flipX = shouldFlip;
        
        if (katanaSprite != null)
            katanaSprite.flipX = shouldFlip;
    }

    private void FlipSpriteTowardsDash()
    {
        bool shouldFlip = dashDirection.x < 0;
        
        if (spriteRenderer != null)
            spriteRenderer.flipX = shouldFlip;
        
        if (katanaSprite != null)
            katanaSprite.flipX = shouldFlip;
    }

    private void SetSpriteAlpha(float alpha)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1, 1, 1, alpha);
        
        if (katanaSprite != null)
            katanaSprite.color = new Color(1, 1, 1, alpha);
    }

    public void TakeDamage(int damage, Vector2 knockbackDirection)
    {
        if (isInvincible || isDashing) return;

        currentHealth -= damage;
        
        // ลด HP UI (Destroy HP icons)
        for (int i = 0; i < damage; i++)
        {
            GameManager.Instance?.RemoveHealth();
        }
        
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

        isInvincible = true;
        invincibilityTimer = invincibilityDuration;
    }

    private void Die()
    {
        GameManager.Instance?.GameOver();
    }

    private void OnDrawGizmosSelected()
    {
        if (isDashing)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, dashPathRadius);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, dashPathRadius);
            
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, dashEndRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, dashEndRadius);
        }
        else
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, dashPathRadius);
            
            Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, dashEndRadius);
        }
    }
}