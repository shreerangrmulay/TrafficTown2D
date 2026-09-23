using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TrafficTown2D.Core;

namespace TrafficTown2D.Level7
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Level7PlayerCar : MonoBehaviour
    {
        [Header("Driving Physics")]
        [SerializeField, Min(1f)] private float maxSpeed = 8.5f;          // 8.5 units/s ~= 42 km/h
        [SerializeField, Min(1f)] private float acceleration = 5.5f;      // Acceleration rate
        [SerializeField, Min(1f)] private float brakingRate = 9.0f;       // Braking deceleration
        [SerializeField, Min(1f)] private float naturalDrag = 3.2f;       // Natural coasting drag
        [SerializeField, Min(10f)] private float steeringSpeed = 130f;    // Steering rate deg/s
        [SerializeField, Min(1f)] private float maxReverseSpeed = 3.0f;   // Reverse limit
        [SerializeField] private float speedLimitKmh = 40f;

        [Header("Visuals & Lighting")]
        [SerializeField] private SpriteRenderer[] brakeLights;
        [SerializeField] private SpriteRenderer[] headlights;
        [SerializeField] private Color brakeNormalColor = new Color(0.5f, 0.1f, 0.1f, 0.7f);
        [SerializeField] private Color brakeActiveColor = new Color(1.0f, 0.15f, 0.15f, 1f);

        [Header("Audio")]
        [SerializeField] private AudioSource engineAudio;
        [SerializeField] private AudioSource brakeAudio;
        [SerializeField] private AudioSource hornAudio;

        // Dynamic State
        private Rigidbody2D rb;
        private float forwardSpeed = 0f;
        private float throttleInput = 0f;
        private float steerInput = 0f;
        private bool isBraking = false;
        private bool isHandbraking = false;
        private bool isControlEnabled = true;
        private float collisionCooldown = 0f;

        // Public Accessors
        public float ForwardSpeed => forwardSpeed;
        public float CurrentSpeedKmh => Mathf.Abs(forwardSpeed) * 5f;
        public float SpeedLimitKmh => speedLimitKmh;
        public bool IsStopped => Mathf.Abs(forwardSpeed) < 0.12f;
        public bool IsControlEnabled => isControlEnabled;

        // Events
        public event Action<float, float> SpeedChanged;                     // (speedKmh, limitKmh)
        public event Action<Collision2D> CarCollided;
        public event Action<bool> BrakeChanged;

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
            Time.timeScale = 1f;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }
        }

        public void SetControlEnabled(bool enabled)
        {
            isControlEnabled = enabled;
            if (!enabled)
            {
                throttleInput = 0f;
                steerInput = 0f;
                isBraking = false;
                isHandbraking = false;
            }
        }

        public void StopCar()
        {
            forwardSpeed = 0f;
            throttleInput = 0f;
            steerInput = 0f;
            isBraking = false;
            isHandbraking = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        private void Update()
        {
            bool isPaused = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused;
            if (!isControlEnabled || isPaused)
            {
                throttleInput = 0f;
                steerInput = 0f;
                UpdateBrakeLights();
                SpeedChanged?.Invoke(CurrentSpeedKmh, speedLimitKmh);
                return;
            }

            ReadInput();
            UpdateBrakeLights();
            UpdateEngineAudio();

            if (collisionCooldown > 0f)
            {
                collisionCooldown -= Time.deltaTime;
            }

            SpeedChanged?.Invoke(CurrentSpeedKmh, speedLimitKmh);
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

            ApplyDrivingPhysics();
        }

        private void ReadInput()
        {
            throttleInput = 0f;
            steerInput = 0f;
            isHandbraking = false;

            // 1. Modern Input System (Keyboard.current)
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) throttleInput += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) throttleInput -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steerInput += 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steerInput -= 1f;
                if (kb.spaceKey.isPressed) isHandbraking = true;
            }

            // 2. Direct Key Polling Fallback (always works regardless of Input System configuration)
            if (Mathf.Approximately(throttleInput, 0f) && Mathf.Approximately(steerInput, 0f))
            {
                try
                {
                    if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) throttleInput += 1f;
                    if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) throttleInput -= 1f;
                    if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) steerInput += 1f;
                    if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) steerInput -= 1f;
                    if (Input.GetKey(KeyCode.Space)) isHandbraking = true;

                    if (Mathf.Approximately(throttleInput, 0f) && Mathf.Approximately(steerInput, 0f))
                    {
                        float v = Input.GetAxisRaw("Vertical");
                        float h = Input.GetAxisRaw("Horizontal");
                        if (Mathf.Abs(v) > 0.1f) throttleInput = Mathf.Sign(v);
                        if (Mathf.Abs(h) > 0.1f) steerInput = -Mathf.Sign(h);
                    }
                }
                catch { }
            }
        }

        private void ApplyDrivingPhysics()
        {
            if (isHandbraking)
            {
                isBraking = true;
                forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, brakingRate * 2.5f * Time.fixedDeltaTime);
            }
            else if (throttleInput > 0.05f)
            {
                isBraking = false;
                forwardSpeed += acceleration * throttleInput * Time.fixedDeltaTime;
                if (forwardSpeed > maxSpeed)
                {
                    forwardSpeed = Mathf.MoveTowards(forwardSpeed, maxSpeed, naturalDrag * Time.fixedDeltaTime);
                }
            }
            else if (throttleInput < -0.05f)
            {
                if (forwardSpeed > 0.1f)
                {
                    // Braking while rolling forward
                    isBraking = true;
                    forwardSpeed -= brakingRate * Mathf.Abs(throttleInput) * Time.fixedDeltaTime;
                    if (forwardSpeed < 0f) forwardSpeed = 0f;
                }
                else
                {
                    // Reverse
                    isBraking = false;
                    forwardSpeed -= (acceleration * 0.6f) * Mathf.Abs(throttleInput) * Time.fixedDeltaTime;
                    forwardSpeed = Mathf.Max(-maxReverseSpeed, forwardSpeed);
                }
            }
            else
            {
                // Natural deceleration / coasting
                isBraking = false;
                forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, naturalDrag * Time.fixedDeltaTime);
            }

            // Steering: responsive from low speeds, inverts slightly when reversing
            if (Mathf.Abs(steerInput) > 0.05f)
            {
                float speedFactor = Mathf.Clamp(Mathf.Abs(forwardSpeed) / 2.5f, 0.25f, 1f);
                float direction = forwardSpeed >= -0.05f ? 1f : -1f;
                float rotationChange = steerInput * steeringSpeed * speedFactor * direction * Time.fixedDeltaTime;
                rb.rotation += rotationChange;
            }

            // Apply forward velocity
            Vector2 forwardDir = transform.up;
            rb.linearVelocity = forwardDir * forwardSpeed;
        }

        private void UpdateBrakeLights()
        {
            if (brakeLights == null || brakeLights.Length == 0) return;
            Color targetColor = (isBraking || isHandbraking) ? brakeActiveColor : brakeNormalColor;
            for (int i = 0; i < brakeLights.Length; i++)
            {
                if (brakeLights[i] != null) brakeLights[i].color = targetColor;
            }
        }

        private void UpdateEngineAudio()
        {
            if (engineAudio == null) return;
            float speedRatio = Mathf.Clamp01(CurrentSpeedKmh / 50f);
            engineAudio.pitch = Mathf.Lerp(0.85f, 1.45f, speedRatio);
            engineAudio.volume = Mathf.Lerp(0.35f, 0.75f, speedRatio);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collisionCooldown > 0f) return;
            collisionCooldown = 0.5f;

            // Bounce back slightly on collision
            forwardSpeed = -forwardSpeed * 0.35f;

            CarCollided?.Invoke(collision);
        }
    }
}
