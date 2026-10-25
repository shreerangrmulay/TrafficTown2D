using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using TrafficTown2D.Core;
using TrafficTown2D.Level3;
using TrafficTown2D.Player;
using TrafficTown2D.UI;

namespace TrafficTown2D.Level8
{
    /// <summary>
    /// Self-contained runtime bootstrapper and builder for Level 8 — THE OVERTAKE.
    /// Runs automatically upon scene load or editor bake, constructing roads,
    /// signs, traffic, player car, HUD, and mission architecture.
    /// </summary>
    public class Level8Bootstrap : MonoBehaviour
    {
        private const string WorldSquareSpritePath = "Assets/Sprites/Generated/WorldSquare.png";
        private const string WorldCircleSpritePath = "Assets/Sprites/Generated/WorldCircle.png";
        private const string CarBlueTopDownPath = "Assets/Sprites/Vehicles/CarBlueTopDown.png";
        private const string CheckeredFinishPath = "Assets/Sprites/Environment/CheckeredFinish.png";
        private const string ZebraCrossingPath = "Assets/Sprites/Environment/ZebraCrossingStripes.png";
        private const string SignPostBasePath = "Assets/Sprites/Environment/SignPostBase.png";
        private const string RoundedPanelPath = "Assets/UI/RoundedPanel.png";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == "Level8")
            {
                EnsureLevel8Scene();
            }
        }

        private void Awake()
        {
            EnsureLevel8Scene();
        }

        public static void EnsureLevel8Scene()
        {
            Camera cam = EnsureCamera();
            EnsureGlobalLight();
            EnsureEventSystem();

            // Clean any legacy placeholder objects (old emergency vehicles, old traffic spawners)
            CleanLegacyElements();

            GameObject services = FindOrCreate("Services");
            SceneLoader sceneLoader = GetOrAdd<SceneLoader>(services);
            GetOrAdd<GameManager>(services);

            // 1. Environment & Route Construction
            GameObject env = FindOrCreate("Environment");
            Transform existingRoad = env.transform.Find("RoadCorridor");
            if (existingRoad == null)
            {
                BuildCompleteRoadEnvironment(env.transform);
            }

            // 2. Player Car
            GameObject playerCarObj = GameObject.Find("PlayerCar");
            if (playerCarObj == null)
            {
                playerCarObj = BuildPlayerCar(env.transform);
            }
            Level3PlayerCar playerCar = GetOrAdd<Level3PlayerCar>(playerCarObj);
            CarIndicatorController indCtrl = GetOrAdd<CarIndicatorController>(playerCarObj);

            // Connect Camera Follow
            TopDownCameraFollow camFollow = cam.GetComponent<TopDownCameraFollow>();
            if (camFollow == null) camFollow = cam.gameObject.AddComponent<TopDownCameraFollow>();
            camFollow.SetTarget(playerCar.transform);
            camFollow.SetBounds(-8f, 310f);

            // 3. Gameplay Managers
            GameObject gameplay = FindOrCreate("Gameplay");
            Level8OvertakeManager mgr = GetOrAdd<Level8OvertakeManager>(gameplay);
            OvertakeSafetyEvaluator evaluator = GetOrAdd<OvertakeSafetyEvaluator>(gameplay);
            OvertakeScenarioController scenario = GetOrAdd<OvertakeScenarioController>(gameplay);
            OvertakeMissionController missionCtrl = GetOrAdd<OvertakeMissionController>(gameplay);

            evaluator.BindPlayer(playerCar);
            missionCtrl.BindDependencies(playerCar, evaluator, scenario);

            // 4. UI Hierarchy
            Level8UIController uiCtrl = EnsureUI();

            // 5. Educational Tutorial / Intro Panel for The Overtake
            EnsureIntroPanel(playerCar);

            // 6. Turn Indicator HUD
            CarIndicatorHUD.EnsureHUD(indCtrl);

            Debug.Log("[Level8Bootstrap] Level 8 — THE OVERTAKE initialized successfully.");
        }

        #region Infrastructure Helpers
        private static Camera EnsureCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = 7.5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.24f, 0.58f, 0.28f, 1f); // Lush Grass Green
            cam.clearFlags = CameraClearFlags.SolidColor;

            if (cam.GetComponent<AudioListener>() == null)
                cam.gameObject.AddComponent<AudioListener>();

            UniversalAdditionalCameraData urp = cam.GetComponent<UniversalAdditionalCameraData>();
            if (urp == null) cam.gameObject.AddComponent<UniversalAdditionalCameraData>();

            return cam;
        }

        private static void EnsureGlobalLight()
        {
            Light2D light = UnityEngine.Object.FindAnyObjectByType<Light2D>();
            if (light == null)
            {
                GameObject lObj = new GameObject("Global Light 2D");
                light = lObj.AddComponent<Light2D>();
            }
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1.0f;
            light.color = Color.white;
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }
        #endregion

        #region Environment Construction
        private static void BuildCompleteRoadEnvironment(Transform env)
        {
            Sprite square = LoadSpriteAsset(WorldSquareSpritePath);
            Sprite circle = LoadSpriteAsset(WorldCircleSpritePath);

            // 1. Grass Ground
            CreateSprite(env, "GrassGround", new Vector3(0f, 150f, 5f), new Vector3(70f, 360f, 1f), new Color(0.24f, 0.58f, 0.28f, 1f), -20, square);

            // 2. Main Asphalt Road (Width: 6.6, Length: 350, covers Y = -20 to Y = 330)
            GameObject road = CreateSprite(env, "RoadCorridor", new Vector3(0f, 150f, 0f), new Vector3(6.6f, 350f, 1f), new Color(0.14f, 0.16f, 0.20f, 1f), -10, square);

            // 3. Sidewalks
            CreateSprite(env, "LeftSidewalk", new Vector3(-4.5f, 150f, 0f), new Vector3(2.4f, 350f, 1f), new Color(0.68f, 0.70f, 0.74f, 1f), -12, square);
            CreateSprite(env, "RightSidewalk", new Vector3(4.5f, 150f, 0f), new Vector3(2.4f, 350f, 1f), new Color(0.68f, 0.70f, 0.74f, 1f), -12, square);

            // 4. Curbs & Physical Road Boundaries
            GameObject leftCurb = CreateSprite(env, "LeftCurb", new Vector3(-3.3f, 150f, 0f), new Vector3(0.18f, 350f, 1f), new Color(0.88f, 0.90f, 0.92f, 1f), -9, square);
            BoxCollider2D lCol = leftCurb.AddComponent<BoxCollider2D>();
            lCol.size = new Vector2(0.35f, 350f);

            GameObject rightCurb = CreateSprite(env, "RightCurb", new Vector3(3.3f, 150f, 0f), new Vector3(0.18f, 350f, 1f), new Color(0.88f, 0.90f, 0.92f, 1f), -9, square);
            BoxCollider2D rCol = rightCurb.AddComponent<BoxCollider2D>();
            rCol.size = new Vector2(0.35f, 350f);

            GameObject bottomWall = new GameObject("BottomBoundaryWall");
            bottomWall.transform.SetParent(env, false);
            bottomWall.transform.position = new Vector3(0f, -20.5f, 0f);
            BoxCollider2D bCol = bottomWall.AddComponent<BoxCollider2D>();
            bCol.size = new Vector2(10f, 1f);

            GameObject topWall = new GameObject("TopBoundaryWall");
            topWall.transform.SetParent(env, false);
            topWall.transform.position = new Vector3(0f, 330.5f, 0f);
            BoxCollider2D tCol = topWall.AddComponent<BoxCollider2D>();
            tCol.size = new Vector2(10f, 1f);

            // 5. Road Markings (Dashed vs Solid vs Crossing)
            GameObject markings = FindOrCreateChild(env, "RoadMarkings");

            // Zone 1: Dashed Line (Y = -15 to 55)
            for (float y = -12f; y <= 52f; y += 4f)
            {
                CreateSprite(markings.transform, $"Dash_Z1_{y}", new Vector3(0f, y, 0f), new Vector3(0.18f, 2.2f, 1f), new Color(0.96f, 0.88f, 0.35f, 0.9f), -8, square);
            }

            // Zone 2 & 3: Double Solid Yellow Lines (Y = 55 to 180) - NO OVERTAKING
            CreateSprite(markings.transform, "DoubleSolid_L", new Vector3(-0.10f, 117.5f, 0f), new Vector3(0.12f, 125f, 1f), new Color(0.98f, 0.82f, 0.15f, 1f), -8, square);
            CreateSprite(markings.transform, "DoubleSolid_R", new Vector3(0.10f, 117.5f, 0f), new Vector3(0.12f, 125f, 1f), new Color(0.98f, 0.82f, 0.15f, 1f), -8, square);

            // Zone 4: Intersection & Zebra Pedestrian Crossing (Y = 180 to 235)
            // Cross Street
            CreateSprite(env, "CrossStreet", new Vector3(0f, 205f, 0f), new Vector3(32f, 6.2f, 1f), new Color(0.14f, 0.16f, 0.20f, 1f), -11, square);
            // Zebra Crossing
            GameObject zebra = CreateSprite(env, "ZebraCrossing", new Vector3(0f, 196f, 0f), new Vector3(6.4f, 2.6f, 1f), Color.white, -8, square);
            for (float x = -2.8f; x <= 2.8f; x += 0.8f)
            {
                CreateSprite(zebra.transform, $"Stripe_{x}", new Vector3(x, 196f, -0.01f), new Vector3(0.4f, 2.4f, 1f), Color.white, -7, square);
            }

            // Zone 5: Final Dashed Line (Y = 235 to 300)
            for (float y = 236f; y <= 298f; y += 4f)
            {
                CreateSprite(markings.transform, $"Dash_Z5_{y}", new Vector3(0f, y, 0f), new Vector3(0.18f, 2.2f, 1f), new Color(0.96f, 0.88f, 0.35f, 0.9f), -8, square);
            }

            // Finish Line Checkered Banner at Y = 300
            CreateSprite(env, "FinishBanner", new Vector3(0f, 300f, 0f), new Vector3(6.6f, 1.2f, 1f), new Color(0.95f, 0.95f, 0.95f, 1f), -6, square);

            // 6. Road Signs
            CreateRoadSigns(env);

            // 7. Scenery (Trees, Streetlamps, Barriers)
            CreateScenery(env, square, circle);
        }

        private static void CreateRoadSigns(Transform env)
        {
            GameObject signs = FindOrCreateChild(env, "RoadSigns");

            // Sign 1 (Y = 0): Overtaking Permitted
            CreateSignPost(signs.transform, "Sign_Permitted_M1", new Vector3(3.8f, 0f, 0f), "[OVERTAKE PERMITTED]", new Color(0.15f, 0.55f, 0.90f, 1f));

            // Sign 2 (Y = 56): No Overtaking (Red Prohibition)
            CreateSignPost(signs.transform, "Sign_NoOvertake_M2", new Vector3(3.8f, 56f, 0f), "[NO OVERTAKING]", new Color(0.85f, 0.20f, 0.20f, 1f));

            // Sign 3 (Y = 116): Blind Bend Ahead
            CreateSignPost(signs.transform, "Sign_BlindBend_M3", new Vector3(3.8f, 116f, 0f), "[! BLIND BEND]", new Color(0.95f, 0.65f, 0.10f, 1f));

            // Sign 4 (Y = 142): Hill Crest
            CreateSignPost(signs.transform, "Sign_HillCrest_M3", new Vector3(3.8f, 142f, 0f), "[! HILL CREST]", new Color(0.95f, 0.65f, 0.10f, 1f));

            // Sign 5 (Y = 184): Pedestrian Crossing
            CreateSignPost(signs.transform, "Sign_Crossing_M4", new Vector3(3.8f, 184f, 0f), "[PEDESTRIAN CROSSING]", new Color(0.15f, 0.55f, 0.90f, 1f));

            // Sign 6 (Y = 236): Overtaking Permitted
            CreateSignPost(signs.transform, "Sign_Permitted_M5", new Vector3(3.8f, 236f, 0f), "[OVERTAKE PERMITTED]", new Color(0.15f, 0.55f, 0.90f, 1f));

            // Finish Arch (Y = 300)
            CreateSignPost(signs.transform, "Sign_Finish", new Vector3(0f, 301f, 0f), "FINISH LINE", new Color(0.18f, 0.75f, 0.35f, 1f));
        }

        private static void CreateSignPost(Transform parent, string name, Vector3 pos, string text, Color badgeColor)
        {
            GameObject signObj = FindOrCreateChild(parent, name);
            signObj.transform.position = pos;

            // Post
            GameObject post = CreateSprite(signObj.transform, "Post", new Vector3(0f, -0.4f, 0f), new Vector3(0.12f, 0.9f, 1f), new Color(0.4f, 0.45f, 0.5f, 1f), 11, null);

            // Badge
            GameObject badge = CreateSprite(signObj.transform, "Badge", new Vector3(0f, 0.4f, 0f), new Vector3(1.6f, 0.8f, 1f), badgeColor, 12, null);

            // Text
            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(badge.transform, false);
            txtObj.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            TextMeshPro tmp = txtObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 2.4f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            if (txtObj.TryGetComponent<MeshRenderer>(out var mr)) mr.sortingOrder = 14;
            tmp.rectTransform.sizeDelta = new Vector2(1.8f, 0.8f);
        }

        private static void CreateScenery(Transform env, Sprite square, Sprite circle)
        {
            GameObject scenery = FindOrCreateChild(env, "Scenery");

            // Roadside Trees along both sides
            for (float y = -10f; y <= 310f; y += 12f)
            {
                // Skip intersections
                if (y >= 195f && y <= 215f) continue;

                // Left Tree
                GameObject tLeft = CreateSprite(scenery.transform, $"Tree_L_{y}", new Vector3(-6.8f, y, 0f), new Vector3(1.8f, 1.8f, 1f), new Color(0.18f, 0.45f, 0.22f, 1f), 5, circle);
                // Right Tree
                GameObject tRight = CreateSprite(scenery.transform, $"Tree_R_{y}", new Vector3(6.8f, y + 4f, 0f), new Vector3(1.8f, 1.8f, 1f), new Color(0.14f, 0.42f, 0.18f, 1f), 5, circle);
            }

            // Rock Berms along the blind bend (Y = 120 to 160)
            for (float y = 120f; y <= 165f; y += 8f)
            {
                CreateSprite(scenery.transform, $"Rock_L_{y}", new Vector3(-5.5f, y, 0f), new Vector3(1.4f, 2.2f, 1f), new Color(0.45f, 0.45f, 0.48f, 1f), 4, square);
                CreateSprite(scenery.transform, $"Rock_R_{y}", new Vector3(5.5f, y, 0f), new Vector3(1.4f, 2.2f, 1f), new Color(0.45f, 0.45f, 0.48f, 1f), 4, square);
            }
        }
        #endregion

        #region Player Car Construction
        private static GameObject BuildPlayerCar(Transform env)
        {
            GameObject car = new GameObject("PlayerCar");
            car.transform.SetParent(env, false);
            car.transform.position = new Vector3(1.5f, -5f, 0f); // Right lane
            car.transform.rotation = Quaternion.identity;
            car.tag = "Player";

            SpriteRenderer sr = car.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSpriteAsset(CarBlueTopDownPath);
            sr.color = Color.white;
            sr.sortingOrder = 10;
            car.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            Rigidbody2D rb = car.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D col = car.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.6f, 3.2f);

            Level3PlayerCar playerCar = car.AddComponent<Level3PlayerCar>();
            CarIndicatorController ind = car.AddComponent<CarIndicatorController>();

            return car;
        }
        #endregion

        #region UI Construction
        private static Level8UIController EnsureUI()
        {
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Level8_Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Remove legacy placeholder HUD to avoid duplicate HUD cards
            Transform oldHUD = canvas.transform.Find("HUD");
            if (oldHUD != null)
            {
                UnityEngine.Object.DestroyImmediate(oldHUD.gameObject);
            }

            Level8UIController ui = canvas.GetComponent<Level8UIController>();
            if (ui != null) return ui;

            ui = canvas.gameObject.AddComponent<Level8UIController>();

            // 1. Mission Card (Top-Left: 380 x 120)
            GameObject mCard = CreateUIPanel(canvas.transform, "MissionCard", new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(380f, 125f), new Color(0.08f, 0.12f, 0.18f, 0.94f));
            TMP_Text mBadge = CreateUIText(mCard.transform, "Badge", "MISSION 1 / 5", 13f, TextAlignmentOptions.Left, new Vector2(240f, 20f), new Vector2(14f, -12f), new Vector2(0f, 1f));
            TMP_Text mTitle = CreateUIText(mCard.transform, "Title", "KNOW YOUR ROAD", 16f, TextAlignmentOptions.Left, new Vector2(350f, 24f), new Vector2(14f, -34f), new Vector2(0f, 1f));
            TMP_Text mObj = CreateUIText(mCard.transform, "Objective", "Maintain safe distance and overtake only in the permitted dashed section.", 12f, TextAlignmentOptions.Left, new Vector2(350f, 44f), new Vector2(14f, -60f), new Vector2(0f, 1f));
            TMP_Text mChecklist = CreateUIText(mCard.transform, "Checklist", "", 11f, TextAlignmentOptions.Left, new Vector2(350f, 20f), new Vector2(14f, -104f), new Vector2(0f, 1f));

            // 2. Score Card (Top-Right: 340 x 115)
            GameObject sCard = CreateUIPanel(canvas.transform, "ScoreCard", new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(340f, 115f), new Color(0.08f, 0.12f, 0.18f, 0.94f));
            TMP_Text sVal = CreateUIText(sCard.transform, "ScoreText", "SCORE: 100", 16f, TextAlignmentOptions.Right, new Vector2(310f, 22f), new Vector2(-14f, -12f), new Vector2(1f, 1f));
            TMP_Text sfVal = CreateUIText(sCard.transform, "SafetyText", "SAFETY: 100%", 13.5f, TextAlignmentOptions.Left, new Vector2(140f, 20f), new Vector2(14f, -38f), new Vector2(0f, 1f));
            GameObject barTrough = CreateUIPanel(sCard.transform, "BarTrough", new Vector2(1f, 1f), new Vector2(-14f, -40f), new Vector2(150f, 12f), new Color(0.2f, 0.25f, 0.32f, 0.8f));
            GameObject barFillObj = CreateUIPanel(barTrough.transform, "BarFill", new Vector2(0f, 0f), Vector2.zero, new Vector2(150f, 12f), new Color(0.18f, 0.78f, 0.35f, 1f));
            Image barFill = barFillObj.GetComponent<Image>();
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillAmount = 1f;
            TMP_Text statsVal = CreateUIText(sCard.transform, "StatsText", "Overtakes: 0  |  Safe Waits: 0", 12f, TextAlignmentOptions.Left, new Vector2(310f, 20f), new Vector2(14f, -70f), new Vector2(0f, 1f));

            // 3. Overtake Status Pill (Top-Center: 320 x 48)
            GameObject pill = CreateUIPanel(canvas.transform, "OvertakeStatusPill", new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(320f, 48f), new Color(0.15f, 0.75f, 0.35f, 0.95f));
            Image pillBg = pill.GetComponent<Image>();
            TMP_Text pTitle = CreateUIText(pill.transform, "PillTitle", "[SAFE TO OVERTAKE]", 14.5f, TextAlignmentOptions.Center, new Vector2(300f, 20f), new Vector2(0f, -6f), new Vector2(0.5f, 1f));
            pTitle.fontStyle = FontStyles.Bold;
            TMP_Text pReason = CreateUIText(pill.transform, "PillReason", "Road Clear Ahead", 11.5f, TextAlignmentOptions.Center, new Vector2(300f, 18f), new Vector2(0f, -26f), new Vector2(0.5f, 1f));

            // 4. Controls Card (Bottom-Left: 270 x 65)
            GameObject ctrlCard = CreateUIPanel(canvas.transform, "ControlsCard", new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(270f, 65f), new Color(0.08f, 0.12f, 0.18f, 0.88f));
            CreateUIText(ctrlCard.transform, "CtrlTitle", "CONTROLS", 11.5f, TextAlignmentOptions.Left, new Vector2(240f, 16f), new Vector2(14f, -8f), new Vector2(0f, 1f));
            CreateUIText(ctrlCard.transform, "CtrlBody", "W/S : Drive/Brake | A/D : Steer\nQ / E : Turn Signals | H : Hazard", 11.5f, TextAlignmentOptions.Left, new Vector2(240f, 34f), new Vector2(14f, -26f), new Vector2(0f, 1f));

            // 5. Feedback Banner (Bottom-Center: 580 x 44, positioned at Y = 78 above the indicator HUD)
            GameObject fBanner = CreateUIPanel(canvas.transform, "FeedbackBanner", new Vector2(0.5f, 0f), new Vector2(0f, 78f), new Vector2(580f, 44f), new Color(0.06f, 0.09f, 0.14f, 0.94f));
            CanvasGroup fGroup = fBanner.AddComponent<CanvasGroup>();
            TMP_Text fText = CreateUIText(fBanner.transform, "Text", "Observe road conditions before overtaking.", 13.5f, TextAlignmentOptions.Center, new Vector2(560f, 32f), Vector2.zero, new Vector2(0.5f, 0.5f));

            // 6. Completion Modal (Overlay)
            GameObject compModal = CreateUIPanel(canvas.transform, "CompletionModal", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 360f), new Color(0.06f, 0.09f, 0.14f, 0.98f));
            CanvasGroup compGroup = compModal.AddComponent<CanvasGroup>();
            TMP_Text cTitle = CreateUIText(compModal.transform, "Title", "LEVEL 8 COMPLETED!", 24f, TextAlignmentOptions.Center, new Vector2(480f, 36f), new Vector2(0f, -25f), new Vector2(0.5f, 1f));
            cTitle.fontStyle = FontStyles.Bold;
            TMP_Text cSub = CreateUIText(compModal.transform, "Sub", "Master of Safe Overtaking", 14f, TextAlignmentOptions.Center, new Vector2(480f, 22f), new Vector2(0f, -62f), new Vector2(0.5f, 1f));
            TMP_Text cStars = CreateUIText(compModal.transform, "Stars", "★★★", 28f, TextAlignmentOptions.Center, new Vector2(480f, 36f), new Vector2(0f, -90f), new Vector2(0.5f, 1f));
            cStars.color = new Color(1f, 0.82f, 0.15f, 1f);
            TMP_Text cScore = CreateUIText(compModal.transform, "Score", "FINAL SCORE: 145", 18f, TextAlignmentOptions.Center, new Vector2(480f, 26f), new Vector2(0f, -135f), new Vector2(0.5f, 1f));
            TMP_Text cStats = CreateUIText(compModal.transform, "Stats", "Safety: 100% | Overtakes: 4 | Safe Waits: 2", 13.5f, TextAlignmentOptions.Center, new Vector2(480f, 40f), new Vector2(0f, -170f), new Vector2(0.5f, 1f));

            Button replayBtn = CreateUIButton(compModal.transform, "ReplayBtn", "REPLAY", new Vector2(-150f, 40f), new Vector2(130f, 42f), new Color(0.2f, 0.28f, 0.4f, 1f));
            Button nextBtn = CreateUIButton(compModal.transform, "NextBtn", "NEXT LEVEL", new Vector2(0f, 40f), new Vector2(140f, 42f), new Color(0.18f, 0.72f, 0.35f, 1f));
            Button menuBtn = CreateUIButton(compModal.transform, "MenuBtn", "MAIN MENU", new Vector2(150f, 40f), new Vector2(130f, 42f), new Color(0.2f, 0.28f, 0.4f, 1f));
            compModal.SetActive(false);

            // Bind Fields via Reflection
            SetField(ui, "missionBadge", mBadge);
            SetField(ui, "missionTitle", mTitle);
            SetField(ui, "missionObjective", mObj);
            SetField(ui, "missionChecklistText", mChecklist);

            SetField(ui, "scoreText", sVal);
            SetField(ui, "safetyText", sfVal);
            SetField(ui, "statsText", statsVal);
            SetField(ui, "safetyBarFill", barFill);

            SetField(ui, "statusPillRoot", pill);
            SetField(ui, "statusPillBg", pillBg);
            SetField(ui, "statusTitleText", pTitle);
            SetField(ui, "statusReasonText", pReason);

            SetField(ui, "feedbackBanner", fBanner);
            SetField(ui, "feedbackGroup", fGroup);
            SetField(ui, "feedbackText", fText);

            SetField(ui, "controlsCard", ctrlCard);

            SetField(ui, "completionModal", compModal);
            SetField(ui, "completionGroup", compGroup);
            SetField(ui, "completionTitle", cTitle);
            SetField(ui, "completionSubtitle", cSub);
            SetField(ui, "completionStars", cStars);
            SetField(ui, "completionScore", cScore);
            SetField(ui, "completionStats", cStats);
            SetField(ui, "replayButton", replayBtn);
            SetField(ui, "nextLevelButton", nextBtn);
            SetField(ui, "menuButton", menuBtn);

            return ui;
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = color;

            Sprite rounded = LoadSpriteAsset(RoundedPanelPath);
            if (rounded != null)
            {
                img.sprite = rounded;
                img.type = Image.Type.Sliced;
            }

            return obj;
        }

        private static TMP_Text CreateUIText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align, Vector2 size, Vector2 pos, Vector2 pivot)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;

            RectTransform rt = tmp.rectTransform;
            rt.pivot = pivot;
            rt.anchorMin = pivot;
            rt.anchorMax = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            return tmp;
        }

        private static Button CreateUIButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            Sprite rounded = LoadSpriteAsset(RoundedPanelPath);
            if (rounded != null)
            {
                img.sprite = rounded;
                img.type = Image.Type.Sliced;
            }

            Button btn = obj.AddComponent<Button>();

            TMP_Text txt = CreateUIText(obj.transform, "Label", text, 14f, TextAlignmentOptions.Center, size, Vector2.zero, new Vector2(0.5f, 0.5f));
            txt.fontStyle = FontStyles.Bold;
            txt.raycastTarget = false;

            return btn;
        }

        private static void EnsureIntroPanel(Level3PlayerCar playerCar)
        {
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            Transform introT = canvas.transform.Find("LevelIntroPanel") ?? canvas.transform.Find("IntroPanel");
            GameObject introPanel = introT != null ? introT.gameObject : null;

            if (introPanel == null)
            {
                introPanel = CreateUIPanel(canvas.transform, "LevelIntroPanel", Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.04f, 0.06f, 0.10f, 0.88f));
                SetFullScreen(introPanel.GetComponent<RectTransform>());
            }

            Transform cardT = introPanel.transform.Find("IntroCard");
            GameObject cardObj = cardT != null ? cardT.gameObject : null;
            if (cardObj == null)
            {
                cardObj = CreateUIPanel(introPanel.transform, "IntroCard", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 400f), new Color(0.96f, 0.97f, 0.98f, 1f));
            }
            else
            {
                RectTransform crt = cardObj.GetComponent<RectTransform>();
                crt.sizeDelta = new Vector2(500f, 400f);
                crt.anchoredPosition = Vector2.zero;
            }

            // Title Text
            Transform titleT = cardObj.transform.Find("Title");
            TMP_Text titleText = titleT != null ? titleT.GetComponent<TMP_Text>() : null;
            if (titleText == null)
            {
                titleText = CreateUIText(cardObj.transform, "Title", "", 24f, TextAlignmentOptions.Center, new Vector2(460f, 36f), new Vector2(0f, 155f), new Vector2(0.5f, 0.5f));
            }
            else
            {
                RectTransform trt = titleText.rectTransform;
                trt.anchoredPosition = new Vector2(0f, 155f);
                trt.sizeDelta = new Vector2(460f, 36f);
            }
            titleText.text = "🚗 THE OVERTAKE";
            titleText.color = new Color(0.10f, 0.15f, 0.25f, 1f);
            titleText.fontStyle = FontStyles.Bold;
            titleText.raycastTarget = false;

            // Message Text
            Transform msgT = cardObj.transform.Find("Message") ?? cardObj.transform.Find("Body");
            TMP_Text msgText = msgT != null ? msgT.GetComponent<TMP_Text>() : null;
            if (msgText == null)
            {
                msgText = CreateUIText(cardObj.transform, "Message", "", 13f, TextAlignmentOptions.Left, new Vector2(440f, 220f), new Vector2(0f, 20f), new Vector2(0.5f, 0.5f));
            }
            else
            {
                RectTransform mrt = msgText.rectTransform;
                mrt.anchoredPosition = new Vector2(0f, 20f);
                mrt.sizeDelta = new Vector2(440f, 220f);
            }
            msgText.text = "Don't just overtake. Overtake safely.\n\n" +
                           "1. Check Road Markings: Dashed lines allow passing; solid lines strictly prohibit it.\n" +
                           "2. Judge Oncoming Traffic: Wait behind lead vehicles until oncoming lanes are completely clear.\n" +
                           "3. Prohibited Zones: NEVER overtake on blind bends, crests, or crossings.\n" +
                           "4. Signal Your Intentions: Press Q before pulling out, and E before returning.\n" +
                           "5. Patience Saves Lives: Waiting safely behind a slower vehicle is often the smartest move!";
            msgText.color = new Color(0.18f, 0.22f, 0.28f, 1f);
            msgText.raycastTarget = false;

            // GotItButton
            Transform btnT = cardObj.transform.Find("GotItButton");
            Button gotItBtn = btnT != null ? btnT.GetComponent<Button>() : null;
            if (gotItBtn == null)
            {
                gotItBtn = CreateUIButton(cardObj.transform, "GotItButton", "GOT IT!", new Vector2(0f, -150f), new Vector2(200f, 48f), new Color(0.12f, 0.55f, 0.84f, 1f));
            }
            else
            {
                RectTransform brt = gotItBtn.GetComponent<RectTransform>();
                brt.anchoredPosition = new Vector2(0f, -150f);
                brt.sizeDelta = new Vector2(200f, 48f);
            }
            gotItBtn.interactable = true;

            // Crucial: ensure GotItButton's own graphic receives raycasts and child labels do NOT block them!
            Graphic btnGraphic = gotItBtn.GetComponent<Graphic>();
            if (btnGraphic != null) btnGraphic.raycastTarget = true;

            Graphic[] childGraphics = gotItBtn.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < childGraphics.Length; i++)
            {
                if (childGraphics[i].gameObject != gotItBtn.gameObject)
                {
                    childGraphics[i].raycastTarget = false;
                }
            }

            // Attach / configure LevelIntroController
            LevelIntroController introCtrl = GetOrAdd<LevelIntroController>(introPanel);
            SetField(introCtrl, "introPanel", introPanel);
            SetField(introCtrl, "gotItButton", gotItBtn);
            SetField(introCtrl, "titleText", titleText);
            SetField(introCtrl, "messageText", msgText);

            gotItBtn.onClick.RemoveAllListeners();
            gotItBtn.onClick.AddListener(() =>
            {
                introPanel.SetActive(false);
                Time.timeScale = 1f;
                GameManager.Instance?.SetState(GameState.Playing);
                if (playerCar != null) playerCar.SetControlEnabled(true);
            });

            introPanel.SetActive(true);
            introPanel.transform.SetAsLastSibling();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Paused);
            }
            else
            {
                Time.timeScale = 0f;
            }
        }

        private static void CleanLegacyElements()
        {
            // Clean old placeholder traffic spawner
            GameObject oldTraffic = GameObject.Find("Traffic");
            if (oldTraffic != null && oldTraffic.transform.Find("TopLane") != null)
            {
                UnityEngine.Object.DestroyImmediate(oldTraffic);
            }

            // Clean old emergency vehicle controller if present
            var allMBs = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allMBs.Length; i++)
            {
                if (allMBs[i] != null && allMBs[i].GetType().Name == "EmergencyVehicleController")
                {
                    UnityEngine.Object.DestroyImmediate(allMBs[i].gameObject);
                }
            }
        }

        private static void SetFullScreen(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }
        #endregion

        #region Helpers
        private static GameObject FindOrCreate(string name)
        {
            GameObject obj = GameObject.Find(name);
            return obj != null ? obj : new GameObject(name);
        }

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) return t.gameObject;
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            if (!go.TryGetComponent<T>(out var comp)) comp = go.AddComponent<T>();
            return comp;
        }

        private static GameObject CreateSprite(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int sortingOrder, Sprite sprite)
        {
            GameObject obj = FindOrCreateChild(parent, name);
            obj.transform.position = pos;
            obj.transform.localScale = scale;

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite ?? LoadSpriteAsset(WorldSquareSpritePath);
            sr.color = color;
            sr.sortingOrder = sortingOrder;

            return obj;
        }

        private static Sprite LoadSpriteAsset(string path)
        {
            #if UNITY_EDITOR
            Sprite loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (loaded != null) return loaded;
            #endif
            return Resources.Load<Sprite>(path);
        }

        private static void SetField(object target, string fieldName, object val)
        {
            var f = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (f != null) f.SetValue(target, val);
        }
        #endregion
    }
}
