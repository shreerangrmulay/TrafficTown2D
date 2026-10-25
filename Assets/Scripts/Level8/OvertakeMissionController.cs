using System;
using System.Collections.Generic;
using UnityEngine;
using TrafficTown2D.Level3;
using TrafficTown2D.Player;

namespace TrafficTown2D.Level8
{
    [System.Serializable]
    public class OvertakeMissionData
    {
        public int missionNumber;
        public string title;
        public string subtitle;
        public string objective;
        public float startY;
        public float targetY;
        public bool requiresWaitDecision;
    }

    /// <summary>
    /// Manages the 5 distinct educational overtaking missions for Level 8.
    /// Tracks objectives, steps, waypoints, and completion criteria.
    /// </summary>
    public class OvertakeMissionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Level3PlayerCar playerCar;
        [SerializeField] private OvertakeSafetyEvaluator evaluator;
        [SerializeField] private OvertakeScenarioController scenarioController;

        [Header("Active Mission State")]
        [SerializeField] private int currentMissionIndex = 0; // 0 to 4 (Missions 1 to 5)
        [SerializeField] private bool isMissionActive = false;

        // Mission 4 Step Checklist Tracking
        private bool m4SafeDistanceConfirmed = false;
        private bool m4SignaledLeft = false;
        private bool m4MovedIntoPassingLane = false;
        private bool m4PassedVehicle = false;
        private bool m4ClearanceConfirmed = false;
        private bool m4SignaledRight = false;
        private bool m4ReturnedSafely = false;

        // Mission 2 & 3 Patient Waiting Tracking
        private bool patientWaitAwarded = false;
        private float patientWaitTimer = 0f;

        // Mission Definitions
        private readonly List<OvertakeMissionData> missions = new List<OvertakeMissionData>
        {
            new OvertakeMissionData
            {
                missionNumber = 1,
                title = "KNOW YOUR ROAD",
                subtitle = "Observe road markings and restrictions",
                objective = "Follow the slow delivery vehicle. Maintain a safe following distance and overtake only in the permitted dashed-line section.",
                startY = -5f,
                targetY = 50f,
                requiresWaitDecision = false
            },
            new OvertakeMissionData
            {
                missionNumber = 2,
                title = "FIND THE SAFE GAP",
                subtitle = "Judge oncoming traffic speed and distance",
                objective = "The first oncoming car is too close! Hold position, wait for the oncoming vehicle to pass, then overtake during the clear gap.",
                startY = 50f,
                targetY = 110f,
                requiresWaitDecision = true
            },
            new OvertakeMissionData
            {
                missionNumber = 3,
                title = "KNOW WHEN NOT TO OVERTAKE",
                subtitle = "Waiting is often the safest decision",
                objective = "The road enters a blind bend with a solid line. Maintain safe following distance behind the truck and DO NOT attempt to overtake.",
                startY = 110f,
                targetY = 175f,
                requiresWaitDecision = true
            },
            new OvertakeMissionData
            {
                missionNumber = 4,
                title = "COMPLETE THE OVERTAKE",
                subtitle = "Master the safe 7-step sequence",
                objective = "Perform the full manoeuvre: Keep distance -> Signal Left [Q] -> Move out -> Pass -> Check clearance -> Signal Right [E] -> Return.",
                startY = 175f,
                targetY = 230f,
                requiresWaitDecision = false
            },
            new OvertakeMissionData
            {
                missionNumber = 5,
                title = "THE OVERTAKING TEST",
                subtitle = "The final comprehensive challenge",
                objective = "Combine all skills: Wait behind traffic when unsafe, avoid passing on bends/crossings, and complete safe passes to reach the finish line!",
                startY = 235f,
                targetY = 300f,
                requiresWaitDecision = true
            }
        };

        // Events
        public event Action<OvertakeMissionData> MissionStarted;
        public event Action<int, int> MissionCompleted; // (missionNumber, scoreEarned)
        public event Action<string> StepProgressUpdated;

        public OvertakeMissionData CurrentMission => (currentMissionIndex >= 0 && currentMissionIndex < missions.Count) ? missions[currentMissionIndex] : missions[0];
        public int CurrentMissionNumber => currentMissionIndex + 1;
        public int TotalMissions => missions.Count;

        public void BindDependencies(Level3PlayerCar car, OvertakeSafetyEvaluator eval, OvertakeScenarioController scen)
        {
            playerCar = car;
            evaluator = eval;
            scenarioController = scen;
        }

        private void Start()
        {
            StartMission(0);
        }

        public void StartMission(int index)
        {
            if (index < 0 || index >= missions.Count) return;

            currentMissionIndex = index;
            isMissionActive = true;
            patientWaitAwarded = false;
            patientWaitTimer = 0f;
            ResetMission4Checklist();

            OvertakeMissionData mission = missions[currentMissionIndex];

            // Configure scenario traffic for this mission
            if (scenarioController != null)
            {
                scenarioController.ConfigureForMission(mission.missionNumber);
                if (evaluator != null)
                {
                    evaluator.SetActiveVehicles(scenarioController.CurrentLeadVehicle, scenarioController.CurrentOncomingVehicle);
                }
            }

            // Position player at mission start
            if (playerCar != null)
            {
                playerCar.transform.position = new Vector3(1.5f, mission.startY, 0f);
                playerCar.transform.rotation = Quaternion.identity;
                playerCar.StopCar();
            }

            MissionStarted?.Invoke(mission);
        }

        public void RetryCurrentMission()
        {
            StartMission(currentMissionIndex);
        }

        private void Update()
        {
            if (!isMissionActive || playerCar == null) return;

            float playerY = playerCar.transform.position.y;
            OvertakeMissionData mission = CurrentMission;

            // Mission 4 Step Checklist Processing
            if (currentMissionIndex == 3)
            {
                ProcessMission4Steps();
            }

            // Patience / Safe Waiting detection (Missions 2, 3, 5)
            if (mission.requiresWaitDecision && !patientWaitAwarded && evaluator != null)
            {
                ProcessPatientWaiting();
            }

            // Check if player reached the target Y position for this mission
            if (playerY >= mission.targetY)
            {
                CompleteCurrentMission();
            }
        }

        private void ProcessPatientWaiting()
        {
            // If player is in right lane and held safe following distance behind lead vehicle while conditions were Wait or Prohibited
            if (evaluator.IsPlayerInRightLane && evaluator.DistanceToLead < 14f && evaluator.DistanceToLead >= 3.0f)
            {
                if (evaluator.CurrentState == OvertakeSafetyState.Wait || evaluator.CurrentState == OvertakeSafetyState.Prohibited)
                {
                    patientWaitTimer += Time.deltaTime;
                    if (patientWaitTimer >= 2.5f)
                    {
                        patientWaitAwarded = true;
                        if (Level8OvertakeManager.Instance != null)
                        {
                            Level8OvertakeManager.Instance.RecordSafeWaitDecision();
                        }
                    }
                }
            }
            else
            {
                patientWaitTimer = Mathf.Max(0f, patientWaitTimer - Time.deltaTime);
            }
        }

        private void ProcessMission4Steps()
        {
            if (evaluator == null) return;

            var ind = playerCar.GetComponent<CarIndicatorController>();

            // Step 1: Safe Distance
            if (!m4SafeDistanceConfirmed && evaluator.IsPlayerInRightLane && evaluator.DistanceToLead < 12f && evaluator.DistanceToLead >= 3.2f)
            {
                m4SafeDistanceConfirmed = true;
                StepProgressUpdated?.Invoke("[1/7] Safe following distance confirmed.");
            }

            // Step 2: Signal Left (Q)
            if (m4SafeDistanceConfirmed && !m4SignaledLeft && ind != null && ind.LeftSignalActive)
            {
                m4SignaledLeft = true;
                StepProgressUpdated?.Invoke("[2/7] Left signal engaged with [Q]!");
            }

            // Step 3: Move to passing lane
            if (m4SignaledLeft && !m4MovedIntoPassingLane && evaluator.IsPlayerInPassingLane)
            {
                m4MovedIntoPassingLane = true;
                StepProgressUpdated?.Invoke("[3/7] Moved into passing lane safely.");
            }

            // Step 4: Pass vehicle
            if (m4MovedIntoPassingLane && !m4PassedVehicle && scenarioController.CurrentLeadVehicle != null)
            {
                if (playerCar.transform.position.y > scenarioController.CurrentLeadVehicle.position.y + 0.5f)
                {
                    m4PassedVehicle = true;
                    StepProgressUpdated?.Invoke("[4/7] Vehicle passed!");
                }
            }

            // Step 5: Check clearance (> 3m ahead)
            if (m4PassedVehicle && !m4ClearanceConfirmed && scenarioController.CurrentLeadVehicle != null)
            {
                if (playerCar.transform.position.y > scenarioController.CurrentLeadVehicle.position.y + 3.0f)
                {
                    m4ClearanceConfirmed = true;
                    StepProgressUpdated?.Invoke("[5/7] Safe clearance ahead confirmed.");
                }
            }

            // Step 6: Signal Right (E)
            if (m4ClearanceConfirmed && !m4SignaledRight && ind != null && ind.RightSignalActive)
            {
                m4SignaledRight = true;
                StepProgressUpdated?.Invoke("[6/7] Right signal engaged with [E]!");
            }

            // Step 7: Return safely to lane
            if (m4SignaledRight && !m4ReturnedSafely && evaluator.IsPlayerInRightLane)
            {
                m4ReturnedSafely = true;
                StepProgressUpdated?.Invoke("[7/7] Safe return completed! Full sequence mastered.");
                if (Level8OvertakeManager.Instance != null)
                {
                    Level8OvertakeManager.Instance.RecordSafeOvertake();
                }
            }
        }

        private void ResetMission4Checklist()
        {
            m4SafeDistanceConfirmed = false;
            m4SignaledLeft = false;
            m4MovedIntoPassingLane = false;
            m4PassedVehicle = false;
            m4ClearanceConfirmed = false;
            m4SignaledRight = false;
            m4ReturnedSafely = false;
        }

        public string GetMission4ChecklistStatus()
        {
            return $"1. Distance: {(m4SafeDistanceConfirmed ? "[OK]" : "..")}  " +
                   $"2. Left [Q]: {(m4SignaledLeft ? "[OK]" : "..")}  " +
                   $"3. Pull Out: {(m4MovedIntoPassingLane ? "[OK]" : "..")}\n" +
                   $"4. Pass: {(m4PassedVehicle ? "[OK]" : "..")}  " +
                   $"5. Clearance: {(m4ClearanceConfirmed ? "[OK]" : "..")}  " +
                   $"6. Right [E]: {(m4SignaledRight ? "[OK]" : "..")}  " +
                   $"7. Return: {(m4ReturnedSafely ? "[OK]" : "..")}";
        }

        private void CompleteCurrentMission()
        {
            isMissionActive = false;
            int completedMissionNum = CurrentMissionNumber;

            // Reward completion points
            int scoreBonus = 25;
            if (Level8OvertakeManager.Instance != null)
            {
                Level8OvertakeManager.Instance.AddScore(scoreBonus, $"Mission {completedMissionNum} Completed!");
            }

            MissionCompleted?.Invoke(completedMissionNum, scoreBonus);

            // Auto-advance to next mission or trigger Level Complete if Mission 5
            if (currentMissionIndex < missions.Count - 1)
            {
                StartMission(currentMissionIndex + 1);
            }
            else
            {
                // Level 8 Finished!
                if (Level8OvertakeManager.Instance != null)
                {
                    Level8OvertakeManager.Instance.TriggerLevelCompletion();
                }
            }
        }
    }
}
