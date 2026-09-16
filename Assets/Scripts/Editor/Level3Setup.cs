#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using TrafficTown2D.Core;
using TrafficTown2D.Gameplay;
using TrafficTown2D.Player;
using TrafficTown2D.Traffic;
using TrafficTown2D.UI;

namespace TrafficTown2D.Editor
{
    public static class Level3Setup
    {
        private const string Level3ScenePath = "Assets/Scenes/Level3.unity";
        private const string WorldSquareSpritePath = "Assets/Sprites/Generated/WorldSquare.png";
        private const string WorldCircleSpritePath = "Assets/Sprites/Generated/WorldCircle.png";
        private const string TrafficLightBodySpritePath = "Assets/Sprites/TrafficLightBody.png";
        private const string RoundedPanelSpritePath = "Assets/UI/RoundedPanel.png";
        private const string CarBluePrefabPath = "Assets/Prefabs/CarBlue.prefab";
        private const string CarRedPrefabPath = "Assets/Prefabs/CarRed.prefab";
        private const string CarYellowPrefabPath = "Assets/Prefabs/CarYellow.prefab";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

        private static readonly Color RoadColor = new Color(0.10f, 0.11f, 0.13f, 1f);
        private static readonly Color RoadEdgeColor = new Color(0.92f, 0.88f, 0.62f, 1f);
        private static readonly Color LaneMarkColor = new Color(1f, 0.94f, 0.45f, 1f);
        private static readonly Color SidewalkColor = new Color(0.67f, 0.72f, 0.69f, 1f);
        private static readonly Color SidewalkTileColor = new Color(0.78f, 0.82f, 0.79f, 1f);
        private static readonly Color CrossingColor = new Color(0.98f, 0.96f, 0.84f, 1f);
        private static readonly Color TrafficLightBodyColor = new Color(0.04f, 0.045f, 0.05f, 1f);

        [MenuItem("TrafficTown/Setup Level 3")]
        public static void SetupLevel3()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before running TrafficTown -> Setup Level 3.");
                return;
            }

            EnsureAssetFolders();

            Scene level3Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(level3Scene, Level3ScenePath);

            EnsureCamera();
            EnsureGlobalLight();

            // --- Clean Up Any Old Menu or Level 1 Scene Artifacts ---
            CleanOldSceneArtifacts();

            // --- Services ---
            GameObject services = FindOrCreate("Services");
            SceneLoader sceneLoader = GetOrAdd<SceneLoader>(services);
            GameManager gm = GetOrAdd<GameManager>(services);
            gm.SetState(GameState.Playing);
            Time.timeScale = 1f;
            EnsureEventSystem();

            // --- Environment ---
            GameObject environment = FindOrCreate("Environment");
            CreateTownBackground(environment.transform);
            CreateRoadScene(environment.transform);
            CreateCrossing(environment.transform);
            CreateTrafficSigns(environment.transform);

            // --- Traffic: spawned cars that approach the crosswalk ---
            GameObject traffic = FindOrCreate("Traffic");
            CreateOncomingTrafficLanes(traffic.transform);

            // --- Pedestrian ---
            GameObject pedestrianGO = FindOrCreateChild(FindOrCreate("Pedestrians").transform, "Pedestrian");
            pedestrianGO.transform.position = new Vector3(-0.5f, -3.3f, 0f);
            Rigidbody2D pedBody = GetOrAdd<Rigidbody2D>(pedestrianGO);
            pedBody.gravityScale = 0f;
            pedBody.freezeRotation = true;
            GetOrAdd<BoxCollider2D>(pedestrianGO);
            CreateWorldSprite(pedestrianGO.transform, "Body", Vector3.zero, new Vector3(0.5f, 0.85f, 1f), new Color(0.2f, 0.5f, 0.9f, 1f), 5, false);
            CreateWorldSprite(pedestrianGO.transform, "Head", new Vector3(0f, 0.58f, 0f), new Vector3(0.38f, 0.38f, 1f), new Color(0.95f, 0.78f, 0.60f, 1f), 5, true);
            PedestrianAI pedestrianAI = GetOrAdd<PedestrianAI>(pedestrianGO);

            // Pedestrian destination: on the opposite sidewalk
            GameObject pedDest = FindOrCreateChild(FindOrCreate("Pedestrians").transform, "PedestrianDestination");
            pedDest.transform.position = new Vector3(-0.5f, 3.3f, 0f);

            // --- Player Car ---
            GameObject playerCar = CreatePlayerCar();
            CarPlayerController carController = GetOrAdd<CarPlayerController>(playerCar);

            // --- Gameplay Controller ---
            GameObject gameplay = FindOrCreate("Gameplay");
            ScoreManager score = GetOrAdd<ScoreManager>(FindOrCreateChild(gameplay.transform, "ScoreManager"));

            // Crossing zone trigger
            GameObject crossingZoneGO = FindOrCreateChild(environment.transform, "ZebraCrossing");
            BoxCollider2D crossingCollider = crossingZoneGO.GetComponent<BoxCollider2D>();
            if (crossingCollider == null) crossingCollider = crossingZoneGO.AddComponent<BoxCollider2D>();
            crossingCollider.isTrigger = true;
            crossingCollider.size = new Vector2(3.8f, 4.3f);

            // --- UI ---
            LevelUIController ui = CreateUI(score, sceneLoader);
            FeedbackController feedbackController = Object.FindAnyObjectByType<FeedbackController>(FindObjectsInactive.Include);

            // --- Wire YieldingController ---
            YieldingController yieldCtrl = GetOrAdd<YieldingController>(playerCar);
            SetReference(yieldCtrl, "playerCar", carController);
            SetReference(yieldCtrl, "pedestrian", pedestrianAI);
            SetReference(yieldCtrl, "pedestrianDestination", pedDest.transform);
            SetReference(yieldCtrl, "crossingZone", crossingZoneGO.transform);
            SetReference(yieldCtrl, "feedback", feedbackController);
            SetReference(yieldCtrl, "levelUI", ui);

            // --- Wire pedestrian destination ---
            SerializedObject pedSO = new SerializedObject(pedestrianAI);
            SerializedProperty destProp = pedSO.FindProperty("targetPoint");
            if (destProp != null) { destProp.objectReferenceValue = pedDest.transform; pedSO.ApplyModifiedPropertiesWithoutUndo(); }

            EnsureBuildSettingsScenes();

            EditorSceneManager.MarkSceneDirty(level3Scene);
            EditorSceneManager.SaveScene(level3Scene);
            Selection.activeGameObject = playerCar;
            Debug.Log("TrafficTown Level 3 setup completed successfully without menu glitches!");
        }

        private static void CleanOldSceneArtifacts()
        {
            // Destroy any MainMenuController component or object
            MainMenuController[] mmcs = Object.FindObjectsByType<MainMenuController>(FindObjectsInactive.Include);
            for (int i = 0; i < mmcs.Length; i++)
            {
                if (mmcs[i] != null) Undo.DestroyObjectImmediate(mmcs[i]);
            }

            // Destroy MainMenu Services
            GameObject mainMenuServices = GameObject.Find("MainMenu Services");
            if (mainMenuServices != null) Undo.DestroyObjectImmediate(mainMenuServices);

            // Destroy old pedestrian player if present (name is "Player")
            GameObject oldPlayer = GameObject.Find("Player");
            if (oldPlayer != null && oldPlayer.GetComponent<CarPlayerController>() == null)
            {
                Undo.DestroyObjectImmediate(oldPlayer);
            }

            // Destroy any LevelIntroPanel or LevelIntroController (no menu needed in Level 3)
            LevelIntroController[] introControllers = Object.FindObjectsByType<LevelIntroController>(FindObjectsInactive.Include);
            for (int i = 0; i < introControllers.Length; i++)
            {
                if (introControllers[i] != null) Undo.DestroyObjectImmediate(introControllers[i].gameObject);
            }
            GameObject introPanel = GameObject.Find("LevelIntroPanel");
            if (introPanel != null) Undo.DestroyObjectImmediate(introPanel);

            // Clean menu buttons/titles if they exist on Canvas
            string[] menuGarbageNames = { "PlayButton", "LearnButton", "QuizButton", "SettingsButton", "ExitButton", "Title", "Subtitle", "Message", "Background" };
            foreach (string gName in menuGarbageNames)
            {
                GameObject g = GameObject.Find(gName);
                if (g != null && g.transform.parent != null && (g.transform.parent.name.Contains("Canvas") || g.transform.parent.name == "UI"))
                {
                    Undo.DestroyObjectImmediate(g);
                }
            }
        }

        // ─── Player Car ────────────────────────────────────────────────────────────
        private static GameObject CreatePlayerCar()
        {
            GameObject car = GameObject.Find("PlayerCar");
            if (car == null)
            {
                // Try to use a car prefab as a base
                GameObject carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarBluePrefabPath);
                if (carPrefab != null)
                {
                    car = PrefabUtility.InstantiatePrefab(carPrefab) as GameObject;
                    car.name = "PlayerCar";
                    // Remove VehicleController if present — player drives this
                    VehicleController vc = car.GetComponent<VehicleController>();
                    if (vc != null) Object.DestroyImmediate(vc, true);
                }
                else
                {
                    car = new GameObject("PlayerCar");
                    CreateWorldSprite(car.transform, "CarBody", Vector3.zero, new Vector3(1.6f, 0.8f, 1f), new Color(0.2f, 0.5f, 0.9f, 1f), 5, false);
                    CreateWorldSprite(car.transform, "CarTop", new Vector3(0f, 0.35f, -0.01f), new Vector3(1.0f, 0.55f, 1f), new Color(0.15f, 0.40f, 0.75f, 1f), 6, false);
                    CreateWorldSprite(car.transform, "WheelFL", new Vector3(-0.55f, -0.42f, -0.02f), new Vector3(0.3f, 0.3f, 1f), new Color(0.1f, 0.1f, 0.1f, 1f), 7, true);
                    CreateWorldSprite(car.transform, "WheelFR", new Vector3(0.55f, -0.42f, -0.02f), new Vector3(0.3f, 0.3f, 1f), new Color(0.1f, 0.1f, 0.1f, 1f), 7, true);
                }
            }

            car.transform.position = new Vector3(-7f, -1.1f, 0f);
            car.tag = "Player";

            Rigidbody2D body = GetOrAdd<Rigidbody2D>(car);
            body.gravityScale = 0f;
            body.freezeRotation = true;

            BoxCollider2D col = GetOrAdd<BoxCollider2D>(car);
            col.size = new Vector2(1.5f, 0.75f);

            return car;
        }

        // ─── Traffic Lanes ─────────────────────────────────────────────────────────
        private static void CreateOncomingTrafficLanes(Transform parent)
        {
            VehicleController bluePrefab  = AssetDatabase.LoadAssetAtPath<VehicleController>(CarBluePrefabPath);
            VehicleController redPrefab   = AssetDatabase.LoadAssetAtPath<VehicleController>(CarRedPrefabPath);
            VehicleController yellowPrefab = AssetDatabase.LoadAssetAtPath<VehicleController>(CarYellowPrefabPath);
            VehicleController[] prefabs = new[] { bluePrefab, redPrefab, yellowPrefab };

            // Top lane: cars travel left→right (direction +1), player is in bottom lane
            GameObject topLane = FindOrCreateChild(parent, "TopLane");
            Transform topSpawn = CreateVisual(topLane.transform, "TopSpawnPoint",   new Vector3(-9.5f,  1.1f, 0f), new Vector3(0.2f, 0.2f, 0.2f), Color.clear).transform;
            Transform topStop  = CreateVisual(topLane.transform, "TopStopPoint",    new Vector3(-2.8f,  1.1f, -0.1f), new Vector3(0.15f, 0.15f, 0.15f), Color.clear).transform;
            Transform topExit  = CreateVisual(topLane.transform, "TopExitPoint",    new Vector3( 9.5f,  1.1f, -0.1f), new Vector3(0.15f, 0.15f, 0.15f), Color.clear).transform;

            VehicleSpawner topSpawner = GetOrAdd<VehicleSpawner>(FindOrCreateChild(topLane.transform, "TopSpawner"));
            SetReference(topSpawner, "vehiclePrefab", prefabs.Length > 0 ? prefabs[0] : null);
            SetObjectArray(topSpawner, "vehiclePrefabs", prefabs);
            SetReference(topSpawner, "spawnPoint",   topSpawn);
            SetReference(topSpawner, "carStopPoint", topStop);
            SetReference(topSpawner, "carExitPoint", topExit);
            SetFloat(topSpawner, "spawnInterval",          4f);
            SetInt  (topSpawner, "maximumActiveVehicles",  3);
            SetFloat(topSpawner, "minVehicleSpeed",        2.5f);
            SetFloat(topSpawner, "maxVehicleSpeed",        4.0f);
            SetFloat(topSpawner, "travelDirection",        1f);
        }

        // ─── Environment ───────────────────────────────────────────────────────────
        private static void CreateTownBackground(Transform parent)
        {
            GameObject bg = ResetVisualGroup(parent, "TownBackground");
            CreateWorldSprite(bg.transform, "Ground", new Vector3(0f, 0f, 5f), new Vector3(22f, 12f, 1f), new Color(0.85f, 0.92f, 0.88f, 1f), -10, false);
            // Suburbs – slightly warmer tones than Level 2
            CreateWorldSprite(bg.transform, "BuildingA", new Vector3(-7f,   4.9f, 2f), new Vector3(3.6f, 2.4f, 1f), new Color(0.88f, 0.84f, 0.76f, 1f), -5, false);
            CreateWorldSprite(bg.transform, "BuildingB", new Vector3(-2.5f, 5.3f, 2f), new Vector3(3.0f, 3.0f, 1f), new Color(0.76f, 0.83f, 0.90f, 1f), -5, false);
            CreateWorldSprite(bg.transform, "BuildingC", new Vector3( 2.5f, 5.0f, 2f), new Vector3(4.0f, 2.8f, 1f), new Color(0.90f, 0.82f, 0.80f, 1f), -5, false);
            CreateWorldSprite(bg.transform, "BuildingD", new Vector3( 7.5f, 4.7f, 2f), new Vector3(3.2f, 2.2f, 1f), new Color(0.82f, 0.88f, 0.78f, 1f), -5, false);

            CreateStreetLamp(bg.transform, "LampLeft",   new Vector3(-6f, 2.6f, 0f));
            CreateStreetLamp(bg.transform, "LampCenter", new Vector3( 0f, 2.6f, 0f));
            CreateStreetLamp(bg.transform, "LampRight",  new Vector3( 6f, 2.6f, 0f));
        }

        private static void CreateStreetLamp(Transform parent, string name, Vector3 position)
        {
            GameObject lamp = FindOrCreateChild(parent, name);
            lamp.transform.localPosition = position;
            CreateWorldSprite(lamp.transform, "Pole", new Vector3(0f, 0.35f, 0f), new Vector3(0.08f, 0.9f, 1f), new Color(0.25f, 0.28f, 0.32f, 1f), -1, false);
            CreateWorldSprite(lamp.transform, "Head", new Vector3(0f, 0.85f, 0f), new Vector3(0.3f, 0.15f, 1f), new Color(0.95f, 0.88f, 0.45f, 1f),  0, false);
        }

        private static void CreateRoadScene(Transform parent)
        {
            GameObject road = ResetVisualGroup(parent, "Road");
            CreateWorldSprite(road.transform, "RoadBase",   Vector3.zero,              new Vector3(22f, 4.4f, 1f), RoadColor,     0, false);
            CreateWorldSprite(road.transform, "TopEdge",    new Vector3(0f,  2.15f, -0.01f), new Vector3(22f, 0.1f, 1f), RoadEdgeColor, 1, false);
            CreateWorldSprite(road.transform, "BottomEdge", new Vector3(0f, -2.15f, -0.01f), new Vector3(22f, 0.1f, 1f), RoadEdgeColor, 1, false);

            GameObject centerLine = FindOrCreateChild(road.transform, "CenterLine");
            ClearChildren(centerLine.transform);
            for (float x = -10f; x <= 10f; x += 1.2f)
                CreateWorldSprite(centerLine.transform, "Dash_" + x.ToString("F1"), new Vector3(x, 0f, -0.01f), new Vector3(0.7f, 0.08f, 1f), LaneMarkColor, 1, false);

            CreateSidewalk(road.transform, "SidewalkTop",    new Vector3(0f,  3.25f, 0f));
            CreateSidewalk(road.transform, "SidewalkBottom", new Vector3(0f, -3.25f, 0f));
        }

        private static void CreateSidewalk(Transform parent, string name, Vector3 position)
        {
            GameObject sw = FindOrCreateChild(parent, name);
            sw.transform.localPosition = position;
            CreateWorldSprite(sw.transform, "Base", Vector3.zero, new Vector3(22f, 1.9f, 1f), SidewalkColor, -2, false);
            for (float x = -10f; x <= 10f; x += 1.2f)
                CreateWorldSprite(sw.transform, "Tile_" + x.ToString("F1"), new Vector3(x, 0f, -0.01f), new Vector3(1.1f, 1.8f, 1f), SidewalkTileColor, -1, false);
        }

        private static void CreateCrossing(Transform parent)
        {
            GameObject zebra = FindOrCreateChild(parent, "ZebraCrossing");
            ClearChildren(zebra.transform);
            zebra.transform.position = new Vector3(-0.5f, 0f, -0.05f);
            BoxCollider2D col = GetOrAdd<BoxCollider2D>(zebra);
            col.isTrigger = true;
            col.size   = new Vector2(3.8f, 4.3f);
            col.offset = Vector2.zero;
            for (float x = -1.6f; x <= 1.6f; x += 0.8f)
                CreateWorldSprite(zebra.transform, "Stripe_" + x.ToString("F1"), new Vector3(x, 0f, 0f), new Vector3(0.45f, 4.3f, 1f), CrossingColor, 2, false);
        }

        private static void CreateTrafficSigns(Transform parent)
        {
            GameObject signs = ResetVisualGroup(parent, "TrafficSigns");
            // Yield sign on player's side (bottom left of crossing)
            CreateYieldSign(signs.transform, "YieldSign", new Vector3(-3f, -2.8f, 0f));
            // Pedestrian crossing diamond sign
            CreateCrossingSign(signs.transform, "CrossingSign", new Vector3(-2f, 2.6f, 0f));
        }

        private static void CreateYieldSign(Transform parent, string name, Vector3 position)
        {
            GameObject sign = FindOrCreateChild(parent, name);
            ClearChildren(sign.transform);
            sign.transform.localPosition = position;
            CreateWorldSprite(sign.transform, "Post", new Vector3(0f, -0.45f, 0f),  new Vector3(0.08f, 0.9f, 1f), new Color(0.29f, 0.31f, 0.32f, 1f), 12, false);
            CreateWorldSprite(sign.transform, "Base", new Vector3(0f, -0.92f, 0f),  new Vector3(0.5f, 0.08f, 1f), new Color(0.29f, 0.31f, 0.32f, 1f), 12, false);
            // Inverted triangle shape simulated with a rotated diamond
            GameObject face = CreateWorldSprite(sign.transform, "Face",  new Vector3(0f, 0.2f, 0f), new Vector3(0.74f, 0.74f, 1f), new Color(0.85f, 0.12f, 0.12f, 1f), 13, false);
            face.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            CreateWorldSprite(sign.transform, "InnerWhite", new Vector3(0f, 0.2f, -0.01f), new Vector3(0.60f, 0.60f, 1f), Color.white, 14, false).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private static void CreateCrossingSign(Transform parent, string name, Vector3 position)
        {
            GameObject sign = FindOrCreateChild(parent, name);
            ClearChildren(sign.transform);
            sign.transform.localPosition = position;
            CreateWorldSprite(sign.transform, "Post", new Vector3(0f, -0.45f, 0f), new Vector3(0.08f, 0.9f, 1f), new Color(0.29f, 0.31f, 0.32f, 1f), 12, false);
            CreateWorldSprite(sign.transform, "Base", new Vector3(0f, -0.92f, 0f), new Vector3(0.5f, 0.08f, 1f), new Color(0.29f, 0.31f, 0.32f, 1f), 12, false);
            GameObject face = CreateWorldSprite(sign.transform, "Face",  new Vector3(0f, 0.2f, 0f), new Vector3(0.72f, 0.72f, 1f), new Color(0.12f, 0.45f, 0.85f, 1f), 13, false);
            face.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            CreateWorldSprite(sign.transform, "PedestrianHead", new Vector3(0f,  0.34f, -0.02f), new Vector3(0.12f, 0.12f, 1f), Color.white, 15, true);
            CreateWorldSprite(sign.transform, "PedestrianBody", new Vector3(0f,  0.20f, -0.02f), new Vector3(0.06f, 0.24f, 1f), Color.white, 15, false);
        }

        // ─── UI ────────────────────────────────────────────────────────────────────
        private static LevelUIController CreateUI(ScoreManager score, SceneLoader sceneLoader)
        {
            EnsureEventSystem();
            Sprite roundedPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPanelSpritePath);

            // Clean up any MainMenuController on any Canvas
            Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Canvas canvas = null;
            for (int i = 0; i < allCanvases.Length; i++)
            {
                MainMenuController mmc = allCanvases[i].GetComponent<MainMenuController>();
                if (mmc != null)
                {
                    Undo.DestroyObjectImmediate(mmc);
                }
                if (canvas == null)
                {
                    canvas = allCanvases[i];
                }
            }

            if (canvas == null)
            {
                GameObject canvasGO = FindOrCreate("UI");
                canvas = GetOrAdd<Canvas>(canvasGO);
            }

            canvas.name = "UI";
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvas.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            GetOrAdd<GraphicRaycaster>(canvas.gameObject);

            // Remove any old non-HUD children on Canvas (e.g. Background, buttons from Main Menu)
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.transform.GetChild(i);
                if (child.name != "HUD")
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            GameObject hud = FindOrCreateChild(canvas.transform, "HUD");
            RectTransform hudRect = EnsureRectTransform(hud);
            hud.transform.SetAsFirstSibling();
            SetRect(hudRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Destroy any existing LevelIntroPanel inside HUD (no menu needed in Level 3)
            Transform existingIntro = hud.transform.Find("LevelIntroPanel");
            if (existingIntro != null)
            {
                Undo.DestroyObjectImmediate(existingIntro.gameObject);
            }

            GameObject gameplayHud = FindOrCreateChild(hud.transform, "GameplayHUD");
            SetRect(EnsureRectTransform(gameplayHud), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Mission Card
            GameObject missionCard = CreateUIPanel(gameplayHud.transform, "MissionCard", new Color(0.10f, 0.15f, 0.22f, 0.88f), roundedPanelSprite);
            SetRect(missionCard.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(170f, -48f), new Vector2(320f, 68f));
            CreateUIText(missionCard.transform, "Title", "MISSION", 13, TextAlignmentOptions.Left, new Vector2(290f, 20f), new Vector2(10f, 16f), new Vector2(0f, 0.5f)).color = new Color(0.55f, 0.76f, 0.98f, 1f);
            TextMeshProUGUI objective = CreateUIText(missionCard.transform, "Objective", "Drive forward and YIELD to pedestrians at the crosswalk!", 14, TextAlignmentOptions.Left, new Vector2(290f, 32f), new Vector2(10f, -10f), new Vector2(0f, 0.5f));
            objective.color = Color.white;

            // Score Card
            GameObject scoreCard = CreateUIPanel(gameplayHud.transform, "ScoreCard", new Color(0.10f, 0.15f, 0.22f, 0.88f), roundedPanelSprite);
            SetRect(scoreCard.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-110f, -48f), new Vector2(180f, 68f));
            CreateUIText(scoreCard.transform, "Title", "⭐ SCORE", 13, TextAlignmentOptions.Right, new Vector2(150f, 20f), new Vector2(-10f, 16f), new Vector2(1f, 0.5f)).color = new Color(1f, 0.82f, 0.28f, 1f);
            TextMeshProUGUI scoreText = CreateUIText(scoreCard.transform, "Value", "100", 24, TextAlignmentOptions.Right, new Vector2(150f, 32f), new Vector2(-10f, -10f), new Vector2(1f, 0.5f));
            scoreText.color = Color.white;
            scoreText.fontStyle = FontStyles.Bold;

            // Feedback Banner
            GameObject feedbackBanner = CreateUIPanel(hud.transform, "FeedbackBanner", new Color(0.08f, 0.12f, 0.18f, 0.92f), roundedPanelSprite);
            SetRect(feedbackBanner.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(560f, 54f));
            CanvasGroup feedbackGroup = GetOrAdd<CanvasGroup>(feedbackBanner);
            FeedbackController feedbackCtrl = GetOrAdd<FeedbackController>(feedbackBanner);
            TextMeshProUGUI feedbackText = CreateUIText(feedbackBanner.transform, "Message", string.Empty, 16, TextAlignmentOptions.Center, new Vector2(530f, 40f), Vector2.zero, new Vector2(0.5f, 0.5f));
            feedbackText.color = Color.white;
            SetReference(feedbackCtrl, "bannerPanel",  feedbackBanner);
            SetReference(feedbackCtrl, "messageText",  feedbackText);
            SetReference(feedbackCtrl, "feedbackText", feedbackText);
            SetReference(feedbackCtrl, "canvasGroup",  feedbackGroup);

            // Completion Panel
            GameObject overlay = CreateUIPanel(hud.transform, "CompletionPanel", new Color(0.04f, 0.06f, 0.10f, 0.65f), null);
            SetRect(overlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CanvasGroup completionGroup = GetOrAdd<CanvasGroup>(overlay);

            GameObject completionCard = CreateUIPanel(overlay.transform, "CompletionCard", new Color(0.96f, 0.97f, 0.98f, 1f), roundedPanelSprite);
            RectTransform completionRect = completionCard.GetComponent<RectTransform>();
            SetRect(completionRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 440f));

            TextMeshProUGUI header = CreateUIText(completionCard.transform, "Header", "🎉 LEVEL COMPLETE!", 28, TextAlignmentOptions.Center, new Vector2(420f, 40f), new Vector2(0f, 170f), new Vector2(0.5f, 0.5f));
            header.color = new Color(0.10f, 0.15f, 0.22f, 1f);
            header.fontStyle = FontStyles.Bold;
            TextMeshProUGUI sub = CreateUIText(completionCard.transform, "Subtitle", "You yielded safely!", 17, TextAlignmentOptions.Center, new Vector2(420f, 28f), new Vector2(0f, 134f), new Vector2(0.5f, 0.5f));
            sub.color = new Color(0.25f, 0.58f, 0.32f, 1f);

            CreateUIText(completionCard.transform, "ScoreTitle", "FINAL SCORE", 13, TextAlignmentOptions.Center, new Vector2(260f, 20f), new Vector2(0f, 80f), new Vector2(0.5f, 0.5f)).color = new Color(0.45f, 0.50f, 0.58f, 1f);
            TextMeshProUGUI finalScore = CreateUIText(completionCard.transform, "FinalScore", "100", 42, TextAlignmentOptions.Center, new Vector2(260f, 50f), new Vector2(0f, 40f), new Vector2(0.5f, 0.5f));
            finalScore.color = new Color(0.10f, 0.15f, 0.22f, 1f);
            finalScore.fontStyle = FontStyles.Bold;

            GameObject statsRow = CreateUIPanel(completionCard.transform, "StatisticsRow", new Color(0f, 0f, 0f, 0f));
            SetRect(statsRow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(412f, 76f));
            TextMeshProUGUI safeActions = CreateStatCard(statsRow.transform, "SafeActionsCard", "SAFE YIELDS", "0",  new Vector2(-106f, 0f), new Color(0.18f, 0.67f, 0.32f, 1f), roundedPanelSprite);
            TextMeshProUGUI mistakes    = CreateStatCard(statsRow.transform, "MistakesCard",    "ERRORS",      "0",  new Vector2( 106f, 0f), new Color(0.92f, 0.45f, 0.18f, 1f), roundedPanelSprite);

            TextMeshProUGUI ratingStars = CreateUIText(completionCard.transform, "Rating",     "⭐⭐⭐⭐⭐", 24, TextAlignmentOptions.Center, new Vector2(260f, 32f), new Vector2(0f, -102f), new Vector2(0.5f, 0.5f));
            ratingStars.color = new Color(1f, 0.68f, 0.12f, 1f);
            TextMeshProUGUI ratingLabel = CreateUIText(completionCard.transform, "RatingText", "Excellent!", 18, TextAlignmentOptions.Center, new Vector2(220f, 24f), new Vector2(0f, -130f), new Vector2(0.5f, 0.5f));
            ratingLabel.color = new Color(0.11f, 0.15f, 0.20f, 1f);
            ratingLabel.fontStyle = FontStyles.Bold;

            Button backButton = GetOrAddChildButton(completionCard.transform, "BackToMenuButton", "BACK TO MENU", new Vector2(-105f, -175f), roundedPanelSprite);
            backButton.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 50f);
            Button nextButton = GetOrAddChildButton(completionCard.transform, "NextLevelButton", "REPLAY",         new Vector2( 105f, -175f), roundedPanelSprite);
            nextButton.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 50f);

            LevelUIController ui = GetOrAdd<LevelUIController>(gameplayHud);
            SetReference(ui, "objectiveText",    objective);
            SetReference(ui, "scoreText",        scoreText);
            SetReference(ui, "completionPanel",  overlay);
            SetReference(ui, "completionGroup",  completionGroup);
            SetReference(ui, "completionCard",   completionRect);
            SetReference(ui, "finalScoreText",   finalScore);
            SetReference(ui, "safeActionsText",  safeActions);
            SetReference(ui, "mistakesText",     mistakes);
            SetReference(ui, "ratingStarsText",  ratingStars);
            SetReference(ui, "ratingText",       ratingLabel);
            SetReference(ui, "backButton",       backButton);
            SetReference(ui, "nextButton",       nextButton);
            SetReference(ui, "scoreManager",     score);
            SetReference(ui, "sceneLoader",      sceneLoader);

            UnityEventTools.AddPersistentListener(backButton.onClick, ui.BackToMenu);
            UnityEventTools.AddPersistentListener(nextButton.onClick, ui.LoadNextOrReplay);

            gameplayHud.SetActive(true);
            overlay.SetActive(false);
            return ui;
        }

        // ─── Shared Helpers (mirrored from Level2Setup) ────────────────────────────
        private static void EnsureCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
                go.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.orthographicSize = 4.8f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.48f, 0.68f, 0.84f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            GetOrAdd<AudioListener>(cam.gameObject);
            UniversalAdditionalCameraData data = GetOrAdd<UniversalAdditionalCameraData>(cam.gameObject);
            data.renderShadows = false;
        }

        private static void EnsureGlobalLight()
        {
            Light2D light = Object.FindAnyObjectByType<Light2D>();
            if (light == null) light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color = Color.white;
            light.intensity = 1f;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private static void EnsureAssetFolders()
        {
            foreach (string folder in new[] { "Assets/Scenes", "Assets/Prefabs", "Assets/Sprites", "Assets/Sprites/Generated", "Assets/UI" })
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        }

        private static void EnsureBuildSettingsScenes()
        {
            string[] required = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Level1.unity", "Assets/Scenes/Level2.unity", "Assets/Scenes/Level3.unity" };
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (string path in required)
                if (!scenes.Exists(s => s.path == path) && File.Exists(path))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ─── GameObject Helpers ────────────────────────────────────────────────────
        private static GameObject FindOrCreate(string name)
        {
            GameObject go = GameObject.Find(name);
            return go != null ? go : new GameObject(name);
        }

        private static GameObject FindOrCreateChild(Transform parent, string childName)
        {
            Transform t = parent.Find(childName);
            if (t != null) return t.gameObject;
            GameObject go = parent is RectTransform ? new GameObject(childName, typeof(RectTransform)) : new GameObject(childName);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject ResetVisualGroup(Transform parent, string name)
        {
            GameObject group = FindOrCreateChild(parent, name);
            ClearChildren(group.transform);
            return group;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            if (!go.TryGetComponent<T>(out var comp) || (UnityEngine.Object)comp == null)
            {
                comp = go.AddComponent<T>();
            }
            return comp;
        }

        private static GameObject CreateWorldSprite(Transform parent, string name, Vector3 position, Vector3 scale, Color color, int sortOrder, bool circle)
        {
            GameObject go = FindOrCreateChild(parent, name);
            go.transform.localPosition = position;
            go.transform.localScale    = scale;
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = go.AddComponent<SpriteRenderer>();
            }
            sr.sprite       = EnsureSprite(circle ? WorldCircleSpritePath : WorldSquareSpritePath, circle);
            sr.color        = color;
            sr.sortingOrder = sortOrder;
            return go;
        }

        private static GameObject CreateVisual(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject go = parent == null ? new GameObject(name) : FindOrCreateChild(parent, name);
            go.transform.localPosition = position;
            go.transform.localScale    = scale;
            MeshRenderer mr  = GetOrAdd<MeshRenderer>(go);
            MeshFilter   mf  = GetOrAdd<MeshFilter>(go);
            GameObject   tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mf.sharedMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);
            mr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mr.sharedMaterial.color = color;
            return go;
        }

        private static Sprite EnsureSprite(string assetPath, bool circle)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null) return sprite;

            const int size = 128;
            string dir = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            Texture2D tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            Color32 white    = new Color32(255, 255, 255, 255);
            Color32 clear    = new Color32(255, 255, 255, 0);
            float center = (size - 1) * 0.5f;
            float rSq    = (size * 0.46f) * (size * 0.46f);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center, dy = y - center;
                    pixels[y * size + x] = (!circle || dx * dx + dy * dy <= rSq) ? white : clear;
                }

            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        // ─── UI Helpers ────────────────────────────────────────────────────────────
        private static GameObject CreateUIPanel(Transform parent, string name, Color color, Sprite sprite = null)
        {
            GameObject go = FindOrCreateChild(parent, name);
            EnsureRectTransform(go);
            Image img = GetOrAdd<Image>(go);
            img.sprite = sprite;
            img.type   = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            img.color  = color;
            return go;
        }

        private static TextMeshProUGUI CreateUIText(Transform parent, string name, string content, float fontSize, TextAlignmentOptions alignment, Vector2 size, Vector2 position, Vector2 anchor)
        {
            GameObject go   = FindOrCreateChild(parent, name);
            RectTransform r = EnsureRectTransform(go);
            SetRect(r, anchor, anchor, position, size);
            TextMeshProUGUI t = GetOrAdd<TextMeshProUGUI>(go);
            t.text      = content;
            t.fontSize  = fontSize;
            t.alignment = alignment;
            t.textWrappingMode = TMPro.TextWrappingModes.Normal;
            return t;
        }

        private static TextMeshProUGUI CreateStatCard(Transform parent, string name, string label, string value, Vector2 position, Color accent, Sprite sprite)
        {
            GameObject card = CreateUIPanel(parent, name, new Color(0.92f, 0.94f, 0.96f, 1f), sprite);
            SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(180f, 76f));
            CreateUIText(card.transform, "Label", label, 11, TextAlignmentOptions.Left, new Vector2(140f, 18f), new Vector2(12f, 18f), new Vector2(0.5f, 0.5f)).color = new Color(0.45f, 0.50f, 0.58f, 1f);
            TextMeshProUGUI val = CreateUIText(card.transform, "Value", value, 24, TextAlignmentOptions.Left, new Vector2(140f, 32f), new Vector2(12f, -10f), new Vector2(0.5f, 0.5f));
            val.color      = new Color(0.10f, 0.15f, 0.22f, 1f);
            val.fontStyle  = FontStyles.Bold;
            return val;
        }

        private static Button GetOrAddChildButton(Transform parent, string name, string label, Vector2 position, Sprite sprite)
        {
            GameObject go = CreateUIPanel(parent, name, new Color(0.12f, 0.55f, 0.84f, 1f), sprite);
            SetRect(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(250f, 60f));
            Button btn = GetOrAdd<Button>(go);
            GetOrAdd<AnimatedUIButton>(go);
            TextMeshProUGUI t = CreateUIText(go.transform, "Text", label, 18, TextAlignmentOptions.Center, new Vector2(230f, 40f), Vector2.zero, new Vector2(0.5f, 0.5f));
            t.color     = Color.white;
            t.fontStyle = FontStyles.Bold;
            return btn;
        }

        private static RectTransform EnsureRectTransform(GameObject go)
        {
            if (go.TryGetComponent<RectTransform>(out var rt) && (UnityEngine.Object)rt != null)
            {
                return rt;
            }
            return go.AddComponent<RectTransform>();
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            rt.anchorMin       = anchorMin;
            rt.anchorMax       = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta       = size;
        }

        // ─── SerializedObject Helpers ──────────────────────────────────────────────
        private static void SetReference(Object target, string fieldName, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty sp = so.FindProperty(fieldName);
            if (sp != null) { sp.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
            else Debug.LogWarning($"[Level3Setup] Field '{fieldName}' not found on {target.GetType().Name}");
        }

        private static void SetObjectArray(Object target, string fieldName, Object[] values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty sp = so.FindProperty(fieldName);
            if (sp == null) { Debug.LogWarning($"[Level3Setup] Array field '{fieldName}' not found."); return; }
            sp.ClearArray();
            for (int i = 0; i < values.Length; i++) { sp.InsertArrayElementAtIndex(i); sp.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string fieldName, float value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty sp = so.FindProperty(fieldName);
            if (sp != null) { sp.floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        private static void SetInt(Object target, string fieldName, int value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty sp = so.FindProperty(fieldName);
            if (sp != null) { sp.intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
    }
}
#endif
