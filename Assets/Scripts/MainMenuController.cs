using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Options")]
    [SerializeField] private string[] menuItems = new string[] { "PLAY LEVEL 1", "LEVEL 2", "QUIT GAME" };
    [SerializeField] private int selectedIndex = 0;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip navigateSound;
    [SerializeField] private AudioClip selectSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Visual Theme")]
    [SerializeField] private Color normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color titleColor = new Color(1f, 0.45f, 0.2f, 1f);

    private bool isTransitioning = false;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

#if UNITY_EDITOR
        if (navigateSound == null)
        {
            navigateSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Punch_v1.ogg");
        }
        if (selectSound == null)
        {
            selectSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Game Start_v1.ogg");
        }
#endif
    }

    private void Update()
    {
        if (isTransitioning) return;

        // Navigation using UP / DOWN arrows and W / S
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            selectedIndex = (selectedIndex - 1 + menuItems.Length) % menuItems.Length;
            PlaySound(navigateSound);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            selectedIndex = (selectedIndex + 1) % menuItems.Length;
            PlaySound(navigateSound);
        }

        // Selection using ENTER / Return / Space / KeypadEnter
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            ExecuteSelection(selectedIndex);
        }

        // Direct number keys
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            selectedIndex = 0;
            ExecuteSelection(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            selectedIndex = 1;
            ExecuteSelection(1);
        }
    }

    public void SelectOption(int index)
    {
        if (isTransitioning) return;
        selectedIndex = Mathf.Clamp(index, 0, menuItems.Length - 1);
        ExecuteSelection(selectedIndex);
    }

    private void ExecuteSelection(int index)
    {
        if (isTransitioning) return;
        PlaySound(selectSound);

        switch (index)
        {
            case 0:
                StartCoroutine(LoadLevelRoutine("Leve1"));
                break;
            case 1:
                StartCoroutine(LoadLevelRoutine("Leve2"));
                break;
            case 2:
                Debug.Log("[MainMenu] Quitting Game...");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }

    private IEnumerator LoadLevelRoutine(string sceneName)
    {
        isTransitioning = true;
        yield return new WaitForSeconds(0.35f);
        LevelManager.Instance.LoadLevel(sceneName);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, 1f);
        }
        else if (SoundManager.Instance != null && clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }
    }

    private void OnGUI()
    {
        // Custom Styled Menu Layout
        float screenW = Screen.width;
        float screenH = Screen.height;

        // Dark vignette background box
        GUIStyle darkBg = new GUIStyle(GUI.skin.box);
        darkBg.normal.background = Texture2D.whiteTexture;
        Color oldColor = GUI.color;
        GUI.color = new Color(0.12f, 0.08f, 0.08f, 0.94f);
        GUI.Box(new Rect(0, 0, screenW, screenH), GUIContent.none, darkBg);
        GUI.color = oldColor;

        // Container
        float containerW = Mathf.Min(480f, screenW * 0.9f);
        float containerH = 400f;
        float startX = (screenW - containerW) * 0.5f;
        float startY = Mathf.Max(30f, (screenH - containerH) * 0.5f - 20f);

        // Title: THE BOMB GAME
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(screenH * 0.06f), 32, 54);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = titleColor;

        // Title drop shadow
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.Label(new Rect(startX + 3f, startY + 3f, containerW, 60f), "THE BOMB GAME", titleStyle);
        GUI.color = oldColor;
        GUI.Label(new Rect(startX, startY, containerW, 60f), "THE BOMB GAME", titleStyle);

        // Subtitle
        GUIStyle subTitleStyle = new GUIStyle(GUI.skin.label);
        subTitleStyle.fontSize = 15;
        subTitleStyle.fontStyle = FontStyle.Italic;
        subTitleStyle.alignment = TextAnchor.MiddleCenter;
        subTitleStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 0.85f);
        GUI.Label(new Rect(startX, startY + 62f, containerW, 25f), "PIRATE BOMB PLATFORMER", subTitleStyle);

        // Menu items
        float menuStartY = startY + 115f;
        float itemH = 50f;
        float itemSpacing = 12f;

        for (int i = 0; i < menuItems.Length; i++)
        {
            bool isSelected = (i == selectedIndex);
            Rect itemRect = new Rect(startX + 20f, menuStartY + i * (itemH + itemSpacing), containerW - 40f, itemH);

            // Hover check
            if (itemRect.Contains(Event.current.mousePosition))
            {
                if (selectedIndex != i && Event.current.type == EventType.MouseMove)
                {
                    selectedIndex = i;
                    PlaySound(navigateSound);
                }
            }

            // Button style
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = 18;
            btnStyle.fontStyle = FontStyle.Bold;
            btnStyle.alignment = TextAnchor.MiddleCenter;

            if (isSelected)
            {
                btnStyle.normal.textColor = selectedColor;
                btnStyle.hover.textColor = selectedColor;
                // Highlight border
                GUI.color = new Color(1f, 0.85f, 0.2f, 0.35f);
                GUI.Box(new Rect(itemRect.x - 4f, itemRect.y - 4f, itemRect.width + 8f, itemRect.height + 8f), GUIContent.none);
                GUI.color = oldColor;
            }
            else
            {
                btnStyle.normal.textColor = normalColor;
            }

            string labelText = isSelected ? $">  {menuItems[i]}  <" : menuItems[i];
            if (GUI.Button(itemRect, labelText, btnStyle))
            {
                ExecuteSelection(i);
            }
        }

        // Instructions Footer: EXACT match for TheBombGame.md
        GUIStyle instrStyle = new GUIStyle(GUI.skin.label);
        instrStyle.fontSize = 16;
        instrStyle.fontStyle = FontStyle.Bold;
        instrStyle.alignment = TextAnchor.MiddleCenter;
        instrStyle.normal.textColor = new Color(0.4f, 1f, 0.5f, 1f);

        float footerY = menuStartY + menuItems.Length * (itemH + itemSpacing) + 20f;
        GUI.Label(new Rect(startX, footerY, containerW, 30f), "Use UP / DOWN and ENTER", instrStyle);

        // Controls hint
        GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = 12;
        hintStyle.alignment = TextAnchor.MiddleCenter;
        hintStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
        GUI.Label(new Rect(startX, footerY + 30f, containerW, 25f), "Move: A / D or Arrows  |  Jump: Space  |  Bomb: F / J / Click", hintStyle);
    }
}
