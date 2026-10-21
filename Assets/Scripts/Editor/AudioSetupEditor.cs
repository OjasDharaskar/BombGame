#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AudioSetupEditor
{
    static AudioSetupEditor()
    {
        EditorApplication.delayCall += SetupAudioSettings;
    }

    [MenuItem("Tools/Setup Audio and Scene Settings")]
    public static void SetupAudioSettings()
    {
        SetupBuildSettings();
        SetupPlayerPrefab();
        SetupBombPrefab();
        SetupSceneSoundManager();
        AssetDatabase.SaveAssets();
        Debug.Log("[AudioSetup] Audio clips and Scene Build Settings configured successfully!");
    }

    public static void SetupBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        bool hasMainMenu = false;
        bool hasLevel1 = false;
        bool hasLevel2 = false;

        foreach (var s in scenes)
        {
            if (s.path.Contains("MainMenu")) hasMainMenu = true;
            if (s.path.Contains("Leve1")) hasLevel1 = true;
            if (s.path.Contains("Leve2")) hasLevel2 = true;
        }

        if (!hasMainMenu || !hasLevel1 || !hasLevel2)
        {
            var newScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Leve1.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Leve2.unity", true)
            };
            EditorBuildSettings.scenes = newScenes;
            Debug.Log("[AudioSetup] Configured EditorBuildSettings with MainMenu.unity, Leve1.unity and Leve2.unity.");
        }
    }

    public static void SetupPlayerPrefab()
    {
        string prefabPath = "Assets/Prefabs/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (playerPrefab == null) return;

        PlayerMovement playerMovement = playerPrefab.GetComponent<PlayerMovement>();
        if (playerMovement == null) return;

        SerializedObject so = new SerializedObject(playerMovement);
        so.Update();

        AudioClip walkClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Gravel.ogg");
        if (walkClip == null) walkClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Gravel.mp3");
        AudioClip jumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Jump_v1.ogg");
        AudioClip hurtClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Hurt_v1.ogg");
        AudioClip deathClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Death_v1.ogg");
        AudioClip fallClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Fall_v1.ogg");

        SetClipProperty(so, "walkSound", walkClip);
        SetClipProperty(so, "jumpSound", jumpClip);
        SetClipProperty(so, "hurtSound", hurtClip);
        SetClipProperty(so, "deathSound", deathClip);
        SetClipProperty(so, "fallSound", fallClip);

        so.ApplyModifiedProperties();
        PrefabUtility.SavePrefabAsset(playerPrefab);
        Debug.Log("[AudioSetup] Player.prefab audio mapping updated.");
    }

    public static void SetupBombPrefab()
    {
        string prefabPath = "Assets/Prefabs/Bomb.prefab";
        GameObject bombPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (bombPrefab == null) return;

        Bomb bomb = bombPrefab.GetComponent<Bomb>();
        if (bomb == null) return;

        SerializedObject so = new SerializedObject(bomb);
        so.Update();

        AudioClip explosionClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Explosion_v1.ogg");
        SetClipProperty(so, "explosionSound", explosionClip);

        var playAudioOnInitiatedProp = so.FindProperty("playAudioOnInitiated");
        if (playAudioOnInitiatedProp != null)
        {
            playAudioOnInitiatedProp.boolValue = true;
        }

        so.ApplyModifiedProperties();
        PrefabUtility.SavePrefabAsset(bombPrefab);
        Debug.Log("[AudioSetup] Bomb.prefab audio mapping updated.");
    }

    public static void SetupSceneSoundManager()
    {
#if UNITY_2023_1_OR_NEWER
        var audioMgr = Object.FindAnyObjectByType<SoundManager>();
#else
        var audioMgr = Object.FindObjectOfType<SoundManager>();
#endif
        if (audioMgr == null)
        {
            GameObject go = new GameObject("SoundManager");
            audioMgr = go.AddComponent<SoundManager>();
        }

        audioMgr.EnsureClipsLoaded();
        EditorUtility.SetDirty(audioMgr);
    }

    private static void SetClipProperty(SerializedObject so, string propertyName, AudioClip clip)
    {
        var prop = so.FindProperty(propertyName);
        if (prop != null && clip != null)
        {
            prop.objectReferenceValue = clip;
        }
    }
}
#endif
