using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Level 2 Water Hazard Controller:
/// In Level 2, water starts at the bottom floor of the starting well and fills up over 15 seconds
/// until reaching the platform directly above the table (y ≈ -2.85), which is the maximum height
/// the water will rise.
/// Any player submerged in the water takes periodic damage (1 heart per second).
/// </summary>
public class Level2WaterController : MonoBehaviour
{
    [Header("Rise & Timing Settings")]
    [Tooltip("Duration in seconds for the water to completely fill from start to the platform above the table")]
    [SerializeField] private float fillDuration = 15.0f;

    [Tooltip("Delay in seconds before the water begins rising")]
    [SerializeField] private float startDelay = 0.0f;

    [Header("Elevation & Dimensions")]
    [Tooltip("Starting water level (bottom floor of starting well)")]
    [SerializeField] private float startY = -6.20f;

    [Tooltip("Target water level: max height water will rise (the platform above the table, y ≈ -2.85)")]
    [SerializeField] private float targetY = -10.99f;

    [Tooltip("Width of the water hazard volume covering the shaft interior")]
    [SerializeField] private float waterWidth = 6.60f;

    [Tooltip("Horizontal center X coordinate of the water shaft")]
    [SerializeField] private float waterCenterX = -3.25f;

    [Header("Damage Settings")]
    [Tooltip("Interval in seconds between damage ticks while submerged")]
    [SerializeField] private float damageInterval = 1.0f;

    [Tooltip("Damage dealt to player per tick while submerged")]
    [SerializeField] private int damagePerTick = 1;

    [Tooltip("Upward buoyancy knockback applied when taking water damage")]
    [SerializeField] private float buoyancyKnockback = 2.0f;

    [Header("Visual Settings")]
    [SerializeField] private Color deepWaterColor = new Color(0.08f, 0.42f, 0.88f, 0.70f);
    [SerializeField] private Color shallowWaterColor = new Color(0.18f, 0.72f, 0.98f, 0.65f);
    [SerializeField] private Color surfaceFoamColor = new Color(0.85f, 0.98f, 1.0f, 0.88f);
    [SerializeField] private string sortingLayerName = "Player";
    [SerializeField] private int waterBodySortingOrder = 2;
    [SerializeField] private int surfaceSortingOrder = 3;

    // Internal state
    private float elapsedTime = 0f;
    private float currentSurfaceY;
    private float currentHeight;
    private bool hasFinishedFilling = false;
    private float nextDamageTime = 0f;

    // References
    private Transform waterBodyTransform;
    private SpriteRenderer waterBodyRenderer;
    private Transform surfaceTransform;
    private SpriteRenderer surfaceRenderer;
    private BoxCollider2D triggerCollider;
    private ParticleSystem bubbleParticles;

    // Tracked player
    private PlayerMovement player;
    private bool isPlayerSubmerged = false;

    public float CurrentSurfaceY => currentSurfaceY;
    public float TargetY => targetY;
    public float FillProgress => Mathf.Clamp01((elapsedTime - startDelay) / fillDuration);
    public bool HasFinishedFilling => hasFinishedFilling;
    public bool IsPlayerSubmerged => isPlayerSubmerged;

    private void Awake()
    {
        // Enforce: ONLY execute in Level 2 scene
        string sceneName = SceneManager.GetActiveScene().name;
        if (!sceneName.Contains("2") && !sceneName.Equals("Level2", System.StringComparison.OrdinalIgnoreCase) && !sceneName.Equals("Leve2", System.StringComparison.OrdinalIgnoreCase))
        {
            enabled = false;
            return;
        }

        SetupWaterStructure();
    }

    private void Start()
    {
        currentSurfaceY = startY;
        currentHeight = 0.01f;
        UpdateWaterGeometry(currentSurfaceY);

        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.ShowMessage("WARNING: WATER IS RISING! CLIMB UP!", 3.0f);
        }
    }

    private void SetupWaterStructure()
    {
        transform.position = new Vector3(waterCenterX, startY, 0f);

        // 1. Water Body Child
        Transform bodyChild = transform.Find("WaterBody");
        if (bodyChild == null)
        {
            GameObject bodyGO = new GameObject("WaterBody");
            bodyGO.transform.SetParent(transform, false);
            bodyChild = bodyGO.transform;
        }
        waterBodyTransform = bodyChild;
        waterBodyTransform.localPosition = Vector3.zero;

        waterBodyRenderer = waterBodyTransform.GetComponent<SpriteRenderer>();
        if (waterBodyRenderer == null)
        {
            waterBodyRenderer = waterBodyTransform.gameObject.AddComponent<SpriteRenderer>();
        }
        waterBodyRenderer.sprite = CreateGradientWaterSprite();
        waterBodyRenderer.sortingLayerName = sortingLayerName;
        waterBodyRenderer.sortingOrder = waterBodySortingOrder;

        // 2. Water Surface Crest Child
        Transform surfChild = transform.Find("WaterSurface");
        if (surfChild == null)
        {
            GameObject surfGO = new GameObject("WaterSurface");
            surfGO.transform.SetParent(transform, false);
            surfChild = surfGO.transform;
        }
        surfaceTransform = surfChild;

        surfaceRenderer = surfaceTransform.GetComponent<SpriteRenderer>();
        if (surfaceRenderer == null)
        {
            surfaceRenderer = surfaceTransform.gameObject.AddComponent<SpriteRenderer>();
        }
        surfaceRenderer.sprite = CreateSurfaceWaveSprite();
        surfaceRenderer.sortingLayerName = sortingLayerName;
        surfaceRenderer.sortingOrder = surfaceSortingOrder;

        // 3. Trigger Collider
        triggerCollider = GetComponent<BoxCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        triggerCollider.isTrigger = true;

        // 4. Subtle bubble particles with proper material (prevents magenta squares)
        SetupBubbleParticles();
    }

    private void SetupBubbleParticles()
    {
        bubbleParticles = GetComponentInChildren<ParticleSystem>();
        if (bubbleParticles == null)
        {
            GameObject bubbleGO = new GameObject("BubbleParticles");
            bubbleGO.transform.SetParent(transform, false);
            bubbleGO.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            bubbleParticles = bubbleGO.AddComponent<ParticleSystem>();
        }

        var main = bubbleParticles.main;
        main.startLifetime = 3.0f;
        main.startSpeed = 0.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = new Color(0.85f, 0.95f, 1.0f, 0.55f);
        main.maxParticles = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = bubbleParticles.emission;
        emission.rateOverTime = 6f;

        var shape = bubbleParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(waterWidth * 0.85f, 0.15f, 0.1f);

        var velOverLifetime = bubbleParticles.velocityOverLifetime;
        velOverLifetime.enabled = true;
        velOverLifetime.y = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        velOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

        var renderer = bubbleParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            Shader particleShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
            if (particleShader == null) particleShader = Shader.Find("Sprites/Default");
            if (particleShader != null)
            {
                Material mat = new Material(particleShader);
                mat.mainTexture = CreateBubbleTexture();
                renderer.material = mat;
            }
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = surfaceSortingOrder;
        }
    }

    private Texture2D CreateBubbleTexture()
    {
        const int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float radius = (size - 1) * 0.5f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    float norm = dist / radius;
                    float rim = Mathf.Pow(norm, 1.8f) * 0.6f + 0.4f;
                    float alpha = Mathf.Clamp01((1f - norm) * 2.5f) * rim;
                    tex.SetPixel(x, y, new Color(0.9f, 0.96f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private void Update()
    {
        // Advance timer
        elapsedTime += Time.deltaTime;

        if (elapsedTime >= startDelay)
        {
            float fillTime = elapsedTime - startDelay;
            float progress = Mathf.Clamp01(fillTime / fillDuration);

            currentSurfaceY = Mathf.Lerp(startY, targetY, progress);
            currentHeight = Mathf.Max(0.01f, currentSurfaceY - startY);

            if (progress >= 1.0f && !hasFinishedFilling)
            {
                hasFinishedFilling = true;
                currentSurfaceY = targetY;
                currentHeight = targetY - startY;
                if (GameHUD.Instance != null)
                {
                    GameHUD.Instance.ShowMessage("WATER HAS FILLED TO THE FIRST PLATFORM!", 2.5f);
                }
            }
        }

        UpdateWaterGeometry(currentSurfaceY);
        CheckPlayerSubmersion();
        UpdateWaveSurfaceAnimation();
    }

    private void UpdateWaterGeometry(float surfaceY)
    {
        currentHeight = Mathf.Max(0.01f, surfaceY - startY);

        // Update body transform: anchored at bottom center (pivot 0.5, 0)
        if (waterBodyTransform != null)
        {
            waterBodyTransform.localPosition = Vector3.zero;
            waterBodyTransform.localScale = new Vector3(waterWidth, currentHeight, 1f);
        }

        // Update surface crest transform: sits right on the water line
        if (surfaceTransform != null)
        {
            surfaceTransform.localPosition = new Vector3(0f, currentHeight, 0f);
            surfaceTransform.localScale = new Vector3(waterWidth, 0.35f, 1f);
        }

        // Update BoxCollider2D to match exact water volume
        if (triggerCollider != null)
        {
            triggerCollider.size = new Vector2(waterWidth, currentHeight);
            triggerCollider.offset = new Vector2(0f, currentHeight * 0.5f);
        }
    }

    private void UpdateWaveSurfaceAnimation()
    {
        if (surfaceTransform == null) return;

        // Gentle undulating wave bobbing
        float waveOffset = Mathf.Sin(Time.time * 3.5f) * 0.035f;
        surfaceTransform.localPosition = new Vector3(0f, currentHeight + waveOffset, 0f);
    }

    private void CheckPlayerSubmersion()
    {
        if (player == null)
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) player = pObj.GetComponent<PlayerMovement>();
            if (player == null)
            {
#if UNITY_2023_1_OR_NEWER
                player = Object.FindAnyObjectByType<PlayerMovement>();
#else
                player = Object.FindObjectOfType<PlayerMovement>();
#endif
            }
        }

        if (player == null || player.IsDead)
        {
            isPlayerSubmerged = false;
            return;
        }

        Vector3 pPos = player.transform.position;
        float minX = waterCenterX - waterWidth * 0.5f;
        float maxX = waterCenterX + waterWidth * 0.5f;

        // Player is horizontally inside the water column and vertically below the water surface
        bool inHorizontalBounds = pPos.x >= minX && pPos.x <= maxX;
        bool inVerticalBounds = pPos.y < (currentSurfaceY + 0.15f) && pPos.y >= (startY - 0.5f);

        if (inHorizontalBounds && inVerticalBounds)
        {
            isPlayerSubmerged = true;
            ApplyWaterBuoyancy(player);

            // Periodic damage: lose 1 heart every second while submerged
            if (Time.time >= nextDamageTime)
            {
                nextDamageTime = Time.time + damageInterval;
                ApplyWaterDamage(player);
            }
        }
        else
        {
            isPlayerSubmerged = false;
        }
    }

    private void ApplyWaterBuoyancy(PlayerMovement p)
    {
        var rb = p.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Dampen severe downward plunging speed (water drag/buoyancy)
            if (rb.linearVelocity.y < -3.5f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.95f, -3.0f);
            }
        }
    }

    private void ApplyWaterDamage(PlayerMovement p)
    {
        if (p == null || p.IsDead) return;

        var damageable = p.GetComponent<IDamageable>();
        if (damageable != null)
        {
            Vector2 upwardBump = new Vector2(0f, buoyancyKnockback);
            damageable.TakeDamage(damagePerTick, upwardBump);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayHurt();
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            if (player == null) player = other.GetComponent<PlayerMovement>();
            if (player != null && !player.IsDead)
            {
                // Submersion check
                if (player.transform.position.y < (currentSurfaceY + 0.15f))
                {
                    isPlayerSubmerged = true;
                    if (Time.time >= nextDamageTime)
                    {
                        nextDamageTime = Time.time + damageInterval;
                        ApplyWaterDamage(player);
                    }
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            isPlayerSubmerged = false;
        }
    }

    // =========================================================================
    // PROCEDURAL TEXTURES & SPRITES
    // =========================================================================

    private Sprite CreateGradientWaterSprite()
    {
        const int width = 32;
        const int height = 64;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            float t = (float)y / (height - 1);
            // Smooth gradient from deep ocean blue at bottom to crystal blue near top
            Color col = Color.Lerp(deepWaterColor, shallowWaterColor, t);

            // Subtle vertical caustics ripple
            for (int x = 0; x < width; x++)
            {
                float ripple = Mathf.Sin((x + y * 0.5f) * 0.8f) * 0.02f;
                Color pixelCol = new Color(
                    Mathf.Clamp01(col.r + ripple),
                    Mathf.Clamp01(col.g + ripple * 1.5f),
                    Mathf.Clamp01(col.b + ripple),
                    col.a
                );
                tex.SetPixel(x, y, pixelCol);
            }
        }
        tex.Apply();

        // 32 pixels wide at 32 PPU = exactly 1.0 unit unscaled width.
        // Pivot at bottom center (0.5, 0)
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), 32f);
    }

    private Sprite CreateSurfaceWaveSprite()
    {
        const int width = 64;
        const int height = 16;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            float normY = (float)y / (height - 1);
            for (int x = 0; x < width; x++)
            {
                float normX = (float)x / (width - 1);
                // Sine wave crest
                float wave = Mathf.Sin(normX * Mathf.PI * 4f) * 0.25f + 0.65f;
                if (normY <= wave)
                {
                    // Foam gradient
                    float alpha = Mathf.Lerp(surfaceFoamColor.a, 0f, (wave - normY) / wave);
                    tex.SetPixel(x, y, new Color(surfaceFoamColor.r, surfaceFoamColor.g, surfaceFoamColor.b, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();

        // 64 pixels wide at 64 PPU = exactly 1.0 unit unscaled width, matching the water body!
        // Pivot at center (0.5, 0.5)
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.7f, 1f, 0.35f);
        Vector3 center = new Vector3(waterCenterX, (startY + targetY) * 0.5f, 0f);
        Vector3 size = new Vector3(waterWidth, Mathf.Abs(targetY - startY), 0.1f);
        Gizmos.DrawCube(center, size);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(waterCenterX - waterWidth * 0.5f, targetY, 0f), new Vector3(waterCenterX + waterWidth * 0.5f, targetY, 0f));
    }
}
