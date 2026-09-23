using System;
using UnityEngine;

namespace TrafficTown2D.Level6
{
    /// <summary>
    /// Manages scoring, general safety score, route selection score, hazard avoidance,
    /// and final report metrics for Level 6 (Extreme Road Conditions).
    /// </summary>
    public class Level6SafetyManager : MonoBehaviour
    {
        public static Level6SafetyManager Instance { get; private set; }

        [Header("Scores")]
        private int totalScore = 0;
        private float safetyScore = 100f;
        private float routeSafetyScore = 100f;

        [Header("Statistics")]
        private int collisionCount = 0;
        private int hazardsAvoided = 0;
        private int uTurnsCompleted = 0;
        private int safeActionsCount = 0;
        private int mistakesCount = 0;

        private float collisionCooldown = 0f;

        // Caps
        public float MaxSafetyCap => Mathf.Max(0f, 100f - (collisionCount * 25f));
        public int TotalScore => totalScore;
        public float SafetyScore => Mathf.Clamp(safetyScore, 0f, MaxSafetyCap);
        public float RouteSafetyScore => Mathf.Clamp(routeSafetyScore, 0f, 100f);
        public int CollisionCount => collisionCount;
        public int HazardsAvoided => hazardsAvoided;
        public int UTurnsCompleted => uTurnsCompleted;
        public int SafeActionsCount => safeActionsCount;
        public int MistakesCount => mistakesCount;

        // Events
        public event Action<int> ScoreChanged;
        public event Action<float> SafetyScoreChanged;
        public event Action<float> RouteSafetyChanged;
        public event Action<string> FeedbackTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (collisionCooldown > 0f)
            {
                collisionCooldown -= Time.deltaTime;
            }
        }

        public void AddScore(int delta)
        {
            totalScore = Mathf.Max(0, totalScore + delta);
            ScoreChanged?.Invoke(totalScore);
        }

        public void RegisterSafeAction(int scoreBonus, string description)
        {
            AddScore(scoreBonus);
            safeActionsCount++;
            // Gradual safe driving recovery capped by collisions
            safetyScore = Mathf.Min(MaxSafetyCap, safetyScore + 1f);
            SafetyScoreChanged?.Invoke(SafetyScore);
            if (!string.IsNullOrEmpty(description))
            {
                FeedbackTriggered?.Invoke($"[+] {description} (+{scoreBonus} pts)");
            }
        }

        public void RegisterHazardAvoided(string hazardName)
        {
            hazardsAvoided++;
            AddScore(25);
            routeSafetyScore = Mathf.Min(100f, routeSafetyScore + 5f);
            RouteSafetyChanged?.Invoke(RouteSafetyScore);
            FeedbackTriggered?.Invoke($"[SAFE] Hazard bypassed: {hazardName} safely navigated! (+25 pts)");
        }

        public void RegisterUTurnCompleted(bool clean)
        {
            uTurnsCompleted++;
            if (clean)
            {
                routeSafetyScore = Mathf.Min(100f, routeSafetyScore + 10f);
            }
            else
            {
                routeSafetyScore = Mathf.Max(0f, routeSafetyScore - 5f);
            }
            RouteSafetyChanged?.Invoke(RouteSafetyScore);
        }

        public void RegisterWrongWay(string reason)
        {
            mistakesCount++;
            routeSafetyScore = Mathf.Max(0f, routeSafetyScore - 15f);
            RouteSafetyChanged?.Invoke(RouteSafetyScore);
            safetyScore = Mathf.Max(0f, safetyScore - 10f);
            SafetyScoreChanged?.Invoke(SafetyScore);
            AddScore(-20);
            FeedbackTriggered?.Invoke($"[!] {reason} (-20 pts)");
        }

        public void RegisterMinorInfraction(string reason)
        {
            mistakesCount++;
            safetyScore = Mathf.Max(0f, safetyScore - 5f);
            SafetyScoreChanged?.Invoke(SafetyScore);
            AddScore(-10);
            FeedbackTriggered?.Invoke($"[!] {reason} (-10 pts)");
        }

        public void RegisterHazardMistake(string reason)
        {
            mistakesCount++;
            safetyScore = Mathf.Max(0f, safetyScore - 15f);
            routeSafetyScore = Mathf.Max(0f, routeSafetyScore - 12f);
            SafetyScoreChanged?.Invoke(SafetyScore);
            RouteSafetyChanged?.Invoke(RouteSafetyScore);
            AddScore(-15);
            FeedbackTriggered?.Invoke($"[!] {reason} (-15 pts)");
        }

        public void RegisterMinorHazardContact(string reason)
        {
            mistakesCount++;
            safetyScore = Mathf.Max(0f, safetyScore - 6f);
            SafetyScoreChanged?.Invoke(SafetyScore);
            AddScore(-10);
            FeedbackTriggered?.Invoke($"[!] {reason} (-10 pts)");
        }

        private float offRoadAccTimer = 0f;

        public void RegisterOffRoadTick(float dt)
        {
            offRoadAccTimer += dt;
            if (offRoadAccTimer >= 1.2f)
            {
                offRoadAccTimer = 0f;
                mistakesCount++;
                safetyScore = Mathf.Max(0f, safetyScore - 4f);
                SafetyScoreChanged?.Invoke(SafetyScore);
                AddScore(-5);
                FeedbackTriggered?.Invoke("[!] OFF-ROAD! Steered off marked roadway onto grass. (-5 pts)");
            }
        }

        public void RegisterSevereHazardImpact(string hazardType, float penalty)
        {
            mistakesCount++;
            safetyScore = Mathf.Max(0f, safetyScore - penalty);
            routeSafetyScore = Mathf.Max(0f, routeSafetyScore - (penalty * 0.8f));
            SafetyScoreChanged?.Invoke(SafetyScore);
            RouteSafetyChanged?.Invoke(RouteSafetyScore);
            AddScore(-15);
            FeedbackTriggered?.Invoke($"[DANGER] Drove into severe {hazardType}!");
        }

        public void RegisterCollision(string obstacleName)
        {
            if (collisionCooldown > 0f) return;
            collisionCooldown = 2.0f;

            collisionCount++;
            mistakesCount++;
            AddScore(-40);

            // Collisions permanently reduce attainable safety
            safetyScore = Mathf.Clamp(safetyScore - 25f, 0f, MaxSafetyCap);
            routeSafetyScore = Mathf.Max(0f, routeSafetyScore - 15f);

            SafetyScoreChanged?.Invoke(SafetyScore);
            RouteSafetyChanged?.Invoke(RouteSafetyScore);
            FeedbackTriggered?.Invoke($"[CRASH] Impact with {obstacleName}! Severe safety deduction.");

            if (safetyScore <= 0f)
            {
                if (Level6MissionManager.Instance != null)
                {
                    Level6MissionManager.Instance.FailMission("Safety Rating dropped to 0%! Vehicle disabled by collisions.");
                }
            }
        }

        public void TriggerGeneralFeedback(string message)
        {
            FeedbackTriggered?.Invoke(message);
        }

        public int CalculateStars()
        {
            float combined = (SafetyScore + RouteSafetyScore) * 0.5f;
            if (collisionCount == 0 && combined >= 85f) return 3;
            if (collisionCount <= 1 && combined >= 60f) return 2;
            if (combined >= 40f) return 1;
            return 0;
        }

        public void ResetMissionScores()
        {
            // Retain cumulative totals or keep progressive tracking
        }
    }
}
