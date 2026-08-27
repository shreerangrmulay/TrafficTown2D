using UnityEngine;
using UnityEngine.UI;

namespace TrafficTown2D.Traffic
{
    public enum PedestrianSignalState
    {
        DontWalk,
        Walk
    }

    public sealed class PedestrianSignalController : MonoBehaviour
    {
        [SerializeField] private TrafficLightController trafficLight;
        [SerializeField] private Text stateText;
        [SerializeField] private SpriteRenderer signalRenderer;
        [SerializeField] private SpriteRenderer walkRenderer;
        [SerializeField] private SpriteRenderer dontWalkRenderer;

        public PedestrianSignalState CurrentState { get; private set; }

        private void OnEnable()
        {
            if (trafficLight != null)
            {
                trafficLight.StateChanged += UpdateState;
            }
        }

        private void Start()
        {
            if (trafficLight != null)
            {
                UpdateState(trafficLight.CurrentState);
            }
        }

        private void OnDisable()
        {
            if (trafficLight != null)
            {
                trafficLight.StateChanged -= UpdateState;
            }
        }

        private void UpdateState(TrafficLightState trafficState)
        {
            CurrentState = trafficState == TrafficLightState.Red ? PedestrianSignalState.Walk : PedestrianSignalState.DontWalk;
            if (stateText != null)
            {
                stateText.text = CurrentState == PedestrianSignalState.Walk ? "WALK" : "DON'T WALK";
                stateText.color = CurrentState == PedestrianSignalState.Walk ? new Color(0.2f, 0.8f, 0.35f) : new Color(0.95f, 0.25f, 0.2f);
            }

            if (signalRenderer != null)
            {
                signalRenderer.color = CurrentState == PedestrianSignalState.Walk ? new Color(0.2f, 0.8f, 0.35f) : new Color(0.95f, 0.25f, 0.2f);
            }

            if (walkRenderer != null)
            {
                walkRenderer.color = CurrentState == PedestrianSignalState.Walk ? new Color(0.2f, 0.8f, 0.35f) : new Color(0.04f, 0.16f, 0.07f, 1f);
            }

            if (dontWalkRenderer != null)
            {
                dontWalkRenderer.color = CurrentState == PedestrianSignalState.DontWalk ? new Color(0.95f, 0.25f, 0.2f) : new Color(0.19f, 0.05f, 0.04f, 1f);
            }
        }
    }
}
