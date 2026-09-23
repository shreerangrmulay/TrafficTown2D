using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TrafficTown2D.Level4
{
    public class OncomingTrafficVehicle : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed = 3.5f;
        [SerializeField] private Vector2 direction = Vector2.down;
        [SerializeField] private bool isDriving = false;

        [Header("Headlights")]
        [SerializeField] private SpriteRenderer[] headlights;
        [SerializeField] private Light2D headlightLight;
        [SerializeField] private SpriteRenderer beamCone;

        [Header("Dazzle Detection")]
        [SerializeField] private float dazzleDistanceThreshold = 24f;
        [SerializeField] private float dazzleToleranceTime = 1.8f;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private PlayerHeadlightController playerHeadlights;

        // State
        private bool dazzleEncounterActive = false;
        private bool dazzleViolationRecorded = false;
        private bool courteousDipAwarded = false;
        private float highBeamTimer = 0f;

        public event Action OnDazzleWarning;
        public event Action OnDazzlePenalty;
        public event Action OnCourteousDipSuccess;

        public bool IsDriving => isDriving;

        private void Start()
        {
            if (playerHeadlights == null)
            {
                playerHeadlights = UnityEngine.Object.FindAnyObjectByType<PlayerHeadlightController>();
            }
            if (playerTransform == null && playerHeadlights != null)
            {
                playerTransform = playerHeadlights.transform;
            }

            ConfigureHeadlights();
        }

        public void StartDriving()
        {
            isDriving = true;
        }

        public void StopDriving()
        {
            isDriving = false;
        }

        private void ConfigureHeadlights()
        {
            if (headlightLight != null)
            {
                headlightLight.enabled = true;
                headlightLight.pointLightOuterRadius = 10f;
                headlightLight.pointLightOuterAngle = 55f;
                headlightLight.intensity = 1.2f;
            }
            if (beamCone != null)
            {
                beamCone.enabled = true;
                Color c = beamCone.color;
                c.a = 0.35f;
                beamCone.color = c;
            }
        }

        private void Update()
        {
            if (isDriving)
            {
                transform.position += (Vector3)(direction * speed * Time.deltaTime);
            }

            CheckDazzleInteraction();
        }

        private void CheckDazzleInteraction()
        {
            if (playerTransform == null || playerHeadlights == null) return;

            // Player is approaching if player Y is less than oncoming vehicle Y
            float distanceY = transform.position.y - playerTransform.position.y;
            float totalDist = Vector2.Distance(transform.position, playerTransform.position);

            // Active dazzle zone: car is in front of player and within range
            if (distanceY > 1f && totalDist <= dazzleDistanceThreshold)
            {
                dazzleEncounterActive = true;

                if (playerHeadlights.IsHighBeam)
                {
                    highBeamTimer += Time.deltaTime;
                    OnDazzleWarning?.Invoke();

                    if (highBeamTimer >= dazzleToleranceTime && !dazzleViolationRecorded)
                    {
                        dazzleViolationRecorded = true;
                        OnDazzlePenalty?.Invoke();
                    }
                }
                else if (playerHeadlights.IsLowBeam)
                {
                    // Player successfully dipped high beam to low beam!
                    if (!courteousDipAwarded && !dazzleViolationRecorded)
                    {
                        courteousDipAwarded = true;
                        OnCourteousDipSuccess?.Invoke();
                    }
                }
            }
            else if (distanceY <= 0.5f && dazzleEncounterActive)
            {
                // Car has safely passed the player
                dazzleEncounterActive = false;
            }
        }
    }
}
