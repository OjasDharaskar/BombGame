using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class LevelSetupUtility
{
    private static bool isInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (isInitialized) return;
        isInitialized = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;

        // Skip MainMenu
        if (sceneName.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // 1. Ensure LevelDoor exists in this level
        EnsureLevelDoor(sceneName);

        // 2. If Level 3, ensure Level3PlatformController exists
        if (sceneName.Equals("Level3", System.StringComparison.OrdinalIgnoreCase) || sceneName.Contains("3"))
        {
            EnsureLevel3Platforms();
        }

        // 3. If Level 4, ensure Level4PlatformController exists
        if (sceneName.Equals("Level4", System.StringComparison.OrdinalIgnoreCase) || sceneName.Contains("4"))
        {
            EnsureLevel4Platforms();
        }
    }

    public static LevelDoor EnsureLevelDoor(string sceneName)
    {
        LevelDoor door = Object.FindAnyObjectByType<LevelDoor>();
        if (door != null)
        {
            return door;
        }

        Vector3 spawnPos = Vector3.zero;
        if (sceneName.Equals("Leve1", System.StringComparison.OrdinalIgnoreCase) || sceneName.Contains("1"))
        {
            spawnPos = new Vector3(6.45f, 3.87f, 0f);
        }
        else if (sceneName.Equals("Level2", System.StringComparison.OrdinalIgnoreCase) 
                 || sceneName.Equals("Leve2", System.StringComparison.OrdinalIgnoreCase)
                 || sceneName.Contains("2"))
        {
            // Right-side platform exit in Level 2
            spawnPos = new Vector3(8.5f, -2.4f, 0f);
        }
        else if (sceneName.Equals("Level3", System.StringComparison.OrdinalIgnoreCase) || sceneName.Contains("3"))
        {
            // Top-right victory platform exit in Level 3
            spawnPos = new Vector3(11.5f, 4.8f, 0f);
        }
        else if (sceneName.Equals("Level4", System.StringComparison.OrdinalIgnoreCase) || sceneName.Contains("4"))
        {
            // Top-right exit ledge in Level 4
            spawnPos = new Vector3(7.0f, 15.0f, 0f);
        }

        return CreateDoorGameObject(spawnPos);
    }

    public static LevelDoor CreateDoorGameObject(Vector3 position)
    {
        GameObject doorGO = new GameObject("LevelDoor");
        doorGO.transform.position = position;
        doorGO.transform.localScale = new Vector3(1.28f, 1.28f, 1.28f);

        // Visual Sprite Renderer
        SpriteRenderer sr = doorGO.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "MiddleGround";
        sr.sortingOrder = 5;

        Sprite closedSprite = null;
        Sprite openSprite = null;
        AudioClip openSound = null;
        AudioClip lockSound = null;

#if UNITY_EDITOR
        closedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/7-Objects/2-Door/1-Closed/1.png");
        openSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/7-Objects/2-Door/2-Opening/5.png");
        openSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Power up_v1.ogg");
        lockSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Punch_v1.ogg");
#endif

        if (closedSprite != null)
        {
            sr.sprite = closedSprite;
        }

        // Trigger Collider
        BoxCollider2D col = doorGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.5f);

        // LevelDoor Component
        LevelDoor door = doorGO.AddComponent<LevelDoor>();
        return door;
    }

    public static void EnsureLevel3Platforms()
    {
        Level3PlatformController ctrl = Object.FindAnyObjectByType<Level3PlatformController>();
        if (ctrl == null)
        {
            GameObject ctrlGO = new GameObject("Level3PlatformController");
            ctrl = ctrlGO.AddComponent<Level3PlatformController>();
        }
    }

    public static void EnsureLevel4Platforms()
    {
        Level4PlatformController ctrl = Object.FindAnyObjectByType<Level4PlatformController>();
        if (ctrl == null)
        {
            GameObject ctrlGO = new GameObject("Level4PlatformController");
            ctrl = ctrlGO.AddComponent<Level4PlatformController>();
        }
    }
}
