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

        private Rigidbody2D body;
        private float currentSpeed;
        private float moveInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        private void Update()
        {
            moveInput = ReadMovementInput();
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
            
            // Assume the car travels horizontally (along X axis) based on initial rotation or just rightwards
            body.linearVelocity = new Vector2(currentSpeed, 0f);
        }

        private static float ReadMovementInput()
        {
            if (Keyboard.current == null) return 0f;
            
            float input = 0f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input -= 1f;
            return input;
        }
    }
}
