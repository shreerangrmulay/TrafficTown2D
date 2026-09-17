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

            // Resolve text components if not assigned
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

            int levelNum = SceneLoader.GetCurrentLevelNumber();

            switch (levelNum)
            {
                case 1:
                    SetTexts("SAFE CROSSING",
                        "Use WASD or arrow keys to move.\n\nFind the zebra crossing, wait for WALK, then cross.");
                    break;
                case 2:
                    SetTexts("SMART CROSSING",
                        "1. Stop beside the STOP sign.\n2. Press Q and E to check both ways.\n3. Use the crossing when WALK appears.");
                    break;
                case 3:
                    SetTexts("🚗 YIELD TO PEDESTRIANS",
                        "Drive with A/D or ← → keys.\n\nSlow down and STOP when a pedestrian steps onto the crosswalk.");
                    break;
                case 4:
                    SetTexts("🚲 BIKE LANE AWARENESS",
                        "Drive with A/D or ← → keys.\n\nPress Q or E to check blind spots before turning across the bike lane.\nDo NOT hit cyclists!");
                    break;
                case 5:
                    SetTexts("🛑 THE STOP SIGN",
                        "Approach the 4-way stop intersection.\n\n1. Come to a COMPLETE stop at the line.\n2. Yield to vehicles that arrived first.\n3. Proceed when clear.");
                    break;
                case 6:
                    SetTexts("⬆️ ONE-WAY STREETS",
                        "Drive with A/D and W/S keys.\n\nWatch for DO NOT ENTER signs.\nDo NOT drive the wrong way on one-way streets!\nReach the destination.");
                    break;
                case 7:
                    SetTexts("🏫 SCHOOL ZONES & SPEED LIMITS",
                        "Drive forward with A/D keys.\n\nObey all posted speed limits.\nSLOW DOWN in the school zone!\nYour speed is shown on the HUD.");
                    break;
                case 8:
                    SetTexts("🚑 EMERGENCY VEHICLES",
                        "Drive forward with A/D keys.\n\nWhen you hear sirens or see flashing lights:\n1. Pull over to the shoulder (W/S to move up/down).\n2. STOP completely.\n3. Wait until the emergency vehicle passes.");
                    break;
                case 9:
                    SetTexts("🔄 ROUNDABOUTS",
                        "Drive with A/D and W/S keys.\n\n1. YIELD to traffic inside the roundabout.\n2. Enter when clear.\n3. Press Q or E to SIGNAL before exiting.");
                    break;
                case 10:
                    SetTexts("🏆 THE ULTIMATE COMMUTE",
                        "Apply everything you've learned!\n\nDrive through the city and pass each checkpoint without breaking any traffic rules.\n3 strikes and you're out!");
                    break;
                default:
                    SetTexts("TRAFFIC TOWN", "Follow the traffic rules to complete the level.");
                    break;
            }
        }

        private void SetTexts(string title, string message)
        {
            if (titleText != null) titleText.text = title;
            if (messageText != null) messageText.text = message;
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
