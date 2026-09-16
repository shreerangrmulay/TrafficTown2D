using UnityEngine;

namespace TrafficTown2D.Traffic
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PedestrianAI : MonoBehaviour
    {
        [SerializeField] private float walkingSpeed = 1.5f;
        [SerializeField] private Transform targetPoint;
        
        private Rigidbody2D body;
        private bool isWalking = false;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        public void StartWalking(Transform destination)
        {
            targetPoint = destination;
            isWalking = true;
        }

        private void FixedUpdate()
        {
            if (!isWalking || targetPoint == null)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 direction = (targetPoint.position - transform.position).normalized;
            body.linearVelocity = direction * walkingSpeed;

            if (Vector2.Distance(transform.position, targetPoint.position) < 0.2f)
            {
                isWalking = false;
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
