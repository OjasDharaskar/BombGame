using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameHUD : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private PlayerMovement player;

    [Header("UI Canvas Components")]
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private RectTransform healthBarRoot;
    [SerializeField] private Image healthBarImage;
    [SerializeField] private Image[] heartImages;
    [SerializeField] private RectTransform bombBarRoot;
    [SerializeField] private Image bombBarImage;

    [Header("Sprite Assets")]
    [SerializeField] private Sprite healthBarSprite;
    [SerializeField] private Sprite heartSprite;
    [SerializeField] private Sprite[] chargingSprites;
    [SerializeField] private Sprite[] fullSprites;
    [SerializeField] private Sprite closingSprite;

    [Header("HUD Settings")]
    [SerializeField] private float initialShowcaseDuration = 2.0f;
    [SerializeField] private Vector2 healthBarPos = new Vector2(24f, -20f);
    [SerializeField] private Vector2 healthBarSize = new Vector2(308f, 124f);
    [SerializeField] private Vector2 bombBarPos = new Vector2(95f, -154f);
    [SerializeField] private Vector2 bombBarSize = new Vector2(117f, 27f);

    public static GameHUD Instance { get; private set; }

    private string bannerMessage = "";
    private float bannerTimer = 0f;

    public void ShowMessage(string message, float duration = 2.5f)
    {
        bannerMessage = message;
        bannerTimer = duration;
    }

    private Coroutine bombBarRoutine;
    private int lastHealth = -1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        FindPlayer();
        EnsureSpritesLoaded();
        EnsureUIReferences();
        BuildUIIfNeeded();
    }

    private void Start()
    {
        if (Instance == null) Instance = this;
        FindPlayer();
        EnsureUIReferences();

        if (player != null)
        {
            player.OnHealthChanged += HandleHealthChanged;
            player.OnBombThrown += HandleBombThrown;
            UpdateHearts(player.CurrentHealth);
        }
        else
        {
            UpdateHearts(3);
        }

        // Showcase the bomb bar charging and then disappearing on start
        ShowBombBarCharge(initialShowcaseDuration);
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.OnHealthChanged -= HandleHealthChanged;
            player.OnBombThrown -= HandleBombThrown;
        }
    }

    private void Update()
    {
        // Fail-safe player lookup
        if (player == null)
        {
            if (Instance == null) Instance = this;
        FindPlayer();
            if (player != null)
            {
                player.OnHealthChanged += HandleHealthChanged;
                player.OnBombThrown += HandleBombThrown;
                UpdateHearts(player.CurrentHealth);
            }
        }
        else if (player.CurrentHealth != lastHealth)
        {
            UpdateHearts(player.CurrentHealth);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Debug testing hotkeys:
        // H = take 1 damage (hearts reduce)
        // T = heal 1 health
        // B = trigger bomb bar charge and disappear
        if (Input.GetKeyDown(KeyCode.H) && player != null)
        {
            player.TakeDamage(1, Vector2.zero);
        }
        else if (Input.GetKeyDown(KeyCode.T) && player != null)
        {
            player.Heal(1);
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            ShowBombBarCharge(2.5f);
        }
#endif
    }

    private void OnGUI()
    {
        if (bannerTimer > 0f)
        {
            bannerTimer -= Time.deltaTime;
            if (bannerTimer <= 0f) bannerMessage = "";
        }

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string levelTitle = "LEVEL 1";
        if (sceneName.Contains("3") || sceneName.Equals("Level3", System.StringComparison.OrdinalIgnoreCase))
            levelTitle = "LEVEL 3";
        else if (sceneName.Contains("2") || sceneName.Equals("Level2", System.StringComparison.OrdinalIgnoreCase) || sceneName.Equals("Leve2", System.StringComparison.OrdinalIgnoreCase))
            levelTitle = "LEVEL 2";

        int aliveEnemies = EnemyAI.GetAliveEnemyCount();
        string statusText = aliveEnemies > 0 
            ? $"{levelTitle}  |  ENEMIES: {aliveEnemies}" 
            : $"{levelTitle}  |  DOOR OPEN! [PRESS X]";

        float screenW = Screen.width;
        float headerW = 320f;
        float headerH = 34f;
        Rect headerRect = new Rect(screenW - headerW - 20f, 16f, headerW, headerH);

        GUIStyle headerBg = new GUIStyle(GUI.skin.box);
        Color oldColor = GUI.color;
        GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
        GUI.Box(headerRect, GUIContent.none, headerBg);
        GUI.color = oldColor;

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 15;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.normal.textColor = aliveEnemies > 0 
            ? new Color(1f, 0.85f, 0.3f, 1f) 
            : new Color(0.3f, 1f, 0.4f, 1f);

        GUI.Label(headerRect, statusText, headerStyle);

        if (!string.IsNullOrEmpty(bannerMessage) && bannerTimer > 0f)
        {
            float bannerW = Mathf.Min(600f, screenW * 0.9f);
            float bannerH = 50f;
            Rect bannerRect = new Rect((screenW - bannerW) * 0.5f, 75f, bannerW, bannerH);

            GUI.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);
            GUI.Box(bannerRect, GUIContent.none, headerBg);
            GUI.color = oldColor;

            GUIStyle bannerStyle = new GUIStyle(GUI.skin.label);
            bannerStyle.fontSize = 16;
            bannerStyle.fontStyle = FontStyle.Bold;
            bannerStyle.alignment = TextAnchor.MiddleCenter;
            bannerStyle.normal.textColor = new Color(1f, 0.95f, 0.3f, 1f);

            GUI.Label(bannerRect, bannerMessage, bannerStyle);
        }
    }

    private void FindPlayer()
    {
        if (player != null) return;

        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<PlayerMovement>();
        }
        else
        {
#if UNITY_2023_1_OR_NEWER
            player = Object.FindAnyObjectByType<PlayerMovement>();
#else
            player = FindObjectOfType<PlayerMovement>();
#endif
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        UpdateHearts(current);
    }

    private void HandleBombThrown(Bomb bomb)
    {
        float duration = bomb != null ? bomb.FuseTime : 3.5f;
        ShowBombBarCharge(duration);
    }

    public void UpdateHearts(int currentHealth)
    {
        lastHealth = currentHealth;
        EnsureUIReferences();

        if (heartImages == null || heartImages.Length == 0) return;

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;

            bool isHeartActive = (i < currentHealth);

            // Actively reduce hearts by disabling/hiding lost hearts
            heartImages[i].enabled = isHeartActive;
            heartImages[i].gameObject.SetActive(isHeartActive);

            Color c = heartImages[i].color;
            c.a = isHeartActive ? 1f : 0f;
            heartImages[i].color = c;
        }
    }

    public void ShowBombBarCharge(float duration)
    {
        EnsureUIReferences();
        if (bombBarRoot == null || bombBarImage == null) return;

        if (bombBarRoutine != null)
        {
            StopCoroutine(bombBarRoutine);
        }

        bombBarRoutine = StartCoroutine(BombBarChargeRoutine(duration));
    }

    private IEnumerator BombBarChargeRoutine(float duration)
    {
        bombBarRoot.gameObject.SetActive(true);
        if (bombBarImage != null) bombBarImage.enabled = true;

        float chargeDuration = duration * 0.72f;
        float fullDuration = duration * 0.16f;
        float closeDuration = duration * 0.08f;

        // Phase 1: Charging Bar (0 to 100%)
        float elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            if (chargingSprites != null && chargingSprites.Length > 0 && bombBarImage != null)
            {
                float t = Mathf.Clamp01(elapsed / chargeDuration);
                int index = Mathf.Clamp(Mathf.FloorToInt(t * chargingSprites.Length), 0, chargingSprites.Length - 1);
                bombBarImage.sprite = chargingSprites[index];
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 2: Full (Intermittent / flashing)
        elapsed = 0f;
        while (elapsed < fullDuration)
        {
            if (fullSprites != null && fullSprites.Length > 0 && bombBarImage != null)
            {
                int fIdx = Mathf.FloorToInt(Time.time * 12f) % fullSprites.Length;
                bombBarImage.sprite = fullSprites[fIdx];
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 3: Closing Frame
        if (closingSprite != null && bombBarImage != null)
        {
            bombBarImage.sprite = closingSprite;
        }
        elapsed = 0f;
        while (elapsed < closeDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 4: Disappear!
        bombBarRoot.gameObject.SetActive(false);
        bombBarRoutine = null;
    }

    public void EnsureUIReferences()
    {
        if (hudCanvas == null)
        {
#if UNITY_2023_1_OR_NEWER
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
#else
            var canvases = FindObjectsOfType<Canvas>();
#endif
            foreach (var c in canvases)
            {
                if (c.name.Contains("HUD") || c.transform.Find("HealthBar") != null)
                {
                    hudCanvas = c;
                    break;
                }
            }
        }

        if (hudCanvas != null)
        {
            if (healthBarRoot == null)
            {
                var hb = hudCanvas.transform.Find("HealthBar");
                if (hb != null) healthBarRoot = hb.GetComponent<RectTransform>();
            }

            if (healthBarRoot != null)
            {
                if (healthBarImage == null)
                {
                    healthBarImage = healthBarRoot.GetComponent<Image>();
                }

                if (heartImages == null || heartImages.Length != 3 || heartImages[0] == null)
                {
                    heartImages = new Image[3];
                    for (int i = 0; i < 3; i++)
                    {
                        var h = healthBarRoot.Find($"Heart_{i + 1}");
                        if (h != null)
                        {
                            heartImages[i] = h.GetComponent<Image>();
                        }
                    }
                }
            }

            if (bombBarRoot == null)
            {
                var bb = hudCanvas.transform.Find("BombBar");
                if (bb != null)
                {
                    bombBarRoot = bb.GetComponent<RectTransform>();
                    bombBarImage = bb.GetComponent<Image>();
                }
            }
        }
    }

    public void BuildUIIfNeeded()
    {
        EnsureSpritesLoaded();
        EnsureUIReferences();

        if (hudCanvas == null)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            hudCanvas = canvasObj.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Health Bar Root & Image
        if (healthBarRoot == null)
        {
            Transform existing = hudCanvas.transform.Find("HealthBar");
            if (existing != null) healthBarRoot = existing.GetComponent<RectTransform>();
        }

        if (healthBarRoot == null)
        {
            GameObject hbObj = new GameObject("HealthBar");
            hbObj.transform.SetParent(hudCanvas.transform, false);
            healthBarRoot = hbObj.AddComponent<RectTransform>();

            healthBarRoot.anchorMin = new Vector2(0f, 1f);
            healthBarRoot.anchorMax = new Vector2(0f, 1f);
            healthBarRoot.pivot = new Vector2(0f, 1f);
            healthBarRoot.anchoredPosition = healthBarPos;
            healthBarRoot.sizeDelta = healthBarSize;

            healthBarImage = hbObj.AddComponent<Image>();
            healthBarImage.sprite = healthBarSprite;
            healthBarImage.preserveAspect = true;
            healthBarImage.raycastTarget = false;
        }
        else if (healthBarImage == null)
        {
            healthBarImage = healthBarRoot.GetComponent<Image>();
            if (healthBarImage != null && healthBarImage.sprite == null)
            {
                healthBarImage.sprite = healthBarSprite;
                healthBarImage.preserveAspect = true;
            }
        }

        // 3 Hearts
        if (heartImages == null || heartImages.Length != 3 || heartImages[0] == null)
        {
            heartImages = new Image[3];
            float[] xOffsets = new float[] { -53f, 0f, 53f };

            for (int i = 0; i < 3; i++)
            {
                string heartName = $"Heart_{i + 1}";
                Transform existingHeart = healthBarRoot.Find(heartName);
                GameObject heartObj;
                RectTransform heartRect;

                if (existingHeart != null)
                {
                    heartObj = existingHeart.gameObject;
                    heartRect = existingHeart.GetComponent<RectTransform>();
                }
                else
                {
                    heartObj = new GameObject(heartName);
                    heartObj.transform.SetParent(healthBarRoot, false);
                    heartRect = heartObj.AddComponent<RectTransform>();
                }

                heartRect.anchorMin = new Vector2(0.5f, 0.5f);
                heartRect.anchorMax = new Vector2(0.5f, 0.5f);
                heartRect.pivot = new Vector2(0.5f, 0.5f);
                heartRect.anchoredPosition = new Vector2(xOffsets[i], 5f);
                heartRect.sizeDelta = new Vector2(44f, 38f);

                Image img = heartObj.GetComponent<Image>();
                if (img == null) img = heartObj.AddComponent<Image>();
                img.sprite = heartSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;

                heartImages[i] = img;
            }
        }

        // Bomb Bar Root & Image
        if (bombBarRoot == null)
        {
            Transform existing = hudCanvas.transform.Find("BombBar");
            if (existing != null) bombBarRoot = existing.GetComponent<RectTransform>();
        }

        if (bombBarRoot == null)
        {
            GameObject bbObj = new GameObject("BombBar");
            bbObj.transform.SetParent(hudCanvas.transform, false);
            bombBarRoot = bbObj.AddComponent<RectTransform>();

            bombBarRoot.anchorMin = new Vector2(0f, 1f);
            bombBarRoot.anchorMax = new Vector2(0f, 1f);
            bombBarRoot.pivot = new Vector2(0f, 1f);
            bombBarRoot.anchoredPosition = bombBarPos;
            bombBarRoot.sizeDelta = bombBarSize;

            bombBarImage = bbObj.AddComponent<Image>();
            if (chargingSprites != null && chargingSprites.Length > 0)
            {
                bombBarImage.sprite = chargingSprites[0];
            }
            bombBarImage.preserveAspect = true;
            bombBarImage.raycastTarget = false;
        }
        else if (bombBarImage == null)
        {
            bombBarImage = bombBarRoot.GetComponent<Image>();
        }
    }

    public void EnsureSpritesLoaded()
    {
#if UNITY_EDITOR
        if (healthBarSprite == null)
            healthBarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/7-Objects/11-Health Bar/Health Bar.png");

        if (heartSprite == null)
            heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/7-Objects/11-Health Bar/Heart.png");

        if (chargingSprites == null || chargingSprites.Length != 11 || chargingSprites[0] == null)
        {
            chargingSprites = new Sprite[11];
            for (int i = 1; i <= 11; i++)
            {
                chargingSprites[i - 1] = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/7-Objects/3-Bomb Bar/1-Charging Bar/{i}.png");
            }
        }

        if (fullSprites == null || fullSprites.Length != 3 || fullSprites[0] == null)
        {
            fullSprites = new Sprite[3];
            for (int i = 1; i <= 3; i++)
            {
                fullSprites[i - 1] = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/7-Objects/3-Bomb Bar/2-Full (Intermitent)/{i}.png");
            }
        }

        if (closingSprite == null)
            closingSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/7-Objects/3-Bomb Bar/3-Closing/1.png");
#endif
    }
}
