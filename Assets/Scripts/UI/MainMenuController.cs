using System;
using UnityEngine;
using UnityEngine.UI;
using TrafficTown2D.Core;

namespace TrafficTown2D.UI
{
    /// <summary>
    /// Coordinates high-level Main Menu actions and seamless toggling between
    /// the Main Menu buttons and the Winding Road Level Select map.
    /// Preserves all existing level loading signatures and save data integrations.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        public static event Action ExitRequested;

        [Header("Dependencies")]
        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private Text messageText;
        [SerializeField] private TMPro.TMP_Text messageTextTmp;

        [Header("UI Panels")]
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private GameObject mainMenuContent;
        [SerializeField] private LevelMapController levelMapController;

        private void Awake()
        {
            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(activeScene) && activeScene != "MainMenu")
            {
                Debug.LogWarning($"[MainMenuController] Found in non-MainMenu scene '{activeScene}'! Destroying menu object.");
                Destroy(gameObject);
                return;
            }

            MainMenuBootstrap.ConfigureRuntimeMenu();
            ResolvePanelReferences();
        }

        private void ResolvePanelReferences()
        {
            if (levelSelectPanel == null)
            {
                Transform p = transform.Find("LevelSelectPanel");
                if (p == null && transform.parent != null) p = transform.parent.Find("LevelSelectPanel");
                if (p != null) levelSelectPanel = p.gameObject;
            }

            if (mainMenuContent == null)
            {
                Transform c = transform.Find("MainMenuContent");
                if (c == null && transform.parent != null) c = transform.parent.Find("MainMenuContent");
                if (c != null) mainMenuContent = c.gameObject;
            }

            if (levelMapController == null && levelSelectPanel != null)
            {
                levelMapController = levelSelectPanel.GetComponentInChildren<LevelMapController>(true);
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
        public void PlayLevel2() => PlayLevel(2);
        public void PlayLevel3() => PlayLevel(3);
        public void PlayLevel4() => PlayLevel(4);
        public void PlayLevel5() => PlayLevel(5);
        public void PlayLevel6() => PlayLevel(6);
        public void PlayLevel7() => PlayLevel(7);
        public void PlayLevel8() => PlayLevel(8);
        public void PlayLevel9() => PlayLevel(9);
        public void PlayLevel10() => PlayLevel(10);

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
            MainMenuBootstrap.ConfigureRuntimeMenu();
            ResolvePanelReferences();

            if (mainMenuContent != null)
            {
                mainMenuContent.SetActive(false);
            }

            if (levelSelectPanel != null)
            {
                levelSelectPanel.SetActive(true);
                levelSelectPanel.transform.SetAsLastSibling();

                if (levelMapController != null)
                {
                    levelMapController.RefreshMap();
                }
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

        private void DestroyMenuCanvas()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}