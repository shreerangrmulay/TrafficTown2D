using System;
using UnityEngine;

namespace TrafficTown2D.Level6
{
    public abstract class EnvironmentalHazard : MonoBehaviour
    {
        [Header("Hazard Info")]
        [SerializeField] private string hazardName = "Road Hazard";
        [SerializeField] private string warningMessage = "[!] Road condition ahead!";
        [SerializeField] private bool triggerWarningOnApproach = true;
        [SerializeField] private float approachDistance = 14f;

        protected bool hasTriggeredApproachWarning = false;
        protected Level6PlayerCar currentCarInZone = null;

        public string HazardName => hazardName;
        public string WarningMessage => warningMessage;

        public static event Action<string> HazardWarningBroadcasted;
        public static event Action<string> HazardEncountered;

        protected virtual void Update()
        {
            if (triggerWarningOnApproach && !hasTriggeredApproachWarning)
            {
                CheckApproach();
            }
        }

        private void CheckApproach()
        {
            if (Level6MissionManager.Instance != null && Level6MissionManager.Instance.PlayerCar != null)
            {
                float dist = Vector2.Distance(transform.position, Level6MissionManager.Instance.PlayerCar.transform.position);
                if (dist < approachDistance)
                {
                    hasTriggeredApproachWarning = true;
                    HazardWarningBroadcasted?.Invoke(warningMessage);
                }
            }
        }

        public void ResetWarning()
        {
            hasTriggeredApproachWarning = false;
        }

        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            Level6PlayerCar car = collision.GetComponent<Level6PlayerCar>();
            if (car != null)
            {
                currentCarInZone = car;
                OnCarEntered(car);
                HazardEncountered?.Invoke(hazardName);
            }
        }

        protected virtual void OnTriggerExit2D(Collider2D collision)
        {
            Level6PlayerCar car = collision.GetComponent<Level6PlayerCar>();
            if (car != null && car == currentCarInZone)
            {
                OnCarExited(car);
                currentCarInZone = null;
            }
        }

        protected abstract void OnCarEntered(Level6PlayerCar car);
        protected abstract void OnCarExited(Level6PlayerCar car);
    }
}
