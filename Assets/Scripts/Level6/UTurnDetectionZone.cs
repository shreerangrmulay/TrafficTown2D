using System;
using UnityEngine;

namespace TrafficTown2D.Level6
{
    /// <summary>
    /// Detects when the player enters a dedicated U-turn loop, monitors turning progress,
    /// and verifies that a complete 140-deg to 180-deg heading reversal occurred safely.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class UTurnDetectionZone : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string uTurnId = "UTurn_Main";
        [SerializeField] private float requiredAngleDelta = 135f;
        [SerializeField] private float maxSafeSpeedInLoop = 30f;
        [SerializeField] private int completionScore = 30;

        [Header("Visual & Feedback")]
        [SerializeField] private string completionMessage = "[OK] Safe U-turn executed! Direction successfully reversed.";
        [SerializeField] private string speedingWarningMessage = "[!] Caution: High speed during U-turn loop! Slow down to maintain grip.";

        public event Action<string, bool> UTurnCompleted; // (uTurnId, wasClean)
        public event Action<string> FeedbackTriggered;

        private bool playerInside = false;
        private Vector2 entryHeading = Vector2.zero;
        private float maxAngleObserved = 0f;
        private bool hasCompleted = false;
        private bool wasSpeeding = false;

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Level6PlayerCar player = collision.GetComponent<Level6PlayerCar>();
            if (player != null && !hasCompleted)
            {
                playerInside = true;
                entryHeading = player.transform.up;
                maxAngleObserved = 0f;
                wasSpeeding = false;
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (!playerInside || hasCompleted) return;

            Level6PlayerCar player = collision.GetComponent<Level6PlayerCar>();
            if (player == null) return;

            Vector2 currentHeading = player.transform.up;
            float angleDelta = Vector2.Angle(entryHeading, currentHeading);
            if (angleDelta > maxAngleObserved)
            {
                maxAngleObserved = angleDelta;
            }

            if (player.CurrentSpeedKmh > maxSafeSpeedInLoop && !wasSpeeding)
            {
                wasSpeeding = true;
                FeedbackTriggered?.Invoke(speedingWarningMessage);
                if (Level6SafetyManager.Instance != null)
                {
                    Level6SafetyManager.Instance.RegisterMinorInfraction("Excessive speed in U-turn loop");
                }
            }

            if (maxAngleObserved >= requiredAngleDelta && !hasCompleted)
            {
                hasCompleted = true;
                bool isClean = !wasSpeeding;
                UTurnCompleted?.Invoke(uTurnId, isClean);
                FeedbackTriggered?.Invoke(completionMessage);

                if (Level6SafetyManager.Instance != null)
                {
                    Level6SafetyManager.Instance.RegisterSafeAction(completionScore, "Successful U-turn");
                    Level6SafetyManager.Instance.RegisterUTurnCompleted(isClean);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            Level6PlayerCar player = collision.GetComponent<Level6PlayerCar>();
            if (player != null)
            {
                playerInside = false;
            }
        }

        public void ResetZone()
        {
            hasCompleted = false;
            playerInside = false;
            maxAngleObserved = 0f;
            wasSpeeding = false;
        }
    }
}
