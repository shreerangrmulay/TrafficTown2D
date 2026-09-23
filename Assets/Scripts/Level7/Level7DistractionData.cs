using System;
using UnityEngine;

namespace TrafficTown2D.Level7
{
    public enum DistractionType
    {
        PhoneMessage,
        Notification,
        MusicChange,
        NavigationUpdate,
        PassengerInteraction
    }

    [System.Serializable]
    public class DistractionEventData
    {
        public string id;
        public DistractionType type;
        public string headerTitle;
        public string messageText;
        public string iconGlyph;
        public string actionButtonLabel;
        public string ignoreButtonLabel;
        public float displayDuration = 6.0f;
        public float safetyPenalty = 15f;
        public float focusPenalty = 20f;
        public int scorePenalty = 50;
        public int ignoreRewardScore = 10;
        public float ignoreRecoveryFocus = 2f;
        public int safeStopRewardScore = 20;
        public float safeStopRewardFocus = 5f;

        public static DistractionEventData CreateDefault(DistractionType type)
        {
            switch (type)
            {
                case DistractionType.PhoneMessage:
                    return new DistractionEventData
                    {
                        id = "phone_msg_" + Guid.NewGuid().ToString().Substring(0, 5),
                        type = DistractionType.PhoneMessage,
                        headerTitle = "[MESSAGE] NEW TEXT",
                        messageText = "\"Hey! Are you on your way?\"",
                        iconGlyph = "MSG",
                        actionButtonLabel = "OPEN",
                        ignoreButtonLabel = "IGNORE",
                        displayDuration = 6.0f,
                        safetyPenalty = 18f,
                        focusPenalty = 20f,
                        scorePenalty = 50
                    };

                case DistractionType.Notification:
                    return new DistractionEventData
                    {
                        id = "notif_" + Guid.NewGuid().ToString().Substring(0, 5),
                        type = DistractionType.Notification,
                        headerTitle = "[ALERT] NOTIFICATION",
                        messageText = "Social: 3 new photo likes",
                        iconGlyph = "NOTIF",
                        actionButtonLabel = "VIEW",
                        ignoreButtonLabel = "IGNORE",
                        displayDuration = 5.5f,
                        safetyPenalty = 14f,
                        focusPenalty = 15f,
                        scorePenalty = 40
                    };

                case DistractionType.MusicChange:
                    return new DistractionEventData
                    {
                        id = "music_" + Guid.NewGuid().ToString().Substring(0, 5),
                        type = DistractionType.MusicChange,
                        headerTitle = "[MUSIC] TRACK FINISHED",
                        messageText = "Track: Upbeat Drive - Choose next?",
                        iconGlyph = "AUDIO",
                        actionButtonLabel = "CHANGE",
                        ignoreButtonLabel = "KEEP",
                        displayDuration = 6.0f,
                        safetyPenalty = 12f,
                        focusPenalty = 15f,
                        scorePenalty = 35
                    };

                case DistractionType.NavigationUpdate:
                    return new DistractionEventData
                    {
                        id = "nav_" + Guid.NewGuid().ToString().Substring(0, 5),
                        type = DistractionType.NavigationUpdate,
                        headerTitle = "[GPS] REROUTE ALERT",
                        messageText = "Heavy traffic detected ahead! Reroute?",
                        iconGlyph = "GPS",
                        actionButtonLabel = "VIEW MAP",
                        ignoreButtonLabel = "IGNORE",
                        displayDuration = 6.5f,
                        safetyPenalty = 16f,
                        focusPenalty = 18f,
                        scorePenalty = 45
                    };

                case DistractionType.PassengerInteraction:
                default:
                    return new DistractionEventData
                    {
                        id = "passenger_" + Guid.NewGuid().ToString().Substring(0, 5),
                        type = DistractionType.PassengerInteraction,
                        headerTitle = "[PASSENGER] TALKING",
                        messageText = "\"Hey! Look at this funny clip on my phone!\"",
                        iconGlyph = "PASS",
                        actionButtonLabel = "LOOK",
                        ignoreButtonLabel = "IGNORE",
                        displayDuration = 6.0f,
                        safetyPenalty = 15f,
                        focusPenalty = 16f,
                        scorePenalty = 40
                    };
            }
        }
    }
}
