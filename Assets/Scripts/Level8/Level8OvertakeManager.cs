using System;
using UnityEngine;
using TrafficTown2D.Core;
using TrafficTown2D.Level3;

namespace TrafficTown2D.Level8
{
    /// <summary>
    /// Master coordinator for Level 8 — THE OVERTAKE.
    /// Integrates mission flow, safety evaluations, scoring, feedback,
    /// and level completion/progression.
    /// </summary>
    public class Level8OvertakeManager : MonoBehaviour
    {
        public static Level8OvertakeManager Instance { get; private set; }

        [Header("System References")]
        [SerializeField] private Level3PlayerCar playerCar;
        [SerializeField] private OvertakeSafetyEvaluator evaluator;
        [SerializeField] private OvertakeScenarioController scenarioController;
        [SerializeField] private OvertakeMissionController missionController;
        [SerializeField] private Level8UIController uiController;

        [Header("Scoring & Telemetry")]
        [SerializeField] private int currentScore = 100;
        [SerializeField] private int safetyPercentage = 100;
        [SerializeField] private int completedOvertakes = 0;
        [SerializeField] private int safeWaitDecisions = 0;
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
            if (collisionCooldown > 0f) collisionCooldown -= Time.deltaTime;
        }

        private void FindDependencies()
        {
            if (playerCar == null) playerCar = FindAnyObjectByType<Level3PlayerCar>();
            if (evaluator == null) evaluator = FindAnyObjectByType<OvertakeSafetyEvaluator>();
            if (scenarioController == null) scenarioController = FindAnyObjectByType<OvertakeScenarioController>();
            if (missionController == null) missionController = FindAnyObjectByType<OvertakeMissionController>();
            if (uiController == null) uiController = FindAnyObjectByType<Level8UIController>();

            if (evaluator != null && playerCar != null)
            {
                evaluator.BindPlayer(playerCar);
            }
            if (missionController != null && playerCar != null && evaluator != null && scenarioController != null)
            {
                missionController.BindDependencies(playerCar, evaluator, scenarioController);
            }
        }

        private void SubscribeEvents()
        {
            if (evaluator != null)
            {
                evaluator.SafetyStateChanged += OnSafetyStateChanged;
                evaluator.UnsafeActionDetected += OnUnsafeAction;
                evaluator.SafeActionCompleted += OnSafeAction;
            }

            if (missionController != null)
            {
                missionController.MissionStarted += OnMissionStarted;
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
                evaluator.SafetyStateChanged -= OnSafetyStateChanged;
                evaluator.UnsafeActionDetected -= OnUnsafeAction;
                evaluator.SafeActionCompleted -= OnSafeAction;
            }

            if (missionController != null)
            {
                missionController.MissionStarted -= OnMissionStarted;
                missionController.StepProgressUpdated -= OnStepProgress;
            }

            if (playerCar != null)
            {
                playerCar.CarCollided -= OnCarCollided;
            }
        }

        #region Scoring & Decisions
        public void AddScore(int points, string reason)
        {
            currentScore += points;
            UpdateUI();
            if (!string.IsNullOrEmpty(reason) && uiController != null)
            {
                uiController.ShowFeedback($"+{points} {reason}", false);
            }
        }

        public void RecordSafeOvertake()
        {
            completedOvertakes++;
            AddScore(25, "Safe Overtake Completed!");
        }

        public void RecordSafeWaitDecision()
        {
            safeWaitDecisions++;
            AddScore(15, "Patience Rewarded! Waited for safe conditions.");
        }

        public void RecordMistake(int penalty, string reason)
        {
            mistakesCount++;
            currentScore = Mathf.Max(0, currentScore - penalty);
            safetyPercentage = Mathf.Max(20, safetyPercentage - 15);
            UpdateUI();

            if (uiController != null)
            {
                uiController.ShowFeedback($"[-{penalty}] {reason}", true);
            }
        }

        private void OnCarCollided(Collision2D collision)
        {
            if (collisionCooldown > 0f) return;
            collisionCooldown = 2.0f;

            if (collision != null && collision.collider != null)
            {
                if (collision.collider.name.Contains("Curb") || collision.collider.name.Contains("Wall"))
                {
                    RecordMistake(10, "CURB CONTACT! Keep your vehicle centered in the road.");
                    return;
                }
            }

            RecordMistake(25, "COLLISION! Always keep a safe distance from other vehicles.");
        }

        private void OnUnsafeAction(string reason)
        {
            RecordMistake(15, reason);
        }

        private void OnSafeAction(string feedback)
        {
            AddScore(10, feedback);
        }

        private void OnSafetyStateChanged(OvertakeSafetyState state, string reason)
        {
            if (uiController != null)
            {
                uiController.UpdateOvertakeIndicator(state, reason);
            }
        }

        private void OnMissionStarted(OvertakeMissionData mission)
        {
            if (uiController != null)
            {
                uiController.UpdateMissionInfo(mission.missionNumber, missionController.TotalMissions, mission.title, mission.objective);
                uiController.ShowFeedback($"Mission {mission.missionNumber}: {mission.title}", false);
            }
        }

        private void OnStepProgress(string stepInfo)
        {
            if (uiController != null)
            {
                uiController.UpdateChecklist(missionController.GetMission4ChecklistStatus());
                uiController.ShowFeedback(stepInfo, false);
            }
        }

        private void UpdateUI()
        {
            if (uiController != null)
            {
                uiController.UpdateScoreAndSafety(currentScore, safetyPercentage, completedOvertakes, safeWaitDecisions);
            }
        }
        #endregion

        #region Level Progression & Completion
        public void TriggerLevelCompletion()
        {
            Time.timeScale = 1f;

            // Save Level 8 completion and unlock Level 9
            PlayerPrefs.SetInt("Level8_Completed", 1);
            PlayerPrefs.SetInt("Level9_Unlocked", 1);
            PlayerPrefs.Save();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.LevelComplete);
            }

            if (uiController != null)
            {
                uiController.ShowCompletionModal(currentScore, safetyPercentage, completedOvertakes, safeWaitDecisions);
            }
        }

        public void RestartLevel()
        {
            currentScore = 100;
            safetyPercentage = 100;
            completedOvertakes = 0;
            safeWaitDecisions = 0;
            mistakesCount = 0;

            if (missionController != null)
            {
                missionController.StartMission(0);
            }
            UpdateUI();
        }
        #endregion
    }
}
