using UnityEngine;
using TrafficTown2D.Player;

namespace TrafficTown2D.Gameplay
{
    /// <summary>
    /// Marks the entry/exit area of a roundabout. Tracks whether the player
    /// yielded before entering and whether they signaled before exiting.
    /// </summary>
    public sealed class RoundaboutZone : MonoBehaviour
    {
        [SerializeField] private bool isEntry = true;

        public bool IsEntry => isEntry;
        public bool PlayerInside { get; private set; }

        public event System.Action<RoundaboutZone> PlayerEntered;
        public event System.Action<RoundaboutZone> PlayerExited;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<CarPlayerController>() != null)
            {
                PlayerInside = true;
                PlayerEntered?.Invoke(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<CarPlayerController>() != null)
            {
                PlayerInside = false;
                PlayerExited?.Invoke(this);
            }
        }
    }
}
