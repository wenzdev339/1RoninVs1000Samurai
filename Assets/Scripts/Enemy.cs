using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int damage = 1;
    
    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("Sprite Variants")]
    [SerializeField] private GameObject normalSprite;
    [SerializeField] private GameObject holoFoilSprite;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackDuration = 0.5f;
    [SerializeField] private float knockbackDrag = 3f;
    [SerializeField] private float minKnockbackForce = 10f;

    private Transform player;
    private Rigidbody2D rb;
    private float attackTimer;
    private int health = 1;
    private float updateInterval = 0.2f;
    private float updateTimer;
    private Vector2 cachedDirection;
    private float sqrAttackRange;
    private bool isActive = true;
    
    private float cachedSqrDistance;
    
    private float separationRadius = 0.7f;
    private float sqrSeparationRadius;
    private float separationForce = 1.2f;
    private Vector2 separationVelocity;
    private float separationCheckInterval = 0.5f;
    private float separationCheckTimer;
    private static Collider2D[] nearbyBuffer = new Collider2D[4];
    private static ContactFilter2D enemyContactFilter;
    private static bool filterInitialized = false;
    
    [Header("Camera Culling")]
    [SerializeField] private float cullDistance = 25f;
    private float cullCheckInterval = 1f;
    private float cullCheckTimer;
    private bool isCulled = false;

    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;
    private Vector2 currentKnockbackVelocity;

    private static Transform playerTransform;
    private static int frameCounter = 0;
    private int enemyFrameOffset;

    // SPATIAL PARTITIONING
    public int spatialGroup = -1;
    private int lastSpatialGroup = -1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        
        sqrAttackRange = attackRange * attackRange;
        sqrSeparationRadius = separationRadius * separationRadius;
        
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.sleepMode = RigidbodySleepMode2D.StartAwake;
        rb.gravityScale = 0f;
        rb.drag = 0f;
        rb.angularDrag = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        if (!filterInitialized)
        {
            enemyContactFilter = new ContactFilter2D();
            enemyContactFilter.useTriggers = false;
            enemyContactFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
            filterInitialized = true;
        }

        if (gameObject.layer != LayerMask.NameToLayer("Enemy"))
        {
            gameObject.layer = LayerMask.NameToLayer("Enemy");
        }

        enemyFrameOffset = Random.Range(0, 10);
    }

    private void OnEnable()
    {
        isActive = true;
        isCulled = false;
        
        updateTimer = Random.Range(0f, updateInterval);
        separationCheckTimer = Random.Range(0f, separationCheckInterval);
        cullCheckTimer = Random.Range(0f, cullCheckInterval);
        
        separationVelocity = Vector2.zero;
        cachedSqrDistance = float.MaxValue;
        isKnockedBack = false;
        knockbackTimer = 0f;
        currentKnockbackVelocity = Vector2.zero;
        
        RandomizeSpriteVariant();
        
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
        player = playerTransform;

        // REGISTER กับ EnemyManager
        if (EnemyManager.Instance != null)
        {
            spatialGroup = EnemyManager.Instance.GetSpatialGroup(transform.position.x, transform.position.y);
            lastSpatialGroup = spatialGroup;
            EnemyManager.Instance.RegisterEnemy(this);
        }
    }

    private void OnDisable()
    {
        // UNREGISTER จาก EnemyManager
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy(this);
        }
    }
    
    private void RandomizeSpriteVariant()
    {
        int random = Random.Range(0, 4);
        
        if (random == 0)
        {
            if (normalSprite != null) normalSprite.SetActive(false);
            if (holoFoilSprite != null) 
            {
                holoFoilSprite.SetActive(true);
                SpriteRenderer holoRenderer = holoFoilSprite.GetComponent<SpriteRenderer>();
                if (holoRenderer != null)
                {
                    spriteRenderer = holoRenderer;
                }
            }
        }
        else
        {
            if (normalSprite != null) 
            {
                normalSprite.SetActive(true);
                SpriteRenderer normalRenderer = normalSprite.GetComponent<SpriteRenderer>();
                if (normalRenderer != null)
                {
                    spriteRenderer = normalRenderer;
                }
            }
            if (holoFoilSprite != null) holoFoilSprite.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isActive || player == null || isCulled) return;

        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            
            currentKnockbackVelocity = Vector2.Lerp(currentKnockbackVelocity, Vector2.zero, knockbackDrag * Time.deltaTime);
            rb.velocity = currentKnockbackVelocity;
            
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
                currentKnockbackVelocity = Vector2.zero;
                rb.drag = 0f;
            }
            return;
        }

        if (Time.frameCount % 10 == enemyFrameOffset)
        {
            cullCheckTimer += Time.deltaTime * 10f;
            if (cullCheckTimer >= cullCheckInterval)
            {
                PerformCullingCheck();
                cullCheckTimer = 0f;
            }
        }

        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            cachedSqrDistance = (player.position - transform.position).sqrMagnitude;
            UpdateDirection();
            UpdateSpatialGroup(); // อัพเดท Spatial Group
            updateTimer = 0f;
        }

        if (Time.frameCount % 5 == enemyFrameOffset % 5)
        {
            separationCheckTimer += Time.deltaTime * 5f;
            if (separationCheckTimer >= separationCheckInterval)
            {
                CalculateSeparation();
                separationCheckTimer = 0f;
            }
        }
        
        if (cachedSqrDistance <= sqrAttackRange)
        {
            rb.velocity = separationVelocity * separationForce;
            
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackCooldown)
            {
                AttackPlayer();
                attackTimer = 0f;
            }
        }
        else
        {
            Vector2 moveDir = cachedDirection * moveSpeed;
            Vector2 separateDir = separationVelocity * separationForce;
            rb.velocity = moveDir + separateDir;
        }
    }

    /// <summary>
    /// อัพเดท Spatial Group เมื่อเคลื่อนที่ (เหมือนในโค้ดตัวอย่าง)
    /// </summary>
    private void UpdateSpatialGroup()
    {
        if (EnemyManager.Instance == null) return;

        int newSpatialGroup = EnemyManager.Instance.GetSpatialGroup(transform.position.x, transform.position.y);
        
        if (newSpatialGroup != spatialGroup)
        {
            // ลบจาก Group เก่า
            EnemyManager.Instance.RemoveFromSpatialGroup(spatialGroup, this);
            
            // อัพเดท Group ปัจจุบัน
            spatialGroup = newSpatialGroup;
            
            // เพิ่มเข้า Group ใหม่
            EnemyManager.Instance.AddToSpatialGroup(spatialGroup, this);
        }
    }

    private void PerformCullingCheck()
    {
        if (player == null) return;
        
        float sqrCullDistance = cullDistance * cullDistance;
        float sqrDistanceToPlayer = (player.position - transform.position).sqrMagnitude;
        
        if (sqrDistanceToPlayer > sqrCullDistance && !isCulled)
        {
            isCulled = true;
            rb.simulated = false;
            spriteRenderer.enabled = false;
            
            if (normalSprite != null) normalSprite.SetActive(false);
            if (holoFoilSprite != null) holoFoilSprite.SetActive(false);
        }
        else if (sqrDistanceToPlayer <= sqrCullDistance && isCulled)
        {
            isCulled = false;
            rb.simulated = true;
            spriteRenderer.enabled = true;
            
            RandomizeSpriteVariant();
        }
    }

    private void UpdateDirection()
    {
        cachedDirection = (player.position - transform.position).normalized;
        
        if (Mathf.Abs(cachedDirection.x) > 0.2f)
        {
            spriteRenderer.flipX = cachedDirection.x < 0;
        }
    }

    private void CalculateSeparation()
    {
        int count = Physics2D.OverlapCircle(transform.position, separationRadius, enemyContactFilter, nearbyBuffer);
        
        if (count == 0)
        {
            separationVelocity = Vector2.zero;
            return;
        }
        
        Vector2 separationSum = Vector2.zero;
        int separationCount = 0;

        int maxChecks = Mathf.Min(count, 3);
        for (int i = 0; i < maxChecks; i++)
        {
            if (nearbyBuffer[i] != null && nearbyBuffer[i].gameObject != gameObject)
            {
                Vector2 diff = (Vector2)transform.position - (Vector2)nearbyBuffer[i].transform.position;
                float sqrDist = diff.sqrMagnitude;
                
                if (sqrDist > 0.01f && sqrDist < sqrSeparationRadius)
                {
                    separationSum += diff.normalized;
                    separationCount++;
                }
            }
        }

        separationVelocity = separationCount > 0 ? separationSum / separationCount : Vector2.zero;
    }

    private void AttackPlayer()
    {
        if (!EnemySpawner.CanAttack()) return;
        
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.TakeDamage(damage, cachedDirection);
            EnemySpawner.IncrementAttackerCount();
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    public void ApplyKnockback(Vector2 force)
    {
        if (!isActive || rb == null || isCulled) return;
        
        float forceMagnitude = force.magnitude;
        if (forceMagnitude < minKnockbackForce)
        {
            force = force.normalized * minKnockbackForce;
        }
        
        if (rb.bodyType == RigidbodyType2D.Kinematic)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        
        isKnockedBack = true;
        knockbackTimer = knockbackDuration;
        currentKnockbackVelocity = force;
        rb.velocity = currentKnockbackVelocity;
    }

    private void Die()
    {
        isActive = false;
        rb.velocity = Vector2.zero;
        
        EnemySpawner.Instance?.OnEnemyDestroyed();
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        if (isKnockedBack)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + currentKnockbackVelocity.normalized * 2f);
        }
    }
}