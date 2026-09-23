using System;
using System.Collections;
using UnityEngine;
using TrafficTown2D.Core;

namespace TrafficTown2D.Level6
{
    public class Level6MissionManager : MonoBehaviour
    {
        public static Level6MissionManager Instance { get; private set; }

        [Header("Scene References")]
        [SerializeField] private Level6PlayerCar playerCar;
        [SerializeField] private Level6Destination destinationGoal;
        [SerializeField] private WeatherController weatherController;
        [SerializeField] private EnvironmentalHazardController hazardController;
        [SerializeField] private Level6UIController uiController;

        // Mission State
        private Level6MissionConfig[] missions;
        private int currentMissionIndex = 1; // 1 to 5
        private Level6MissionConfig currentConfig;
        private float timeRemaining = 0f;
        private bool isMissionRunning = false;
        private bool isLevelCompleted = false;

        public int CurrentMissionIndex => currentMissionIndex;
        public Level6MissionConfig CurrentConfig => currentConfig;
        public float TimeRemaining => Mathf.Max(0f, timeRemaining);
        public bool IsLevelCompleted => isLevelCompleted;
        public Level6PlayerCar PlayerCar => playerCar;

        // Events
        public event Action<Level6MissionConfig> MissionStarted;
        public event Action<Level6MissionConfig, int> MissionCompleted;
        public event Action<string> MissionFailed;
        public event Action LevelCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (playerCar == null)
            {
                playerCar = FindFirstObjectByType<Level6PlayerCar>();
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }
            Time.timeScale = 1f;

            missions = Level6MissionDatabase.GetMissions();

            if (destinationGoal != null)
            {
                destinationGoal.PlayerArrived += OnDestinationReached;
            }

            StartCoroutine(InitializeFirstMission());
        }

        private void OnDestroy()
        {
            if (destinationGoal != null)
            {
                destinationGoal.PlayerArrived -= OnDestinationReached;
            }
        }

        private IEnumerator InitializeFirstMission()
        {
            yield return null; // Wait 1 frame for all Singletons to initialize
            StartMission(1);
        }

        private void Update()
        {
            if (!isMissionRunning || isLevelCompleted) return;

            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                FailMission("Time expired! Extreme conditions delayed the journey too long.");
            }
        }

        public void StartMission(int missionNumber)
        {
            if (missionNumber < 1 || missionNumber > missions.Length) return;

            currentMissionIndex = missionNumber;
            currentConfig = missions[missionNumber - 1];
            timeRemaining = currentConfig.timeLimitSeconds;
            isMissionRunning = true;

            // 1. Position player car
            if (playerCar != null)
            {
                playerCar.Teleport(currentConfig.playerSpawnPosition, currentConfig.playerSpawnHeading);
            }

            // 2. Position Destination
            if (destinationGoal != null)
            {
                destinationGoal.SetDestination(currentConfig.destinationPosition, currentConfig.destinationLabel);
                destinationGoal.gameObject.SetActive(true);
            }

            // 3. Set Weather
            if (weatherController != null)
            {
                weatherController.SetWeather(currentConfig.weather);
            }
            else if (WeatherController.Instance != null)
            {
                WeatherController.Instance.SetWeather(currentConfig.weather);
            }

            // 4. Configure Hazards
            if (hazardController != null)
            {
                hazardController.ConfigureForMission(currentMissionIndex);
            }
            else if (EnvironmentalHazardController.Instance != null)
            {
                EnvironmentalHazardController.Instance.ConfigureForMission(currentMissionIndex);
            }

            MissionStarted?.Invoke(currentConfig);

            if (Level6SafetyManager.Instance != null)
            {
                Level6SafetyManager.Instance.TriggerGeneralFeedback($"[MISSION] {currentConfig.title} initiated. Drive with caution!");
            }
        }

        private void OnDestinationReached()
        {
            if (!isMissionRunning || isLevelCompleted) return;

            isMissionRunning = false;
            int reward = currentConfig.reward;

            // Award reward
            if (Level6SafetyManager.Instance != null)
            {
                Level6SafetyManager.Instance.RegisterSafeAction(reward, $"{currentConfig.title} Completed!");
            }

            MissionCompleted?.Invoke(currentConfig, reward);

            if (currentMissionIndex < missions.Length)
            {
                StartCoroutine(NextMissionTransitionRoutine());
            }
            else
            {
                CompleteFullLevel();
            }
        }

        private IEnumerator NextMissionTransitionRoutine()
        {
            if (Level6SafetyManager.Instance != null)
            {
                Level6SafetyManager.Instance.TriggerGeneralFeedback($"[SUCCESS] {currentConfig.title} SUCCESSFUL! Moving to next mission...");
            }

            yield return new WaitForSeconds(3.0f);
            StartMission(currentMissionIndex + 1);
        }

        private void CompleteFullLevel()
        {
            isLevelCompleted = true;
            isMissionRunning = false;

            if (destinationGoal != null)
            {
                destinationGoal.gameObject.SetActive(false);
            }

            LevelCompleted?.Invoke();

            if (Level6SafetyManager.Instance != null)
            {
                Level6SafetyManager.Instance.TriggerGeneralFeedback("[COMPLETED] ALL EXTREME ROAD MISSIONS COMPLETED! You mastered extreme conditions!");
            }
        }

        public void FailMission(string reason)
        {
            if (!isMissionRunning || isLevelCompleted) return;

            isMissionRunning = false;
            MissionFailed?.Invoke(reason);

            if (Level6SafetyManager.Instance != null)
            {
                Level6SafetyManager.Instance.TriggerGeneralFeedback($"[FAILED] MISSION FAILED: {reason}");
            }
        }

        public void RestartCurrentMission()
        {
            Time.timeScale = 1f;
            StartMission(currentMissionIndex);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            StartMission(1);
        }
    }
}
