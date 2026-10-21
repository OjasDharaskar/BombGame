using System.Collections;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [Header("Timer & Explosion")]
    [SerializeField] private float fuseTime = 3.5f;
    [SerializeField] private float explosionRadius = 1.6f;
    [SerializeField] private int explosionDamage = 1;
    [SerializeField] private float explosionKnockback = 7f;

    [Header("Bomb Bar Sprites")]
    [SerializeField] private SpriteRenderer barRenderer;
    [SerializeField] private GameObject barContainer;
    [SerializeField] private Sprite[] chargingSprites;
    [SerializeField] private Sprite[] fullSprites;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D col;

    private float currentTimer;
    private bool isExploded;
    private bool isSwallowed;
    private bool isHeld;
    private Transform holdParent;
    private Vector3 holdOffset = new Vector3(0f, 0.4f, 0f);

    public bool IsExploded => isExploded;
    public bool IsSwallowed => isSwallowed;
    public bool IsHeld => isHeld;
    public float CurrentTimer => currentTimer;
    public float FuseTime => fuseTime;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (col == null) col = GetComponent<Collider2D>();
        currentTimer = fuseTime;
    }

    private void Start()
    {
        UpdateBarSprite();
    }

    private void Update()
    {
        if (isExploded || isSwallowed) return;

        if (isHeld)
        {
            if (holdParent != null)
            {
                transform.position = holdParent.position + holdOffset;
            }
            else
            {
                isHeld = false;
                if (rb != null) rb.simulated = true;
            }
        }

        currentTimer -= Time.deltaTime;
        UpdateBarSprite();

        if (currentTimer <= 0f)
        {
            Explode();
        }
    }

    private void UpdateBarSprite()
    {
        if (barRenderer == null) return;

        float progress = Mathf.Clamp01(1f - (currentTimer / fuseTime));

        if (progress >= 1f && fullSprites != null && fullSprites.Length > 0)
        {
            int fIdx = Mathf.FloorToInt(Time.time * 10f) % fullSprites.Length;
            barRenderer.sprite = fullSprites[fIdx];
        }
        else if (chargingSprites != null && chargingSprites.Length > 0)
        {
            int index = Mathf.Clamp(Mathf.FloorToInt(progress * chargingSprites.Length), 0, chargingSprites.Length - 1);
            barRenderer.sprite = chargingSprites[index];
        }
    }

    public void Kick(Vector2 direction, float force)
    {
        if (isExploded || isSwallowed) return;

        if (isHeld) Drop();

        if (rb != null)
        {
            rb.simulated = true;
            // Kick away with strong horizontal speed and slight upward arc
            Vector2 kickVelocity = new Vector2(Mathf.Sign(direction.x) * force, Mathf.Max(2.5f, force * 0.35f));
            rb.linearVelocity = kickVelocity;
            rb.angularVelocity = -Mathf.Sign(direction.x) * 180f;
        }
    }

    public void PickUp(Transform holder, Vector3 offset)
    {
        if (isExploded || isSwallowed) return;

        isHeld = true;
        holdParent = holder;
        holdOffset = offset;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
    }

    public void Drop()
    {
        isHeld = false;
        holdParent = null;
        if (rb != null)
        {
            rb.simulated = true;
        }
    }

    public void Throw(Vector2 direction, float force)
    {
        if (isExploded || isSwallowed) return;

        isHeld = false;
        holdParent = null;

        if (rb != null)
        {
            rb.simulated = true;
            Vector2 throwVelocity = new Vector2(direction.x * force, Mathf.Max(3f, force * 0.4f));
            rb.linearVelocity = throwVelocity;
            rb.angularVelocity = -Mathf.Sign(direction.x) * 200f;
        }
    }

    public void Swallow()
    {
        if (isExploded || isSwallowed) return;

        isSwallowed = true;
        if (isHeld) Drop();

        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (barContainer != null) barContainer.SetActive(false);
        else if (barRenderer != null) barRenderer.gameObject.SetActive(false);

        StartCoroutine(SwallowRoutine());
    }

    private IEnumerator SwallowRoutine()
    {
        Vector3 initialScale = transform.localScale;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, elapsed / duration);
            yield return null;
        }

        Destroy(gameObject);
    }

    public void Explode()
    {
        if (isExploded || isSwallowed) return;

        isExploded = true;
        if (isHeld) Drop();

        if (barContainer != null) barContainer.SetActive(false);
        else if (barRenderer != null) barRenderer.gameObject.SetActive(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (col != null) col.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger("Explode");
        }

        // Deal damage in blast radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            if (hit == null || hit.gameObject == gameObject) continue;

            Vector2 dir = (hit.transform.position - transform.position).normalized;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.up;

            var damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(explosionDamage, dir * explosionKnockback);
            }
        }

        Destroy(gameObject, 0.7f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
