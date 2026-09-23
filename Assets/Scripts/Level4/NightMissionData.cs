using System;
using System.IO;
using UnityEngine;

namespace TrafficTown2D.Level4
{
    [Serializable]
    public class NightMissionConfig
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
    public class NightMissionsContainer
    {
        public NightMissionConfig[] missions;
    }

    public static class NightMissionDatabase
    {
        public static NightMissionConfig[] LoadMissions()
        {
            string fullPath = Path.Combine(Application.dataPath, "JSON/Level4Missions.json");
            if (File.Exists(fullPath))
            {
                try
                {
                    string json = File.ReadAllText(fullPath);
                    NightMissionsContainer container = JsonUtility.FromJson<NightMissionsContainer>(json);
                    if (container != null && container.missions != null && container.missions.Length > 0)
                    {
                        return container.missions;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NightMissionDatabase] Failed parsing JSON: {ex.Message}");
                }
            }

            return GetFallbackMissions();
        }

        private static NightMissionConfig[] GetFallbackMissions()
        {
            return new[]
            {
                new NightMissionConfig
                {
                    index = 1,
                    name = "LEARN THE NIGHT",
                    subtitle = "HEADLIGHTS & SAFE SPEED",
                    objective = "Turn headlights ON [H], keep speed under 40 km/h, and stay centered in your lane.",
                    speedLimit = 40f,
                    reward = 30,
                    speedingPenalty = 20,
                    mistakePenalty = 25
                },
                new NightMissionConfig
                {
                    index = 2,
                    name = "SEE AND REACT",
                    subtitle = "PEDESTRIANS AT NIGHT",
                    objective = "Pedestrians crossing ahead in dark clothing! Switch to High Beam [F] if needed, slow down, and yield safely.",
                    speedLimit = 30f,
                    reward = 35,
                    speedingPenalty = 20,
                    mistakePenalty = 30
                },
                new NightMissionConfig
                {
                    index = 3,
                    name = "HEADLIGHT DISCIPLINE",
                    subtitle = "DIP YOUR LIGHTS",
                    objective = "Oncoming traffic approaching! Switch High Beam to Low Beam [F] to avoid blinding oncoming drivers.",
                    speedLimit = 40f,
                    reward = 35,
                    speedingPenalty = 20,
                    mistakePenalty = 25
                },
                new NightMissionConfig
                {
                    index = 4,
                    name = "NIGHT CITY DRIVE",
                    subtitle = "URBAN HAZARDS & EMERGENCY",
                    objective = "Navigate illuminated avenue. Obey 20 km/h school zone, stop at red light, and yield to ambulance.",
                    speedLimit = 20f,
                    reward = 40,
                    speedingPenalty = 20,
                    mistakePenalty = 25
                },
                new NightMissionConfig
                {
                    index = 5,
                    name = "RAINY NIGHT",
                    subtitle = "LOW VISIBILITY & WET ROADS",
                    objective = "Heavy rain has reduced visibility to 50%! Braking distances double. Drive with caution to the finish line.",
                    speedLimit = 30f,
                    reward = 50,
                    speedingPenalty = 20,
                    mistakePenalty = 20
                }
            };
        }
    }
}
