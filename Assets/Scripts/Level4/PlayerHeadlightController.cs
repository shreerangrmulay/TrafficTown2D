using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace TrafficTown2D.Level4
{
    public enum HeadlightMode
    {
        Off,
        LowBeam,
        HighBeam
    }

    public class PlayerHeadlightController : MonoBehaviour
    {
        [Header("Headlight Mode")]
        [SerializeField] private HeadlightMode currentMode = HeadlightMode.Off;

        [Header("Light2D Sources")]
        [SerializeField] private Light2D leftSpotLight;
        [SerializeField] private Light2D rightSpotLight;
        [SerializeField] private Light2D forwardBeamLight;

        [Header("Beam Sprite Renderers")]
        [SerializeField] private SpriteRenderer leftBeamCone;
        [SerializeField] private SpriteRenderer rightBeamCone;

        [Header("Bulb Sprites")]
        [SerializeField] private SpriteRenderer[] bulbRenderers;

        [Header("Low Beam Settings")]
        [SerializeField] private float lowBeamRange = 11f;
        [SerializeField] private float lowBeamOuterAngle = 55f;
        [SerializeField] private float lowBeamIntensity = 1.3f;
        [SerializeField] private Vector3 lowBeamConeScale = new Vector3(1.5f, 9.5f, 1f);
        [SerializeField] private float lowBeamConeAlpha = 0.35f;

        [Header("High Beam Settings")]
        [SerializeField] private float highBeamRange = 22f;
        [SerializeField] private float highBeamOuterAngle = 42f;
        [SerializeField] private float highBeamIntensity = 2.2f;
        [SerializeField] private Vector3 highBeamConeScale = new Vector3(2.2f, 20f, 1f);
        [SerializeField] private float highBeamConeAlpha = 0.55f;

        [Header("Colors")]
        [SerializeField] private Color beamWarmColor = new Color(1f, 0.96f, 0.82f, 1f);
        [SerializeField] private Color bulbOffColor = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        [SerializeField] private Color bulbOnColor = new Color(1f, 1f, 0.85f, 1f);

        public HeadlightMode CurrentMode => currentMode;
        public bool IsOn => currentMode != HeadlightMode.Off;
        public bool IsHighBeam => currentMode == HeadlightMode.HighBeam;
        public bool IsLowBeam => currentMode == HeadlightMode.LowBeam;

        public event Action<HeadlightMode> HeadlightModeChanged;

        private void Start()
        {
            ApplyModeVisuals();
        }

        private void Update()
        {
            HandleInputs();
        }

        private void HandleInputs()
        {
            bool toggleHeadlights = false;
            bool toggleBeam = false;

            // 1. Modern Input System
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.hKey.wasPressedThisFrame) toggleHeadlights = true;
                if (kb.fKey.wasPressedThisFrame) toggleBeam = true;
            }

            // 2. Classic Input System fallback
            if (!toggleHeadlights && !toggleBeam)
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.H)) toggleHeadlights = true;
                    if (Input.GetKeyDown(KeyCode.F)) toggleBeam = true;
                }
                catch
                {
                    // Ignore if classic input is unavailable
                }
            }

            if (toggleHeadlights)
            {
                ToggleHeadlights();
            }
            else if (toggleBeam)
            {
                ToggleBeamMode();
            }
        }

        public void ToggleHeadlights()
        {
            if (currentMode == HeadlightMode.Off)
            {
                SetMode(HeadlightMode.LowBeam);
            }
            else
            {
                SetMode(HeadlightMode.Off);
            }
        }

        public void ToggleBeamMode()
        {
            if (currentMode == HeadlightMode.Off)
            {
                // Turning on directly to Low Beam
                SetMode(HeadlightMode.LowBeam);
            }
            else if (currentMode == HeadlightMode.LowBeam)
            {
                SetMode(HeadlightMode.HighBeam);
            }
            else
            {
                SetMode(HeadlightMode.LowBeam);
            }
        }

        public void SetMode(HeadlightMode newMode)
        {
            if (currentMode == newMode) return;
            currentMode = newMode;
            ApplyModeVisuals();
            HeadlightModeChanged?.Invoke(currentMode);
        }

        private void ApplyModeVisuals()
        {
            switch (currentMode)
            {
                case HeadlightMode.Off:
                    SetLightsActive(false, 0f, 0f);
                    SetBeamConesActive(false, Vector3.one, 0f);
                    SetBulbsColor(bulbOffColor);
                    break;

                case HeadlightMode.LowBeam:
                    SetLightsActive(true, lowBeamRange, lowBeamIntensity, lowBeamOuterAngle);
                    SetBeamConesActive(true, lowBeamConeScale, lowBeamConeAlpha);
                    SetBulbsColor(bulbOnColor);
                    break;

                case HeadlightMode.HighBeam:
                    SetLightsActive(true, highBeamRange, highBeamIntensity, highBeamOuterAngle);
                    SetBeamConesActive(true, highBeamConeScale, highBeamConeAlpha);
                    SetBulbsColor(new Color(1f, 1f, 1f, 1f));
                    break;
            }
        }

        private void SetLightsActive(bool active, float range, float intensity, float outerAngle = 50f)
        {
            if (leftSpotLight != null)
            {
                leftSpotLight.enabled = active;
                if (active)
                {
                    leftSpotLight.pointLightOuterRadius = range;
                    leftSpotLight.pointLightOuterAngle = outerAngle;
                    leftSpotLight.intensity = intensity;
                    leftSpotLight.color = beamWarmColor;
                }
            }

            if (rightSpotLight != null)
            {
                rightSpotLight.enabled = active;
                if (active)
                {
                    rightSpotLight.pointLightOuterRadius = range;
                    rightSpotLight.pointLightOuterAngle = outerAngle;
                    rightSpotLight.intensity = intensity;
                    rightSpotLight.color = beamWarmColor;
                }
            }

            if (forwardBeamLight != null)
            {
                forwardBeamLight.enabled = active;
                if (active)
                {
                    forwardBeamLight.pointLightOuterRadius = range * 1.1f;
                    forwardBeamLight.pointLightOuterAngle = outerAngle * 0.9f;
                    forwardBeamLight.intensity = intensity;
                    forwardBeamLight.color = beamWarmColor;
                }
            }
        }

        private void SetBeamConesActive(bool active, Vector3 scale, float alpha)
        {
            if (leftBeamCone != null)
            {
                leftBeamCone.enabled = active;
                if (active)
                {
                    leftBeamCone.transform.localScale = scale;
                    Color c = beamWarmColor;
                    c.a = alpha;
                    leftBeamCone.color = c;
                }
            }

            if (rightBeamCone != null)
            {
                rightBeamCone.enabled = active;
                if (active)
                {
                    rightBeamCone.transform.localScale = scale;
                    Color c = beamWarmColor;
                    c.a = alpha;
                    rightBeamCone.color = c;
                }
            }
        }

        private void SetBulbsColor(Color color)
        {
            if (bulbRenderers == null) return;
            for (int i = 0; i < bulbRenderers.Length; i++)
            {
                if (bulbRenderers[i] != null)
                {
                    bulbRenderers[i].color = color;
                }
            }
        }

        /// <summary>
        /// Checks if a world position ahead is within the cone of illumination.
        /// </summary>
        public bool IsIlluminating(Vector2 targetPos)
        {
            if (!IsOn) return false;

            Vector2 carPos = transform.position;
            Vector2 forward = transform.up;
            Vector2 toTarget = targetPos - carPos;
            float distance = toTarget.magnitude;

            float maxRange = IsHighBeam ? highBeamRange : lowBeamRange;
            if (distance > maxRange) return false;

            float dot = Vector2.Dot(forward, toTarget.normalized);
            // Low beam angle ~60 deg (cos 30 = 0.86), High beam ~45 deg (cos 22.5 = 0.92)
            float threshold = IsHighBeam ? 0.90f : 0.84f;
            return dot >= threshold;
        }

        public float GetEffectiveRange()
        {
            if (!IsOn) return 0f;
            return IsHighBeam ? highBeamRange : lowBeamRange;
        }
    }
}
