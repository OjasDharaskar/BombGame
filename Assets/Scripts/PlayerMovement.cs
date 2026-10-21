using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
public class PlayerMovement : MonoBehaviour, IDamageable
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6.5f;
    [SerializeField] private float jumpForce = 11.5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.4f, 0.1f);

    [Header("Bomb Throwing")]
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwForceX = 7.5f;
    [SerializeField] private float throwForceY = 4f;
    [SerializeField] private float throwCooldown = 0.5f;

    [Header("Health & Combat")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float invulnerabilityDuration = 1.2f;

    [Header("Audio Settings")]
    [Tooltip("Walking sound - Gravel.mp3 / Gravel.ogg")]
    [SerializeField] private AudioClip walkSound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip fallSound;
    [SerializeField] private float stepInterval = 0.32f;
    [SerializeField] private float fallBoundaryY = -6.5f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource footstepSource;

    [Header("Level Restart Settings")]
    [SerializeField] private float restartDelay = 0.35f;

    // Components
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    // State
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isFacingRight = true;
    private int currentHealth;
    private bool isInvulnerable;
    private bool isDead;
    private float nextThrowTime;
    private Vector3 baseScale;
    private float stepTimer;
    private Coroutine restartCoroutine;

    // Jump buffer & coyote time for responsive jumping
    private float coyoteTime = 0.12f;
    private float coyoteTimer;
    private float jumpBufferTime = 0.12f;
    private float jumpBufferTimer;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    public event System.Action<int, int> OnHealthChanged;
    public event System.Action<Bomb> OnBombThrown;
    public event System.Action OnPlayerDied;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        baseScale = transform.localScale;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Ground Layer setup: make sure both Ground and Default layers are recognized
        if (groundLayer.value == 0)
        {
            groundLayer = LayerMask.GetMask("Ground", "Default");
            if (groundLayer.value == 0) groundLayer = ~0;
        }
        else
        {
            groundLayer |= LayerMask.GetMask("Default", "Ground");
        }

        SetupAudioSources();
        EnsureClipsLoaded();
    }

    private void SetupAudioSources()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        if (footstepSource == null)
        {
            var sources = GetComponents<AudioSource>();
            if (sources.Length > 1)
            {
                footstepSource = sources[1];
            }
            else
            {
                footstepSource = gameObject.AddComponent<AudioSource>();
            }
            footstepSource.playOnAwake = false;
            footstepSource.spatialBlend = 0f;
        }
    }

    public void EnsureClipsLoaded()
    {
#if UNITY_EDITOR
        if (walkSound == null)
        {
            walkSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Gravel.ogg");
            if (walkSound == null) walkSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Gravel.mp3");
        }
        if (jumpSound == null) jumpSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Jump_v1.ogg");
        if (hurtSound == null) hurtSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Hurt_v1.ogg");
        if (deathSound == null) deathSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Death_v1.ogg");
        if (fallSound == null) fallSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Fall_v1.ogg");
#endif
    }

    private void Update()
    {
        // Restart hotkey R is always available (during life or death)
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartCurrentLevel();
            return;
        }

        // Direct level hotkeys for testing or switching
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            LevelManager.Instance.LoadLevel1();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            LevelManager.Instance.LoadLevel2();
            return;
        }

        if (isDead)
        {
            // If dead, pressing Space or Return restarts immediately
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                RestartCurrentLevel();
            }
            return;
        }

        // Pit fall death check
        if (transform.position.y < fallBoundaryY)
        {
            HandleFallDeath();
            return;
        }

        // Input
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Ground Check
        CheckGrounded();

        // Walking footsteps audio (Gravel.mp3 / Gravel.ogg)
        UpdateWalkingSound();

        // Coyote Timer
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        // Jump mapped EXCLUSIVELY to Spacebar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            Jump();
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        // Shoot Bomb
        if ((Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)) && Time.time >= nextThrowTime)
        {
            ShootBomb();
        }

        // Flip character sprite
        if (horizontalInput > 0.05f && !isFacingRight)
        {
            Flip();
        }
        else if (horizontalInput < -0.05f && isFacingRight)
        {
            Flip();
        }

        // Update Animator
        UpdateAnimator();

        wasGrounded = isGrounded;
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    private void CheckGrounded()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (col == null) return;

        // Check area slightly below the bottom edge of the player's collider
        float checkWidth = col.bounds.size.x * 0.75f;
        float checkHeight = 0.12f;
        Vector2 checkPos = groundCheckPoint != null 
            ? (Vector2)groundCheckPoint.position 
            : new Vector2(col.bounds.center.x, col.bounds.min.y - 0.04f);

        Vector2 checkSize = groundCheckPoint != null ? groundCheckSize : new Vector2(checkWidth, checkHeight);

        Collider2D[] hits = Physics2D.OverlapBoxAll(checkPos, checkSize, 0f, groundLayer);
        bool grounded = false;
        foreach (var h in hits)
        {
            if (h != null && h.gameObject != gameObject && !h.transform.IsChildOf(transform) && !h.isTrigger)
            {
                // Ignore enemy colliders and bomb colliders for ground standing
                if (!h.CompareTag("Enemy") && !h.CompareTag("Bomb"))
                {
                    grounded = true;
                    break;
                }
            }
        }

        // Fallback downward raycast for slopes and edge detection
        if (!grounded)
        {
            Vector2 rayOrigin = new Vector2(col.bounds.center.x, col.bounds.min.y + 0.05f);
            RaycastHit2D[] rayHits = Physics2D.RaycastAll(rayOrigin, Vector2.down, 0.18f, groundLayer);
            foreach (var rh in rayHits)
            {
                if (rh.collider != null && rh.collider.gameObject != gameObject && !rh.collider.transform.IsChildOf(transform) && !rh.collider.isTrigger)
                {
                    if (!rh.collider.CompareTag("Enemy") && !rh.collider.CompareTag("Bomb"))
                    {
                        grounded = true;
                        break;
                    }
                }
            }
        }

        isGrounded = grounded;
    }

    private void UpdateWalkingSound()
    {
        if (isGrounded && !isDead && Mathf.Abs(horizontalInput) > 0.05f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                PlayWalkSound();
                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    private void PlayWalkSound()
    {
        if (walkSound != null && footstepSource != null)
        {
            footstepSource.pitch = Random.Range(0.92f, 1.08f);
            footstepSource.PlayOneShot(walkSound, 0.85f);
        }
        else if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayWalk();
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        isGrounded = false;
        coyoteTimer = 0f;

        PlayJumpSound();
    }

    private void PlayJumpSound()
    {
        if (jumpSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(jumpSound, 1f);
        }
        else if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayJump();
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3((isFacingRight ? 1f : -1f) * Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
    }

    private void ShootBomb()
    {
        if (bombPrefab == null) return;

        nextThrowTime = Time.time + throwCooldown;

        Vector3 spawnPos = throwPoint != null ? throwPoint.position : transform.position + new Vector3(isFacingRight ? 0.6f : -0.6f, 0.2f, 0f);
        GameObject bombObj = Instantiate(bombPrefab, spawnPos, Quaternion.identity);

        Bomb bomb = bombObj.GetComponent<Bomb>();
        if (bomb != null)
        {
            float dirX = isFacingRight ? 1f : -1f;
            Vector2 throwVelocity = new Vector2(dirX * throwForceX, throwForceY);
            bomb.Throw(throwVelocity.normalized, throwVelocity.magnitude);
            bomb.OnInitiated();
        }

        // Play explosion sound effect whenever the bomb is initiated
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBombInitiated(spawnPos);
        }

        OnBombThrown?.Invoke(bomb);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        animator.SetBool("IsGrounded", isGrounded);
    }

    // ==========================================
    // ENEMY CONTACT & COLLISION DAMAGE
    // ==========================================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleEnemyContact(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleEnemyContact(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleEnemyContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandleEnemyContact(other);
    }

    private void HandleEnemyContact(Collider2D other)
    {
        if (isDead || isInvulnerable) return;

        // Check if other is an enemy
        if (other.CompareTag("Enemy") || other.GetComponent<EnemyAI>() != null || other.GetComponentInParent<EnemyAI>() != null)
        {
            EnemyAI enemy = other.GetComponent<EnemyAI>();
            if (enemy == null) enemy = other.GetComponentInParent<EnemyAI>();

            if (enemy != null && enemy.IsDead) return;

            // Knockback away from enemy
            Vector2 dir = (transform.position - other.transform.position).normalized;
            if (dir.sqrMagnitude < 0.001f) dir = (isFacingRight ? Vector2.left : Vector2.right) + Vector2.up;
            dir.y = Mathf.Max(0.5f, dir.y);

            TakeDamage(1, dir.normalized * 7.5f);
        }
    }

    public void TakeDamage(int damage, Vector2 knockback)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (animator != null)
        {
            animator.SetTrigger(currentHealth <= 0 ? "Dead" : "Hit");
        }

        // Apply knockback
        rb.linearVelocity = knockback;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            PlayHurtSound();
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    private void PlayHurtSound()
    {
        if (hurtSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(hurtSound, 1f);
        }
        else if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayHurt();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        float elapsed = 0f;
        float flashInterval = 0.1f;

        while (elapsed < invulnerabilityDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
        isInvulnerable = false;
    }

    private void HandleFallDeath()
    {
        if (isDead) return;

        PlayFallSound();
        currentHealth = 0;
        OnHealthChanged?.Invoke(0, maxHealth);
        Die();
    }

    private void PlayFallSound()
    {
        if (fallSound != null && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(fallSound, 1f);
        }
        else if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayFall();
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        rb.linearVelocity = Vector2.zero;
        if (col != null) col.enabled = false;

        PlayDeathSound();
        OnPlayerDied?.Invoke();

        if (animator != null)
        {
            animator.SetTrigger("Dead");
        }

        // Start level restart sequence
        if (restartCoroutine != null) StopCoroutine(restartCoroutine);
        restartCoroutine = StartCoroutine(RestartLevelRoutine(restartDelay));
    }

    private void PlayDeathSound()
    {
        if (deathSound != null && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(deathSound, 1f);
        }
        else if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayDeath();
        }
    }

    public void RestartCurrentLevel()
    {
        LevelManager.Instance.RestartCurrentLevel();
    }

    private IEnumerator RestartLevelRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        RestartCurrentLevel();
    }

    private void OnDrawGizmosSelected()
    {
        if (col != null)
        {
            Gizmos.color = Color.green;
            Vector2 checkPos = groundCheckPoint != null 
                ? (Vector2)groundCheckPoint.position 
                : new Vector2(col.bounds.center.x, col.bounds.min.y - 0.04f);
            Vector2 checkSize = groundCheckPoint != null 
                ? groundCheckSize 
                : new Vector2(col.bounds.size.x * 0.75f, 0.12f);
            Gizmos.DrawWireCube(checkPos, checkSize);
        }

        // Draw fall boundary in red
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-20f, fallBoundaryY, 0f), new Vector3(20f, fallBoundaryY, 0f));
    }
}
