using UnityEngine;

namespace TrafficTown2D.Level9
{
    /// <summary>
    /// Smooth 2D camera follow system tailored for Level 9's multi-roundabout network.
    /// Follows the player car with forward lead distance and bounds clamping.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class Level9CameraFollow : MonoBehaviour
    {
        [Header("Target & Lead")]
        [SerializeField] private Transform target;
        [SerializeField] private float leadDistance = 3.2f;
        [SerializeField] private float smoothTime = 0.18f;

        [Header("World Bounds")]
        [SerializeField] private float minX = -18f;
        [SerializeField] private float maxX = 58f;
        [SerializeField] private float minY = -25f;
        [SerializeField] private float maxY = 105f;

        private Vector3 currentVelocity;
        private float shakeTimer = 0f;
        private float shakeIntensity = 0f;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetBounds(float xMin, float xMax, float yMin, float yMax)
        {
            minX = xMin;
            maxX = xMax;
            minY = yMin;
            maxY = yMax;
        }

        public void TriggerShake(float intensity = 0.25f, float duration = 0.35f)
        {
            shakeIntensity = intensity;
            shakeTimer = duration;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Compute lead offset in player car's forward direction
            Vector3 forwardOffset = target.up * leadDistance;
            float desiredX = Mathf.Clamp(target.position.x + forwardOffset.x, minX, maxX);
            float desiredY = Mathf.Clamp(target.position.y + forwardOffset.y, minY, maxY);
            Vector3 targetPos = new Vector3(desiredX, desiredY, -10f);

            // Smooth damping
            Vector3 smoothed = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);

            // Screen shake
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                Vector2 shake = Random.insideUnitCircle * shakeIntensity;
                smoothed += new Vector3(shake.x, shake.y, 0f);
            }

            transform.position = smoothed;
        }
    }
}
