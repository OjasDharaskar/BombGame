using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Level4PlatformController : MonoBehaviour
{
    [Header("Platform Visual Assets")]
    [SerializeField] private Sprite platformTileSprite;

    [Header("Speed & Distance Settings (Fast & Further)")]
    [Tooltip("Fast speed for the platform moving up and down")]
    [SerializeField] private float fastSpeedY = 6.0f;
    [Tooltip("Distance to travel vertically down into the shaft and back up")]
    [SerializeField] private float verticalTravelDistance = 13.5f;
    [Tooltip("Pause duration at turnaround points (top and bottom) in seconds")]
    [SerializeField] private float pauseAtEnds = 0.25f;

    [Header("Spawned Platform (Read-Only)")]
    [SerializeField] private MovingPlatform platformShaft;

    // Tile coordinates in Foreground tilemap of the platform in Level 4
    private static readonly Vector3Int[] staticPlatformTileCoords = new Vector3Int[]
    {
        new Vector3Int(2, 14, 0),
        new Vector3Int(3, 14, 0),
        new Vector3Int(4, 14, 0)
    };

    private void Awake()
    {
        // Enforce: ONLY execute in Level 4 scene
        string sceneName = SceneManager.GetActiveScene().name;
        if (!sceneName.Contains("4") && !sceneName.Equals("Level4", System.StringComparison.OrdinalIgnoreCase))
        {
            enabled = false;
            return;
        }

        EnsureSpriteLoaded();
        ClearStaticPlatformTiles();
        SetupMovingPlatform();
    }

    private void EnsureSpriteLoaded()
    {
        // Extract sprite from Foreground tilemap before clearing
        Tilemap fg = FindForegroundTilemap();
        if (fg != null)
        {
            foreach (var coord in staticPlatformTileCoords)
            {
                TileBase tb = fg.GetTile(coord);
                if (tb is Tile t && t.sprite != null)
                {
                    platformTileSprite = t.sprite;
                    break;
                }
            }
        }

#if UNITY_EDITOR
        if (platformTileSprite == null)
        {
            // Fallback: load platform tile sprite from Tile-Sets (64-64).png
            Object[] allSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/8-Tile-Sets/Tile-Sets (64-64).png");
            foreach (var obj in allSprites)
            {
                if (obj is Sprite s && (s.name == "Tile-Sets (64-64)_23" || s.name == "Tile-Sets (64-64)_22"))
                {
                    platformTileSprite = s;
                    break;
                }
            }
        }
#endif
    }

    public void ClearStaticPlatformTiles()
    {
        Tilemap fg = FindForegroundTilemap();
        if (fg == null) return;

        foreach (var coord in staticPlatformTileCoords)
        {
            fg.SetTile(coord, null);
        }
        fg.RefreshAllTiles();
    }

    private Tilemap FindForegroundTilemap()
    {
        Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude);
        foreach (var tm in tilemaps)
        {
            if (tm.gameObject.name.Equals("Foreground", System.StringComparison.OrdinalIgnoreCase))
            {
                return tm;
            }
        }
        return tilemaps.Length > 0 ? tilemaps[0] : null;
    }

    public void SetupMovingPlatform()
    {
        // Check if platform already exists
        var existing = Object.FindObjectsByType<MovingPlatform>(FindObjectsInactive.Exclude);
        foreach (var p in existing)
        {
            if (p.name.Contains("Shaft") || p.name.Contains("Level4"))
            {
                platformShaft = p;
                break;
            }
        }

        if (platformShaft == null)
        {
            // Shaft platform: 3 blocks wide, moves DOWN and UP very fast and further
            platformShaft = CreatePlatform(
                name: "MovingPlatform_Shaft",
                position: new Vector3(3.0f, 14.15f, 0f),
                blockCount: 3,
                direction: Vector2.down,
                distance: verticalTravelDistance,
                speed: fastSpeedY,
                waitTime: pauseAtEnds
            );
        }
    }

    private MovingPlatform CreatePlatform(string name, Vector3 position, int blockCount, Vector2 direction, float distance, float speed, float waitTime)
    {
        GameObject platformGO = new GameObject(name);
        platformGO.transform.position = position;

        // Ground Layer (Layer 6 is Ground in project TagManager, fallback to 0)
        int groundLayer = LayerMask.NameToLayer("Ground");
        platformGO.layer = groundLayer != -1 ? groundLayer : 0;

        // Visual Sprite Blocks
        float startX = -(blockCount - 1) * 0.5f;
        for (int i = 0; i < blockCount; i++)
        {
            GameObject blockGO = new GameObject($"Block_{i}");
            blockGO.transform.SetParent(platformGO.transform, false);
            blockGO.transform.localPosition = new Vector3(startX + i, 0f, 0f);

            SpriteRenderer sr = blockGO.AddComponent<SpriteRenderer>();
            sr.sprite = platformTileSprite;
            sr.sortingLayerName = "MiddleGround";
            sr.sortingOrder = 2;
        }

        // Solid BoxCollider2D for player and enemies to stand on
        BoxCollider2D col = platformGO.AddComponent<BoxCollider2D>();
        col.size = new Vector2(blockCount * 0.98f, 0.45f);
        col.offset = new Vector2(0f, 0.15f);

        // Rigidbody2D Kinematic for robust, jitter-free platform physics
        Rigidbody2D rb = platformGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // MovingPlatform Controller (up and down vertically)
        MovingPlatform mp = platformGO.AddComponent<MovingPlatform>();
        mp.Initialize(direction, distance, speed, waitTime);

        return mp;
    }
}
