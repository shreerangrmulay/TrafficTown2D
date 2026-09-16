using System;
using UnityEngine;
using TrafficTown2D.Core;
using TrafficTown2D.Player;
using TrafficTown2D.Traffic;
using TrafficTown2D.UI;

namespace TrafficTown2D.Gameplay
{
    public sealed class YieldingController : MonoBehaviour
    {
        [SerializeField] private CarPlayerController playerCar;
        [SerializeField] private PedestrianAI pedestrian;
        [SerializeField] private Transform pedestrianDestination;
        [SerializeField] private Transform crossingZone;
        [SerializeField] private float triggerDistance = 8f;
        [SerializeField] private FeedbackController feedback;
        [SerializeField] private LevelUIController levelUI;

        private bool pedestrianSpawned;
        private bool levelCompleted;
        private bool levelFailed;

        private void Awake()
        {
            if (playerCar == null) playerCar = GetComponent<CarPlayerController>();
            if (crossingZone == null)
            {
                GameObject zebra = GameObject.Find("ZebraCrossing");
                if (zebra != null) crossingZone = zebra.transform;
            }
            if (feedback == null) feedback = FindAnyObjectByType<FeedbackController>();
            if (levelUI == null) levelUI = FindAnyObjectByType<LevelUIController>();
        }

        private void Start()
        {
            Time.timeScale = 1f;
            GameManager.Instance?.SetState(GameState.Playing);
            feedback?.Show("Drive forward and yield to pedestrians at the crosswalk.");
        }

        private void Update()
        {
            if (levelCompleted || levelFailed || playerCar == null) return;

            float distanceToCrossing = Mathf.Abs(playerCar.transform.position.x - crossingZone.position.x);

            if (!pedestrianSpawned && distanceToCrossing < triggerDistance)
            {
                pedestrianSpawned = true;
                if (pedestrian != null && pedestrianDestination != null)
                {
                    pedestrian.StartWalking(pedestrianDestination);
                    feedback?.Show("A pedestrian is crossing! Slow down and yield.");
                }
            }

            if (pedestrianSpawned && playerCar.transform.position.x > crossingZone.position.x + 2f)
            {
                CompleteLevel();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (levelFailed || levelCompleted) return;

            if (collision.gameObject.GetComponent<PedestrianAI>() != null)
            {
                FailLevel("You hit a pedestrian! Always yield at crosswalks.");
            }
            else if (collision.gameObject.GetComponent<VehicleController>() != null)
            {
                FailLevel("You crashed into a vehicle!");
            }
        }

        private void CompleteLevel()
        {
            levelCompleted = true;
            feedback?.Show("Great job! You yielded to the pedestrian safely.");
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
