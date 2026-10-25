using UnityEngine;

namespace TrafficTown2D.UI
{
    /// <summary>
    /// Centralized color palette for TrafficTown 2D children's road safety game (ages 5-10).
    /// Flat, bright, high-contrast, strictly no gradients, glows, or drop shadows.
    /// Every UI element in the Main Menu and Level Select reads directly from this class.
    /// </summary>
    public static class ThemeColors
    {
        // -------------------------------------------------------------
        // Core Palette
        // -------------------------------------------------------------
        /// <summary>Primary bright yellow used for backgrounds and canvas backplates (#FFF21A).</summary>
        public static readonly Color PrimaryYellow = new Color32(0xFF, 0xF2, 0x1A, 0xFF);

        /// <summary>Ink black used for typography, road surface, and high-contrast outlines (#171717).</summary>
        public static readonly Color InkBlack = new Color32(0x17, 0x17, 0x17, 0xFF);

        /// <summary>Vibrant accent purple used for buttons, interactive highlights, and progress (#A66BFF).</summary>
        public static readonly Color AccentPurple = new Color32(0xA6, 0x6B, 0xFF, 0xFF);

        /// <summary>Pressed state for purple buttons (slightly deeper purple).</summary>
        public static readonly Color AccentPurpleDark = new Color32(0x8C, 0x4D, 0xEE, 0xFF);

        /// <summary>Hover / focused state for purple buttons (slightly lighter purple).</summary>
        public static readonly Color AccentPurpleLight = new Color32(0xB7, 0x82, 0xFF, 0xFF);

        // -------------------------------------------------------------
        // Traffic Light Decoration Colors (ONLY used on traffic light icon/widget)
        // -------------------------------------------------------------
        public static readonly Color TrafficRed = new Color32(0xEE, 0x33, 0x33, 0xFF);
        public static readonly Color TrafficAmber = new Color32(0xFF, 0xAA, 0x00, 0xFF);
        public static readonly Color TrafficGreen = new Color32(0x22, 0xCC, 0x44, 0xFF);

        // -------------------------------------------------------------
        // Supporting UI & Map States
        // -------------------------------------------------------------
        /// <summary>Locked node / barrier disabled grey (#8E8E93).</summary>
        public static readonly Color LockedGrey = new Color32(0x8E, 0x8E, 0x93, 0xFF);

        /// <summary>Dark slate locked node inner icon tint (#4A4A4F).</summary>
        public static readonly Color LockedDark = new Color32(0x4A, 0x4A, 0x4F, 0xFF);

        /// <summary>Pure white for high-contrast labels on purple/black surfaces.</summary>
        public static readonly Color White = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        /// <summary>Gold star fill color (#FFD200).</summary>
        public static readonly Color StarFilled = new Color32(0xFF, 0xD2, 0x00, 0xFF);

        /// <summary>Empty star outline/faint fill color (#171717 with alpha, or dark line).</summary>
        public static readonly Color StarEmpty = new Color32(0x17, 0x17, 0x17, 0x40);

        /// <summary>Road dashed yellow lane marking color (#FFF21A).</summary>
        public static readonly Color RoadDashedYellow = new Color32(0xFF, 0xF2, 0x1A, 0xFF);
    }
}
