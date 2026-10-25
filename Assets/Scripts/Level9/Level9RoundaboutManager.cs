using System;
using UnityEngine;
using TrafficTown2D.Core;
using TrafficTown2D.Level3;

namespace TrafficTown2D.Level9
{
    /// <summary>
    /// Master coordinator for Level 9 — ROUNDABOUT RULES.
    /// Manages scoring, safety metrics, mission advancement, and level completion.
    /// </summary>
    public class Level9RoundaboutManager : MonoBehaviour
    {
        public static Level9RoundaboutManager Instance { get; private set; }

        [Header("System References")]
        [SerializeField] private Level3PlayerCar playerCar;
        [SerializeField] private RoundaboutSafetyEvaluator evaluator;
        [SerializeField] private RoundaboutMissionController missionController;
        [SerializeField] private Level9UIController uiController;

        [Header("Telemetry")]
        [SerializeField] private int currentScore = 100;
        [SerializeField] private int safetyPercentage = 100;
        [SerializeField] private int roundaboutsCleared = 0;
        [SerializeField] private int safeYieldsCount = 0;
        [SerializeField] private int mistakesCount = 0;

        private float collisionCooldown = 0f;

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
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }

            FindDependencies();
            SubscribeEvents();
            UpdateUI();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (collisionCooldown > 0f)
                collisionCooldown -= Time.deltaTime;
        }

        private void FindDependencies()
        {
            if (playerCar == null) playerCar = FindAnyObjectByType<Level3PlayerCar>();
            if (evaluator == null) evaluator = FindAnyObjectByType<RoundaboutSafetyEvaluator>();
            if (missionController == null) missionController = FindAnyObjectByType<RoundaboutMissionController>();
            if (uiController == null) uiController = FindAnyObjectByType<Level9UIController>();

            if (evaluator != null && playerCar != null)
            {
                evaluator.BindPlayer(playerCar);
            }

            if (missionController != null && playerCar != null && evaluator != null)
            {
                missionController.BindDependencies(playerCar, evaluator);
            }
        }

        private void SubscribeEvents()
        {
            if (evaluator != null)
            {
                evaluator.PillStatusChanged += OnPillStatusChanged;
                evaluator.OnSafetyViolation += OnSafetyViolation;
                evaluator.OnSafeActionReward += OnSafeReward;
            }

            if (missionController != null)
            {
                missionController.MissionStarted += OnMissionStarted;
                missionController.MissionCompleted += OnMissionCompleted;
                missionController.StepProgressUpdated += OnStepProgress;
            }

            if (playerCar != null)
            {
                playerCar.CarCollided += OnCarCollided;
            }
        }

        private void UnsubscribeEvents()
        {
            if (evaluator != null)
            {
                evaluator.PillStatusChanged -= OnPillStatusChanged;
                evaluator.OnSafetyViolation -= OnSafetyViolation;
                evaluator.OnSafeActionReward -= OnSafeReward;
            }

            if (missionController != null)
            {
                missionController.MissionStarted -= OnMissionStarted;
                missionController.MissionCompleted -= OnMissionCompleted;
                missionController.StepProgressUpdated -= OnStepProgress;
            }

            if (playerCar != null)
            {
                playerCar.CarCollided -= OnCarCollided;
            }
        }

        #region Scoring & Evaluation Methods
        public void AddScore(int points, string reason)
        {
            currentScore += points;
            UpdateUI();
            if (!string.IsNullOrEmpty(reason) && uiController != null)
            {
                uiController.ShowFeedback($"+{points} {reason}", false);
            }
        }

        public void RecordSafeYield()
        {
            safeYieldsCount++;
            AddScore(15, "Yielded to circulating traffic!");
        }

        public void RecordSafeExitSignal()
        {
            AddScore(15, "Signaled exit in advance!");
        }

        public void RecordRoundaboutCleared(int num)
        {
            if (num > roundaboutsCleared)
            {
                roundaboutsCleared = num;
                AddScore(20, $"Roundabout {num} Cleared!");
            }
        }

        public void RecordMistake(int penalty, string reason)
        {
            mistakesCount++;
            currentScore = Mathf.Max(0, currentScore - penalty);
            safetyPercentage = Mathf.Max(15, safetyPercentage - 15);
            UpdateUI();

            if (uiController != null)
            {
                uiController.ShowFeedback($"[-{penalty}] {reason}", true);
            }
        }

        public void RecordCollisionWithCar()
        {
            if (collisionCooldown > 0f) return;
            collisionCooldown = 2.0f;

            RecordMistake(25, "COLLISION! Maintain safe distance inside roundabouts.");

            Level9CameraFollow camFollow = FindAnyObjectByType<Level9CameraFollow>();
            if (camFollow != null) camFollow.TriggerShake(0.35f, 0.4f);
        }

        private void OnCarCollided(Collision2D col)
        {
            if (collisionCooldown > 0f) return;
            collisionCooldown = 2.0f;

            RecordMistake(20, "COLLISION! Watch road curbs and surrounding vehicles.");

            Level9CameraFollow camFollow = FindAnyObjectByType<Level9CameraFollow>();
            if (camFollow != null) camFollow.TriggerShake(0.3f, 0.35f);
        }

        private void OnSafetyViolation(string message)
        {
            // Already reported with penalty
        }

        private void OnSafeReward(string message)
        {
            // Already awarded
        }

        private void OnPillStatusChanged(RoundaboutPillStatus status, string message)
        {
            if (uiController != null)
            {
                uiController.UpdateStatusPill(status, message);
            }
        }

        private void OnMissionStarted(RoundaboutMissionData mission)
        {
            if (uiController != null)
            {
                uiController.UpdateMissionInfo(mission.missionNumber, missionController.TotalMissions, mission.title, mission.objective);
                uiController.ShowFeedback($"Mission {mission.missionNumber}: {mission.title}", false);
            }
        }

        private void OnMissionCompleted(int missionNum, string title)
        {
            if (missionNum <= 3)
            {
                RecordRoundaboutCleared(missionNum);
            }
        }

        private void OnStepProgress(string message)
        {
            if (uiController != null)
            {
                uiController.ShowFeedback(message, false);
            }
        }

        private void UpdateUI()
        {
            if (uiController != null)
            {
                uiController.UpdateScoreAndSafety(currentScore, safetyPercentage, roundaboutsCleared, safeYieldsCount);
            }
        }
        #endregion

        #region Level Progression
        public void TriggerLevelCompletion()
        {
            roundaboutsCleared = 3;
            Time.timeScale = 1f;

            PlayerPrefs.SetInt("Level9_Completed", 1);
            PlayerPrefs.SetInt("Level10_Unlocked", 1);
            PlayerPrefs.Save();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.LevelComplete);
            }

            if (uiController != null)
            {
                uiController.ShowCompletionModal(currentScore, safetyPercentage, roundaboutsCleared, safeYieldsCount);
            }
        }
        #endregion
    }
}
