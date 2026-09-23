using System;
using UnityEngine;

namespace TrafficTown2D.Level7
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Level7TrafficVehicle : MonoBehaviour
    {
        [Header("Path Following")]
        [SerializeField] private Vector3[] waypoints;
        [SerializeField] private float speed = 4.2f;
        [SerializeField] private float stopDistance = 3.5f;
        [SerializeField] private bool loopPath = true;

        [Header("Traffic Light Link")]
        [SerializeField] private Level7TrafficLight linkedLight;
        [SerializeField] private Vector3 lightStopLine;
        [SerializeField] private float lightCheckRadius = 4.0f;

        private Rigidbody2D rb;
        private int currentWaypointIndex = 0;
        private bool isStoppedByObstacle = false;

        public void SetWaypoints(Vector3[] points, bool loop = true)
        {
            waypoints = points;
            loopPath = loop;
            currentWaypointIndex = 0;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        private void Update()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            // Check if blocked by red light
            if (linkedLight != null && linkedLight.CurrentState == SignalColorState.Red)
            {
                float distToStopLine = Vector2.Distance(transform.position, lightStopLine);
                if (distToStopLine < lightCheckRadius)
                {
                    return; // Wait at red light
                }
            }

            // Check forward obstacle with raycast / circlecast
            Vector2 forwardDir = transform.up;
            RaycastHit2D hit = Physics2D.CircleCast(transform.position + (Vector3)forwardDir * 1.0f, 0.7f, forwardDir, stopDistance);
            if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger)
            {
                // Obstacle ahead
                return;
            }

            Vector3 target = waypoints[currentWaypointIndex];
            Vector3 moveDir = (target - transform.position).normalized;

            if (moveDir != Vector3.zero)
            {
                float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) < 0.4f)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Length)
                {
                    if (loopPath) currentWaypointIndex = 0;
                    else enabled = false;
                }
            }
        }
    }
}
