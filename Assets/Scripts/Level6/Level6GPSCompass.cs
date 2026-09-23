using System;
using UnityEngine;
using TMPro;

namespace TrafficTown2D.Level6
{
    /// <summary>
    /// Live GPS Navigational Compass and Waypoint Guide for Level 6.
    /// Calculates live heading angle, distance, and directional arrows to the active mission objective,
    /// displaying dynamic HUD wayfinding guidance so the player always knows where to drive.
    /// </summary>
    public class Level6GPSCompass : MonoBehaviour
    {
        public static Level6GPSCompass Instance { get; private set; }

        [Header("Target References")]
        [SerializeField] private Level6PlayerCar playerCar;
        [SerializeField] private Level6Destination destination;

        [Header("UI Element")]
        [SerializeField] private TMP_Text navText;
        [SerializeField] private RectTransform compassArrowRect;

        private float currentDistance = 0f;
        private float relativeAngle = 0f;

        public float DistanceToGoal => currentDistance;
        public float DistanceToTarget => currentDistance;
        public float RelativeAngle => relativeAngle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public string GetFormattedGuidance()
        {
            if (destination != null && destination.IsReached)
            {
                return "<color=#2ECC71>[GOAL REACHED!]</color>";
            }

            string arrowSymbol = GetDirectionArrow(relativeAngle);
            string destName = destination != null ? destination.DestinationName.ToUpper() : "TARGET";
            int distMeters = Mathf.RoundToInt(currentDistance);
            return $"GPS: {arrowSymbol} {destName} ({distMeters}m)";
        }

        public void BindReferences(Level6PlayerCar car, Level6Destination dest, TMP_Text text, RectTransform arrow = null)
        {
            playerCar = car;
            destination = dest;
            navText = text;
            compassArrowRect = arrow;
        }

        private void Update()
        {
            if (playerCar == null)
            {
                playerCar = FindFirstObjectByType<Level6PlayerCar>();
                if (playerCar == null) return;
            }

            if (destination == null)
            {
                destination = Level6Destination.Instance ?? FindFirstObjectByType<Level6Destination>();
                if (destination == null) return;
            }

            Vector2 carPos = playerCar.transform.position;
            Vector2 destPos = destination.transform.position;
            Vector2 toDest = destPos - carPos;

            currentDistance = toDest.magnitude;

            // Direction arrow symbol based on relative angle to car heading
            float carHeading = playerCar.transform.eulerAngles.z;
            float worldAngle = Mathf.Atan2(toDest.y, toDest.x) * Mathf.Rad2Deg - 90f;
            relativeAngle = Mathf.DeltaAngle(carHeading, worldAngle);

            string arrowSymbol = GetDirectionArrow(relativeAngle);

            if (compassArrowRect != null)
            {
                compassArrowRect.localRotation = Quaternion.Euler(0f, 0f, -relativeAngle);
            }

            if (navText != null)
            {
                if (destination.IsReached)
                {
                    navText.text = "<color=#2ECC71>[GOAL REACHED!]</color>";
                }
                else
                {
                    string destName = destination.DestinationName.ToUpper();
                    int distMeters = Mathf.RoundToInt(currentDistance);
                    navText.text = $"<color=#00E5FF>GPS: {arrowSymbol} {destName} ({distMeters}m)</color>";
                }
            }
        }

        private string GetDirectionArrow(float relAngle)
        {
            // relAngle: 0 = straight ahead, +90 = left, -90 = right, 180 = behind
            if (relAngle > -22.5f && relAngle <= 22.5f) return "↑";
            if (relAngle > 22.5f && relAngle <= 67.5f) return "↖";
            if (relAngle > 67.5f && relAngle <= 112.5f) return "←";
            if (relAngle > 112.5f && relAngle <= 157.5f) return "↙";
            if (relAngle > -67.5f && relAngle <= -22.5f) return "↗";
            if (relAngle > -112.5f && relAngle <= -67.5f) return "→";
            if (relAngle > -157.5f && relAngle <= -112.5f) return "↘";
            return "↓";
        }
    }
}
