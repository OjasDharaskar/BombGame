using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Movement Configuration")]
    [Tooltip("Movement direction vector (e.g. (1, 0) for X, (0, 1) for Y)")]
    [SerializeField] private Vector2 moveDirection = Vector2.right;
    [Tooltip("Distance to travel from start position before turning back")]
    [SerializeField] private float moveDistance = 2.5f;
    [Tooltip("Speed of the platform (medium speed recommended: 2.0 - 2.5)")]
    [SerializeField] private float moveSpeed = 2.2f;
    [Tooltip("Pause duration at each end of the movement in seconds")]
    [SerializeField] private float waitTimeAtEnds = 0.15f;

    [Header("Level Restriction")]
    [Tooltip("Enforce that moving platform logic only executes in levels with moving platforms (Level 3, Level 4)")]
    [SerializeField] private bool onlyActiveInMovingPlatformLevels = true;

    // Movement state
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private bool movingToTarget = true;
    private float waitTimer = 0f;
    private Rigidbody2D rb;
    private Collider2D col;

    // Rider management (smoothly moving characters standing on top)
    private readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();
    private readonly List<Rigidbody2D> ridersToRemove = new List<Rigidbody2D>();

    public Vector2 MoveDirection
    {
        get => moveDirection;
        set => moveDirection = value;
    }

    public float MoveDistance
    {
        get => moveDistance;
        set => moveDistance = value;
    }

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Ensure Rigidbody2D is Kinematic for reliable, jitter-free platform movement
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        startPosition = rb.position;
        targetPosition = startPosition + moveDirection.normalized * moveDistance;

        // Restriction check: only active in Level 3 and Level 4
        if (onlyActiveInMovingPlatformLevels)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            bool isAllowed = currentScene.Contains("3") || currentScene.Contains("4");
            if (!isAllowed)
            {
                enabled = false;
            }
        }
    }

    public void Initialize(Vector2 direction, float distance, float speed, float waitTime = 0.15f)
    {
        moveDirection = direction;
        moveDistance = distance;
        moveSpeed = speed;
        waitTimeAtEnds = waitTime;
        startPosition = transform.position;
        targetPosition = startPosition + moveDirection.normalized * moveDistance;
        movingToTarget = true;
        waitTimer = 0f;
    }

    private void FixedUpdate()
    {
        if (onlyActiveInMovingPlatformLevels)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            bool isAllowed = currentScene.Contains("3") || currentScene.Contains("4");
            if (!isAllowed) return;
        }

        Vector2 currentPos = rb.position;

        // Handle wait time at turnaround points
        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector2 destination = movingToTarget ? targetPosition : startPosition;
        Vector2 nextPos = Vector2.MoveTowards(currentPos, destination, moveSpeed * Time.fixedDeltaTime);
        Vector2 delta = nextPos - currentPos;

        // Move the platform
        rb.MovePosition(nextPos);

        // Move any riders standing on the platform by the exact same delta
        MoveRiders(delta);

        // Check if reached destination
        if (Vector2.Distance(nextPos, destination) <= 0.001f)
        {
            movingToTarget = !movingToTarget;
            waitTimer = waitTimeAtEnds;
        }
    }

    private void MoveRiders(Vector2 delta)
    {
        if (riders.Count == 0) return;

        ridersToRemove.Clear();
        foreach (var rider in riders)
        {
            if (rider == null || !rider.gameObject.activeInHierarchy)
            {
                ridersToRemove.Add(rider);
                continue;
            }

            // Carry rider along with the platform movement
            rider.position += delta;
        }

        for (int i = 0; i < ridersToRemove.Count; i++)
        {
            riders.Remove(ridersToRemove[i]);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryAddRider(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryAddRider(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Rigidbody2D riderRb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (riderRb != null && riders.Contains(riderRb))
        {
            riders.Remove(riderRb);
        }
    }

    private void TryAddRider(Collision2D collision)
    {
        // Only accept objects that are landing on TOP of the platform (normal pointing downwards from contact)
        bool isFromAbove = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            // Normal points from other collider to platform, so if platform is below, normal.y < -0.4f
            if (contact.normal.y < -0.4f)
            {
                isFromAbove = true;
                break;
            }
        }

        if (isFromAbove)
        {
            Rigidbody2D riderRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (riderRb != null && !riders.Contains(riderRb))
            {
                riders.Add(riderRb);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 start = Application.isPlaying ? (Vector3)startPosition : transform.position;
        Vector3 end = start + (Vector3)(moveDirection.normalized * moveDistance);
        Gizmos.DrawLine(start, end);

        Collider2D c = GetComponent<Collider2D>();
        Vector3 boxSize = c != null ? (Vector3)c.bounds.size : Vector3.one;
        Gizmos.DrawWireCube(start, boxSize);
        Gizmos.DrawWireCube(end, boxSize);
    }
}
