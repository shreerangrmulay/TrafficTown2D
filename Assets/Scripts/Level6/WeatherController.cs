using System;
using System.Collections;
using UnityEngine;

namespace TrafficTown2D.Level6
{
    public enum WeatherType
    {
        Clear,
        Rain,
        HeavyRain,
        Fog,
        Wind,
        Storm
    }

    public class WeatherController : MonoBehaviour
    {
        public static WeatherController Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Level6PlayerCar playerCar;
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private ParticleSystem windParticles;
        [SerializeField] private SpriteRenderer fogOverlay;
        [SerializeField] private AudioSource rainAudio;
        [SerializeField] private AudioSource windAudio;

        [Header("Current Conditions")]
        [SerializeField] private WeatherType currentWeather = WeatherType.Clear;
        [SerializeField, Range(0.2f, 1f)] private float roadGrip = 1.0f;
        [SerializeField, Range(0.2f, 1f)] private float visibility = 1.0f;
        [SerializeField] private Vector2 activeWindForce = Vector2.zero;

        public WeatherType CurrentWeather => currentWeather;
        public float RoadGrip => roadGrip;
        public float Visibility => visibility;
        public Vector2 ActiveWindForce => activeWindForce;
        public Vector2 CurrentWind => activeWindForce;

        public event Action<WeatherType, float, float> WeatherConditionChanged;
        public event Action<WeatherType> WeatherChanged;

        [SerializeField] private UnityEngine.Rendering.Universal.Light2D globalLight;

        public void BindGlobalLight(UnityEngine.Rendering.Universal.Light2D light) => globalLight = light;
        public void BindRainParticles(ParticleSystem ps) => rainParticles = ps;
        public void BindFogOverlay(SpriteRenderer sr) => fogOverlay = sr;
        public void BindWindParticles(ParticleSystem ps) => windParticles = ps;

        private Coroutine transitionRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (playerCar == null)
            {
                SetPlayerCar(FindFirstObjectByType<Level6PlayerCar>());
            }
            ApplyWeatherImmediate(currentWeather);
        }

        public void SetPlayerCar(Level6PlayerCar car)
        {
            playerCar = car;
            if (playerCar != null)
            {
                playerCar.SetGripModifier(roadGrip);
                playerCar.SetWindVelocity(activeWindForce);
            }
        }

        public void SetWeather(WeatherType newWeather, float transitionTime = 2.5f)
        {
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(TransitionWeatherRoutine(newWeather, transitionTime));
        }

        private IEnumerator TransitionWeatherRoutine(WeatherType targetType, float duration)
        {
            WeatherType oldType = currentWeather;
            currentWeather = targetType;

            float targetGrip = GetTargetGrip(targetType);
            float targetVis = GetTargetVisibility(targetType);
            Vector2 targetWind = GetTargetWind(targetType);

            float startGrip = roadGrip;
            float startVis = visibility;
            Vector2 startWind = activeWindForce;

            float startRainVol = rainAudio != null ? rainAudio.volume : 0f;
            float targetRainVol = (targetType == WeatherType.Rain || targetType == WeatherType.HeavyRain || targetType == WeatherType.Storm) ? (targetType == WeatherType.HeavyRain ? 0.65f : 0.45f) : 0f;

            float startWindVol = windAudio != null ? windAudio.volume : 0f;
            float targetWindVol = (targetType == WeatherType.Wind || targetType == WeatherType.Storm) ? 0.60f : 0f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                roadGrip = Mathf.Lerp(startGrip, targetGrip, t);
                visibility = Mathf.Lerp(startVis, targetVis, t);
                activeWindForce = Vector2.Lerp(startWind, targetWind, t);

                if (globalLight != null)
                {
                    float targetIntensity = GetTargetLightIntensity(targetType);
                    Color targetColor = GetTargetLightColor(targetType);
                    globalLight.intensity = Mathf.Lerp(globalLight.intensity, targetIntensity, t);
                    globalLight.color = Color.Lerp(globalLight.color, targetColor, t);
                }

                if (fogOverlay != null)
                {
                    float targetFogAlpha = (targetType == WeatherType.Fog) ? 0.22f : (targetType == WeatherType.Storm ? 0.16f : 0f);
                    Color c = new Color(0.72f, 0.78f, 0.85f, targetFogAlpha);
                    c.a = Mathf.Lerp(fogOverlay.color.a, targetFogAlpha, t);
                    fogOverlay.color = c;
                }

                if (rainParticles != null)
                {
                    var emission = rainParticles.emission;
                    float targetEmission = (targetType == WeatherType.HeavyRain || targetType == WeatherType.Storm) ? 140f : (targetType == WeatherType.Rain ? 75f : 0f);
                    emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, targetEmission, t);
                    if (targetEmission > 0f && !rainParticles.isPlaying) rainParticles.Play();
                    else if (targetEmission <= 0f && rainParticles.isPlaying && t > 0.9f) rainParticles.Stop();
                }

                if (windParticles != null)
                {
                    var emission = windParticles.emission;
                    float targetEmission = (targetType == WeatherType.Wind || targetType == WeatherType.Storm) ? 30f : 0f;
                    emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, targetEmission, t);
                    if (targetEmission > 0f && !windParticles.isPlaying) windParticles.Play();
                    else if (targetEmission <= 0f && windParticles.isPlaying && t > 0.9f) windParticles.Stop();
                }

                if (rainAudio != null)
                {
                    rainAudio.volume = Mathf.Lerp(startRainVol, targetRainVol, t);
                    if (targetRainVol > 0f && !rainAudio.isPlaying) rainAudio.Play();
                }

                if (windAudio != null)
                {
                    windAudio.volume = Mathf.Lerp(startWindVol, targetWindVol, t);
                    if (targetWindVol > 0f && !windAudio.isPlaying) windAudio.Play();
                }

                if (playerCar != null)
                {
                    playerCar.SetGripModifier(roadGrip);
                    playerCar.SetWindVelocity(activeWindForce);
                }

                WeatherConditionChanged?.Invoke(currentWeather, roadGrip, visibility);
                yield return null;
            }

            roadGrip = targetGrip;
            visibility = targetVis;
            activeWindForce = targetWind;

            if (playerCar != null)
            {
                playerCar.SetGripModifier(roadGrip);
                playerCar.SetWindVelocity(activeWindForce);
            }

            WeatherConditionChanged?.Invoke(currentWeather, roadGrip, visibility);
            WeatherChanged?.Invoke(currentWeather);
            transitionRoutine = null;
        }

        public void ApplyWeatherImmediate(WeatherType type)
        {
            currentWeather = type;
            roadGrip = GetTargetGrip(type);
            visibility = GetTargetVisibility(type);
            activeWindForce = GetTargetWind(type);

            if (globalLight != null)
            {
                globalLight.intensity = GetTargetLightIntensity(type);
                globalLight.color = GetTargetLightColor(type);
            }

            if (fogOverlay != null)
            {
                float targetFogAlpha = (type == WeatherType.Fog) ? 0.22f : (type == WeatherType.Storm ? 0.16f : 0f);
                fogOverlay.color = new Color(0.72f, 0.78f, 0.85f, targetFogAlpha);
            }

            if (rainParticles != null)
            {
                var emission = rainParticles.emission;
                float rate = (type == WeatherType.HeavyRain || type == WeatherType.Storm) ? 140f : (type == WeatherType.Rain ? 75f : 0f);
                emission.rateOverTime = rate;
                if (rate > 0f) rainParticles.Play();
                else rainParticles.Stop();
            }

            if (windParticles != null)
            {
                var emission = windParticles.emission;
                float rate = (type == WeatherType.Wind || type == WeatherType.Storm) ? 30f : 0f;
                emission.rateOverTime = rate;
                if (rate > 0f) windParticles.Play();
                else windParticles.Stop();
            }

            if (rainAudio != null)
            {
                float vol = (type == WeatherType.HeavyRain || type == WeatherType.Storm) ? 0.65f : (type == WeatherType.Rain ? 0.45f : 0f);
                rainAudio.volume = vol;
                if (vol > 0f && !rainAudio.isPlaying) rainAudio.Play();
                else if (vol == 0f && rainAudio.isPlaying) rainAudio.Stop();
            }

            if (windAudio != null)
            {
                float vol = (type == WeatherType.Wind || type == WeatherType.Storm) ? 0.60f : 0f;
                windAudio.volume = vol;
                if (vol > 0f && !windAudio.isPlaying) windAudio.Play();
                else if (vol == 0f && windAudio.isPlaying) windAudio.Stop();
            }

            if (playerCar != null)
            {
                playerCar.SetGripModifier(roadGrip);
                playerCar.SetWindVelocity(activeWindForce);
            }

            WeatherConditionChanged?.Invoke(currentWeather, roadGrip, visibility);
            WeatherChanged?.Invoke(currentWeather);
        }

        private float GetTargetGrip(WeatherType type)
        {
            switch (type)
            {
                case WeatherType.Rain: return 0.80f;
                case WeatherType.HeavyRain: return 0.70f;
                case WeatherType.Fog: return 0.90f;
                case WeatherType.Wind: return 0.85f;
                case WeatherType.Storm: return 0.65f;
                default: return 1.0f;
            }
        }

        private float GetTargetVisibility(WeatherType type)
        {
            switch (type)
            {
                case WeatherType.Rain: return 0.85f;
                case WeatherType.HeavyRain: return 0.70f;
                case WeatherType.Fog: return 0.60f;
                case WeatherType.Wind: return 0.85f;
                case WeatherType.Storm: return 0.55f;
                default: return 1.0f;
            }
        }

        private Vector2 GetTargetWind(WeatherType type)
        {
            switch (type)
            {
                case WeatherType.Wind:
                    return new Vector2(-2.8f, 0.5f); // Subtle, noticeable westerly crosswind
                case WeatherType.Storm:
                    return new Vector2(-4.0f, -0.8f); // Stronger storm gusts requiring steering correction
                default:
                    return Vector2.zero;
            }
        }

        private float GetTargetLightIntensity(WeatherType type)
        {
            switch (type)
            {
                case WeatherType.Clear: return 0.95f;
                case WeatherType.Rain: return 0.72f;
                case WeatherType.HeavyRain: return 0.62f;
                case WeatherType.Fog: return 0.58f;
                case WeatherType.Wind: return 0.82f;
                case WeatherType.Storm: return 0.48f;
                default: return 0.90f;
            }
        }

        private Color GetTargetLightColor(WeatherType type)
        {
            switch (type)
            {
                case WeatherType.Clear: return new Color(0.96f, 0.96f, 1.0f);
                case WeatherType.Rain: return new Color(0.72f, 0.80f, 0.90f);
                case WeatherType.HeavyRain: return new Color(0.62f, 0.72f, 0.84f);
                case WeatherType.Fog: return new Color(0.74f, 0.80f, 0.86f);
                case WeatherType.Wind: return new Color(0.86f, 0.88f, 0.92f);
                case WeatherType.Storm: return new Color(0.52f, 0.60f, 0.72f);
                default: return Color.white;
            }
        }
    }
}
