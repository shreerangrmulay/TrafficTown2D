using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TrafficTown2D.Core;
using TrafficTown2D.Level3;

namespace TrafficTown2D.Level4
{
    public class Level4UIController : MonoBehaviour
    {
        [Header("Mission Card (Top-Left)")]
        [SerializeField] private TMP_Text missionBadgeText;
        [SerializeField] private TMP_Text missionTitleText;
        [SerializeField] private TMP_Text missionObjectiveText;

        [Header("Score & Safety (Top-Right)")]
        [SerializeField] private TMP_Text scoreValueText;
        [SerializeField] private TMP_Text safetyValueText;
        [SerializeField] private Image safetyMeterFill;

        [Header("Speedometer (Bottom-Right)")]
        [SerializeField] private TMP_Text currentSpeedText;
        [SerializeField] private TMP_Text speedLimitText;
        [SerializeField] private Image speedometerBadge;

        [Header("Night Status (Bottom-Left)")]
        [SerializeField] private TMP_Text headlightBadgeText;
        [SerializeField] private Image headlightBadgeBackground;
        [SerializeField] private TMP_Text visibilityText;
        [SerializeField] private Image visibilityMeterFill;

        [Header("Feedback Banner")]
        [SerializeField] private GameObject feedbackBanner;
        [SerializeField] private CanvasGroup feedbackGroup;
        [SerializeField] private TMP_Text feedbackIconText;
        [SerializeField] private TMP_Text feedbackMessageText;

        [Header("Completion Screen")]
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private CanvasGroup completionGroup;
        [SerializeField] private TMP_Text completionTitleText;
        [SerializeField] private TMP_Text completionSubtitleText;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text safetyScoreText;
        [SerializeField] private TMP_Text safeActionsText;
        [SerializeField] private TMP_Text violationsText;
        [SerializeField] private TMP_Text starRatingText;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button backToMenuButton;

        [Header("Dependencies")]
        [SerializeField] private Level3PlayerCar playerCar;
        [SerializeField] private PlayerHeadlightController playerHeadlights;
        [SerializeField] private NightVisibilityController visibilityController;

        private Coroutine feedbackRoutine;

        private void Awake()
        {
            if (completionPanel != null) completionPanel.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            }
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }
            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            }
        }

        private void Start()
        {
            if (playerCar != null)
            {
                playerCar.SpeedChanged += OnPlayerSpeedChanged;
            }

            if (playerHeadlights != null)
            {
                playerHeadlights.HeadlightModeChanged += UpdateHeadlightBadge;
                UpdateHeadlightBadge(playerHeadlights.CurrentMode);
            }
            else
            {
                UpdateHeadlightBadge(HeadlightMode.Off);
            }

            if (visibilityController != null)
            {
                visibilityController.VisibilityChanged += UpdateVisibilityUI;
                UpdateVisibilityUI(visibilityController.CurrentVisibilityPercent);
            }
        }

        private void OnDestroy()
        {
            if (playerCar != null)
            {
                playerCar.SpeedChanged -= OnPlayerSpeedChanged;
            }
            if (playerHeadlights != null)
            {
                playerHeadlights.HeadlightModeChanged -= UpdateHeadlightBadge;
            }
            if (visibilityController != null)
            {
                visibilityController.VisibilityChanged -= UpdateVisibilityUI;
            }
        }

        public void SetDependencies(Level3PlayerCar car, PlayerHeadlightController headlights, NightVisibilityController vis)
        {
            playerCar = car;
            playerHeadlights = headlights;
            visibilityController = vis;

            if (playerCar != null)
            {
                playerCar.SpeedChanged += OnPlayerSpeedChanged;
            }
            if (playerHeadlights != null)
            {
                playerHeadlights.HeadlightModeChanged += UpdateHeadlightBadge;
                UpdateHeadlightBadge(playerHeadlights.CurrentMode);
            }
            if (visibilityController != null)
            {
                visibilityController.VisibilityChanged += UpdateVisibilityUI;
                UpdateVisibilityUI(visibilityController.CurrentVisibilityPercent);
            }
        }

        public void SetMissionInfo(int missionIndex, int totalMissions, string title, string subtitle, string objective, float speedLimit)
        {
            if (missionBadgeText != null)
            {
                missionBadgeText.text = $"MISSION {missionIndex} / {totalMissions}";
            }
            if (missionTitleText != null)
            {
                missionTitleText.text = title;
            }
            if (missionObjectiveText != null)
            {
                missionObjectiveText.text = objective;
            }
            if (speedLimitText != null)
            {
                speedLimitText.text = $"LIMIT: {Mathf.RoundToInt(speedLimit)} KM/H";
            }
        }

        public void UpdateScores(int score, int safety)
        {
            if (scoreValueText != null)
            {
                scoreValueText.text = $"{score}";
            }
            if (safetyValueText != null)
            {
                safetyValueText.text = $"{Mathf.Clamp(safety, 0, 100)}%";
            }
            if (safetyMeterFill != null)
            {
                safetyMeterFill.fillAmount = Mathf.Clamp01(safety / 100f);
                safetyMeterFill.color = safety >= 70 ? new Color(0.2f, 0.85f, 0.35f, 1f) :
                                       safety >= 40 ? new Color(1f, 0.75f, 0.2f, 1f) :
                                                      new Color(0.95f, 0.25f, 0.25f, 1f);
            }
        }

        private void OnPlayerSpeedChanged(float speedKmh, float limitKmh)
        {
            if (currentSpeedText != null)
            {
                currentSpeedText.text = $"{Mathf.RoundToInt(speedKmh)}";
            }

            if (speedometerBadge != null)
            {
                bool isSpeeding = speedKmh > limitKmh + 2f;
                speedometerBadge.color = isSpeeding ? new Color(0.95f, 0.22f, 0.22f, 0.9f) : new Color(0.12f, 0.22f, 0.35f, 0.85f);
            }
        }

        public void UpdateHeadlightBadge(HeadlightMode mode)
        {
            if (headlightBadgeText == null) return;

            switch (mode)
            {
                case HeadlightMode.Off:
                    headlightBadgeText.text = "❌ LIGHTS OFF [H]";
                    if (headlightBadgeBackground != null)
                        headlightBadgeBackground.color = new Color(0.85f, 0.2f, 0.2f, 0.9f);
                    break;

                case HeadlightMode.LowBeam:
                    headlightBadgeText.text = "💡 LOW BEAM [H/F]";
                    if (headlightBadgeBackground != null)
                        headlightBadgeBackground.color = new Color(0.15f, 0.65f, 0.35f, 0.9f);
                    break;

                case HeadlightMode.HighBeam:
                    headlightBadgeText.text = "🔦 HIGH BEAM [F]";
                    if (headlightBadgeBackground != null)
                        headlightBadgeBackground.color = new Color(0.2f, 0.55f, 0.95f, 0.9f);
                    break;
            }
        }

        public void UpdateVisibilityUI(float visPercent)
        {
            if (visibilityText != null)
            {
                visibilityText.text = $"VISIBILITY: {Mathf.RoundToInt(visPercent)}%";
            }
            if (visibilityMeterFill != null)
            {
                visibilityMeterFill.fillAmount = Mathf.Clamp01(visPercent / 100f);
                if (visibilityController != null)
                {
                    visibilityMeterFill.color = visibilityController.GetVisibilityColor();
                }
            }
        }

        public void ShowFeedback(string message, bool isPositive)
        {
            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
            }
            feedbackRoutine = StartCoroutine(ShowFeedbackRoutine(message, isPositive));
        }

        private IEnumerator ShowFeedbackRoutine(string message, bool isPositive)
        {
            if (feedbackBanner == null) yield break;

            feedbackBanner.SetActive(true);
            if (feedbackIconText != null)
            {
                feedbackIconText.text = isPositive ? "✓" : "⚠️";
                feedbackIconText.color = isPositive ? new Color(0.2f, 0.9f, 0.4f) : new Color(1f, 0.75f, 0.1f);
            }
            if (feedbackMessageText != null)
            {
                feedbackMessageText.text = message;
            }

            if (feedbackGroup != null)
            {
                feedbackGroup.alpha = 0f;
                float elapsed = 0f;
                while (elapsed < 0.25f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    feedbackGroup.alpha = Mathf.Clamp01(elapsed / 0.25f);
                    yield return null;
                }
                feedbackGroup.alpha = 1f;
            }

            yield return new WaitForSecondsRealtime(2.2f);

            if (feedbackGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < 0.35f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    feedbackGroup.alpha = Mathf.Clamp01(1f - (elapsed / 0.35f));
                    yield return null;
                }
                feedbackGroup.alpha = 0f;
            }

            feedbackBanner.SetActive(false);
            feedbackRoutine = null;
        }

        public void ShowCompletionPanel(int totalScore, int safetyScore, int safeActions, int violations)
        {
            if (completionPanel == null) return;

            completionPanel.SetActive(true);

            if (finalScoreText != null) finalScoreText.text = $"{totalScore}";
            if (safetyScoreText != null) safetyScoreText.text = $"{Mathf.Clamp(safetyScore, 0, 100)}%";
            if (safeActionsText != null) safeActionsText.text = $"{safeActions}";
            if (violationsText != null) violationsText.text = $"{violations}";

            string stars = "⭐⭐⭐";
            string subtitle = "NIGHT DRIVING MASTER!";
            if (safetyScore < 60)
            {
                stars = "⭐";
                subtitle = "PRACTICE CAUTION AT NIGHT!";
            }
            else if (safetyScore < 85)
            {
                stars = "⭐⭐";
                subtitle = "WELL DONE!";
            }

            if (starRatingText != null) starRatingText.text = stars;
            if (completionSubtitleText != null) completionSubtitleText.text = subtitle;

            StartCoroutine(FadeInCompletionPanel());
        }

        private IEnumerator FadeInCompletionPanel()
        {
            if (completionGroup != null)
            {
                completionGroup.alpha = 0f;
                float elapsed = 0f;
                while (elapsed < 0.4f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    completionGroup.alpha = Mathf.Clamp01(elapsed / 0.4f);
                    yield return null;
                }
                completionGroup.alpha = 1f;
            }
        }

        private void OnNextLevelClicked()
        {
            Time.timeScale = 1f;
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadNextLevel();
            }
            else
            {
                SceneManager.LoadScene(SceneLoader.FifthLevelSceneName);
            }
        }

        private void OnRetryClicked()
        {
            Time.timeScale = 1f;
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.ReloadCurrentLevel();
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void OnBackToMenuClicked()
        {
            Time.timeScale = 1f;
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadMainMenu();
            }
            else
            {
                SceneManager.LoadScene(SceneLoader.MainMenuSceneName);
            }
        }
    }
}
