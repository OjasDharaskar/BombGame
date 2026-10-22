using System.Collections;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [Header("Timer & Explosion")]
    [SerializeField] private float fuseTime = 3.5f;
    [SerializeField] private float explosionRadius = 1.6f;
    [SerializeField] private int explosionDamage = 1;
    [SerializeField] private float explosionKnockback = 7f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private bool playAudioOnInitiated = true;

    [Header("Bomb Bar Sprites")]
    [SerializeField] private SpriteRenderer barRenderer;
    [SerializeField] private GameObject barContainer;
    [SerializeField] private Sprite[] chargingSprites;
    [SerializeField] private Sprite[] fullSprites;
    [SerializeField] private Sprite closingSprite;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D col;

    private float currentTimer;
    private float elapsedTimer;
    private bool isExploded;
    private bool isSwallowed;
    private bool isHeld;
    private bool isExtinguished;
    private Transform holdParent;
    private Vector3 holdOffset = new Vector3(0f, 0.4f, 0f);
    private Vector3 barLocalOffset = new Vector3(0f, 0.45f, 0f);

    public bool IsExploded => isExploded;
    public bool IsSwallowed => isSwallowed;
    public bool IsHeld => isHeld;
    public bool IsExtinguished => isExtinguished;
    public float CurrentTimer => currentTimer;
    public float FuseTime => fuseTime;
    public float ElapsedTimer => elapsedTimer;
    public float Progress => Mathf.Clamp01(elapsedTimer / fuseTime);

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (col == null) col = GetComponent<Collider2D>();
        currentTimer = fuseTime;

#if UNITY_EDITOR
        if (explosionSound == null)
        {
            explosionSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Explosion_v1.ogg");
        }
#endif
    }

    private void Start()
    {
        elapsedTimer = 0f;
        UpdateBarSprite();
    }

    private void Update()
    {
        if (isExploded || isSwallowed || isExtinguished) return;

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

        elapsedTimer += Time.deltaTime;
        currentTimer -= Time.deltaTime;
        UpdateBarSprite();

        if (currentTimer <= 0f)
        {
            Explode();
        }
    }

    private void LateUpdate()
    {
        if (barContainer != null && barContainer.activeSelf)
        {
            barContainer.transform.rotation = Quaternion.identity;
            barContainer.transform.position = transform.position + barLocalOffset;
        }
    }

    private void UpdateBarSprite()
    {
        if (barRenderer == null || barContainer == null || !barContainer.activeSelf) return;

        float chargeDuration = fuseTime * 0.72f;
        float fullDuration = fuseTime * 0.88f;
        float closeDuration = fuseTime * 0.95f;

        if (elapsedTimer < chargeDuration)
        {
            if (chargingSprites != null && chargingSprites.Length > 0)
            {
                float t = Mathf.Clamp01(elapsedTimer / chargeDuration);
                int index = Mathf.Clamp(Mathf.FloorToInt(t * chargingSprites.Length), 0, chargingSprites.Length - 1);
                barRenderer.sprite = chargingSprites[index];
            }
        }
        else if (elapsedTimer < fullDuration)
        {
            if (fullSprites != null && fullSprites.Length > 0)
            {
                int fIdx = Mathf.FloorToInt(Time.time * 12f) % fullSprites.Length;
                barRenderer.sprite = fullSprites[fIdx];
            }
        }
        else if (elapsedTimer < closeDuration)
        {
            if (closingSprite != null)
            {
                barRenderer.sprite = closingSprite;
            }
        }
        else
        {
            // Bar gets charged and then it disappears before explosion!
            barContainer.SetActive(false);
        }
    }

    public void Kick(Vector2 direction, float force)
    {
        if (isExploded || isSwallowed || isExtinguished) return;

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
        if (isExploded || isSwallowed || isExtinguished) return;

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
        if (isExploded || isSwallowed || isExtinguished) return;

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
        if (isExploded || isSwallowed || isExtinguished) return;

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

    public void Extinguish()
    {
        if (isExploded || isSwallowed || isExtinguished) return;

        isExtinguished = true;
        if (isHeld) Drop();

        if (barContainer != null) barContainer.SetActive(false);
        else if (barRenderer != null) barRenderer.gameObject.SetActive(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (col != null) col.enabled = false;

        StartCoroutine(ExtinguishRoutine());
    }

    private IEnumerator ExtinguishRoutine()
    {
        AudioClip extinguishSound = null;
#if UNITY_EDITOR
        extinguishSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Power down_v1.ogg");
#endif
        if (extinguishSound != null)
        {
            AudioSource.PlayClipAtPoint(extinguishSound, transform.position);
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float duration = 0.45f;
        float elapsed = 0f;
        Color initColor = sr != null ? sr.color : Color.white;
        Vector3 initScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (sr != null)
            {
                sr.color = new Color(initColor.r, initColor.g, initColor.b, Mathf.Lerp(1f, 0f, t));
            }
            transform.localScale = Vector3.Lerp(initScale, initScale * 0.7f, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    public void OnInitiated()
    {
        if (playAudioOnInitiated)
        {
            PlayExplosionSound();
        }
    }

    private void PlayExplosionSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayExplosion(transform.position);
        }
        else if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }
    }

    public void Explode()
    {
        if (isExploded || isSwallowed || isExtinguished) return;

        isExploded = true;
        if (isHeld) Drop();

        PlayExplosionSound();

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
