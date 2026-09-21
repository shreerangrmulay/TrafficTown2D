using System;
using System.Collections;
using UnityEngine;
using TrafficTown2D.Core;
using TrafficTown2D.UI;

namespace TrafficTown2D.Level3
{
    public class Level3MissionManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Level3PlayerCar playerCar;
        [SerializeField] private TopDownCameraFollow cameraFollow;
        [SerializeField] private Level3UIController uiController;
        [SerializeField] private Level3TrafficLight intersectionLight;
        [SerializeField] private Level3Pedestrian schoolPedestrian;
        [SerializeField] private Level3Pedestrian finalPedestrian;
        [SerializeField] private Level3TopDownVehicle ambulanceVehicle;
        [SerializeField] private GameObject[] crossTrafficVehicles;

        [Header("Mission Route Waypoints (Y Positions)")]
        [SerializeField] private float schoolZoneStartY = 8f;
        [SerializeField] private float schoolCrosswalkStopY = 24.5f;
        [SerializeField] private float schoolZoneEndY = 35f;

        [SerializeField] private float intersectionApproachY = 38f;
        [SerializeField] private float intersectionStopY = 46.5f;
        [SerializeField] private float intersectionClearY = 56f;

        [SerializeField] private float emergencyZoneStartY = 66f;
        [SerializeField] private float distractionTriggerY = 92f;

        [SerializeField] private float finalZoneStartY = 104f;
        [SerializeField] private float finishLineY = 128f;

        // State
        private MissionConfig[] missions;
        private int currentMissionIndex = 1; // 1 to 5
        private int totalScore = 100;
        private int safetyScore = 100;
        private int safeActionsCount = 0;
        private int violationsCount = 0;
        private bool isLevelCompleted = false;

        // Mission 1 State
        private bool schoolZoneEntered;
        private bool schoolPedestrianCrossingTriggered;
        private bool schoolStoppedAwarded;
        private bool schoolSpeedingDeducted;
        private float schoolSpeedingTimer;

        // Mission 2 State
        private bool intersectionApproached;
        private bool intersectionStoppedAwarded;
        private bool intersectionViolationDeducted;
        private bool intersectionCleared;

        // Mission 3 State
        private bool emergencyTriggered;
        private bool emergencyAmbulanceSpawned;
        private bool emergencyPulledOverAwarded;
        private bool emergencyCompleted;

        // Mission 4 State
        private bool distractionTriggered;
        private bool distractionResolved;

        // Mission 5 State
        private bool finalZoneEntered;
        private bool finalPedestrianTriggered;

        // Collision cooldown
        private float collisionCooldown;

        public int CurrentMissionIndex => currentMissionIndex;
        public int TotalScore => totalScore;
        public int SafetyScore => safetyScore;
        public int SafeActionsCount => safeActionsCount;
        public int ViolationsCount => violationsCount;
        public bool IsLevelCompleted => isLevelCompleted;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }
            Time.timeScale = 1f;

            missions = Level3MissionDatabase.LoadMissions();

            if (playerCar != null)
            {
                playerCar.CarCollided += HandleCarCollision;
            }

            if (schoolPedestrian != null)
            {
                schoolPedestrian.CrossingCompleted += OnSchoolPedestrianCompleted;
            }

            // Hide ambulance until Mission 3
            if (ambulanceVehicle != null)
            {
                ambulanceVehicle.gameObject.SetActive(false);
            }

            // Cross traffic initial state
            SetCrossTrafficActive(false);

            UpdateMissionUI();
            uiController.UpdateScoreAndSafety(totalScore, safetyScore);
        }

        private void OnDestroy()
        {
            if (playerCar != null)
            {
                playerCar.CarCollided -= HandleCarCollision;
            }

            if (schoolPedestrian != null)
            {
                schoolPedestrian.CrossingCompleted -= OnSchoolPedestrianCompleted;
            }
        }

        private void Update()
        {
            if (isLevelCompleted || playerCar == null) return;

            float playerY = playerCar.transform.position.y;
            float playerSpeedKmh = playerCar.CurrentSpeedKmh;

            if (collisionCooldown > 0f) collisionCooldown -= Time.deltaTime;

            // Mission 1: School Zone
            HandleMission1SchoolZone(playerY, playerSpeedKmh);

            // Mission 2: Intersection
            HandleMission2Intersection(playerY, playerSpeedKmh);

            // Mission 3: Emergency Vehicle
            HandleMission3Emergency(playerY, playerSpeedKmh);

            // Mission 4: Distraction
            HandleMission4Distraction(playerY);

            // Mission 5: Final City Drive & Finish Line
            HandleMission5FinalDrive(playerY, playerSpeedKmh);
        }

        #region Mission 1 — School Zone
        private void HandleMission1SchoolZone(float playerY, float playerSpeed)
        {
            if (currentMissionIndex != 1) return;

            // Enter School Zone
            if (!schoolZoneEntered && playerY >= schoolZoneStartY)
            {
                schoolZoneEntered = true;
                playerCar.CurrentSpeedLimitKmh = 20f;
                uiController.ShowFeedback("SCHOOL ZONE AHEAD! Slow down to 20 km/h.", false);
            }

            // Speeding Check in School Zone
            if (schoolZoneEntered && playerY < schoolZoneEndY && !schoolSpeedingDeducted)
            {
                if (playerSpeed > 23f)
                {
                    schoolSpeedingTimer += Time.deltaTime;
                    if (schoolSpeedingTimer > 1.4f)
                    {
                        schoolSpeedingDeducted = true;
                        DeductSafety(20, "SLOW DOWN! Exceeding 20 km/h in school zone.");
                    }
                }
                else
                {
                    schoolSpeedingTimer = Mathf.Max(0f, schoolSpeedingTimer - Time.deltaTime);
                }
            }

            // Approach Crosswalk & Stop Check
            if (playerY >= 18f && !schoolPedestrianCrossingTriggered && schoolPedestrian != null)
            {
                schoolPedestrianCrossingTriggered = true;
                schoolPedestrian.StartCrossing();
                uiController.ShowFeedback("Children crossing ahead! Come to a complete stop.", false);
            }

            // Check if player stopped before crosswalk stop line
            if (schoolPedestrianCrossingTriggered && !schoolStoppedAwarded && playerY >= 20f && playerY <= schoolCrosswalkStopY + 0.5f)
            {
                if (playerCar.IsStopped)
                {
                    schoolStoppedAwarded = true;
                    AwardSafety(20, "GOOD STOP! Yielding to school children.");
                }
            }

            // Failure to stop (drove over crosswalk while pedestrian crossing)
            if (schoolPedestrian != null && schoolPedestrian.IsCrossing && playerY > schoolCrosswalkStopY + 1.2f && !schoolStoppedAwarded)
            {
                schoolStoppedAwarded = true; // prevent duplicate penalty
                DeductSafety(30, "MISTAKE! Did not stop for school children!");
            }

            // Fallback progression past school zone
            if (playerY >= schoolZoneEndY && currentMissionIndex == 1)
            {
                StartCoroutine(TransitionToMission(2, schoolStoppedAwarded ? 20 : 0));
            }
        }

        private void OnSchoolPedestrianCompleted()
        {
            if (currentMissionIndex == 1)
            {
                AwardSafety(10, "Pedestrians crossed safely. You may proceed!");
                StartCoroutine(TransitionToMission(2, 30));
            }
        }
        #endregion

        #region Mission 2 — Intersection
        private void HandleMission2Intersection(float playerY, float playerSpeed)
        {
            if (currentMissionIndex != 2) return;

            // Player approaches intersection
            if (!intersectionApproached && playerY >= intersectionApproachY)
            {
                intersectionApproached = true;
                playerCar.CurrentSpeedLimitKmh = 40f;
                SetCrossTrafficActive(true);

                if (intersectionLight != null)
                {
                    intersectionLight.SetAutonomous(false);
                    intersectionLight.SetState(SignalState.Yellow);
                    StartCoroutine(CycleIntersectionLightSequence());
                }
            }

            // Check if player stops at Red light
            if (intersectionLight != null && intersectionLight.CurrentState == SignalState.Red)
            {
                // Stopped before stop line
                if (!intersectionStoppedAwarded && playerY >= 40f && playerY <= intersectionStopY + 0.8f)
                {
                    if (playerCar.IsStopped)
                    {
                        intersectionStoppedAwarded = true;
                        AwardSafety(20, "EXCELLENT! Stopped safely at the red light.");
                    }
                }

                // Ran the Red light
                if (!intersectionViolationDeducted && playerY > intersectionStopY + 1.5f)
                {
                    intersectionViolationDeducted = true;
                    DeductSafety(25, "RED LIGHT VIOLATION! Always stop on red!");
                }
            }

            // Cleared the intersection
            if (!intersectionCleared && playerY >= intersectionClearY)
            {
                intersectionCleared = true;
                SetCrossTrafficActive(false);

                if (!intersectionViolationDeducted)
                {
                    StartCoroutine(TransitionToMission(3, 40));
                }
                else
                {
                    StartCoroutine(TransitionToMission(3, 10));
                }
            }
        }

        private IEnumerator CycleIntersectionLightSequence()
        {
            yield return new WaitForSeconds(2.0f);
            if (intersectionLight != null) intersectionLight.SetState(SignalState.Red);

            yield return new WaitForSeconds(4.0f);
            if (intersectionLight != null) intersectionLight.SetState(SignalState.Green);
            uiController.ShowFeedback("Green signal! You may cross the intersection.", false);
        }

        private void SetCrossTrafficActive(bool active)
        {
            if (crossTrafficVehicles == null) return;
            for (int i = 0; i < crossTrafficVehicles.Length; i++)
            {
                if (crossTrafficVehicles[i] != null)
                {
                    crossTrafficVehicles[i].SetActive(active);
                }
            }
        }
        #endregion

        #region Mission 3 — Emergency Vehicle
        private void HandleMission3Emergency(float playerY, float playerSpeed)
        {
            if (currentMissionIndex != 3) return;

            // Trigger emergency vehicle approach
            if (!emergencyTriggered && playerY >= emergencyZoneStartY)
            {
                emergencyTriggered = true;
                uiController.ShowFeedback("SIREN ALERT! Ambulance approaching from behind. Pull over and stop!", true);

                if (ambulanceVehicle != null)
                {
                    ambulanceVehicle.gameObject.SetActive(true);
                    // Spawn behind player in left lane
                    ambulanceVehicle.transform.position = new Vector3(-1.0f, playerY - 14f, 0f);
                    ambulanceVehicle.SetMovement(Vector2.up, 7.5f);
                    emergencyAmbulanceSpawned = true;
                }
            }

            // Check if player pulled over to the right and stopped/slowed
            if (emergencyAmbulanceSpawned && !emergencyPulledOverAwarded)
            {
                float playerX = playerCar.transform.position.x;

                if (playerX > 0.6f && (playerCar.IsStopped || playerSpeed < 8f))
                {
                    emergencyPulledOverAwarded = true;
                    AwardSafety(30, "PERFECT! Pulled over safely for the ambulance.");
                }
            }

            // Ambulance passes player or distance threshold reached
            if (emergencyAmbulanceSpawned && !emergencyCompleted)
            {
                bool ambulancePassed = false;
                if (ambulanceVehicle != null && ambulanceVehicle.gameObject.activeInHierarchy)
                {
                    float ambY = ambulanceVehicle.transform.position.y;
                    if (ambY > playerY + 5f || ambY > emergencyZoneStartY + 25f)
                    {
                        ambulancePassed = true;
                    }
                }
                else
                {
                    ambulancePassed = true;
                }

                // Fallback: player reached Mission 4 boundary
                if (playerY >= distractionTriggerY - 4f)
                {
                    ambulancePassed = true;
                }

                if (ambulancePassed)
                {
                    emergencyCompleted = true;
                    if (!emergencyPulledOverAwarded)
                    {
                        DeductSafety(30, "MISTAKE! Did not pull over for emergency vehicle.");
                    }
                    StartCoroutine(TransitionToMission(4, emergencyPulledOverAwarded ? 40 : 10));
                }
            }
        }
        #endregion

        #region Mission 4 — Distraction
        private void HandleMission4Distraction(float playerY)
        {
            if (currentMissionIndex != 4) return;

            if (!distractionTriggered && playerY >= distractionTriggerY)
            {
                distractionTriggered = true;
                if (playerCar != null)
                {
                    playerCar.SetControlEnabled(false);
                }
                uiController.ShowDistractionPopup(OnDistractionChoice);
            }
            else if (!distractionTriggered && playerY >= finalZoneStartY - 2f)
            {
                // Fallback if player somehow passed trigger coordinate
                distractionTriggered = true;
                if (playerCar != null)
                {
                    playerCar.SetControlEnabled(false);
                }
                uiController.ShowDistractionPopup(OnDistractionChoice);
            }
        }

        private void OnDistractionChoice(bool choseIgnore)
        {
            if (distractionResolved) return;
            distractionResolved = true;

            if (playerCar != null)
            {
                playerCar.SetControlEnabled(true);
            }

            if (choseIgnore)
            {
                AwardSafety(30, "GOOD DECISION! Stay focused on the road while driving.");
                StartCoroutine(TransitionToMission(5, 30));
            }
            else
            {
                DeductSafety(25, "DISTRACTED DRIVING! Never check phone while driving.");
                StartCoroutine(TransitionToMission(5, 0));
            }
        }
        #endregion

        #region Mission 5 — Final City Drive
        private void HandleMission5FinalDrive(float playerY, float playerSpeed)
        {
            if (currentMissionIndex != 5) return;

            if (!finalZoneEntered && playerY >= finalZoneStartY)
            {
                finalZoneEntered = true;
                playerCar.CurrentSpeedLimitKmh = 30f;
                uiController.ShowFeedback("FINAL CITY DRIVE: Obey 30 km/h speed limit and reach the finish line!", false);
            }

            if (!finalPedestrianTriggered && playerY >= 110f && finalPedestrian != null)
            {
                finalPedestrianTriggered = true;
                finalPedestrian.StartCrossing();
            }

            // Finish Line Reached
            if (playerY >= finishLineY)
            {
                CompleteLevel();
            }
        }
        #endregion

        private IEnumerator TransitionToMission(int nextIndex, int rewardPoints)
        {
            if (rewardPoints > 0)
            {
                totalScore += rewardPoints;
                uiController.UpdateScoreAndSafety(totalScore, safetyScore);
                uiController.ShowFeedback($"MISSION {currentMissionIndex} COMPLETE! +{rewardPoints} PTS", false);
            }

            yield return new WaitForSeconds(1.8f);

            currentMissionIndex = nextIndex;
            UpdateMissionUI();
        }

        private void UpdateMissionUI()
        {
            if (missions == null || missions.Length == 0) return;

            int idx = Mathf.Clamp(currentMissionIndex - 1, 0, missions.Length - 1);
            MissionConfig mission = missions[idx];

            uiController.UpdateMissionDisplay(currentMissionIndex, missions.Length, mission.name, mission.objective);
        }

        public void AwardSafety(int points, string message)
        {
            safeActionsCount++;
            totalScore += points;
            safetyScore = Mathf.Min(100, safetyScore + 10);
            uiController.UpdateScoreAndSafety(totalScore, safetyScore);
            uiController.ShowFeedback(message, false);
        }

        public void DeductSafety(int points, string message)
        {
            violationsCount++;
            totalScore = Mathf.Max(0, totalScore - points);
            safetyScore = Mathf.Max(0, safetyScore - points);
            uiController.UpdateScoreAndSafety(totalScore, safetyScore);
            uiController.ShowFeedback(message, true);
        }

        private void HandleCarCollision(Collision2D collision)
        {
            if (isLevelCompleted || collisionCooldown > 0f) return;
            collisionCooldown = 1.8f;

            if (cameraFollow != null)
            {
                cameraFollow.TriggerShake(0.3f, 0.4f);
            }

            DeductSafety(30, "COLLISION! Slow down and maintain vehicle distance.");
        }

        private void CompleteLevel()
        {
            if (isLevelCompleted) return;
            isLevelCompleted = true;

            if (playerCar != null)
            {
                playerCar.StopCar();
                playerCar.SetControlEnabled(false);
            }

            totalScore += 50; // Finish line bonus
            uiController.UpdateScoreAndSafety(totalScore, safetyScore);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.LevelComplete);
            }

            // Show level completion dialog with full statistics
            uiController.ShowCompletionDialog(
                totalScore: totalScore,
                safetyScore: safetyScore,
                safeActions: safeActionsCount,
                violations: violationsCount,
                missionsCompleted: 5
            );
        }
    }
}
