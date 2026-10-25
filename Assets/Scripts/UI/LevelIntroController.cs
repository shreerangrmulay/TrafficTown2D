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
            EnsureGotItButton();
        }

        private void Start()
        {
            EnsureGotItButton();

            // Do NOT fall back to FindAnyObjectByType<Canvas>() -- that grabs
            // unrelated canvases (e.g. the persistent MainMenu Canvas) and shows
            // them as an intro panel, blocking gameplay.
            if (introPanel == null)
            {
                Time.timeScale = 1f;
                GameManager.Instance?.SetState(GameState.Playing);
                return;
            }

            introPanel.SetActive(true);
            SetIntroContent();
            PauseForInstructions();
        }

        private void EnsureGotItButton()
        {
            if (gotItButton == null && introPanel != null)
            {
                gotItButton = introPanel.GetComponentInChildren<Button>(true);
            }

            if (gotItButton != null)
            {
                gotItButton.onClick.RemoveListener(DismissIntro);
                gotItButton.onClick.AddListener(DismissIntro);

                // Crucial fix: Ensure any child text/image inside GotItButton does NOT block raycasts,
                // otherwise clicks on the label will not trigger the button!
                Graphic[] childGraphics = gotItButton.GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < childGraphics.Length; i++)
                {
                    if (childGraphics[i].gameObject != gotItButton.gameObject)
                    {
                        childGraphics[i].raycastTarget = false;
                    }
                }
            }
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
                    SetTexts("🚗 THE OVERTAKE",
                        "Don't just overtake. Overtake safely.\n\n" +
                        "1. Check Road Markings: Only overtake on dashed lines; solid lines prohibit passing.\n" +
                        "2. Judge Oncoming Traffic: Wait behind lead vehicles until oncoming lanes are completely clear.\n" +
                        "3. Prohibited Zones: NEVER overtake on blind bends, crests, or crossings.\n" +
                        "4. Signal Your Intentions: Press Q to signal before pulling out, and E before returning.\n" +
                        "5. Patience Saves Lives: Waiting safely behind a slower vehicle is often the safest decision!");
                    break;
                case 9:
                    SetTexts("ROUNDABOUT RULES",
                        "Slow Down. Give Way. Choose Your Exit.\n\n" +
                        "1. Reduce Speed: Slow down before entering (< 25 km/h).\n" +
                        "2. Give Way: Vehicles circulating inside have right of way.\n" +
                        "3. Circulate Clockwise: Travel around the central island.\n" +
                        "4. Signal Before Exit: Press Q or E to indicate.\n" +
                        "5. Yield to Pedestrians: Stop at zebra crossings.");
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

            Time.timeScale = 1f;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }

            var playerCars = UnityEngine.Object.FindObjectsByType<TrafficTown2D.Level3.Level3PlayerCar>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < playerCars.Length; i++)
            {
                playerCars[i].SetControlEnabled(true);
            }
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
