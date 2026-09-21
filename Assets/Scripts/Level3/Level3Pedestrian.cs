using System;
using UnityEngine;

namespace TrafficTown2D.Level3
{
    public class Level3Pedestrian : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private Vector3 targetPosition;
        [SerializeField] private float walkSpeed = 1.6f;
        [SerializeField] private bool triggerOnProximity = true;
        [SerializeField] private float triggerDistance = 9f;
        [SerializeField] private Transform playerTransform;

        [Header("Visuals")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float bobFrequency = 8f;
        [SerializeField] private float bobHeight = 0.08f;

        private bool isCrossing;
        private bool hasCrossed;
        private float stepTimer;

        public bool IsCrossing => isCrossing;
        public bool HasCrossed => hasCrossed;
        public event Action CrossingStarted;
        public event Action CrossingCompleted;

        private void Start()
        {
            if (startPosition == Vector3.zero && transform.position != Vector3.zero)
            {
                startPosition = transform.position;
            }
        }

        private void Update()
        {
            if (hasCrossed) return;

            if (!isCrossing && triggerOnProximity && playerTransform != null)
            {
                // Check if player is approaching crossing
                float dist = Vector2.Distance(playerTransform.position, transform.position);
                if (dist <= triggerDistance && playerTransform.position.y < transform.position.y)
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

                if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
                {
                    isCrossing = false;
                    hasCrossed = true;
                    if (visualRoot != null) visualRoot.localPosition = Vector3.zero;
                    CrossingCompleted?.Invoke();
                }
            }
        }

        public void StartCrossing()
        {
            if (isCrossing || hasCrossed) return;
            isCrossing = true;
            CrossingStarted?.Invoke();
        }

        public void Configure(Vector3 start, Vector3 target, Transform player)
        {
            startPosition = start;
            targetPosition = target;
            playerTransform = player;
            transform.position = start;
        }

        public void SetVisualRoot(Transform root)
        {
            visualRoot = root;
        }
    }
}
