using UnityEngine;
using TrafficTown2D.Core;
using TrafficTown2D.Player;
using TrafficTown2D.Traffic;
using TrafficTown2D.UI;

namespace TrafficTown2D.Gameplay
{
    /// <summary>
    /// Level 5 – The Stop Sign (4-way stop intersection).
    /// Player must come to a complete stop, then yield to the vehicle that
    /// arrived first (or the one on the right if simultaneous).
    /// </summary>
    public sealed class StopSignController : MonoBehaviour
    {
        [SerializeField] private CarPlayerController playerCar;
        [SerializeField] private FeedbackController feedback;
        [SerializeField] private LevelUIController levelUI;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private Transform stopLine;
        [SerializeField] private Transform intersectionCenter;
        [SerializeField] private Transform destinationPoint;
        [SerializeField] private float stopZoneRadius = 1.5f;
        [SerializeField] private float requiredStopSeconds = 1.5f;
        [SerializeField] private float destinationX = 8f;

        private bool stoppedCompletely;
        private bool yieldedCorrectly;
        private bool enteredIntersection;
        private bool levelCompleted;
        private bool levelFailed;
        private bool ignoredStopPenalised;
        private float stopTimer;

        private void Awake()
        {
            if (playerCar == null) playerCar = FindAnyObjectByType<CarPlayerController>();
            if (feedback == null) feedback = FindAnyObjectByType<FeedbackController>();
            if (levelUI == null) levelUI = FindAnyObjectByType<LevelUIController>();
            if (scoreManager == null) scoreManager = FindAnyObjectByType<ScoreManager>();
        }

        private void Start()
        {
            Time.timeScale = 1f;
            GameManager.Instance?.SetState(GameState.Playing);
            feedback?.Show("Approach the STOP sign and come to a complete stop.");
        }

        private void Update()
        {
            if (levelCompleted || levelFailed || playerCar == null) return;

            UpdateStopDetection();
            UpdateIntersectionEntry();
            CheckDestination();
        }

        private void UpdateStopDetection()
        {
            if (stoppedCompletely) return;
            if (stopLine == null) return;

            float distToStop = Vector2.Distance(playerCar.transform.position, stopLine.position);
            if (distToStop > stopZoneRadius) { stopTimer = 0f; return; }

            if (playerCar.CurrentSpeed < 0.1f)
            {
                stopTimer += Time.deltaTime;
                if (stopTimer >= requiredStopSeconds)
                {
                    stoppedCompletely = true;
                    scoreManager?.RewardSafeAction(15);
                    feedback?.Show("Good stop! Now check for other vehicles and proceed when safe.");
                }
            }
            else
            {
                stopTimer = 0f;
            }
        }

        private void UpdateIntersectionEntry()
        {
            if (enteredIntersection || intersectionCenter == null) return;

            float distToCenter = Vector2.Distance(playerCar.transform.position, intersectionCenter.position);
            if (distToCenter > 2.5f) return;

            enteredIntersection = true;

            if (!stoppedCompletely && !ignoredStopPenalised)
            {
                ignoredStopPenalised = true;
                scoreManager?.PenalizeMistake(20);
                feedback?.Show("You ran the STOP sign! Always come to a complete stop first.");
            }

            // Check if any AI vehicle is closer to the intersection (right-of-way)
            VehicleController[] vehicles = FindObjectsByType<VehicleController>(FindObjectsInactive.Exclude);
            bool vehicleHasPriority = false;
            for (int i = 0; i < vehicles.Length; i++)
            {
                if (vehicles[i] == null) continue;
                float vehicleDist = Vector2.Distance(vehicles[i].transform.position, intersectionCenter.position);
                if (vehicleDist < 3f)
                {
                    vehicleHasPriority = true;
                    break;
                }
            }

            if (vehicleHasPriority && stoppedCompletely)
            {
                // Player should have waited
                scoreManager?.PenalizeMistake(15);
                feedback?.Show("WAIT! Yield to vehicles already in or closer to the intersection.");
            }
            else if (stoppedCompletely && !vehicleHasPriority)
            {
                yieldedCorrectly = true;
                scoreManager?.RewardSafeAction(15);
                feedback?.Show("Great! You yielded correctly at the intersection.");
            }
        }

        private void CheckDestination()
        {
            if (playerCar.transform.position.x >= destinationX)
            {
                CompleteLevel();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (levelFailed || levelCompleted) return;

            if (collision.gameObject.GetComponent<VehicleController>() != null)
            {
                FailLevel("You crashed at the intersection! Always stop and yield before proceeding.");
            }
        }

        private void CompleteLevel()
        {
            levelCompleted = true;
            bool perfect = stoppedCompletely && yieldedCorrectly;
            if (perfect)
            {
                scoreManager?.RewardSafeAction(30);
                feedback?.Show("Excellent! You navigated the stop sign intersection perfectly.");
            }
            else
            {
                feedback?.Show("Level complete. Practice your stop sign technique!");
            }
            GameManager.Instance?.SetState(GameState.LevelComplete);
            levelUI?.ShowCompletion();
        }

        private void FailLevel(string reason)
        {
            levelFailed = true;
            feedback?.Show(reason);
            GameManager.Instance?.SetState(GameState.GameOver);
            Time.timeScale = 0f;
            levelUI?.ShowFailure(reason + "\n\nTry again.");
        }
    }
}
