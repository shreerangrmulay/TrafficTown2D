using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrafficTown2D.Core
{
    public sealed class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        public const string MainMenuSceneName = "MainMenu";
        public const string FirstLevelSceneName = "Level1";

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

        public void LoadMainMenu()
        {
            LoadScene(MainMenuSceneName);
        }

        public void LoadLevel()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }

            LoadScene(FirstLevelSceneName);
        }

        public void LoadNextLevel()
        {
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetState(GameState.Playing);
                }

                SceneManager.LoadScene(nextSceneIndex);
            }
        }

        public void ReloadCurrentLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static void LoadScene(string sceneName)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogError($"Scene '{sceneName}' is not available in Build Settings.");
            }
        }
    }
}