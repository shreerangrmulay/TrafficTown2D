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
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.MainMenu);
            }
            LoadScene(MainMenuSceneName);
        }

        public void LoadLevel()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }

            LoadScene(FirstLevelSceneName);
        }

        public void LoadNextLevel()
        {
            Time.timeScale = 1f;
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

        public void LoadLevel2()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }

            LoadScene(SecondLevelSceneName);
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