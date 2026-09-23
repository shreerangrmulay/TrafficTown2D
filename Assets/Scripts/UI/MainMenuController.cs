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
        [SerializeField] private TMPro.TMP_Text messageTextTmp;
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private GameObject mainMenuContent;

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
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
            if (mainMenuContent != null) mainMenuContent.SetActive(true);
        }

        public void Play()
        {
            OpenLevelSelect();
        }

        public void PlayLevel1() => PlayLevel(1);

        public void PlayLevel(int levelNumber)
        {
            DestroyMenuCanvas();
            Time.timeScale = 1f;

            if (sceneLoader != null)
            {
                sceneLoader.LoadLevelByNumber(levelNumber);
                return;
            }

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadLevelByNumber(levelNumber);
                return;
            }

            string sceneName = SceneLoader.GetLevelSceneName(levelNumber);
            if (sceneName != null)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }

        public void PlayLevel2() => PlayLevel(2);
        public void PlayLevel3() => PlayLevel(3);
        public void PlayLevel4() => PlayLevel(4);
        public void PlayLevel5() => PlayLevel(5);
        public void PlayLevel6() => PlayLevel(6);
        public void PlayLevel7() => PlayLevel(7);
        public void PlayLevel8() => PlayLevel(8);
        public void PlayLevel9() => PlayLevel(9);
        public void PlayLevel10() => PlayLevel(10);

        public void Learn()
        {
            PlayLevel2();
        }

        public void Quiz()
        {
            DestroyMenuCanvas();
            Time.timeScale = 1f;

            if (sceneLoader != null)
            {
                sceneLoader.LoadQuiz();
                return;
            }

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadQuiz();
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("Quiz");
        }

        public void OpenLevelSelect()
        {
            if (mainMenuContent != null)
            {
                mainMenuContent.SetActive(false);
            }

            if (levelSelectPanel != null)
            {
                levelSelectPanel.SetActive(true);
                levelSelectPanel.transform.SetAsLastSibling();
            }
        }

        public void CloseLevelSelect()
        {
            if (levelSelectPanel != null)
            {
                levelSelectPanel.SetActive(false);
            }

            if (mainMenuContent != null)
            {
                mainMenuContent.SetActive(true);
            }
        }

        public void Settings()
        {
            OpenLevelSelect();
        }

        public void Exit()
        {
            ExitRequested?.Invoke();
            Application.Quit();
        }

        private void ShowMessage(string message)
        {
            if (messageTextTmp != null)
            {
                messageTextTmp.text = message;
            }
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