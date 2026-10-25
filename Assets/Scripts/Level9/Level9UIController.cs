using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TrafficTown2D.Core;

namespace TrafficTown2D.Level9
{
    /// <summary>
    /// UI Controller for Level 9 — ROUNDABOUT RULES.
    /// Manages Mission HUD, Score/Safety telemetry, Roundabout Status Pill,
    /// Feedback Banners, and Level Completion Modal.
    /// </summary>
    public class Level9UIController : MonoBehaviour
    {
        [Header("Mission Card")]
        [SerializeField] private TMP_Text missionBadge;
        [SerializeField] private TMP_Text missionTitle;
        [SerializeField] private TMP_Text missionObjective;
        [SerializeField] private TMP_Text missionChecklistText;

        [Header("Score & Telemetry Card")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text safetyText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private Image safetyBarFill;

        [Header("Roundabout Status Pill")]
        [SerializeField] private GameObject statusPillRoot;
        [SerializeField] private Image statusPillBg;
        [SerializeField] private TMP_Text statusTitleText;
        [SerializeField] private TMP_Text statusReasonText;

        [Header("Controls Card")]
        [SerializeField] private GameObject controlsCard;

        [Header("Feedback Banner")]
        [SerializeField] private GameObject feedbackBanner;
        [SerializeField] private CanvasGroup feedbackGroup;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Completion Modal")]
        [SerializeField] private GameObject completionModal;
        [SerializeField] private CanvasGroup completionGroup;
        [SerializeField] private TMP_Text completionTitle;
        [SerializeField] private TMP_Text completionSubtitle;
        [SerializeField] private TMP_Text completionStars;
        [SerializeField] private TMP_Text completionScore;
        [SerializeField] private TMP_Text completionStats;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button menuButton;

        private Coroutine feedbackRoutine;

        private readonly Color safeGreen = new Color(0.15f, 0.75f, 0.35f, 0.95f);
        private readonly Color cautionAmber = new Color(0.95f, 0.65f, 0.15f, 0.95f);
        private readonly Color dangerRed = new Color(0.90f, 0.20f, 0.20f, 0.95f);
        private readonly Color infoBlue = new Color(0.18f, 0.52f, 0.90f, 0.95f);

        private void Awake()
        {
            BindButtons();

            if (completionModal != null)
                completionModal.SetActive(false);

            if (feedbackGroup != null)
                feedbackGroup.alpha = 0f;
        }

        private void Start()
        {
            BindButtons();
        }

        public void BindButtons()
        {
            if (replayButton != null)
            {
                replayButton.onClick.RemoveListener(OnReplayClicked);
                replayButton.onClick.AddListener(OnReplayClicked);
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveListener(OnNextLevelClicked);
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            }

            if (menuButton != null)
            {
                menuButton.onClick.RemoveListener(OnMenuClicked);
                menuButton.onClick.AddListener(OnMenuClicked);
            }
        }

        public void UpdateMissionInfo(int current, int total, string title, string objective)
        {
            if (missionBadge != null)
                missionBadge.text = $"MISSION {current} / {total}";

            if (missionTitle != null)
                missionTitle.text = title.ToUpper();

            if (missionObjective != null)
                missionObjective.text = objective;
        }

        public void UpdateChecklist(string text)
        {
            if (missionChecklistText != null)
                missionChecklistText.text = text;
        }

        public void UpdateScoreAndSafety(int score, int safetyPercent, int roundaboutsCleared, int safeYields)
        {
            if (scoreText != null)
                scoreText.text = $"SCORE: {score}";

            if (safetyText != null)
                safetyText.text = $"SAFETY: {safetyPercent}%";

            if (safetyBarFill != null)
            {
                safetyBarFill.fillAmount = Mathf.Clamp01(safetyPercent / 100f);
                safetyBarFill.color = safetyPercent > 70 ? safeGreen : (safetyPercent > 40 ? cautionAmber : dangerRed);
            }

            if (statsText != null)
                statsText.text = $"Roundabouts: {roundaboutsCleared}/3  |  Safe Yields: {safeYields}";
        }

        public void UpdateStatusPill(RoundaboutPillStatus status, string message)
        {
            if (statusPillRoot == null) return;

            switch (status)
            {
                case RoundaboutPillStatus.Clear:
                    if (statusPillBg != null) statusPillBg.color = safeGreen;
                    if (statusTitleText != null) statusTitleText.text = "[ROAD CLEAR]";
                    break;

                case RoundaboutPillStatus.SlowDownApproach:
                    if (statusPillBg != null) statusPillBg.color = cautionAmber;
                    if (statusTitleText != null) statusTitleText.text = "[REDUCE SPEED]";
                    break;

                case RoundaboutPillStatus.GiveWayTraffic:
                    if (statusPillBg != null) statusPillBg.color = cautionAmber;
                    if (statusTitleText != null) statusTitleText.text = "[GIVE WAY TO RIGHT]";
                    break;

                case RoundaboutPillStatus.SafeGapMerge:
                    if (statusPillBg != null) statusPillBg.color = safeGreen;
                    if (statusTitleText != null) statusTitleText.text = "[SAFE GAP — MERGE NOW]";
                    break;

                case RoundaboutPillStatus.WrongWay:
                    if (statusPillBg != null) statusPillBg.color = dangerRed;
                    if (statusTitleText != null) statusTitleText.text = "[WRONG WAY! CIRCULATE CLOCKWISE]";
                    break;

                case RoundaboutPillStatus.IndicateExit:
                    if (statusPillBg != null) statusPillBg.color = infoBlue;
                    if (statusTitleText != null) statusTitleText.text = "[INDICATE BEFORE EXIT]";
                    break;

                case RoundaboutPillStatus.YieldPedestrian:
                    if (statusPillBg != null) statusPillBg.color = cautionAmber;
                    if (statusTitleText != null) statusTitleText.text = "[YIELD TO PEDESTRIAN]";
                    break;
            }

            if (statusReasonText != null)
                statusReasonText.text = message;
        }

        public void ShowFeedback(string text, bool isWarning = false)
        {
            if (feedbackText != null)
            {
                feedbackText.text = text;
                feedbackText.color = isWarning ? new Color(1f, 0.4f, 0.4f, 1f) : Color.white;
            }

            if (feedbackRoutine != null)
                StopCoroutine(feedbackRoutine);

            feedbackRoutine = StartCoroutine(AnimateFeedbackBanner());
        }

        private IEnumerator AnimateFeedbackBanner()
        {
            if (feedbackGroup == null) yield break;

            feedbackBanner.SetActive(true);
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                feedbackGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.2f);
                yield return null;
            }
            feedbackGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(2.5f);

            elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                feedbackGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.3f);
                yield return null;
            }
            feedbackGroup.alpha = 0f;
        }

        public void ShowCompletionModal(int finalScore, int safetyPercent, int roundaboutsCleared, int safeYields)
        {
            if (completionModal == null) return;

            completionModal.SetActive(true);

            int stars = 1;
            if (safetyPercent >= 85 && finalScore >= 140) stars = 3;
            else if (safetyPercent >= 60 && finalScore >= 90) stars = 2;

            string starStr = stars == 3 ? "RATING: 3 / 3 STARS (PERFECT)" : (stars == 2 ? "RATING: 2 / 3 STARS (GREAT)" : "RATING: 1 / 3 STARS (PASSED)");

            if (completionStars != null) completionStars.text = starStr;
            if (completionScore != null) completionScore.text = $"FINAL SCORE: {finalScore}";
            if (completionStats != null)
                completionStats.text = $"Safety: {safetyPercent}% | Roundabouts: {roundaboutsCleared}/3 | Safe Yields: {safeYields}";

            if (completionGroup != null)
            {
                StartCoroutine(FadeInModal(completionGroup));
            }
        }

        private IEnumerator FadeInModal(CanvasGroup group)
        {
            group.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.3f);
                yield return null;
            }
            group.alpha = 1f;
        }

        private void OnReplayClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnNextLevelClicked()
        {
            Time.timeScale = 1f;
            // Load Level 10 or MainMenu if 10 not available
            if (Application.CanStreamedLevelBeLoaded("Level10"))
            {
                SceneManager.LoadScene("Level10");
            }
            else
            {
                SceneManager.LoadScene("MainMenu");
            }
        }

        private void OnMenuClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
