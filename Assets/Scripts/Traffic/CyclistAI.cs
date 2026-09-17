using UnityEngine;

namespace TrafficTown2D.Traffic
{
    /// <summary>
    /// Cyclist NPC that travels along a bike lane at a fixed speed.
    /// Moves in a straight line and destroys itself after passing the exit point.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class CyclistAI : MonoBehaviour
    {
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private float exitPoint = -9f;
        [SerializeField] private float direction = -1f;

        private Rigidbody2D body;

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

            body.linearVelocity = new Vector2(speed * direction, 0f);
        }
    }
}
