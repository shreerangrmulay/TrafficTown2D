using UnityEngine;

namespace TrafficTown2D.Traffic
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class VehicleController : MonoBehaviour
    {
        [SerializeField] private float speed = 2.5f;
        [SerializeField] private TrafficLightController trafficLight;
        [SerializeField] private float stoppingPoint = 2.5f;
        [SerializeField] private float exitPoint = -9f;
        [SerializeField] private float direction = -1f;

        private Rigidbody2D body;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        private void FixedUpdate()
        {
            if (direction < 0f && transform.position.x < exitPoint)
            {
                Destroy(gameObject);
                return;
            }

            if (direction > 0f && transform.position.x > exitPoint)
            {
                Destroy(gameObject);
                return;
            }

            bool stop = trafficLight != null && trafficLight.ShouldStopAt(transform.position.x, stoppingPoint, direction);
            body.linearVelocity = stop ? Vector2.zero : new Vector2(speed * direction, 0f);
        }

        public void Configure(TrafficLightController light, float configuredSpeed, float configuredStoppingPoint, float configuredExitPoint, float configuredDirection = -1f)
        {
            trafficLight = light;
            speed = configuredSpeed;
            stoppingPoint = configuredStoppingPoint;
            exitPoint = configuredExitPoint;
            direction = configuredDirection;
        }
    }
}
