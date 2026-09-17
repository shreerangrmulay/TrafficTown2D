using UnityEngine;
using UnityEngine.InputSystem;
using TrafficTown2D.Core;

namespace TrafficTown2D.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CarPlayerController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float maxSpeed = 8f;
        [SerializeField, Min(0f)] private float acceleration = 10f;
        [SerializeField, Min(0f)] private float deceleration = 15f;
        [SerializeField] private bool allowVerticalMovement = false;
        [SerializeField, Min(0f)] private float verticalSpeed = 3f;

        private Rigidbody2D body;
        private float currentSpeed;
        private float moveInput;
        private float verticalInput;

        /// <summary>Current horizontal speed of the car (absolute value).</summary>
        public float CurrentSpeed => Mathf.Abs(currentSpeed);

        /// <summary>Current maximum speed limit (can be changed by speed zones).</summary>
        public float MaxSpeed
        {
            get => maxSpeed;
            set => maxSpeed = Mathf.Max(0f, value);
        }

        /// <summary>True if the left turn signal is currently active.</summary>
        public bool LeftSignalActive { get; private set; }

        /// <summary>True if the right turn signal is currently active.</summary>
        public bool RightSignalActive { get; private set; }

        /// <summary>Event invoked when a turn signal is toggled. Parameter: +1 right, -1 left, 0 off.</summary>
        public event System.Action<int> TurnSignalChanged;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        private void Update()
        {
            moveInput = ReadHorizontalInput();
            verticalInput = allowVerticalMovement ? ReadVerticalInput() : 0f;
            ReadTurnSignalInput();
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            if (moveInput > 0f)
            {
                currentSpeed += acceleration * Time.fixedDeltaTime;
            }
            else if (moveInput < 0f)
            {
                currentSpeed -= acceleration * Time.fixedDeltaTime;
            }
            else
            {
                if (currentSpeed > 0f)
                {
                    currentSpeed -= deceleration * Time.fixedDeltaTime;
                    if (currentSpeed < 0f) currentSpeed = 0f;
                }
                else if (currentSpeed < 0f)
                {
                    currentSpeed += deceleration * Time.fixedDeltaTime;
                    if (currentSpeed > 0f) currentSpeed = 0f;
                }
            }

            currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed / 2f, maxSpeed);

            float vy = verticalInput * verticalSpeed;
            body.linearVelocity = new Vector2(currentSpeed, vy);
        }

        /// <summary>Cancels all turn signals.</summary>
        public void CancelSignals()
        {
            if (LeftSignalActive || RightSignalActive)
            {
                LeftSignalActive = false;
                RightSignalActive = false;
                TurnSignalChanged?.Invoke(0);
            }
        }

        private static float ReadHorizontalInput()
        {
            if (Keyboard.current == null) return 0f;
            
            float input = 0f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input -= 1f;
            return input;
        }

        private static float ReadVerticalInput()
        {
            if (Keyboard.current == null) return 0f;

            float input = 0f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input -= 1f;
            return input;
        }

        private void ReadTurnSignalInput()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                LeftSignalActive = !LeftSignalActive;
                if (LeftSignalActive) RightSignalActive = false;
                TurnSignalChanged?.Invoke(LeftSignalActive ? -1 : 0);
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                RightSignalActive = !RightSignalActive;
                if (RightSignalActive) LeftSignalActive = false;
                TurnSignalChanged?.Invoke(RightSignalActive ? 1 : 0);
            }
        }
    }
}
