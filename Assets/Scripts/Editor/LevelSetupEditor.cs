using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class LevelSetupEditor
{
    static LevelSetupEditor()
    {
        EditorApplication.delayCall += () =>
        {
            EnsureDoorPrefab();
        };
    }

    [MenuItem("TheBombGame/Ensure Door Prefab & Setup Scenes")]
    public static void SetupAll()
    {
        EnsureDoorPrefab();
        SetupSceneDoorsAndPlatforms();
    }

    public static GameObject EnsureDoorPrefab()
    {
        string prefabPath = "Assets/Prefabs/Door.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null) return existing;

        GameObject tempGO = new GameObject("Door");
        tempGO.transform.localScale = new Vector3(1.28f, 1.28f, 1.28f);

        SpriteRenderer sr = tempGO.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "MiddleGround";
        sr.sortingOrder = 5;

        Sprite closedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/7-Objects/2-Door/1-Closed/1.png");
        if (closedSprite != null) sr.sprite = closedSprite;

        BoxCollider2D col = tempGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.5f);

        tempGO.AddComponent<LevelDoor>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempGO, prefabPath);
        Object.DestroyImmediate(tempGO);
        AssetDatabase.SaveAssets();
        Debug.Log("[LevelSetupEditor] Created Door prefab at " + prefabPath);
        return prefab;
    }

    public static void SetupSceneDoorsAndPlatforms()
    {
        string currentActiveScenePath = SceneManager.GetActiveScene().path;

        // Setup Level 2
        SetupScene("Assets/Scenes/Level2.unity", scene =>
        {
            LevelDoor door = Object.FindAnyObjectByType<LevelDoor>();
            if (door == null)
            {
                LevelSetupUtility.CreateDoorGameObject(new Vector3(8.5f, -2.4f, 0f));
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("[LevelSetupEditor] Added LevelDoor to Level2.unity");
            }
        });

        // Setup Level 3
        SetupScene("Assets/Scenes/Level3.unity", scene =>
        {
            LevelDoor door = Object.FindAnyObjectByType<LevelDoor>();
            if (door == null)
            {
                LevelSetupUtility.CreateDoorGameObject(new Vector3(11.5f, 4.8f, 0f));
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("[LevelSetupEditor] Added LevelDoor to Level3.unity");
            }

            Level3PlatformController ctrl = Object.FindAnyObjectByType<Level3PlatformController>();
            if (ctrl == null)
            {
                GameObject ctrlGO = new GameObject("Level3PlatformController");
                ctrl = ctrlGO.AddComponent<Level3PlatformController>();
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("[LevelSetupEditor] Added Level3PlatformController to Level3.unity");
            }
        });

        // Setup Level 4
        SetupScene("Assets/Scenes/Level4.unity", scene =>
        {
            LevelDoor door = Object.FindAnyObjectByType<LevelDoor>();
            if (door == null)
            {
                LevelSetupUtility.CreateDoorGameObject(new Vector3(7.0f, 15.0f, 0f));
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("[LevelSetupEditor] Added LevelDoor to Level4.unity");
            }

            Level4PlatformController ctrl = Object.FindAnyObjectByType<Level4PlatformController>();
            if (ctrl == null)
            {
                GameObject ctrlGO = new GameObject("Level4PlatformController");
                ctrl = ctrlGO.AddComponent<Level4PlatformController>();
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("[LevelSetupEditor] Added Level4PlatformController to Level4.unity");
            }
        });

        // Reopen previously active scene if any
        if (!string.IsNullOrEmpty(currentActiveScenePath))
        {
            EditorSceneManager.OpenScene(currentActiveScenePath);
        }
    }

    private static void SetupScene(string scenePath, System.Action<Scene> onOpened)
    {
        if (!System.IO.File.Exists(scenePath)) return;
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        onOpened(scene);
        if (scene.isDirty)
        {
            EditorSceneManager.SaveScene(scene);
        }
    }
}
