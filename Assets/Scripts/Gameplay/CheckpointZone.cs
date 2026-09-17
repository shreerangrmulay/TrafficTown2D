using UnityEngine;
using TrafficTown2D.Player;

namespace TrafficTown2D.Gameplay
{
    /// <summary>
    /// A checkpoint along a route (e.g. Level 10's multi-rule commute).
    /// Each checkpoint carries a rule description and fires an event when the player reaches it.
    /// </summary>
    public sealed class CheckpointZone : MonoBehaviour
    {
        [SerializeField] private string checkpointId;
        [SerializeField] private string ruleDescription;
        [SerializeField] private int orderIndex;

        public string CheckpointId => checkpointId;
        public string RuleDescription => ruleDescription;
        public int OrderIndex => orderIndex;
        public bool Reached { get; private set; }

        public event System.Action<CheckpointZone> PlayerReached;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Reached) return;
            if (other.GetComponent<CarPlayerController>() != null)
            {
                Reached = true;
                PlayerReached?.Invoke(this);
            }
        }
    }
}
