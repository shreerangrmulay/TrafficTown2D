using UnityEngine;
using TrafficTown2D.Core;
using TrafficTown2D.Player;
using TrafficTown2D.Traffic;
using TrafficTown2D.UI;

namespace TrafficTown2D.Gameplay
{
    /// <summary>
    /// Level 10 – The Ultimate Commute.
    /// A long drive through the city that tests all previously learned traffic rules.
    /// CheckpointZones mark sections; each section verifies a specific rule.
    /// </summary>
    public sealed class UltimateCommuteController : MonoBehaviour
    {
        [SerializeField] private CarPlayerController playerCar;
        [SerializeField] private FeedbackController feedback;
        [SerializeField] private LevelUIController levelUI;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private CheckpointZone[] checkpoints;
        [SerializeField] private float destinationX = 14f;

        private int checkpointsReached;
        private int totalCheckpoints;
        private int ruleViolations;
        private bool levelCompleted;
        private bool levelFailed;

        private void Awake()
        {
            if (playerCar == null) playerCar = FindAnyObjectByType<CarPlayerController>();
            if (feedback == null) feedback = FindAnyObjectByType<FeedbackController>();
            if (levelUI == null) levelUI = FindAnyObjectByType<LevelUIController>();
            if (scoreManager == null) scoreManager = FindAnyObjectByType<ScoreManager>();
        }

        private void OnEnable()
        {
            if (checkpoints == null) return;
            totalCheckpoints = checkpoints.Length;
            for (int i = 0; i < checkpoints.Length; i++)
            {
                if (checkpoints[i] != null)
                    checkpoints[i].PlayerReached += OnCheckpointReached;
            }
        }

        private void OnDisable()
        {
            if (checkpoints == null) return;
            for (int i = 0; i < checkpoints.Length; i++)
            {
                if (checkpoints[i] != null)
                    checkpoints[i].PlayerReached -= OnCheckpointReached;
            }
        }

        private void Start()
        {
            Time.timeScale = 1f;
            GameManager.Instance?.SetState(GameState.Playing);
            feedback?.Show("The Ultimate Commute! Apply everything you've learned. Drive safely!");
        }

        private void Update()
        {
            if (levelCompleted || levelFailed || playerCar == null) return;

            if (playerCar.transform.position.x >= destinationX)
            {
                CompleteLevel();
            }
        }

        private void OnCheckpointReached(CheckpointZone checkpoint)
        {
            checkpointsReached++;
            scoreManager?.RewardSafeAction(10);
            feedback?.Show("Checkpoint " + checkpointsReached + "/" + totalCheckpoints +
                           ": " + checkpoint.RuleDescription);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (levelFailed || levelCompleted) return;

            if (collision.gameObject.GetComponent<VehicleController>() != null ||
                collision.gameObject.GetComponent<PedestrianAI>() != null ||
                collision.gameObject.GetComponent<CyclistAI>() != null ||
                collision.gameObject.GetComponent<EmergencyVehicleAI>() != null)
            {
                ruleViolations++;
                if (ruleViolations >= 3)
                {
                    FailLevel("Too many collisions! Review all traffic rules and try again.");
                    return;
                }
                scoreManager?.PenalizeMistake(15);
                feedback?.Show("COLLISION! Be more careful. (" + ruleViolations + "/3 strikes)");
            }
        }

        private void CompleteLevel()
        {
            levelCompleted = true;
            bool perfect = ruleViolations == 0 && checkpointsReached >= totalCheckpoints;
            if (perfect)
            {
                scoreManager?.RewardSafeAction(50);
                feedback?.Show("🏆 MASTER DRIVER! Perfect commute with zero violations!");
            }
            else if (ruleViolations == 0)
            {
                scoreManager?.RewardSafeAction(30);
                feedback?.Show("Great commute! No violations. You're a safe driver!");
            }
            else
            {
                feedback?.Show("Commute complete with " + ruleViolations + " violation(s). Keep practicing!");
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
