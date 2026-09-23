using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficTown2D.Level7
{
    public class Level7MissionManager : MonoBehaviour
    {
        public static Level7MissionManager Instance { get; private set; }

        [Header("Scene References")]
        [SerializeField] private Level7PlayerCar playerCar;
        [SerializeField] private Level7DistractionManager distractionManager;
        [SerializeField] private Level7UIController uiController;
        [SerializeField] private Level7Ambulance ambulance;
        [SerializeField] private Level7Pedestrian pedestrian;
        [SerializeField] private Transform destinationMarker;

        private List<MissionDefinition> missions;
        private int currentMissionIndex = 0;
        private bool isTransitioning = false;
        private int safeStopsAtMissionStart = 0;

        public MissionDefinition CurrentMission => (missions != null && currentMissionIndex < missions.Count) ? missions[currentMissionIndex] : null;
        public int CurrentMissionNumber => currentMissionIndex + 1;
        public int TotalMissions => missions != null ? missions.Count : 5;

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
            missions = Level7MissionData.GetMissions();

            if (playerCar == null) playerCar = FindAnyObjectByType<Level7PlayerCar>();
            if (distractionManager == null) distractionManager = FindAnyObjectByType<Level7DistractionManager>();
            if (uiController == null) uiController = FindAnyObjectByType<Level7UIController>();
            if (ambulance == null) ambulance = FindAnyObjectByType<Level7Ambulance>();
            if (pedestrian == null) pedestrian = FindAnyObjectByType<Level7Pedestrian>();

            StartMission(0);
        }

        public void StartMission(int index)
        {
            if (missions == null || index < 0 || index >= missions.Count) return;
            currentMissionIndex = index;
            MissionDefinition mission = missions[currentMissionIndex];

            if (Level7FocusManager.Instance != null)
            {
                safeStopsAtMissionStart = Level7FocusManager.Instance.SafeStopsCount;
                Level7FocusManager.Instance.ResetMissionMetrics();
            }

            // Reposition player
            if (playerCar != null)
            {
                playerCar.transform.position = mission.playerSpawnPosition;
                playerCar.transform.rotation = Quaternion.Euler(0f, 0f, mission.playerSpawnRotationZ);
                playerCar.StopCar();
                playerCar.SetControlEnabled(true);
            }

            // Update destination marker
            if (destinationMarker != null)
            {
                destinationMarker.position = mission.destinationPosition;
                destinationMarker.gameObject.SetActive(true);
            }

            // Update Minimap
            if (uiController != null && uiController.MiniMap != null)
            {
                uiController.MiniMap.SetDestination(mission.destinationPosition);
            }

            // Configure distractions
            if (distractionManager != null)
            {
                distractionManager.ConfigureMission(mission.allowedDistractionTypes, mission.minSpawnInterval, mission.maxSpawnInterval);
            }

            // Reset pedestrian
            if (pedestrian != null)
            {
                pedestrian.ResetPedestrian();
            }

            // Deactivate ambulance until needed
            if (ambulance != null && !mission.spawnAmbulance)
            {
                ambulance.DeactivateAmbulance();
            }

            // Update UI
            if (uiController != null)
            {
                uiController.SetMission(mission.missionNumber, missions.Count, mission.title, mission.shortObjective);
                uiController.ShowFeedback($"MISSION {mission.missionNumber}: {mission.title}", true);
            }

            isTransitioning = false;
        }

        private void Update()
        {
            if (isTransitioning || missions == null || currentMissionIndex >= missions.Count) return;
            MissionDefinition mission = missions[currentMissionIndex];
            if (playerCar == null) return;

            // Check smart trigger
            if (mission.hasSmartTrigger && distractionManager != null)
            {
                float distToTrigger = Vector2.Distance(playerCar.transform.position, mission.smartTriggerLocation);
                if (distToTrigger <= mission.smartTriggerRadius)
                {
                    distractionManager.TriggerSmartDistraction(mission.smartTriggerId, mission.smartDistractionType);

                    // If Mission 5, activate ambulance when trigger reached
                    if (mission.spawnAmbulance && ambulance != null && !ambulance.IsActive)
                    {
                        ambulance.ActivateAmbulance(mission.ambulanceRoute);
                    }
                }
            }

            // Check destination arrival
            float distToDest = Vector2.Distance(playerCar.transform.position, mission.destinationPosition);
            bool reachedDest = (distToDest <= mission.destinationRadius && playerCar.IsStopped)
                            || (distToDest <= 2.2f && playerCar.CurrentSpeedKmh < 8f);

            if (reachedDest)
            {
                // Check if special safe stop required
                if (mission.requireSafeStopInteraction)
                {
                    int stopsInThisMission = (Level7FocusManager.Instance != null)
                        ? Level7FocusManager.Instance.SafeStopsCount - safeStopsAtMissionStart
                        : 0;

                    if (stopsInThisMission <= 0)
                    {
                        if (uiController != null)
                        {
                            uiController.ShowFeedback("[WARNING] Complete the Safe Stop requirement before finishing!", false);
                        }
                        return;
                    }
                }

                OnMissionCompleted();
            }
        }

        private void OnMissionCompleted()
        {
            isTransitioning = true;
            if (playerCar != null) playerCar.SetControlEnabled(false);

            if (Level7FocusManager.Instance != null)
            {
                Level7FocusManager.Instance.RecordSafeAction($"Mission {CurrentMissionNumber} Completed!", 50);
            }

            StartCoroutine(TransitionRoutine());
        }

        private IEnumerator TransitionRoutine()
        {
            yield return new WaitForSeconds(1.8f);

            int nextIndex = currentMissionIndex + 1;
            if (nextIndex < missions.Count)
            {
                StartMission(nextIndex);
            }
            else
            {
                // Level 7 complete!
                Debug.Log("[Level7MissionManager] ALL MISSIONS COMPLETED! Displaying Level 7 completion modal.");
                if (destinationMarker != null) destinationMarker.gameObject.SetActive(false);

                int score = Level7FocusManager.Instance != null ? Level7FocusManager.Instance.Score : 500;
                float safety = Level7FocusManager.Instance != null ? Level7FocusManager.Instance.SafetyScore : 100f;
                float focus = Level7FocusManager.Instance != null ? Level7FocusManager.Instance.Focus : 100f;
                int ignored = Level7FocusManager.Instance != null ? Level7FocusManager.Instance.DistractionsIgnored : 12;
                int unsafeCount = Level7FocusManager.Instance != null ? Level7FocusManager.Instance.UnsafeInteractions : 0;
                int safeStops = Level7FocusManager.Instance != null ? Level7FocusManager.Instance.SafeStopsCount : 2;

                if (uiController == null) uiController = FindAnyObjectByType<Level7UIController>();

                if (uiController != null)
                {
                    uiController.ShowCompletionModal(score, safety, focus, ignored, unsafeCount, safeStops);
                }
                else
                {
                    Level7CompletionModal fallbackModal = FindAnyObjectByType<Level7CompletionModal>(FindObjectsInactive.Include);
                    if (fallbackModal != null)
                    {
                        fallbackModal.Show(score, safety, focus, ignored, unsafeCount, safeStops);
                    }
                    else
                    {
                        Debug.LogError("[Level7MissionManager] Completion modal could not be found!");
                    }
                }

                if (TrafficTown2D.Core.GameManager.Instance != null)
                {
                    TrafficTown2D.Core.GameManager.Instance.SetState(TrafficTown2D.Core.GameState.LevelComplete);
                }
            }
        }
    }
}
