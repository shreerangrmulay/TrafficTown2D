using System;
using UnityEngine;
using TrafficTown2D.Core;
using TrafficTown2D.Player;
using TrafficTown2D.Traffic;
using TrafficTown2D.UI;

namespace TrafficTown2D.Gameplay
{
    [RequireComponent(typeof(PlayerController))]
    public sealed class SafeCrossingController : MonoBehaviour
    {
        [SerializeField] private CrossingZone crossingZone;
        [SerializeField] private TrafficLightController trafficLight;
        [SerializeField] private PedestrianSignalController pedestrianSignal;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private FeedbackController feedback;
        [SerializeField] private LevelUIController levelUI;

        public event Action Completed;
        private bool enteredRoad;
        private bool countedCrossing;
        private bool countedSignalWait;
        private bool usedCrossing;
        private bool unsafeCrossing;
        private bool crossingWasSafe;
        private bool completed;

        private void Update()
        {
            if (crossingZone != null && crossingZone.PlayerIsInside)
            {
                usedCrossing = true;
                if (enteredRoad && pedestrianSignal != null && pedestrianSignal.CurrentState == PedestrianSignalState.Walk && !unsafeCrossing)
                {
                    crossingWasSafe = true;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<RoadZone>() != null)
            {
                enteredRoad = true;
                if (crossingZone == null || !crossingZone.PlayerIsInside)
                {
                    RegisterUnsafe("Use the zebra crossing to cross safely.", false);
                }

                if (pedestrianSignal != null && pedestrianSignal.CurrentState != PedestrianSignalState.Walk)
                {
                    RegisterUnsafe("Always wait for the WALK signal before crossing.", false);
                }
                else if (!countedSignalWait)
                {
                    countedSignalWait = true;
                    scoreManager?.RewardWait();
                    feedback?.Show("Great job! You waited for the WALK signal.");
                }
            }

            if (other.GetComponent<SafeZone>() != null && enteredRoad && usedCrossing && crossingWasSafe && !completed)
            {
                completed = true;
                if (!countedCrossing)
                {
                    countedCrossing = true;
                    scoreManager?.RewardCrossing();
                    feedback?.Show("Excellent! You used the zebra crossing.");
                }

                scoreManager?.RewardCompletion();
                GameManager.Instance?.SetState(GameState.LevelComplete);
                levelUI?.ShowCompletion();
                Completed?.Invoke();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.GetComponent<VehicleController>() == null)
            {
                return;
            }

            RegisterUnsafe("Be careful! Vehicles have the right of way.", true);
            Rigidbody2D vehicleBody = collision.rigidbody;
            if (vehicleBody != null)
            {
                vehicleBody.linearVelocity = Vector2.zero;
            }
        }

        private void RegisterUnsafe(string message, bool collision)
        {
            unsafeCrossing = true;
            if (collision) scoreManager?.PenalizeCollision(); else scoreManager?.PenalizeUnsafe();
            feedback?.Show(message);
        }
    }

    public sealed class RoadZone : MonoBehaviour { }
    public sealed class SafeZone : MonoBehaviour { }
}
