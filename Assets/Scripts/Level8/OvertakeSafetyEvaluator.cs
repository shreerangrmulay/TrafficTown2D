using System;
using UnityEngine;
using TrafficTown2D.Level3;
using TrafficTown2D.Player;

namespace TrafficTown2D.Level8
{
    public enum OvertakeSafetyState
    {
        SafeToOvertake,   // Green: Permitted section, gap is clear, good visibility
        Wait,             // Amber: Oncoming traffic approaching or waiting for clear gap
        Prohibited        // Red: Solid line, blind bend, pedestrian crossing, or extreme risk
    }

    public enum RoadZoneType
    {
        PermittedDashed,
        SolidLineProhibited,
        BlindBend,
        PedestrianCrossing,
        Intersection
    }

    /// <summary>
    /// Evaluates real-time road conditions, overtaking restrictions, following distance,
    /// oncoming traffic gaps, and safe lane return clearances for Level 8.
    /// </summary>
    public class OvertakeSafetyEvaluator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Level3PlayerCar playerCar;
        [SerializeField] private CarIndicatorController indicatorController;

        [Header("Active Vehicles")]
        [SerializeField] private Transform currentLeadVehicle;
        [SerializeField] private Transform currentOncomingVehicle;

        [Header("Zone Boundaries (Y Coordinates)")]
        [SerializeField] private float zone1End = 55f;   // Permitted dashed line
        [SerializeField] private float zone2End = 115f;  // Solid line prohibited
        [SerializeField] private float zone3End = 180f;  // Blind bend & hill crest
        [SerializeField] private float zone4End = 235f;  // Pedestrian crossing & intersection
        // Above zone4End is Zone 5 (Final Dynamic Zone, permitted dashed line)

        [Header("Safety Thresholds")]
        [SerializeField] private float minFollowingDistance = 3.2f;
        [SerializeField] private float minOncomingDistanceSafe = 34f;
        [SerializeField] private float minTimeToCloseSafe = 5.2f;
        [SerializeField] private float minReturnClearance = 2.8f;

        // Dynamic State
        private OvertakeSafetyState currentState = OvertakeSafetyState.SafeToOvertake;
        private RoadZoneType currentZone = RoadZoneType.PermittedDashed;
        private string statusReason = "Road Clear — Permitted to Overtake";
        private float distanceToLead = 999f;
        private float distanceToOncoming = 999f;
        private float closingSpeed = 0f;
        private bool wasInPassingLane = false;
        private bool wasFollowingTooClose = false;
        private float tailgatingTimer = 0f;

        // Public Accessors
        public OvertakeSafetyState CurrentState => currentState;
        public RoadZoneType CurrentZone => currentZone;
        public string StatusReason => statusReason;
        public float DistanceToLead => distanceToLead;
        public float DistanceToOncoming => distanceToOncoming;
        public bool IsPlayerInPassingLane => playerCar != null && playerCar.transform.position.x < -0.2f;
        public bool IsPlayerInRightLane => playerCar != null && playerCar.transform.position.x > 0.4f;
        public bool IsFollowingDistanceSafe => distanceToLead >= minFollowingDistance || !IsPlayerInRightLane;

        // Events
        public event Action<OvertakeSafetyState, string> SafetyStateChanged;
        public event Action<string> UnsafeActionDetected;
        public event Action<string> SafeActionCompleted;

        public void BindPlayer(Level3PlayerCar car)
        {
            playerCar = car;
            if (playerCar != null)
            {
                indicatorController = playerCar.GetComponent<CarIndicatorController>();
            }
        }

        public void SetActiveVehicles(Transform lead, Transform oncoming)
        {
            currentLeadVehicle = lead;
            currentOncomingVehicle = oncoming;
        }

        private void Update()
        {
            if (playerCar == null)
            {
                playerCar = FindAnyObjectByType<Level3PlayerCar>();
                if (playerCar != null && indicatorController == null)
                    indicatorController = playerCar.GetComponent<CarIndicatorController>();
                if (playerCar == null) return;
            }

            UpdateZone();
            UpdateDistanceCalculations();
            EvaluateSafetyConditions();
            MonitorLaneTransitions();
            MonitorFollowingDistance();
        }

        private void UpdateZone()
        {
            float y = playerCar.transform.position.y;
            RoadZoneType prevZone = currentZone;

            if (y < zone1End)
                currentZone = RoadZoneType.PermittedDashed;
            else if (y < zone2End)
                currentZone = RoadZoneType.SolidLineProhibited;
            else if (y < zone3End)
                currentZone = RoadZoneType.BlindBend;
            else if (y < zone4End)
                currentZone = RoadZoneType.PedestrianCrossing;
            else
                currentZone = RoadZoneType.PermittedDashed;
        }

        private void UpdateDistanceCalculations()
        {
            float playerY = playerCar.transform.position.y;

            // 1. Distance to Lead Vehicle
            if (currentLeadVehicle != null && currentLeadVehicle.gameObject.activeInHierarchy)
            {
                float leadY = currentLeadVehicle.position.y;
                distanceToLead = (leadY > playerY) ? (leadY - playerY) : 999f;
            }
            else
            {
                distanceToLead = 999f;
            }

            // 2. Distance to Oncoming Vehicle
            if (currentOncomingVehicle != null && currentOncomingVehicle.gameObject.activeInHierarchy)
            {
                float oncomingY = currentOncomingVehicle.position.y;
                if (oncomingY > playerY - 3f)
                {
                    distanceToOncoming = oncomingY - playerY;

                    float pSpeed = playerCar.ForwardSpeed;
                    float oSpeed = 5.0f; // Average oncoming speed
                    var oncomingComp = currentOncomingVehicle.GetComponent<Level3TopDownVehicle>();
                    if (oncomingComp != null) oSpeed = oncomingComp.Speed;

                    closingSpeed = Mathf.Max(1f, pSpeed + oSpeed);
                }
                else
                {
                    distanceToOncoming = 999f;
                    closingSpeed = 0f;
                }
            }
            else
            {
                distanceToOncoming = 999f;
                closingSpeed = 0f;
            }
        }

        private void EvaluateSafetyConditions()
        {
            OvertakeSafetyState newState = OvertakeSafetyState.SafeToOvertake;
            string newReason = "Path Clear — Permitted to Overtake";

            // 1. Prohibited by Road Markings or Infrastructure
            if (currentZone == RoadZoneType.SolidLineProhibited)
            {
                newState = OvertakeSafetyState.Prohibited;
                newReason = "NO OVERTAKING — Solid Line Restriction";
            }
            else if (currentZone == RoadZoneType.BlindBend)
            {
                newState = OvertakeSafetyState.Prohibited;
                newReason = "NO OVERTAKING — Blind Bend / Low Visibility";
            }
            else if (currentZone == RoadZoneType.PedestrianCrossing)
            {
                newState = OvertakeSafetyState.Prohibited;
                newReason = "NO OVERTAKING — Pedestrian Crossing Zone";
            }
            else
            {
                // 2. Permitted Section: Check Oncoming Traffic Gap
                if (distanceToOncoming < 900f)
                {
                    float timeToClose = (closingSpeed > 0.1f) ? (distanceToOncoming / closingSpeed) : 99f;

                    if (distanceToOncoming < 16f || timeToClose < 3.2f)
                    {
                        newState = OvertakeSafetyState.Prohibited;
                        newReason = $"DANGER — Oncoming Car Imminent ({distanceToOncoming:F0}m)";
                    }
                    else if (distanceToOncoming < minOncomingDistanceSafe || timeToClose < minTimeToCloseSafe)
                    {
                        newState = OvertakeSafetyState.Wait;
                        newReason = $"WAIT FOR GAP — Oncoming Traffic Close ({distanceToOncoming:F0}m)";
                    }
                    else
                    {
                        newState = OvertakeSafetyState.SafeToOvertake;
                        newReason = "SAFE TO OVERTAKE — Sufficient Gap Ahead";
                    }
                }
                else
                {
                    newState = OvertakeSafetyState.SafeToOvertake;
                    newReason = "SAFE TO OVERTAKE — Road Clear Ahead";
                }
            }

            if (newState != currentState || newReason != statusReason)
            {
                currentState = newState;
                statusReason = newReason;
                SafetyStateChanged?.Invoke(currentState, statusReason);
            }
        }

        private void MonitorLaneTransitions()
        {
            bool inPassing = IsPlayerInPassingLane;
            bool inRight = IsPlayerInRightLane;

            // Transition: Right Lane -> Passing Lane (Beginning of overtake)
            if (inPassing && !wasInPassingLane)
            {
                wasInPassingLane = true;
                OnPlayerEnteredPassingLane();
            }
            // Transition: Passing Lane -> Right Lane (Returning from overtake)
            else if (inRight && wasInPassingLane)
            {
                wasInPassingLane = false;
                OnPlayerReturnedToRightLane();
            }
        }

        private void OnPlayerEnteredPassingLane()
        {
            // Check if overtaking was prohibited
            if (currentState == OvertakeSafetyState.Prohibited)
            {
                string reason = currentZone switch
                {
                    RoadZoneType.SolidLineProhibited => "Never cross solid center lines to overtake! Road markings must be obeyed even if the road appears empty.",
                    RoadZoneType.BlindBend => "Extreme hazard! Never overtake on blind bends where forward visibility is blocked.",
                    RoadZoneType.PedestrianCrossing => "Prohibited! Overtaking near pedestrian crossings or intersections is illegal and dangerous.",
                    _ => "Dangerous manoeuvre! Overtaking was strictly prohibited in this section."
                };
                UnsafeActionDetected?.Invoke(reason);
                return;
            }

            // Check if oncoming traffic gap was insufficient
            if (currentState == OvertakeSafetyState.Wait)
            {
                UnsafeActionDetected?.Invoke("Insufficient gap! The oncoming vehicle was too close for a safe overtake. Always wait for a clear gap.");
                return;
            }

            // Check indicator usage (Q key)
            if (indicatorController != null && !indicatorController.LeftSignalActive)
            {
                SafeActionCompleted?.Invoke("Overtake initiated. (Tip: Always signal Left with [Q] before pulling out!)");
            }
            else
            {
                SafeActionCompleted?.Invoke("Excellent! Signaled Left with [Q] and moved out into the passing lane.");
            }
        }

        private void OnPlayerReturnedToRightLane()
        {
            // Check clearance ahead of the passed vehicle
            if (currentLeadVehicle != null && currentLeadVehicle.gameObject.activeInHierarchy)
            {
                float clearance = playerCar.transform.position.y - currentLeadVehicle.position.y;
                if (clearance < 0.2f)
                {
                    UnsafeActionDetected?.Invoke("Aborted overtake or returned behind the vehicle. Hold position until a safe opportunity appears.");
                }
                else if (clearance < minReturnClearance)
                {
                    UnsafeActionDetected?.Invoke("Cut off! You returned to the lane too close to the passed vehicle. Allow more forward clearance.");
                }
                else
                {
                    bool signaledRight = indicatorController != null && indicatorController.RightSignalActive;
                    if (signaledRight)
                    {
                        SafeActionCompleted?.Invoke("Perfect overtake! Maintained clearance, signaled Right with [E], and returned safely.");
                    }
                    else
                    {
                        SafeActionCompleted?.Invoke("Safe return! Completed pass with good clearance. (Tip: Signal Right with [E] when returning.)");
                    }
                }
            }
            else
            {
                SafeActionCompleted?.Invoke("Safe overtake completed! Returned safely to the driving lane.");
            }
        }

        private void MonitorFollowingDistance()
        {
            if (IsPlayerInRightLane && distanceToLead < minFollowingDistance)
            {
                tailgatingTimer += Time.deltaTime;
                if (tailgatingTimer > 2.5f && !wasFollowingTooClose)
                {
                    wasFollowingTooClose = true;
                    UnsafeActionDetected?.Invoke("Too close! Drop back to maintain a safe following distance so you have full visibility of the road ahead.");
                }
            }
            else
            {
                tailgatingTimer = 0f;
                wasFollowingTooClose = false;
            }
        }
    }
}
