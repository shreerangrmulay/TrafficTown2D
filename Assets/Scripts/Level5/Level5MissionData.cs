using System;
using System.IO;
using UnityEngine;

namespace TrafficTown2D.Level5
{
    [Serializable]
    public class Level5MissionConfig
    {
        public int index;
        public string name;
        public string subtitle;
        public string objective;
        public float duration = 60f;
        public int targetVehiclesCleared = 12;
        public int targetPedestriansCrossed = 0;
        public int targetEmergenciesCleared = 0;
        public float spawnInterval = 3.0f;
        public float northSpawnRate = 1.0f;
        public float southSpawnRate = 1.0f;
        public float eastSpawnRate = 1.0f;
        public float westSpawnRate = 1.0f;
        public bool pedestriansActive = false;
        public bool emergencyActive = false;
        public int reward = 50;
    }

    [Serializable]
    public class Level5MissionsContainer
    {
        public Level5MissionConfig[] missions;
    }

    public static class Level5MissionDatabase
    {
        public static Level5MissionConfig[] LoadMissions()
        {
            string fullPath = Path.Combine(Application.dataPath, "JSON/Level5Missions.json");
            if (File.Exists(fullPath))
            {
                try
                {
                    string json = File.ReadAllText(fullPath);
                    Level5MissionsContainer container = JsonUtility.FromJson<Level5MissionsContainer>(json);
                    if (container != null && container.missions != null && container.missions.Length > 0)
                    {
                        return container.missions;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Level5MissionDatabase] Failed parsing JSON: {ex.Message}");
                }
            }

            return GetFallbackMissions();
        }

        public static Level5MissionConfig[] GetFallbackMissions()
        {
            return new Level5MissionConfig[]
            {
                new Level5MissionConfig
                {
                    index = 1,
                    name = "LEARN THE INTERSECTION",
                    subtitle = "PHASE SWITCHING & CLEARANCE DELAYS",
                    objective = "Operate the traffic signals. Maintain safe transitions (Yellow and All-Red intervals) and keep traffic flowing smoothly.",
                    duration = 30f,
                    targetVehiclesCleared = 6,
                    spawnInterval = 3.2f,
                    pedestriansActive = false,
                    emergencyActive = false,
                    reward = 40
                },
                new Level5MissionConfig
                {
                    index = 2,
                    name = "MANAGE THE TRAFFIC",
                    subtitle = "QUEUE BALANCING & CONGESTION",
                    objective = "Traffic volume is increasing from the East avenue! Monitor queue build-up and prevent congestion from hitting Critical levels.",
                    duration = 30f,
                    targetVehiclesCleared = 10,
                    spawnInterval = 2.2f,
                    eastSpawnRate = 2.2f,
                    westSpawnRate = 1.4f,
                    pedestriansActive = false,
                    emergencyActive = false,
                    reward = 50
                },
                new Level5MissionConfig
                {
                    index = 3,
                    name = "PEDESTRIAN PRIORITY",
                    subtitle = "SAFE CROSSINGS & ALL-RED WALK",
                    objective = "Pedestrian groups are waiting at zebra crossings. Switch to Pedestrian Walk [P] to allow safe crossing without causing gridlock.",
                    duration = 35f,
                    targetVehiclesCleared = 8,
                    targetPedestriansCrossed = 4,
                    spawnInterval = 2.4f,
                    pedestriansActive = true,
                    emergencyActive = false,
                    reward = 60
                },
                new Level5MissionConfig
                {
                    index = 4,
                    name = "EMERGENCY PRIORITY",
                    subtitle = "AMBULANCE ROUTE CLEARANCE",
                    objective = "Emergency vehicles are approaching! Identify their direction, switch signals to grant an open corridor, and resume normal flow.",
                    duration = 40f,
                    targetVehiclesCleared = 10,
                    targetPedestriansCrossed = 2,
                    targetEmergenciesCleared = 2,
                    spawnInterval = 2.2f,
                    pedestriansActive = true,
                    emergencyActive = true,
                    reward = 70
                },
                new Level5MissionConfig
                {
                    index = 5,
                    name = "RUSH HOUR",
                    subtitle = "THE ULTIMATE TRAFFIC CHALLENGE",
                    objective = "Peak rush hour! Manage dense multi-lane traffic, pedestrian crowds, and emergency ambulances. Maintain high safety and flow scores.",
                    duration = 70f,
                    targetVehiclesCleared = 18,
                    targetPedestriansCrossed = 6,
                    targetEmergenciesCleared = 3,
                    spawnInterval = 1.6f,
                    northSpawnRate = 1.6f,
                    southSpawnRate = 1.6f,
                    eastSpawnRate = 2.0f,
                    westSpawnRate = 2.0f,
                    pedestriansActive = true,
                    emergencyActive = true,
                    reward = 100
                }
            };
        }
    }
}
