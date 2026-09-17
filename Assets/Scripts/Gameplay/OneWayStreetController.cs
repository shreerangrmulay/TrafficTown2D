using UnityEngine;
using TrafficTown2D.Core;
using TrafficTown2D.Player;
using TrafficTown2D.Traffic;
using TrafficTown2D.UI;

namespace TrafficTown2D.Gameplay
{
    /// <summary>
    /// Level 6 – One-Way Streets.
    /// Player must reach the destination without entering a one-way street
    /// in the wrong direction. DirectionZones placed in the scene fire events
    /// when the player drives against the allowed direction.
    /// </summary>
    public sealed class OneWayStreetController : MonoBehaviour
    {
        [SerializeField] private CarPlayerController playerCar;
        [SerializeField] private FeedbackController feedback;
        [SerializeField] private LevelUIController levelUI;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private DirectionZone[] directionZones;
        [SerializeField] private float destinationX = 8f;

        private int wrongWayCount;
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
            if (directionZones == null) return;
            for (int i = 0; i < directionZones.Length; i++)
            {
                if (directionZones[i] != null)
                    directionZones[i].WrongWayDetected += OnWrongWay;
            }
        }

        private void OnDisable()
        {
            if (directionZones == null) return;
            for (int i = 0; i < directionZones.Length; i++)
            {
                if (directionZones[i] != null)
                    directionZones[i].WrongWayDetected -= OnWrongWay;
            }
        }

        private void Start()
        {
            Time.timeScale = 1f;
            GameManager.Instance?.SetState(GameState.Playing);
            feedback?.Show("Drive to the destination. Watch for one-way street signs!");
        }

        private void Update()
        {
            if (levelCompleted || levelFailed || playerCar == null) return;

            if (playerCar.transform.position.x >= destinationX)
            {
                CompleteLevel();
            }
        }

        private void OnWrongWay(DirectionZone zone)
        {
            if (levelCompleted || levelFailed) return;

            wrongWayCount++;
            if (wrongWayCount >= 3)
            {
                FailLevel("Too many wrong-way entries! Watch for Do Not Enter signs.");
                return;
            }

            scoreManager?.PenalizeMistake(20);
            feedback?.Show("WRONG WAY! You entered a one-way street in the wrong direction.");
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (levelFailed || levelCompleted) return;

            if (collision.gameObject.GetComponent<VehicleController>() != null)
            {
                FailLevel("You crashed into oncoming traffic on a one-way street!");
            }
        }

        private void CompleteLevel()
        {
            levelCompleted = true;
            bool perfect = wrongWayCount == 0;
            if (perfect)
            {
                scoreManager?.RewardSafeAction(30);
                feedback?.Show("Perfect! You navigated all one-way streets correctly.");
            }
            else
            {
                feedback?.Show("Level complete, but watch those one-way signs next time!");
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
