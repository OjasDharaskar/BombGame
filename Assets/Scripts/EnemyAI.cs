using System.Collections;
using UnityEngine;

public enum EnemyType
{
    BaldPirate,
    BigGuy,
    Cucumber,
    Captain,
    Whale
}

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
public class EnemyAI : MonoBehaviour, IDamageable
{
    [Header("Enemy Configuration")]
    [SerializeField] private EnemyType enemyType = EnemyType.BaldPirate;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask bombLayer;
    [SerializeField] private LayerMask playerLayer;

    [Header("Combat & Detection")]
    [SerializeField] private float detectionRange = 4.5f;
    [SerializeField] private float actionRange = 1.3f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int maxHealth = 1;

    [Header("Fall Damage (Whale)")]
    [SerializeField] private float lethalFallSpeed = -6.5f;
    [SerializeField] private float mapFallBoundaryY = -7.2f;

    // Components
    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;

    // State
    private int currentHealth;
    private bool isDead;
    private bool isGrounded;
    private bool movingRight = true;
    private Vector3 baseScale;
    private float nextActionTime;
    private float previousFallSpeed;
    private bool wasGrounded;
    private bool isScared;
    private float calmTimer;
    private bool isHandlingBomb;
    private float lastTurnTime;
    private const float MIN_TURN_COOLDOWN = 0.35f;

    // Cached references
    private Transform playerTransform;

    public EnemyType Type => enemyType;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    public static event System.Action<EnemyAI> OnAnyEnemyDied;

    public static int GetAliveEnemyCount()
    {
        var enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude);
        int count = 0;
        foreach (var enemy in enemies)
        {
            if (enemy != null && !enemy.isDead && enemy.gameObject.activeInHierarchy)
            {
                count++;
            }
        }
        return count;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;

        ConfigureEnemyDefaults();

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene.Contains("4") || currentScene.Equals("Level4", System.StringComparison.OrdinalIgnoreCase))
        {
            mapFallBoundaryY = -24.0f;
        }

        groundLayer |= LayerMask.GetMask("Default", "Ground");
        if (bombLayer.value == 0) bombLayer = LayerMask.GetMask("Bomb", "Default");
        if (playerLayer.value == 0) playerLayer = LayerMask.GetMask("Player", "Default");
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private void ConfigureEnemyDefaults()
    {
        switch (enemyType)
        {
            case EnemyType.BaldPirate:
                maxHealth = 1;
                moveSpeed = 2.2f;
                actionRange = 1.4f;
                break;
            case EnemyType.BigGuy:
                maxHealth = 3; // Requires at least 3 bombs
                moveSpeed = 2.0f;
                actionRange = 1.8f;
                break;
            case EnemyType.Cucumber:
                maxHealth = 2; // Requires 2 bombs
                moveSpeed = 2.5f;
                actionRange = 1.2f;
                break;
            case EnemyType.Captain:
                maxHealth = 1; // 1 bomb to kill
                moveSpeed = 2.2f;
                runSpeed = 5.2f; // Fearful fast run
                detectionRange = 5.0f;
                break;
            case EnemyType.Whale:
                maxHealth = 999; // Immune to direct bomb explosion, dies with fall damage
                moveSpeed = 1.8f;
                actionRange = 1.6f;
                break;
        }
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (isDead) return;

        CheckWindowAndMapFall();
        if (isDead) return;

        CheckGroundAndFallDamage();

        if (isHandlingBomb) return;

        switch (enemyType)
        {
            case EnemyType.BaldPirate:
                UpdateBaldPirate();
                break;
            case EnemyType.BigGuy:
                UpdateBigGuy();
                break;
            case EnemyType.Cucumber:
                UpdateCucumber();
                break;
            case EnemyType.Captain:
                UpdateCaptain();
                break;
            case EnemyType.Whale:
                UpdateWhale();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (isDead || isHandlingBomb) return;

        previousFallSpeed = rb.linearVelocity.y;

        float currentSpeed = (enemyType == EnemyType.Captain && isScared) ? runSpeed : moveSpeed;
        rb.linearVelocity = new Vector2((movingRight ? 1f : -1f) * currentSpeed, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.SetBool("IsRunning", Mathf.Abs(rb.linearVelocity.x) > 0.1f);
        }

        // Ledge & Wall checking for patrol
        CheckLedgeAndWall();
    }

    private void CheckWindowAndMapFall()
    {
        if (isDead) return;

        bool fallen = false;

        // 1. Map window bottom boundary check
        if (transform.position.y < mapFallBoundaryY)
        {
            fallen = true;
        }

        // 2. Camera window boundary check
        if (!fallen && Camera.main != null)
        {
            Camera cam = Camera.main;
            Vector3 vp = cam.WorldToViewportPoint(transform.position);

            // If enemy falls below the bottom edge of the camera view window
            if (vp.y < -0.05f)
            {
                fallen = true;
            }
            // If enemy falls beyond horizontal sides of camera view
            else if ((vp.x < -0.3f || vp.x > 1.3f) && transform.position.y < cam.transform.position.y - 1.5f)
            {
                fallen = true;
            }
        }

        if (fallen)
        {
            Die();
        }
    }

    private void CheckGroundAndFallDamage()
    {
        Vector2 checkPos = new Vector2(col.bounds.center.x, col.bounds.min.y);
        Vector2 checkSize = new Vector2(col.bounds.size.x * 0.7f, 0.1f);
        Collider2D hit = Physics2D.OverlapBox(checkPos, checkSize, 0f, groundLayer);
        isGrounded = hit != null && hit.gameObject != gameObject;

        // Whale fall damage check: landing after falling at high speed
        if (!wasGrounded && isGrounded && enemyType == EnemyType.Whale)
        {
            if (previousFallSpeed <= lethalFallSpeed)
            {
                DieFromFallDamage();
            }
        }

        wasGrounded = isGrounded;
    }

    private void CheckLedgeAndWall()
    {
        // Don't turn around if airborne
        if (!isGrounded || col == null) return;

        // Wall check ahead
        float dir = movingRight ? 1f : -1f;
        Vector2 wallOrigin = new Vector2(col.bounds.center.x, col.bounds.center.y);
        RaycastHit2D wallHit = Physics2D.Raycast(wallOrigin, Vector2.right * dir, col.bounds.extents.x + 0.2f, groundLayer);

        // Ledge check ahead (bottom forward ray downward)
        Vector2 ledgeOrigin = new Vector2(col.bounds.center.x + dir * (col.bounds.extents.x + 0.15f), col.bounds.min.y + 0.1f);
        RaycastHit2D ledgeHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, 0.5f, groundLayer);

        if (wallHit.collider != null || ledgeHit.collider == null)
        {
            // If facing the player across a ledge/wall, stand at the edge and idle instead of rapid flipping
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= detectionRange)
            {
                bool playerIsAhead = (playerTransform.position.x > transform.position.x) == movingRight;
                if (playerIsAhead)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    if (animator != null) animator.SetBool("IsRunning", false);
                    return;
                }
            }

            if (Time.time >= lastTurnTime + MIN_TURN_COOLDOWN)
            {
                FlipDirection();
            }
        }
    }

    private void FlipDirection()
    {
        lastTurnTime = Time.time;
        movingRight = !movingRight;
        transform.localScale = new Vector3((movingRight ? 1f : -1f) * Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
    }

    private void FaceTarget(Vector3 targetPos)
    {
        bool targetIsRight = targetPos.x > transform.position.x;
        if (targetIsRight != movingRight)
        {
            // If turning would face a wall or drop-off where movement is blocked anyway, do not turn
            if (!CanSafelyWalk(targetIsRight))
            {
                return;
            }

            if (Time.time >= lastTurnTime + MIN_TURN_COOLDOWN)
            {
                FlipDirection();
            }
        }
    }

    private bool CanSafelyWalk(bool faceRight)
    {
        if (!isGrounded || col == null) return true;
        float dir = faceRight ? 1f : -1f;

        Vector2 wallOrigin = new Vector2(col.bounds.center.x, col.bounds.center.y);
        RaycastHit2D wallHit = Physics2D.Raycast(wallOrigin, Vector2.right * dir, col.bounds.extents.x + 0.2f, groundLayer);
        if (wallHit.collider != null) return false;

        Vector2 ledgeOrigin = new Vector2(col.bounds.center.x + dir * (col.bounds.extents.x + 0.15f), col.bounds.min.y + 0.1f);
        RaycastHit2D ledgeHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, 0.5f, groundLayer);
        if (ledgeHit.collider == null) return false;

        return true;
    }

    private Bomb FindClosestBomb(float range)
    {
        Bomb[] bombs = FindObjectsByType<Bomb>(FindObjectsInactive.Exclude);
        Bomb closest = null;
        float minDist = range;

        foreach (var b in bombs)
        {
            if (b == null || b.IsExploded || b.IsSwallowed || b.IsHeld || b.IsExtinguished) continue;
            float dist = Vector2.Distance(transform.position, b.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = b;
            }
        }
        return closest;
    }

    // ==========================================
    // 1. BALD PIRATE: kicks the bomb to dodge it
    // ==========================================
    private void UpdateBaldPirate()
    {
        Bomb bomb = FindClosestBomb(actionRange);
        if (bomb != null && Time.time >= nextActionTime)
        {
            nextActionTime = Time.time + attackCooldown;
            FaceTarget(bomb.transform.position);

            if (animator != null)
            {
                animator.SetTrigger("Kick");
            }

            StartCoroutine(KickRoutine(bomb));
        }
    }

    private IEnumerator KickRoutine(Bomb bomb)
    {
        yield return new WaitForSeconds(0.15f);

        if (bomb != null && !bomb.IsExploded)
        {
            float kickDir = movingRight ? 1f : -1f;
            bomb.Kick(new Vector2(kickDir, 0.4f).normalized, 8.5f);
        }
    }

    // =========================================================================
    // 2. BIG GUY: throws bomb to player to dodge it, takes 3 bombs to kill
    // =========================================================================
    private void UpdateBigGuy()
    {
        Bomb bomb = FindClosestBomb(actionRange);
        if (bomb != null && Time.time >= nextActionTime)
        {
            nextActionTime = Time.time + attackCooldown;
            StartCoroutine(BigGuyPickAndThrowRoutine(bomb));
        }
    }

    private IEnumerator BigGuyPickAndThrowRoutine(Bomb bomb)
    {
        isHandlingBomb = true;
        rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetBool("IsRunning", false);

        FaceTarget(bomb.transform.position);

        // Pick up bomb
        if (animator != null) animator.SetTrigger("PickBomb");
        yield return new WaitForSeconds(0.25f);

        if (bomb != null && !bomb.IsExploded)
        {
            bomb.PickUp(transform, new Vector3(movingRight ? 0.4f : -0.4f, 0.5f, 0f));

            yield return new WaitForSeconds(0.35f);

            // Face Player before throwing
            if (playerTransform != null)
            {
                FaceTarget(playerTransform.position);
            }

            if (animator != null) animator.SetTrigger("ThrowBomb");
            yield return new WaitForSeconds(0.2f);

            if (bomb != null && !bomb.IsExploded)
            {
                Vector2 throwDir = playerTransform != null 
                    ? (playerTransform.position - transform.position).normalized 
                    : new Vector2(movingRight ? 1f : -1f, 0.5f).normalized;

                bomb.Throw(throwDir, 9.0f);
            }
        }

        yield return new WaitForSeconds(0.3f);
        isHandlingBomb = false;
    }

    // ==============================================================
    // 3. CUCUMBER: blows out bomb wick, bites player, requires 2 bombs to kill
    // ==============================================================
    private void UpdateCucumber()
    {
        if (isHandlingBomb) return;

        // 1. Check for bomb to extinguish (blow the wick)
        Bomb bomb = FindClosestBomb(actionRange + 0.35f);
        if (bomb != null && !bomb.IsExploded && !bomb.IsSwallowed && !bomb.IsExtinguished && Time.time >= nextActionTime)
        {
            nextActionTime = Time.time + attackCooldown;
            StartCoroutine(CucumberBlowWickRoutine(bomb));
            return;
        }

        // 2. Check for player to chase / bite
        if (playerTransform != null)
        {
            float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

            if (distToPlayer <= detectionRange)
            {
                FaceTarget(playerTransform.position);

                if (distToPlayer <= actionRange && Time.time >= nextActionTime)
                {
                    nextActionTime = Time.time + attackCooldown;
                    StartCoroutine(CucumberBiteRoutine());
                }
            }
        }
    }

    private IEnumerator CucumberBlowWickRoutine(Bomb bomb)
    {
        isHandlingBomb = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("BlowWick");
        }

        FaceTarget(bomb.transform.position);

        // BlowWick animation is 11 frames @ 12fps = 0.916s duration
        // Wick blows out around frame 6 (~0.45s)
        yield return new WaitForSeconds(0.45f);

        if (bomb != null && !bomb.IsExploded && !bomb.IsSwallowed && !bomb.IsExtinguished)
        {
            bomb.Extinguish();
        }

        yield return new WaitForSeconds(0.45f);
        isHandlingBomb = false;
    }

    private IEnumerator CucumberBiteRoutine()
    {
        isHandlingBomb = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("Bite");
        }

        // Bite animation is 11 frames @ 14fps = 0.785s duration
        // Bite connects at ~0.28s
        yield return new WaitForSeconds(0.28f);

        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= actionRange + 0.4f)
        {
            var damageable = playerTransform.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector2 knockback = new Vector2(movingRight ? 5f : -5f, 3.5f);
                damageable.TakeDamage(1, knockback);
            }
        }

        yield return new WaitForSeconds(0.5f);
        isHandlingBomb = false;
    }

    // =========================================================================
    // 4. CAPTAIN: fearful, just runs away, requires 1 bomb to kill
    // =========================================================================
    private void UpdateCaptain()
    {
        bool threatDetected = false;
        Vector3 threatPos = Vector3.zero;

        // Check if player is near
        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= detectionRange)
        {
            threatDetected = true;
            threatPos = playerTransform.position;
        }

        // Check if bomb is near
        Bomb bomb = FindClosestBomb(detectionRange);
        if (bomb != null)
        {
            threatDetected = true;
            threatPos = bomb.transform.position;
        }

        if (threatDetected)
        {
            isScared = true;
            calmTimer = 2.0f;

            // Run AWAY from the threat
            bool threatIsRight = threatPos.x > transform.position.x;
            if (threatIsRight && movingRight)
            {
                FlipDirection();
            }
            else if (!threatIsRight && !movingRight)
            {
                FlipDirection();
            }
        }
        else if (isScared)
        {
            calmTimer -= Time.deltaTime;
            if (calmTimer <= 0f)
            {
                isScared = false;
            }
        }

        if (animator != null)
        {
            animator.SetBool("IsScared", isScared);
        }
    }

    // =========================================================================
    // 5. WHALE: eats up bomb, bites player, dies with fall damage
    // =========================================================================
    private void UpdateWhale()
    {
        // Check for bomb to swallow
        Bomb bomb = FindClosestBomb(actionRange + 0.3f);
        if (bomb != null && Time.time >= nextActionTime)
        {
            nextActionTime = Time.time + attackCooldown;
            FaceTarget(bomb.transform.position);

            if (animator != null) animator.SetTrigger("Swallow");
            bomb.Swallow();
            return;
        }

        // Check for player to bite
        if (playerTransform != null)
        {
            float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distToPlayer <= detectionRange)
            {
                FaceTarget(playerTransform.position);

                if (distToPlayer <= actionRange && Time.time >= nextActionTime)
                {
                    nextActionTime = Time.time + attackCooldown;
                    StartCoroutine(WhaleBiteRoutine());
                }
            }
        }
    }

    private IEnumerator WhaleBiteRoutine()
    {
        if (animator != null) animator.SetTrigger("Bite");
        yield return new WaitForSeconds(0.3f);

        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= actionRange + 0.3f)
        {
            var damageable = playerTransform.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector2 knockback = new Vector2(movingRight ? 6f : -6f, 4f);
                damageable.TakeDamage(1, knockback);
            }
        }
    }

    private void DieFromFallDamage()
    {
        if (isDead) return;
        Die();
    }

    // ==============================================================
    // IDAMAGEABLE IMPLEMENTATION
    // ==============================================================
    public void TakeDamage(int damage, Vector2 knockback)
    {
        if (isDead) return;

        // Whale only dies from fall damage, but still takes knockback from explosions!
        if (enemyType == EnemyType.Whale)
        {
            rb.linearVelocity = knockback;
            return;
        }

        currentHealth -= damage;

        if (animator != null)
        {
            animator.SetTrigger(currentHealth <= 0 ? "Dead" : "Hit");
        }

        rb.linearVelocity = knockback;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(HitStunRoutine(0.35f));
        }
    }

    private IEnumerator HitStunRoutine(float duration)
    {
        isHandlingBomb = true;
        if (animator != null) animator.SetBool("IsRunning", false);
        yield return new WaitForSeconds(duration);
        isHandlingBomb = false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
        if (col != null) col.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger("Dead");
        }

        OnAnyEnemyDied?.Invoke(this);

        // Destroy after death animations complete
        Destroy(gameObject, 1.2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, actionRange);
    }
}
