using UnityEngine;

namespace TrafficTown2D.Level6
{
    public class RoadClosureBarrier : EnvironmentalHazard
    {
        [Header("Barrier Visuals")]
        [SerializeField] private SpriteRenderer[] warningLights;
        [SerializeField] private float blinkInterval = 0.5f;

        private float blinkTimer = 0f;
        private bool isLightOn = true;

        protected override void Update()
        {
            base.Update();

            if (warningLights != null && warningLights.Length > 0)
            {
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= blinkInterval)
                {
                    blinkTimer = 0f;
                    isLightOn = !isLightOn;
                    for (int i = 0; i < warningLights.Length; i++)
                    {
                        if (warningLights[i] != null)
                        {
                            warningLights[i].color = isLightOn ? new Color(1f, 0.65f, 0.1f, 1f) : new Color(0.3f, 0.2f, 0.05f, 0.5f);
                        }
                    }
                }
            }
        }

        protected override void OnCarEntered(Level6PlayerCar car)
        {
            if (Level6SafetyManager.Instance != null)
            {
                Level6SafetyManager.Instance.RegisterHazardMistake("Ignored road closure barrier! Route is inaccessible.");
            }
        }

        protected override void OnCarExited(Level6PlayerCar car)
        {
        }
    }
}
