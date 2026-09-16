using UnityEngine;
using UnityEngine.UI;
using TrafficTown2D.Core;
using System;

namespace TrafficTown2D.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        public static event Action ExitRequested;

        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private Text messageText;

        private void Awake()
        {
            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(activeScene) && activeScene != "MainMenu")
            {
                Debug.LogWarning($"[MainMenuController] Found in non-MainMenu scene '{activeScene}'! Destroying menu object.");
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.MainMenu);
            }

            ShowMessage(string.Empty);
        }

        public void Play()
        {
            DestroyMenuCanvas();
            Time.timeScale = 1f;

            if (sceneLoader != null)
            {
                sceneLoader.LoadLevel();
                return;
            }

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadLevel();
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("Level1");
        }

        public void PlayLevel2()
        {
            DestroyMenuCanvas();
            Time.timeScale = 1f;

            if (sceneLoader != null)
            {
                sceneLoader.LoadLevel2();
                return;
            }

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadLevel2();
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("Level2");
        }

        public void PlayLevel3()
        {
            DestroyMenuCanvas();
            Time.timeScale = 1f;

            if (sceneLoader != null)
            {
                sceneLoader.LoadLevel3();
                return;
            }

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadLevel3();
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("Level3");
        }

        public void Learn()
        {
            PlayLevel2();
        }

        public void Quiz()
        {
            PlayLevel3();
        }

        public void Settings()
        {
            ShowMessage("Settings Coming Soon");
        }

        public void Exit()
        {
            ExitRequested?.Invoke();
            Application.Quit();
        }

        private void ShowMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        /// <summary>
        /// Destroys the Canvas root GameObject so the main-menu UI does not persist
        /// into level scenes. The GameManager / SceneLoader singletons on their own
        /// separate GameObjects survive via DontDestroyOnLoad as intended.
        /// </summary>
        private void DestroyMenuCanvas()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }
            else
            {
                // Fallback: destroy this controller's root
                Destroy(gameObject);
            }
        }
    }
}