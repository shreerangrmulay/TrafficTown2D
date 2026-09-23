using System;
using UnityEngine;
using TrafficTown2D.Level3;

namespace TrafficTown2D.Level5
{
    public class EmergencyVehicleManager : MonoBehaviour
    {
        public static EmergencyVehicleManager Instance { get; private set; }

        [Header("Audio")]
        [SerializeField] private AudioSource sirenAudioSource;

        private TrafficIntersectionVehicle currentAmbulance;
        private ApproachDirection currentDirection;
        private float delayTimer = 0f;
        private bool hasWarnedDelay = false;
        private int emergenciesCleared = 0;

        public bool IsEmergencyActive => currentAmbulance != null;
        public ApproachDirection ActiveDirection => currentDirection;
        public int EmergenciesCleared => emergenciesCleared;

        public event Action<ApproachDirection> EmergencyDispatched;
        public event Action<bool> EmergencyResolved; // true = cleared fast, false = delayed
        public event Action<string> EmergencyAlertTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (currentAmbulance != null)
            {
                delayTimer += Time.deltaTime;

                if (currentAmbulance.IsWaitingInQueue && delayTimer > 20f && !hasWarnedDelay)
                {
                    hasWarnedDelay = true;
                    EmergencyAlertTriggered?.Invoke("Emergency vehicle delayed! Change phase now!");
                }
            }
        }

        public void DispatchAmbulance(ApproachDirection dir)
        {
            if (TrafficQueueManager.Instance == null) return;

            currentDirection = dir;
            delayTimer = 0f;
            hasWarnedDelay = false;

            currentAmbulance = TrafficQueueManager.Instance.SpawnVehicle(dir, VehicleCategory.Ambulance, isEmergency: true);

            if (currentAmbulance != null)
            {
                currentAmbulance.VehiclePassedIntersection += OnAmbulancePassed;
                currentAmbulance.VehicleDespawned += OnAmbulanceDespawned;

                if (sirenAudioSource != null && !sirenAudioSource.isPlaying)
                {
                    sirenAudioSource.Play();
                }

                EmergencyDispatched?.Invoke(dir);
                EmergencyAlertTriggered?.Invoke($"AMBULANCE APPROACHING FROM {dir.ToString().ToUpper()}!");
            }
        }

        private void OnAmbulancePassed(TrafficIntersectionVehicle v)
        {
            if (v == currentAmbulance)
            {
                emergenciesCleared++;
                bool fastPass = delayTimer < 25f;

                if (sirenAudioSource != null && sirenAudioSource.isPlaying)
                {
                    sirenAudioSource.Stop();
                }

                EmergencyResolved?.Invoke(fastPass);
                EmergencyAlertTriggered?.Invoke(fastPass ? "✓ Emergency vehicle cleared safely!" : "! Ambulance cleared after delay.");

                currentAmbulance = null;
            }
        }

        private void OnAmbulanceDespawned(TrafficIntersectionVehicle v)
        {
            if (v == currentAmbulance)
            {
                currentAmbulance = null;
                if (sirenAudioSource != null && sirenAudioSource.isPlaying)
                {
                    sirenAudioSource.Stop();
                }
            }
        }

        public void ResetCurrentEmergency()
        {
            currentAmbulance = null;
            delayTimer = 0f;
            hasWarnedDelay = false;
            if (sirenAudioSource != null && sirenAudioSource.isPlaying) sirenAudioSource.Stop();
        }

        public void ResetManager()
        {
            ResetCurrentEmergency();
            emergenciesCleared = 0;
        }

        public void SetAudioSource(AudioSource source)
        {
            sirenAudioSource = source;
        }
    }
}
