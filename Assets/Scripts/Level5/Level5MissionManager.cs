using System;
using System.Collections;
using UnityEngine;

namespace TrafficTown2D.Level5
{
    public class Level5MissionManager : MonoBehaviour
    {
        public static Level5MissionManager Instance { get; private set; }

        [Header("Managers")]
        [SerializeField] private TrafficQueueManager queueManager;
        [SerializeField] private PedestrianIntersectionManager pedestrianManager;
        [SerializeField] private EmergencyVehicleManager emergencyManager;
        [SerializeField] private Level5SafetyManager safetyManager;
        [SerializeField] private Level5UIController uiController;

        private Level5MissionConfig[] missions;
        private int currentMissionIndex = 0;
        private float missionTimer = 0f;
        private bool isMissionRunning = false;

        private float eventTimer = 0f;
        private int missionEmergencyCount = 0;
        private bool missionCrowdSpawned = false;

        public Level5MissionConfig CurrentMission => (missions != null && currentMissionIndex < missions.Length) 
            ? missions[currentMissionIndex] 
            : null;

        public int CurrentMissionIndex => currentMissionIndex;
        public float TimeRemaining => CurrentMission != null ? Mathf.Max(0f, CurrentMission.duration - missionTimer) : 0f;
        public float MissionProgress => CurrentMission != null ? Mathf.Clamp01(missionTimer / CurrentMission.duration) : 0f;
        public bool IsMissionRunning => isMissionRunning;

        public event Action<Level5MissionConfig> MissionStarted;
        public event Action<Level5MissionConfig> MissionCompleted;
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
            Time.timeScale = 1f;
        }

        private void Start()
        {
            Time.timeScale = 1f;
            missions = Level5MissionDatabase.LoadMissions();
            currentMissionIndex = 0;
            StartMission(0);
        }

        private void Update()
        {
            if (!isMissionRunning || CurrentMission == null) return;

            missionTimer += Time.deltaTime;
            eventTimer += Time.deltaTime;

            HandleMissionSpecificEvents();

            if (missionTimer >= CurrentMission.duration)
            {
                CompleteCurrentMission();
            }
        }

        public void StartMission(int index)
        {
            if (missions == null || index >= missions.Length) return;

            currentMissionIndex = index;
            missionTimer = 0f;
            eventTimer = 0f;
            missionEmergencyCount = 0;
            missionCrowdSpawned = false;
            isMissionRunning = true;

            Level5MissionConfig config = missions[currentMissionIndex];

            // Configure subsystems
            if (queueManager != null)
            {
                queueManager.ClearAllVehicles();
                queueManager.ConfigureMission(config);
                queueManager.PrepopulateInitialVehicles();
            }

            if (pedestrianManager != null)
            {
                pedestrianManager.ClearActivePedestrians();
                pedestrianManager.SetSpawningActive(config.pedestriansActive);
            }

            if (emergencyManager != null)
            {
                emergencyManager.ResetCurrentEmergency();
            }

            MissionStarted?.Invoke(config);
        }

        public void FailMission(string reason)
        {
            if (!isMissionRunning) return;
            isMissionRunning = false;
            MissionFailed?.Invoke(reason);
        }

        private void HandleMissionSpecificEvents()
        {
            // Mission 4 (40s): Scripted ambulance arrivals
            if (CurrentMission.index == 4)
            {
                if (missionEmergencyCount == 0 && missionTimer >= 9f)
                {
                    missionEmergencyCount = 1;
                    if (emergencyManager != null) emergencyManager.DispatchAmbulance(ApproachDirection.North);
                }
                else if (missionEmergencyCount == 1 && missionTimer >= 22f)
                {
                    missionEmergencyCount = 2;
                    if (emergencyManager != null) emergencyManager.DispatchAmbulance(ApproachDirection.East);
                }
            }
            // Mission 5 (70s): Rush Hour dynamic chaos
            else if (CurrentMission.index == 5)
            {
                if (missionEmergencyCount == 0 && missionTimer >= 14f)
                {
                    missionEmergencyCount = 1;
                    if (emergencyManager != null) emergencyManager.DispatchAmbulance(ApproachDirection.South);
                }
                else if (missionEmergencyCount == 1 && missionTimer >= 34f)
                {
                    missionEmergencyCount = 2;
                    if (emergencyManager != null) emergencyManager.DispatchAmbulance(ApproachDirection.East);
                }
                else if (missionEmergencyCount == 2 && missionTimer >= 50f)
                {
                    missionEmergencyCount = 3;
                    if (emergencyManager != null) emergencyManager.DispatchAmbulance(ApproachDirection.West);
                }

                // Random pedestrian crowd spawn at 25s
                if (!missionCrowdSpawned && missionTimer >= 25f)
                {
                    missionCrowdSpawned = true;
                    if (pedestrianManager != null)
                    {
                        pedestrianManager.SpawnRandomPedestrian();
                        pedestrianManager.SpawnRandomPedestrian();
                    }
                }
            }
        }

        private void CompleteCurrentMission()
        {
            isMissionRunning = false;
            Level5MissionConfig completed = CurrentMission;

            MissionCompleted?.Invoke(completed);

            if (currentMissionIndex + 1 < missions.Length)
            {
                // Advance to next mission after small intermission delay
                StartCoroutine(AdvanceMissionDelayed(currentMissionIndex + 1));
            }
            else
            {
                // FINAL LEVEL COMPLETED!
                LevelCompleted?.Invoke();
            }
        }

        private IEnumerator AdvanceMissionDelayed(int nextIndex)
        {
            yield return new WaitForSeconds(3.0f);
            StartMission(nextIndex);
        }

        public void RestartLevel()
        {
            StopAllCoroutines();
            if (safetyManager != null) safetyManager.ResetScores();
            if (emergencyManager != null) emergencyManager.ResetManager();
            if (pedestrianManager != null) pedestrianManager.ResetAll();
            StartMission(0);
        }
    }
}
