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
    [SerializeField] private float playerProximityDistance = 1.3f;

    [Header("Visual & Audio Feedback")]
    [SerializeField] private Sprite closedDoorSprite;
    [SerializeField] private Sprite openDoorSprite;
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorLockedSound;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private bool isUnlocked = false;
    private bool hasTriggered = false;
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
            // Remain locked
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

        if (!silent)
        {
            if (doorOpenSound != null)
            {
                AudioSource.PlayClipAtPoint(doorOpenSound, transform.position, 1f);
            }
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.ShowMessage("ALL ENEMIES DEFEATED! DOOR IS OPEN!", 2.5f);
            }
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

        // Proximity detection fallback
        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= playerProximityDistance)
            {
                OnPlayerReachedDoor();
            }
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
            OnPlayerReachedDoor();
        }
    }

    private void OnPlayerReachedDoor()
    {
        if (hasTriggered) return;

        int aliveEnemies = EnemyAI.GetAliveEnemyCount();
        if (aliveEnemies > 0)
        {
            // Door is LOCKED: Player must defeat all enemies first!
            ShowLockedFeedback(aliveEnemies);
            return;
        }

        // All enemies are dead -> Door is unlocked! Proceed to next level
        TriggerDoorTransition();
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
            GameHUD.Instance.ShowMessage("LEVEL CLEARED!", transitionDelay + 0.5f);
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isUnlocked ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, playerProximityDistance);
    }
}
