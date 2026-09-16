using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using TrafficTown2D.Core;
using TrafficTown2D.Gameplay;

namespace TrafficTown2D.UI
{
    public sealed class LevelUIController : MonoBehaviour
    {
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private CanvasGroup completionGroup;
        [SerializeField] private RectTransform completionCard;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text safeActionsText;
        [SerializeField] private TMP_Text mistakesText;
        [SerializeField] private TMP_Text ratingStarsText;
        [SerializeField] private TMP_Text ratingText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private SceneLoader sceneLoader;

        private Coroutine completionRoutine;
        private bool isLevel2;
        private bool stoppedAtStopSign;
        private bool checkedBothDirections;
        private bool crossedSafely;
        private bool enteredCrosswalk;
        private bool waitedForWalk;
        private bool reachedDestination;

        private void Awake()
        {
            HideCompletion();
        }

        private void OnEnable()
        {
            if (scoreManager != null) scoreManager.ScoreChanged += UpdateScore;
        }

        private void Start()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            isLevel2 = sceneName == SceneLoader.SecondLevelSceneName;
            bool isLevel3 = sceneName == SceneLoader.ThirdLevelSceneName;

            LevelIntroController intro = FindAnyObjectByType<LevelIntroController>(FindObjectsInactive.Include);
            if (intro == null || !intro.IsShowing)
            {
                GameManager.Instance?.SetState(GameState.Playing);
            }

            if (objectiveText != null)
            {
                if (isLevel2)
                {
                    PrepareLevel2MissionCard();
                    UpdateLevel2Objectives(false, false, false);
                }
                else if (isLevel3)
                {
                    // Level 3 mission text is configured by Level 3 setup; do not overwrite with Level 1 objectives
                }
                else
                {
                    PrepareLevel1MissionCard();
                    UpdateLevel1Objectives(false, false, false);
                }
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(BackToMenu);
                backButton.onClick.AddListener(BackToMenu);
                RenameBackButton();
            }

            ResolveNextButton();
            if (nextButton != null)
            {
                nextButton.interactable = true;
                nextButton.onClick.RemoveListener(LoadNextOrReplay);
                nextButton.onClick.AddListener(LoadNextOrReplay);
                RenameNextButton();
            }

            HideCompletion();
            UpdateScore(scoreManager != null ? scoreManager.CurrentScore : 0);
        }

        private void OnDisable()
        {
            if (scoreManager != null) scoreManager.ScoreChanged -= UpdateScore;
        }

        public void ShowCompletion()
        {
            if (scoreManager != null)
            {
                if (finalScoreText != null) finalScoreText.text = scoreManager.CurrentScore.ToString();
                if (safeActionsText != null) safeActionsText.text = scoreManager.SafeActions.ToString();
                if (mistakesText != null) mistakesText.text = scoreManager.Mistakes.ToString();

                int starCount = scoreManager.Mistakes == 0 ? 5 : scoreManager.Mistakes < 3 ? 4 : 3;
                if (ratingStarsText != null) ratingStarsText.text = starCount + "/5";
                if (ratingText != null) ratingText.text = starCount == 5 ? "Excellent!" : "Great effort!";
            }

            if (completionPanel == null) return;

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }

            completionPanel.SetActive(true);
            completionPanel.transform.SetAsLastSibling();
            if (completionRoutine != null) StopCoroutine(completionRoutine);
            completionRoutine = StartCoroutine(AnimateCompletion());
        }

        public void ShowFailure(string instructions)
        {
            if (completionPanel == null || completionCard == null) return;

            UpdateFailureCard(instructions);

            if (backButton != null)
            {
                backButton.onClick = new Button.ButtonClickedEvent();
                backButton.onClick.AddListener(RestartCurrentLevel);
                SetButtonLabel(backButton, "RESTART LEVEL");
                RectTransform restartRect = backButton.GetComponent<RectTransform>();
                if (restartRect != null) restartRect.anchoredPosition = new Vector2(0f, -164f);
            }

            if (nextButton != null) nextButton.gameObject.SetActive(false);

            completionPanel.SetActive(true);
            completionPanel.transform.SetAsLastSibling();
            if (completionRoutine != null) StopCoroutine(completionRoutine);
            completionRoutine = StartCoroutine(AnimateCompletion());
        }

        public void UpdateLevel2Objectives(bool stopped, bool lookedBothWays, bool crossed)
        {
            if (!isLevel2 || objectiveText == null) return;

            stoppedAtStopSign = stoppedAtStopSign || stopped;
            checkedBothDirections = checkedBothDirections || lookedBothWays;
            crossedSafely = crossedSafely || crossed;

            objectiveText.text =
                "Smart Crossing\n" +
                FormatObjective(stoppedAtStopSign, "Stop at STOP sign") + "\n" +
                FormatObjective(checkedBothDirections, "Look both ways") + "\n" +
                FormatObjective(crossedSafely, "Cross safely");
        }

        public void UpdateLevel1Objectives(bool enteredCrosswalk, bool waitedForWalk, bool reachedDestination)
        {
            if (isLevel2 || objectiveText == null) return;

            this.enteredCrosswalk = this.enteredCrosswalk || enteredCrosswalk;
            this.waitedForWalk = this.waitedForWalk || waitedForWalk;
            this.reachedDestination = this.reachedDestination || reachedDestination;

            objectiveText.text =
                "Safe Crossing\n" +
                FormatObjective(this.enteredCrosswalk, "Use the crosswalk") + "\n" +
                FormatObjective(this.waitedForWalk, "Wait for WALK") + "\n" +
                FormatObjective(this.reachedDestination, "Reach the sidewalk");
        }

        public void BackToMenu()
        {
            Time.timeScale = 1f;
            if (sceneLoader != null)
            {
                sceneLoader.LoadMainMenu();
                return;
            }

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadMainMenu();
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        public void LoadNextOrReplay()
        {
            Time.timeScale = 1f;

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName == SceneLoader.ThirdLevelSceneName)
            {
                if (sceneLoader != null)
                {
                    sceneLoader.ReloadCurrentLevel();
                    return;
                }
                if (SceneLoader.Instance != null)
                {
                    SceneLoader.Instance.ReloadCurrentLevel();
                    return;
                }
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                return;
            }

            if (isLevel2 || sceneName == SceneLoader.SecondLevelSceneName)
            {
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

                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneLoader.ThirdLevelSceneName);
                return;
            }

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

            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneLoader.SecondLevelSceneName);
        }

        public void RestartCurrentLevel()
        {
            Time.timeScale = 1f;
            if (sceneLoader != null)
            {
                sceneLoader.ReloadCurrentLevel();
                return;
            }

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.ReloadCurrentLevel();
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void UpdateScore(int score)
        {
            if (scoreText != null) scoreText.text = score.ToString();
        }

        private void PrepareLevel2MissionCard()
        {
            RectTransform objectiveRect = objectiveText.GetComponent<RectTransform>();
            RectTransform missionCard = objectiveText.transform.parent as RectTransform;
            if (missionCard != null)
            {
                missionCard.anchorMin = new Vector2(0f, 1f);
                missionCard.anchorMax = new Vector2(0f, 1f);
                missionCard.anchoredPosition = new Vector2(180f, -78f);
                missionCard.sizeDelta = new Vector2(320f, 128f);
            }

            if (objectiveRect != null)
            {
                objectiveRect.anchorMin = new Vector2(0f, 0.5f);
                objectiveRect.anchorMax = new Vector2(0f, 0.5f);
                objectiveRect.anchoredPosition = new Vector2(12f, -18f);
                objectiveRect.sizeDelta = new Vector2(292f, 86f);
            }

            objectiveText.fontSize = 12.5f;
            objectiveText.alignment = TextAlignmentOptions.Left;
            objectiveText.textWrappingMode = TextWrappingModes.Normal;
        }

        private void PrepareLevel1MissionCard()
        {
            RectTransform objectiveRect = objectiveText.GetComponent<RectTransform>();
            RectTransform missionCard = objectiveText.transform.parent as RectTransform;
            if (missionCard != null)
            {
                missionCard.sizeDelta = new Vector2(360f, 132f);
            }

            if (objectiveRect != null)
            {
                objectiveRect.anchoredPosition = new Vector2(82f, -62f);
                objectiveRect.sizeDelta = new Vector2(250f, 90f);
            }

            objectiveText.fontSize = 14f;
            objectiveText.alignment = TextAlignmentOptions.Left;
            objectiveText.textWrappingMode = TextWrappingModes.Normal;
        }

        private void RenameBackButton()
        {
            TMP_Text[] labels = backButton.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < labels.Length; index++)
            {
                labels[index].text = "BACK TO MAIN MENU";
                labels[index].fontSize = Mathf.Min(labels[index].fontSize, 15f);
            }
        }

        private void RenameNextButton()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string buttonLabel = sceneName == SceneLoader.ThirdLevelSceneName ? "REPLAY" : "NEXT LEVEL";
            SetButtonLabel(nextButton, buttonLabel);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null) return;

            TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < labels.Length; index++)
            {
                labels[index].text = label;
                labels[index].fontSize = Mathf.Min(labels[index].fontSize, 15f);
            }
        }

        private void UpdateFailureCard(string instructions)
        {
            SetCompletionText("Title", "CROSSING FAILED");
            SetCompletionText("Header", "CROSSING FAILED");
            TMP_Text subtitle = FindCompletionText("Subtitle");
            if (subtitle != null)
            {
                subtitle.text = instructions;
                subtitle.fontSize = 17f;
                subtitle.alignment = TextAlignmentOptions.Center;
                RectTransform subtitleRect = subtitle.GetComponent<RectTransform>();
                if (subtitleRect != null)
                {
                    subtitleRect.anchoredPosition = new Vector2(0f, 34f);
                    subtitleRect.sizeDelta = new Vector2(420f, 190f);
                }
            }

            SetCompletionText("FinalScore", string.Empty);
            SetCompletionText("FinalScoreLabel", string.Empty);
            SetCompletionText("Rating", string.Empty);
            SetCompletionText("RatingText", string.Empty);
            SetCompletionObjectActive("StatisticsRow", false);
        }

        private void SetCompletionText(string objectName, string value)
        {
            TMP_Text text = FindCompletionText(objectName);
            if (text != null) text.text = value;
        }

        private TMP_Text FindCompletionText(string objectName)
        {
            TMP_Text[] textElements = completionCard.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < textElements.Length; index++)
            {
                if (textElements[index].name == objectName) return textElements[index];
            }

            return null;
        }

        private void SetCompletionObjectActive(string objectName, bool active)
        {
            Transform[] transforms = completionCard.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                if (transforms[index].name == objectName)
                {
                    transforms[index].gameObject.SetActive(active);
                    return;
                }
            }
        }

        private void ResolveNextButton()
        {
            if (nextButton == null)
            {
                Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
                for (int index = 0; index < buttons.Length; index++)
                {
                    if (buttons[index].gameObject.scene == gameObject.scene && buttons[index].name == "NextLevelButton")
                    {
                        nextButton = buttons[index];
                        break;
                    }
                }
            }

            if (nextButton != null || backButton == null) return;

            nextButton = Instantiate(backButton, backButton.transform.parent);
            nextButton.name = "NextLevelButton";
            nextButton.onClick = new Button.ButtonClickedEvent();

            RectTransform backRect = backButton.GetComponent<RectTransform>();
            RectTransform nextRect = nextButton.GetComponent<RectTransform>();
            if (backRect != null && nextRect != null)
            {
                backRect.anchoredPosition = new Vector2(-132f, backRect.anchoredPosition.y);
                nextRect.anchoredPosition = new Vector2(132f, nextRect.anchoredPosition.y);
                nextRect.sizeDelta = backRect.sizeDelta;
            }
        }

        private static string FormatObjective(bool done, string label)
        {
            return (done ? "[x] " : "[ ] ") + label;
        }

        private void HideCompletion()
        {
            if (completionRoutine != null)
            {
                StopCoroutine(completionRoutine);
                completionRoutine = null;
            }

            if (completionGroup != null)
            {
                completionGroup.alpha = 0f;
                completionGroup.interactable = false;
                completionGroup.blocksRaycasts = false;
            }

            if (completionCard != null) completionCard.localScale = Vector3.one * 0.9f;
            if (backButton != null) backButton.interactable = false;
            if (nextButton != null) nextButton.interactable = false;
            if (completionPanel != null) completionPanel.SetActive(false);
        }

        private System.Collections.IEnumerator AnimateCompletion()
        {
            const float duration = 0.35f;
            float elapsed = 0f;

            if (completionGroup != null)
            {
                completionGroup.alpha = 0f;
                completionGroup.interactable = false;
                completionGroup.blocksRaycasts = true;
            }

            if (completionCard != null) completionCard.localScale = Vector3.one * 0.9f;
            if (backButton != null) backButton.interactable = false;
            if (nextButton != null) nextButton.interactable = false;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);

                if (completionGroup != null) completionGroup.alpha = eased;
                if (completionCard != null) completionCard.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, eased);
                yield return null;
            }

            if (completionGroup != null)
            {
                completionGroup.alpha = 1f;
                completionGroup.interactable = true;
            }

            if (completionCard != null) completionCard.localScale = Vector3.one;
            if (backButton != null) backButton.interactable = true;
            if (nextButton != null) nextButton.interactable = true;
            completionRoutine = null;
        }
    }

    [RequireComponent(typeof(RectTransform))]
    public sealed class AnimatedUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.04f;
        [SerializeField] private float pressedScale = 0.96f;
        [SerializeField] private float animationSeconds = 0.08f;
        [SerializeField] private Image targetImage;
        [SerializeField] private Color normalColor = new Color(0.12f, 0.55f, 0.84f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.16f, 0.64f, 0.94f, 1f);
        [SerializeField] private Color pressedColor = new Color(0.08f, 0.44f, 0.72f, 1f);

        private RectTransform rectTransform;
        private Coroutine animationRoutine;
        private bool hovering;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (targetImage == null) targetImage = GetComponent<Image>();
            Apply(1f, normalColor);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            AnimateTo(hoverScale, hoverColor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            AnimateTo(1f, normalColor);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            AnimateTo(pressedScale, pressedColor);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateTo(hovering ? hoverScale : 1f, hovering ? hoverColor : normalColor);
        }

        private void AnimateTo(float scale, Color color)
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(Animate(scale, color));
        }

        private System.Collections.IEnumerator Animate(float targetScale, Color targetColor)
        {
            Vector3 startScale = rectTransform.localScale;
            Color startColor = targetImage != null ? targetImage.color : targetColor;
            float elapsed = 0f;

            while (elapsed < animationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / animationSeconds);
                Apply(Mathf.Lerp(startScale.x, targetScale, progress), Color.Lerp(startColor, targetColor, progress));
                yield return null;
            }

            Apply(targetScale, targetColor);
            animationRoutine = null;
        }

        private void Apply(float scale, Color color)
        {
            rectTransform.localScale = Vector3.one * scale;
            if (targetImage != null) targetImage.color = color;
        }
    }
}
