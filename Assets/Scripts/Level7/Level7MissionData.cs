using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficTown2D.Level7
{
    [System.Serializable]
    public class MissionDefinition
    {
        public int missionNumber;
        public string title;
        public string shortObjective;
        public string detailedInstructions;

        public Vector3 playerSpawnPosition;
        public float playerSpawnRotationZ; // Euler Z in degrees

        public Vector3 destinationPosition;
        public float destinationRadius = 3.2f;
        public string destinationName;

        public List<DistractionType> allowedDistractionTypes;
        public float minSpawnInterval = 14f;
        public float maxSpawnInterval = 22f;

        // Smart triggers
        public bool hasSmartTrigger;
        public string smartTriggerId;
        public Vector3 smartTriggerLocation;
        public float smartTriggerRadius = 6.0f;
        public DistractionType smartDistractionType;

        // Special mission mechanics
        public bool requireSafeStopInteraction = false;
        public bool spawnAmbulance = false;
        public Vector3[] ambulanceRoute;
    }

    public static class Level7MissionData
    {
        public static List<MissionDefinition> GetMissions()
        {
            return new List<MissionDefinition>
            {
                // Mission 1: Learn to Ignore
                new MissionDefinition
                {
                    missionNumber = 1,
                    title = "LEARN TO IGNORE",
                    shortObjective = "Drive to South Avenue Depot. Ignore phone alerts.",
                    detailedInstructions = "Focus on the road! Phone notifications will chime while driving. IGNORE them and navigate straight to the depot safely.",
                    playerSpawnPosition = new Vector3(-25f, -20f, 0f),
                    playerSpawnRotationZ = -90f, // Facing East on South Ave
                    destinationPosition = new Vector3(25f, -20f, 0f), // South Ave East pad
                    destinationRadius = 4.5f,
                    destinationName = "South Avenue Depot",
                    allowedDistractionTypes = new List<DistractionType> { DistractionType.Notification },
                    minSpawnInterval = 10f,
                    maxSpawnInterval = 16f,
                    hasSmartTrigger = true,
                    smartTriggerId = "m1_ignore_trigger",
                    smartTriggerLocation = new Vector3(-5f, -20f, 0f),
                    smartDistractionType = DistractionType.Notification
                },

                // Mission 2: Multiple Distractions
                new MissionDefinition
                {
                    missionNumber = 2,
                    title = "MULTIPLE DISTRACTIONS",
                    shortObjective = "Navigate the curved park way to North Vista.",
                    detailedInstructions = "Phone messages, notifications, and music changes will compete for your attention. Keep driving and do not tap them!",
                    playerSpawnPosition = new Vector3(-35f, -8f, 0f),
                    playerSpawnRotationZ = 0f, // Facing North on West Ave
                    destinationPosition = new Vector3(25f, 20f, 0f), // North Vista Plaza
                    destinationRadius = 4.5f,
                    destinationName = "North Vista Plaza",
                    allowedDistractionTypes = new List<DistractionType> { DistractionType.PhoneMessage, DistractionType.Notification, DistractionType.MusicChange },
                    minSpawnInterval = 9f,
                    maxSpawnInterval = 15f,
                    hasSmartTrigger = true,
                    smartTriggerId = "m2_curve_trigger",
                    smartTriggerLocation = new Vector3(-35f, 0f, 0f),
                    smartDistractionType = DistractionType.MusicChange
                },

                // Mission 3: Distraction + Traffic
                new MissionDefinition
                {
                    missionNumber = 3,
                    title = "DISTRACTION + TRAFFIC",
                    shortObjective = "Stay focused around busy traffic & signals.",
                    detailedInstructions = "Heavy traffic ahead! A navigation alert will appear right before the intersection. Observe the signal and pedestrian crossing!",
                    playerSpawnPosition = new Vector3(0f, -18f, 0f),
                    playerSpawnRotationZ = 0f, // Facing North on Central Street
                    destinationPosition = new Vector3(0f, 18f, 0f), // North Central Gate
                    destinationRadius = 4.5f,
                    destinationName = "North Central Gate",
                    allowedDistractionTypes = new List<DistractionType> { DistractionType.PhoneMessage, DistractionType.NavigationUpdate, DistractionType.MusicChange },
                    minSpawnInterval = 8f,
                    maxSpawnInterval = 14f,
                    hasSmartTrigger = true,
                    smartTriggerId = "m3_signal_trigger",
                    smartTriggerLocation = new Vector3(0f, -14f, 0f),
                    smartDistractionType = DistractionType.NavigationUpdate
                },

                // Mission 4: Safe Stop
                new MissionDefinition
                {
                    missionNumber = 4,
                    title = "SAFE STOP PULL-OVER",
                    shortObjective = "Pull into the Safe Stop Bay to check the alert.",
                    detailedInstructions = "When the navigation update appears, DO NOT tap it while moving. Pull into the green Safe Stop Bay, stop completely, and interact safely!",
                    playerSpawnPosition = new Vector3(-25f, -20f, 0f),
                    playerSpawnRotationZ = -90f, // Facing East on South Ave
                    destinationPosition = new Vector3(32f, -20f, 0f), // South Terminal
                    destinationRadius = 4.5f,
                    destinationName = "South Terminal",
                    allowedDistractionTypes = new List<DistractionType> { DistractionType.NavigationUpdate, DistractionType.PhoneMessage, DistractionType.PassengerInteraction },
                    minSpawnInterval = 7f,
                    maxSpawnInterval = 13f,
                    hasSmartTrigger = true,
                    smartTriggerId = "m4_safestop_trigger",
                    smartTriggerLocation = new Vector3(2f, -20f, 0f),
                    smartDistractionType = DistractionType.NavigationUpdate,
                    requireSafeStopInteraction = true
                },

                // Mission 5: Focus Under Pressure
                new MissionDefinition
                {
                    missionNumber = 5,
                    title = "FOCUS UNDER PRESSURE",
                    shortObjective = "Complete the city circuit & yield to the ambulance!",
                    detailedInstructions = "The ultimate test: All distractions are active. When you hear and see the ambulance, yield safely without taking your attention off the road!",
                    playerSpawnPosition = new Vector3(35f, -18f, 0f),
                    playerSpawnRotationZ = 0f, // Facing North on East Blvd
                    destinationPosition = new Vector3(0f, 0f, 0f), // Central Civic Hub
                    destinationRadius = 4.5f,
                    destinationName = "Central Civic Hub",
                    allowedDistractionTypes = new List<DistractionType> {
                        DistractionType.PhoneMessage,
                        DistractionType.Notification,
                        DistractionType.MusicChange,
                        DistractionType.NavigationUpdate,
                        DistractionType.PassengerInteraction
                    },
                    minSpawnInterval = 6f,
                    maxSpawnInterval = 12f,
                    hasSmartTrigger = true,
                    smartTriggerId = "m5_ambulance_trigger",
                    smartTriggerLocation = new Vector3(35f, 5f, 0f),
                    smartDistractionType = DistractionType.PassengerInteraction,
                    spawnAmbulance = true,
                    ambulanceRoute = new Vector3[]
                    {
                        new Vector3(35f, 25f, 0f),
                        new Vector3(35f, 10f, 0f),
                        new Vector3(35f, -10f, 0f),
                        new Vector3(35f, -20f, 0f),
                        new Vector3(15f, -20f, 0f),
                        new Vector3(-10f, -20f, 0f)
                    }
                }
            };
        }
    }
}
