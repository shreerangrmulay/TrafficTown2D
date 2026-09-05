using UnityEngine;

namespace TrafficTown2D.Visuals
{
    public sealed class PlayerVisualAnimator : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D observedBody;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float movingThreshold = 0.03f;

        private static readonly int Moving = Animator.StringToHash("Moving");

        private void Awake()
        {
            if (observedBody == null) observedBody = GetComponent<Rigidbody2D>();
            if (animator == null) animator = GetComponent<Animator>();
        }

        private void Update()
        {
            if (observedBody == null || animator == null)
            {
                return;
            }

            bool moving = observedBody.linearVelocity.sqrMagnitude > movingThreshold * movingThreshold;
            animator.SetBool(Moving, moving);
        }
    }
}
