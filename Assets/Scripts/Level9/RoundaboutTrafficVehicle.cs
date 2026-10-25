using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficTown2D.Level9
{
    /// <summary>
    /// Autonomous traffic vehicle navigating roundabouts and approach corridors.
    /// Handles waypoint traversal, circular movement, gap yielding, obstacle avoidance,
    /// and pedestrian detection.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class RoundaboutTrafficVehicle : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float cruiseSpeed = 4.2f;
        [SerializeField] private float turnSpeed = 160f;
        [SerializeField] private float safeFollowDistance = 4.5f;

        [Header("Path Following")]
        [SerializeField] private List<Vector2> waypoints = new List<Vector2>();
        [SerializeField] private bool loopPath = true;
        [SerializeField] private int currentWaypointIndex = 0;

        [Header("Obstacle & Yielding")]
        [SerializeField] private bool isStopped;
        [SerializeField] private LayerMask obstacleLayers;

        private Rigidbody2D rb;
        private float currentSpeed;
        private SpriteRenderer spriteRenderer;

        public float CurrentSpeed => currentSpeed;
        public bool IsStopped => isStopped;
        public Vector2 ForwardVector => transform.up;

        public void SetupPath(IEnumerable<Vector2> points, bool loop, float speed = 4.2f)
        {
            waypoints = new List<Vector2>(points);
            loopPath = loop;
            cruiseSpeed = speed;
            currentWaypointIndex = 0;
            if (waypoints.Count > 0)
            {
                transform.position = waypoints[0];
            }
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.isKinematic = true;
            }
            spriteRenderer = GetComponent<SpriteRenderer>();
            currentSpeed = cruiseSpeed;
            obstacleLayers = LayerMask.GetMask("Default");
        }

        private void Update()
        {
            if (waypoints == null || waypoints.Count < 2) return;

            // 1. Detect obstacles ahead (player car, other traffic, pedestrians)
            bool obstacleAhead = CheckObstacleAhead();

            // Smooth speed adjustment
            float targetSpeed = obstacleAhead ? 0f : cruiseSpeed;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, (obstacleAhead ? 12f : 4f) * Time.deltaTime);
            isStopped = currentSpeed < 0.1f;

            // 2. Waypoint traversal
            Vector2 currentPos = transform.position;
            Vector2 targetWp = waypoints[currentWaypointIndex];
            Vector2 toTarget = targetWp - currentPos;

            if (toTarget.sqrMagnitude < 0.8f)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Count)
                {
                    if (loopPath)
                    {
                        currentWaypointIndex = 0;
                    }
                    else
                    {
                        // Path completed, despawn or disable
                        Destroy(gameObject);
                        return;
                    }
                }
                targetWp = waypoints[currentWaypointIndex];
                toTarget = targetWp - currentPos;
            }

            // 3. Steering & Rotation (car faces up in 2D sprites)
            if (toTarget.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
                float currentAngle = transform.eulerAngles.z;
                float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
            }

            // 4. Movement forward
            Vector2 forward = transform.up;
            Vector2 newPos = currentPos + forward * (currentSpeed * Time.deltaTime);
            if (rb != null)
            {
                rb.MovePosition(newPos);
            }
            else
            {
                transform.position = newPos;
            }
        }

        private bool CheckObstacleAhead()
        {
            Vector2 origin = (Vector2)transform.position + (Vector2)transform.up * 1.2f;
            Vector2 dir = transform.up;

            RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, 0.65f, dir, safeFollowDistance, obstacleLayers);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i].collider;
                if (col == null || col.gameObject == gameObject) continue;

                // Ignore island colliders, curbs, or non-vehicle scenery
                if (col.name.Contains("Island") || col.name.Contains("Roundabout") || col.name.Contains("Road"))
                {
                    continue;
                }

                if (col.isTrigger)
                {
                    RoundaboutPedestrian ped = col.GetComponent<RoundaboutPedestrian>();
                    if (ped != null && ped.IsCrossing) return true;
                    continue;
                }

                // Check if obstacle is player car or another traffic vehicle
                if (col.CompareTag("Player") ||
                    col.GetComponent<Level3.Level3PlayerCar>() != null ||
                    col.GetComponent<RoundaboutTrafficVehicle>() != null)
                {
                    return true;
                }
            }

            // Also check for nearby crossing pedestrians within close radius
            Collider2D[] nearby = Physics2D.OverlapCircleAll(origin, 2.5f);
            foreach (var col in nearby)
            {
                if (col.gameObject == gameObject) continue;
                RoundaboutPedestrian ped = col.GetComponent<RoundaboutPedestrian>();
                if (ped != null && ped.IsCrossing)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<Level3.Level3PlayerCar>() != null)
            {
                if (Level9RoundaboutManager.Instance != null)
                {
                    Level9RoundaboutManager.Instance.RecordCollisionWithCar();
                }
            }
        }
    }
}
