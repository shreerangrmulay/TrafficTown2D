using System;
using UnityEngine;
using TMPro;

namespace TrafficTown2D.Level7
{
    public enum SignalColorState
    {
        Red,
        Yellow,
        Green
    }

    public class Level7TrafficLight : MonoBehaviour
    {
        [Header("Renderers")]
        [SerializeField] private SpriteRenderer redLens;
        [SerializeField] private SpriteRenderer yellowLens;
        [SerializeField] private SpriteRenderer greenLens;
        [SerializeField] private TMP_Text stateLabel;

        [Header("Colors")]
        [SerializeField] private Color redActive = new Color(1f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color redDim = new Color(0.3f, 0.05f, 0.05f, 0.5f);
        [SerializeField] private Color yellowActive = new Color(1f, 0.85f, 0.15f, 1f);
        [SerializeField] private Color yellowDim = new Color(0.3f, 0.25f, 0.05f, 0.5f);
        [SerializeField] private Color greenActive = new Color(0.15f, 0.95f, 0.35f, 1f);
        [SerializeField] private Color greenDim = new Color(0.05f, 0.3f, 0.1f, 0.5f);

        [Header("Cycle Timings")]
        [SerializeField] private float greenDuration = 7.0f;
        [SerializeField] private float yellowDuration = 2.2f;
        [SerializeField] private float redDuration = 5.5f;

        [Header("Stop Line Detection")]
        [SerializeField] private Transform stopLineTransform;
        [SerializeField] private float detectionRadius = 3.5f;

        private SignalColorState currentState = SignalColorState.Green;
        private float stateTimer = 0f;
        private bool isAutonomous = true;
        private bool violationReportedThisCycle = false;
        private Level7PlayerCar cachedPlayerCar;

        public SignalColorState CurrentState => currentState;
        public event Action<SignalColorState> StateChanged;

        private void Start()
        {
            cachedPlayerCar = FindAnyObjectByType<Level7PlayerCar>();
            ApplyVisuals();
        }

        public void SetState(SignalColorState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            stateTimer = 0f;
            violationReportedThisCycle = false;
            ApplyVisuals();
            StateChanged?.Invoke(currentState);
        }

        public void SetAutonomous(bool autonomous)
        {
            isAutonomous = autonomous;
        }

        private void Update()
        {
            if (isAutonomous)
            {
                stateTimer += Time.deltaTime;
                switch (currentState)
                {
                    case SignalColorState.Green:
                        if (stateTimer >= greenDuration) SetState(SignalColorState.Yellow);
                        break;
                    case SignalColorState.Yellow:
                        if (stateTimer >= yellowDuration) SetState(SignalColorState.Red);
                        break;
                    case SignalColorState.Red:
                        if (stateTimer >= redDuration) SetState(SignalColorState.Green);
                        break;
                }
            }

            CheckStopLineViolation();
        }

        private void CheckStopLineViolation()
        {
            if (currentState != SignalColorState.Red || violationReportedThisCycle) return;
            if (stopLineTransform == null || cachedPlayerCar == null) return;

            float dist = Vector2.Distance(cachedPlayerCar.transform.position, stopLineTransform.position);
            if (dist <= detectionRadius && !cachedPlayerCar.IsStopped)
            {
                violationReportedThisCycle = true;
                if (Level7FocusManager.Instance != null)
                {
                    Level7FocusManager.Instance.RecordViolation("RED LIGHT VIOLATION: You crossed during a red light!", 35, 15f);
                }
            }
        }

        private void ApplyVisuals()
        {
            if (redLens != null) redLens.color = currentState == SignalColorState.Red ? redActive : redDim;
            if (yellowLens != null) yellowLens.color = currentState == SignalColorState.Yellow ? yellowActive : yellowDim;
            if (greenLens != null) greenLens.color = currentState == SignalColorState.Green ? greenActive : greenDim;

            if (stateLabel != null)
            {
                stateLabel.text = currentState.ToString().ToUpper();
                stateLabel.color = currentState == SignalColorState.Red ? redActive : (currentState == SignalColorState.Yellow ? yellowActive : greenActive);
            }
        }
    }
}
