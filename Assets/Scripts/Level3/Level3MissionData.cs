using System;
using System.IO;
using UnityEngine;

namespace TrafficTown2D.Level3
{
    [Serializable]
    public class MissionConfig
    {
        public int index;
        public string name;
        public string subtitle;
        public string objective;
        public float speedLimit = 40f;
        public int reward = 30;
        public int speedingPenalty = 20;
        public int mistakePenalty = 25;
    }

    [Serializable]
    public class Level3MissionsContainer
    {
        public MissionConfig[] missions;
    }

    public static class Level3MissionDatabase
    {
        public static MissionConfig[] LoadMissions()
        {
            string fullPath = Path.Combine(Application.dataPath, "JSON/Level3Missions.json");
            if (File.Exists(fullPath))
            {
                try
                {
                    string json = File.ReadAllText(fullPath);
                    Level3MissionsContainer container = JsonUtility.FromJson<Level3MissionsContainer>(json);
                    if (container != null && container.missions != null && container.missions.Length > 0)
                    {
                        return container.missions;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Level3MissionDatabase] Failed parsing JSON: {ex.Message}");
                }
            }

            return GetFallbackMissions();
        }

        private static MissionConfig[] GetFallbackMissions()
        {
            return new[]
            {
                new MissionConfig { index = 1, name = "SCHOOL ZONE", subtitle = "CONTROL YOUR SPEED", objective = "Slow down to 20 km/h and stop for crossing pedestrians.", speedLimit = 20f, reward = 30, speedingPenalty = 20, mistakePenalty = 30 },
                new MissionConfig { index = 2, name = "INTERSECTION", subtitle = "OBEY THE SIGNAL", objective = "Stop at the red light and cross only on green.", speedLimit = 40f, reward = 40, speedingPenalty = 20, mistakePenalty = 25 },
                new MissionConfig { index = 3, name = "EMERGENCY VEHICLE", subtitle = "GIVE WAY", objective = "Pull over to the right and stop to allow the ambulance to pass.", speedLimit = 40f, reward = 40, speedingPenalty = 20, mistakePenalty = 30 },
                new MissionConfig { index = 4, name = "DISTRACTION", subtitle = "STAY FOCUSED", objective = "Ignore phone alerts and keep your full attention on the road.", speedLimit = 40f, reward = 30, speedingPenalty = 20, mistakePenalty = 25 },
                new MissionConfig { index = 5, name = "FINAL CITY DRIVE", subtitle = "APPLY EVERYTHING", objective = "Maintain safe speed, watch for hazards, and drive to the finish line!", speedLimit = 30f, reward = 50, speedingPenalty = 20, mistakePenalty = 20 }
            };
        }
    }
}
