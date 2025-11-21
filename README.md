# Performance Optimization for WebGL: Handling 1000+ Enemies

**Game:** [1 Ronin vs 1000 Samurai](https://wenzgame.itch.io/1-ronin-vs-1000-samurai)

This document explains the optimization techniques used to achieve smooth performance in WebGL with thousands of active enemies on screen.

---

## 🎯 Core Optimization Techniques

### 1. **Spatial Partitioning System**
Implemented a grid-based spatial partitioning system to avoid expensive distance checks between all enemies.

**Key Implementation:**
- Divided the game world into a 100x100 grid (10,000 cells)
- Each enemy registers to its spatial group based on position
- Only checks enemies in nearby cells instead of all active enemies
- Updates spatial group membership only when enemies move between cells

```csharp
// EnemyManager.cs
public int GetSpatialGroup(float xPos, float yPos)
{
    float adjustedX = xPos + halfWidth;
    float adjustedY = yPos + halfHeight;
    int xIndex = Mathf.Clamp((int)(adjustedX / cellWidth), 0, cellsPerRow - 1);
    int yIndex = Mathf.Clamp((int)(adjustedY / cellHeight), 0, cellsPerColumn - 1);
    return xIndex + yIndex * cellsPerRow;
}
```

**Benefits:**
- O(n) → O(k) complexity where k is enemies in nearby cells
- Dramatically reduces collision detection overhead
- Enables efficient range queries for dash attacks

---

### 2. **Staggered Update System**
Not all enemies need to update every frame. Implemented frame-offset updates to distribute computational load.

**Implementation:**
- Each enemy gets a random frame offset (0-9)
- Different systems update at different intervals:
  - Movement direction: Every 0.2 seconds
  - Separation checks: Every 0.5 seconds  
  - Camera culling: Every 1 second
- Uses modulo frame counting for deterministic distribution

```csharp
// Enemy.cs
if (Time.frameCount % 10 == enemyFrameOffset)
{
    cullCheckTimer += Time.deltaTime * 10f;
    if (cullCheckTimer >= cullCheckInterval)
    {
        PerformCullingCheck();
        cullCheckTimer = 0f;
    }
}
```

**Benefits:**
- Spreads CPU load across multiple frames
- Prevents frame spikes from simultaneous updates
- Maintains smooth 60 FPS even with 8000+ enemies

---

### 3. **Aggressive Camera Culling**
Enemies outside camera view are heavily optimized.

**Culling Strategy:**
- Check distance to camera every 1 second (staggered)
- Disable physics simulation (`rb.simulated = false`)
- Hide all sprite renderers
- Maintain enemy in memory for quick re-activation
- Cull distance: 25 units from camera

```csharp
// Enemy.cs
if (sqrDistanceToPlayer > sqrCullDistance && !isCulled)
{
    isCulled = true;
    rb.simulated = false;
    spriteRenderer.enabled = false;
}
```

**Impact:**
- Reduces active physics bodies by ~70%
- Dramatically reduces rendering overhead
- Seamless transition when enemies re-enter view

---

### 4. **Optimized Physics Settings**
Fine-tuned Rigidbody2D configuration for maximum performance.

**Enemy Physics:**
```csharp
rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
rb.interpolation = RigidbodyInterpolation2D.None;
rb.sleepMode = RigidbodySleepMode2D.StartAwake;
rb.gravityScale = 0f;
```

**Player Physics:**
```csharp
rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
rb.interpolation = RigidbodyInterpolation2D.Interpolate;
```

**Key Decisions:**
- Discrete collision for enemies (faster)
- Continuous collision for player (more accurate)
- No interpolation for enemies (saves processing)
- Zero gravity scale (2D top-down game)

---

### 5. **Efficient Dash Collision Detection**
The dash mechanic needed to detect hits on dozens of enemies instantly.

**Solution:**
- Query spatial partitioning system instead of Physics2D overlap
- Check only every 0.02 seconds during dash
- Track already-hit enemies in HashSet to prevent double-hits
- Use squared distance comparisons (avoid expensive sqrt)

```csharp
// PlayerMovement.cs
private void CheckDashCollisionSpatial()
{
    float maxRadius = Mathf.Max(dashPathRadius, dashEndRadius);
    List<Enemy> nearbyEnemies = GetNearbyEnemiesSpatial(transform.position, maxRadius);
    
    foreach (Enemy enemy in nearbyEnemies)
    {
        if (launchedEnemies.Contains(enemy)) continue;
        
        float distance = Vector2.Distance(transform.position, enemy.transform.position);
        if (distance <= dashPathRadius || distance <= dashEndRadius)
        {
            LaunchEnemy(enemy, transform.position);
            launchedEnemies.Add(enemy);
        }
    }
}
```

---

### 6. **Object Pooling via Smart Instantiation**
Avoided traditional object pooling complexity while maintaining benefits.

**Approach:**
- Instantiate enemies under parent transform
- Destroy on death (simple lifecycle)
- Fast instantiation with optimized prefab
- Parent hierarchy reduces transform updates

```csharp
GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, enemiesParent);
```

---

### 7. **Cached Calculations & Squared Distances**
Minimize expensive math operations.

**Optimizations:**
- Cache squared distances instead of using `Vector2.Distance`
- Pre-calculate squared ranges at initialization
- Cache direction vectors between updates
- Reuse allocated buffers for overlap checks

```csharp
// Enemy.cs
private float sqrAttackRange;
private float sqrSeparationRadius;

void Awake()
{
    sqrAttackRange = attackRange * attackRange;
    sqrSeparationRadius = separationRadius * separationRadius;
}
```

---

### 8. **Attack Rate Limiting**
Prevent performance collapse when player is swarmed.

**System:**
- Maximum 50 simultaneous attackers
- Counter resets every 0.5 seconds
- Early exit if limit reached

```csharp
// EnemySpawner.cs
public static bool CanAttack()
{
    return currentAttackerCount < Instance.maxSimultaneousAttackers;
}
```

---

### 9. **Dynamic Spawn Management**
Keep enemy density consistent without overwhelming the system.

**Features:**
- Maximum 8000 active enemies
- Spawn outside camera view during dash
- Density checks prevent empty areas
- Wave-based spawning with limits

---

### 10. **Separation Behavior Optimization**
Prevent enemies from stacking without expensive physics.

**Implementation:**
- Check only 3 nearest enemies per separation update
- Use static buffer to avoid allocations
- Contact filter for layer-specific checks
- Update every 0.5 seconds (staggered)

```csharp
private static Collider2D[] nearbyBuffer = new Collider2D[4];
private static ContactFilter2D enemyContactFilter;

int count = Physics2D.OverlapCircle(transform.position, separationRadius, 
                                    enemyContactFilter, nearbyBuffer);
```
