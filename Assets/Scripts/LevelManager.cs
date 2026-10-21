using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    private static LevelManager instance;
    public static LevelManager Instance
    {
        get
        {
            if (instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                instance = Object.FindAnyObjectByType<LevelManager>();
#else
                instance = Object.FindObjectOfType<LevelManager>();
#endif
                if (instance == null)
                {
                    GameObject go = new GameObject("LevelManager");
                    instance = go.AddComponent<LevelManager>();
                }
            }
            return instance;
        }
    }

    [Header("Scenes Configuration")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string level1Scene = "Leve1";
    [SerializeField] private string level2Scene = "Level2";
    [SerializeField] private string level3Scene = "Level3";

    [Header("Settings")]
    [SerializeField] private float quickRestartDelay = 0.35f;

    private bool isTransitioning = false;

    public string MainMenuScene => mainMenuScene;
    public string Level1Scene => level1Scene;
    public string Level2Scene => level2Scene;
    public string Level3Scene => level3Scene;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartGame()
    {
        LoadLevel1();
    }

    public void LoadMainMenu()
    {
        LoadScene(mainMenuScene);
    }

    public void LoadLevel1()
    {
        LoadScene(level1Scene);
    }

    public void LoadLevel2()
    {
        LoadScene(level2Scene);
    }

    public void LoadLevel3()
    {
        LoadScene(level3Scene);
    }

    public void LoadLevel(string sceneName)
    {
        LoadScene(sceneName);
    }

    public void MoveToNextLevel()
    {
        string current = SceneManager.GetActiveScene().name;
        if (current.Equals(level1Scene, System.StringComparison.OrdinalIgnoreCase) || current.Contains("1"))
        {
            LoadLevel2();
        }
        else if (current.Equals(level2Scene, System.StringComparison.OrdinalIgnoreCase) 
                 || current.Equals("Leve2", System.StringComparison.OrdinalIgnoreCase)
                 || current.Contains("2"))
        {
            LoadLevel3();
        }
        else if (current.Equals(level3Scene, System.StringComparison.OrdinalIgnoreCase) || current.Contains("3"))
        {
            // Level 3 is the final level - trigger Game Complete victory!
            GameComplete();
        }
        else
        {
            LoadLevel1();
        }
    }

    public void GameComplete()
    {
        if (isTransitioning) return;
        StartCoroutine(GameCompleteRoutine());
    }

    private IEnumerator GameCompleteRoutine()
    {
        isTransitioning = true;
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.ShowMessage("VICTORY! ALL LEVELS COMPLETED!", 3.5f);
        }
        yield return new WaitForSeconds(3.0f);
        SceneManager.LoadScene(mainMenuScene);
        isTransitioning = false;
    }

    public void RestartCurrentLevel()
    {
        string current = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(current))
        {
            SceneManager.LoadScene(current);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void RestartCurrentLevelDelayed(float delay = -1f)
    {
        if (delay < 0f) delay = quickRestartDelay;
        StartCoroutine(RestartDelayedRoutine(delay));
    }

    private IEnumerator RestartDelayedRoutine(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }
        RestartCurrentLevel();
    }

    private void LoadScene(string sceneName)
    {
        if (isTransitioning) return;
        string resolved = ResolveSceneName(sceneName);
        StartCoroutine(LoadSceneRoutine(resolved));
    }

    private string ResolveSceneName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return level1Scene;
        if (sceneName.Equals("Leve2", System.StringComparison.OrdinalIgnoreCase))
        {
            return "Level2";
        }
        return sceneName;
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isTransitioning = true;
        yield return null;
        SceneManager.LoadScene(sceneName);
        isTransitioning = false;
    }
}
