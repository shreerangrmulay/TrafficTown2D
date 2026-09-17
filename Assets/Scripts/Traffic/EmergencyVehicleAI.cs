using UnityEngine;

namespace TrafficTown2D.Traffic
{
    /// <summary>
    /// Emergency vehicle (ambulance / fire truck / police) that travels along the
    /// road with flashing lights. The SirenActive flag lets gameplay controllers
    /// check whether the player needs to pull over.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class EmergencyVehicleAI : MonoBehaviour
    {
        [SerializeField] private float speed = 5f;
        [SerializeField] private float exitPoint = 12f;
        [SerializeField] private float direction = 1f;
        [SerializeField] private SpriteRenderer flashingLight1;
        [SerializeField] private SpriteRenderer flashingLight2;
        [SerializeField] private float flashInterval = 0.25f;

        private Rigidbody2D body;
        private float flashTimer;
        private bool flashState;

        public bool SirenActive { get; private set; } = true;

        public event System.Action<EmergencyVehicleAI> Destroyed;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        public void Configure(float configuredSpeed, float configuredExitPoint, float configuredDirection)
        {
            speed = configuredSpeed;
            exitPoint = configuredExitPoint;
            direction = configuredDirection;
        }

        private void Update()
        {
            if (!SirenActive) return;

            flashTimer += Time.deltaTime;
            if (flashTimer >= flashInterval)
            {
                flashTimer = 0f;
                flashState = !flashState;
                if (flashingLight1 != null) flashingLight1.color = flashState ? Color.red : new Color(1f, 0f, 0f, 0.2f);
                if (flashingLight2 != null) flashingLight2.color = flashState ? Color.blue : new Color(0f, 0f, 1f, 0.2f);
            }
        }

        private void FixedUpdate()
        {
            bool pastExit = direction > 0f ? transform.position.x > exitPoint : transform.position.x < exitPoint;
            if (pastExit)
            {
                Destroyed?.Invoke(this);
                Destroy(gameObject);
                return;
            }

            body.linearVelocity = new Vector2(speed * direction, 0f);
        }
    }
}
