using UnityEngine;
using TrafficTown2D.Traffic;

namespace TrafficTown2D.Visuals
{
    public sealed class PedestrianSignalVisual : MonoBehaviour
    {
        [SerializeField] private PedestrianSignalController signal;
        [SerializeField] private TextMesh walkLabel;
        [SerializeField] private TextMesh dontWalkLabel;

        private void Awake()
        {
            if (signal == null) signal = GetComponent<PedestrianSignalController>();
        }

        private void LateUpdate()
        {
            if (signal == null)
            {
                return;
            }

            bool walk = signal.CurrentState == PedestrianSignalState.Walk;
            if (walkLabel != null) walkLabel.color = walk ? new Color(0.60f, 1f, 0.68f, 1f) : new Color(0.08f, 0.22f, 0.10f, 1f);
            if (dontWalkLabel != null) dontWalkLabel.color = walk ? new Color(0.26f, 0.07f, 0.05f, 1f) : new Color(1f, 0.48f, 0.40f, 1f);
        }
    }
}
