using UnityEngine;
using TrafficTown2D.Level3;

namespace TrafficTown2D.Level4
{
    public class RainController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ParticleSystem rainParticleSystem;
        [SerializeField] private NightVisibilityController visibilityController;
        [SerializeField] private Level3PlayerCar playerCar;
        [SerializeField] private AudioSource rainAudioSource;

        [Header("Rain Settings")]
        [SerializeField] private bool autoStartRain = false;
        [SerializeField] private float wetRoadBrakingRate = 5.2f;
        [SerializeField] private float normalBrakingRate = 8.0f;

        [Header("Rain Visuals")]
        [SerializeField] private Color rainColorMin = new Color(0.65f, 0.76f, 0.88f, 0.60f); // Bluish-grey
        [SerializeField] private Color rainColorMax = new Color(0.78f, 0.86f, 0.95f, 0.80f); // Watery light blue-grey

        private bool isRaining = false;
        public bool IsRaining => isRaining;

        private void Awake()
        {
            EnsureRainVisuals();
        }

        private void Start()
        {
            EnsureRainVisuals();

            if (autoStartRain)
            {
                StartRain();
            }
            else
            {
                StopRain();
            }
        }

        public void EnsureRainVisuals()
        {
            if (rainParticleSystem == null)
            {
                rainParticleSystem = GetComponent<ParticleSystem>();
            }

            if (rainParticleSystem != null)
            {
                var main = rainParticleSystem.main;
                main.startColor = new ParticleSystem.MinMaxGradient(rainColorMin, rainColorMax);

                var psRenderer = rainParticleSystem.GetComponent<ParticleSystemRenderer>();
                if (psRenderer != null)
                {
                    bool needsMaterial = psRenderer.sharedMaterial == null || 
                                          psRenderer.sharedMaterial.shader == null ||
                                          psRenderer.sharedMaterial.shader.name.Contains("InternalError") ||
                                          psRenderer.sharedMaterial.shader.name.Contains("Error");

                    if (needsMaterial)
                    {
                        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                        if (shader == null) shader = Shader.Find("Sprites/Default");
                        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");

                        if (shader != null)
                        {
                            Material mat = new Material(shader);
                            mat.name = "RuntimeRainMaterial";
                            mat.color = new Color(0.72f, 0.82f, 0.94f, 0.75f);
                            psRenderer.material = mat;
                        }
                    }

                    psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                    psRenderer.velocityScale = 0.06f;
                    psRenderer.lengthScale = 1.6f;
                }
            }
        }

        public void StartRain()
        {
            EnsureRainVisuals();
            isRaining = true;

            if (rainParticleSystem != null)
            {
                var emission = rainParticleSystem.emission;
                emission.enabled = true;
                if (!rainParticleSystem.isPlaying)
                {
                    rainParticleSystem.Play();
                }
            }

            if (visibilityController != null)
            {
                visibilityController.SetRaining(true);
            }

            if (playerCar != null)
            {
                playerCar.BrakingRate = wetRoadBrakingRate;
            }

            if (rainAudioSource != null)
            {
                if (!rainAudioSource.isPlaying)
                {
                    rainAudioSource.loop = true;
                    rainAudioSource.Play();
                }
            }
        }

        public void StopRain()
        {
            isRaining = false;

            if (rainParticleSystem != null)
            {
                var emission = rainParticleSystem.emission;
                emission.enabled = false;
            }

            if (visibilityController != null)
            {
                visibilityController.SetRaining(false);
            }

            if (playerCar != null)
            {
                playerCar.BrakingRate = normalBrakingRate;
            }

            if (rainAudioSource != null && rainAudioSource.isPlaying)
            {
                rainAudioSource.Stop();
            }
        }
    }
}
