using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace TrafficTown2D.UI
{
    public sealed class LevelIntroController : MonoBehaviour
    {
        [SerializeField] private GameObject introPanel;
        [SerializeField] private Button gotItButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;

        private void Awake()
        {
            if (gotItButton != null)
            {
                gotItButton.onClick.AddListener(DismissIntro);
            }
        }

        private void Start()
        {
            // Find or create the intro panel
            if (introPanel == null)
            {
                introPanel = FindObjectOfType<Canvas>()?.gameObject;
            }

            if (introPanel != null)
            {
                introPanel.SetActive(true);
                SetIntroContent();
            }
        }

        private void SetIntroContent()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            
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
        }

        public void DismissIntro()
        {
            if (introPanel != null)
            {
                introPanel.SetActive(false);
            }
        }
    }
}
