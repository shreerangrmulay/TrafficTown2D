using UnityEngine;

namespace TrafficTown2D.Level4
{
    public class ReflectiveElement : MonoBehaviour
    {
        [Header("Renderers")]
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Color baseColor = new Color(0.7f, 0.7f, 0.7f, 0.85f);
        [SerializeField] private Color reflectiveGlowColor = new Color(1.3f, 1.3f, 1.3f, 1f);

        [Header("Reflective Properties")]
        [SerializeField] private float glowTransitionSpeed = 8f;
        [SerializeField] private bool autoFindPlayer = true;
        [SerializeField] private PlayerHeadlightController playerHeadlights;

        private Color currentColor;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<SpriteRenderer>();
            }
            if (targetRenderer != null)
            {
                currentColor = targetRenderer.color;
            }
        }

        private void Start()
        {
            if (autoFindPlayer && playerHeadlights == null)
            {
                playerHeadlights = UnityEngine.Object.FindAnyObjectByType<PlayerHeadlightController>();
            }
        }

        public void SetPlayerHeadlights(PlayerHeadlightController headlights)
        {
            playerHeadlights = headlights;
        }

        private void Update()
        {
            if (targetRenderer == null) return;

            bool isIlluminated = false;
            if (playerHeadlights != null && playerHeadlights.IsOn)
            {
                isIlluminated = playerHeadlights.IsIlluminating(transform.position);
            }

            Color targetColor = isIlluminated ? reflectiveGlowColor : baseColor;
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * glowTransitionSpeed);
            targetRenderer.color = currentColor;
        }
    }
}
