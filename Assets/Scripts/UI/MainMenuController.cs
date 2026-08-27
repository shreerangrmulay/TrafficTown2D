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
            if (sceneLoader == null)
            {
                Debug.LogError("MainMenuController needs a SceneLoader reference.");
                return;
            }

            sceneLoader.LoadLevel();
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