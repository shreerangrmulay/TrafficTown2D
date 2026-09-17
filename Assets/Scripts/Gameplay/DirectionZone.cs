using UnityEngine;
using TrafficTown2D.Player;

namespace TrafficTown2D.Gameplay
{
    /// <summary>
    /// Marks a one-way street zone. The allowed direction is specified as a
    /// normalised Vector2 (e.g. (1,0) for rightward travel). When the player
    /// enters traveling against the allowed direction, WrongWayDetected fires.
    /// </summary>
    public sealed class DirectionZone : MonoBehaviour
    {
        [SerializeField] private Vector2 allowedDirection = Vector2.right;
        [Tooltip("Dot-product threshold below which the player is considered going the wrong way.")]
        [SerializeField] private float wrongWayThreshold = -0.1f;

        public event System.Action<DirectionZone> WrongWayDetected;
        public Vector2 AllowedDirection => allowedDirection.normalized;
        public bool PlayerInside { get; private set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CarPlayerController car = other.GetComponent<CarPlayerController>();
            if (car == null) return;

            PlayerInside = true;
            Rigidbody2D body = car.GetComponent<Rigidbody2D>();
            if (body == null) return;

            float dot = Vector2.Dot(body.linearVelocity.normalized, allowedDirection.normalized);
            if (dot < wrongWayThreshold)
            {
                WrongWayDetected?.Invoke(this);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CarPlayerController car = other.GetComponent<CarPlayerController>();
            if (car == null) return;

            Rigidbody2D body = car.GetComponent<Rigidbody2D>();
            if (body == null || body.linearVelocity.sqrMagnitude < 0.1f) return;

            float dot = Vector2.Dot(body.linearVelocity.normalized, allowedDirection.normalized);
            if (dot < wrongWayThreshold)
            {
                WrongWayDetected?.Invoke(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<CarPlayerController>() != null)
                PlayerInside = false;
        }
    }
}
