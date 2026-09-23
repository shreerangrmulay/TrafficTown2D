using System;
using UnityEngine;

namespace TrafficTown2D.Level5
{
    public class TrafficInspectorController : MonoBehaviour
    {
        public static TrafficInspectorController Instance { get; private set; }

        [Header("Visuals & Animation")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float breatheSpeed = 2.4f;
        [SerializeField] private float breatheIntensity = 0.035f;
        [SerializeField] private float gestureInterval = 6.0f;

        private float gestureTimer = 0f;
        private Vector3 baseScale = Vector3.one;
        private float targetRotationZ = 0f;
        private float currentRotationZ = 0f;

        public float SafetyRadius => 1.4f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (visualRoot == null)
            {
                Transform v = transform.Find("Visual");
                visualRoot = v != null ? v : transform;
            }
            baseScale = visualRoot.localScale;
        }

        private void Start()
        {
            if (TrafficPhaseController.Instance != null)
            {
                TrafficPhaseController.Instance.PhaseChanged += OnPhaseChanged;
            }
        }

        private void OnDestroy()
        {
            if (TrafficPhaseController.Instance != null)
            {
                TrafficPhaseController.Instance.PhaseChanged -= OnPhaseChanged;
            }
        }

        private void Update()
        {
            // 1. Subtle breathing animation
            float breath = Mathf.Sin(Time.time * breatheSpeed) * breatheIntensity;
            visualRoot.localScale = new Vector3(baseScale.x * (1f + breath), baseScale.y * (1f - breath * 0.5f), baseScale.z);

            // 2. Smoothly rotate towards active traffic direction
            currentRotationZ = Mathf.LerpAngle(currentRotationZ, targetRotationZ, Time.deltaTime * 3.5f);
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, currentRotationZ);

            // 3. Periodic whistle / gesture
            gestureTimer += Time.deltaTime;
            if (gestureTimer >= gestureInterval)
            {
                gestureTimer = 0f;
                // Slight bounce gesture
                visualRoot.localScale = baseScale * 1.08f;
            }
        }

        private void OnPhaseChanged(IntersectionPhase phase)
        {
            switch (phase)
            {
                case IntersectionPhase.NorthSouthGreen:
                    targetRotationZ = 0f; // Facing down/up towards N/S
                    break;
                case IntersectionPhase.EastWestGreen:
                    targetRotationZ = 90f; // Facing East/West
                    break;
                case IntersectionPhase.PedestrianWalk:
                    targetRotationZ = 45f; // Facing crosswalk quadrant
                    break;
            }
        }

        public void SetVisualRoot(Transform root)
        {
            visualRoot = root;
            if (visualRoot != null)
            {
                baseScale = visualRoot.localScale;
            }
        }
    }
}
