using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider2D))]
public class LevelDoor : MonoBehaviour
{
    [Header("Level Transition")]
    [SerializeField] private string targetScene = "Leve2";
    [SerializeField] private float transitionDelay = 0.5f;
    [SerializeField] private float playerProximityDistance = 1.3f;

    [Header("Visual & Audio Feedback")]
    [SerializeField] private Sprite openDoorSprite;
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private bool hasTriggered = false;
    private Transform playerTransform;

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
        if (openDoorSprite == null)
        {
            openDoorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/7-Objects/2-Door/2-Opening/5.png");
        }
        if (doorOpenSound == null)
        {
            doorOpenSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Power up_v1.ogg");
            if (doorOpenSound == null) doorOpenSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Game Start_v1.ogg");
        }
#endif
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (hasTriggered) return;

        if (playerTransform == null)
        {
            FindPlayer();
        }

        // Proximity detection fallback
        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= playerProximityDistance)
            {
                TriggerDoorTransition();
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
            TriggerDoorTransition();
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
            AudioSource.PlayClipAtPoint(doorOpenSound, transform.position);
        }

        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        yield return new WaitForSeconds(transitionDelay);
        LevelManager.Instance.LoadLevel(targetScene);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, playerProximityDistance);
    }
}
