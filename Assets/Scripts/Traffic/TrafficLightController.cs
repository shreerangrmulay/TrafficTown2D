using System;
using System.Collections;
using UnityEngine;

namespace TrafficTown2D.Traffic
{
    public enum TrafficLightState
    {
        Red,
        Yellow,
        Green
    }

    public sealed class TrafficLightController : MonoBehaviour
    {
        [SerializeField] private float redDuration = 5f;
        [SerializeField] private float greenDuration = 5f;
        [SerializeField] private float yellowDuration = 2f;
        [SerializeField] private SpriteRenderer redLight;
        [SerializeField] private SpriteRenderer yellowLight;
        [SerializeField] private SpriteRenderer greenLight;

        public TrafficLightState CurrentState { get; private set; }
        public event Action<TrafficLightState> StateChanged;
        private bool hasState;

        private void Start()
        {
            Debug.Log("TrafficLightController STARTED");
            ReportMissingVisual("redLight", redLight);
            ReportMissingVisual("yellowLight", yellowLight);
            ReportMissingVisual("greenLight", greenLight);
            StartCoroutine(CycleLights());
        }

        public void ConfigureDurations(float red, float green, float yellow)
        {
            redDuration = red;
            greenDuration = green;
            yellowDuration = yellow;
        }

        public bool ShouldStopAt(float vehiclePosition, float stoppingPoint, float direction)
        {
            if (CurrentState == TrafficLightState.Green)
            {
                return false;
            }

            return direction < 0f ? vehiclePosition > stoppingPoint : vehiclePosition < stoppingPoint;
        }

        private IEnumerator CycleLights()
        {
            while (true)
            {
                SetState(TrafficLightState.Red);
                yield return new WaitForSeconds(redDuration);
                SetState(TrafficLightState.Green);
                yield return new WaitForSeconds(greenDuration);
                SetState(TrafficLightState.Yellow);
                yield return new WaitForSeconds(yellowDuration);
            }
        }

        private void SetState(TrafficLightState state)
        {
            bool stateChanged = !hasState || CurrentState != state;
            CurrentState = state;
            hasState = true;
            SetLight(redLight, state == TrafficLightState.Red, Color.red);
            SetLight(yellowLight, state == TrafficLightState.Yellow, Color.yellow);
            SetLight(greenLight, state == TrafficLightState.Green, Color.green);
            if (stateChanged)
            {
                Debug.Log("TRAFFIC LIGHT -> " + state.ToString().ToUpperInvariant());
                StateChanged?.Invoke(state);
            }
        }

        private static void SetLight(SpriteRenderer light, bool active, Color lightColor)
        {
            if (light != null)
            {
                light.color = new Color(lightColor.r, lightColor.g, lightColor.b, active ? 1f : 0.15f);
            }
        }

        private static void ReportMissingVisual(string fieldName, SpriteRenderer light)
        {
            if (light == null)
            {
                Debug.LogError("TrafficLightController is missing its " + fieldName + " SpriteRenderer reference.");
            }
        }
    }
}
