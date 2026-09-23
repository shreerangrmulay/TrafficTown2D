using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TrafficTown2D.Core;

namespace TrafficTown2D.Level3
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Level3PlayerCar : MonoBehaviour
    {
        [Header("Driving Physics")]
        [SerializeField, Min(1f)] private float maxSpeed = 8f;             // Max forward speed (8 units/s = 40 km/h)
        [SerializeField, Min(1f)] private float acceleration = 5f;          // Acceleration rate
        [SerializeField, Min(1f)] private float brakingRate = 8f;           // Braking deceleration rate
        [SerializeField, Min(1f)] private float naturalDrag = 3.2f;         // Natural coasting deceleration
        [SerializeField, Min(10f)] private float steeringSpeed = 120f;      // Degrees per second
        [SerializeField, Min(1f)] private float maxReverseSpeed = 2.5f;     // Max reverse speed

        [Header("Speed Limits")]
        [SerializeField] private float currentSpeedLimitKmh = 40f;
        public float CurrentSpeedLimitKmh
        {
            get => currentSpeedLimitKmh;
            set => currentSpeedLimitKmh = Mathf.Max(5f, value);
        }

        public float BrakingRate
        {
            get => brakingRate;
            set => brakingRate = Mathf.Max(1f, value);
        }

        [Header("Visual Elements")]
        [SerializeField] private SpriteRenderer[] brakeLights;
        [SerializeField] private SpriteRenderer[] headlights;
        [SerializeField] private Color brakeNormalColor = new Color(0.5f, 0.1f, 0.1f, 0.7f);
        [SerializeField] private Color brakeActiveColor = new Color(1f, 0.12f, 0.10f, 1f);

        [Header("Audio / Effects")]
        [SerializeField] private AudioSource engineAudio;

        // State
        private Rigidbody2D rb;
        private float forwardSpeed;
        private float throttleInput;
        private float steerInput;
        private bool isBraking;
        private bool isOffRoad;
        private float collisionCooldown;

        [Header("Controls")]
        [SerializeField] private bool isControlEnabled = true;

        // Events
        public event Action<float, float> SpeedChanged;                     // (speedKmh, limitKmh)
        public event Action<Collision2D> CarCollided;
        public event Action<bool> BrakeChanged;

        /// <summary>Forward speed in km/h (1 unit/s = 5 km/h).</summary>
        public float CurrentSpeedKmh => Mathf.Abs(forwardSpeed) * 5f;

        /// <summary>Raw world forward velocity magnitude.</summary>
        public float ForwardSpeed => forwardSpeed;

        /// <summary>Whether car is currently at a complete stop.</summary>
        public bool IsStopped => Mathf.Abs(forwardSpeed) < 0.1f;

        /// <summary>Enables or disables player throttle and steering controls.</summary>
        public void SetControlEnabled(bool enabled)
        {
            isControlEnabled = enabled;
            if (!enabled)
            {
                throttleInput = 0f;
                steerInput = 0f;
            }
        }

        /// <summary>Immediately halts the car's movement and resets throttle/steering.</summary>
        public void StopCar()
        {
            forwardSpeed = 0f;
            throttleInput = 0f;
            steerInput = 0f;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
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
            // Only lock controls if explicitly disabled or paused
            bool isPaused = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused;
            if (!isControlEnabled || isPaused)
            {
                throttleInput = 0f;
                steerInput = 0f;
                UpdateBrakeLights();
                SpeedChanged?.Invoke(CurrentSpeedKmh, currentSpeedLimitKmh);
                return;
            }

            ReadInput();
            UpdateBrakeLights();
            UpdateEngineAudio();

            if (collisionCooldown > 0f)
            {
                collisionCooldown -= Time.deltaTime;
            }

            SpeedChanged?.Invoke(CurrentSpeedKmh, currentSpeedLimitKmh);
        }

        private void FixedUpdate()
        {
            bool isPaused = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused;
            if (!isControlEnabled || isPaused)
            {
                forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, brakingRate * 1.5f * Time.fixedDeltaTime);
                if (Mathf.Abs(forwardSpeed) < 0.05f) forwardSpeed = 0f;
                if (rb != null) rb.linearVelocity = (Vector2)transform.up * forwardSpeed;
                return;
            }

            ApplyDrivingPhysics();
        }

        private void ReadInput()
        {
            throttleInput = 0f;
            steerInput = 0f;

            // 1. Try modern Input System Keyboard
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) throttleInput += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) throttleInput -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steerInput += 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steerInput -= 1f;
            }

            // 2. Fallback to classic Input (works if modern input returned 0 or if old InputManager is active)
            if (Mathf.Approximately(throttleInput, 0f) && Mathf.Approximately(steerInput, 0f))
            {
                try
                {
                    float v = Input.GetAxisRaw("Vertical");
                    float h = Input.GetAxisRaw("Horizontal");
                    if (Mathf.Abs(v) > 0.1f) throttleInput = Mathf.Sign(v);
                    if (Mathf.Abs(h) > 0.1f) steerInput = -Mathf.Sign(h);
                }
                catch
                {
                    // In case classic input axes are not configured
                }
            }
        }

        private void ApplyDrivingPhysics()
        {
            float effectiveMaxSpeed = maxSpeed;
            if (isOffRoad)
            {
                // Road shoulder / edge slowdown
                effectiveMaxSpeed = Mathf.Min(maxSpeed * 0.45f, 3.5f);
            }

            // Acceleration & Braking
            if (throttleInput > 0.05f)
            {
                isBraking = false;
                forwardSpeed += acceleration * throttleInput * Time.fixedDeltaTime;
                if (forwardSpeed > effectiveMaxSpeed)
                {
                    forwardSpeed = Mathf.MoveTowards(forwardSpeed, effectiveMaxSpeed, naturalDrag * Time.fixedDeltaTime);
                }
            }
            else if (throttleInput < -0.05f)
            {
                if (forwardSpeed > 0.1f)
                {
                    // Braking
                    isBraking = true;
                    forwardSpeed -= brakingRate * Mathf.Abs(throttleInput) * Time.fixedDeltaTime;
                    if (forwardSpeed < 0f) forwardSpeed = 0f;
                }
                else
                {
                    // Reverse
                    isBraking = false;
                    forwardSpeed -= (acceleration * 0.6f) * Mathf.Abs(throttleInput) * Time.fixedDeltaTime;
                    forwardSpeed = Mathf.Max(forwardSpeed, -maxReverseSpeed);
                }
            }
            else
            {
                // Coasting / Friction
                isBraking = false;
                float drag = isOffRoad ? naturalDrag * 2.5f : naturalDrag;
                forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, drag * Time.fixedDeltaTime);
            }

            // Steering (scales smoothly with forward speed, inverts slightly when reversing)
            if (Mathf.Abs(steerInput) > 0.05f && Mathf.Abs(forwardSpeed) > 0.08f)
            {
                float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 3f);
                float direction = forwardSpeed >= 0f ? 1f : -1f;
                float rotationChange = steerInput * steeringSpeed * speedFactor * direction * Time.fixedDeltaTime;
                rb.rotation += rotationChange;
            }

            // Apply forward velocity in vehicle's facing direction
            Vector2 forwardDir = transform.up;
            rb.linearVelocity = forwardDir * forwardSpeed;
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

        public void SetOffRoad(bool offRoad)
        {
            isOffRoad = offRoad;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collisionCooldown > 0f) return;
            collisionCooldown = 1.6f;

            // Reduce speed significantly upon collision
            forwardSpeed *= 0.2f;

            CarCollided?.Invoke(collision);
        }
    }
}
