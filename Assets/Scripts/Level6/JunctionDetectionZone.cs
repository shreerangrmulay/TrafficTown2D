using System;
using UnityEngine;

namespace TrafficTown2D.Level6
{
    public enum JunctionType
    {
        Fork,
        TJunction,
        SharpTurn
    }

    /// <summary>
    /// Trigger zone placed before complex road junctions (Fork, T-Junction, Sharp Turn)
    /// to issue driving alerts, navigation guidance, and track route decisions.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class JunctionDetectionZone : MonoBehaviour
    {
        [Header("Junction Settings")]
        [SerializeField] private JunctionType junctionType = JunctionType.Fork;
        [SerializeField] private string junctionName = "North Fork";
        [SerializeField] private float approachSpeedLimitKmh = 35f;

        [Header("Messages & Guidance")]
        [TextArea(2, 4)]
        [SerializeField] private string approachAdvice = "FORK AHEAD: Left route flooded. Detour via Right route!";
        [SerializeField] private string speedingWarning = "[!] Slow down! Approach junction with caution under extreme weather.";

        [Header("Route Scoring")]
        [SerializeField] private bool awardsSafeApproach = true;
        [SerializeField] private int safeApproachScore = 15;

        public event Action<string> FeedbackTriggered;

        private bool hasTriggered = false;

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (hasTriggered) return;

            Level6PlayerCar player = collision.GetComponent<Level6PlayerCar>();
            if (player != null)
            {
                hasTriggered = true;
                FeedbackTriggered?.Invoke(approachAdvice);

                if (player.CurrentSpeedKmh > approachSpeedLimitKmh)
                {
                    FeedbackTriggered?.Invoke(speedingWarning);
                    if (Level6SafetyManager.Instance != null)
                    {
                        Level6SafetyManager.Instance.RegisterMinorInfraction($"Approached {junctionName} too fast");
                    }
                }
                else if (awardsSafeApproach)
                {
                    if (Level6SafetyManager.Instance != null)
                    {
                        Level6SafetyManager.Instance.RegisterSafeAction(safeApproachScore, $"Safe approach to {junctionName}");
                    }
                }
            }
        }

        public void ResetZone()
        {
            hasTriggered = false;
        }
    }
}
