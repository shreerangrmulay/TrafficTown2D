using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TrafficTown2D.Core;

namespace TrafficTown2D.Level6
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Level6PlayerCar : MonoBehaviour
    {
        [Header("Driving Physics")]
        [SerializeField, Min(1f)] private float maxSpeed = 8.5f;          // 8.5 units/s ~= 42 km/h
        [SerializeField, Min(1f)] private float acceleration = 5.2f;      // Base acceleration
        [SerializeField, Min(1f)] private float brakingRate = 8.5f;       // Base braking rate
        [SerializeField, Min(1f)] private float naturalDrag = 3.2f;       // Natural rolling resistance
        [SerializeField, Min(10f)] private float steeringSpeed = 125f;    // Base degrees per second
        [SerializeField, Min(1f)] private float maxReverseSpeed = 2.8f;   // Max reverse speed

        [Header("Environmental Adaptation")]
        [SerializeField, Range(0.1f, 1f)] private float currentGrip = 1.0f;
        [SerializeField] private Vector2 windVelocity = Vector2.zero;
        [SerializeField] private bool isFlooded = false;
        [SerializeField] private bool isMuddy = false;
        [SerializeField] private bool isOffRoad = false;

        [Header("Visuals & Lighting")]
        [SerializeField] private SpriteRenderer[] brakeLights;
        [SerializeField] private SpriteRenderer[] headlights;
        [SerializeField] private Color brakeNormalColor = new Color(0.5f, 0.1f, 0.1f, 0.7f);
        [SerializeField] private Color brakeActiveColor = new Color(1f, 0.12f, 0.10f, 1f);

        [Header("Audio")]
        [SerializeField] private AudioSource engineAudio;

        // Internal State
        private Rigidbody2D rb;
        private float forwardSpeed = 0f;
        private float throttleInput = 0f;
        private float steerInput = 0f;
        private bool isBraking = false;
        private bool isControlEnabled = true;
        private float collisionCooldown = 0f;
        private bool wasSkidding = false;

        // Public Telemetry & Properties
        public float CurrentSpeedKmh => Mathf.Abs(forwardSpeed) * 5f;
        public float ForwardSpeed => forwardSpeed;
        public bool IsStopped => Mathf.Abs(forwardSpeed) < 0.1f;
        public bool IsBraking => isBraking;
        public float CurrentGripRatio => Mathf.Clamp01(currentGrip);
        public bool IsFlooded => isFlooded;
        public bool IsMuddy => isMuddy;
        public bool IsOffRoad => isOffRoad;
        public Rigidbody2D Body => rb;

        // Events
        public event Action<float, float> SpeedChanged;
        public event Action<float> GripChanged;
        public event Action<bool> SkidStateChanged;
        public event Action<Collision2D> CarCollided;
        public event Action<bool> BrakeChanged;
        public event Action<bool> OffRoadChanged;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        private void Start()
        {
            isControlEnabled = true;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }
        }

        private void Update()
        {
            bool isPaused = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused;
            if (!isControlEnabled || isPaused)
            {
                throttleInput = 0f;
                steerInput = 0f;
                UpdateBrakeLights();
                SpeedChanged?.Invoke(CurrentSpeedKmh, 40f);
                if (wasSkidding)
                {
                    wasSkidding = false;
                    SkidStateChanged?.Invoke(false);
                }
                return;
            }

            ReadInput();
            UpdateBrakeLights();
            UpdateEngineAudio();

            if (collisionCooldown > 0f)
            {
                collisionCooldown -= Time.deltaTime;
            }

            SpeedChanged?.Invoke(CurrentSpeedKmh, 40f);
        }

        private void FixedUpdate()
        {
            bool isPaused = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused;
            if (!isControlEnabled || isPaused)
            {
                forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, brakingRate * 2f * Time.fixedDeltaTime);
                if (Mathf.Abs(forwardSpeed) < 0.05f) forwardSpeed = 0f;
                if (rb != null) rb.linearVelocity = (Vector2)transform.up * forwardSpeed;
                return;
            }

            // Road containment check
            if (Level6RoadNetwork.Instance != null)
            {
                bool onRoad = Level6RoadNetwork.Instance.IsPointOnRoad(transform.position);
                bool newlyOffRoad = !onRoad;
                if (newlyOffRoad != isOffRoad)
                {
                    isOffRoad = newlyOffRoad;
                    OffRoadChanged?.Invoke(isOffRoad);
                }

                if (isOffRoad && Level6SafetyManager.Instance != null)
                {
                    Level6SafetyManager.Instance.RegisterOffRoadTick(Time.fixedDeltaTime);
                }
            }

            ApplyDrivingPhysics();
        }

        private void ReadInput()
        {
            throttleInput = 0f;
            steerInput = 0f;

            // 1. Modern Input System
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) throttleInput += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) throttleInput -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steerInput += 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steerInput -= 1f;
            }

            // 2. Fallback Classic Input
            if (Mathf.Approximately(throttleInput, 0f) && Mathf.Approximately(steerInput, 0f))
            {
                try
                {
                    float v = Input.GetAxisRaw("Vertical");
                    float h = Input.GetAxisRaw("Horizontal");
                    if (Mathf.Abs(v) > 0.1f) throttleInput = Mathf.Sign(v);
                    if (Mathf.Abs(h) > 0.1f) steerInput = -Mathf.Sign(h);
                }
                catch { }
            }
        }

        private void ApplyDrivingPhysics()
        {
            // Effective grip scaling
            float effectiveGrip = currentGrip;
            if (isMuddy) effectiveGrip = Mathf.Min(effectiveGrip, 0.40f);
            if (isFlooded) effectiveGrip = Mathf.Min(effectiveGrip, 0.35f);
            if (isOffRoad) effectiveGrip = Mathf.Min(effectiveGrip, 0.35f);

            // Speed ceilings based on terrain (off-road grass heavily limits speed to ~12 km/h)
            float effectiveMaxSpeed = maxSpeed;
            if (isFlooded) effectiveMaxSpeed = Mathf.Min(effectiveMaxSpeed, 3.2f);
            else if (isMuddy) effectiveMaxSpeed = Mathf.Min(effectiveMaxSpeed, 3.8f);
            else if (isOffRoad) effectiveMaxSpeed = Mathf.Min(effectiveMaxSpeed, 2.4f);

            // Acceleration & Braking with grip scaling
            float effectiveAccel = acceleration * (0.4f + 0.6f * effectiveGrip);
            float effectiveBraking = brakingRate * (0.35f + 0.65f * effectiveGrip);

            if (throttleInput > 0.05f)
            {
                isBraking = false;
                forwardSpeed += effectiveAccel * throttleInput * Time.fixedDeltaTime;
                if (forwardSpeed > effectiveMaxSpeed)
                {
                    float excessDrag = isOffRoad ? naturalDrag * 3.5f : naturalDrag;
                    forwardSpeed = Mathf.MoveTowards(forwardSpeed, effectiveMaxSpeed, excessDrag * Time.fixedDeltaTime);
                }
            }
            else if (throttleInput < -0.05f)
            {
                if (forwardSpeed > 0.1f)
                {
                    // Braking (increased stopping distance when grip is low!)
                    isBraking = true;
                    forwardSpeed -= effectiveBraking * Mathf.Abs(throttleInput) * Time.fixedDeltaTime;
                    if (forwardSpeed < 0f) forwardSpeed = 0f;
                }
                else
                {
                    // Reversing
                    isBraking = false;
                    forwardSpeed -= (effectiveAccel * 0.6f) * Mathf.Abs(throttleInput) * Time.fixedDeltaTime;
                    forwardSpeed = Mathf.Max(forwardSpeed, -maxReverseSpeed);
                }
            }
            else
            {
                // Coasting with surface drag
                isBraking = false;
                float drag = naturalDrag;
                if (isFlooded) drag *= 3.5f;
                else if (isMuddy) drag *= 2.8f;
                else if (isOffRoad) drag *= 3.5f;

                forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, drag * Time.fixedDeltaTime);
            }

            // Steering: scales smoothly with speed, with slightly reduced responsiveness in mud/flood
            if (Mathf.Abs(steerInput) > 0.05f && Mathf.Abs(forwardSpeed) > 0.08f)
            {
                float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 2.8f);
                float direction = forwardSpeed >= 0f ? 1f : -1f;
                float steerGrip = (0.5f + 0.5f * effectiveGrip);
                float rotationChange = steerInput * steeringSpeed * speedFactor * direction * steerGrip * Time.fixedDeltaTime;
                rb.rotation += rotationChange;
            }

            // Apply forward velocity + environmental lateral wind force
            Vector2 forwardDir = transform.up;
            Vector2 velocity = forwardDir * forwardSpeed;

            if (windVelocity.sqrMagnitude > 0.01f)
            {
                // Apply wind force smoothly to vehicle mass
                velocity += windVelocity * Time.fixedDeltaTime;
            }

            rb.linearVelocity = velocity;

            // Skid detection: active when hard steering at speed with reduced grip or heavy braking while turning
            bool isSkiddingNow = (effectiveGrip < 0.75f && Mathf.Abs(steerInput) > 0.45f && Mathf.Abs(forwardSpeed) > 3.8f) ||
                                 (isBraking && Mathf.Abs(steerInput) > 0.35f && Mathf.Abs(forwardSpeed) > 3.0f);
            if (isSkiddingNow != wasSkidding)
            {
                wasSkidding = isSkiddingNow;
                SkidStateChanged?.Invoke(wasSkidding);
            }
        }

        private void UpdateBrakeLights()
        {
            if (brakeLights == null || brakeLights.Length == 0) return;

            Color targetColor = isBraking ? brakeActiveColor : brakeNormalColor;
            for (int i = 0; i < brakeLights.Length; i++)
            {
                if (brakeLights[i] != null)
                {
                    brakeLights[i].color = targetColor;
                }
            }

            BrakeChanged?.Invoke(isBraking);
        }

        private void UpdateEngineAudio()
        {
            if (engineAudio == null) return;

            if (!engineAudio.isPlaying)
            {
                engineAudio.loop = true;
                engineAudio.Play();
            }

            float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / maxSpeed);
            engineAudio.pitch = Mathf.Lerp(0.8f, 1.6f, normalizedSpeed);
            engineAudio.volume = Mathf.Lerp(0.2f, 0.6f, normalizedSpeed + (Mathf.Abs(throttleInput) * 0.2f));
        }

        public void SetControlEnabled(bool enabled)
        {
            isControlEnabled = enabled;
            if (!enabled)
            {
                throttleInput = 0f;
                steerInput = 0f;
            }
        }

        public void StopCar()
        {
            forwardSpeed = 0f;
            throttleInput = 0f;
            steerInput = 0f;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        public void Teleport(Vector2 position, float headingDeg)
        {
            StopCar();
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            transform.rotation = Quaternion.Euler(0f, 0f, headingDeg);
            if (rb != null)
            {
                rb.position = position;
                rb.rotation = headingDeg;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        public void SetGripModifier(float grip)
        {
            currentGrip = Mathf.Clamp(grip, 0.2f, 1.0f);
            GripChanged?.Invoke(currentGrip);
        }

        public void SetWindVelocity(Vector2 wind)
        {
            windVelocity = wind;
        }

        public void SetTerrainConditions(bool flooded, bool muddy, bool offRoad)
        {
            isFlooded = flooded;
            isMuddy = muddy;
            isOffRoad = offRoad;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collisionCooldown > 0f) return;
            collisionCooldown = 1.6f;

            // Reduce speed significantly upon collision
            forwardSpeed *= 0.25f;

            CarCollided?.Invoke(collision);
        }
    }
}
