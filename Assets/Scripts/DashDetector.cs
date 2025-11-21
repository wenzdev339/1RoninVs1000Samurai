using UnityEngine;
using System.Collections;

public class DashDetector : MonoBehaviour
{
    [Header("Launch Settings")]
    [SerializeField] private float launchForce = 35f;
    [SerializeField] private float launchDuration = 0.8f;
    [SerializeField] private float spinSpeed = 720f;
    [SerializeField] private AnimationCurve launchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool addRandomSpin = true;
    
    [Header("Visual Effects")]
    [SerializeField] private float fadeStartTime = 0.5f;
    [SerializeField] private bool scaleDownOnLaunch = true;
    [SerializeField] private float minScale = 0.3f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerMovement player = collision.GetComponent<PlayerMovement>();
            if (player != null && player.IsDashing)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"<color=lime>DashDetector: Player dashed through {transform.parent?.name ?? gameObject.name} - Launching!</color>");
                }
                
                // เริ่มการ Launch ศัตรูขึ้นฟ้า
                GameObject enemyObject = transform.parent != null ? transform.parent.gameObject : gameObject;
                StartCoroutine(LaunchAndDestroy(enemyObject, collision.transform.position));
            }
            else if (enableDebugLogs && player != null && !player.IsDashing)
            {
                Debug.Log($"<color=orange>DashDetector: Player touched but not dashing</color>");
            }
        }
    }
    
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerMovement player = collision.GetComponent<PlayerMovement>();
            if (player != null && player.IsDashing)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"<color=lime>DashDetector (Stay): Player dashing through {transform.parent?.name ?? gameObject.name} - Launching!</color>");
                }
                
                GameObject enemyObject = transform.parent != null ? transform.parent.gameObject : gameObject;
                StartCoroutine(LaunchAndDestroy(enemyObject, collision.transform.position));
            }
        }
    }

    private IEnumerator LaunchAndDestroy(GameObject enemyObject, Vector3 playerPosition)
    {
        // เพิ่มคะแนน
        GameManager.Instance?.AddScore(1);
        
        Enemy enemyScript = enemyObject.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.enabled = false;
        }
        
        // ปิด Collider เพื่อไม่ให้ชนกับอะไร
        Collider2D[] colliders = enemyObject.GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
        
        // หา Rigidbody2D
        Rigidbody2D rb = enemyObject.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f; // ไม่ใช้แรงโน้มถ่วง
            rb.drag = 0f;
            rb.angularDrag = 0f;
            rb.constraints = RigidbodyConstraints2D.None;
            
            Vector2 launchDirection = (enemyObject.transform.position - playerPosition).normalized;
            launchDirection += Vector2.up * 2f;
            launchDirection.Normalize();
            
            rb.velocity = launchDirection * launchForce;
            
            if (addRandomSpin)
            {
                float randomSpin = Random.Range(-spinSpeed, spinSpeed);
                rb.angularVelocity = randomSpin;
            }
        }
        
        // หา SpriteRenderer
        SpriteRenderer[] sprites = enemyObject.GetComponentsInChildren<SpriteRenderer>();
        Vector3 originalScale = enemyObject.transform.localScale;
        
        float elapsedTime = 0f;
        bool fadeStarted = false;
        
        // Animation loop
        while (elapsedTime < launchDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / launchDuration;
            
            // ใช้ Animation Curve สำหรับการเคลื่อนที่
            if (rb != null && launchCurve != null)
            {
                float curveValue = launchCurve.Evaluate(normalizedTime);
                
                // ลดความเร็วการลอยตาม curve
                rb.velocity = rb.velocity * (1f - Time.deltaTime * 2f);
                
                // เพิ่มแรงลอยขึ้นเล็กน้อย
                rb.velocity += Vector2.up * curveValue * Time.deltaTime * 10f;
            }

            // Fade effect
            if (!fadeStarted && elapsedTime >= fadeStartTime)
            {
                fadeStarted = true;
            }
            
            if (fadeStarted)
            {
                float fadeProgress = (elapsedTime - fadeStartTime) / (launchDuration - fadeStartTime);
                float alpha = Mathf.Lerp(1f, 0f, fadeProgress);
                
                // Apply fade to all sprites
                foreach (var sprite in sprites)
                {
                    if (sprite != null)
                    {
                        Color color = sprite.color;
                        color.a = alpha;
                        sprite.color = color;
                    }
                }
            }
            
            yield return null;
        }
        
        // แจ้ง Spawner ก่อนทำลาย
        EnemySpawner.Instance?.OnEnemyDestroyed();
        
        // ทำลาย Enemy GameObject
        Destroy(enemyObject);
        
        if (enableDebugLogs)
        {
            Debug.Log($"<color=magenta>Enemy launched and destroyed!</color>");
        }
    }
    
    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && col.isTrigger)
        {
            // แสดงขอบเขต Trigger สีเขียว
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            
            if (col is CircleCollider2D circle)
            {
                Gizmos.DrawSphere(transform.position, circle.radius * transform.lossyScale.x);
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, circle.radius * transform.lossyScale.x);
            }
            else if (col is BoxCollider2D box)
            {
                Vector3 size = new Vector3(box.size.x * transform.lossyScale.x, 
                                          box.size.y * transform.lossyScale.y, 0.1f);
                Gizmos.DrawCube(transform.position + (Vector3)box.offset, size);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, size);
            }
        }
    }
}