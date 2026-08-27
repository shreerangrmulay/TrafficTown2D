using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficTown2D.Traffic
{
    public sealed class VehicleSpawner : MonoBehaviour
    {
        [SerializeField] private VehicleController vehiclePrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform carStopPoint;
        [SerializeField] private Transform carExitPoint;
        [SerializeField] private TrafficLightController trafficLight;
        [SerializeField, Min(0.25f)] private float spawnInterval = 3f;
        [SerializeField, Min(1)] private int maximumActiveVehicles = 4;
        [SerializeField, Min(0f)] private float vehicleSpeed = 2.5f;
        [SerializeField] private float stoppingPoint = 2.5f;
        [SerializeField, Min(0f)] private float minimumSpawnDistance = 1.5f;

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
            if (vehiclePrefab == null || spawnPoint == null || trafficLight == null || activeVehicles.Count >= maximumActiveVehicles)
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
            VehicleController vehicle = Instantiate(vehiclePrefab, spawnPoint.position, Quaternion.identity, transform);
            float configuredStoppingPoint = carStopPoint != null ? carStopPoint.position.x : stoppingPoint;
            float configuredExitPoint = carExitPoint != null ? carExitPoint.position.x : -9f;
            vehicle.Configure(trafficLight, vehicleSpeed, configuredStoppingPoint, configuredExitPoint);
            activeVehicles.Add(vehicle);
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
            if (vehiclePrefab == null) Debug.LogError("VehicleSpawner: vehiclePrefab is missing.");
            if (spawnPoint == null) Debug.LogError("VehicleSpawner: spawnPoint is missing.");
            if (trafficLight == null) Debug.LogError("VehicleSpawner: trafficLight is missing.");
        }
    }
}
