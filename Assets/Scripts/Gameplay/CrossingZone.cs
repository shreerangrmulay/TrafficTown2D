using UnityEngine;

namespace TrafficTown2D.Gameplay
{
    public sealed class CrossingZone : MonoBehaviour
    {
        public bool IsLegalCrossing { get; } = true;
        public bool PlayerIsInside { get; private set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<TrafficTown2D.Player.PlayerController>() != null)
            {
                PlayerIsInside = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<TrafficTown2D.Player.PlayerController>() != null)
            {
                PlayerIsInside = false;
            }
        }
    }
}
