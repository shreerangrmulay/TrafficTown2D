using UnityEngine;
using TrafficTown2D.Player;

namespace TrafficTown2D.Gameplay
{
    /// <summary>
    /// Defines a speed-limited zone. While the player car is inside, the
    /// controller can query whether the player is exceeding the limit.
    /// </summary>
    public sealed class SpeedZone : MonoBehaviour
    {
        [SerializeField] private float speedLimit = 30f;
        [SerializeField] private string zoneName = "Zone";

        public float SpeedLimit => speedLimit;
        public string ZoneName => zoneName;
        public bool PlayerInside { get; private set; }

        public event System.Action<SpeedZone> PlayerEnteredZone;
        public event System.Action<SpeedZone> PlayerExitedZone;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<CarPlayerController>() != null)
            {
                PlayerInside = true;
                PlayerEnteredZone?.Invoke(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<CarPlayerController>() != null)
            {
                PlayerInside = false;
                PlayerExitedZone?.Invoke(this);
            }
        }
    }
}
