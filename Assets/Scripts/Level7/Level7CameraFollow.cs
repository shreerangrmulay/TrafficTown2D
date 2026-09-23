using UnityEngine;

namespace TrafficTown2D.Level7
{
    [RequireComponent(typeof(Camera))]
    public class Level7CameraFollow : MonoBehaviour
    {
        [Header("Target & Lead")]
        [SerializeField] private Transform target;
        [SerializeField] private float leadDistance = 3.8f;
        [SerializeField] private float smoothTime = 0.22f;

        [Header("2D Boundaries")]
        [SerializeField] private float minX = -45f;
        [SerializeField] private float maxX = 45f;
        [SerializeField] private float minY = -30f;
        [SerializeField] private float maxY = 30f;

        private Vector3 currentVelocity;
        private float shakeTimer = 0f;
        private float shakeIntensity = 0f;
        private Camera cam;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void TriggerShake(float intensity = 0.25f, float duration = 0.35f)
        {
            shakeIntensity = intensity;
            shakeTimer = duration;
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 8.5f;
            }
        }

        private void Start()
        {
            if (target == null)
            {
                Level7PlayerCar player = FindAnyObjectByType<Level7PlayerCar>();
                if (player != null) target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 forwardOffset = target.up * leadDistance;
            float desiredX = Mathf.Clamp(target.position.x + forwardOffset.x, minX, maxX);
            float desiredY = Mathf.Clamp(target.position.y + forwardOffset.y, minY, maxY);
            Vector3 targetPos = new Vector3(desiredX, desiredY, -10f);

            Vector3 smoothed = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);

            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                smoothed.x += UnityEngine.Random.Range(-shakeIntensity, shakeIntensity);
                smoothed.y += UnityEngine.Random.Range(-shakeIntensity, shakeIntensity);
            }

            transform.position = smoothed;
        }
    }
}
