using System;
using System.Collections;
using UnityEngine;
using TrafficTown2D.Core;
using TrafficTown2D.Level3;

namespace TrafficTown2D.Level4
{
    public class NightMissionManager : MonoBehaviour
    {
        [Header("Player & Core References")]
        [SerializeField] private Level3PlayerCar playerCar;
        [SerializeField] private PlayerHeadlightController playerHeadlights;
        [SerializeField] private NightVisibilityController visibilityController;
        [SerializeField] private RainController rainController;
        [SerializeField] private TopDownCameraFollow cameraFollow;
        [SerializeField] private Level4UIController uiController;

        [Header("Mission Actors")]
        [SerializeField] private Level3Pedestrian darkPedestrian;
        [SerializeField] private Level3Pedestrian guardPedestrian;
        [SerializeField] private OncomingTrafficVehicle oncomingCar;
        [SerializeField] private Level3TrafficLight intersectionLight;
        [SerializeField] private Level3TopDownVehicle ambulanceVehicle;
        [SerializeField] private GameObject[] crossTrafficVehicles;

        [Header("Route Waypoints (Y Coordinates)")]
        [SerializeField] private float mission1EndY = 32f;
        [SerializeField] private float zebraCrossingY = 56f;
        [SerializeField] private float zebraStopY = 53.5f;
        [SerializeField] private float mission2EndY = 70f;
        [SerializeField] private float oncomingTriggerY = 74f;
        [SerializeField] private float mission3EndY = 108f;
        [SerializeField] private float intersectionStopY = 123.5f;
        [SerializeField] private float intersectionClearY = 132f;
        [SerializeField] private float ambulanceTriggerY = 136f;
        [SerializeField] private float mission4EndY = 152f;
        [SerializeField] private float finishLineY = 186f;

        // State
        private NightMissionConfig[] missions;
        private int currentMissionIndex = 1; // 1 to 5
        private int totalScore = 100;
        private int safetyScore = 100;
        private int safeActionsCount = 0;
        private int violationsCount = 0;
        private bool isLevelCompleted = false;

        // Mission 1 State
        private bool headlightsChecked = false;
        private bool headlightsOffWarned = false;

        // Mission 2 State
        private bool zebraApproached = false;
        private bool zebraYieldAwarded = false;
        private bool zebraPedestriansFinished = false;

        // Mission 3 State
        private bool oncomingCarStarted = false;
        private bool dazzleWarned = false;
        private bool dazzleHandled = false;

        // Mission 4 State
        private bool intersectionApproached = false;
        private bool intersectionStopAwarded = false;
        private bool intersectionViolationRecorded = false;
        private bool ambulanceSpawned = false;
        private bool ambulanceYieldAwarded = false;

        // Mission 5 State
        private bool rainStarted = false;
        private bool finishReached = false;

        // General tracking
        private float speedingTimer = 0f;
        private float collisionCooldown = 0f;

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

            missions = NightMissionDatabase.LoadMissions();

            if (playerCar != null)
            {
                playerCar.CarCollided += HandleCarCollision;
            }

            if (darkPedestrian != null)
            {
                darkPedestrian.CrossingCompleted += OnPedestriansCrossingDone;
            }

            if (oncomingCar != null)
            {
                oncomingCar.OnDazzleWarning += HandleDazzleWarning;
                oncomingCar.OnDazzlePenalty += HandleDazzlePenalty;
                oncomingCar.OnCourteousDipSuccess += HandleCourteousDipSuccess;
            }

            // Deactivate cross-traffic and ambulance initially
            if (crossTrafficVehicles != null)
            {
                for (int i = 0; i < crossTrafficVehicles.Length; i++)
                {
                    if (crossTrafficVehicles[i] != null)
                        crossTrafficVehicles[i].SetActive(false);
                }
            }

            if (ambulanceVehicle != null)
            {
                ambulanceVehicle.gameObject.SetActive(false);
            }

            ApplyMission(1);
        }

        private void OnDestroy()
        {
            if (playerCar != null)
            {
                playerCar.CarCollided -= HandleCarCollision;
            }
            if (darkPedestrian != null)
            {
                darkPedestrian.CrossingCompleted -= OnPedestriansCrossingDone;
            }
            if (oncomingCar != null)
            {
                oncomingCar.OnDazzleWarning -= HandleDazzleWarning;
                oncomingCar.OnDazzlePenalty -= HandleDazzlePenalty;
                oncomingCar.OnCourteousDipSuccess -= HandleCourteousDipSuccess;
            }
        }

        private void Update()
        {
            if (isLevelCompleted || playerCar == null) return;

            float playerY = playerCar.transform.position.y;
            float playerSpeed = playerCar.CurrentSpeedKmh;

            if (collisionCooldown > 0f)
            {
                collisionCooldown -= Time.deltaTime;
            }

            // Speed Limit Checking
            CheckSpeedLimit(playerSpeed);

            // Mission-specific state machines
            switch (currentMissionIndex)
            {
                case 1:
                    UpdateMission1(playerY);
                    break;
                case 2:
                    UpdateMission2(playerY, playerSpeed);
                    break;
                case 3:
                    UpdateMission3(playerY);
                    break;
                case 4:
                    UpdateMission4(playerY, playerSpeed);
                    break;
                case 5:
                    UpdateMission5(playerY);
                    break;
            }
        }

        #region Mission 1: Learn the Night
        private void UpdateMission1(float playerY)
        {
            // Prompt if driving in the dark with lights off
            if (!headlightsOffWarned && playerHeadlights != null && !playerHeadlights.IsOn && playerY > 4f)
            {
                headlightsOffWarned = true;
                uiController?.ShowFeedback("Turn headlights ON [H]! Driving unlit at night is illegal and dangerous.", false);
            }

            // Reward turning on headlights
            if (!headlightsChecked && playerHeadlights != null && playerHeadlights.IsOn && playerY > 6f)
            {
                headlightsChecked = true;
                AddScore(30, "Headlights ON! Visibility restored.");
                safeActionsCount++;
            }

            if (playerY >= mission1EndY)
            {
                ApplyMission(2);
            }
        }
        #endregion

        #region Mission 2: See and React
        private void UpdateMission2(float playerY, float playerSpeed)
        {
            if (!zebraApproached && playerY >= 40f)
            {
                zebraApproached = true;
                uiController?.ShowFeedback("Pedestrian Crossing Ahead! Dark clothing reduces visibility — slow down.", false);
            }

            // Check if player stopped safely before crossing while pedestrians cross
            if (!zebraYieldAwarded && !zebraPedestriansFinished)
            {
                if (playerY >= zebraStopY - 4.5f && playerY <= zebraStopY + 0.5f && playerSpeed < 3f)
                {
                    zebraYieldAwarded = true;
                    AddScore(35, "Safely yielded to crossing pedestrians!");
                    safeActionsCount++;
                }
                else if (playerY > zebraStopY + 1.2f && !zebraPedestriansFinished)
                {
                    // Ran through pedestrian crossing while pedestrians were on road
                    DeductScore(30, "Crossed while pedestrians were on the crosswalk!");
                    violationsCount++;
                    zebraPedestriansFinished = true; // Prevent multiple triggers
                }
            }

            if (playerY >= mission2EndY)
            {
                ApplyMission(3);
            }
        }

        private void OnPedestriansCrossingDone()
        {
            zebraPedestriansFinished = true;
            if (currentMissionIndex == 2)
            {
                uiController?.ShowFeedback("Pedestrians safely across. Proceed forward with caution.", true);
            }
        }
        #endregion

        #region Mission 3: Headlight Discipline
        private void UpdateMission3(float playerY)
        {
            if (!oncomingCarStarted && playerY >= oncomingTriggerY)
            {
                oncomingCarStarted = true;
                if (oncomingCar != null)
                {
                    oncomingCar.gameObject.SetActive(true);
                    oncomingCar.StartDriving();
                }
                uiController?.ShowFeedback("Oncoming Vehicle Approaching! Dip High Beam to Low Beam [F].", false);
            }

            if (playerY >= mission3EndY)
            {
                ApplyMission(4);
            }
        }

        private void HandleDazzleWarning()
        {
            if (!dazzleWarned)
            {
                dazzleWarned = true;
                uiController?.ShowFeedback("⚠️ Dazzling oncoming driver! Switch to Low Beam [F] now!", false);
            }
        }

        private void HandleDazzlePenalty()
        {
            if (!dazzleHandled)
            {
                dazzleHandled = true;
                DeductScore(20, "Failed to dip high beam! Blinding oncoming drivers causes accidents.");
                violationsCount++;
            }
        }

        private void HandleCourteousDipSuccess()
        {
            if (!dazzleHandled)
            {
                dazzleHandled = true;
                AddScore(35, "Courteous Driver! Low beam prevented dangerous glare.");
                safeActionsCount++;
            }
        }
        #endregion

        #region Mission 4: Night City Drive
        private void UpdateMission4(float playerY, float playerSpeed)
        {
            if (!intersectionApproached && playerY >= 115f)
            {
                intersectionApproached = true;
                if (crossTrafficVehicles != null)
                {
                    for (int i = 0; i < crossTrafficVehicles.Length; i++)
                    {
                        if (crossTrafficVehicles[i] != null) crossTrafficVehicles[i].SetActive(true);
                    }
                }
            }

            // Intersection Red Light check
            if (intersectionLight != null && !intersectionStopAwarded)
            {
                if (intersectionLight.CurrentState == SignalState.Red)
                {
                    if (playerY >= intersectionStopY - 4f && playerY <= intersectionStopY && playerSpeed < 2.5f)
                    {
                        intersectionStopAwarded = true;
                        AddScore(30, "Stopped at red signal in night intersection.");
                        safeActionsCount++;
                    }
                    else if (playerY > intersectionStopY + 1.5f && !intersectionViolationRecorded)
                    {
                        intersectionViolationRecorded = true;
                        DeductScore(30, "Ran red signal at night intersection!");
                        violationsCount++;
                    }
                }
            }

            // Ambulance spawn
            if (!ambulanceSpawned && playerY >= ambulanceTriggerY)
            {
                ambulanceSpawned = true;
                if (ambulanceVehicle != null)
                {
                    ambulanceVehicle.gameObject.SetActive(true);
                    ambulanceVehicle.transform.position = new Vector3(playerCar.transform.position.x, playerCar.transform.position.y - 12f, 0f);
                }
                uiController?.ShowFeedback("EMERGENCY AMBULANCE BEHIND! Pull over to the right and yield!", false);
            }

            // Check if player pulls over to the right lane
            if (ambulanceSpawned && !ambulanceYieldAwarded && ambulanceVehicle != null)
            {
                // Player is on the right side of the road (X > 0.8f) and moving slowly or stopped
                if (playerCar.transform.position.x > 0.7f && playerSpeed < 10f)
                {
                    ambulanceYieldAwarded = true;
                    AddScore(40, "Safely pulled over to right lane for ambulance!");
                    safeActionsCount++;
                }
            }

            if (playerY >= mission4EndY)
            {
                ApplyMission(5);
            }
        }
        #endregion

        #region Mission 5: Rainy Night
        private void UpdateMission5(float playerY)
        {
            if (!rainStarted)
            {
                rainStarted = true;
                if (rainController != null)
                {
                    rainController.StartRain();
                }
                uiController?.ShowFeedback("RAIN HAS STARTED! Wet road doubles stopping distance. Brake early!", false);
            }

            if (!finishReached && playerY >= finishLineY)
            {
                finishReached = true;
                CompleteLevel();
            }
        }
        #endregion

        private void CheckSpeedLimit(float playerSpeed)
        {
            if (currentMissionIndex < 1 || currentMissionIndex > missions.Length) return;
            NightMissionConfig currentConfig = missions[currentMissionIndex - 1];

            if (playerSpeed > currentConfig.speedLimit + 5f)
            {
                speedingTimer += Time.deltaTime;
                if (speedingTimer >= 2.0f)
                {
                    speedingTimer = 0f;
                    DeductScore(currentConfig.speedingPenalty, $"Exceeding safe night speed limit ({Mathf.RoundToInt(currentConfig.speedLimit)} km/h)!");
                    violationsCount++;
                }
            }
            else
            {
                speedingTimer = Mathf.Max(0f, speedingTimer - Time.deltaTime);
            }
        }

        private void HandleCarCollision(Collision2D collision)
        {
            if (collisionCooldown > 0f) return;
            collisionCooldown = 1.8f;

            DeductScore(25, "Night Collision! Keep safe distance and stay in lane.");
            violationsCount++;
        }

        public void AddScore(int amount, string reason)
        {
            totalScore += amount;
            safetyScore = Mathf.Min(100, safetyScore + (amount / 2));
            uiController?.UpdateScores(totalScore, safetyScore);
            uiController?.ShowFeedback($"+{amount} {reason}", true);
        }

        public void DeductScore(int amount, string reason)
        {
            totalScore = Mathf.Max(0, totalScore - amount);
            safetyScore = Mathf.Max(0, safetyScore - amount);
            uiController?.UpdateScores(totalScore, safetyScore);
            uiController?.ShowFeedback($"-{amount} {reason}", false);
        }

        private void ApplyMission(int missionIndex)
        {
            currentMissionIndex = missionIndex;
            if (missions == null || missions.Length == 0) return;

            int configIdx = Mathf.Clamp(missionIndex - 1, 0, missions.Length - 1);
            NightMissionConfig config = missions[configIdx];

            if (playerCar != null)
            {
                playerCar.CurrentSpeedLimitKmh = config.speedLimit;
            }

            if (uiController != null)
            {
                uiController.SetMissionInfo(
                    config.index,
                    missions.Length,
                    config.name,
                    config.subtitle,
                    config.objective,
                    config.speedLimit
                );
                uiController.UpdateScores(totalScore, safetyScore);
            }
        }

        private void CompleteLevel()
        {
            isLevelCompleted = true;
            if (playerCar != null)
            {
                playerCar.SetControlEnabled(false);
                playerCar.StopCar();
            }

            // Save progress
            int stars = safetyScore >= 85 ? 3 : (safetyScore >= 60 ? 2 : 1);
            int prevStars = PlayerPrefs.GetInt("Level_4_Stars", 0);
            if (stars > prevStars)
            {
                PlayerPrefs.SetInt("Level_4_Stars", stars);
            }
            int prevScore = PlayerPrefs.GetInt("Level_4_Score", 0);
            if (totalScore > prevScore)
            {
                PlayerPrefs.SetInt("Level_4_Score", totalScore);
            }
            PlayerPrefs.SetInt("Level_4_Completed", 1);
            PlayerPrefs.Save();

            if (uiController != null)
            {
                uiController.ShowCompletionPanel(totalScore, safetyScore, safeActionsCount, violationsCount);
            }
        }
    }
}
