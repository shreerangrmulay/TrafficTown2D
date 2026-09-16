using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TrafficTown2D.Core;

namespace TrafficTown2D.UI
{
    public sealed class LevelIntroController : MonoBehaviour
    {
        [SerializeField] private GameObject introPanel;
        [SerializeField] private Button gotItButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;

        public bool IsShowing => introPanel != null && introPanel.activeSelf;

        private void Awake()
        {
            if (gotItButton != null)
            {
                gotItButton.onClick.RemoveListener(DismissIntro);
                gotItButton.onClick.AddListener(DismissIntro);
            }
        }

        private void Start()
        {
            // Do NOT fall back to FindAnyObjectByType<Canvas>() -- that grabs
            // unrelated canvases (e.g. the persistent MainMenu Canvas) and shows
            // them as an intro panel, blocking gameplay.
            if (introPanel == null)
            {
                GameManager.Instance?.SetState(GameState.Playing);
                return;
            }

            introPanel.SetActive(true);
            SetIntroContent();
            PauseForInstructions();
        }

        private void SetIntroContent()
        {
            string currentScene = SceneManager.GetActiveScene().name;

            if (titleText == null || messageText == null)
            {
                TMP_Text[] textElements = introPanel.GetComponentsInChildren<TMP_Text>(true);
                for (int index = 0; index < textElements.Length; index++)
                {
                    TMP_Text textElement = textElements[index];
                    if (titleText == null && textElement.name.Contains("Title")) titleText = textElement;
                    if (messageText == null && (textElement.name.Contains("Message") || textElement.name.Contains("Body"))) messageText = textElement;
                }
            }

            if (currentScene == "Level2")
            {
                if (titleText != null) titleText.text = "SMART CROSSING";
                if (messageText != null)
                {
                    messageText.text = "1. Stop beside the STOP sign.\n2. Press Q and E to check both ways.\n3. Use the crossing when WALK appears.";
                }
                return;
            }

            if (currentScene == "Level1")
            {
                if (titleText != null) titleText.text = "SAFE CROSSING";
                if (messageText != null)
                {
                    messageText.text = "Use WASD or arrow keys to move.\n\nFind the zebra crossing, wait for WALK, then cross.";
                }
                return;
            }
            
            // Find text components if not assigned
            if (titleText == null)
            {
                Transform[] allChildren = introPanel.GetComponentsInChildren<Transform>();
                foreach (var child in allChildren)
                {
                    if (child.name.Contains("Title"))
                    {
                        titleText = child.GetComponent<TMP_Text>();
                        break;
                    }
                }
            }

            if (messageText == null)
            {
                Transform[] allChildren = introPanel.GetComponentsInChildren<Transform>();
                foreach (var child in allChildren)
                {
                    if (child.name.Contains("Message"))
                    {
                        messageText = child.GetComponent<TMP_Text>();
                        break;
                    }
                }
            }

            if (currentScene == "Level2")
            {
                if (titleText != null) titleText.text = "🚸 SMART CROSSING";
                if (messageText != null)
                {
                    messageText.text = "Look BOTH ways before crossing.\n\nWait for a safe gap in traffic.";
                }
            }
            else if (currentScene == "Level1")
            {
                if (titleText != null) titleText.text = "🚸 SAFE CROSSING";
                if (messageText != null)
                {
                    messageText.text = "Use the zebra crossing.\n\nObey the traffic signal.";
                }
            }
            else if (currentScene == "Level3")
            {
                if (titleText != null) titleText.text = "🚗 YIELD TO PEDESTRIANS";
                if (messageText != null)
                {
                    messageText.text = "Drive with A/D or ← → keys.\n\nSlow down and STOP when a pedestrian steps onto the crosswalk.";
                }
            }
        }

        public void DismissIntro()
        {
            if (introPanel != null)
            {
                introPanel.SetActive(false);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
                return;
            }

            Time.timeScale = 1f;
        }

        private static void PauseForInstructions()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Paused);
                return;
            }

            Time.timeScale = 0f;
        }
    }
}
