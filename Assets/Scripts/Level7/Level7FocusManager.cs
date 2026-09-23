using System;
using UnityEngine;
using TrafficTown2D.Gameplay;

namespace TrafficTown2D.Level7
{
    public class Level7FocusManager : MonoBehaviour
    {
        public static Level7FocusManager Instance { get; private set; }

        [Header("State Values")]
        [SerializeField, Range(0f, 100f)] private float focus = 100f;
        [SerializeField, Range(0f, 100f)] private float safetyScore = 100f;
        [SerializeField] private int score = 100;

        [Header("Counters")]
        [SerializeField] private int distractionsIgnored = 0;
        [SerializeField] private int unsafeInteractions = 0;
        [SerializeField] private int safeStopsCount = 0;

        [Header("Thresholds")]
        [SerializeField] private float focusCriticalThreshold = 30f;

        private bool isCriticalNotified = false;
        private ScoreManager globalScoreManager;

        // Public Properties
        public float Focus => focus;
        public float SafetyScore => safetyScore;
        public int Score => score;
        public int DistractionsIgnored => distractionsIgnored;
        public int UnsafeInteractions => unsafeInteractions;
        public int SafeStopsCount => safeStopsCount;
        public bool IsFocusCritical => focus <= focusCriticalThreshold;

        // Events
        public event Action<float> FocusChanged;
        public event Action<float> SafetyChanged;
        public event Action<int> ScoreChanged;
        public event Action<string, bool> FeedbackTriggered; // (message, isPositive)
        public event Action FocusCriticalTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            globalScoreManager = FindAnyObjectByType<ScoreManager>();
            if (globalScoreManager != null)
            {
                score = globalScoreManager.CurrentScore;
            }
        }

        private void Start()
        {
            NotifyAll();
        }

        public void ResetMissionMetrics()
        {
            isCriticalNotified = false;
            // Focus recovers partially at start of new mission if low
            if (focus < 80f) focus = 85f;
            NotifyAll();
        }

        public void RecordDistractionIgnored(DistractionEventData data)
        {
            distractionsIgnored++;
            AddFocus(data != null ? data.ignoreRecoveryFocus : 2f);
            AddScore(data != null ? data.ignoreRewardScore : 10);

            FeedbackTriggered?.Invoke("[OK] GOOD FOCUS: You kept your attention on the road!", true);
        }

        public void RecordUnsafeInteraction(DistractionEventData data)
        {
            unsafeInteractions++;
            float fPen = data != null ? data.focusPenalty : 20f;
            float sPen = data != null ? data.safetyPenalty : 15f;
            int scPen = data != null ? data.scorePenalty : 50;

            DeductFocus(fPen);
            DeductSafety(sPen);
            DeductScore(scPen);

            string alert = data != null && data.type == DistractionType.PhoneMessage
                ? "[WARNING] DISTRACTION DETECTED: You used your phone while driving!"
                : "[WARNING] DISTRACTION DETECTED: Your attention left the road!";

            FeedbackTriggered?.Invoke(alert, false);
        }

        public void RecordSafeStopInteraction(DistractionEventData data)
        {
            safeStopsCount++;
            AddFocus(data != null ? data.safeStopRewardFocus : 5f);
            AddScore(data != null ? data.safeStopRewardScore : 20);

            FeedbackTriggered?.Invoke("[OK] SAFE STOP: You pulled over safely before interacting!", true);
        }

        public void RecordSafeAction(string reason, int bonusScore = 15)
        {
            AddScore(bonusScore);
            AddFocus(2f);
            FeedbackTriggered?.Invoke($"[OK] {reason}", true);
        }

        public void RecordViolation(string reason, int penaltyScore = 30, float safetyLoss = 10f)
        {
            DeductScore(penaltyScore);
            DeductSafety(safetyLoss);
            DeductFocus(8f);
            FeedbackTriggered?.Invoke($"[WARNING] {reason}", false);
        }

        private void AddFocus(float amount)
        {
            focus = Mathf.Clamp(focus + amount, 0f, 100f);
            if (focus > focusCriticalThreshold) isCriticalNotified = false;
            FocusChanged?.Invoke(focus);
        }

        private void DeductFocus(float amount)
        {
            focus = Mathf.Clamp(focus - amount, 0f, 100f);
            FocusChanged?.Invoke(focus);

            if (focus <= focusCriticalThreshold && !isCriticalNotified)
            {
                isCriticalNotified = true;
                FocusCriticalTriggered?.Invoke();
                FeedbackTriggered?.Invoke("[CRITICAL] FOCUS CRITICAL: Keep your eyes on the road!", false);
            }
        }

        private void DeductSafety(float amount)
        {
            safetyScore = Mathf.Clamp(safetyScore - amount, 10f, 100f);
            SafetyChanged?.Invoke(safetyScore);
        }

        private void AddScore(int amount)
        {
            score += amount;
            if (globalScoreManager != null) globalScoreManager.RewardSafeAction(amount);
            ScoreChanged?.Invoke(score);
        }

        private void DeductScore(int amount)
        {
            score = Mathf.Max(0, score - amount);
            if (globalScoreManager != null) globalScoreManager.PenalizeMistake(amount);
            ScoreChanged?.Invoke(score);
        }

        private void NotifyAll()
        {
            FocusChanged?.Invoke(focus);
            SafetyChanged?.Invoke(safetyScore);
            ScoreChanged?.Invoke(score);
        }
    }
}
