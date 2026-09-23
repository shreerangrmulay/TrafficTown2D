using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TrafficTown2D.Core;

namespace TrafficTown2D.Level6
{
    public class Level6UIController : MonoBehaviour
    {
        public static Level6UIController Instance { get; private set; }

        [Header("Mission Card (Top-Left)")]
        [SerializeField] private TMP_Text missionBadgeText;
        [SerializeField] private TMP_Text missionTitleText;
        [SerializeField] private TMP_Text missionObjectiveText;
        [SerializeField] private TMP_Text missionTimerText;
        [SerializeField] private TMP_Text gpsNavigationText;

        [Header("Environmental Status (Top-Center)")]
        [SerializeField] private TMP_Text weatherStatusText;
        [SerializeField] private Image gripBarFill;
        [SerializeField] private TMP_Text gripValueText;
        [SerializeField] private TMP_Text windStatusText;
        [SerializeField] private TMP_Text hazardNoticeText;

        [Header("Safety & Score (Top-Right)")]
        [SerializeField] private TMP_Text scoreValueText;
        [SerializeField] private TMP_Text safetyValueText;
        [SerializeField] private Image safetyMeterFill;
        [SerializeField] private TMP_Text routeSafetyValueText;
        [SerializeField] private Image routeSafetyMeterFill;

        [Header("Speedometer (Bottom-Left)")]
        [SerializeField] private TMP_Text currentSpeedText;
        [SerializeField] private TMP_Text speedLimitBadgeText;
        [SerializeField] private TMP_Text skidWarningText;
        [SerializeField] private TMP_Text offRoadWarningText;

        [Header("Feedback Banner (Bottom-Center)")]
        [SerializeField] private GameObject feedbackBanner;
        [SerializeField] private CanvasGroup feedbackGroup;
        [SerializeField] private TMP_Text feedbackMessageText;

        [Header("Mission Failed Modal")]
        [SerializeField] private GameObject failModal;
        [SerializeField] private TMP_Text failReasonText;
        [SerializeField] private Button failRetryButton;
        [SerializeField] private Button failMenuButton;

        [Header("Level 6 Completion Certificate Modal")]
        [SerializeField] private GameObject completionModal;
        [SerializeField] private TMP_Text certificateTitleText;
        [SerializeField] private TMP_Text certificateSubtitleText;
        [SerializeField] private TMP_Text starRatingText;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text finalSafetyText;
        [SerializeField] private TMP_Text finalRouteSafetyText;
        [SerializeField] private TMP_Text finalCollisionsText;
        [SerializeField] private TMP_Text finalHazardsAvoidedText;
        [SerializeField] private TMP_Text finalUTurnsText;
        [SerializeField] private Button certificateReplayButton;
        [SerializeField] private Button certificateMenuButton;

        [Header("References")]
        [SerializeField] private Level6PlayerCar playerCar;

        private Coroutine feedbackRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (failModal != null) failModal.SetActive(false);
            if (completionModal != null) completionModal.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);
            if (skidWarningText != null) skidWarningText.gameObject.SetActive(false);
            if (offRoadWarningText != null) offRoadWarningText.gameObject.SetActive(false);

            if (failRetryButton != null)
            {
                failRetryButton.onClick.AddListener(OnFailRetryClicked);
            }
            if (failMenuButton != null)
            {
                failMenuButton.onClick.AddListener(OnReturnToMenuClicked);
            }
            if (certificateReplayButton != null)
            {
                certificateReplayButton.onClick.AddListener(OnCertificateReplayClicked);
            }
            if (certificateMenuButton != null)
            {
                certificateMenuButton.onClick.AddListener(OnReturnToMenuClicked);
            }
        }

        private void Start()
        {
            if (skidWarningText != null) skidWarningText.gameObject.SetActive(false);
            if (offRoadWarningText != null) offRoadWarningText.gameObject.SetActive(false);

            if (playerCar == null)
            {
                playerCar = FindFirstObjectByType<Level6PlayerCar>();
            }

            if (playerCar != null)
            {
                playerCar.SpeedChanged += OnSpeedChanged;
                playerCar.GripChanged += OnGripChanged;
                playerCar.SkidStateChanged += OnSkidStateChanged;
                playerCar.OffRoadChanged += OnOffRoadChanged;
            }

            if (Level6SafetyManager.Instance != null)
            {
                Level6SafetyManager.Instance.ScoreChanged += OnScoreChanged;
                Level6SafetyManager.Instance.SafetyScoreChanged += OnSafetyScoreChanged;
                Level6SafetyManager.Instance.RouteSafetyChanged += OnRouteSafetyChanged;
                Level6SafetyManager.Instance.FeedbackTriggered += ShowFeedback;

                // Initial values
                OnScoreChanged(Level6SafetyManager.Instance.TotalScore);
                OnSafetyScoreChanged(Level6SafetyManager.Instance.SafetyScore);
                OnRouteSafetyChanged(Level6SafetyManager.Instance.RouteSafetyScore);
            }

            if (Level6MissionManager.Instance != null)
            {
                Level6MissionManager.Instance.MissionStarted += OnMissionStarted;
                Level6MissionManager.Instance.MissionFailed += OnMissionFailed;
                Level6MissionManager.Instance.LevelCompleted += OnLevelCompleted;
            }

            if (WeatherController.Instance != null)
            {
                WeatherController.Instance.WeatherChanged += OnWeatherChanged;
            }
        }

        private void OnDestroy()
        {
            if (playerCar != null)
            {
                playerCar.SpeedChanged -= OnSpeedChanged;
                playerCar.GripChanged -= OnGripChanged;
                playerCar.SkidStateChanged -= OnSkidStateChanged;
                playerCar.OffRoadChanged -= OnOffRoadChanged;
            }

            if (Level6SafetyManager.Instance != null)
            {
                Level6SafetyManager.Instance.ScoreChanged -= OnScoreChanged;
                Level6SafetyManager.Instance.SafetyScoreChanged -= OnSafetyScoreChanged;
                Level6SafetyManager.Instance.RouteSafetyChanged -= OnRouteSafetyChanged;
                Level6SafetyManager.Instance.FeedbackTriggered -= ShowFeedback;
            }

            if (Level6MissionManager.Instance != null)
            {
                Level6MissionManager.Instance.MissionStarted -= OnMissionStarted;
                Level6MissionManager.Instance.MissionFailed -= OnMissionFailed;
                Level6MissionManager.Instance.LevelCompleted -= OnLevelCompleted;
            }

            if (WeatherController.Instance != null)
            {
                WeatherController.Instance.WeatherChanged -= OnWeatherChanged;
            }
        }

        private void Update()
        {
            // Update Mission Timer
            if (Level6MissionManager.Instance != null && missionTimerText != null)
            {
                float t = Level6MissionManager.Instance.TimeRemaining;
                int mins = Mathf.FloorToInt(t / 60f);
                int secs = Mathf.FloorToInt(t % 60f);
                missionTimerText.text = $"TIME: {mins:00}:{secs:00}";
                missionTimerText.color = (t < 15f) ? new Color(1f, 0.3f, 0.3f) : Color.white;
            }

            // Update GPS Navigation Guidance
            if (Level6GPSCompass.Instance != null && gpsNavigationText != null)
            {
                gpsNavigationText.text = Level6GPSCompass.Instance.GetFormattedGuidance();
                float dist = Level6GPSCompass.Instance.DistanceToTarget;
                gpsNavigationText.color = dist < 20f ? new Color(0.35f, 1f, 0.45f, 1f) : new Color(1f, 0.88f, 0.25f, 1f);
            }

            // Update Wind vector telemetry
            if (WeatherController.Instance != null && windStatusText != null)
            {
                Vector2 wind = WeatherController.Instance.CurrentWind;
                if (wind.magnitude > 0.05f)
                {
                    string dir = wind.x > 0 ? "East" : "West";
                    windStatusText.text = $"Wind: {wind.magnitude * 10f:0} km/h {dir}";
                }
                else
                {
                    windStatusText.text = "Wind: Calm";
                }
            }
        }

        private void OnOffRoadChanged(bool isOffRoad)
        {
            if (offRoadWarningText != null)
            {
                offRoadWarningText.gameObject.SetActive(isOffRoad);
                if (isOffRoad)
                {
                    offRoadWarningText.text = "[!] OFF-ROAD! RETURN TO ROADWAY";
                }
            }
        }

        private void OnSpeedChanged(float speedKmh, float forwardSpeed)
        {
            if (currentSpeedText != null)
            {
                currentSpeedText.text = $"{Mathf.RoundToInt(speedKmh)}";
            }
        }

        private void OnGripChanged(float grip)
        {
            if (gripBarFill != null)
            {
                gripBarFill.fillAmount = Mathf.Clamp01(grip);
                // Color shifts from green to yellow to red
                gripBarFill.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.85f, 0.3f), grip);
            }
            if (gripValueText != null)
            {
                gripValueText.text = $"Grip: {Mathf.RoundToInt(grip * 100f)}%";
            }
        }

        private void OnSkidStateChanged(bool isSkidding)
        {
            if (skidWarningText != null)
            {
                skidWarningText.text = "SKIDDING! SLOW DOWN";
                skidWarningText.gameObject.SetActive(isSkidding);
            }
        }

        private void OnScoreChanged(int score)
        {
            if (scoreValueText != null)
            {
                scoreValueText.text = $"Score: {score}";
            }
        }

        private void OnSafetyScoreChanged(float safety)
        {
            if (safetyValueText != null)
            {
                safetyValueText.text = $"Safety: {Mathf.RoundToInt(safety)}%";
            }
            if (safetyMeterFill != null)
            {
                safetyMeterFill.fillAmount = safety / 100f;
                safetyMeterFill.color = Color.Lerp(new Color(0.85f, 0.2f, 0.2f), new Color(0.2f, 0.85f, 0.3f), safety / 100f);
            }
        }

        private void OnRouteSafetyChanged(float routeSafety)
        {
            if (routeSafetyValueText != null)
            {
                routeSafetyValueText.text = $"Route Rating: {Mathf.RoundToInt(routeSafety)}%";
            }
            if (routeSafetyMeterFill != null)
            {
                routeSafetyMeterFill.fillAmount = routeSafety / 100f;
            }
        }

        private void OnWeatherChanged(WeatherType weather)
        {
            if (weatherStatusText != null)
            {
                string label = "CLEAR";
                switch (weather)
                {
                    case WeatherType.Rain: label = "RAIN"; break;
                    case WeatherType.HeavyRain: label = "HEAVY RAIN"; break;
                    case WeatherType.Fog: label = "DENSE FOG"; break;
                    case WeatherType.Wind: label = "STRONG WIND"; break;
                    case WeatherType.Storm: label = "SEVERE STORM"; break;
                    default: label = "CLEAR"; break;
                }
                weatherStatusText.text = label;
            }
        }

        private void OnMissionStarted(Level6MissionConfig config)
        {
            if (missionBadgeText != null) missionBadgeText.text = $"MISSION {config.index} / 5";
            if (missionTitleText != null) missionTitleText.text = config.title;
            if (missionObjectiveText != null) missionObjectiveText.text = config.objective;
            if (speedLimitBadgeText != null) speedLimitBadgeText.text = $"MAX {Mathf.RoundToInt(config.speedLimitKmh)}";

            if (failModal != null) failModal.SetActive(false);
            if (completionModal != null) completionModal.SetActive(false);
        }

        private void OnMissionFailed(string reason)
        {
            if (failModal != null)
            {
                failModal.SetActive(true);
                if (failReasonText != null) failReasonText.text = reason;
            }
        }

        private void OnLevelCompleted()
        {
            if (completionModal != null)
            {
                completionModal.SetActive(true);

                Level6SafetyManager sm = Level6SafetyManager.Instance;
                int stars = (sm != null) ? sm.CalculateStars() : 3;

                if (starRatingText != null)
                {
                    starRatingText.text = stars == 3 ? "3 OF 3 STARS" : (stars == 2 ? "2 OF 3 STARS" : (stars == 1 ? "1 OF 3 STARS" : "0 OF 3 STARS"));
                }
                if (finalScoreText != null) finalScoreText.text = $"Final Score: {(sm != null ? sm.TotalScore : 500)}";
                if (finalSafetyText != null) finalSafetyText.text = $"Overall Safety: {(sm != null ? Mathf.RoundToInt(sm.SafetyScore) : 100)}%";
                if (finalRouteSafetyText != null) finalRouteSafetyText.text = $"Route Rating: {(sm != null ? Mathf.RoundToInt(sm.RouteSafetyScore) : 100)}%";
                if (finalCollisionsText != null) finalCollisionsText.text = $"Collisions: {(sm != null ? sm.CollisionCount : 0)}";
                if (finalHazardsAvoidedText != null) finalHazardsAvoidedText.text = $"Hazards Avoided: {(sm != null ? sm.HazardsAvoided : 5)}";
                if (finalUTurnsText != null) finalUTurnsText.text = $"Safe U-Turns: {(sm != null ? sm.UTurnsCompleted : 1)}";
            }
        }

        public void ShowFeedback(string message)
        {
            if (feedbackBanner == null || feedbackMessageText == null) return;

            feedbackMessageText.text = message;
            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
            }
            feedbackRoutine = StartCoroutine(FeedbackBannerRoutine());
        }

        private IEnumerator FeedbackBannerRoutine()
        {
            feedbackBanner.SetActive(true);
            if (feedbackGroup != null) feedbackGroup.alpha = 1f;

            yield return new WaitForSeconds(3.5f);

            if (feedbackGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.deltaTime;
                    feedbackGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
                    yield return null;
                }
            }

            feedbackBanner.SetActive(false);
        }

        private void OnFailRetryClicked()
        {
            if (failModal != null) failModal.SetActive(false);
            if (Level6MissionManager.Instance != null)
            {
                Level6MissionManager.Instance.RestartCurrentMission();
            }
        }

        private void OnCertificateReplayClicked()
        {
            if (completionModal != null) completionModal.SetActive(false);
            if (Level6MissionManager.Instance != null)
            {
                Level6MissionManager.Instance.RestartLevel();
            }
        }

        private void OnReturnToMenuClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        public void BindDynamicReferences(
            TMP_Text mBadge, TMP_Text mTitle, TMP_Text mObj, TMP_Text mTimer,
            TMP_Text wStatus, Image gFill, TMP_Text gVal, TMP_Text wWind, TMP_Text hNotice,
            TMP_Text sVal, TMP_Text sfVal, Image sfFill, TMP_Text rsVal, Image rsFill,
            TMP_Text cSpeed, TMP_Text spBadge, TMP_Text skWarning,
            GameObject fBanner, CanvasGroup fGroup, TMP_Text fMsg,
            GameObject fModal, TMP_Text fReason, Button fRetry, Button fMenu,
            GameObject cModal, TMP_Text cTitle, TMP_Text cSub, TMP_Text cStars,
            TMP_Text cScore, TMP_Text cSafety, TMP_Text cRoute, TMP_Text cCollisions,
            TMP_Text cHazards, TMP_Text cUTurns, Button cReplay, Button cMenu,
            TMP_Text mGps = null, TMP_Text offRoadWarn = null)
        {
            missionBadgeText = mBadge;
            missionTitleText = mTitle;
            missionObjectiveText = mObj;
            missionTimerText = mTimer;
            gpsNavigationText = mGps;
            offRoadWarningText = offRoadWarn;

            weatherStatusText = wStatus;
            gripBarFill = gFill;
            gripValueText = gVal;
            windStatusText = wWind;
            hazardNoticeText = hNotice;

            scoreValueText = sVal;
            safetyValueText = sfVal;
            safetyMeterFill = sfFill;
            routeSafetyValueText = rsVal;
            routeSafetyMeterFill = rsFill;

            currentSpeedText = cSpeed;
            speedLimitBadgeText = spBadge;
            skidWarningText = skWarning;

            feedbackBanner = fBanner;
            feedbackGroup = fGroup;
            feedbackMessageText = fMsg;

            failModal = fModal;
            failReasonText = fReason;
            failRetryButton = fRetry;
            failMenuButton = fMenu;

            completionModal = cModal;
            certificateTitleText = cTitle;
            certificateSubtitleText = cSub;
            starRatingText = cStars;
            finalScoreText = cScore;
            finalSafetyText = cSafety;
            finalRouteSafetyText = cRoute;
            finalCollisionsText = cCollisions;
            finalHazardsAvoidedText = cHazards;
            finalUTurnsText = cUTurns;
            certificateReplayButton = cReplay;
            certificateMenuButton = cMenu;
        }
    }
}
