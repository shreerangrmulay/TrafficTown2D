using UnityEngine;

namespace TrafficTown2D.UI
{
    [CreateAssetMenu(fileName = "TrafficTownTheme", menuName = "TrafficTown/Theme", order = 1)]
    public class TrafficTownTheme : ScriptableObject
    {
        private static TrafficTownTheme instance;

        public static TrafficTownTheme Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<TrafficTownTheme>("TrafficTownTheme");
                    if (instance == null)
                    {
                        instance = CreateInstance<TrafficTownTheme>();
                    }
                }
                return instance;
            }
        }

        [Header("Brand & Primary Colors")]
        [SerializeField] private Color primaryColor = new Color(0.12f, 0.55f, 0.84f, 1f);        // TrafficTown Blue (#1E8CD6)
        [SerializeField] private Color secondaryColor = new Color(0.16f, 0.70f, 0.32f, 1f);      // Emerald Green (#29B352)
        [SerializeField] private Color accentColor = new Color(0.98f, 0.76f, 0.18f, 1f);         // Gold / Yellow (#FAC22E)
        [SerializeField] private Color backgroundColor = new Color(0.92f, 0.95f, 0.97f, 1f);     // Light Slate Canvas

        [Header("Cards & Panels")]
        [SerializeField] private Color cardDarkColor = new Color(0.08f, 0.12f, 0.18f, 0.92f);    // Dark Glass HUD
        [SerializeField] private Color cardLightColor = new Color(0.98f, 0.99f, 1.0f, 0.98f);    // Clean Light Modal
        [SerializeField] private Color cardBorderColor = new Color(0.20f, 0.30f, 0.45f, 0.40f);  // Subtle Glass Border

        [Header("Typography")]
        [SerializeField] private Color textPrimaryColor = new Color(1f, 1f, 1f, 1f);             // High Contrast White
        [SerializeField] private Color textDarkColor = new Color(0.10f, 0.15f, 0.22f, 1f);       // Deep Charcoal Navy
        [SerializeField] private Color textSecondaryColor = new Color(0.70f, 0.78f, 0.88f, 1f);  // Muted Slate Blue

        [Header("Status & Feedback")]
        [SerializeField] private Color successColor = new Color(0.16f, 0.72f, 0.36f, 1f);        // Vibrant Green
        [SerializeField] private Color warningColor = new Color(0.95f, 0.55f, 0.15f, 1f);        // Amber Orange
        [SerializeField] private Color dangerColor = new Color(0.90f, 0.22f, 0.20f, 1f);         // Coral Red

        [Header("Environment")]
        [SerializeField] private Color roadColor = new Color(0.14f, 0.15f, 0.18f, 1f);           // Dark Asphalt
        [SerializeField] private Color roadMarkingColor = new Color(1f, 0.95f, 0.45f, 1f);       // Road Yellow
        [SerializeField] private Color roadStripeColor = new Color(0.95f, 0.95f, 0.95f, 1f);     // Road White Stripe
        [SerializeField] private Color sidewalkColor = new Color(0.72f, 0.76f, 0.74f, 1f);       // Sidewalk Gray
        [SerializeField] private Color sidewalkCurbColor = new Color(0.55f, 0.58f, 0.56f, 1f);   // Curb Edge
        [SerializeField] private Color grassColor = new Color(0.40f, 0.72f, 0.38f, 1f);          // Park Grass

        [Header("Buttons")]
        [SerializeField] private Color buttonPrimaryColor = new Color(0.12f, 0.55f, 0.84f, 1f);   // Blue Button
        [SerializeField] private Color buttonHoverColor = new Color(0.18f, 0.65f, 0.96f, 1f);     // Light Blue Hover
        [SerializeField] private Color buttonPressedColor = new Color(0.09f, 0.44f, 0.70f, 1f);   // Deep Blue Pressed
        [SerializeField] private Color buttonSuccessColor = new Color(0.16f, 0.70f, 0.32f, 1f);   // Green Button
        [SerializeField] private Color buttonDangerColor = new Color(0.86f, 0.22f, 0.18f, 1f);    // Red Button

        [Header("Metrics")]
        [SerializeField] private float cardCornerRadius = 24f;
        [SerializeField] private float buttonCornerRadius = 18f;
        [SerializeField] private int headerFontSize = 24;
        [SerializeField] private int subheaderFontSize = 18;
        [SerializeField] private int bodyFontSize = 14;

        // Static Property Accessors
        public static Color PrimaryColor => Instance.primaryColor;
        public static Color SecondaryColor => Instance.secondaryColor;
        public static Color AccentColor => Instance.accentColor;
        public static Color BackgroundColor => Instance.backgroundColor;

        public static Color CardDarkColor => Instance.cardDarkColor;
        public static Color CardLightColor => Instance.cardLightColor;
        public static Color CardBorderColor => Instance.cardBorderColor;

        public static Color TextPrimaryColor => Instance.textPrimaryColor;
        public static Color TextDarkColor => Instance.textDarkColor;
        public static Color TextSecondaryColor => Instance.textSecondaryColor;

        public static Color SuccessColor => Instance.successColor;
        public static Color WarningColor => Instance.warningColor;
        public static Color DangerColor => Instance.dangerColor;

        public static Color RoadColor => Instance.roadColor;
        public static Color RoadMarkingColor => Instance.roadMarkingColor;
        public static Color RoadStripeColor => Instance.roadStripeColor;
        public static Color SidewalkColor => Instance.sidewalkColor;
        public static Color SidewalkCurbColor => Instance.sidewalkCurbColor;
        public static Color GrassColor => Instance.grassColor;

        public static Color ButtonPrimaryColor => Instance.buttonPrimaryColor;
        public static Color ButtonHoverColor => Instance.buttonHoverColor;
        public static Color ButtonPressedColor => Instance.buttonPressedColor;
        public static Color ButtonSuccessColor => Instance.buttonSuccessColor;
        public static Color ButtonDangerColor => Instance.buttonDangerColor;

        public static float CardCornerRadius => Instance.cardCornerRadius;
        public static float ButtonCornerRadius => Instance.buttonCornerRadius;
        public static int HeaderFontSize => Instance.headerFontSize;
        public static int SubheaderFontSize => Instance.subheaderFontSize;
        public static int BodyFontSize => Instance.bodyFontSize;
    }
}
