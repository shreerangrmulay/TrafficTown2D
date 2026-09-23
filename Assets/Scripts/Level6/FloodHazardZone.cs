using UnityEngine;

namespace TrafficTown2D.Level6
{
    public class FloodHazardZone : EnvironmentalHazard
    {
        [Header("Flood Properties")]
        [SerializeField] private float waterDragMultiplier = 3.5f;
        [SerializeField] private ParticleSystem splashParticles;
        [SerializeField] private AudioSource splashAudio;

        protected override void OnCarEntered(Level6PlayerCar car)
        {
            car.SetTerrainConditions(flooded: true, muddy: false, offRoad: false);
            if (splashParticles != null) splashParticles.Play();
            if (splashAudio != null && !splashAudio.isPlaying) splashAudio.Play();

            if (Level6SafetyManager.Instance != null)
            {
                Level6SafetyManager.Instance.RegisterHazardMistake("Entered deep flood water! Severe risk of hydroplaning.");
            }
        }

        protected override void OnCarExited(Level6PlayerCar car)
        {
            car.SetTerrainConditions(flooded: false, muddy: false, offRoad: false);
            if (splashParticles != null) splashParticles.Stop();
            if (splashAudio != null && splashAudio.isPlaying) splashAudio.Stop();
        }
    }
}
