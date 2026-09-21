using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TrafficTown2D.Core;
using TrafficTown2D.UI;

namespace TrafficTown2D.Level3
{
    public class Level3UIController : MonoBehaviour
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

        [Header("Feedback Banner (Bottom-Center)")]
        [SerializeField] private GameObject feedbackBanner;
        [SerializeField] private CanvasGroup feedbackGroup;
        [SerializeField] private TMP_Text feedbackIconText;
        [SerializeField] private TMP_Text feedbackMessageText;

        [Header("Distraction Modal")]
        [SerializeField] private GameObject distractionModal;
        [SerializeField] private Button ignoreButton;
        [SerializeField] private Button openButton;

        [Header("Completion Screen")]
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private CanvasGroup completionGroup;
        [SerializeField] private TMP_Text completionTitleText;
        [SerializeField] private TMP_Text completionSubtitleText;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text safetyScoreText;
        [SerializeField] private TMP_Text safeActionsText;
        [SerializeField] private TMP_Text violationsText;
        [SerializeField] private TMP_Text missionsCompletedText;
        [SerializeField] private TMP_Text starRatingText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button backToMenuButton;

        [Header("Dependencies")]
        [SerializeField] private Level3PlayerCar playerCar;

        private Coroutine feedbackRoutine;
        private Action<bool> distractionCallback;

        private void Awake()
        {
            if (completionPanel != null) completionPanel.SetActive(false);
            if (distractionModal != null) distractionModal.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }
            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            }

            if (ignoreButton != null)
            {
                ignoreButton.onClick.AddListener(() => OnDistractionButtonPressed(true));
            }
            if (openButton != null)
            {
                openButton.onClick.AddListener(() => OnDistractionButtonPressed(false));
            }
        }

        private void Start()
        {
            if (playerCar != null)
            {
                playerCar.SpeedChanged += OnPlayerSpeedChanged;
            }
        }

        private void OnDestroy()
        {
            if (playerCar != null)
            {
                playerCar.SpeedChanged -= OnPlayerSpeedChanged;
            }
        }

        private void OnPlayerSpeedChanged(float currentKmh, float limitKmh)
        {
            if (currentSpeedText != null)
            {
                currentSpeedText.text = $"{Mathf.RoundToInt(currentKmh)} <size=14>km/h</size>";
                currentSpeedText.color = currentKmh > (limitKmh + 2f) ? TrafficTownTheme.DangerColor : TrafficTownTheme.TextPrimaryColor;
            }

            if (speedLimitText != null)
            {
                speedLimitText.text = $"LIMIT: {Mathf.RoundToInt(limitKmh)}";
            }

            if (speedometerBadge != null)
            {
                speedometerBadge.color = currentKmh > (limitKmh + 2f) ? TrafficTownTheme.DangerColor : TrafficTownTheme.ButtonPrimaryColor;
            }
        }

        public void UpdateMissionDisplay(int missionNumber, int totalMissions, string title, string objective)
        {
            if (missionBadgeText != null)
            {
                missionBadgeText.text = $"MISSION {missionNumber} / {totalMissions}";
            }
            if (missionTitleText != null)
            {
                missionTitleText.text = title;
            }
            if (missionObjectiveText != null)
            {
                missionObjectiveText.text = objective;
            }
        }

        public void UpdateScoreAndSafety(int score, int safety)
        {
            if (scoreValueText != null)
            {
                scoreValueText.text = $"{score}";
            }

            if (safetyValueText != null)
            {
                safetyValueText.text = $"{safety}%";
            }

            if (safetyMeterFill != null)
            {
                float normalized = Mathf.Clamp01(safety / 100f);
                safetyMeterFill.fillAmount = normalized;

                if (normalized >= 0.70f)
                {
                    safetyMeterFill.color = TrafficTownTheme.SuccessColor;
                }
                else if (normalized >= 0.40f)
                {
                    safetyMeterFill.color = TrafficTownTheme.WarningColor;
                }
                else
                {
                    safetyMeterFill.color = TrafficTownTheme.DangerColor;
                }
            }
        }

        public void ShowFeedback(string message, bool isWarning)
        {
            if (feedbackBanner == null) return;

            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
            feedbackRoutine = StartCoroutine(ShowFeedbackRoutine(message, isWarning));
        }

        private IEnumerator ShowFeedbackRoutine(string message, bool isWarning)
        {
            feedbackBanner.SetActive(true);

            if (feedbackIconText != null)
            {
                // Note: using ASCII-safe characters to avoid TMP missing glyph warnings
                feedbackIconText.text = isWarning ? "!" : "[OK]";
                feedbackIconText.color = isWarning ? TrafficTownTheme.WarningColor : TrafficTownTheme.SuccessColor;
            }

            if (feedbackMessageText != null)
            {
                feedbackMessageText.text = message;
            }

            // Fade in
            if (feedbackGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < 0.15f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    feedbackGroup.alpha = Mathf.Clamp01(elapsed / 0.15f);
                    yield return null;
                }
                feedbackGroup.alpha = 1f;
            }

            yield return new WaitForSeconds(2.2f);

            // Fade out
            if (feedbackGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < 0.2f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    feedbackGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.2f);
                    yield return null;
                }
                feedbackGroup.alpha = 0f;
            }

            feedbackBanner.SetActive(false);
            feedbackRoutine = null;
        }

        public void ShowDistractionPopup(Action<bool> callback)
        {
            distractionCallback = callback;
            if (distractionModal != null)
            {
                distractionModal.SetActive(true);
            }
        }

        private void OnDistractionButtonPressed(bool choseIgnore)
        {
            if (distractionModal != null)
            {
                distractionModal.SetActive(false);
            }

            distractionCallback?.Invoke(choseIgnore);
            distractionCallback = null;
        }

        public void ShowCompletionDialog(int totalScore, int safetyScore, int safeActions, int violations, int missionsCompleted)
        {
            if (completionPanel == null) return;

            completionPanel.SetActive(true);

            if (completionTitleText != null) completionTitleText.text = "SAFE CITY DRIVER";
            if (completionSubtitleText != null) completionSubtitleText.text = "LEVEL COMPLETE!";

            if (finalScoreText != null) finalScoreText.text = $"{totalScore}";
            if (safetyScoreText != null) safetyScoreText.text = $"{safetyScore}%";
            if (safeActionsText != null) safeActionsText.text = $"{safeActions}";
            if (violationsText != null) violationsText.text = $"{violations}";
            if (missionsCompletedText != null) missionsCompletedText.text = $"{missionsCompleted} / 5";

            // Rating calculation
            if (starRatingText != null)
            {
                if (safetyScore >= 80 && violations <= 1)
                {
                    starRatingText.text = "★ ★ ★";
                    starRatingText.color = TrafficTownTheme.AccentColor;
                }
                else if (safetyScore >= 50)
                {
                    starRatingText.text = "★ ★ ☆";
                    starRatingText.color = TrafficTownTheme.AccentColor;
                }
                else
                {
                    starRatingText.text = "★ ☆ ☆";
                    starRatingText.color = TrafficTownTheme.AccentColor;
                }
            }

            StartCoroutine(AnimateCompletionIn());
        }

        private IEnumerator AnimateCompletionIn()
        {
            if (completionGroup != null)
            {
                completionGroup.alpha = 0f;
                float elapsed = 0f;
                while (elapsed < 0.35f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    completionGroup.alpha = Mathf.Clamp01(elapsed / 0.35f);
                    yield return null;
                }
                completionGroup.alpha = 1f;
            }
        }

        public void OnRetryClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnBackToMenuClicked()
        {
            Time.timeScale = 1f;
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadMainMenu();
            }
            else
            {
                SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
