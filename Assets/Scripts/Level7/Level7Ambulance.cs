using System;
using UnityEngine;

namespace TrafficTown2D.Level7
{
    public class Level7Ambulance : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Vector3[] pathPoints;
        [SerializeField] private float speed = 5.5f;
        [SerializeField] private bool isActive = false;

        [Header("Emergency Lights")]
        [SerializeField] private SpriteRenderer redStrobe;
        [SerializeField] private SpriteRenderer blueStrobe;
        [SerializeField] private float strobeFrequency = 6f;

        [Header("Player Interaction")]
        [SerializeField] private float yieldCheckDistance = 8f;
        [SerializeField] private AudioSource sirenAudio;

        private int currentPointIndex = 0;
        private float strobeTimer = 0f;
        private bool yieldRewarded = false;
        private bool blockPenalized = false;
        private Level7PlayerCar playerCar;

        public bool IsActive => isActive;

        public void ActivateAmbulance(Vector3[] route)
        {
            if (route != null && route.Length > 0)
            {
                pathPoints = route;
                transform.position = route[0];
                currentPointIndex = 1;
            }
            isActive = true;
            yieldRewarded = false;
            blockPenalized = false;
            gameObject.SetActive(true);

            if (sirenAudio != null) sirenAudio.Play();

            if (Level7FocusManager.Instance != null)
            {
                Level7FocusManager.Instance.RecordSafeAction("EMERGENCY VEHICLE APPROACHING: Give way safely!", 10);
            }
        }

        public void DeactivateAmbulance()
        {
            isActive = false;
            if (sirenAudio != null) sirenAudio.Stop();
            gameObject.SetActive(false);
        }

        private void Start()
        {
            playerCar = FindAnyObjectByType<Level7PlayerCar>();
            if (!isActive) gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!isActive) return;

            // Strobe lights
            strobeTimer += Time.deltaTime * strobeFrequency;
            bool redOn = Mathf.Sin(strobeTimer) > 0f;
            if (redStrobe != null) redStrobe.enabled = redOn;
            if (blueStrobe != null) blueStrobe.enabled = !redOn;

            // Move along path
            if (pathPoints != null && currentPointIndex < pathPoints.Length)
            {
                Vector3 target = pathPoints[currentPointIndex];
                Vector3 dir = (target - transform.position).normalized;

                if (dir != Vector3.zero)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                    transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }

                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

                if (Vector3.Distance(transform.position, target) < 0.5f)
                {
                    currentPointIndex++;
                    if (currentPointIndex >= pathPoints.Length)
                    {
                        DeactivateAmbulance();
                        return;
                    }
                }
            }

            // Yield detection
            if (playerCar != null && !yieldRewarded && !blockPenalized)
            {
                float dist = Vector2.Distance(playerCar.transform.position, transform.position);
                if (dist < yieldCheckDistance)
                {
                    // If player is stopped or in a safe stop zone or driving slowly to the side
                    bool pulledAside = playerCar.IsStopped || Level7SafeStopZone.IsPlayerInAnySafeStop() || playerCar.CurrentSpeedKmh < 15f;
                    if (pulledAside)
                    {
                        yieldRewarded = true;
                        if (Level7FocusManager.Instance != null)
                        {
                            Level7FocusManager.Instance.RecordSafeAction("EMERGENCY YIELD: Successfully pulled aside for ambulance!", 25);
                        }
                    }
                    else if (dist < 4.0f && playerCar.ForwardSpeed > 0f)
                    {
                        blockPenalized = true;
                        if (Level7FocusManager.Instance != null)
                        {
                            Level7FocusManager.Instance.RecordViolation("FAILURE TO YIELD: Ambulance was blocked while moving!", 35, 20f);
                        }
                    }
                }
            }
        }
    }
}
