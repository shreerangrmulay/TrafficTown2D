using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrafficTown2D.Core;

namespace TrafficTown2D.Level8
{
    /// <summary>
    /// Manages the visual UI for Level 8 (Mission Card, Safety Meter,
    /// Overtaking Status Pill, Feedback Banner, Controls Card, and Completion Modal).
    /// </summary>
    public class Level8UIController : MonoBehaviour
    {
        [Header("Mission Card (Top-Left)")]
        [SerializeField] private TMP_Text missionBadge;
        [SerializeField] private TMP_Text missionTitle;
        [SerializeField] private TMP_Text missionObjective;
        [SerializeField] private TMP_Text missionChecklistText;

        [Header("Score Card (Top-Right)")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text safetyText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private Image safetyBarFill;

        [Header("Overtake Status Pill (Top-Center)")]
        [SerializeField] private GameObject statusPillRoot;
        [SerializeField] private Image statusPillBg;
        [SerializeField] private TMP_Text statusTitleText;
        [SerializeField] private TMP_Text statusReasonText;

        [Header("Feedback Banner (Bottom-Center)")]
        [SerializeField] private GameObject feedbackBanner;
        [SerializeField] private CanvasGroup feedbackGroup;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Controls Card (Bottom-Left)")]
        [SerializeField] private GameObject controlsCard;

        [Header("Completion Modal (Overlay)")]
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

        // Styling Colors
        private static readonly Color StatusGreen = new Color(0.15f, 0.75f, 0.35f, 0.95f);
        private static readonly Color StatusAmber = new Color(0.95f, 0.70f, 0.10f, 0.95f);
        private static readonly Color StatusRed = new Color(0.90f, 0.22f, 0.20f, 0.95f);

        private Coroutine feedbackRoutine;

        private void Awake()
        {
            if (completionModal != null) completionModal.SetActive(false);
            if (feedbackGroup != null) feedbackGroup.alpha = 0f;
            EnsureButtonListeners();
        }

        private void EnsureButtonListeners()
        {
            if (replayButton != null)
            {
                replayButton.onClick.RemoveListener(OnReplayClicked);
                replayButton.onClick.AddListener(OnReplayClicked);
            }
            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveListener(OnNextClicked);
                nextLevelButton.onClick.AddListener(OnNextClicked);
            }
            if (menuButton != null)
            {
                menuButton.onClick.RemoveListener(OnMenuClicked);
                menuButton.onClick.AddListener(OnMenuClicked);
            }
        }

        #region UI Updates
        public void UpdateMissionInfo(int number, int total, string title, string objective)
        {
            if (missionBadge != null) missionBadge.text = $"MISSION {number} / {total}";
            if (missionTitle != null) missionTitle.text = title;
            if (missionObjective != null) missionObjective.text = objective;
            if (missionChecklistText != null) missionChecklistText.text = string.Empty;
        }

        public void UpdateChecklist(string text)
        {
            if (missionChecklistText != null)
            {
                missionChecklistText.text = text;
            }
        }

        public void UpdateScoreAndSafety(int score, int safetyPercent, int overtakes, int safeWaits)
        {
            if (scoreText != null) scoreText.text = $"SCORE: {score}";
            if (safetyText != null) safetyText.text = $"SAFETY: {safetyPercent}%";
            if (statsText != null) statsText.text = $"Overtakes: {overtakes}  |  Safe Waits: {safeWaits}";
            if (safetyBarFill != null) safetyBarFill.fillAmount = Mathf.Clamp01(safetyPercent / 100f);
        }

        public void UpdateOvertakeIndicator(OvertakeSafetyState state, string reason)
        {
            if (statusPillBg == null) return;

            switch (state)
            {
                case OvertakeSafetyState.SafeToOvertake:
                    statusPillBg.color = StatusGreen;
                    if (statusTitleText != null) statusTitleText.text = "[SAFE TO OVERTAKE]";
                    break;
                case OvertakeSafetyState.Wait:
                    statusPillBg.color = StatusAmber;
                    if (statusTitleText != null) statusTitleText.text = "[WAIT FOR SAFE GAP]";
                    break;
                case OvertakeSafetyState.Prohibited:
                    statusPillBg.color = StatusRed;
                    if (statusTitleText != null) statusTitleText.text = "[NO OVERTAKING]";
                    break;
            }

            if (statusReasonText != null)
            {
                statusReasonText.text = reason;
            }
        }

        public void ShowFeedback(string message, bool isWarning = false)
        {
            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
            feedbackRoutine = StartCoroutine(FeedbackBannerRoutine(message, isWarning));
        }

        private IEnumerator FeedbackBannerRoutine(string message, bool isWarning)
        {
            if (feedbackBanner != null) feedbackBanner.SetActive(true);
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = isWarning ? new Color(1f, 0.45f, 0.40f, 1f) : Color.white;
            }

            // Fade In
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                if (feedbackGroup != null) feedbackGroup.alpha = t / 0.2f;
                yield return null;
            }
            if (feedbackGroup != null) feedbackGroup.alpha = 1f;

            yield return new WaitForSeconds(3.2f);

            // Fade Out
            t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                if (feedbackGroup != null) feedbackGroup.alpha = 1f - (t / 0.3f);
                yield return null;
            }
            if (feedbackGroup != null) feedbackGroup.alpha = 0f;
        }

        public void ShowCompletionModal(int finalScore, int safetyPercent, int overtakes, int safeWaits)
        {
            if (completionModal == null) return;
            completionModal.SetActive(true);

            int stars = (finalScore >= 120 && safetyPercent >= 85) ? 3 : (finalScore >= 80 ? 2 : 1);
            string starStr = new string('★', stars) + new string('☆', 3 - stars);

            if (completionTitle != null) completionTitle.text = "LEVEL 8 COMPLETED!";
            if (completionSubtitle != null) completionSubtitle.text = "Don't just overtake. Overtake safely.";
            if (completionStars != null) completionStars.text = starStr;
            if (completionScore != null) completionScore.text = $"FINAL SCORE: {finalScore}";
            if (completionStats != null)
            {
                completionStats.text = $"Safety Rating: {safetyPercent}%\n" +
                                      $"Safe Overtakes Completed: {overtakes}\n" +
                                      $"Patient Wait Decisions: {safeWaits}";
            }

            StartCoroutine(FadeInGroup(completionGroup, 0.4f));
        }

        private IEnumerator FadeInGroup(CanvasGroup cg, float dur)
        {
            if (cg == null) yield break;
            cg.alpha = 0f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                cg.alpha = t / dur;
                yield return null;
            }
            cg.alpha = 1f;
        }
        #endregion

        #region Button Actions
        private void OnReplayClicked()
        {
            if (Level8OvertakeManager.Instance != null)
            {
                Level8OvertakeManager.Instance.RestartLevel();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Level8");
            }
        }

        private void OnNextClicked()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene("Level9");
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("Level9");
        }

        private void OnMenuClicked()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene("MainMenu");
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
        #endregion
    }
}
