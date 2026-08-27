using System;

namespace TrafficTown2D.Data
{
    [Serializable]
    public sealed class TrafficRuleData
    {
        public string ruleId;
        public string title;
        public string description;
    }

    [Serializable]
    public sealed class MissionData
    {
        public string missionId;
        public string description;
        public TrafficRuleData trafficRule;
        public int reward;
    }

    [Serializable]
    public sealed class LevelData
    {
        public string levelId;
        public string levelName;
        public string difficulty;
        public MissionData[] missions;
    }

    [Serializable]
    public sealed class PlayerProgressData
    {
        public int totalScore;
        public string[] completedMissionIds;
        public string lastUnlockedLevelId;
    }
}