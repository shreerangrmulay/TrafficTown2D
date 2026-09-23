using UnityEngine;

namespace TrafficTown2D.Level6
{
    [RequireComponent(typeof(Camera))]
    public class Level6CameraFollow : MonoBehaviour
    {
        [Header("Target & Lead")]
        [SerializeField] private Transform target;
        [SerializeField] private float leadDistance = 3.5f;
        [SerializeField] private float smoothTime = 0.20f;

        [Header("2D Boundaries")]
        [SerializeField] private float minX = -75f;
        [SerializeField] private float maxX = 75f;
        [SerializeField] private float minY = -55f;
        [SerializeField] private float maxY = 55f;

        private Vector3 currentVelocity;
        private float shakeTimer = 0f;
        private float shakeIntensity = 0f;
        private Camera cam;

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

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Compute desired position with forward velocity lead
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
