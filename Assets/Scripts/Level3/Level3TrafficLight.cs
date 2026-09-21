using System;
using UnityEngine;
using TMPro;

namespace TrafficTown2D.Level3
{
    public enum SignalState
    {
        Red,
        Yellow,
        Green
    }

    public class Level3TrafficLight : MonoBehaviour
    {
        [Header("Signal Renderers")]
        [SerializeField] private SpriteRenderer redLens;
        [SerializeField] private SpriteRenderer yellowLens;
        [SerializeField] private SpriteRenderer greenLens;
        [SerializeField] private TMP_Text signalLabel;

        [Header("State Colors")]
        [SerializeField] private Color redActive = new Color(1f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color redDim = new Color(0.3f, 0.05f, 0.05f, 0.5f);
        [SerializeField] private Color yellowActive = new Color(1f, 0.85f, 0.15f, 1f);
        [SerializeField] private Color yellowDim = new Color(0.3f, 0.25f, 0.05f, 0.5f);
        [SerializeField] private Color greenActive = new Color(0.15f, 0.95f, 0.35f, 1f);
        [SerializeField] private Color greenDim = new Color(0.05f, 0.3f, 0.1f, 0.5f);

        [Header("Timing")]
        [SerializeField] private float redDuration = 4.5f;
        [SerializeField] private float yellowDuration = 2.0f;
        [SerializeField] private float greenDuration = 6.0f;

        private SignalState currentState = SignalState.Green;
        private float stateTimer;
        private bool isAutonomous = true;

        public SignalState CurrentState => currentState;
        public event Action<SignalState> StateChanged;

        private void Start()
        {
            ApplyVisualState();
        }

        private void Update()
        {
            if (!isAutonomous) return;

            stateTimer += Time.deltaTime;
            switch (currentState)
            {
                case SignalState.Green:
                    if (stateTimer >= greenDuration)
                    {
                        SetState(SignalState.Yellow);
                    }
                    break;

                case SignalState.Yellow:
                    if (stateTimer >= yellowDuration)
                    {
                        SetState(SignalState.Red);
                    }
                    break;

                case SignalState.Red:
                    if (stateTimer >= redDuration)
                    {
                        SetState(SignalState.Green);
                    }
                    break;
            }
        }

        public void SetState(SignalState newState)
        {
            currentState = newState;
            stateTimer = 0f;
            ApplyVisualState();
            StateChanged?.Invoke(currentState);
        }

        public void SetAutonomous(bool autonomous)
        {
            isAutonomous = autonomous;
        }

        private void ApplyVisualState()
        {
            if (redLens != null) redLens.color = (currentState == SignalState.Red) ? redActive : redDim;
            if (yellowLens != null) yellowLens.color = (currentState == SignalState.Yellow) ? yellowActive : yellowDim;
            if (greenLens != null) greenLens.color = (currentState == SignalState.Green) ? greenActive : greenDim;

            if (signalLabel != null)
            {
                switch (currentState)
                {
                    case SignalState.Red:
                        signalLabel.text = "STOP";
                        signalLabel.color = redActive;
                        break;
                    case SignalState.Yellow:
                        signalLabel.text = "WAIT";
                        signalLabel.color = yellowActive;
                        break;
                    case SignalState.Green:
                        signalLabel.text = "GO";
                        signalLabel.color = greenActive;
                        break;
                }
            }
        }

        public void SetupLenses(SpriteRenderer red, SpriteRenderer yellow, SpriteRenderer green, TMP_Text label = null)
        {
            redLens = red;
            yellowLens = yellow;
            greenLens = green;
            signalLabel = label;
            ApplyVisualState();
        }
    }
}
