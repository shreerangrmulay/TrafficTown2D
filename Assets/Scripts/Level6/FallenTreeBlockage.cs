using UnityEngine;

namespace TrafficTown2D.Level6
{
    public class FallenTreeBlockage : EnvironmentalHazard
    {
        [Header("Blockage Details")]
        [SerializeField] private Collider2D treeSolidCollider;

        protected override void OnCarEntered(Level6PlayerCar car)
        {
            if (Level6SafetyManager.Instance != null)
            {
                Level6SafetyManager.Instance.RegisterHazardMistake("Road completely blocked by fallen tree! Turn around safely.");
            }
        }

        protected override void OnCarExited(Level6PlayerCar car)
        {
        }
    }
}
