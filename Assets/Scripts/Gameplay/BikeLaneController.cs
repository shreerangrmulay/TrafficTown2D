using UnityEngine;
using TrafficTown2D.Core;
using TrafficTown2D.Player;
using TrafficTown2D.Traffic;
using TrafficTown2D.UI;

namespace TrafficTown2D.Gameplay
{
    /// <summary>
    /// Level 4 – Bike Lane Awareness.
    /// Player drives alongside cyclists in a dedicated bike lane.
    /// Must check blind spots (Q/E) before turning across the bike lane.
    /// Penalised for colliding with cyclists or entering the bike lane.
    /// </summary>
    public sealed class BikeLaneController : MonoBehaviour
    {
        [SerializeField] private CarPlayerController playerCar;
        [SerializeField] private Transform bikeLaneZone;
        [SerializeField] private Transform destinationZone;
        [SerializeField] private FeedbackController feedback;
        [SerializeField] private LevelUIController levelUI;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private float bikeLaneMinY = 0.5f;
        [SerializeField] private float bikeLaneMaxY = 1.5f;
        [SerializeField] private float destinationX = 8f;

        private bool checkedBlindSpot;
        private bool enteredBikeLane;
        private bool levelCompleted;
        private bool levelFailed;
        private bool blindSpotWarningGiven;

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
            feedback?.Show("Drive forward. Check blind spots (Q/E) before crossing the bike lane.");
        }

        private void Update()
        {
            if (levelCompleted || levelFailed || playerCar == null) return;

            // Check if player used Q/E to check blind spot
            if (!checkedBlindSpot)
            {
                if (playerCar.LeftSignalActive || playerCar.RightSignalActive)
                {
                    checkedBlindSpot = true;
                    scoreManager?.RewardSafeAction(15);
                    feedback?.Show("Good! You checked your blind spot.");
                }
            }

            // Check if player entered bike lane zone
            float playerY = playerCar.transform.position.y;
            if (playerY >= bikeLaneMinY && playerY <= bikeLaneMaxY)
            {
                if (!enteredBikeLane)
                {
                    enteredBikeLane = true;
                    if (!checkedBlindSpot && !blindSpotWarningGiven)
                    {
                        blindSpotWarningGiven = true;
                        scoreManager?.PenalizeMistake(15);
                        feedback?.Show("CAREFUL! Check your blind spot before crossing the bike lane.");
                    }
                }
            }

            // Check if reached destination
            if (playerCar.transform.position.x >= destinationX)
            {
                CompleteLevel();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (levelFailed || levelCompleted) return;

            if (collision.gameObject.GetComponent<CyclistAI>() != null)
            {
                FailLevel("You hit a cyclist! Always check blind spots before turning across a bike lane.");
            }
            else if (collision.gameObject.GetComponent<VehicleController>() != null)
            {
                FailLevel("You crashed into a vehicle!");
            }
        }

        private void CompleteLevel()
        {
            levelCompleted = true;
            if (checkedBlindSpot)
            {
                scoreManager?.RewardSafeAction(30);
                feedback?.Show("Excellent! You respected the bike lane and checked your blind spots.");
            }
            else
            {
                feedback?.Show("Level complete, but remember to check blind spots next time!");
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
