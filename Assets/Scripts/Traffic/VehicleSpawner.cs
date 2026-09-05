using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficTown2D.Traffic
{
    public sealed class VehicleSpawner : MonoBehaviour
    {
        [SerializeField] private VehicleController vehiclePrefab;
        [SerializeField] private VehicleController[] vehiclePrefabs;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform carStopPoint;
        [SerializeField] private Transform carExitPoint;
        [SerializeField] private TrafficLightController trafficLight;
        [SerializeField, Min(0.25f)] private float spawnInterval = 3f;
        [SerializeField, Min(1)] private int maximumActiveVehicles = 4;
        [SerializeField, Min(0f)] private float vehicleSpeed = 2.5f;
        [SerializeField, Min(0f)] private float minVehicleSpeed = 2.5f;
        [SerializeField, Min(0f)] private float maxVehicleSpeed = 3.5f;
        [SerializeField] private float stoppingPoint = 2.5f;
        [SerializeField, Min(0f)] private float minimumSpawnDistance = 1.5f;
        [SerializeField] private float travelDirection = -1f;

        private readonly List<VehicleController> activeVehicles = new List<VehicleController>();

        private void Start()
        {
            ReportMissingReferences();
            activeVehicles.AddRange(GetComponentsInChildren<VehicleController>(true));
            StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                RemoveInactiveVehicles();

                if (CanSpawnVehicle())
                {
                    SpawnVehicle();
                }

                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private bool CanSpawnVehicle()
        {
            VehicleController selectedPrefab = GetSelectedPrefab();
            if (selectedPrefab == null || spawnPoint == null || trafficLight == null || activeVehicles.Count >= maximumActiveVehicles)
            {
                return false;
            }

            for (int index = 0; index < activeVehicles.Count; index++)
            {
                if (activeVehicles[index] != null && Mathf.Abs(activeVehicles[index].transform.position.x - spawnPoint.position.x) < minimumSpawnDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private void SpawnVehicle()
        {
            VehicleController selectedPrefab = GetSelectedPrefab();
            if (selectedPrefab == null) return;

            VehicleController vehicle = Instantiate(selectedPrefab, spawnPoint.position, Quaternion.identity, transform);

            if (travelDirection > 0f)
            {
                Vector3 currentScale = vehicle.transform.localScale;
                vehicle.transform.localScale = new Vector3(-Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
            }

            float chosenSpeed = Random.Range(minVehicleSpeed, maxVehicleSpeed);
            if (Mathf.Approximately(minVehicleSpeed, maxVehicleSpeed)) chosenSpeed = vehicleSpeed;

            float defaultExit = travelDirection < 0f ? -9f : 9f;
            float configuredStoppingPoint = carStopPoint != null ? carStopPoint.position.x : stoppingPoint;
            float configuredExitPoint = carExitPoint != null ? carExitPoint.position.x : defaultExit;

            vehicle.Configure(trafficLight, chosenSpeed, configuredStoppingPoint, configuredExitPoint, travelDirection);
            activeVehicles.Add(vehicle);
        }

        private VehicleController GetSelectedPrefab()
        {
            if (vehiclePrefabs != null && vehiclePrefabs.Length > 0)
            {
                int index = Random.Range(0, vehiclePrefabs.Length);
                if (vehiclePrefabs[index] != null) return vehiclePrefabs[index];
            }
            return vehiclePrefab;
        }

        private void RemoveInactiveVehicles()
        {
            for (int index = activeVehicles.Count - 1; index >= 0; index--)
            {
                if (activeVehicles[index] == null)
                {
                    activeVehicles.RemoveAt(index);
                }
            }
        }

        private void ReportMissingReferences()
        {
            if (GetSelectedPrefab() == null) Debug.LogError("VehicleSpawner: vehiclePrefab is missing.");
            if (spawnPoint == null) Debug.LogError("VehicleSpawner: spawnPoint is missing.");
            if (trafficLight == null) Debug.LogError("VehicleSpawner: trafficLight is missing.");
        }
    }
}
