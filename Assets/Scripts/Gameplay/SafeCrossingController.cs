using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
        [SerializeField] private float stopZoneMinY = -2.85f;
        [SerializeField] private float stopZoneMaxY = -2.25f;
        [SerializeField] private float stopZoneHalfWidth = 2.25f;
        [SerializeField] private float requiredStopSeconds = 1f;
        [SerializeField] private float crossingHalfWidth = 1.9f;
        [SerializeField] private float safeGapDistance = 4f;
        [SerializeField] private float laneVerticalRange = 2.25f;

        public event Action Completed;

        private Rigidbody2D body;
        private bool isLevel2;
        private bool inRoad;
        private bool enteredRoad;
        private bool countedCrossing;
        private bool countedSignalWait;
        private bool usedCrossing;
        private bool unsafeCrossing;
        private bool crossingWasSafe;
        private bool completed;
        private bool stopPromptShown;
        private bool stoppedAtStopSign;
        private bool lookedLeft;
        private bool lookedRight;
        private bool bothDirectionsRewarded;
        private bool ignoredStopMistake;
        private bool closeVehicleMistake;
        private bool unsafeThisRoad;
        private float stopTimer;
        private float lastCollisionTime;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            isLevel2 = SceneManager.GetActiveScene().name == SceneLoader.SecondLevelSceneName;
            if (isLevel2)
            {
                levelUI?.UpdateLevel2Objectives(false, false, false);
            }
        }

        private void Update()
        {
            if (isLevel2 && !completed)
            {
                UpdateStopChallenge();
                UpdateLookControls();
                if (inRoad && !closeVehicleMistake && IsVehicleTooClose(out string side))
                {
                    closeVehicleMistake = true;
                    RegisterLevel2Mistake("WAIT! That vehicle is too close. Look " + side + " and wait for a safe gap.", 20);
                }
            }

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
                inRoad = true;
                ResetRoadAttemptMistakes();
                HandleRoadEntry();
            }

            if (other.GetComponent<SafeZone>() != null && enteredRoad && !completed)
            {
                CompleteLevel();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<RoadZone>() != null)
            {
                inRoad = false;
                ResetRoadAttemptMistakes();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.GetComponent<VehicleController>() == null)
            {
                return;
            }

            if (Time.time - lastCollisionTime > 1.5f)
            {
                lastCollisionTime = Time.time;
                if (isLevel2)
                {
                    RegisterLevel2Mistake("BE CAREFUL! A vehicle was approaching. Look BOTH ways before crossing.", 20);
                }
                else
                {
                    RegisterUnsafe("BE CAREFUL! A vehicle was approaching. Look BOTH ways before crossing.", true);
                }
            }

            Rigidbody2D vehicleBody = collision.rigidbody;
            if (vehicleBody != null)
            {
                vehicleBody.linearVelocity = Vector2.zero;
            }
        }

        private void HandleRoadEntry()
        {
            bool usingCrossing = IsUsingCrossing();
            if (usingCrossing)
            {
                usedCrossing = true;
                RewardCrossingUse();
            }

            if (isLevel2)
            {
                if (!stoppedAtStopSign && !ignoredStopMistake)
                {
                    ignoredStopMistake = true;
                    RegisterLevel2Mistake("STOP, LOOK BOTH WAYS, THEN CROSS.", 15);
                }

                if (!usingCrossing)
                {
                    RegisterLevel2Mistake("Use the zebra crossing to cross safely.", 15);
                }

                if (pedestrianSignal != null && pedestrianSignal.CurrentState != PedestrianSignalState.Walk)
                {
                    RegisterLevel2Mistake("Wait for WALK before crossing.", 20);
                }
                else if (!countedSignalWait)
                {
                    countedSignalWait = true;
                    scoreManager?.RewardSafeAction(10);
                    feedback?.Show("Good decision! You waited for the WALK signal.");
                }

                if (!lookedLeft || !lookedRight)
                {
                    feedback?.Show("STOP AND LOOK BOTH WAYS before crossing.");
                }

                if (!closeVehicleMistake && IsVehicleTooClose(out string side))
                {
                    closeVehicleMistake = true;
                    RegisterLevel2Mistake("WAIT! That vehicle is too close. Look " + side + " and wait for a safe gap.", 20);
                }

                return;
            }

            if (!usingCrossing)
            {
                RegisterUnsafe("STOP! Use the zebra crossing to cross safely.", false);
            }

            if (pedestrianSignal != null && pedestrianSignal.CurrentState != PedestrianSignalState.Walk)
            {
                RegisterUnsafe("WAIT! For a safe signal before crossing.", false);
            }
            else if (!countedSignalWait)
            {
                countedSignalWait = true;
                scoreManager?.RewardWait();
                feedback?.Show("Good decision! You waited for the WALK signal.");
            }
        }

        private void CompleteLevel()
        {
            completed = true;

            if (isLevel2)
            {
                bool safeCrossing = usedCrossing && stoppedAtStopSign && lookedLeft && lookedRight && !unsafeThisRoad;
                if (safeCrossing)
                {
                    scoreManager?.RewardSafeAction(30);
                    feedback?.Show("Great job! You looked both ways and crossed safely.");
                }

                levelUI?.UpdateLevel2Objectives(stoppedAtStopSign, lookedLeft && lookedRight, safeCrossing || usedCrossing);
            }
            else
            {
                if (usedCrossing && !countedCrossing)
                {
                    countedCrossing = true;
                    scoreManager?.RewardCrossing();
                    feedback?.Show("Excellent! You used the zebra crossing.");
                }

                scoreManager?.RewardCompletion();
            }

            GameManager.Instance?.SetState(GameState.LevelComplete);
            levelUI?.ShowCompletion();
            Completed?.Invoke();
        }

        private void RegisterUnsafe(string message, bool collision)
        {
            unsafeCrossing = true;
            if (collision) scoreManager?.PenalizeCollision(); else scoreManager?.PenalizeUnsafe();
            feedback?.Show(message);
        }

        private void UpdateStopChallenge()
        {
            if (!IsInsideStopZone())
            {
                stopTimer = 0f;
                return;
            }

            if (!stopPromptShown)
            {
                stopPromptShown = true;
                feedback?.Show("STOP AND LOOK BOTH WAYS");
            }

            if (stoppedAtStopSign) return;

            bool standingStill = body == null || body.linearVelocity.sqrMagnitude < 0.02f;
            if (!standingStill)
            {
                stopTimer = 0f;
                return;
            }

            stopTimer += Time.deltaTime;
            if (stopTimer >= requiredStopSeconds)
            {
                stoppedAtStopSign = true;
                scoreManager?.RewardSafeAction(10);
                feedback?.Show("Good stop. Now check both directions.");
                levelUI?.UpdateLevel2Objectives(true, lookedLeft && lookedRight, false);
            }
        }

        private void UpdateLookControls()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (!lookedLeft && (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame))
            {
                lookedLeft = true;
                scoreManager?.RewardSafeAction(5);
                feedback?.Show("LOOKING LEFT - checked.");
            }

            if (!lookedRight && (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame))
            {
                lookedRight = true;
                scoreManager?.RewardSafeAction(5);
                feedback?.Show("LOOKING RIGHT - checked.");
            }

            if (lookedLeft && lookedRight && !bothDirectionsRewarded)
            {
                bothDirectionsRewarded = true;
                feedback?.Show("BOTH DIRECTIONS CHECKED. Wait for a safe gap.");
                levelUI?.UpdateLevel2Objectives(stoppedAtStopSign, true, false);
            }
        }

        private bool IsInsideStopZone()
        {
            Vector3 position = transform.position;
            return Mathf.Abs(position.x) <= stopZoneHalfWidth && position.y >= stopZoneMinY && position.y <= stopZoneMaxY;
        }

        private bool IsUsingCrossing()
        {
            return (crossingZone != null && crossingZone.PlayerIsInside) || Mathf.Abs(transform.position.x) <= crossingHalfWidth;
        }

        private bool IsVehicleTooClose(out string side)
        {
            side = "both ways";
            VehicleController[] vehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
            for (int index = 0; index < vehicles.Length; index++)
            {
                VehicleController vehicle = vehicles[index];
                if (vehicle == null) continue;

                Vector3 vehiclePosition = vehicle.transform.position;
                if (Mathf.Abs(vehiclePosition.y) > laneVerticalRange) continue;

                Rigidbody2D vehicleBody = vehicle.GetComponent<Rigidbody2D>();
                float velocityX = vehicleBody != null ? vehicleBody.linearVelocity.x : 0f;
                if (Mathf.Abs(velocityX) < 0.05f) continue;

                bool approachingCrossing = velocityX > 0f ? vehiclePosition.x < transform.position.x : vehiclePosition.x > transform.position.x;
                if (!approachingCrossing) continue;

                float distance = Mathf.Abs(vehiclePosition.x - transform.position.x);
                if (distance <= safeGapDistance)
                {
                    side = vehiclePosition.x < transform.position.x ? "left" : "right";
                    return true;
                }
            }

            return false;
        }

        private void RewardCrossingUse()
        {
            if (countedCrossing) return;

            countedCrossing = true;
            if (isLevel2)
            {
                scoreManager?.RewardSafeAction(10);
                levelUI?.UpdateLevel2Objectives(stoppedAtStopSign, lookedLeft && lookedRight, false);
            }
        }

        private void RegisterLevel2Mistake(string message, int penalty)
        {
            unsafeCrossing = true;
            unsafeThisRoad = true;
            scoreManager?.PenalizeMistake(penalty);
            feedback?.Show(message);
        }

        private void ResetRoadAttemptMistakes()
        {
            closeVehicleMistake = false;
            unsafeThisRoad = false;
        }
    }

    public sealed class RoadZone : MonoBehaviour { }
    public sealed class SafeZone : MonoBehaviour { }
}
