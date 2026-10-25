using System;
using UnityEngine;

namespace TrafficTown2D.Level9
{
    /// <summary>
    /// AI Pedestrian that walks across a roundabout zebra crossing back and forth,
    /// providing an educational obstacle requiring players to slow down and yield.
    /// </summary>
    public class RoundaboutPedestrian : MonoBehaviour
    {
        [Header("Patrol Waypoints")]
        [SerializeField] private Vector2 pointA;
        [SerializeField] private Vector2 pointB;
        [SerializeField] private float walkSpeed = 1.4f;
        [SerializeField] private float waitTimeAtPavement = 1.5f;

        [Header("State")]
        [SerializeField] private bool isCrossing;
        [SerializeField] private bool targetIsB = true;
        private float pauseTimer;
        private Vector2 currentTarget;

        [Header("Visual Bobbing")]
        private float bobTimer;
        private Vector3 initialScale;
        private SpriteRenderer spriteRenderer;

        public bool IsCrossing => isCrossing;
        public Vector2 Position => transform.position;

        public event Action<RoundaboutPedestrian> OnCrossingStarted;
        public event Action<RoundaboutPedestrian> OnCrossingFinished;

        public void Setup(Vector2 startA, Vector2 startB, float speed = 1.4f)
        {
            pointA = startA;
            pointB = startB;
            walkSpeed = speed;
            transform.position = pointA;
            currentTarget = pointB;
            targetIsB = true;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            initialScale = transform.localScale;
            if (pointA == Vector2.zero && pointB == Vector2.zero)
            {
                pointA = transform.position;
                pointB = pointA + new Vector2(5.5f, 0f);
            }
            currentTarget = pointB;
        }

        private void Update()
        {
            if (pauseTimer > 0f)
            {
                pauseTimer -= Time.deltaTime;
                isCrossing = false;
                return;
            }

            isCrossing = true;

            Vector2 currentPos = transform.position;
            Vector2 dir = (currentTarget - currentPos);
            float dist = dir.magnitude;

            if (dist < 0.15f)
            {
                // Reached destination, pause at pavement
                pauseTimer = waitTimeAtPavement;
                targetIsB = !targetIsB;
                currentTarget = targetIsB ? pointB : pointA;
                OnCrossingFinished?.Invoke(this);
                return;
            }

            Vector2 moveDir = dir.normalized;
            transform.position = currentPos + moveDir * (walkSpeed * Time.deltaTime);

            // Rotate towards movement direction (in 2D top down, up vector is facing)
            float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Subtle walking bobbing animation
            bobTimer += Time.deltaTime * 8f;
            float bob = 1f + Mathf.Sin(bobTimer) * 0.08f;
            transform.localScale = new Vector3(initialScale.x * bob, initialScale.y, initialScale.z);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<Level3.Level3PlayerCar>() != null)
            {
                if (Level9RoundaboutManager.Instance != null)
                {
                    Level9RoundaboutManager.Instance.RecordMistake(40, "DANGER! Pedestrian hit on zebra crossing!");
                }
            }
        }
    }
}
