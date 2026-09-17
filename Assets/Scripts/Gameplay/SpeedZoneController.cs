using UnityEngine;
using TrafficTown2D.Core;
using TrafficTown2D.Player;
using TrafficTown2D.UI;

namespace TrafficTown2D.Gameplay
{
    /// <summary>
    /// Level 7 – School Zones & Speed Limits.
    /// Player must adjust speed dynamically based on posted limits.
    /// SpeedZones in the scene define limit regions. The controller checks
    /// the player's speed against the active zone's limit.
    /// </summary>
    public sealed class SpeedZoneController : MonoBehaviour
    {
        [SerializeField] private CarPlayerController playerCar;
        [SerializeField] private FeedbackController feedback;
        [SerializeField] private LevelUIController levelUI;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private SpeedZone[] speedZones;
        [SerializeField] private float destinationX = 8f;
        [SerializeField] private float speedCheckInterval = 0.5f;

        private SpeedZone activeZone;
        private int speedingViolations;
        private float speedCheckTimer;
        private bool levelCompleted;
        private bool levelFailed;
        private bool enteredSchoolZone;

        private void Awake()
        {
            if (playerCar == null) playerCar = FindAnyObjectByType<CarPlayerController>();
            if (feedback == null) feedback = FindAnyObjectByType<FeedbackController>();
            if (levelUI == null) levelUI = FindAnyObjectByType<LevelUIController>();
            if (scoreManager == null) scoreManager = FindAnyObjectByType<ScoreManager>();
        }

        private void OnEnable()
        {
            if (speedZones == null) return;
            for (int i = 0; i < speedZones.Length; i++)
            {
                if (speedZones[i] != null)
                {
                    speedZones[i].PlayerEnteredZone += OnEnteredZone;
                    speedZones[i].PlayerExitedZone += OnExitedZone;
                }
            }
        }

        private void OnDisable()
        {
            if (speedZones == null) return;
            for (int i = 0; i < speedZones.Length; i++)
            {
                if (speedZones[i] != null)
                {
                    speedZones[i].PlayerEnteredZone -= OnEnteredZone;
                    speedZones[i].PlayerExitedZone -= OnExitedZone;
                }
            }
        }

        private void Start()
        {
            Time.timeScale = 1f;
            GameManager.Instance?.SetState(GameState.Playing);
            feedback?.Show("Drive forward. Obey all posted speed limits!");
        }

        private void Update()
        {
            if (levelCompleted || levelFailed || playerCar == null) return;

            speedCheckTimer += Time.deltaTime;
            if (speedCheckTimer >= speedCheckInterval)
            {
                speedCheckTimer = 0f;
                CheckSpeed();
            }

            if (playerCar.transform.position.x >= destinationX)
            {
                CompleteLevel();
            }
        }

        private void OnEnteredZone(SpeedZone zone)
        {
            activeZone = zone;
            bool isSchool = zone.ZoneName.ToLowerInvariant().Contains("school");
            if (isSchool && !enteredSchoolZone)
            {
                enteredSchoolZone = true;
                feedback?.Show("SCHOOL ZONE! Reduce speed to " + zone.SpeedLimit + " mph.");
            }
            else
            {
                feedback?.Show("Speed limit: " + zone.SpeedLimit + " mph.");
            }
        }

        private void OnExitedZone(SpeedZone zone)
        {
            if (activeZone == zone)
            {
                activeZone = null;

                // Find any other zone the player might still be in
                if (speedZones != null)
                {
                    for (int i = 0; i < speedZones.Length; i++)
                    {
                        if (speedZones[i] != null && speedZones[i].PlayerInside)
                        {
                            activeZone = speedZones[i];
                            break;
                        }
                    }
                }
            }
        }

        private void CheckSpeed()
        {
            if (activeZone == null) return;

            // Convert game speed to a comparable scale (multiply by factor for display)
            float playerSpeed = playerCar.CurrentSpeed;
            float limit = activeZone.SpeedLimit;

            // Using a proportional check: game speed of maxSpeed maps to ~30mph
            float speedMph = (playerSpeed / playerCar.MaxSpeed) * 30f;

            if (speedMph > limit * 1.1f) // 10% tolerance
            {
                speedingViolations++;
                scoreManager?.PenalizeMistake(10);
                if (speedingViolations >= 3)
                {
                    levelFailed = true;
                    feedback?.Show("Too many speeding violations! Drive slower and obey speed limits.");
                    levelUI?.ShowFailure("Obey the posted speed limit signs.\nReduce speed in school zones.");
                    return;
                }
                feedback?.Show("SLOW DOWN! You're exceeding the " + limit + " mph speed limit.");
            }
        }

        private void CompleteLevel()
        {
            levelCompleted = true;
            bool perfect = speedingViolations == 0;
            if (perfect)
            {
                scoreManager?.RewardSafeAction(35);
                feedback?.Show("Excellent! You obeyed all speed limits perfectly.");
            }
            else
            {
                feedback?.Show("Level complete. " + speedingViolations + " speeding violation(s) recorded.");
            }
            GameManager.Instance?.SetState(GameState.LevelComplete);
            levelUI?.ShowCompletion();
        }
    }
}
