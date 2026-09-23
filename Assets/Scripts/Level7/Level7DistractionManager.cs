using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficTown2D.Level7
{
    public class Level7DistractionManager : MonoBehaviour
    {
        public static Level7DistractionManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Level7DistractionUI distractionUI;
        [SerializeField] private Level7PlayerCar playerCar;

        [Header("Spawn Settings")]
        [SerializeField] private bool autoSpawningEnabled = true;
        [SerializeField] private float minInterval = 14f;
        [SerializeField] private float maxInterval = 22f;

        private float spawnTimer = 0f;
        private float nextSpawnDelay = 15f;
        private List<DistractionType> allowedTypes = new List<DistractionType>();
        private readonly HashSet<string> triggeredSmartLocations = new HashSet<string>();

        public bool IsDistractionActive => distractionUI != null && distractionUI.IsVisible;

        public event Action<DistractionEventData> DistractionSpawned;
        public event Action<DistractionEventData, bool> DistractionResolved; // (data, wasSafe)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (playerCar == null)
            {
                playerCar = FindAnyObjectByType<Level7PlayerCar>();
            }

            if (distractionUI != null)
            {
                distractionUI.ActionClicked += HandleActionAttempt;
                distractionUI.IgnoreClicked += HandleIgnore;
                distractionUI.TimeoutExpired += HandleTimeout;
            }

            ResetTimer();
        }

        private void OnDestroy()
        {
            if (distractionUI != null)
            {
                distractionUI.ActionClicked -= HandleActionAttempt;
                distractionUI.IgnoreClicked -= HandleIgnore;
                distractionUI.TimeoutExpired -= HandleTimeout;
            }
        }

        public void ConfigureMission(List<DistractionType> types, float intervalMin, float intervalMax, bool autoSpawn = true)
        {
            allowedTypes = types ?? new List<DistractionType>();
            minInterval = Mathf.Max(5f, intervalMin);
            maxInterval = Mathf.Max(minInterval, intervalMax);
            autoSpawningEnabled = autoSpawn;
            triggeredSmartLocations.Clear();

            if (distractionUI != null && distractionUI.IsVisible)
            {
                distractionUI.HideDistraction();
            }

            ResetTimer();
        }

        private void Update()
        {
            if (!autoSpawningEnabled || allowedTypes == null || allowedTypes.Count == 0) return;
            if (distractionUI != null && distractionUI.IsVisible) return;

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= nextSpawnDelay)
            {
                TriggerRandomDistraction();
                ResetTimer();
            }
        }

        public void TriggerDistraction(DistractionType type)
        {
            if (distractionUI == null) return;
            if (distractionUI.IsVisible) return;

            DistractionEventData data = DistractionEventData.CreateDefault(type);
            distractionUI.ShowDistraction(data);
            DistractionSpawned?.Invoke(data);
        }

        public void TriggerSmartDistraction(string triggerKey, DistractionType type)
        {
            if (triggeredSmartLocations.Contains(triggerKey)) return;
            triggeredSmartLocations.Add(triggerKey);

            TriggerDistraction(type);
            ResetTimer();
        }

        private void TriggerRandomDistraction()
        {
            if (allowedTypes == null || allowedTypes.Count == 0) return;
            int randomIndex = UnityEngine.Random.Range(0, allowedTypes.Count);
            TriggerDistraction(allowedTypes[randomIndex]);
        }

        private void ResetTimer()
        {
            spawnTimer = 0f;
            nextSpawnDelay = UnityEngine.Random.Range(minInterval, maxInterval);
        }

        private void HandleActionAttempt(DistractionEventData data)
        {
            bool carStopped = playerCar != null && playerCar.IsStopped;
            bool insideSafeStop = Level7SafeStopZone.IsPlayerInAnySafeStop();

            if (insideSafeStop && carStopped)
            {
                // Safe stop interaction!
                if (Level7FocusManager.Instance != null)
                {
                    Level7FocusManager.Instance.RecordSafeStopInteraction(data);
                }
                DistractionResolved?.Invoke(data, true);
            }
            else
            {
                // Unsafe interaction while moving or outside safe zone!
                if (Level7FocusManager.Instance != null)
                {
                    Level7FocusManager.Instance.RecordUnsafeInteraction(data);
                }
                DistractionResolved?.Invoke(data, false);
            }
        }

        private void HandleIgnore(DistractionEventData data)
        {
            if (Level7FocusManager.Instance != null)
            {
                Level7FocusManager.Instance.RecordDistractionIgnored(data);
            }
            DistractionResolved?.Invoke(data, true);
        }

        private void HandleTimeout(DistractionEventData data)
        {
            // Auto dismissed while player kept driving
            if (Level7FocusManager.Instance != null)
            {
                Level7FocusManager.Instance.RecordDistractionIgnored(data);
            }
            DistractionResolved?.Invoke(data, true);
        }
    }
}
