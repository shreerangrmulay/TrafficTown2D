using UnityEngine;

namespace TrafficTown2D.Level6
{
    public class DebrisHazard : EnvironmentalHazard
    {
        protected override void OnCarEntered(Level6PlayerCar car)
        {
            if (Level6SafetyManager.Instance != null)
            {
                Level6SafetyManager.Instance.RegisterMinorHazardContact("Drove over road debris! Slow down and steer around obstacles.");
            }
        }

        protected override void OnCarExited(Level6PlayerCar car)
        {
        }
    }
}
