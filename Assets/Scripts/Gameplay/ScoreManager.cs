using System;
using UnityEngine;

namespace TrafficTown2D.Gameplay
{
    public sealed class ScoreManager : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingScore = 100;
        [SerializeField] private int waitReward = 20;
        [SerializeField] private int crossingReward = 20;
        [SerializeField] private int completionReward = 30;
        [SerializeField] private int unsafePenalty = 20;
        [SerializeField] private int collisionPenalty = 20;

        public int CurrentScore { get; private set; }
        public int SafeActions { get; private set; }
        public int Mistakes { get; private set; }
        public event Action<int> ScoreChanged;

        private void Awake() => CurrentScore = startingScore;
        public void RewardWait() => ChangeScore(waitReward, true);
        public void RewardCrossing() => ChangeScore(crossingReward, true);
        public void RewardCompletion() => ChangeScore(completionReward, true);
        public void PenalizeUnsafe() => ChangeScore(-unsafePenalty, false);
        public void PenalizeCollision() => ChangeScore(-collisionPenalty, false);
        public void RewardSafeAction(int amount) => ChangeScore(Mathf.Max(0, amount), true);
        public void PenalizeMistake(int penalty) => ChangeScore(-Mathf.Max(0, penalty), false);

        private void ChangeScore(int amount, bool safe)
        {
            int previousScore = CurrentScore;
            CurrentScore = Mathf.Max(0, CurrentScore + amount);
            if (safe) SafeActions++; else Mistakes++;
            if (CurrentScore != previousScore)
            {
                ScoreChanged?.Invoke(CurrentScore);
            }
        }
    }
}
