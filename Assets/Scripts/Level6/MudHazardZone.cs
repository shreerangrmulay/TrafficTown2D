using UnityEngine;

namespace TrafficTown2D.Level6
{
    public class MudHazardZone : EnvironmentalHazard
    {
        [Header("Mud Properties")]
        [SerializeField] private ParticleSystem mudParticles;
        [SerializeField] private AudioSource mudAudio;

        protected override void OnCarEntered(Level6PlayerCar car)
        {
            car.SetTerrainConditions(flooded: false, muddy: true, offRoad: false);
            if (mudParticles != null) mudParticles.Play();
            if (mudAudio != null && !mudAudio.isPlaying) mudAudio.Play();

            if (Level6SafetyManager.Instance != null)
            {
                Level6SafetyManager.Instance.RegisterHazardMistake("Mud section entered! Reduce speed to maintain traction.");
            }
        }

        protected override void OnCarExited(Level6PlayerCar car)
        {
            car.SetTerrainConditions(flooded: false, muddy: false, offRoad: false);
            if (mudParticles != null) mudParticles.Stop();
            if (mudAudio != null && mudAudio.isPlaying) mudAudio.Stop();
        }
    }
}
