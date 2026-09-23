using System;
using UnityEngine;

namespace TrafficTown2D.Level5
{
    public class Level5SafetyManager : MonoBehaviour
    {
        public static Level5SafetyManager Instance { get; private set; }

        private int score = 0;
        private float safetyScore = 100f;
        private float trafficFlowScore = 100f;
        private int collisionCount = 0;
        private int mistakesCount = 0;
        private int safeActionsCount = 0;

        private float collisionCooldown = 0f;

        public int Score => score;
        public float MaxSafetyCap => Mathf.Max(0f, 100f - (collisionCount * 25f));
        public float SafetyScore => Mathf.Clamp(safetyScore, 0f, MaxSafetyCap);
        public float TrafficFlowScore => Mathf.Clamp(trafficFlowScore, 0f, 100f);
        public int CollisionCount => collisionCount;
        public int MistakesCount => mistakesCount;
        public int SafeActionsCount => safeActionsCount;

        public event Action<int> ScoreChanged;
        public event Action<float> SafetyScoreChanged;
        public event Action<float> TrafficFlowChanged;
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

        private void Start()
        {
            if (TrafficPhaseController.Instance != null)
            {
                TrafficPhaseController.Instance.SafePhaseChangeCompleted += OnSafePhaseChange;
            }
            if (TrafficQueueManager.Instance != null)
            {
                TrafficQueueManager.Instance.VehicleCleared += OnVehicleCleared;
                TrafficQueueManager.Instance.QueueWarningTriggered += OnQueueWarning;
            }
            if (PedestrianIntersectionManager.Instance != null)
            {
                PedestrianIntersectionManager.Instance.PedestrianCrossed += OnPedestrianCrossed;
                PedestrianIntersectionManager.Instance.PedestrianWarningTriggered += OnPedestrianWarning;
            }
            if (EmergencyVehicleManager.Instance != null)
            {
                EmergencyVehicleManager.Instance.EmergencyResolved += OnEmergencyResolved;
                EmergencyVehicleManager.Instance.EmergencyAlertTriggered += OnEmergencyAlert;
            }
        }

        private void Update()
        {
            collisionCooldown -= Time.deltaTime;

            // Dynamically calculate traffic flow score based on current congestion
            if (TrafficQueueManager.Instance != null)
            {
                float congestion = TrafficQueueManager.Instance.CongestionRatio;
                float targetFlow = Mathf.Lerp(100f, 30f, congestion);
                trafficFlowScore = Mathf.MoveTowards(trafficFlowScore, targetFlow, 5f * Time.deltaTime);
                TrafficFlowChanged?.Invoke(TrafficFlowScore);
            }
        }

        public void OnSafePhaseChange()
        {
            AddScore(20);
            safeActionsCount++;
            // Minor safety recovery only up to MaxSafetyCap
            safetyScore = Mathf.Min(MaxSafetyCap, safetyScore + 0.5f);
            SafetyScoreChanged?.Invoke(SafetyScore);
            FeedbackTriggered?.Invoke("✓ Safe phase transition completed.");
        }

        public void OnVehicleCleared(int total)
        {
            AddScore(10);
            safeActionsCount++;
        }

        public void OnPedestrianCrossed(int total)
        {
            AddScore(20);
            safeActionsCount++;
            // Pedestrians award score without erasing collision penalties
            safetyScore = Mathf.Min(MaxSafetyCap, safetyScore + 0.2f);
            SafetyScoreChanged?.Invoke(SafetyScore);
            FeedbackTriggered?.Invoke("✓ Pedestrians crossed safely.");
        }

        public void OnEmergencyResolved(bool fast)
        {
            if (fast)
            {
                AddScore(35);
                safeActionsCount++;
                safetyScore = Mathf.Min(MaxSafetyCap, safetyScore + 2f);
            }
            else
            {
                AddScore(10);
                mistakesCount++;
                safetyScore = Mathf.Max(0f, safetyScore - 10f);
            }
            SafetyScoreChanged?.Invoke(SafetyScore);
        }

        public void RegisterCollision(TrafficIntersectionVehicle v)
        {
            if (collisionCooldown > 0f) return;
            collisionCooldown = 2.5f;

            collisionCount++;
            mistakesCount++;
            AddScore(-50);

            // Collisions permanently drop safety rating and cap maximum attainable safety
            safetyScore = Mathf.Clamp(safetyScore - 25f, 0f, MaxSafetyCap);

            SafetyScoreChanged?.Invoke(SafetyScore);
            FeedbackTriggered?.Invoke("💥 Traffic collision! Tow truck clearing accident (Press [C] for quick clear)!");

            if (safetyScore <= 0f)
            {
                if (Level5MissionManager.Instance != null)
                {
                    Level5MissionManager.Instance.FailMission("Safety Rating dropped to 0%! Multiple traffic collisions occurred.");
                }
            }
        }

        public void OnQueueWarning(string msg)
        {
            FeedbackTriggered?.Invoke($"⚠️ {msg}");
        }

        public void OnPedestrianWarning(string msg)
        {
            FeedbackTriggered?.Invoke($"⚠️ {msg}");
            safetyScore = Mathf.Max(0f, safetyScore - 4f);
            SafetyScoreChanged?.Invoke(SafetyScore);
        }

        private void OnEmergencyAlert(string msg)
        {
            FeedbackTriggered?.Invoke(msg);
        }

        public void AddScore(int amount)
        {
            score = Mathf.Max(0, score + amount);
            ScoreChanged?.Invoke(score);
        }

        public void ResetScores()
        {
            score = 0;
            safetyScore = 100f;
            trafficFlowScore = 100f;
            collisionCount = 0;
            mistakesCount = 0;
            safeActionsCount = 0;
            collisionCooldown = 0f;
            ScoreChanged?.Invoke(score);
            SafetyScoreChanged?.Invoke(safetyScore);
            TrafficFlowChanged?.Invoke(trafficFlowScore);
        }
    }
}
