using System;
using UnityEngine;

namespace TrafficTown2D.Level7
{
    public class Level7Pedestrian : MonoBehaviour
    {
        [Header("Path Points")]
        [SerializeField] private Vector3 startPosition = new Vector3(-4.2f, -8f, 0f);
        [SerializeField] private Vector3 targetPosition = new Vector3(4.2f, -8f, 0f);
        [SerializeField] private float walkSpeed = 1.8f;
        [SerializeField] private float triggerProximity = 11f;

        [Header("Visuals")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float bobFrequency = 8f;
        [SerializeField] private float bobHeight = 0.08f;

        private bool isCrossing = false;
        private bool hasCrossed = false;
        private bool yieldRewardGiven = false;
        private float stepTimer = 0f;
        private Level7PlayerCar playerCar;

        public bool IsCrossing => isCrossing;
        public bool HasCrossed => hasCrossed;

        public event Action CrossingStarted;
        public event Action CrossingCompleted;

        private void Start()
        {
            if (transform.position != Vector3.zero && startPosition == Vector3.zero)
            {
                startPosition = transform.position;
            }
            transform.position = startPosition;
            playerCar = FindAnyObjectByType<Level7PlayerCar>();
        }

        public void ResetPedestrian()
        {
            isCrossing = false;
            hasCrossed = false;
            yieldRewardGiven = false;
            transform.position = startPosition;
            if (visualRoot != null) visualRoot.localPosition = Vector3.zero;
        }

        public void StartCrossing()
        {
            if (isCrossing || hasCrossed) return;
            isCrossing = true;
            CrossingStarted?.Invoke();
        }

        private void Update()
        {
            if (hasCrossed) return;

            // Trigger crossing on player car proximity
            if (!isCrossing && playerCar != null)
            {
                float dist = Vector2.Distance(playerCar.transform.position, transform.position);
                if (dist <= triggerProximity)
                {
                    StartCrossing();
                }
            }

            if (isCrossing)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, walkSpeed * Time.deltaTime);

                // Walking bob animation
                stepTimer += Time.deltaTime * bobFrequency;
                if (visualRoot != null)
                {
                    float offsetY = Mathf.Abs(Mathf.Sin(stepTimer)) * bobHeight;
                    visualRoot.localPosition = new Vector3(0f, offsetY, 0f);
                }

                // Check player yield behavior
                if (playerCar != null && !yieldRewardGiven)
                {
                    float dist = Vector2.Distance(playerCar.transform.position, transform.position);
                    if (dist < 6.0f)
                    {
                        if (playerCar.IsStopped || playerCar.CurrentSpeedKmh < 15f)
                        {
                            yieldRewardGiven = true;
                            if (Level7FocusManager.Instance != null)
                            {
                                Level7FocusManager.Instance.RecordSafeAction("PEDESTRIAN AWARENESS: Safely yielded at zebra crossing!", 20);
                            }
                        }
                        else if (playerCar.CurrentSpeedKmh > 30f)
                        {
                            yieldRewardGiven = true;
                            if (Level7FocusManager.Instance != null)
                            {
                                Level7FocusManager.Instance.RecordViolation("HAZARD: You sped past an active pedestrian crossing!", 30, 15f);
                            }
                        }
                    }
                }

                if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
                {
                    isCrossing = false;
                    hasCrossed = true;
                    if (visualRoot != null) visualRoot.localPosition = Vector3.zero;
                    CrossingCompleted?.Invoke();
                }
            }
        }
    }
}
