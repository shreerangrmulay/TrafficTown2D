using System;
using System.Collections;
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

        private Coroutine activeDistractionRoutine;
        private DistractionEventData currentEventData;

        public bool IsDistractionActive => activeDistractionRoutine != null || (distractionUI != null && distractionUI.IsVisible);

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

            if (distractionUI == null)
            {
                distractionUI = FindAnyObjectByType<Level7DistractionUI>(FindObjectsInactive.Include);
            }

            if (distractionUI != null)
            {
                distractionUI.ActionClicked += HandleActionAttempt;
                distractionUI.IgnoreClicked += HandleIgnore;
            }

            ResetTimer();
        }

        private void OnDestroy()
        {
            if (activeDistractionRoutine != null)
            {
                StopCoroutine(activeDistractionRoutine);
                activeDistractionRoutine = null;
            }

            if (distractionUI != null)
            {
                distractionUI.ActionClicked -= HandleActionAttempt;
                distractionUI.IgnoreClicked -= HandleIgnore;
            }
        }

        public void ConfigureMission(List<DistractionType> types, float intervalMin, float intervalMax, bool autoSpawn = true)
        {
            allowedTypes = types ?? new List<DistractionType>();
            minInterval = Mathf.Max(5f, intervalMin);
            maxInterval = Mathf.Max(minInterval, intervalMax);
            autoSpawningEnabled = autoSpawn;
            triggeredSmartLocations.Clear();

            DismissDistraction(true);
            ResetTimer();
        }

        private void Update()
        {
            if (!autoSpawningEnabled || allowedTypes == null || allowedTypes.Count == 0) return;
            if (IsDistractionActive) return;

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
            if (IsDistractionActive) return;

            DistractionEventData data = DistractionEventData.CreateDefault(type);
            TriggerDistractionDirect(data);
        }

        public void TriggerDistractionDirect(DistractionEventData data)
        {
            if (distractionUI == null || data == null) return;
            if (IsDistractionActive) return;

            if (activeDistractionRoutine != null)
            {
                StopCoroutine(activeDistractionRoutine);
                activeDistractionRoutine = null;
            }

            activeDistractionRoutine = StartCoroutine(DistractionSequenceRoutine(data));
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

        private IEnumerator DistractionSequenceRoutine(DistractionEventData data)
        {
            currentEventData = data;

            // 1. Activate DistractionCard GameObject FIRST
            distractionUI.gameObject.SetActive(true);

            // 2. Populate text and visuals
            distractionUI.SetupCard(data);

            // 3. Play slide-in animation (hosted safely on DistractionManager)
            yield return StartCoroutine(distractionUI.AnimateSlideRoutine(true));

            // 4. Run countdown timer
            float duration = data.displayDuration;
            float remaining = duration;
            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                distractionUI.UpdateCountdownBar(Mathf.Clamp01(remaining / duration));
                yield return null;
            }

            // 5. Timed out naturally! Player ignored the distraction while driving
            HandleTimeout(data);
            if (distractionUI != null)
            {
                distractionUI.TriggerTimeoutExpired(data);
            }

            // 6. Play slide-out animation (hosted safely on DistractionManager)
            yield return StartCoroutine(distractionUI.AnimateSlideRoutine(false));

            // 7. Deactivate DistractionCard AFTER animation completes
            distractionUI.gameObject.SetActive(false);
            distractionUI.SnapHidden();
            activeDistractionRoutine = null;
            currentEventData = null;
        }

        public void DismissDistraction(bool immediate = false)
        {
            if (activeDistractionRoutine != null)
            {
                StopCoroutine(activeDistractionRoutine);
                activeDistractionRoutine = null;
            }

            if (distractionUI == null) return;

            if (immediate || !distractionUI.gameObject.activeInHierarchy)
            {
                distractionUI.SnapHidden();
                distractionUI.gameObject.SetActive(false);
                currentEventData = null;
            }
            else
            {
                activeDistractionRoutine = StartCoroutine(DismissRoutine());
            }
        }

        private IEnumerator DismissRoutine()
        {
            if (distractionUI != null && distractionUI.gameObject.activeInHierarchy)
            {
                yield return StartCoroutine(distractionUI.AnimateSlideRoutine(false));
                distractionUI.gameObject.SetActive(false);
                distractionUI.SnapHidden();
            }
            activeDistractionRoutine = null;
            currentEventData = null;
        }

        private void HandleActionAttempt(DistractionEventData data)
        {
            if (data == null) data = currentEventData;
            if (data == null) return;

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

            DismissDistraction(false);
        }

        private void HandleIgnore(DistractionEventData data)
        {
            if (data == null) data = currentEventData;
            if (data == null) return;

            if (Level7FocusManager.Instance != null)
            {
                Level7FocusManager.Instance.RecordDistractionIgnored(data);
            }
            DistractionResolved?.Invoke(data, true);

            DismissDistraction(false);
        }

        private void HandleTimeout(DistractionEventData data)
        {
            if (data == null) data = currentEventData;
            if (data == null) return;

            // Auto dismissed while player kept driving
            if (Level7FocusManager.Instance != null)
            {
                Level7FocusManager.Instance.RecordDistractionIgnored(data);
            }
            DistractionResolved?.Invoke(data, true);
        }
    }
}
