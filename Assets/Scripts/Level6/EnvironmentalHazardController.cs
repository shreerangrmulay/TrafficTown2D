using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficTown2D.Level6
{
    public class EnvironmentalHazardController : MonoBehaviour
    {
        public static EnvironmentalHazardController Instance { get; private set; }

        [Header("Hazard Containers")]
        [SerializeField] private GameObject floodZoneObject;
        [SerializeField] private GameObject mudZoneObject;
        [SerializeField] private GameObject fallenTreeObject;
        [SerializeField] private GameObject roadClosureObject;
        [SerializeField] private GameObject debrisContainer;

        public event Action<string> HazardAlertTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnvironmentalHazard.HazardWarningBroadcasted += OnHazardWarning;
        }

        private void OnDestroy()
        {
            EnvironmentalHazard.HazardWarningBroadcasted -= OnHazardWarning;
        }

        private void OnHazardWarning(string message)
        {
            HazardAlertTriggered?.Invoke(message);
        }

        public void ConfigureMissionHazards(int missionIndex)
        {
            // Reset approach warnings on all child hazards
            var hazards = GetComponentsInChildren<EnvironmentalHazard>(true);
            for (int i = 0; i < hazards.Length; i++)
            {
                hazards[i].ResetWarning();
            }

            switch (missionIndex)
            {
                case 1:
                    // Mission 1: Heavy rain only, road network is open
                    if (floodZoneObject != null) floodZoneObject.SetActive(false);
                    if (mudZoneObject != null) mudZoneObject.SetActive(false);
                    if (fallenTreeObject != null) fallenTreeObject.SetActive(false);
                    if (roadClosureObject != null) roadClosureObject.SetActive(false);
                    if (debrisContainer != null) debrisContainer.SetActive(false);
                    break;

                case 2:
                    // Mission 2: Flooded route on Fork Left (elevated safe route on Fork Right)
                    if (floodZoneObject != null) floodZoneObject.SetActive(true);
                    if (mudZoneObject != null) mudZoneObject.SetActive(false);
                    if (fallenTreeObject != null) fallenTreeObject.SetActive(false);
                    if (roadClosureObject != null) roadClosureObject.SetActive(false);
                    if (debrisContainer != null) debrisContainer.SetActive(true);
                    break;

                case 3:
                    // Mission 3: Fog & strong wind around roundabout & curves
                    if (floodZoneObject != null) floodZoneObject.SetActive(false);
                    if (mudZoneObject != null) mudZoneObject.SetActive(false);
                    if (fallenTreeObject != null) fallenTreeObject.SetActive(false);
                    if (roadClosureObject != null) roadClosureObject.SetActive(false);
                    if (debrisContainer != null) debrisContainer.SetActive(true);
                    break;

                case 4:
                    // Mission 4: Storm damage with fallen tree and road closure requiring U-turn
                    if (floodZoneObject != null) floodZoneObject.SetActive(false);
                    if (mudZoneObject != null) mudZoneObject.SetActive(true);
                    if (fallenTreeObject != null) fallenTreeObject.SetActive(true);
                    if (roadClosureObject != null) roadClosureObject.SetActive(true);
                    if (debrisContainer != null) debrisContainer.SetActive(true);
                    break;

                case 5:
                    // Mission 5: The Grand Finale with full hazards
                    if (floodZoneObject != null) floodZoneObject.SetActive(true);
                    if (mudZoneObject != null) mudZoneObject.SetActive(true);
                    if (fallenTreeObject != null) fallenTreeObject.SetActive(true);
                    if (roadClosureObject != null) roadClosureObject.SetActive(true);
                    if (debrisContainer != null) debrisContainer.SetActive(true);
                    break;
            }
        }

        public void ConfigureForMission(int missionIndex) => ConfigureMissionHazards(missionIndex);

        public void BindHazards(FloodHazardZone[] floods, MudHazardZone[] muds, FallenTreeBlockage[] trees, RoadClosureBarrier[] barriers, DebrisHazard[] debrises)
        {
            if (floods != null && floods.Length > 0) floodZoneObject = floods[0].gameObject;
            if (muds != null && muds.Length > 0) mudZoneObject = muds[0].gameObject;
            if (trees != null && trees.Length > 0) fallenTreeObject = trees[0].gameObject;
            if (barriers != null && barriers.Length > 0) roadClosureObject = barriers[0].gameObject;
            if (debrises != null && debrises.Length > 0) debrisContainer = debrises[0].gameObject;
        }
    }
}
