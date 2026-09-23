using System;
using UnityEngine;

namespace TrafficTown2D.Level6
{
    [Serializable]
    public class Level6MissionConfig
    {
        public int index;
        public string title;
        public string subtitle;
        public string objective;
        public WeatherType weather;
        public Vector2 playerSpawnPosition;
        public float playerSpawnHeading; // degrees (0 = up/North, 90 = right/East, 180 = down/South, 270 = left/West)
        public Vector2 destinationPosition;
        public string destinationLabel;
        public float timeLimitSeconds;
        public float speedLimitKmh;
        public int reward;
    }

    public static class Level6MissionDatabase
    {
        public static Level6MissionConfig[] GetMissions()
        {
            return new Level6MissionConfig[]
            {
                new Level6MissionConfig
                {
                    index = 1,
                    title = "MISSION 1: RAIN SLICK ROADS",
                    subtitle = "REDUCED TIRE GRIP & CORNERING",
                    objective = "Heavy downpour has reduced road grip. Control speed through the 90° turn and curved road to reach the Depot.",
                    weather = WeatherType.HeavyRain,
                    playerSpawnPosition = new Vector2(-45f, -35f),
                    playerSpawnHeading = 0f, // Facing North
                    destinationPosition = new Vector2(25f, -15f),
                    destinationLabel = "East Service Depot",
                    timeLimitSeconds = 90f,
                    speedLimitKmh = 40f,
                    reward = 50
                },
                new Level6MissionConfig
                {
                    index = 2,
                    title = "MISSION 2: FLOODED DECISION FORK",
                    subtitle = "ROUTE SELECTION & WATER HAZARDS",
                    objective = "Flooding has submerged the main route. Approach the Fork and take the elevated Right Detour to avoid hydroplaning.",
                    weather = WeatherType.Rain,
                    playerSpawnPosition = new Vector2(25f, -15f),
                    playerSpawnHeading = 0f, // Facing North towards Fork
                    destinationPosition = new Vector2(-42f, 25f),
                    destinationLabel = "North-West Logistics Hub",
                    timeLimitSeconds = 95f,
                    speedLimitKmh = 40f,
                    reward = 60
                },
                new Level6MissionConfig
                {
                    index = 3,
                    title = "MISSION 3: ROUNDABOUT IN FOG & WIND",
                    subtitle = "VISIBILITY & LATERAL GUST CONTROL",
                    objective = "Dense fog impairs vision and crosswinds buffet the car. Circulate the Roundabout safely and take the 3rd exit North.",
                    weather = WeatherType.Fog,
                    playerSpawnPosition = new Vector2(-42f, 25f),
                    playerSpawnHeading = 90f, // Facing East towards Roundabout
                    destinationPosition = new Vector2(0f, 45f),
                    destinationLabel = "Northern Weather Station",
                    timeLimitSeconds = 100f,
                    speedLimitKmh = 35f,
                    reward = 70
                },
                new Level6MissionConfig
                {
                    index = 4,
                    title = "MISSION 4: STORM DAMAGE & U-TURN",
                    subtitle = "ROAD BLOCKAGE & HEADING REVERSAL",
                    objective = "A fallen tree has blocked the expressway ahead! Enter the dedicated U-Turn Loop, reverse direction, and reroute via the T-Junction.",
                    weather = WeatherType.Storm,
                    playerSpawnPosition = new Vector2(0f, 45f),
                    playerSpawnHeading = 90f, // Facing East towards blockage
                    destinationPosition = new Vector2(-45f, -15f),
                    destinationLabel = "Valley Relief Shelter",
                    timeLimitSeconds = 110f,
                    speedLimitKmh = 35f,
                    reward = 80
                },
                new Level6MissionConfig
                {
                    index = 5,
                    title = "MISSION 5: THE ULTIMATE EXTREME JOURNEY",
                    subtitle = "FULL NETWORK RESCUE RUN",
                    objective = "Dynamic storm conditions across the entire network. Navigate turns, forks, roundabout, and detours to deliver emergency aid!",
                    weather = WeatherType.Storm,
                    playerSpawnPosition = new Vector2(-45f, -35f),
                    playerSpawnHeading = 0f, // Facing North
                    destinationPosition = new Vector2(45f, 40f),
                    destinationLabel = "Regional Emergency Command Center",
                    timeLimitSeconds = 140f,
                    speedLimitKmh = 45f,
                    reward = 100
                }
            };
        }
    }
}
