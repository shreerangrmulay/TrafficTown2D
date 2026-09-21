using UnityEngine;

namespace TrafficTown2D.Level3
{
    [RequireComponent(typeof(Camera))]
    public class TopDownCameraFollow : MonoBehaviour
    {
        [Header("Target & Offset")]
        [SerializeField] private Transform target;
        [SerializeField] private float forwardLeadDistance = 2.8f;   // Offsets camera ahead of car so car stays ~45% from bottom
        [SerializeField] private float lateralLimit = 1.8f;           // Restricts horizontal camera drift
        [SerializeField] private float smoothTime = 0.16f;

        [Header("Camera Bounds")]
        [SerializeField] private float minY = -6f;
        [SerializeField] private float maxY = 165f;

        private Vector3 currentVelocity;
        private float shakeTimer;
        private float shakeIntensity;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetBounds(float min, float max)
        {
            minY = min;
            maxY = max;
        }

        public void TriggerShake(float intensity = 0.25f, float duration = 0.35f)
        {
            shakeIntensity = intensity;
            shakeTimer = duration;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Target position: player position + lead offset forward
            float targetX = Mathf.Clamp(target.position.x * 0.4f, -lateralLimit, lateralLimit);
            float targetY = Mathf.Clamp(target.position.y + forwardLeadDistance, minY, maxY);
            Vector3 desiredPosition = new Vector3(targetX, targetY, -10f);

            // Smooth damping
            Vector3 smoothed = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);

            // Apply gentle shake offset if active
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                Vector3 shakeOffset = Random.insideUnitCircle * shakeIntensity;
                smoothed += new Vector3(shakeOffset.x, shakeOffset.y, 0f);
            }

            transform.position = smoothed;
        }
    }
}
