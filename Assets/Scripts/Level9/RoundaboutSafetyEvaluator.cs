using System;
using System.Collections.Generic;
using UnityEngine;
using TrafficTown2D.Level3;

namespace TrafficTown2D.Level9
{
    public enum RoundaboutPillStatus
    {
        Clear,
        SlowDownApproach,
        GiveWayTraffic,
        SafeGapMerge,
        WrongWay,
        IndicateExit,
        YieldPedestrian
    }

    /// <summary>
    /// Evaluates driving actions across all 3 roundabouts:
    /// speed control, give-way compliance, gap selection, circulation direction,
    /// turn indicators, and pedestrian safety.
    /// </summary>
    public class RoundaboutSafetyEvaluator : MonoBehaviour
    {
        [Header("Evaluator Settings")]
        [SerializeField] private float maxSafeApproachSpeedKmh = 25f;
        [SerializeField] private float giveWayConflictDistance = 7.5f;

        [Header("State")]
        [SerializeField] private RoundaboutPillStatus currentPillStatus = RoundaboutPillStatus.Clear;
        [SerializeField] private string currentPillMessage = "Road Clear";

        private Level3PlayerCar playerCar;
        private List<RoundaboutTrafficVehicle> activeTraffic = new List<RoundaboutTrafficVehicle>();
        private List<RoundaboutPedestrian> activePedestrians = new List<RoundaboutPedestrian>();

        // Roundabout centers
        private readonly Dictionary<RoundaboutID, Vector2> roundaboutCenters = new Dictionary<RoundaboutID, Vector2>()
        {
            { RoundaboutID.Roundabout1_Basics, new Vector2(0f, 0f) },
            { RoundaboutID.Roundabout2_BusyTraffic, new Vector2(42f, 22f) },
            { RoundaboutID.Roundabout3_FinalChallenge, new Vector2(0f, 72f) }
        };

        private RoundaboutID activeRoundabout = RoundaboutID.Roundabout1_Basics;
        private bool isInsideCirculation = false;
        private bool hasYieldedAtCurrentEntry = false;
        private bool hasSignaledExit = false;
        private float wrongWayTimer = 0f;
        private float pillResetTimer = 0f;

        // Events
        public event Action<RoundaboutPillStatus, string> PillStatusChanged;
        public event Action<string> OnSafetyViolation;
        public event Action<string> OnSafeActionReward;

        public RoundaboutPillStatus CurrentPillStatus => currentPillStatus;
        public string CurrentPillMessage => currentPillMessage;
        public bool HasYieldedAtCurrentEntry => hasYieldedAtCurrentEntry;
        public bool HasSignaledExit => hasSignaledExit;

        public void BindPlayer(Level3PlayerCar car)
        {
            playerCar = car;
        }

        public void RegisterTraffic(RoundaboutTrafficVehicle vehicle)
        {
            if (!activeTraffic.Contains(vehicle)) activeTraffic.Add(vehicle);
        }

        public void RegisterPedestrian(RoundaboutPedestrian ped)
        {
            if (!activePedestrians.Contains(ped)) activePedestrians.Add(ped);
        }

        private void Update()
        {
            if (playerCar == null) return;

            // Clear temporary status pills
            if (pillResetTimer > 0f)
            {
                pillResetTimer -= Time.deltaTime;
                if (pillResetTimer <= 0f)
                {
                    SetPill(RoundaboutPillStatus.Clear, "Navigating Safely");
                }
            }

            // Continuously evaluate circulation direction if inside a roundabout
            if (isInsideCirculation)
            {
                EvaluateCirculationDirection();
            }

            // Check if player is near any crossing with a pedestrian
            EvaluateNearbyPedestrians();
        }

        public void OnZoneEntered(RoundaboutZoneTrigger zone)
        {
            if (playerCar == null) return;

            activeRoundabout = zone.roundaboutId;

            switch (zone.zoneType)
            {
                case RoundaboutZoneType.Approach:
                    EvaluateApproachSpeed(zone);
                    break;

                case RoundaboutZoneType.GiveWayLine:
                    EvaluateGiveWayAtEntry(zone);
                    break;

                case RoundaboutZoneType.Circulation:
                    isInsideCirculation = true;
                    hasSignaledExit = false;
                    break;

                case RoundaboutZoneType.ExitArm:
                    EvaluateExit(zone);
                    break;

                case RoundaboutZoneType.ZebraCrossing:
                    EvaluateZebraApproach(zone);
                    break;
            }
        }

        public void OnZoneExited(RoundaboutZoneTrigger zone)
        {
            if (zone.zoneType == RoundaboutZoneType.Circulation)
            {
                isInsideCirculation = false;
                wrongWayTimer = 0f;
            }
        }

        private void EvaluateApproachSpeed(RoundaboutZoneTrigger zone)
        {
            float speed = playerCar.CurrentSpeedKmh;
            if (speed > maxSafeApproachSpeedKmh)
            {
                SetPill(RoundaboutPillStatus.SlowDownApproach, "TOO FAST! SLOW DOWN");
                OnSafetyViolation?.Invoke($"Approach Speed: {speed:F0} km/h! Slow down before roundabout (Limit: {maxSafeApproachSpeedKmh} km/h).");
                if (Level9RoundaboutManager.Instance != null)
                {
                    Level9RoundaboutManager.Instance.RecordMistake(10, "Approached roundabout too fast. Always reduce speed before entering.");
                }
            }
            else
            {
                SetPill(RoundaboutPillStatus.GiveWayTraffic, "PREPARE TO GIVE WAY");
                OnSafeActionReward?.Invoke("Controlled approach speed! Well judged.");
                if (Level9RoundaboutManager.Instance != null)
                {
                    Level9RoundaboutManager.Instance.AddScore(10, "Controlled approach speed!");
                }
            }
        }

        private void EvaluateGiveWayAtEntry(RoundaboutZoneTrigger zone)
        {
            // Find closest circulating vehicle
            RoundaboutTrafficVehicle conflictVehicle = FindConflictingVehicle(zone.transform.position);

            if (conflictVehicle != null)
            {
                // Conflict vehicle approaching from right
                if (playerCar.CurrentSpeedKmh > 10f)
                {
                    // Player rushed through without yielding!
                    SetPill(RoundaboutPillStatus.GiveWayTraffic, "FAILED TO GIVE WAY");
                    OnSafetyViolation?.Invoke("Dangerous Merge! Vehicles in the roundabout have right of way.");
                    if (Level9RoundaboutManager.Instance != null)
                    {
                        Level9RoundaboutManager.Instance.RecordMistake(20, "Failed to yield to circulating traffic!");
                    }
                }
                else
                {
                    // Player yielded safely
                    hasYieldedAtCurrentEntry = true;
                    SetPill(RoundaboutPillStatus.GiveWayTraffic, "YIELDING - WAITING FOR GAP");
                    OnSafeActionReward?.Invoke("Good patience! Waiting for safe gap.");
                    if (Level9RoundaboutManager.Instance != null)
                    {
                        Level9RoundaboutManager.Instance.RecordSafeYield();
                    }
                }
            }
            else
            {
                // Safe gap available
                SetPill(RoundaboutPillStatus.SafeGapMerge, "SAFE GAP - MERGE NOW");
                if (Level9RoundaboutManager.Instance != null)
                {
                    Level9RoundaboutManager.Instance.AddScore(10, "Clear roundabout entry.");
                }
            }
        }

        private void EvaluateCirculationDirection()
        {
            if (!roundaboutCenters.TryGetValue(activeRoundabout, out Vector2 center)) return;

            Vector2 carPos = playerCar.transform.position;
            Vector2 radial = carPos - center;
            float distFromCenter = radial.magnitude;

            // In India/LHT, roundabout flows CLOCKWISE.
            // Tangent in clockwise direction is (radial.y, -radial.x).
            Vector2 clockwiseTangent = new Vector2(radial.y, -radial.x).normalized;
            Vector2 carForward = playerCar.transform.up;

            float alignment = Vector2.Dot(carForward, clockwiseTangent);

            // If alignment is negative and car is moving, player is going counter-clockwise (wrong way!)
            if (alignment < -0.3f && playerCar.CurrentSpeedKmh > 4f)
            {
                wrongWayTimer += Time.deltaTime;
                if (wrongWayTimer > 0.8f)
                {
                    SetPill(RoundaboutPillStatus.WrongWay, "WRONG WAY! CIRCULATE CLOCKWISE");
                    OnSafetyViolation?.Invoke("WRONG WAY! Always circulate clockwise around the island.");
                    if (Level9RoundaboutManager.Instance != null)
                    {
                        Level9RoundaboutManager.Instance.RecordMistake(25, "Driving against roundabout traffic flow!");
                    }
                    wrongWayTimer = 0f;
                }
            }
            else
            {
                wrongWayTimer = 0f;
            }
        }

        private void EvaluateExit(RoundaboutZoneTrigger zone)
        {
            // Check if player signaled exit
            bool signaled = playerCar.LeftSignalActive || playerCar.RightSignalActive;

            if (signaled)
            {
                hasSignaledExit = true;
                SetPill(RoundaboutPillStatus.Clear, "EXIT SIGNAL VERIFIED");
                OnSafeActionReward?.Invoke("Turn signal used correctly before exit!");
                if (Level9RoundaboutManager.Instance != null)
                {
                    Level9RoundaboutManager.Instance.RecordSafeExitSignal();
                }
            }
            else
            {
                SetPill(RoundaboutPillStatus.IndicateExit, "SIGNAL BEFORE EXITING");
                OnSafetyViolation?.Invoke("Always use your turn signal (Q / E) before exiting a roundabout!");
                if (Level9RoundaboutManager.Instance != null)
                {
                    Level9RoundaboutManager.Instance.RecordMistake(10, "Exited roundabout without indicating.");
                }
            }
        }

        private void EvaluateZebraApproach(RoundaboutZoneTrigger zone)
        {
            // Check if any pedestrian is crossing on this zebra crossing
            foreach (var ped in activePedestrians)
            {
                if (ped == null) continue;
                float dist = Vector2.Distance(ped.Position, zone.transform.position);
                if (dist < 4.5f && ped.IsCrossing)
                {
                    if (playerCar.CurrentSpeedKmh > 12f)
                    {
                        SetPill(RoundaboutPillStatus.YieldPedestrian, "SLOW DOWN! PEDESTRIAN CROSSING");
                    }
                }
            }
        }

        private void EvaluateNearbyPedestrians()
        {
            Vector2 carPos = playerCar.transform.position;
            foreach (var ped in activePedestrians)
            {
                if (ped == null || !ped.IsCrossing) continue;

                float dist = Vector2.Distance(carPos, ped.Position);
                if (dist < 5.0f)
                {
                    if (playerCar.CurrentSpeedKmh < 4f)
                    {
                        // Stopped for pedestrian!
                        SetPill(RoundaboutPillStatus.YieldPedestrian, "YIELDING TO PEDESTRIAN");
                    }
                    else if (playerCar.CurrentSpeedKmh > 18f)
                    {
                        // Too fast near crossing!
                        SetPill(RoundaboutPillStatus.YieldPedestrian, "DANGER! YIELD TO PEDESTRIAN");
                    }
                }
            }
        }

        private RoundaboutTrafficVehicle FindConflictingVehicle(Vector2 entryPos)
        {
            foreach (var vehicle in activeTraffic)
            {
                if (vehicle == null) continue;
                float dist = Vector2.Distance(entryPos, vehicle.transform.position);
                if (dist < giveWayConflictDistance)
                {
                    return vehicle;
                }
            }
            return null;
        }

        public void SetPill(RoundaboutPillStatus status, string message, float duration = 2.5f)
        {
            currentPillStatus = status;
            currentPillMessage = message;
            pillResetTimer = duration;
            PillStatusChanged?.Invoke(status, message);
        }
    }
}
