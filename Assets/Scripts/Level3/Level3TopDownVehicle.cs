using UnityEngine;

namespace TrafficTown2D.Level3
{
    public enum VehicleCategory
    {
        Sedan,
        Taxi,
        Bus,
        Ambulance
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class Level3TopDownVehicle : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private VehicleCategory category = VehicleCategory.Sedan;
        [SerializeField] private float speed = 4f;
        [SerializeField] private Vector2 moveDirection = Vector2.up;
        [SerializeField] private float frontSensorDistance = 2.4f;
        [SerializeField] private LayerMask obstacleLayers;

        [Header("Emergency Vehicle Visuals")]
        [SerializeField] private SpriteRenderer beaconBlue;
        [SerializeField] private SpriteRenderer beaconRed;
        [SerializeField] private float beaconFlashRate = 8f;

        private Rigidbody2D rb;
        private bool isBlocked;
        private bool isStoppedBySignal;
        private float beaconTimer;
        private Vector3 spawnPosition;
        private float travelDistanceMax = 120f;

        public VehicleCategory Category => category;
        public float Speed => speed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            spawnPosition = transform.position;
        }

        private void Start()
        {
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                // Align visual rotation to movement direction
                float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void Update()
        {
            CheckFrontObstacle();
            UpdateEmergencyBeacons();

            // Auto recycle if travelled too far
            if (Vector3.Distance(transform.position, spawnPosition) > travelDistanceMax)
            {
                gameObject.SetActive(false);
            }
        }

        private void FixedUpdate()
        {
            if (isBlocked || isStoppedBySignal)
            {
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                rb.linearVelocity = moveDirection.normalized * speed;
            }
        }

        private void CheckFrontObstacle()
        {
            // Cast ray ahead in moving direction to avoid rear-ending vehicles
            Vector2 origin = (Vector2)transform.position + moveDirection.normalized * 0.8f;
            RaycastHit2D hit = Physics2D.Raycast(origin, moveDirection.normalized, frontSensorDistance, obstacleLayers);

            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                // If it's another vehicle or player stopped ahead, stop
                isBlocked = true;
            }
            else
            {
                isBlocked = false;
            }
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

        public void SetMovement(Vector2 direction, float targetSpeed)
        {
            moveDirection = direction.normalized;
            speed = targetSpeed;
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void SetStoppedBySignal(bool stop)
        {
            isStoppedBySignal = stop;
        }

        public void SetBeacons(SpriteRenderer red, SpriteRenderer blue)
        {
            beaconRed = red;
            beaconBlue = blue;
        }
    }
}
