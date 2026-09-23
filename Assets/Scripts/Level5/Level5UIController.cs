using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TrafficTown2D.Core;

namespace TrafficTown2D.Level5
{
    public class Level5UIController : MonoBehaviour
    {
        [Header("Mission Card (Top-Left)")]
        [SerializeField] private TMP_Text missionBadgeText;
        [SerializeField] private TMP_Text missionTitleText;
        [SerializeField] private TMP_Text missionObjectiveText;
        [SerializeField] private TMP_Text missionTimerText;

        [Header("Score & Status (Top-Right)")]
        [SerializeField] private TMP_Text scoreValueText;
        [SerializeField] private TMP_Text safetyValueText;
        [SerializeField] private Image safetyMeterFill;
        [SerializeField] private TMP_Text trafficFlowValueText;
        [SerializeField] private Image trafficFlowMeterFill;

        [Header("Feedback Toast (Top-Center)")]
        [SerializeField] private GameObject feedbackBanner;
        [SerializeField] private CanvasGroup feedbackGroup;
        [SerializeField] private TMP_Text feedbackMessageText;

        [Header("Control Panel (Bottom-Right)")]
        [SerializeField] private TMP_Text northSouthStatusText;
        [SerializeField] private TMP_Text eastWestStatusText;
        [SerializeField] private TMP_Text pedestrianStatusText;
        [SerializeField] private TMP_Text emergencyStatusText;
        [SerializeField] private TMP_Text phaseTimerText;
        [SerializeField] private TMP_Text vehiclesWaitingText;
        [SerializeField] private TMP_Text pedestriansWaitingText;
        [SerializeField] private Button changePhaseButton;
        [SerializeField] private Button pedestrianWalkButton;

        [Header("Pause Panel")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button pauseMenuButton;

        [Header("Grand Finale Certificate Panel")]
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private CanvasGroup completionGroup;
        [SerializeField] private TMP_Text completionTitleText;
        [SerializeField] private TMP_Text completionSubtitleText;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text safetyScoreText;
        [SerializeField] private TMP_Text flowScoreText;
        [SerializeField] private TMP_Text pedestriansCrossedText;
        [SerializeField] private TMP_Text emergenciesClearedText;
        [SerializeField] private TMP_Text collisionsText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button backToMenuButton;

        private Coroutine feedbackRoutine;
        private bool isPaused = false;

        private void Awake()
        {
            if (completionPanel != null) completionPanel.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);

            SanitizeLayout();
            InitializeButtons();
        }

        public void SanitizeLayout()
        {
            // 1. Mission Card
            if (missionTitleText != null)
            {
                RectTransform rt = missionTitleText.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(18f, -54f);
                rt.sizeDelta = new Vector2(424f, 26f);

                RectTransform parentRt = rt.parent as RectTransform;
                if (parentRt != null && parentRt.name == "MissionCard")
                {
                    parentRt.sizeDelta = new Vector2(460f, 160f);
                }
            }

            if (missionObjectiveText != null)
            {
                RectTransform rt = missionObjectiveText.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(18f, -86f);
                rt.sizeDelta = new Vector2(424f, 60f);
            }

            if (missionTimerText != null)
            {
                RectTransform rt = missionTimerText.rectTransform;
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-18f, -18f);
                rt.sizeDelta = new Vector2(120f, 24f);
            }

            if (missionBadgeText != null && missionBadgeText.transform.parent != null)
            {
                RectTransform badgePillRt = missionBadgeText.transform.parent as RectTransform;
                if (badgePillRt != null && badgePillRt.name == "BadgePill")
                {
                    badgePillRt.anchorMin = new Vector2(0f, 1f);
                    badgePillRt.anchorMax = new Vector2(0f, 1f);
                    badgePillRt.pivot = new Vector2(0f, 1f);
                    badgePillRt.anchoredPosition = new Vector2(18f, -16f);
                    badgePillRt.sizeDelta = new Vector2(140f, 28f);
                }
            }

            // 2. Control Panel
            if (northSouthStatusText != null)
            {
                RectTransform rt = northSouthStatusText.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(20f, -50f);
                rt.sizeDelta = new Vector2(360f, 24f);

                RectTransform ctrlPanelRt = rt.parent as RectTransform;
                if (ctrlPanelRt != null && ctrlPanelRt.name == "TrafficControlPanel")
                {
                    ctrlPanelRt.anchorMin = new Vector2(1f, 0f);
                    ctrlPanelRt.anchorMax = new Vector2(1f, 0f);
                    ctrlPanelRt.pivot = new Vector2(1f, 0f);
                    ctrlPanelRt.anchoredPosition = new Vector2(-35f, 30f);
                    ctrlPanelRt.sizeDelta = new Vector2(400f, 410f);
                }
            }

            if (eastWestStatusText != null)
            {
                RectTransform rt = eastWestStatusText.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(20f, -76f);
                rt.sizeDelta = new Vector2(360f, 24f);
            }

            if (pedestrianStatusText != null)
            {
                RectTransform rt = pedestrianStatusText.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(20f, -102f);
                rt.sizeDelta = new Vector2(360f, 24f);
            }

            if (emergencyStatusText != null)
            {
                RectTransform rt = emergencyStatusText.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(20f, -128f);
                rt.sizeDelta = new Vector2(360f, 24f);
            }

            if (phaseTimerText != null)
            {
                RectTransform rt = phaseTimerText.rectTransform;
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-20f, -16f);
                rt.sizeDelta = new Vector2(140f, 24f);
            }

            if (vehiclesWaitingText != null)
            {
                RectTransform rt = vehiclesWaitingText.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(20f, -158f);
                rt.sizeDelta = new Vector2(175f, 22f);
            }

            if (pedestriansWaitingText != null)
            {
                RectTransform rt = pedestriansWaitingText.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(205f, -158f);
                rt.sizeDelta = new Vector2(175f, 22f);
            }

            if (changePhaseButton != null)
            {
                RectTransform rt = changePhaseButton.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 78f);
                rt.sizeDelta = new Vector2(360f, 48f);
            }

            if (pedestrianWalkButton != null)
            {
                RectTransform rt = pedestrianWalkButton.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 20f);
                rt.sizeDelta = new Vector2(360f, 48f);
            }

            // 3. Stats Column in Grand Finale Certificate Panel
            if (completionPanel != null)
            {
                Transform certCard = completionPanel.transform.Find("CertCard");
                if (certCard != null)
                {
                    Transform statsCol = certCard.Find("StatsCol");
                    if (statsCol != null)
                    {
                        RectTransform statsRt = statsCol as RectTransform;
                        if (statsRt != null)
                        {
                            statsRt.sizeDelta = new Vector2(330f, 280f);
                        }

                        Transform scoreRow = statsCol.Find("ScoreRow");
                        if (scoreRow != null) ((RectTransform)scoreRow).anchoredPosition = new Vector2(0f, -40f);

                        Transform safetyRow = statsCol.Find("SafetyRow");
                        if (safetyRow != null) ((RectTransform)safetyRow).anchoredPosition = new Vector2(0f, -76f);

                        Transform flowRow = statsCol.Find("FlowRow");
                        if (flowRow != null) ((RectTransform)flowRow).anchoredPosition = new Vector2(0f, -112f);

                        Transform pedRow = statsCol.Find("PedRow");
                        if (pedRow != null) ((RectTransform)pedRow).anchoredPosition = new Vector2(0f, -148f);

                        Transform emgRow = statsCol.Find("EmgRow");
                        if (emgRow != null) ((RectTransform)emgRow).anchoredPosition = new Vector2(0f, -184f);

                        Transform colRow = statsCol.Find("CollisionRow");
                        if (colRow == null)
                        {
                            GameObject newRow = new GameObject("CollisionRow");
                            newRow.transform.SetParent(statsCol, false);
                            RectTransform rowRt = newRow.AddComponent<RectTransform>();
                            rowRt.anchorMin = new Vector2(0f, 1f);
                            rowRt.anchorMax = new Vector2(1f, 1f);
                            rowRt.pivot = new Vector2(0.5f, 1f);
                            rowRt.anchoredPosition = new Vector2(0f, -220f);
                            rowRt.sizeDelta = new Vector2(0f, 26f);

                            GameObject lblObj = new GameObject("Label");
                            lblObj.transform.SetParent(newRow.transform, false);
                            RectTransform lblRt = lblObj.AddComponent<RectTransform>();
                            lblRt.anchorMin = new Vector2(0f, 0.5f);
                            lblRt.anchorMax = new Vector2(0f, 0.5f);
                            lblRt.pivot = new Vector2(0f, 0.5f);
                            lblRt.anchoredPosition = new Vector2(16f, 0f);
                            lblRt.sizeDelta = new Vector2(200f, 24f);
                            TMP_Text lbl = lblObj.AddComponent<TextMeshProUGUI>();
                            lbl.text = "Traffic Collisions";
                            lbl.fontSize = 12f;
                            lbl.color = new Color(0.6f, 0.7f, 0.85f, 1f);

                            GameObject valObj = new GameObject("Val");
                            valObj.transform.SetParent(newRow.transform, false);
                            RectTransform valRt = valObj.AddComponent<RectTransform>();
                            valRt.anchorMin = new Vector2(1f, 0.5f);
                            valRt.anchorMax = new Vector2(1f, 0.5f);
                            valRt.pivot = new Vector2(1f, 0.5f);
                            valRt.anchoredPosition = new Vector2(-16f, 0f);
                            valRt.sizeDelta = new Vector2(100f, 24f);
                            TMP_Text val = valObj.AddComponent<TextMeshProUGUI>();
                            val.text = "0";
                            val.fontSize = 13f;
                            val.alignment = TextAlignmentOptions.Right;
                            val.fontStyle = FontStyles.Bold;
                            val.color = Color.white;

                            collisionsText = val;
                        }
                        else
                        {
                            ((RectTransform)colRow).anchoredPosition = new Vector2(0f, -220f);
                            if (collisionsText == null)
                            {
                                collisionsText = colRow.Find("Val")?.GetComponent<TMP_Text>();
                            }
                        }
                    }
                }
            }
        }

        public void InitializeButtons()
        {
            if (changePhaseButton != null)
            {
                changePhaseButton.onClick.RemoveAllListeners();
                changePhaseButton.onClick.AddListener(() =>
                {
                    if (TrafficPhaseController.Instance != null)
                        TrafficPhaseController.Instance.RequestPhaseChange();
                });
            }

            if (pedestrianWalkButton != null)
            {
                pedestrianWalkButton.onClick.RemoveAllListeners();
                pedestrianWalkButton.onClick.AddListener(() =>
                {
                    if (TrafficPhaseController.Instance != null)
                        TrafficPhaseController.Instance.RequestPedestrianPhase();
                });
            }

            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveAllListeners();
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.RemoveAllListeners();
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(ResumeGame);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(RestartGame);
            }

            if (pauseMenuButton != null)
            {
                pauseMenuButton.onClick.RemoveAllListeners();
                pauseMenuButton.onClick.AddListener(OnBackToMenuClicked);
            }
        }

        private void Start()
        {
            SanitizeLayout();
            InitializeButtons();

            if (Level5MissionManager.Instance != null)
            {
                Level5MissionManager.Instance.MissionStarted += UpdateMissionUI;
                Level5MissionManager.Instance.MissionCompleted += OnMissionCompleted;
                Level5MissionManager.Instance.MissionFailed += ShowMissionFailedPanel;
                Level5MissionManager.Instance.LevelCompleted += ShowGrandFinaleCertificate;
            }

            if (Level5SafetyManager.Instance != null)
            {
                Level5SafetyManager.Instance.ScoreChanged += UpdateScoreUI;
                Level5SafetyManager.Instance.SafetyScoreChanged += UpdateSafetyUI;
                Level5SafetyManager.Instance.TrafficFlowChanged += UpdateTrafficFlowUI;
                Level5SafetyManager.Instance.FeedbackTriggered += ShowFeedback;

                UpdateScoreUI(Level5SafetyManager.Instance.Score);
                UpdateSafetyUI(Level5SafetyManager.Instance.SafetyScore);
                UpdateTrafficFlowUI(Level5SafetyManager.Instance.TrafficFlowScore);
            }
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;

            // Toggle pause on Escape (New Input System with fallback)
            bool escPressed = false;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) escPressed = true;
            }
            if (!escPressed)
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.Escape)) escPressed = true;
                }
                catch {}
            }

            if (escPressed)
            {
                TogglePause();
            }

            // Tow Truck quick clear on 'C'
            bool cPressed = false;
            if (kb != null)
            {
                if (kb.cKey.wasPressedThisFrame) cPressed = true;
            }
            if (!cPressed)
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.C)) cPressed = true;
                }
                catch {}
            }
            if (cPressed)
            {
                DispatchTowTruck();
            }

            // Update mission timer
            if (Level5MissionManager.Instance != null && Level5MissionManager.Instance.IsMissionRunning)
            {
                float left = Level5MissionManager.Instance.TimeRemaining;
                if (missionTimerText != null)
                {
                    missionTimerText.text = $"Time: {Mathf.CeilToInt(left)}s";
                }
            }

            UpdateControlPanel();
        }

        private void UpdateControlPanel()
        {
            if (TrafficPhaseController.Instance == null) return;

            IntersectionPhase phase = TrafficPhaseController.Instance.CurrentPhase;
            bool transitioning = TrafficPhaseController.Instance.IsTransitioning;

            // North/South status
            if (northSouthStatusText != null)
            {
                string status = "[RED]";
                Color col = new Color(1f, 0.35f, 0.35f, 1f);
                if (phase == IntersectionPhase.NorthSouthGreen)
                {
                    status = "[GREEN]";
                    col = new Color(0.35f, 0.95f, 0.45f, 1f);
                }
                else if (phase == IntersectionPhase.NorthSouthYellow)
                {
                    status = "[YELLOW]";
                    col = new Color(1f, 0.85f, 0.2f, 1f);
                }

                northSouthStatusText.text = $"NORTH / SOUTH:   {status}";
                northSouthStatusText.color = col;
            }

            // East/West status
            if (eastWestStatusText != null)
            {
                string status = "[RED]";
                Color col = new Color(1f, 0.35f, 0.35f, 1f);
                if (phase == IntersectionPhase.EastWestGreen)
                {
                    status = "[GREEN]";
                    col = new Color(0.35f, 0.95f, 0.45f, 1f);
                }
                else if (phase == IntersectionPhase.EastWestYellow)
                {
                    status = "[YELLOW]";
                    col = new Color(1f, 0.85f, 0.2f, 1f);
                }

                eastWestStatusText.text = $"EAST / WEST:          {status}";
                eastWestStatusText.color = col;
            }

            // Pedestrian status
            if (pedestrianStatusText != null)
            {
                string status = "[DON'T WALK]";
                Color col = new Color(1f, 0.35f, 0.35f, 1f);
                if (phase == IntersectionPhase.PedestrianWalk)
                {
                    status = "[WALK]";
                    col = new Color(0.35f, 0.95f, 0.45f, 1f);
                }
                else if (phase == IntersectionPhase.PedestrianClearance)
                {
                    status = "[CLEARING]";
                    col = new Color(1f, 0.85f, 0.2f, 1f);
                }

                pedestrianStatusText.text = $"PEDESTRIANS:       {status}";
                pedestrianStatusText.color = col;
            }

            // Waiting counters
            if (vehiclesWaitingText != null)
            {
                int totalWaiting = TrafficQueueManager.Instance != null ? TrafficQueueManager.Instance.TotalWaiting : 0;
                vehiclesWaitingText.text = $"Cars Waiting: {totalWaiting}";
            }

            if (pedestriansWaitingText != null)
            {
                int pedWaiting = PedestrianIntersectionManager.Instance != null ? PedestrianIntersectionManager.Instance.WaitingCount : 0;
                pedestriansWaitingText.text = $"Peds Waiting: {pedWaiting}";
            }

            // Emergency status & Accident alert
            if (emergencyStatusText != null)
            {
                if (TrafficQueueManager.Instance != null && TrafficQueueManager.Instance.HasCrashedVehicles())
                {
                    emergencyStatusText.text = "ACCIDENT: 🚨 TOW TRUCK [Press C]";
                    emergencyStatusText.color = new Color(1f, 0.45f, 0.2f, 1f);
                }
                else if (EmergencyVehicleManager.Instance != null && EmergencyVehicleManager.Instance.IsEmergencyActive)
                {
                    emergencyStatusText.text = $"EMERGENCY: AMBULANCE [{EmergencyVehicleManager.Instance.ActiveDirection.ToString().ToUpper()}]";
                    emergencyStatusText.color = new Color(1f, 0.25f, 0.25f, 1f);
                }
                else
                {
                    emergencyStatusText.text = "EMERGENCY: None";
                    emergencyStatusText.color = new Color(0.6f, 0.85f, 0.7f, 1f);
                }
            }

            // Phase timer text
            if (phaseTimerText != null)
            {
                if (transitioning)
                {
                    phaseTimerText.text = "TRANSITION...";
                    phaseTimerText.color = new Color(1f, 0.85f, 0.2f, 1f);
                }
                else
                {
                    int elapsed = Mathf.FloorToInt(TrafficPhaseController.Instance.PhaseElapsedTime);
                    phaseTimerText.text = $"ACTIVE ({elapsed}s)";
                    phaseTimerText.color = Color.white;
                }
            }
        }

        private void UpdateMissionUI(Level5MissionConfig config)
        {
            if (missionBadgeText != null) missionBadgeText.text = $"MISSION {config.index} OF 5";
            if (missionTitleText != null) missionTitleText.text = config.name;
            if (missionObjectiveText != null) missionObjectiveText.text = config.objective;
            ShowFeedback($"Starting Mission {config.index}: {config.name}");
        }

        private void OnMissionCompleted(Level5MissionConfig config)
        {
            ShowFeedback($"Mission {config.index} Complete! +{config.reward} PTS");
        }

        private void UpdateScoreUI(int score)
        {
            if (scoreValueText != null) scoreValueText.text = score.ToString("N0");
        }

        private void UpdateSafetyUI(float safety)
        {
            if (safetyValueText != null) safetyValueText.text = $"{Mathf.RoundToInt(safety)}%";
            if (safetyMeterFill != null)
            {
                safetyMeterFill.fillAmount = safety / 100f;
                safetyMeterFill.color = (safety >= 70f) 
                    ? new Color(0.16f, 0.72f, 0.36f, 1f) 
                    : (safety >= 40f ? new Color(0.95f, 0.65f, 0.15f, 1f) : new Color(0.90f, 0.22f, 0.20f, 1f));
            }
        }

        private void UpdateTrafficFlowUI(float flow)
        {
            if (trafficFlowValueText != null) trafficFlowValueText.text = $"{Mathf.RoundToInt(flow)}%";
            if (trafficFlowMeterFill != null)
            {
                trafficFlowMeterFill.fillAmount = flow / 100f;
                trafficFlowMeterFill.color = (flow >= 60f) 
                    ? new Color(0.20f, 0.65f, 0.95f, 1f) 
                    : (flow >= 35f ? new Color(0.95f, 0.65f, 0.15f, 1f) : new Color(0.90f, 0.22f, 0.20f, 1f));
            }
        }

        public void ShowFeedback(string message)
        {
            if (feedbackBanner == null || feedbackMessageText == null) return;

            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
            feedbackRoutine = StartCoroutine(FeedbackAnimationRoutine(message));
        }

        private IEnumerator FeedbackAnimationRoutine(string message)
        {
            feedbackMessageText.text = message;
            feedbackBanner.SetActive(true);

            if (feedbackGroup != null)
            {
                feedbackGroup.alpha = 0f;
                float t = 0f;
                while (t < 0.25f)
                {
                    t += Time.unscaledDeltaTime;
                    feedbackGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.25f);
                    yield return null;
                }
                feedbackGroup.alpha = 1f;

                yield return new WaitForSecondsRealtime(2.8f);

                t = 0f;
                while (t < 0.3f)
                {
                    t += Time.unscaledDeltaTime;
                    feedbackGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.3f);
                    yield return null;
                }
                feedbackGroup.alpha = 0f;
            }
            else
            {
                yield return new WaitForSecondsRealtime(2.8f);
            }

            feedbackBanner.SetActive(false);
            feedbackRoutine = null;
        }

        public void ShowGrandFinaleCertificate()
        {
            if (completionPanel == null) return;

            completionPanel.SetActive(true);
            if (completionGroup != null) completionGroup.alpha = 1f;

            if (completionTitleText != null)
                completionTitleText.text = "🏆 TRAFFIC SAFETY MASTER 🏆";

            if (completionSubtitleText != null)
                completionSubtitleText.text = "CONGRATULATIONS!\nYou have mastered all 5 levels of TrafficTown 2D!";

            int score = Level5SafetyManager.Instance != null ? Level5SafetyManager.Instance.Score : 0;
            float safety = Level5SafetyManager.Instance != null ? Level5SafetyManager.Instance.SafetyScore : 100f;
            float flow = Level5SafetyManager.Instance != null ? Level5SafetyManager.Instance.TrafficFlowScore : 100f;
            int peds = PedestrianIntersectionManager.Instance != null ? PedestrianIntersectionManager.Instance.TotalCrossed : 0;
            int emg = EmergencyVehicleManager.Instance != null ? EmergencyVehicleManager.Instance.EmergenciesCleared : 0;
            int cols = Level5SafetyManager.Instance != null ? Level5SafetyManager.Instance.CollisionCount : 0;

            if (finalScoreText != null) finalScoreText.text = score.ToString("N0");

            if (safetyScoreText != null)
            {
                int safetyPct = Mathf.RoundToInt(safety);
                safetyScoreText.text = $"{safetyPct}%";
                if (safetyPct >= 80)
                    safetyScoreText.color = new Color(0.35f, 0.90f, 0.45f, 1f);
                else if (safetyPct >= 50)
                    safetyScoreText.color = new Color(1f, 0.75f, 0.2f, 1f);
                else
                    safetyScoreText.color = new Color(1f, 0.35f, 0.25f, 1f);
            }

            if (flowScoreText != null) flowScoreText.text = $"{Mathf.RoundToInt(flow)}%";
            if (pedestriansCrossedText != null) pedestriansCrossedText.text = peds.ToString();
            if (emergenciesClearedText != null) emergenciesClearedText.text = emg.ToString();

            if (collisionsText != null)
            {
                collisionsText.text = cols.ToString();
                if (cols > 0)
                {
                    collisionsText.color = new Color(1f, 0.35f, 0.25f, 1f);
                }
                else
                {
                    collisionsText.color = new Color(0.35f, 0.90f, 0.45f, 1f);
                }
            }
        }

        public void DispatchTowTruck()
        {
            if (TrafficQueueManager.Instance != null && TrafficQueueManager.Instance.HasCrashedVehicles())
            {
                int cleared = TrafficQueueManager.Instance.ClearAllCrashedVehicles();
                if (cleared > 0)
                {
                    if (Level5SafetyManager.Instance != null)
                    {
                        Level5SafetyManager.Instance.AddScore(15);
                    }
                    ShowFeedback($"🚨 Tow truck cleared {cleared} accident vehicle(s)! (+15 PTS)");
                }
            }
        }

        public void ShowMissionFailedPanel(string reason)
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
                TMP_Text pTitle = pausePanel.transform.Find("PauseTitle")?.GetComponent<TMP_Text>();
                if (pTitle != null)
                {
                    pTitle.text = "MISSION FAILED";
                    pTitle.color = new Color(0.95f, 0.25f, 0.25f, 1f);
                }
                ShowFeedback($"❌ {reason}");
                Time.timeScale = 0f;
            }
        }

        private void TogglePause()
        {
            if (completionPanel != null && completionPanel.activeSelf) return;

            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
            if (pausePanel != null) pausePanel.SetActive(isPaused);
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (Level5MissionManager.Instance != null)
            {
                Level5MissionManager.Instance.RestartLevel();
            }
        }

        private void OnPlayAgainClicked()
        {
            Time.timeScale = 1f;
            if (completionPanel != null) completionPanel.SetActive(false);
            if (Level5MissionManager.Instance != null)
            {
                Level5MissionManager.Instance.RestartLevel();
            }
        }

        private void OnBackToMenuClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
