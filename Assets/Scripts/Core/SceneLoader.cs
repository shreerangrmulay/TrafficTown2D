using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrafficTown2D.Core
{
    public sealed class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        public const string MainMenuSceneName = "MainMenu";
        public const string FirstLevelSceneName = "Level1";
        public const string SecondLevelSceneName = "Level2";
        public const string ThirdLevelSceneName = "Level3";
        public const string FourthLevelSceneName = "Level4";
        public const string FifthLevelSceneName = "Level5";
        public const string SixthLevelSceneName = "Level6";
        public const string SeventhLevelSceneName = "Level7";
        public const string EighthLevelSceneName = "Level8";
        public const string NinthLevelSceneName = "Level9";
        public const string TenthLevelSceneName = "Level10";

        public const int TotalLevelCount = 10;

        private static readonly string[] LevelSceneNames =
        {
            FirstLevelSceneName, SecondLevelSceneName, ThirdLevelSceneName,
            FourthLevelSceneName, FifthLevelSceneName, SixthLevelSceneName,
            SeventhLevelSceneName, EighthLevelSceneName, NinthLevelSceneName,
            TenthLevelSceneName
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Returns the scene name for a 1-based level number, or null if out of range.</summary>
        public static string GetLevelSceneName(int levelNumber)
        {
            int index = levelNumber - 1;
            if (index >= 0 && index < LevelSceneNames.Length)
                return LevelSceneNames[index];
            return null;
        }

        /// <summary>Returns the 1-based level number for the currently active scene, or 0 if not a level scene.</summary>
        public static int GetCurrentLevelNumber()
        {
            string activeName = SceneManager.GetActiveScene().name;
            for (int i = 0; i < LevelSceneNames.Length; i++)
            {
                if (LevelSceneNames[i] == activeName)
                    return i + 1;
            }
            return 0;
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.MainMenu);
            }
            LoadScene(MainMenuSceneName);
        }

        /// <summary>Load a level by its 1-based number (1–10).</summary>
        public void LoadLevelByNumber(int levelNumber)
        {
            string sceneName = GetLevelSceneName(levelNumber);
            if (sceneName == null)
            {
                Debug.LogWarning($"SceneLoader: Invalid level number {levelNumber}");
                return;
            }

            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }
            LoadScene(sceneName);
        }

        public void LoadLevel()
        {
            LoadLevelByNumber(1);
        }

        public void LoadNextLevel()
        {
            int currentLevel = GetCurrentLevelNumber();
            if (currentLevel > 0 && currentLevel < TotalLevelCount)
            {
                LoadLevelByNumber(currentLevel + 1);
            }
        }

        public void LoadLevel2()
        {
            LoadLevelByNumber(2);
        }

        public void LoadLevel3()
        {
            LoadLevelByNumber(3);
        }

        public void ReloadCurrentLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}