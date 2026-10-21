#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HUDSetupEditor
{
    [MenuItem("Tools/Setup Bomb Game HUD")]
    public static void SetupHUD()
    {
#if UNITY_2023_1_OR_NEWER
        var hud = Object.FindAnyObjectByType<GameHUD>();
#else
        var hud = Object.FindObjectOfType<GameHUD>();
#endif
        if (hud == null)
        {
            var camera = Camera.main;
            if (camera != null)
            {
                hud = camera.gameObject.AddComponent<GameHUD>();
            }
            else
            {
                var hudObj = new GameObject("HUD");
                hud = hudObj.AddComponent<GameHUD>();
            }
        }

        hud.EnsureSpritesLoaded();
        hud.BuildUIIfNeeded();
        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        Debug.Log("[HUDSetup] Successfully configured HUD Canvas with Health Bar and Bomb Bar!");
    }
}
#endif
