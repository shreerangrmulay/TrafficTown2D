using System;
using UnityEngine;

namespace TrafficTown2D.Level4
{
    public class NightVisibilityController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerHeadlightController headlightController;
        [SerializeField] private Transform playerTransform;

        [Header("Streetlamps")]
        [SerializeField] private Transform[] streetlamps;
        [SerializeField] private float streetlampIlluminationRadius = 6.5f;

        [Header("Rain Influence")]
        [SerializeField] private bool isRaining = false;
        [SerializeField] private float rainVisibilityPenalty = 20f;

        private float currentVisibilityPercent = 30f;
        public float CurrentVisibilityPercent => currentVisibilityPercent;
        public string CurrentVisibilityFormatted => $"{Mathf.RoundToInt(currentVisibilityPercent)}%";
        public bool IsRaining => isRaining;

        public event Action<float> VisibilityChanged;

        private void Start()
        {
            if (headlightController != null)
            {
                headlightController.HeadlightModeChanged += OnHeadlightChanged;
            }
            RecalculateVisibility();
        }

        private void OnDestroy()
        {
            if (headlightController != null)
            {
                headlightController.HeadlightModeChanged -= OnHeadlightChanged;
            }
        }

        private void Update()
        {
            // Regularly evaluate streetlight proximity
            RecalculateVisibility();
        }

        private void OnHeadlightChanged(HeadlightMode mode)
        {
            RecalculateVisibility();
        }

        public void SetRaining(bool raining)
        {
            isRaining = raining;
            RecalculateVisibility();
        }

        public void SetStreetlamps(Transform[] lamps)
        {
            streetlamps = lamps;
            RecalculateVisibility();
        }

        private void RecalculateVisibility()
        {
            float vis = 30f; // Ambient baseline darkness

            // 1. Headlight contribution
            if (headlightController != null)
            {
                switch (headlightController.CurrentMode)
                {
                    case HeadlightMode.LowBeam:
                        vis += 40f;
                        break;
                    case HeadlightMode.HighBeam:
                        vis += 60f;
                        break;
                }
            }

            // 2. Streetlamp proximity
            if (playerTransform != null && streetlamps != null)
            {
                bool nearLamp = false;
                Vector2 pPos = playerTransform.position;
                for (int i = 0; i < streetlamps.Length; i++)
                {
                    if (streetlamps[i] == null) continue;
                    if (Vector2.Distance(pPos, streetlamps[i].position) <= streetlampIlluminationRadius)
                    {
                        nearLamp = true;
                        break;
                    }
                }

                if (nearLamp)
                {
                    vis += 15f;
                }
            }

            // 3. Rain penalty
            if (isRaining)
            {
                vis -= rainVisibilityPenalty;
            }

            vis = Mathf.Clamp(vis, 15f, 100f);

            if (!Mathf.Approximately(vis, currentVisibilityPercent))
            {
                currentVisibilityPercent = vis;
                VisibilityChanged?.Invoke(currentVisibilityPercent);
            }
        }

        public Color GetVisibilityColor()
        {
            if (currentVisibilityPercent >= 75f)
            {
                return new Color(0.2f, 0.85f, 0.4f, 1f); // Safe green
            }
            else if (currentVisibilityPercent >= 50f)
            {
                return new Color(1f, 0.75f, 0.2f, 1f); // Caution amber
            }
            else
            {
                return new Color(0.95f, 0.25f, 0.25f, 1f); // Hazard red
            }
        }
    }
}
