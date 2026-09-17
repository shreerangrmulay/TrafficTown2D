using UnityEngine;
using TrafficTown2D.Core;
using TrafficTown2D.Player;
using TrafficTown2D.Traffic;
using TrafficTown2D.UI;

namespace TrafficTown2D.Gameplay
{
    /// <summary>
    /// Level 8 – Emergency Vehicles.
    /// Player drives normally until an emergency vehicle approaches with sirens.
    /// Player must pull over to the side and stop until the emergency vehicle passes.
    /// </summary>
    public sealed class EmergencyVehicleController : MonoBehaviour
    {
        [SerializeField] private CarPlayerController playerCar;
        [SerializeField] private FeedbackController feedback;
        [SerializeField] private LevelUIController levelUI;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private Transform emergencySpawnPoint;
        [SerializeField] private float triggerDistance = 6f;
        [SerializeField] private float shoulderY = -2.5f;
        [SerializeField] private float shoulderTolerance = 0.8f;
        [SerializeField] private float pullOverTimeLimit = 4f;
        [SerializeField] private float destinationX = 8f;

        private EmergencyVehicleAI activeEmergency;
        private bool emergencySpawned;
        private bool emergencyApproaching;
        private bool pulledOver;
        private bool emergencyPassed;
        private bool failedToPullOver;
        private bool levelCompleted;
        private bool levelFailed;
        private float emergencyTimer;

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
            feedback?.Show("Drive forward. React when you hear emergency sirens!");
        }

        private void Update()
        {
            if (levelCompleted || levelFailed || playerCar == null) return;

            // Spawn emergency vehicle when player reaches trigger distance
            if (!emergencySpawned && playerCar.transform.position.x >= triggerDistance)
            {
                SpawnEmergencyVehicle();
            }

            if (emergencyApproaching && !emergencyPassed)
            {
                UpdateEmergencyResponse();
            }

            if (emergencyPassed && playerCar.transform.position.x >= destinationX)
            {
                CompleteLevel();
            }
        }

        private void SpawnEmergencyVehicle()
        {
            emergencySpawned = true;
            emergencyApproaching = true;

            Vector3 spawnPos = emergencySpawnPoint != null
                ? emergencySpawnPoint.position
                : new Vector3(-10f, -1.1f, 0f);

            GameObject emergencyGO = new GameObject("EmergencyVehicle");
            emergencyGO.transform.position = spawnPos;

            Rigidbody2D body = emergencyGO.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            BoxCollider2D col = emergencyGO.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.8f, 0.8f);

            activeEmergency = emergencyGO.AddComponent<EmergencyVehicleAI>();
            activeEmergency.Configure(6f, 15f, 1f);
            activeEmergency.Destroyed += OnEmergencyPassed;

            // Create simple visual
            GameObject bodySprite = new GameObject("Body");
            bodySprite.transform.SetParent(emergencyGO.transform, false);
            SpriteRenderer sr = bodySprite.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.9f, 0.1f, 0.1f, 1f);
            sr.sortingOrder = 5;
            bodySprite.transform.localScale = new Vector3(1.8f, 0.8f, 1f);

            feedback?.Show("🚨 EMERGENCY VEHICLE! Pull over to the side and STOP!");
        }

        private void UpdateEmergencyResponse()
        {
            emergencyTimer += Time.deltaTime;

            // Check if player has pulled over
            bool onShoulder = Mathf.Abs(playerCar.transform.position.y - shoulderY) < shoulderTolerance;
            bool stopped = playerCar.CurrentSpeed < 0.2f;

            if (onShoulder && stopped && !pulledOver)
            {
                pulledOver = true;
                scoreManager?.RewardSafeAction(30);
                feedback?.Show("Good! You pulled over. Wait for the emergency vehicle to pass.");
            }

            if (emergencyTimer > pullOverTimeLimit && !pulledOver && !failedToPullOver)
            {
                failedToPullOver = true;
                scoreManager?.PenalizeMistake(25);
                feedback?.Show("You didn't pull over in time! Always yield to emergency vehicles.");
            }
        }

        private void OnEmergencyPassed(EmergencyVehicleAI vehicle)
        {
            emergencyApproaching = false;
            emergencyPassed = true;
            activeEmergency = null;

            if (pulledOver)
            {
                feedback?.Show("Emergency vehicle has passed. You may continue driving.");
            }
            else
            {
                feedback?.Show("Emergency vehicle passed. Continue to the destination.");
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (levelFailed || levelCompleted) return;

            if (collision.gameObject.GetComponent<EmergencyVehicleAI>() != null)
            {
                FailLevel("You collided with an emergency vehicle! Always pull over when sirens approach.");
            }
            else if (collision.gameObject.GetComponent<VehicleController>() != null)
            {
                FailLevel("You crashed into a vehicle!");
            }
        }

        private void CompleteLevel()
        {
            levelCompleted = true;
            bool perfect = pulledOver && !failedToPullOver;
            if (perfect)
            {
                scoreManager?.RewardSafeAction(20);
                feedback?.Show("Excellent! You reacted correctly to the emergency vehicle.");
            }
            else
            {
                feedback?.Show("Level complete. Remember to pull over for emergency vehicles!");
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
