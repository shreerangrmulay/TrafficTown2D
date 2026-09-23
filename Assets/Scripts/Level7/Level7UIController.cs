using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrafficTown2D.Level7
{
    public class Level7UIController : MonoBehaviour
    {
        public static Level7UIController Instance { get; private set; }

        [Header("Top Left - Mission Info")]
        [SerializeField] private TMP_Text missionNumberText;
        [SerializeField] private TMP_Text missionTitleText;
        [SerializeField] private TMP_Text missionObjectiveText;

        [Header("Top Right - Performance & Focus HUD")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text safetyText;
        [SerializeField] private TMP_Text focusText;
        [SerializeField] private Image focusMeterFill;
        [SerializeField] private TMP_Text ignoredCountText;
        [SerializeField] private TMP_Text unsafeCountText;

        [Header("Top Center - Feedback & Safe Stop Banners")]
        [SerializeField] private GameObject safeStopBanner;
        [SerializeField] private TMP_Text safeStopBannerText;
        [SerializeField] private GameObject feedbackBanner;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Image feedbackBg;

        [Header("Bottom UI")]
        [SerializeField] private Level7MiniMapController miniMap;
        [SerializeField] private Level7DistractionUI distractionUI;
        [SerializeField] private Level7CompletionModal completionModal;

        [Header("Colors")]
        [SerializeField] private Color goodFeedbackColor = new Color(0.16f, 0.70f, 0.32f, 0.95f);
        [SerializeField] private Color badFeedbackColor = new Color(0.88f, 0.22f, 0.20f, 0.95f);
        [SerializeField] private Color focusHighColor = new Color(0.18f, 0.85f, 0.40f, 1f);
        [SerializeField] private Color focusMidColor = new Color(0.95f, 0.75f, 0.15f, 1f);
        [SerializeField] private Color focusLowColor = new Color(0.92f, 0.25f, 0.20f, 1f);

        private Coroutine activeFeedbackRoutine;

        public Level7DistractionUI DistractionUI => distractionUI;
        public Level7MiniMapController MiniMap => miniMap;
        public Level7CompletionModal CompletionModal => completionModal;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (safeStopBanner != null) safeStopBanner.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);
        }

        private void Start()
        {
            if (safeStopBanner != null) safeStopBanner.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);

            if (Level7FocusManager.Instance != null)
            {
                Level7FocusManager.Instance.FocusChanged += UpdateFocusDisplay;
                Level7FocusManager.Instance.SafetyChanged += UpdateSafetyDisplay;
                Level7FocusManager.Instance.ScoreChanged += UpdateScoreDisplay;
                Level7FocusManager.Instance.FeedbackTriggered += ShowFeedback;

                UpdateFocusDisplay(Level7FocusManager.Instance.Focus);
                UpdateSafetyDisplay(Level7FocusManager.Instance.SafetyScore);
                UpdateScoreDisplay(Level7FocusManager.Instance.Score);
            }

            Level7SafeStopZone.AnySafeStopChanged += OnSafeStopChanged;
        }

        private void OnDestroy()
        {
            if (Level7FocusManager.Instance != null)
            {
                Level7FocusManager.Instance.FocusChanged -= UpdateFocusDisplay;
                Level7FocusManager.Instance.SafetyChanged -= UpdateSafetyDisplay;
                Level7FocusManager.Instance.ScoreChanged -= UpdateScoreDisplay;
                Level7FocusManager.Instance.FeedbackTriggered -= ShowFeedback;
            }

            Level7SafeStopZone.AnySafeStopChanged -= OnSafeStopChanged;
        }

        public void SetMission(int index, int totalMissions, string title, string objective)
        {
            if (missionNumberText != null) missionNumberText.text = $"MISSION {index} / {totalMissions}";
            if (missionTitleText != null) missionTitleText.text = title.ToUpper();
            if (missionObjectiveText != null) missionObjectiveText.text = objective;
        }

        public void ShowFeedback(string message, bool isPositive)
        {
            if (feedbackBanner == null || feedbackText == null) return;

            if (activeFeedbackRoutine != null) StopCoroutine(activeFeedbackRoutine);
            activeFeedbackRoutine = StartCoroutine(FeedbackRoutine(message, isPositive));
        }

        private IEnumerator FeedbackRoutine(string message, bool isPositive)
        {
            feedbackBanner.SetActive(true);
            feedbackText.text = message;
            if (feedbackBg != null) feedbackBg.color = isPositive ? goodFeedbackColor : badFeedbackColor;

            yield return new WaitForSeconds(3.5f);

            feedbackBanner.SetActive(false);
            activeFeedbackRoutine = null;
        }

        private void OnSafeStopChanged(bool isSafe, string zoneName)
        {
            if (safeStopBanner != null)
            {
                safeStopBanner.SetActive(isSafe);
                if (safeStopBannerText != null)
                {
                    safeStopBannerText.text = isSafe ? "[SAFE BAY] SAFE TO CHECK: Vehicle Stopped in Bay" : string.Empty;
                }
            }
        }

        public void ShowCompletionModal(int score, float safety, float focus, int ignored, int unsafeCount, int safeStops)
        {
            if (safeStopBanner != null) safeStopBanner.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);
            if (distractionUI != null) distractionUI.HideDistraction();

            if (completionModal == null)
            {
                completionModal = FindAnyObjectByType<Level7CompletionModal>(FindObjectsInactive.Include);
            }

            if (completionModal != null)
            {
                completionModal.Show(score, safety, focus, ignored, unsafeCount, safeStops);
            }
            else
            {
                Debug.LogError("[Level7UIController] CompletionModal not found!");
            }
        }

        public void UpdateCounters(int ignored, int unsafeCount)
        {
            if (ignoredCountText != null) ignoredCountText.text = ignored.ToString();
            if (unsafeCountText != null) unsafeCountText.text = unsafeCount.ToString();
        }

        private void UpdateScoreDisplay(int score)
        {
            if (scoreText != null) scoreText.text = $"SCORE: {score}";
            if (Level7FocusManager.Instance != null)
            {
                UpdateCounters(Level7FocusManager.Instance.DistractionsIgnored, Level7FocusManager.Instance.UnsafeInteractions);
            }
        }

        private void UpdateSafetyDisplay(float safety)
        {
            if (safetyText != null) safetyText.text = $"SAFETY: {Mathf.RoundToInt(safety)}%";
        }

        private void UpdateFocusDisplay(float focus)
        {
            if (focusText != null) focusText.text = $"FOCUS: {Mathf.RoundToInt(focus)}%";
            if (focusMeterFill != null)
            {
                float fill = Mathf.Clamp01(focus / 100f);
                focusMeterFill.fillAmount = fill;
                if (fill > 0.6f) focusMeterFill.color = focusHighColor;
                else if (fill > 0.3f) focusMeterFill.color = focusMidColor;
                else focusMeterFill.color = focusLowColor;
            }
        }
    }
}
