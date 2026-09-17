using UnityEngine;
using TrafficTown2D.Core;
using TrafficTown2D.Player;
using TrafficTown2D.Traffic;
using TrafficTown2D.UI;

namespace TrafficTown2D.Gameplay
{
    /// <summary>
    /// Level 9 – Roundabouts.
    /// Player must yield to traffic inside the roundabout before entering,
    /// and use turn signals (Q/E) before exiting.
    /// </summary>
    public sealed class RoundaboutController : MonoBehaviour
    {
        [SerializeField] private CarPlayerController playerCar;
        [SerializeField] private FeedbackController feedback;
        [SerializeField] private LevelUIController levelUI;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private RoundaboutZone entryZone;
        [SerializeField] private RoundaboutZone exitZone;
        [SerializeField] private float destinationX = 8f;

        private bool yieldedBeforeEntry;
        private bool enteredRoundabout;
        private bool exitedRoundabout;
        private bool signaledExit;
        private bool levelCompleted;
        private bool levelFailed;
        private bool yieldChecked;
        private float yieldTimer;
        private const float RequiredYieldSeconds = 1.0f;

        private void Awake()
        {
            if (playerCar == null) playerCar = FindAnyObjectByType<CarPlayerController>();
            if (feedback == null) feedback = FindAnyObjectByType<FeedbackController>();
            if (levelUI == null) levelUI = FindAnyObjectByType<LevelUIController>();
            if (scoreManager == null) scoreManager = FindAnyObjectByType<ScoreManager>();
        }

        private void OnEnable()
        {
            if (entryZone != null)
            {
                entryZone.PlayerEntered += OnEntryZoneEntered;
            }
            if (exitZone != null)
            {
                exitZone.PlayerEntered += OnExitZoneEntered;
                exitZone.PlayerExited += OnExitZoneExited;
            }
            if (playerCar != null)
            {
                playerCar.TurnSignalChanged += OnTurnSignal;
            }
        }

        private void OnDisable()
        {
            if (entryZone != null)
            {
                entryZone.PlayerEntered -= OnEntryZoneEntered;
            }
            if (exitZone != null)
            {
                exitZone.PlayerEntered -= OnExitZoneEntered;
                exitZone.PlayerExited -= OnExitZoneExited;
            }
            if (playerCar != null)
            {
                playerCar.TurnSignalChanged -= OnTurnSignal;
            }
        }

        private void Start()
        {
            Time.timeScale = 1f;
            GameManager.Instance?.SetState(GameState.Playing);
            feedback?.Show("Approach the roundabout. Yield to traffic inside before entering.");
        }

        private void Update()
        {
            if (levelCompleted || levelFailed || playerCar == null) return;

            // Track yield time near entry
            if (!enteredRoundabout && entryZone != null && entryZone.PlayerInside)
            {
                if (playerCar.CurrentSpeed < 0.2f)
                {
                    yieldTimer += Time.deltaTime;
                    if (yieldTimer >= RequiredYieldSeconds && !yieldChecked)
                    {
                        yieldChecked = true;
                        yieldedBeforeEntry = true;
                        scoreManager?.RewardSafeAction(15);
                        feedback?.Show("Good yield! Proceed into the roundabout when clear.");
                    }
                }
            }

            if (exitedRoundabout && playerCar.transform.position.x >= destinationX)
            {
                CompleteLevel();
            }
        }

        private void OnEntryZoneEntered(RoundaboutZone zone)
        {
            if (enteredRoundabout) return;
            enteredRoundabout = true;

            if (!yieldedBeforeEntry)
            {
                // Check if any vehicle is inside the roundabout
                VehicleController[] vehicles = FindObjectsByType<VehicleController>(FindObjectsInactive.Exclude);
                bool trafficInside = false;
                for (int i = 0; i < vehicles.Length; i++)
                {
                    if (vehicles[i] == null) continue;
                    float dist = Vector2.Distance(vehicles[i].transform.position, zone.transform.position);
                    if (dist < 5f)
                    {
                        trafficInside = true;
                        break;
                    }
                }

                if (trafficInside)
                {
                    scoreManager?.PenalizeMistake(20);
                    feedback?.Show("You must YIELD to traffic inside the roundabout before entering!");
                }
                else
                {
                    scoreManager?.RewardSafeAction(10);
                    feedback?.Show("No traffic detected. You may enter the roundabout.");
                }
            }
        }

        private void OnExitZoneEntered(RoundaboutZone zone)
        {
            // Check if signal was active before reaching exit
            if (playerCar.RightSignalActive || playerCar.LeftSignalActive)
            {
                signaledExit = true;
                scoreManager?.RewardSafeAction(15);
                feedback?.Show("Good! You signaled your exit from the roundabout.");
            }
            else
            {
                scoreManager?.PenalizeMistake(15);
                feedback?.Show("Signal before exiting! Press Q or E to use your turn signal.");
            }
        }

        private void OnExitZoneExited(RoundaboutZone zone)
        {
            exitedRoundabout = true;
            playerCar?.CancelSignals();
            feedback?.Show("You've exited the roundabout. Continue to the destination.");
        }

        private void OnTurnSignal(int direction)
        {
            if (enteredRoundabout && !exitedRoundabout && direction != 0)
            {
                feedback?.Show(direction > 0 ? "Right signal ON — preparing to exit." : "Left signal ON.");
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (levelFailed || levelCompleted) return;

            if (collision.gameObject.GetComponent<VehicleController>() != null)
            {
                FailLevel("You crashed in the roundabout! Always yield to traffic inside.");
            }
        }

        private void CompleteLevel()
        {
            levelCompleted = true;
            bool perfect = yieldedBeforeEntry && signaledExit;
            if (perfect)
            {
                scoreManager?.RewardSafeAction(25);
                feedback?.Show("Perfect! You navigated the roundabout with proper yielding and signaling.");
            }
            else
            {
                feedback?.Show("Level complete. Practice yielding and signaling in roundabouts!");
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
