using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Level3PlatformController : MonoBehaviour
{
    [Header("Platform Visual Assets")]
    [SerializeField] private Sprite platformTileSprite;

    [Header("Speed Settings (Medium Speed)")]
    [SerializeField] private float mediumSpeedX = 2.2f;
    [SerializeField] private float mediumSpeedY = 2.0f;

    [Header("Platform Range / Distance Settings")]
    [SerializeField] private float platformLeftDistance = 2.6f;
    [SerializeField] private float platformBottomRightDistance = 3.2f;
    [SerializeField] private float platformTopRightDistance = 5.2f;

    [Header("Spawned Platforms (Read-Only)")]
    [SerializeField] private MovingPlatform platformLeft;
    [SerializeField] private MovingPlatform platformBottomRight;
    [SerializeField] private MovingPlatform platformTopRight;

    // Tile coordinates in Foreground tilemap of the 3 platforms in Level 3
    private static readonly Vector3Int[] staticPlatformTileCoords = new Vector3Int[]
    {
        // Platform 1 (Left, 3 tiles)
        new Vector3Int(-5, -1, 0),
        new Vector3Int(-4, -1, 0),
        new Vector3Int(-3, -1, 0),
        // Platform 2 (Bottom-Right, 2 tiles)
        new Vector3Int(2, -2, 0),
        new Vector3Int(3, -2, 0),
        // Platform 3 (Top-Right, 2 tiles)
        new Vector3Int(3, 3, 0),
        new Vector3Int(4, 3, 0)
    };

    private void Awake()
    {
        // Enforce: ONLY execute in Level 3 scene
        string sceneName = SceneManager.GetActiveScene().name;
        if (!sceneName.Contains("3") && !sceneName.Equals("Level3", System.StringComparison.OrdinalIgnoreCase))
        {
            enabled = false;
            return;
        }

        EnsureSpriteLoaded();
        ClearStaticPlatformTiles();
        SetupMovingPlatforms();
    }

    private void EnsureSpriteLoaded()
    {
#if UNITY_EDITOR
        if (platformTileSprite == null)
        {
            // Load the exact platform sprite Tile-Sets (64-64)_22
            Object[] allSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/8-Tile-Sets/Tile-Sets (64-64).png");
            foreach (var obj in allSprites)
            {
                if (obj is Sprite s && s.name == "Tile-Sets (64-64)_22")
                {
                    platformTileSprite = s;
                    break;
                }
            }
        }
#endif
        if (platformTileSprite == null)
        {
            // Fallback: extract sprite from Foreground tilemap if available
            Tilemap fg = FindForegroundTilemap();
            if (fg != null)
            {
                TileBase tb = fg.GetTile(new Vector3Int(-4, -1, 0));
                if (tb is Tile t && t.sprite != null)
                {
                    platformTileSprite = t.sprite;
                }
            }
        }
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

    public void SetupMovingPlatforms()
    {
        // Check if platforms already exist
        var existing = FindObjectsByType<MovingPlatform>(FindObjectsInactive.Exclude);
        foreach (var p in existing)
        {
            if (p.name.Contains("Left")) platformLeft = p;
            else if (p.name.Contains("BottomRight")) platformBottomRight = p;
            else if (p.name.Contains("TopRight")) platformTopRight = p;
        }

        // Platform 1: Left platform (3 blocks wide, moves in X direction)
        if (platformLeft == null)
        {
            platformLeft = CreatePlatform(
                name: "MovingPlatform_Left",
                position: new Vector3(-3.45f, -1.23f, 0f),
                blockCount: 3,
                direction: Vector2.right,
                distance: platformLeftDistance,
                speed: mediumSpeedX
            );
        }
        else
        {
            platformLeft.Initialize(Vector2.right, platformLeftDistance, mediumSpeedX);
        }

        // Platform 2: Bottom-Right platform (2 blocks wide, moves in Y direction)
        if (platformBottomRight == null)
        {
            platformBottomRight = CreatePlatform(
                name: "MovingPlatform_BottomRight",
                position: new Vector3(3.05f, -2.23f, 0f),
                blockCount: 2,
                direction: Vector2.up,
                distance: platformBottomRightDistance,
                speed: mediumSpeedY
            );
        }
        else
        {
            platformBottomRight.Initialize(Vector2.up, platformBottomRightDistance, mediumSpeedY);
        }

        // Platform 3: Top-Right platform (2 blocks wide, moves in X direction)
        if (platformTopRight == null)
        {
            platformTopRight = CreatePlatform(
                name: "MovingPlatform_TopRight",
                position: new Vector3(4.05f, 2.77f, 0f),
                blockCount: 2,
                direction: Vector2.left,
                distance: platformTopRightDistance,
                speed: mediumSpeedX
            );
        }
        else
        {
            platformTopRight.Initialize(Vector2.left, platformTopRightDistance, mediumSpeedX);
        }
    }

    private MovingPlatform CreatePlatform(string name, Vector3 position, int blockCount, Vector2 direction, float distance, float speed)
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
            // Sorting Layer: MiddleGround or Player, order in layer: 2
            sr.sortingLayerName = "MiddleGround";
            sr.sortingOrder = 2;
        }

        // Solid Collider for standing on
        BoxCollider2D col = platformGO.AddComponent<BoxCollider2D>();
        col.size = new Vector2(blockCount * 0.98f, 0.45f);
        col.offset = new Vector2(0f, 0.15f);

        // Rigidbody2D Kinematic
        Rigidbody2D rb = platformGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // MovingPlatform Controller
        MovingPlatform mp = platformGO.AddComponent<MovingPlatform>();
        mp.Initialize(direction, distance, speed);

        return mp;
    }
}
