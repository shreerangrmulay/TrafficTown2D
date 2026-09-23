using System;
using UnityEngine;
using TrafficTown2D.Level3;

namespace TrafficTown2D.Level5
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class TrafficIntersectionVehicle : MonoBehaviour
    {
        [Header("Vehicle Configuration")]
        [SerializeField] private VehicleCategory category = VehicleCategory.Sedan;
        [SerializeField] private ApproachDirection approachDirection = ApproachDirection.South;
        [SerializeField] private TurnType turnType = TurnType.Straight;
        [SerializeField] private float cruiseSpeed = 4.8f;
        [SerializeField] private float acceleration = 6.5f;
        [SerializeField] private float brakingDecel = 10.0f;
        [SerializeField] private float frontSensorDistance = 2.6f;
        [SerializeField] private LayerMask obstacleLayers;

        [Header("Emergency Beacons (Ambulance only)")]
        [SerializeField] private SpriteRenderer beaconRed;
        [SerializeField] private SpriteRenderer beaconBlue;
        [SerializeField] private float beaconFlashRate = 8f;

        private Rigidbody2D rb;
        private TrafficRoute currentRoute;
        private int currentWaypointIndex = 0;
        private float currentSpeed = 0f;
        private bool hasEnteredIntersection = false;
        private bool isWaitingInQueue = false;
        private float stopLineCoordinate = 0f;
        private float waitingTime = 0f;
        private float beaconTimer = 0f;
        private bool isDespawned = false;
        private bool isCrashed = false;

        public VehicleCategory Category => category;
        public ApproachDirection Direction => approachDirection;
        public TurnType Turn => turnType;
        public bool HasEnteredIntersection => hasEnteredIntersection;
        public bool IsWaitingInQueue => isWaitingInQueue;
        public float WaitingTime => waitingTime;
        public float CurrentSpeed => currentSpeed;
        public bool IsCrashed => isCrashed;

        public event Action<TrafficIntersectionVehicle> VehiclePassedIntersection;
        public event Action<TrafficIntersectionVehicle> VehicleCollided;
        public event Action<TrafficIntersectionVehicle> VehicleDespawned;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        public void InitializeRoute(TrafficRoute route, VehicleCategory cat, LayerMask obstacles, Vector3? startPos = null)
        {
            currentRoute = route;
            approachDirection = route.EntryDirection;
            turnType = route.Turn;
            stopLineCoordinate = route.StopLineCoordinate;
            category = cat;
            obstacleLayers = obstacles;
            currentWaypointIndex = 1;
            hasEnteredIntersection = false;
            isWaitingInQueue = false;
            waitingTime = 0f;
            isDespawned = false;
            isCrashed = false;

            transform.position = startPos.HasValue ? startPos.Value : route.SpawnPosition;

            // Orient towards first waypoint
            if (route.Waypoints.Length > 1)
            {
                Vector2 dir = (route.Waypoints[1] - route.Waypoints[0]).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
                currentWaypointIndex = 1;
            }

            if (category == VehicleCategory.Bus)
            {
                cruiseSpeed = 3.8f;
                frontSensorDistance = 3.5f;
            }
            else if (category == VehicleCategory.Ambulance)
            {
                cruiseSpeed = 5.6f;
                frontSensorDistance = 2.8f;
            }
            else
            {
                cruiseSpeed = 4.6f;
                frontSensorDistance = 2.6f;
            }

            currentSpeed = cruiseSpeed * 0.8f;
        }

        // Backward compatibility for existing references
        public void Initialize(ApproachDirection dir, VehicleCategory cat, float stopCoord, LayerMask obstacles)
        {
            TrafficRoute defaultRoute = TrafficRouteManager.GetRoute(dir, TurnType.Straight);
            InitializeRoute(defaultRoute, cat, obstacles);
        }

        private void Update()
        {
            if (isDespawned || isCrashed) return;

            UpdateEmergencyBeacons();

            if (isWaitingInQueue)
            {
                waitingTime += Time.deltaTime;
            }
            else
            {
                waitingTime = 0f;
            }

            // Despawn once safely off-screen after passing intersection
            if (hasEnteredIntersection)
            {
                if (Mathf.Abs(transform.position.x) > 34f || Mathf.Abs(transform.position.y) > 23f)
                {
                    Despawn();
                    return;
                }
            }
        }

        private void FixedUpdate()
        {
            if (isDespawned || isCrashed || currentRoute == null) return;

            // 1. Advance waypoint navigation
            Vector3 targetWp = currentRoute.Waypoints[currentWaypointIndex];
            Vector2 toTarget = (targetWp - transform.position);
            float distToWp = toTarget.magnitude;

            if (distToWp < 1.5f && currentWaypointIndex < currentRoute.Waypoints.Length - 1)
            {
                currentWaypointIndex++;
                targetWp = currentRoute.Waypoints[currentWaypointIndex];
                toTarget = (targetWp - transform.position);
                distToWp = toTarget.magnitude;
            }

            // 2. Steer towards waypoint with straight-lane stabilization & exit protection
            if (currentWaypointIndex >= currentRoute.Waypoints.Length - 1)
            {
                // We are on the final straight exit segment heading off-screen
                Vector2 exitDir = (currentRoute.Waypoints[currentRoute.Waypoints.Length - 1] - currentRoute.Waypoints[currentRoute.Waypoints.Length - 2]).normalized;
                float dot = Vector2.Dot(exitDir, (Vector2)transform.position - (Vector2)targetWp);

                // If reached final exit waypoint or pushed past it, despawn cleanly
                if (distToWp < 1.8f || dot >= 0f)
                {
                    Despawn();
                    return;
                }

                // Strictly lock rotation to the exit heading: NEVER allow turning around or rotating backward!
                float exitAngle = Mathf.Atan2(exitDir.y, exitDir.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, exitAngle);
            }
            else if (turnType == TurnType.Straight || !hasEnteredIntersection || currentWaypointIndex >= currentRoute.Waypoints.Length - 2)
            {
                // On straight paths, lock angle cleanly in lane direction to prevent wobble/drift
                Vector2 segDir = toTarget.normalized;
                if (segDir.sqrMagnitude > 0.01f)
                {
                    float segAngle = Mathf.Atan2(segDir.y, segDir.x) * Mathf.Rad2Deg - 90f;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, 0f, segAngle), 360f * Time.fixedDeltaTime);
                }
            }
            else
            {
                // Smooth turning curve inside the intersection
                if (toTarget.sqrMagnitude > 0.01f)
                {
                    float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
                    float steerSpeed = 240f * Time.fixedDeltaTime;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, 0f, targetAngle), steerSpeed);
                }
            }

            // 3. Evaluate traffic state (Stop Line & Leading Vehicles)
            bool shouldStop = EvaluateTrafficState();

            if (shouldStop)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakingDecel * Time.fixedDeltaTime);
                if (currentSpeed < 0.1f)
                {
                    currentSpeed = 0f;
                    isWaitingInQueue = true;
                }
            }
            else
            {
                isWaitingInQueue = false;
                currentSpeed = Mathf.MoveTowards(currentSpeed, cruiseSpeed, acceleration * Time.fixedDeltaTime);
            }

            rb.linearVelocity = (Vector2)transform.up * currentSpeed;
        }

        private bool EvaluateTrafficState()
        {
            // A. Forward sensor using CircleCast to reliably detect leading vehicles across full lane width
            Vector2 sensorOrigin = (Vector2)transform.position + (Vector2)transform.up * 1.0f;
            float sensorRadius = 0.62f;
            float checkDist = (category == VehicleCategory.Bus) ? 4.2f : 3.4f;

            RaycastHit2D[] hits = Physics2D.CircleCastAll(sensorOrigin, sensorRadius, transform.up, checkDist, obstacleLayers);
            foreach (var hit in hits)
            {
                if (hit.collider != null && hit.collider.gameObject != gameObject)
                {
                    TrafficIntersectionVehicle otherVeh = hit.collider.GetComponent<TrafficIntersectionVehicle>();
                    if (otherVeh != null)
                    {
                        // Only stop for vehicles traveling in the same direction ahead of us (ignore oncoming traffic in adjacent lane)
                        if (Vector2.Dot(transform.up, otherVeh.transform.up) > 0.4f)
                        {
                            return true;
                        }
                    }

                    IntersectionPedestrian ped = hit.collider.GetComponent<IntersectionPedestrian>();
                    if (ped != null)
                    {
                        // Pedestrian crossing in crosswalk
                        return true;
                    }
                }
            }

            // B. Anti-Gridlock yielding inside intersection
            if (hasEnteredIntersection && Mathf.Abs(transform.position.x) <= 6.5f && Mathf.Abs(transform.position.y) <= 6.5f)
            {
                Collider2D[] closeColliders = Physics2D.OverlapCircleAll(transform.position, 2.2f, obstacleLayers);
                foreach (var c in closeColliders)
                {
                    if (c != null && c.gameObject != gameObject)
                    {
                        TrafficIntersectionVehicle otherVeh = c.GetComponent<TrafficIntersectionVehicle>();
                        if (otherVeh != null && otherVeh.HasEnteredIntersection)
                        {
                            Vector2 toOther = (otherVeh.transform.position - transform.position);
                            // If other car is ahead of us along our travel heading, yield to it
                            if (Vector2.Dot(transform.up, toOther) > 0.3f)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            // C. Stop line proximity and traffic light check
            if (!hasEnteredIntersection)
            {
                float distToStopLine = 0f;
                bool isPastStopLine = false;

                switch (approachDirection)
                {
                    case ApproachDirection.North:
                        isPastStopLine = transform.position.y <= stopLineCoordinate;
                        distToStopLine = transform.position.y - stopLineCoordinate;
                        break;
                    case ApproachDirection.South:
                        isPastStopLine = transform.position.y >= stopLineCoordinate;
                        distToStopLine = stopLineCoordinate - transform.position.y;
                        break;
                    case ApproachDirection.East:
                        isPastStopLine = transform.position.x <= stopLineCoordinate;
                        distToStopLine = transform.position.x - stopLineCoordinate;
                        break;
                    case ApproachDirection.West:
                        isPastStopLine = transform.position.x >= stopLineCoordinate;
                        distToStopLine = stopLineCoordinate - transform.position.x;
                        break;
                }

                if (isPastStopLine)
                {
                    hasEnteredIntersection = true;
                    isWaitingInQueue = false;
                    VehiclePassedIntersection?.Invoke(this);
                    return false;
                }

                // If approaching stop line (within smooth braking range 3.2 units)
                if (distToStopLine >= 0f && distToStopLine < 3.2f)
                {
                    bool canProceed = TrafficPhaseController.Instance != null &&
                                      TrafficPhaseController.Instance.CanVehicleProceed(approachDirection);

                    // If light is not Green (Red, Yellow, or Pedestrian Crossing)
                    if (!canProceed)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void UpdateEmergencyBeacons()
        {
            if (category != VehicleCategory.Ambulance) return;

            beaconTimer += Time.deltaTime * beaconFlashRate;
            bool state = Mathf.FloorToInt(beaconTimer) % 2 == 0;

            if (beaconRed != null)
            {
                beaconRed.color = state ? new Color(1f, 0.1f, 0.1f, 1f) : new Color(0.3f, 0f, 0f, 0.4f);
            }
            if (beaconBlue != null)
            {
                beaconBlue.color = !state ? new Color(0.1f, 0.5f, 1f, 1f) : new Color(0f, 0.1f, 0.3f, 0.4f);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isDespawned || isCrashed) return;
            TrafficIntersectionVehicle otherVeh = collision.gameObject.GetComponent<TrafficIntersectionVehicle>();
            if (otherVeh != null && !otherVeh.isDespawned)
            {
                TriggerCrash();
                if (!otherVeh.IsCrashed)
                {
                    otherVeh.TriggerCrash();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDespawned || isCrashed) return;
            TrafficIntersectionVehicle otherVeh = other.GetComponent<TrafficIntersectionVehicle>();
            if (otherVeh == null || otherVeh.isDespawned) return;

            // Differentiate normal queue following from a real collision:
            float headingDot = Vector2.Dot(transform.up, otherVeh.transform.up);
            bool isCrossTraffic = headingDot < 0.75f;
            bool isHighSpeedImpact = (currentSpeed > 1.3f || otherVeh.CurrentSpeed > 1.3f);
            bool insideIntersection = (Mathf.Abs(transform.position.x) < 6.8f && Mathf.Abs(transform.position.y) < 6.8f);

            // Crash if crossing traffic intersects, or vehicles collide inside intersection, or high-speed impact
            if (isCrossTraffic || (insideIntersection && headingDot < 0.9f) || (isHighSpeedImpact && headingDot < 0.85f))
            {
                TriggerCrash();
                if (!otherVeh.IsCrashed)
                {
                    otherVeh.TriggerCrash();
                }
            }
        }

        public void TriggerCrash()
        {
            if (isCrashed || isDespawned) return;
            isCrashed = true;
            currentSpeed = 0f;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            // Visual crash feedback: flash orange/red and tilt slightly
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(1f, 0.35f, 0.25f, 1f);
            }
            transform.Rotate(0f, 0f, UnityEngine.Random.Range(-12f, 12f));

            VehicleCollided?.Invoke(this);
            if (Level5SafetyManager.Instance != null)
            {
                Level5SafetyManager.Instance.RegisterCollision(this);
            }

            // Schedule auto-tow clearance after 2.5 seconds so road never stays blocked
            StartCoroutine(AutoTowAccidentRoutine());
        }

        private System.Collections.IEnumerator AutoTowAccidentRoutine()
        {
            yield return new WaitForSeconds(2.5f);
            if (!isDespawned)
            {
                Despawn();
            }
        }

        public void SetBeacons(SpriteRenderer red, SpriteRenderer blue)
        {
            beaconRed = red;
            beaconBlue = blue;
        }

        public void Despawn()
        {
            if (isDespawned) return;
            isDespawned = true;
            VehicleDespawned?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
