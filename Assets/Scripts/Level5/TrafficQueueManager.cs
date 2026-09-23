using System;
using System.Collections.Generic;
using UnityEngine;
using TrafficTown2D.Level3;

namespace TrafficTown2D.Level5
{
    public enum CongestionLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class TrafficQueueManager : MonoBehaviour
    {
        public static TrafficQueueManager Instance { get; private set; }

        [Header("Spawn Configuration")]
        [SerializeField] private Transform vehicleContainer;
        [SerializeField] private LayerMask obstacleLayers;

        [Header("Stop Line Coordinates")]
        [SerializeField] private float northStopY = 7.6f;
        [SerializeField] private float southStopY = -7.6f;
        [SerializeField] private float eastStopX = 7.6f;
        [SerializeField] private float westStopX = -7.6f;

        [Header("Vehicle Sprites")]
        [SerializeField] private Sprite carBlueSprite;
        [SerializeField] private Sprite carRedSprite;
        [SerializeField] private Sprite taxiSprite;
        [SerializeField] private Sprite busSprite;
        [SerializeField] private Sprite ambulanceSprite;

        private readonly List<TrafficIntersectionVehicle> activeVehicles = new List<TrafficIntersectionVehicle>();

        private float spawnTimer = 0f;
        private float currentSpawnInterval = 3.2f;
        private float northRate = 1f;
        private float southRate = 1f;
        private float eastRate = 1f;
        private float westRate = 1f;

        private int northWaiting = 0;
        private int southWaiting = 0;
        private int eastWaiting = 0;
        private int westWaiting = 0;
        private int totalCleared = 0;

        private float congestionRatio = 0f;
        private CongestionLevel congestionLevel = CongestionLevel.Low;
        private float warningCooldown = 0f;

        public int NorthWaiting => northWaiting;
        public int SouthWaiting => southWaiting;
        public int EastWaiting => eastWaiting;
        public int WestWaiting => westWaiting;
        public int TotalWaiting => northWaiting + southWaiting + eastWaiting + westWaiting;
        public int TotalCleared => totalCleared;
        public float CongestionRatio => congestionRatio;
        public CongestionLevel CurrentCongestionLevel => congestionLevel;

        public event Action<int> VehicleCleared;
        public event Action<string> QueueWarningTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ConfigureMission(Level5MissionConfig config)
        {
            currentSpawnInterval = config.spawnInterval;
            northRate = config.northSpawnRate;
            southRate = config.southSpawnRate;
            eastRate = config.eastSpawnRate;
            westRate = config.westSpawnRate;
            spawnTimer = 0f;

            PrepopulateInitialVehicles();
        }

        public void PrepopulateInitialVehicles()
        {
            if (activeVehicles.Count > 0) return;

            // Spawn 3 initial vehicles approaching the intersection with diverse routes at clean lane centers
            SpawnVehicle(ApproachDirection.North, VehicleCategory.Sedan, false, TurnType.Straight, new Vector3(-2.4f, 14.0f, 0f));
            SpawnVehicle(ApproachDirection.South, VehicleCategory.Taxi, false, TurnType.Straight, new Vector3(2.4f, -14.0f, 0f));
            SpawnVehicle(ApproachDirection.East, VehicleCategory.Sedan, false, TurnType.Straight, new Vector3(14.0f, 2.4f, 0f));
        }

        private void Update()
        {
            spawnTimer += Time.deltaTime;
            warningCooldown -= Time.deltaTime;

            if (spawnTimer >= currentSpawnInterval)
            {
                spawnTimer = 0f;
                SpawnNextVehicle();
            }

            CalculateQueuesAndCongestion();
        }

        private void SpawnNextVehicle()
        {
            // Weighted direction selection
            float totalWeight = northRate + southRate + eastRate + westRate;
            float r = UnityEngine.Random.Range(0f, totalWeight);

            ApproachDirection chosenDir = ApproachDirection.North;
            if (r < northRate)
            {
                chosenDir = ApproachDirection.North;
            }
            else if (r < northRate + southRate)
            {
                chosenDir = ApproachDirection.South;
            }
            else if (r < northRate + southRate + eastRate)
            {
                chosenDir = ApproachDirection.East;
            }
            else
            {
                chosenDir = ApproachDirection.West;
            }

            SpawnVehicle(chosenDir);
        }

        public TrafficIntersectionVehicle SpawnVehicle(
            ApproachDirection dir, 
            VehicleCategory forcedCategory = VehicleCategory.Sedan, 
            bool isEmergency = false, 
            TurnType? forcedTurn = null, 
            Vector3? customSpawnPos = null)
        {
            // Determine turn type
            TurnType chosenTurn = TurnType.Straight;
            if (forcedTurn.HasValue)
            {
                chosenTurn = forcedTurn.Value;
            }
            else if (isEmergency)
            {
                chosenTurn = TurnType.Straight; // Emergencies prefer straight route
            }
            else
            {
                float turnRand = UnityEngine.Random.value;
                if (turnRand < 0.50f) chosenTurn = TurnType.Straight;
                else if (turnRand < 0.75f) chosenTurn = TurnType.RightTurn;
                else chosenTurn = TurnType.LeftTurn;
            }

            TrafficRoute route = TrafficRouteManager.GetRoute(dir, chosenTurn);
            Vector3 spawnPos = customSpawnPos.HasValue ? customSpawnPos.Value : route.SpawnPosition;

            // Check if spawn point is obstructed
            Collider2D overlap = Physics2D.OverlapCircle(spawnPos, 1.8f, obstacleLayers);
            if (overlap != null)
            {
                return null; // Don't spawn on top of another car
            }

            VehicleCategory category = forcedCategory;
            if (!isEmergency && forcedCategory == VehicleCategory.Sedan)
            {
                float randType = UnityEngine.Random.value;
                if (randType < 0.45f) category = VehicleCategory.Sedan;
                else if (randType < 0.70f) category = VehicleCategory.Taxi;
                else category = VehicleCategory.Bus;
            }
            else if (isEmergency)
            {
                category = VehicleCategory.Ambulance;
            }

            GameObject vehicleObj = new GameObject($"Vehicle_{dir}_{chosenTurn}_{category}_{activeVehicles.Count}");
            vehicleObj.transform.position = spawnPos;
            if (vehicleContainer != null) vehicleObj.transform.SetParent(vehicleContainer);

            // Layer setup
            int vehicleLayer = LayerMask.NameToLayer("Default");
            vehicleObj.layer = vehicleLayer;

            SpriteRenderer sr = vehicleObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 2;

            BoxCollider2D col = vehicleObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            Sprite chosenSprite = carBlueSprite;
            switch (category)
            {
                case VehicleCategory.Sedan:
                    chosenSprite = (UnityEngine.Random.value > 0.5f) ? carBlueSprite : carRedSprite;
                    col.size = new Vector2(1.1f, 2.2f);
                    break;
                case VehicleCategory.Taxi:
                    chosenSprite = (taxiSprite != null) ? taxiSprite : carBlueSprite;
                    col.size = new Vector2(1.1f, 2.2f);
                    break;
                case VehicleCategory.Bus:
                    chosenSprite = (busSprite != null) ? busSprite : carBlueSprite;
                    col.size = new Vector2(1.3f, 3.4f);
                    break;
                case VehicleCategory.Ambulance:
                    chosenSprite = (ambulanceSprite != null) ? ambulanceSprite : carBlueSprite;
                    col.size = new Vector2(1.2f, 2.4f);
                    break;
            }

            sr.sprite = chosenSprite;

            TrafficIntersectionVehicle v = vehicleObj.AddComponent<TrafficIntersectionVehicle>();
            v.InitializeRoute(route, category, obstacleLayers, spawnPos);

            if (category == VehicleCategory.Ambulance)
            {
                // Setup emergency beacons
                GameObject redBeacon = new GameObject("BeaconRed");
                redBeacon.transform.SetParent(vehicleObj.transform);
                redBeacon.transform.localPosition = new Vector3(-0.25f, 0.2f, 0f);
                SpriteRenderer rsr = redBeacon.AddComponent<SpriteRenderer>();
                rsr.sprite = sr.sprite;
                rsr.color = Color.red;
                rsr.sortingOrder = 3;
                redBeacon.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

                GameObject blueBeacon = new GameObject("BeaconBlue");
                blueBeacon.transform.SetParent(vehicleObj.transform);
                blueBeacon.transform.localPosition = new Vector3(0.25f, 0.2f, 0f);
                SpriteRenderer bsr = blueBeacon.AddComponent<SpriteRenderer>();
                bsr.sprite = sr.sprite;
                bsr.color = Color.blue;
                bsr.sortingOrder = 3;
                blueBeacon.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

                v.SetBeacons(rsr, bsr);
            }

            v.VehiclePassedIntersection += OnVehiclePassed;
            v.VehicleDespawned += OnVehicleDespawned;

            activeVehicles.Add(v);
            return v;
        }

        private void OnVehiclePassed(TrafficIntersectionVehicle v)
        {
            totalCleared++;
            VehicleCleared?.Invoke(totalCleared);
        }

        private void OnVehicleDespawned(TrafficIntersectionVehicle v)
        {
            activeVehicles.Remove(v);
        }

        private void CalculateQueuesAndCongestion()
        {
            northWaiting = 0;
            southWaiting = 0;
            eastWaiting = 0;
            westWaiting = 0;

            for (int i = activeVehicles.Count - 1; i >= 0; i--)
            {
                TrafficIntersectionVehicle v = activeVehicles[i];
                if (v == null)
                {
                    activeVehicles.RemoveAt(i);
                    continue;
                }

                if (v.IsWaitingInQueue && !v.HasEnteredIntersection)
                {
                    switch (v.Direction)
                    {
                        case ApproachDirection.North: northWaiting++; break;
                        case ApproachDirection.South: southWaiting++; break;
                        case ApproachDirection.East: eastWaiting++; break;
                        case ApproachDirection.West: westWaiting++; break;
                    }
                }
            }

            int total = TotalWaiting;
            // Scale congestion 0 to 1 based on 16 cars waiting max capacity
            congestionRatio = Mathf.Clamp01(total / 14f);

            if (congestionRatio < 0.25f) congestionLevel = CongestionLevel.Low;
            else if (congestionRatio < 0.55f) congestionLevel = CongestionLevel.Medium;
            else if (congestionRatio < 0.85f) congestionLevel = CongestionLevel.High;
            else congestionLevel = CongestionLevel.Critical;

            // Warning checks
            if (warningCooldown <= 0f)
            {
                if (eastWaiting >= 5)
                {
                    QueueWarningTriggered?.Invoke("East traffic is building up!");
                    warningCooldown = 8f;
                }
                else if (northWaiting >= 5)
                {
                    QueueWarningTriggered?.Invoke("North traffic is building up!");
                    warningCooldown = 8f;
                }
                else if (congestionLevel == CongestionLevel.Critical)
                {
                    QueueWarningTriggered?.Invoke("Heavy congestion! Clear intersections!");
                    warningCooldown = 7f;
                }
            }
        }

        public void ClearAllVehicles()
        {
            for (int i = activeVehicles.Count - 1; i >= 0; i--)
            {
                if (activeVehicles[i] != null)
                {
                    Destroy(activeVehicles[i].gameObject);
                }
            }
            activeVehicles.Clear();
            northWaiting = 0;
            southWaiting = 0;
            eastWaiting = 0;
            westWaiting = 0;
            totalCleared = 0;
        }

        public int ClearAllCrashedVehicles()
        {
            int count = 0;
            for (int i = activeVehicles.Count - 1; i >= 0; i--)
            {
                if (activeVehicles[i] != null && activeVehicles[i].IsCrashed)
                {
                    activeVehicles[i].Despawn();
                    count++;
                }
            }
            return count;
        }

        public bool HasCrashedVehicles()
        {
            for (int i = 0; i < activeVehicles.Count; i++)
            {
                if (activeVehicles[i] != null && activeVehicles[i].IsCrashed) return true;
            }
            return false;
        }

        public void SetSprites(Sprite blue, Sprite red, Sprite taxi, Sprite bus, Sprite ambulance)
        {
            carBlueSprite = blue;
            carRedSprite = red;
            taxiSprite = taxi;
            busSprite = bus;
            ambulanceSprite = ambulance;
        }

        public void SetVehicleContainer(Transform container)
        {
            vehicleContainer = container;
        }

        public void SetObstacleLayers(LayerMask layers)
        {
            obstacleLayers = layers;
        }
    }
}
