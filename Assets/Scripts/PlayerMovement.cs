using System.Collections;
using UnityEngine;

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

    // Components
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    // State
    private float horizontalInput;
    private bool isGrounded;
    private bool isFacingRight = true;
    private int currentHealth;
    private bool isInvulnerable;
    private bool isDead;
    private float nextThrowTime;
    private Vector3 baseScale;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        baseScale = transform.localScale;
        currentHealth = maxHealth;

        // If groundLayer not configured, default to Ground and Default layers
        if (groundLayer.value == 0)
        {
            groundLayer = LayerMask.GetMask("Ground", "Default");
            if (groundLayer.value == 0) groundLayer = ~0; // fallback to everything except self
        }
    }

    private void Update()
    {
        if (isDead) return;

        // Input
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Ground Check
        CheckGrounded();

        // Jump
        if ((Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && isGrounded)
        {
            Jump();
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
        Vector2 checkPos = groundCheckPoint != null ? (Vector2)groundCheckPoint.position : (Vector2)col.bounds.min + new Vector2(col.bounds.size.x * 0.5f, 0.05f);
        Collider2D hit = Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer);
        isGrounded = hit != null && hit.gameObject != gameObject;
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
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
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        animator.SetBool("IsGrounded", isGrounded);
    }

    public void TakeDamage(int damage, Vector2 knockback)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= damage;

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
            StartCoroutine(InvulnerabilityRoutine());
        }
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

    private void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        if (col != null) col.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (col != null)
        {
            Gizmos.color = Color.green;
            Vector2 checkPos = groundCheckPoint != null ? (Vector2)groundCheckPoint.position : (Vector2)col.bounds.min + new Vector2(col.bounds.size.x * 0.5f, 0.05f);
            Gizmos.DrawWireCube(checkPos, groundCheckSize);
        }
    }
}
