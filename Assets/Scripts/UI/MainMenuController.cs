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

        public void Learn()
        {
            ShowMessage("Learning Mode Coming Soon");
        }

        public void Quiz()
        {
            ShowMessage("Quiz Mode Coming Soon");
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
    }
}