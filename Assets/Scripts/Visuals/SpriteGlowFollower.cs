using UnityEngine;

namespace TrafficTown2D.Visuals
{
    public sealed class SpriteGlowFollower : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer source;
        [SerializeField] private SpriteRenderer glow;
        [SerializeField, Range(0f, 1f)] private float activeAlpha = 0.28f;

        private void Awake()
        {
            if (source == null) source = GetComponent<SpriteRenderer>();
            if (glow == null) glow = transform.Find("Glow")?.GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (source == null || glow == null)
            {
                return;
            }

            Color sourceColor = source.color;
            glow.color = new Color(sourceColor.r, sourceColor.g, sourceColor.b, sourceColor.a > 0.5f ? activeAlpha : 0f);
        }
    }
}
