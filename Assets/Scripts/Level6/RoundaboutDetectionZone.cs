using System;
using UnityEngine;

namespace TrafficTown2D.Level6
{
    /// <summary>
    /// Monitors roundabout navigation: entry speed, rotational flow direction (counter-clockwise),
    /// safe exiting, and yields Route Safety rewards.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class RoundaboutDetectionZone : MonoBehaviour
    {
        [Header("Roundabout Geometry")]
        [SerializeField] private Vector2 roundaboutCenter = Vector2.zero;
        [SerializeField] private float entrySpeedLimitKmh = 35f;
        [SerializeField] private int safeCompletionScore = 30;

        [Header("Feedback Messages")]
        [SerializeField] private string entryAdvice = "[ROUNDABOUT] Yield to traffic & circulate counter-clockwise.";
        [SerializeField] private string wrongWayWarning = "[!] WRONG WAY! Roundabout flow is counter-clockwise!";
        [SerializeField] private string completedMessage = "[OK] Roundabout navigated safely!";

        public event Action<bool> RoundaboutCompleted;
        public event Action<string> FeedbackTriggered;

        private bool playerInside = false;
        private Vector2 previousPosFromCenter;
        private float accumulatedRotation = 0f;
        private bool wrongWayFlagged = false;
        private bool hasCompleted = false;

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
            roundaboutCenter = transform.position;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Level6PlayerCar player = collision.GetComponent<Level6PlayerCar>();
            if (player != null)
            {
                playerInside = true;
                wrongWayFlagged = false;
                accumulatedRotation = 0f;
                hasCompleted = false;
                previousPosFromCenter = (Vector2)player.transform.position - roundaboutCenter;

                FeedbackTriggered?.Invoke(entryAdvice);

                if (player.CurrentSpeedKmh > entrySpeedLimitKmh)
                {
                    if (Level6SafetyManager.Instance != null)
                    {
                        Level6SafetyManager.Instance.RegisterMinorInfraction("Entered roundabout above safe speed limit");
                    }
                }
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (!playerInside || hasCompleted) return;

            Level6PlayerCar player = collision.GetComponent<Level6PlayerCar>();
            if (player == null) return;

            Vector2 currentPosFromCenter = (Vector2)player.transform.position - roundaboutCenter;
            float deltaAngle = Vector2.SignedAngle(previousPosFromCenter, currentPosFromCenter);
            accumulatedRotation += deltaAngle;
            previousPosFromCenter = currentPosFromCenter;

            // In top-down 2D (where +Z points out of screen), counter-clockwise motion produces positive deltaAngle.
            // Clockwise rotation produces negative deltaAngle.
            if (accumulatedRotation < -40f && !wrongWayFlagged)
            {
                wrongWayFlagged = true;
                FeedbackTriggered?.Invoke(wrongWayWarning);
                if (Level6SafetyManager.Instance != null)
                {
                    Level6SafetyManager.Instance.RegisterWrongWay("Wrong-way rotation inside roundabout!");
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            Level6PlayerCar player = collision.GetComponent<Level6PlayerCar>();
            if (player != null && playerInside)
            {
                playerInside = false;
                if (!hasCompleted && Mathf.Abs(accumulatedRotation) > 45f)
                {
                    hasCompleted = true;
                    bool clean = !wrongWayFlagged;
                    RoundaboutCompleted?.Invoke(clean);

                    if (clean)
                    {
                        FeedbackTriggered?.Invoke(completedMessage);
                        if (Level6SafetyManager.Instance != null)
                        {
                            Level6SafetyManager.Instance.RegisterSafeAction(safeCompletionScore, "Safe roundabout transit");
                        }
                    }
                }
            }
        }

        public void ResetZone()
        {
            playerInside = false;
            wrongWayFlagged = false;
            accumulatedRotation = 0f;
            hasCompleted = false;
        }
    }
}
