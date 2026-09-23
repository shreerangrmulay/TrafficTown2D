using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficTown2D.Level5
{
    public class PedestrianIntersectionManager : MonoBehaviour
    {
        public static PedestrianIntersectionManager Instance { get; private set; }

        [Header("Container & Configuration")]
        [SerializeField] private Transform pedestrianContainer;
        [SerializeField] private float spawnInterval = 5.5f;

        [Header("Pedestrian Sprites")]
        [SerializeField] private Sprite childBoySprite;
        [SerializeField] private Sprite childGirlSprite;
        [SerializeField] private Sprite adultDarkSprite;
        [SerializeField] private Sprite crossingGuardSprite;

        private readonly List<IntersectionPedestrian> activePedestrians = new List<IntersectionPedestrian>();
        private float spawnTimer = 0f;
        private bool isSpawningActive = false;
        private int totalCrossed = 0;
        private float warningCooldown = 0f;

        public int WaitingCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < activePedestrians.Count; i++)
                {
                    if (activePedestrians[i] != null && activePedestrians[i].IsWaiting) count++;
                }
                return count;
            }
        }

        public int TotalCrossed => totalCrossed;

        public event Action<int> PedestrianCrossed;
        public event Action<string> PedestrianWarningTriggered;

        private struct PedestrianJourney
        {
            public Vector3 spawn;
            public Vector3 wait;
            public Vector3 crossTarget;
            public Vector3 exit;

            public PedestrianJourney(Vector3 s, Vector3 w, Vector3 c, Vector3 e)
            {
                spawn = s;
                wait = w;
                crossTarget = c;
                exit = e;
            }
        }

        private PedestrianJourney[] journeys;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 4 edge-to-edge crosswalk journeys
            journeys = new PedestrianJourney[]
            {
                // North Crosswalk across North Road (Y = 6.2)
                new PedestrianJourney(
                    new Vector3(-6.5f, 16f, 0f),
                    new Vector3(-5.4f, 6.2f, 0f),
                    new Vector3(5.4f, 6.2f, 0f),
                    new Vector3(6.5f, 16f, 0f)
                ),
                // South Crosswalk across South Road (Y = -6.2)
                new PedestrianJourney(
                    new Vector3(-6.5f, -16f, 0f),
                    new Vector3(-5.4f, -6.2f, 0f),
                    new Vector3(5.4f, -6.2f, 0f),
                    new Vector3(6.5f, -16f, 0f)
                ),
                // East Crosswalk across East Road (X = 6.2)
                new PedestrianJourney(
                    new Vector3(18f, 6.5f, 0f),
                    new Vector3(6.2f, 5.4f, 0f),
                    new Vector3(6.2f, -5.4f, 0f),
                    new Vector3(18f, -6.5f, 0f)
                ),
                // West Crosswalk across West Road (X = -6.2)
                new PedestrianJourney(
                    new Vector3(-18f, 6.5f, 0f),
                    new Vector3(-6.2f, 5.4f, 0f),
                    new Vector3(-6.2f, -5.4f, 0f),
                    new Vector3(-18f, -6.5f, 0f)
                )
            };
        }

        public void SetSpawningActive(bool active)
        {
            isSpawningActive = active;
            spawnTimer = 0f;

            if (active && activePedestrians.Count == 0)
            {
                SpawnRandomPedestrian();
            }
        }

        private void Update()
        {
            warningCooldown -= Time.deltaTime;

            if (isSpawningActive)
            {
                spawnTimer += Time.deltaTime;
                if (spawnTimer >= spawnInterval)
                {
                    spawnTimer = 0f;
                    if (activePedestrians.Count < 8)
                    {
                        SpawnRandomPedestrian();
                    }
                }
            }

            CheckWaitingTimes();
        }

        public void SpawnRandomPedestrian()
        {
            if (journeys == null || journeys.Length == 0) return;

            int jIdx = UnityEngine.Random.Range(0, journeys.Length);
            PedestrianJourney journey = journeys[jIdx];

            // 50% chance to travel in the reverse direction
            if (UnityEngine.Random.value > 0.5f)
            {
                Vector3 tempSpawn = journey.spawn;
                Vector3 tempWait = journey.wait;
                journey.spawn = journey.exit;
                journey.wait = journey.crossTarget;
                journey.crossTarget = tempWait;
                journey.exit = tempSpawn;
            }

            // Slight offset so pedestrians walking in groups do not overlap
            Vector3 jitter = new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), UnityEngine.Random.Range(-0.2f, 0.2f), 0f);

            GameObject pedObj = new GameObject($"Pedestrian_{activePedestrians.Count}");
            pedObj.transform.position = journey.spawn + jitter;
            if (pedestrianContainer != null) pedObj.transform.SetParent(pedestrianContainer);

            // Visual child for walking bob
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(pedObj.transform, false);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            visual.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            // Select sprite
            Sprite spr = childBoySprite;
            float r = UnityEngine.Random.value;
            if (r < 0.35f) spr = (childGirlSprite != null) ? childGirlSprite : childBoySprite;
            else if (r < 0.70f) spr = (adultDarkSprite != null) ? adultDarkSprite : childBoySprite;
            else if (r < 0.85f) spr = (crossingGuardSprite != null) ? crossingGuardSprite : childBoySprite;

            sr.sprite = spr;

            CircleCollider2D col = pedObj.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;

            IntersectionPedestrian ped = pedObj.AddComponent<IntersectionPedestrian>();
            ped.InitializePath(journey.spawn + jitter, journey.wait + jitter, journey.crossTarget + jitter, journey.exit + jitter, visual.transform);
            ped.CrossedSuccessfully += OnPedestrianCrossed;

            activePedestrians.Add(ped);
        }

        private void OnPedestrianCrossed(IntersectionPedestrian p)
        {
            totalCrossed++;
            PedestrianCrossed?.Invoke(totalCrossed);
        }

        private void CheckWaitingTimes()
        {
            if (warningCooldown > 0f) return;

            for (int i = 0; i < activePedestrians.Count; i++)
            {
                if (activePedestrians[i] != null && activePedestrians[i].IsWaiting && activePedestrians[i].WaitingTime > 25f)
                {
                    PedestrianWarningTriggered?.Invoke("Pedestrians are waiting at the crosswalk!");
                    warningCooldown = 12f;
                    break;
                }
            }
        }

        public void ClearActivePedestrians()
        {
            for (int i = activePedestrians.Count - 1; i >= 0; i--)
            {
                if (activePedestrians[i] != null)
                {
                    Destroy(activePedestrians[i].gameObject);
                }
            }
            activePedestrians.Clear();
        }

        public void ResetAll()
        {
            ClearActivePedestrians();
            totalCrossed = 0;
        }

        public void ClearAllPedestrians()
        {
            ClearActivePedestrians();
        }

        public void SetSprites(Sprite boy, Sprite girl, Sprite adult, Sprite guard)
        {
            childBoySprite = boy;
            childGirlSprite = girl;
            adultDarkSprite = adult;
            crossingGuardSprite = guard;
        }

        public void SetContainer(Transform container)
        {
            pedestrianContainer = container;
        }
    }
}
