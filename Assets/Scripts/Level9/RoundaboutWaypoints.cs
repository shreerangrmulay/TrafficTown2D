using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficTown2D.Level9
{
    /// <summary>
    /// Identifies the specific roundabout in the 3-roundabout sequence.
    /// </summary>
    public enum RoundaboutID
    {
        Roundabout1_Basics = 1,
        Roundabout2_BusyTraffic = 2,
        Roundabout3_FinalChallenge = 3
    }

    /// <summary>
    /// Type of trigger zone across the roundabout network.
    /// </summary>
    public enum RoundaboutZoneType
    {
        Approach,
        GiveWayLine,
        Circulation,
        ExitArm,
        ZebraCrossing,
        Checkpoint
    }

    /// <summary>
    /// Generic trigger component used across roundabout zones to broadcast
    /// player entry and exit events to the safety evaluator and mission controller.
    /// </summary>
    public class RoundaboutZoneTrigger : MonoBehaviour
    {
        [Header("Zone Identification")]
        public RoundaboutID roundaboutId = RoundaboutID.Roundabout1_Basics;
        public RoundaboutZoneType zoneType = RoundaboutZoneType.Approach;
        public int exitNumber = 1; // 1st exit, 2nd exit, 3rd exit, etc.
        public string zoneName = "ApproachZone";

        [Header("Zone Parameters")]
        public Vector2 centerPoint;
        public float approachSpeedLimitKmh = 25f;

        public event Action<RoundaboutZoneTrigger, Collider2D> PlayerEntered;
        public event Action<RoundaboutZoneTrigger, Collider2D> PlayerExited;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<Level3.Level3PlayerCar>() != null)
            {
                if (PlayerEntered != null)
                {
                    PlayerEntered.Invoke(this, other);
                }
                else
                {
                    RoundaboutSafetyEvaluator eval = UnityEngine.Object.FindAnyObjectByType<RoundaboutSafetyEvaluator>();
                    if (eval != null)
                    {
                        eval.OnZoneEntered(this);
                    }
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<Level3.Level3PlayerCar>() != null)
            {
                if (PlayerExited != null)
                {
                    PlayerExited.Invoke(this, other);
                }
                else
                {
                    RoundaboutSafetyEvaluator eval = UnityEngine.Object.FindAnyObjectByType<RoundaboutSafetyEvaluator>();
                    if (eval != null)
                    {
                        eval.OnZoneExited(this);
                    }
                }
            }
        }
    }
}
