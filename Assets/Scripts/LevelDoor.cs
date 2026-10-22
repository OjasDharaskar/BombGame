using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider2D))]
public class LevelDoor : MonoBehaviour
{
    [Header("Level Transition")]
    [Tooltip("Target scene to load. Leave empty or 'auto' to use LevelManager's sequential progression.")]
    [SerializeField] private string targetScene = "auto";
    [SerializeField] private float transitionDelay = 0.5f;
    [SerializeField] private float playerProximityDistance = 1.4f;

    [Header("Visual & Audio Feedback")]
    [SerializeField] private Sprite closedDoorSprite;
    [SerializeField] private Sprite openDoorSprite;
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorLockedSound;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private bool isUnlocked = false;
    private bool hasTriggered = false;
    private bool isPlayerInTrigger = false;
    private float lastLockedFeedbackTime = 0f;
    private Transform playerTransform;

    public bool IsUnlocked => isUnlocked;
    public bool HasTriggered => hasTriggered;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

#if UNITY_EDITOR
        if (closedDoorSprite == null)
        {
            closedDoorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/7-Objects/2-Door/1-Closed/1.png");
        }
        if (openDoorSprite == null)
        {
            openDoorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/7-Objects/2-Door/2-Opening/5.png");
        }
        if (doorOpenSound == null)
        {
            doorOpenSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Power up_v1.ogg");
            if (doorOpenSound == null) doorOpenSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Game Start_v1.ogg");
        }
        if (doorLockedSound == null)
        {
            doorLockedSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Punch_v1.ogg");
        }
#endif

        if (spriteRenderer != null && closedDoorSprite != null)
        {
            spriteRenderer.sprite = closedDoorSprite;
        }
    }

    private void Start()
    {
        FindPlayer();
        CheckEnemiesStatus(silent: true);
    }

    private void OnEnable()
    {
        EnemyAI.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        EnemyAI.OnAnyEnemyDied -= HandleEnemyDied;
    }

    private void HandleEnemyDied(EnemyAI enemy)
    {
        CheckEnemiesStatus(silent: false);
    }

    public void CheckEnemiesStatus(bool silent = false)
    {
        if (isUnlocked) return;

        int aliveCount = EnemyAI.GetAliveEnemyCount();
        if (aliveCount <= 0)
        {
            UnlockDoor(silent);
        }
        else
        {
            if (spriteRenderer != null && closedDoorSprite != null)
            {
                spriteRenderer.sprite = closedDoorSprite;
            }
        }
    }

    private void UnlockDoor(bool silent)
    {
        if (isUnlocked) return;
        isUnlocked = true;

        if (spriteRenderer != null && openDoorSprite != null)
        {
            spriteRenderer.sprite = openDoorSprite;
        }

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isLevel3 = currentScene.Equals("Level3", System.StringComparison.OrdinalIgnoreCase) || currentScene.Contains("3");
        bool isLevel4 = currentScene.Equals("Level4", System.StringComparison.OrdinalIgnoreCase) || currentScene.Contains("4");

        if (!silent)
        {
            if (doorOpenSound != null)
            {
                AudioSource.PlayClipAtPoint(doorOpenSound, transform.position, 1f);
            }
            if (GameHUD.Instance != null)
            {
                if (isLevel3)
                {
                    GameHUD.Instance.ShowMessage("ALL ENEMIES DEFEATED! LEVEL 3 CLEARED! TRANSITIONING TO LEVEL 4...", 3.0f);
                }
                else if (isLevel4)
                {
                    GameHUD.Instance.ShowMessage("ALL ENEMIES DEFEATED! DOOR IS UNLOCKED! PRESS [X] TO COMPLETE GAME", 3.5f);
                }
                else
                {
                    GameHUD.Instance.ShowMessage("ALL ENEMIES DEFEATED! DOOR IS OPEN! PRESS [X] TO ENTER", 3.0f);
                }
            }
        }

        // When finishing Level 3 (i.e. killing all enemies), automatically transition to Level 4
        if (isLevel3 && !hasTriggered)
        {
            StartCoroutine(AutoTransitionAfterEnemiesRoutine(2.0f));
        }
    }

    private IEnumerator AutoTransitionAfterEnemiesRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!hasTriggered)
        {
            TriggerDoorTransition();
        }
    }

    private void Update()
    {
        if (hasTriggered) return;

        if (playerTransform == null)
        {
            FindPlayer();
        }

        // Continual enemy check in case all enemies died
        if (!isUnlocked)
        {
            int aliveCount = EnemyAI.GetAliveEnemyCount();
            if (aliveCount <= 0)
            {
                UnlockDoor(silent: false);
            }
        }

        // Check if player is near or inside door trigger
        bool inProximity = false;
        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            inProximity = dist <= playerProximityDistance;
        }

        bool isPlayerAtDoor = isPlayerInTrigger || inProximity;

        // Player can THEN ONLY press button "X" on keyboard to move to next level
        // Else nothing happens when it is pressed
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (isPlayerAtDoor)
            {
                int aliveEnemies = EnemyAI.GetAliveEnemyCount();
                if (aliveEnemies <= 0 && isUnlocked)
                {
                    TriggerDoorTransition();
                }
                else
                {
                    ShowLockedFeedback(aliveEnemies);
                }
            }
            // If player is not at the door, pressing 'X' does nothing!
        }
    }

    private void FindPlayer()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            var pm = Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null) playerTransform = pm.transform;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            isPlayerInTrigger = true;
            if (!isUnlocked)
            {
                int aliveEnemies = EnemyAI.GetAliveEnemyCount();
                if (aliveEnemies > 0)
                {
                    ShowLockedFeedback(aliveEnemies);
                }
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            isPlayerInTrigger = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            isPlayerInTrigger = false;
        }
    }

    private void ShowLockedFeedback(int aliveEnemies)
    {
        if (Time.time - lastLockedFeedbackTime < 1.2f) return;
        lastLockedFeedbackTime = Time.time;

        if (doorLockedSound != null)
        {
            AudioSource.PlayClipAtPoint(doorLockedSound, transform.position, 0.7f);
        }

        if (GameHUD.Instance != null)
        {
            string enemyStr = aliveEnemies == 1 ? "1 enemy" : $"{aliveEnemies} enemies";
            GameHUD.Instance.ShowMessage($"DOOR LOCKED! DEFEAT ALL ENEMIES! ({enemyStr} remaining)", 2.0f);
        }
    }

    private void TriggerDoorTransition()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        if (spriteRenderer != null && openDoorSprite != null)
        {
            spriteRenderer.sprite = openDoorSprite;
        }

        if (doorOpenSound != null)
        {
            AudioSource.PlayClipAtPoint(doorOpenSound, transform.position, 1f);
        }

        if (GameHUD.Instance != null)
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene.Contains("3"))
            {
                GameHUD.Instance.ShowMessage("ENTERING LEVEL 4...", transitionDelay + 0.5f);
            }
            else if (currentScene.Contains("4"))
            {
                GameHUD.Instance.ShowMessage("GAME COMPLETED! VICTORY!", transitionDelay + 0.5f);
            }
            else
            {
                GameHUD.Instance.ShowMessage("ENTERING NEXT LEVEL...", transitionDelay + 0.5f);
            }
        }

        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        yield return new WaitForSeconds(transitionDelay);

        if (string.IsNullOrEmpty(targetScene) 
            || targetScene.Equals("auto", System.StringComparison.OrdinalIgnoreCase)
            || targetScene.Equals("Leve2", System.StringComparison.OrdinalIgnoreCase))
        {
            LevelManager.Instance.MoveToNextLevel();
        }
        else
        {
            LevelManager.Instance.LoadLevel(targetScene);
        }
    }

    private void OnGUI()
    {
        if (hasTriggered) return;

        bool inProximity = playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= playerProximityDistance;
        bool isAtDoor = isPlayerInTrigger || inProximity;

        if (isAtDoor && isUnlocked)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 worldPos = transform.position + new Vector3(0f, 1.4f, 0f);
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            if (screenPos.z > 0f)
            {
                float badgeW = 180f;
                float badgeH = 32f;
                Rect rect = new Rect(screenPos.x - badgeW * 0.5f, Screen.height - screenPos.y - badgeH * 0.5f, badgeW, badgeH);

                GUIStyle badgeBg = new GUIStyle(GUI.skin.box);
                Color oldColor = GUI.color;
                GUI.color = new Color(0.08f, 0.1f, 0.18f, 0.92f);
                GUI.Box(rect, GUIContent.none, badgeBg);
                GUI.color = oldColor;

                GUIStyle textStyle = new GUIStyle(GUI.skin.label);
                textStyle.fontSize = 13;
                textStyle.fontStyle = FontStyle.Bold;
                textStyle.alignment = TextAnchor.MiddleCenter;
                textStyle.normal.textColor = new Color(1f, 0.9f, 0.2f, 1f);

                GUI.Label(rect, "PRESS  [X]  TO ENTER", textStyle);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isUnlocked ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, playerProximityDistance);
    }
}
