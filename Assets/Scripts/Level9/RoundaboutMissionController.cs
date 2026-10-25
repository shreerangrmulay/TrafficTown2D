using System;
using System.Collections.Generic;
using UnityEngine;
using TrafficTown2D.Level3;

namespace TrafficTown2D.Level9
{
    [Serializable]
    public class RoundaboutMissionData
    {
        public int missionNumber;
        public string title;
        public string objective;
        public string ruleTitle;
        public string ruleDetail;
        public int rewardPoints;

        public RoundaboutMissionData(int num, string t, string obj, string rTitle, string rDetail, int pts)
        {
            missionNumber = num;
            title = t;
            objective = obj;
            ruleTitle = rTitle;
            ruleDetail = rDetail;
            rewardPoints = pts;
        }
    }

    /// <summary>
    /// Coordinates the 5 mission stages of Level 9 across the 3 roundabouts,
    /// tracking progress checkpoints and advancing objectives.
    /// </summary>
    public class RoundaboutMissionController : MonoBehaviour
    {
        [Header("Mission Data")]
        [SerializeField] private int currentMissionIndex = 0;
        private List<RoundaboutMissionData> missions = new List<RoundaboutMissionData>();

        [Header("References")]
        private Level3PlayerCar playerCar;
        private RoundaboutSafetyEvaluator evaluator;

        public int CurrentMissionNumber => currentMissionIndex + 1;
        public int TotalMissions => missions.Count;
        public RoundaboutMissionData CurrentMission => (currentMissionIndex >= 0 && currentMissionIndex < missions.Count) ? missions[currentMissionIndex] : null;

        public event Action<RoundaboutMissionData> MissionStarted;
        public event Action<int, string> MissionCompleted;
        public event Action<string> StepProgressUpdated;

        private void Awake()
        {
            InitializeMissions();
        }

        private void Start()
        {
            StartMission(0);
        }

        public void BindDependencies(Level3PlayerCar car, RoundaboutSafetyEvaluator eval)
        {
            playerCar = car;
            evaluator = eval;
        }

        private void InitializeMissions()
        {
            missions.Clear();
            missions.Add(new RoundaboutMissionData(
                1,
                "ROUNDABOUT BASICS",
                "Approach Roundabout 1, slow down (< 25 km/h), yield to circulating traffic, and take 1st exit East.",
                "Yield to Right",
                "Vehicles inside the roundabout have priority. Slow down and check for a safe gap before entering.",
                20
            ));

            missions.Add(new RoundaboutMissionData(
                2,
                "BUSY TRAFFIC MERGE",
                "Drive along connecting road to Roundabout 2. Observe busy circulating traffic, wait for an opening, and merge.",
                "Safe Gap Selection",
                "Never force your way into moving traffic. Come to a halt at the give-way line if necessary.",
                20
            ));

            missions.Add(new RoundaboutMissionData(
                3,
                "PEDESTRIAN PRIORITY",
                "Navigate to Roundabout 2 North exit. Stop before the zebra crossing to let the pedestrian safely cross.",
                "Yield to Pedestrians",
                "Pedestrians crossing road entry and exit arms have right of way at zebra crossings.",
                20
            ));

            missions.Add(new RoundaboutMissionData(
                4,
                "CHOOSE YOUR EXIT",
                "Navigate Connecting Road 2 to Roundabout 3. Follow signposts: Take 3rd Exit (North) towards Town Center.",
                "Lane and Exit Selection",
                "Plan ahead, maintain clockwise circulation, and prepare to exit into the correct street.",
                20
            ));

            missions.Add(new RoundaboutMissionData(
                5,
                "ROUNDABOUT MASTER",
                "Signal with turn indicators (Q / E), clear the final crossing, and cross the Town Center Finish Line!",
                "Indicate Before Exiting",
                "Signal your exit clearly to inform oncoming drivers, cyclists, and pedestrians.",
                25
            ));
        }

        public void StartMission(int index)
        {
            if (index < 0 || index >= missions.Count) return;
            currentMissionIndex = index;
            MissionStarted?.Invoke(missions[currentMissionIndex]);
        }

        public void CompleteCurrentMission()
        {
            if (currentMissionIndex < 0 || currentMissionIndex >= missions.Count) return;

            RoundaboutMissionData finished = missions[currentMissionIndex];
            MissionCompleted?.Invoke(finished.missionNumber, finished.title);

            if (Level9RoundaboutManager.Instance != null)
            {
                Level9RoundaboutManager.Instance.AddScore(finished.rewardPoints, $"Mission {finished.missionNumber} Complete!");
            }

            int next = currentMissionIndex + 1;
            if (next < missions.Count)
            {
                StartMission(next);
            }
            else
            {
                // All 5 missions finished! Trigger level completion
                if (Level9RoundaboutManager.Instance != null)
                {
                    Level9RoundaboutManager.Instance.TriggerLevelCompletion();
                }
            }
        }

        public void OnCheckpointReached(string checkpointId)
        {
            switch (checkpointId)
            {
                case "R1_ExitEast":
                    if (currentMissionIndex == 0)
                    {
                        StepProgressUpdated?.Invoke("Roundabout 1 Cleared! Proceed along Connecting Road 1.");
                        CompleteCurrentMission();
                    }
                    break;

                case "R2_Entered":
                    if (currentMissionIndex == 1)
                    {
                        StepProgressUpdated?.Invoke("Merged safely into Roundabout 2!");
                        CompleteCurrentMission();
                    }
                    break;

                case "R2_PedestrianCleared":
                    if (currentMissionIndex == 2)
                    {
                        StepProgressUpdated?.Invoke("Pedestrian yield completed! Head towards Roundabout 3.");
                        CompleteCurrentMission();
                    }
                    break;

                case "R3_Approach":
                    if (currentMissionIndex == 3)
                    {
                        StepProgressUpdated?.Invoke("Approaching Roundabout 3: Take 3rd Exit (North)!");
                        CompleteCurrentMission();
                    }
                    break;

                case "FinishLine":
                    if (currentMissionIndex == 4)
                    {
                        StepProgressUpdated?.Invoke("Congratulations! Town Center Reached!");
                        CompleteCurrentMission();
                    }
                    break;
            }
        }
    }
}
