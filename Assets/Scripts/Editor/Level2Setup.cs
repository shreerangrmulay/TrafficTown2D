#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
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
using TrafficTown2D.Visuals;

namespace TrafficTown2D.Editor
{
    public static class Level2Setup
    {
        private const string Level2ScenePath = "Assets/Scenes/Level2.unity";
        private const string WorldSquareSpritePath = "Assets/Sprites/Generated/WorldSquare.png";
        private const string WorldCircleSpritePath = "Assets/Sprites/Generated/WorldCircle.png";
        private const string TrafficLightBodySpritePath = "Assets/Sprites/TrafficLightBody.png";
        private const string TrafficLightLensSpritePath = "Assets/Sprites/TrafficLightLens.png";
        private const string RoundedPanelSpritePath = "Assets/UI/RoundedPanel.png";
        private const string PlayerIdleClipPath = "Assets/Animations/Player/PlayerIdle.anim";
        private const string PlayerWalkClipPath = "Assets/Animations/Player/PlayerWalk.anim";
        private const string PlayerAnimatorPath = "Assets/Animations/Player/Player.controller";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string CarBluePrefabPath = "Assets/Prefabs/CarBlue.prefab";
        private const string CarRedPrefabPath = "Assets/Prefabs/CarRed.prefab";
        private const string CarYellowPrefabPath = "Assets/Prefabs/CarYellow.prefab";
        private const string TrafficLightPrefabPath = "Assets/Prefabs/TrafficLight.prefab";
        private const string PedestrianSignalPrefabPath = "Assets/Prefabs/PedestrianSignal.prefab";
        private const string CrossingSignPrefabPath = "Assets/Prefabs/PedestrianCrossingSign.prefab";
        private const string StreetLampPrefabPath = "Assets/Prefabs/StreetLamp.prefab";

        private static readonly Color RoadColor = new Color(0.10f, 0.11f, 0.13f, 1f);
        private static readonly Color RoadEdgeColor = new Color(0.92f, 0.88f, 0.62f, 1f);
        private static readonly Color LaneMarkColor = new Color(1f, 0.94f, 0.45f, 1f);
        private static readonly Color SidewalkColor = new Color(0.67f, 0.72f, 0.69f, 1f);
        private static readonly Color SidewalkTileColor = new Color(0.78f, 0.82f, 0.79f, 1f);
        private static readonly Color CrossingColor = new Color(0.98f, 0.96f, 0.84f, 1f);
        private static readonly Color TrafficLightBodyColor = new Color(0.04f, 0.045f, 0.05f, 1f);

        [MenuItem("TrafficTown/Setup Level 2")]
        public static void SetupLevel2()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before running TrafficTown -> Setup Level 2.");
                return;
            }

            EnsureAssetFolders();

            Scene level2Scene;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Level2ScenePath) == null)
            {
                level2Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(level2Scene, Level2ScenePath);
            }
            else
            {
                level2Scene = EditorSceneManager.OpenScene(Level2ScenePath, OpenSceneMode.Single);
            }

            if (!level2Scene.IsValid())
            {
                Debug.LogError("Could not open Level 2 scene at " + Level2ScenePath);
                return;
            }

            EnsureCamera();
            EnsureGlobalLight();

            GameObject environment = FindOrCreate("Environment");
            CreateTownBackground(environment.transform);
            CreateRoadScene(environment.transform);
            CreateCrossing(environment.transform);
            CreateTrafficSigns(environment.transform);

            GameObject traffic = FindOrCreate("Traffic");
            TrafficLightController light = CreateTrafficLight(traffic.transform);
            light.ConfigureDurations(5f, 5f, 2f);

            PedestrianSignalController pedestrian = CreatePedestrianSignal(traffic.transform, light);

            VehicleController bluePrefab = AssetDatabase.LoadAssetAtPath<VehicleController>(CarBluePrefabPath);
            VehicleController redPrefab = AssetDatabase.LoadAssetAtPath<VehicleController>(CarRedPrefabPath);
            VehicleController yellowPrefab = AssetDatabase.LoadAssetAtPath<VehicleController>(CarYellowPrefabPath);
            VehicleController[] prefabs = new VehicleController[] { bluePrefab, redPrefab, yellowPrefab };

            CreateTwoWayTrafficLanes(traffic.transform, light, prefabs);

            ScoreManager score = GetOrAdd(FindOrCreateChild(FindOrCreate("Gameplay").transform, "ScoreManager"), typeof(ScoreManager)) as ScoreManager;
            CrossingZone crossing = GetOrAdd(FindOrCreateChild(environment.transform, "ZebraCrossing"), typeof(CrossingZone)) as CrossingZone;
            CreateZone(environment.transform, "RoadZone", new Vector3(0f, 0f, -0.2f), new Vector2(18f, 4.4f), typeof(RoadZone));
            GameObject safeZoneObject = CreateZone(environment.transform, "SafeZone", new Vector3(0f, 3.3f, -0.2f), new Vector2(18f, 1.8f), typeof(SafeZone));

            GameObject playerObject = CreatePlayer();
            SafeCrossingController safety = GetOrAdd(playerObject, typeof(SafeCrossingController)) as SafeCrossingController;

            LevelUIController ui = CreateUI(score, light, pedestrian);
            FeedbackController feedbackController = Object.FindAnyObjectByType<FeedbackController>(FindObjectsInactive.Include);

            SetReference(safety, "crossingZone", crossing);
            SetReference(safety, "trafficLight", light);
            SetReference(safety, "pedestrianSignal", pedestrian);
            SetReference(safety, "scoreManager", score);
            SetReference(safety, "feedback", feedbackController);
            SetReference(safety, "levelUI", ui);

            EnsureBuildSettingsScenes();
            RemoveUnwantedFloatingTextObjects(environment.transform);
            RemoveUnwantedFloatingTextObjects(traffic.transform);
            RemoveUnwantedFloatingTextObjects(FindOrCreate("Gameplay").transform);

            EditorSceneManager.MarkSceneDirty(level2Scene);
            EditorSceneManager.SaveScene(level2Scene);
            Selection.activeGameObject = playerObject;
            Debug.Log("TrafficTown Level 2 setup completed successfully!");
        }

        private static void CreateTwoWayTrafficLanes(Transform parent, TrafficLightController light, VehicleController[] prefabs)
        {
            GameObject topLane = FindOrCreateChild(parent, "TopLane");
            Transform topSpawn = CreateVisual(topLane.transform, "TopSpawnPoint", new Vector3(-9.5f, 1.1f, 0f), new Vector3(0.2f, 0.2f, 0.2f), Color.clear).transform;
            Transform topStop = CreateVisual(topLane.transform, "TopCarStopPoint", new Vector3(-2.8f, 1.1f, -0.1f), new Vector3(0.15f, 0.15f, 0.15f), Color.clear).transform;
            Transform topExit = CreateVisual(topLane.transform, "TopCarExitPoint", new Vector3(9.5f, 1.1f, -0.1f), new Vector3(0.15f, 0.15f, 0.15f), Color.clear).transform;

            VehicleSpawner topSpawner = GetOrAdd(FindOrCreateChild(topLane.transform, "TopSpawner"), typeof(VehicleSpawner)) as VehicleSpawner;
            SetReference(topSpawner, "vehiclePrefab", prefabs.Length > 0 ? prefabs[0] : null);
            SetObjectArray(topSpawner, "vehiclePrefabs", prefabs);
            SetReference(topSpawner, "spawnPoint", topSpawn);
            SetReference(topSpawner, "carStopPoint", topStop);
            SetReference(topSpawner, "carExitPoint", topExit);
            SetReference(topSpawner, "trafficLight", light);
            SetFloat(topSpawner, "spawnInterval", 3.5f);
            SetInt(topSpawner, "maximumActiveVehicles", 3);
            SetFloat(topSpawner, "minVehicleSpeed", 2.5f);
            SetFloat(topSpawner, "maxVehicleSpeed", 3.5f);
            SetFloat(topSpawner, "travelDirection", 1f);

            GameObject bottomLane = FindOrCreateChild(parent, "BottomLane");
            Transform bottomSpawn = CreateVisual(bottomLane.transform, "BottomSpawnPoint", new Vector3(9.5f, -1.1f, 0f), new Vector3(0.2f, 0.2f, 0.2f), Color.clear).transform;
            Transform bottomStop = CreateVisual(bottomLane.transform, "BottomCarStopPoint", new Vector3(2.8f, -1.1f, -0.1f), new Vector3(0.15f, 0.15f, 0.15f), Color.clear).transform;
            Transform bottomExit = CreateVisual(bottomLane.transform, "BottomCarExitPoint", new Vector3(-9.5f, -1.1f, -0.1f), new Vector3(0.15f, 0.15f, 0.15f), Color.clear).transform;

            VehicleSpawner bottomSpawner = GetOrAdd(FindOrCreateChild(bottomLane.transform, "BottomSpawner"), typeof(VehicleSpawner)) as VehicleSpawner;
            SetReference(bottomSpawner, "vehiclePrefab", prefabs.Length > 0 ? prefabs[0] : null);
            SetObjectArray(bottomSpawner, "vehiclePrefabs", prefabs);
            SetReference(bottomSpawner, "spawnPoint", bottomSpawn);
            SetReference(bottomSpawner, "carStopPoint", bottomStop);
            SetReference(bottomSpawner, "carExitPoint", bottomExit);
            SetReference(bottomSpawner, "trafficLight", light);
            SetFloat(bottomSpawner, "spawnInterval", 3.5f);
            SetInt(bottomSpawner, "maximumActiveVehicles", 3);
            SetFloat(bottomSpawner, "minVehicleSpeed", 2.5f);
            SetFloat(bottomSpawner, "maxVehicleSpeed", 3.5f);
            SetFloat(bottomSpawner, "travelDirection", -1f);
        }

        private static void CreateTownBackground(Transform parent)
        {
            GameObject backgroundGroup = ResetVisualGroup(parent, "TownBackground");
            CreateWorldSprite(backgroundGroup.transform, "Ground", new Vector3(0f, 0f, 5f), new Vector3(22f, 12f, 1f), new Color(0.85f, 0.92f, 0.88f, 1f), -10, false);
            CreateWorldSprite(backgroundGroup.transform, "BuildingLeft", new Vector3(-6.5f, 4.8f, 2f), new Vector3(3.8f, 2.5f, 1f), new Color(0.78f, 0.83f, 0.88f, 1f), -5, false);
            CreateWorldSprite(backgroundGroup.transform, "BuildingCenterLeft", new Vector3(-2.2f, 5.2f, 2f), new Vector3(3.2f, 3.2f, 1f), new Color(0.91f, 0.85f, 0.77f, 1f), -5, false);
            CreateWorldSprite(backgroundGroup.transform, "BuildingCenterRight", new Vector3(2.5f, 4.9f, 2f), new Vector3(4.2f, 2.7f, 1f), new Color(0.82f, 0.88f, 0.82f, 1f), -5, false);
            CreateWorldSprite(backgroundGroup.transform, "BuildingRight", new Vector3(7.2f, 5.1f, 2f), new Vector3(3.5f, 3.0f, 1f), new Color(0.89f, 0.82f, 0.85f, 1f), -5, false);

            CreateStreetLamp(backgroundGroup.transform, "LampLeft", new Vector3(-6f, 2.6f, 0f));
            CreateStreetLamp(backgroundGroup.transform, "LampCenter", new Vector3(0f, 2.6f, 0f));
            CreateStreetLamp(backgroundGroup.transform, "LampRight", new Vector3(6f, 2.6f, 0f));
        }

        private static void CreateStreetLamp(Transform parent, string name, Vector3 position)
        {
            GameObject lamp = FindOrCreateChild(parent, name);
            lamp.transform.localPosition = position;
            CreateWorldSprite(lamp.transform, "Pole", new Vector3(0f, 0.35f, 0f), new Vector3(0.08f, 0.9f, 1f), new Color(0.25f, 0.28f, 0.32f, 1f), -1, false);
            CreateWorldSprite(lamp.transform, "Head", new Vector3(0f, 0.85f, 0f), new Vector3(0.3f, 0.15f, 1f), new Color(0.95f, 0.88f, 0.45f, 1f), 0, false);
        }

        private static void CreateRoadScene(Transform parent)
        {
            GameObject roadGroup = ResetVisualGroup(parent, "Road");
            CreateWorldSprite(roadGroup.transform, "RoadBase", Vector3.zero, new Vector3(22f, 4.4f, 1f), RoadColor, 0, false);
            CreateWorldSprite(roadGroup.transform, "TopEdge", new Vector3(0f, 2.15f, -0.01f), new Vector3(22f, 0.1f, 1f), RoadEdgeColor, 1, false);
            CreateWorldSprite(roadGroup.transform, "BottomEdge", new Vector3(0f, -2.15f, -0.01f), new Vector3(22f, 0.1f, 1f), RoadEdgeColor, 1, false);

            GameObject centerLineGroup = FindOrCreateChild(roadGroup.transform, "CenterLine");
            ClearChildren(centerLineGroup.transform);
            for (float x = -10f; x <= 10f; x += 1.2f)
            {
                CreateWorldSprite(centerLineGroup.transform, "Dash_" + x.ToString("F1"), new Vector3(x, 0f, -0.01f), new Vector3(0.7f, 0.08f, 1f), LaneMarkColor, 1, false);
            }

            CreateSidewalk(roadGroup.transform, "SidewalkTop", new Vector3(0f, 3.25f, 0f));
            CreateSidewalk(roadGroup.transform, "SidewalkBottom", new Vector3(0f, -3.25f, 0f));
        }

        private static void CreateSidewalk(Transform parent, string name, Vector3 position)
        {
            GameObject sidewalk = FindOrCreateChild(parent, name);
            sidewalk.transform.localPosition = position;
            CreateWorldSprite(sidewalk.transform, "Base", Vector3.zero, new Vector3(22f, 1.9f, 1f), SidewalkColor, -2, false);
            for (float x = -10f; x <= 10f; x += 1.2f)
            {
                CreateWorldSprite(sidewalk.transform, "Tile_" + x.ToString("F1"), new Vector3(x, 0f, -0.01f), new Vector3(1.1f, 1.8f, 1f), SidewalkTileColor, -1, false);
            }
        }

        private static void CreateCrossing(Transform parent)
        {
            GameObject zebra = FindOrCreateChild(parent, "ZebraCrossing");
            ClearChildren(zebra.transform);
            zebra.transform.position = new Vector3(0f, 0f, -0.05f);
            BoxCollider2D crossingCollider = GetOrAdd(zebra, typeof(BoxCollider2D)) as BoxCollider2D;
            crossingCollider.isTrigger = true;
            crossingCollider.size = new Vector2(3.8f, 4.3f);
            crossingCollider.offset = Vector2.zero;
            for (float x = -1.6f; x <= 1.6f; x += 0.8f)
            {
                CreateWorldSprite(zebra.transform, "Stripe_" + x.ToString("F1"), new Vector3(x, 0f, 0f), new Vector3(0.45f, 4.3f, 1f), CrossingColor, 2, false);
            }
        }

        private static void CreateTrafficSigns(Transform parent)
        {
            GameObject signsGroup = ResetVisualGroup(parent, "TrafficSigns");
            CreateSignPost(signsGroup.transform, "StopSign", new Vector3(-2.2f, -2.8f, 0f), new Color(0.85f, 0.12f, 0.12f, 1f), "STOP", false);
            CreateSignPost(signsGroup.transform, "CrossingSign", new Vector3(-1.2f, 2.6f, 0f), new Color(0.12f, 0.45f, 0.85f, 1f), "XING", true);
            CreateSignPost(signsGroup.transform, "SpeedLimitSign", new Vector3(2.2f, -2.8f, 0f), Color.white, "30", false);
        }

        private static void CreateSignPost(Transform parent, string name, Vector3 position, Color faceColor, string label, bool diamond)
        {
            GameObject sign = FindOrCreateChild(parent, name);
            ClearChildren(sign.transform);
            sign.transform.localPosition = position;
            sign.transform.localScale = Vector3.one;

            CreateWorldSprite(sign.transform, "Post", new Vector3(0f, -0.45f, 0f), new Vector3(0.08f, 0.9f, 1f), new Color(0.29f, 0.31f, 0.32f, 1f), 12, false);
            CreateWorldSprite(sign.transform, "Base", new Vector3(0f, -0.92f, 0f), new Vector3(0.5f, 0.08f, 1f), new Color(0.29f, 0.31f, 0.32f, 1f), 12, false);
            GameObject face = CreateWorldSprite(sign.transform, "Face", new Vector3(0f, 0.2f, 0f), diamond ? new Vector3(0.72f, 0.72f, 1f) : new Vector3(0.74f, 0.48f, 1f), faceColor, 13, diamond);
            face.transform.localRotation = diamond ? Quaternion.Euler(0f, 0f, 45f) : Quaternion.identity;

            if (diamond)
            {
                CreateWorldSprite(sign.transform, "PedestrianIconHead", new Vector3(0f, 0.34f, -0.02f), new Vector3(0.12f, 0.12f, 1f), Color.white, 15, true);
                CreateWorldSprite(sign.transform, "PedestrianIconBody", new Vector3(0f, 0.2f, -0.02f), new Vector3(0.06f, 0.24f, 1f), Color.white, 15, false);
            }
            else if (name.Contains("Stop"))
            {
                CreateWorldSprite(sign.transform, "InnerBorder", new Vector3(0f, 0.2f, -0.01f), new Vector3(0.62f, 0.42f, 1f), Color.white, 14, false);
                CreateWorldSprite(sign.transform, "InnerFace", new Vector3(0f, 0.2f, -0.02f), new Vector3(0.54f, 0.34f, 1f), faceColor, 15, false);
            }
            else if (name.Contains("Speed"))
            {
                CreateWorldSprite(sign.transform, "RedRing", new Vector3(0f, 0.2f, -0.01f), new Vector3(0.74f, 0.74f, 1f), new Color(0.85f, 0.12f, 0.12f, 1f), 14, true);
                CreateWorldSprite(sign.transform, "WhiteInner", new Vector3(0f, 0.2f, -0.02f), new Vector3(0.58f, 0.58f, 1f), Color.white, 15, true);
            }
        }

        private static TrafficLightController CreateTrafficLight(Transform parent)
        {
            GameObject root = FindOrCreateChild(parent, "TrafficLight");
            root.transform.position = new Vector3(3.6f, 2.45f, 0f);
            CreateWorldSprite(root.transform, "Post", new Vector3(0f, -0.9f, 0f), new Vector3(0.12f, 1.8f, 1f), new Color(0.2f, 0.22f, 0.25f, 1f), 3, false);

            GameObject body = CreateWorldSprite(root.transform, "TrafficLightBody", Vector3.zero, new Vector3(0.85f, 2.05f, 1f), TrafficLightBodyColor, 4, false);
            body.GetComponent<SpriteRenderer>().sprite = EnsureTrafficLightSprite(TrafficLightBodySpritePath, false);

            SpriteRenderer red = CreateSignalLight(root.transform, "RedLight", "Red", new Vector3(0f, 0.62f, -0.05f), Color.red);
            SpriteRenderer yellow = CreateSignalLight(root.transform, "YellowLight", "Yellow", new Vector3(0f, 0f, -0.05f), Color.yellow);
            SpriteRenderer green = CreateSignalLight(root.transform, "GreenLight", "Green", new Vector3(0f, -0.62f, -0.05f), Color.green);

            TrafficLightController controller = GetOrAdd(root, typeof(TrafficLightController)) as TrafficLightController;
            SetReference(controller, "redLight", red);
            SetReference(controller, "yellowLight", yellow);
            SetReference(controller, "greenLight", green);
            return controller;
        }

        private static PedestrianSignalController CreatePedestrianSignal(Transform parent, TrafficLightController light)
        {
            GameObject root = FindOrCreateChild(parent, "PedestrianSignal");
            root.transform.position = new Vector3(-3.6f, 2.45f, 0f);
            ClearChildren(root.transform);

            CreateWorldSprite(root.transform, "Post", new Vector3(0f, -0.7f, 0f), new Vector3(0.1f, 1.4f, 1f), new Color(0.2f, 0.22f, 0.25f, 1f), 3, false);
            GameObject body = CreateWorldSprite(root.transform, "SignalBody", Vector3.zero, new Vector3(0.75f, 1.45f, 1f), TrafficLightBodyColor, 4, false);
            body.GetComponent<SpriteRenderer>().sprite = EnsureTrafficLightSprite(TrafficLightBodySpritePath, false);

            SpriteRenderer dontWalk = CreateSignalLight(root.transform, "DontWalkLight", string.Empty, new Vector3(0f, 0.35f, -0.05f), new Color(0.92f, 0.2f, 0.2f, 1f));
            SpriteRenderer walk = CreateSignalLight(root.transform, "WalkLight", string.Empty, new Vector3(0f, -0.35f, -0.05f), new Color(0.2f, 0.85f, 0.3f, 1f));

            PedestrianSignalController controller = GetOrAdd(root, typeof(PedestrianSignalController)) as PedestrianSignalController;
            SetReference(controller, "trafficLight", light);
            SetReference(controller, "signalRenderer", body.GetComponent<SpriteRenderer>());
            SetReference(controller, "dontWalkRenderer", dontWalk);
            SetReference(controller, "walkRenderer", walk);
            return controller;
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = FindOrCreate("Player");
            player.transform.position = new Vector3(0f, -3.0f, 0f);
            player.transform.localScale = Vector3.one;

            Rigidbody2D body = GetOrAdd(player, typeof(Rigidbody2D)) as Rigidbody2D;
            body.gravityScale = 0f;
            body.freezeRotation = true;

            BoxCollider2D box = GetOrAdd(player, typeof(BoxCollider2D)) as BoxCollider2D;
            box.size = new Vector2(0.6f, 0.9f);

            SpriteRenderer renderer = GetOrAdd(player, typeof(SpriteRenderer)) as SpriteRenderer;
            renderer.sprite = EnsureTrafficLightSprite(WorldSquareSpritePath, false);
            renderer.color = new Color(0.22f, 0.58f, 0.92f, 1f);
            renderer.sortingOrder = 10;

            PlayerController controller = GetOrAdd(player, typeof(PlayerController)) as PlayerController;
            SetFloat(controller, "movementSpeed", 3.5f);
            return player;
        }

        private static LevelUIController CreateUI(ScoreManager score, TrafficLightController light, PedestrianSignalController pedestrian)
        {
            EnsureEventSystem();
            Sprite roundedPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPanelSpritePath);

            Canvas canvas = FindReusableCanvas();
            if (canvas == null)
            {
                GameObject canvasObject = FindOrCreate("UI");
                canvas = GetOrAdd(canvasObject, typeof(Canvas)) as Canvas;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = GetOrAdd(canvasObject, typeof(CanvasScaler)) as CanvasScaler;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                GetOrAdd(canvasObject, typeof(GraphicRaycaster));
            }

            GameObject hud = FindOrCreateChild(canvas.transform, "HUD");
            RectTransform hudRect = EnsureRectTransform(hud);
            hud.transform.SetAsFirstSibling();
            SetRect(hudRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject gameplayHud = FindOrCreateChild(hud.transform, "GameplayHUD");
            RectTransform gameplayHudRect = EnsureRectTransform(gameplayHud);
            SetRect(gameplayHudRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject missionCard = CreateUIPanel(gameplayHud.transform, "MissionCard", new Color(0.10f, 0.15f, 0.22f, 0.88f), roundedPanelSprite);
            SetRect(missionCard.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(170f, -48f), new Vector2(300f, 68f));
            CreateUIText(missionCard.transform, "Title", "MISSION", 13, TextAlignmentOptions.Left, new Vector2(270f, 20f), new Vector2(10f, 16f), new Vector2(0f, 0.5f)).color = new Color(0.55f, 0.76f, 0.98f, 1f);
            TextMeshProUGUI objective = CreateUIText(missionCard.transform, "Objective", "Cross the road safely by checking BOTH directions.", 14, TextAlignmentOptions.Left, new Vector2(270f, 32f), new Vector2(10f, -10f), new Vector2(0f, 0.5f));
            objective.color = Color.white;

            GameObject scoreCard = CreateUIPanel(gameplayHud.transform, "ScoreCard", new Color(0.10f, 0.15f, 0.22f, 0.88f), roundedPanelSprite);
            SetRect(scoreCard.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-110f, -48f), new Vector2(180f, 68f));
            CreateUIText(scoreCard.transform, "Title", "⭐ SCORE", 13, TextAlignmentOptions.Right, new Vector2(150f, 20f), new Vector2(-10f, 16f), new Vector2(1f, 0.5f)).color = new Color(1f, 0.82f, 0.28f, 1f);
            TextMeshProUGUI scoreText = CreateUIText(scoreCard.transform, "Value", "100", 24, TextAlignmentOptions.Right, new Vector2(150f, 32f), new Vector2(-10f, -10f), new Vector2(1f, 0.5f));
            scoreText.color = Color.white;
            scoreText.fontStyle = FontStyles.Bold;

            GameObject feedbackBanner = CreateUIPanel(hud.transform, "FeedbackBanner", new Color(0.08f, 0.12f, 0.18f, 0.92f), roundedPanelSprite);
            SetRect(feedbackBanner.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(520f, 54f));
            CanvasGroup feedbackGroup = GetOrAdd(feedbackBanner, typeof(CanvasGroup)) as CanvasGroup;
            FeedbackController staleFeedbackController = hud.GetComponent<FeedbackController>();
            if (staleFeedbackController != null)
            {
                Undo.DestroyObjectImmediate(staleFeedbackController);
            }
            FeedbackController feedbackController = GetOrAdd(feedbackBanner, typeof(FeedbackController)) as FeedbackController;
            TextMeshProUGUI feedbackText = CreateUIText(feedbackBanner.transform, "Message", string.Empty, 16, TextAlignmentOptions.Center, new Vector2(490f, 40f), Vector2.zero, new Vector2(0.5f, 0.5f));
            feedbackText.color = Color.white;
            SetReference(feedbackController, "bannerPanel", feedbackBanner);
            SetReference(feedbackController, "messageText", feedbackText);
            SetReference(feedbackController, "feedbackText", feedbackText);
            SetReference(feedbackController, "canvasGroup", feedbackGroup);

            // Intro Panel
            GameObject introPanel = CreateUIPanel(hud.transform, "LevelIntroPanel", new Color(0.04f, 0.06f, 0.10f, 0.75f), null);
            SetRect(introPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject introCard = CreateUIPanel(introPanel.transform, "Card", new Color(0.96f, 0.97f, 0.98f, 1f), roundedPanelSprite);
            SetRect(introCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 320f));

            TextMeshProUGUI introTitle = CreateUIText(introCard.transform, "Title", "🚸 SMART CROSSING", 26, TextAlignmentOptions.Center, new Vector2(400f, 40f), new Vector2(0f, 100f), new Vector2(0.5f, 0.5f));
            introTitle.color = new Color(0.10f, 0.15f, 0.22f, 1f);
            introTitle.fontStyle = FontStyles.Bold;

            TextMeshProUGUI introBody = CreateUIText(introCard.transform, "Body", "Look BOTH ways before crossing.\nWait for a safe gap in traffic.", 18, TextAlignmentOptions.Center, new Vector2(400f, 80f), new Vector2(0f, 20f), new Vector2(0.5f, 0.5f));
            introBody.color = new Color(0.25f, 0.30f, 0.35f, 1f);

            Button gotItButton = GetOrAddChildButton(introCard.transform, "GotItButton", "GOT IT", new Vector2(0f, -80f), roundedPanelSprite);
            LevelIntroController introController = GetOrAdd(introPanel, typeof(LevelIntroController)) as LevelIntroController;
            SetReference(introController, "introPanel", introPanel);
            SetReference(introController, "gotItButton", gotItButton);

            // Completion Panel
            GameObject overlay = CreateUIPanel(hud.transform, "CompletionPanel", new Color(0.04f, 0.06f, 0.10f, 0.65f), null);
            SetRect(overlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CanvasGroup completionGroup = GetOrAdd(overlay, typeof(CanvasGroup)) as CanvasGroup;

            GameObject completionCard = CreateUIPanel(overlay.transform, "CompletionCard", new Color(0.96f, 0.97f, 0.98f, 1f), roundedPanelSprite);
            RectTransform completionRect = completionCard.GetComponent<RectTransform>();
            SetRect(completionRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 440f));

            TextMeshProUGUI completeHeader = CreateUIText(completionCard.transform, "Header", "🎉 LEVEL COMPLETE!", 28, TextAlignmentOptions.Center, new Vector2(420f, 40f), new Vector2(0f, 170f), new Vector2(0.5f, 0.5f));
            completeHeader.color = new Color(0.10f, 0.15f, 0.22f, 1f);
            completeHeader.fontStyle = FontStyles.Bold;

            TextMeshProUGUI completeSub = CreateUIText(completionCard.transform, "Subtitle", "You made safe decisions!", 17, TextAlignmentOptions.Center, new Vector2(420f, 28f), new Vector2(0f, 134f), new Vector2(0.5f, 0.5f));
            completeSub.color = new Color(0.25f, 0.58f, 0.32f, 1f);

            TextMeshProUGUI scoreTitle = CreateUIText(completionCard.transform, "ScoreTitle", "FINAL SCORE", 13, TextAlignmentOptions.Center, new Vector2(260f, 20f), new Vector2(0f, 80f), new Vector2(0.5f, 0.5f));
            scoreTitle.color = new Color(0.45f, 0.50f, 0.58f, 1f);

            TextMeshProUGUI finalScore = CreateUIText(completionCard.transform, "FinalScore", "100", 42, TextAlignmentOptions.Center, new Vector2(260f, 50f), new Vector2(0f, 40f), new Vector2(0.5f, 0.5f));
            finalScore.color = new Color(0.10f, 0.15f, 0.22f, 1f);
            finalScore.fontStyle = FontStyles.Bold;

            GameObject statisticsRow = CreateUIPanel(completionCard.transform, "StatisticsRow", new Color(0f, 0f, 0f, 0f));
            SetRect(statisticsRow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(412f, 76f));
            TextMeshProUGUI safeActions = CreateStatCard(statisticsRow.transform, "SafeActionsCard", "SAFE ACTIONS", "0", new Vector2(-106f, 0f), new Color(0.18f, 0.67f, 0.32f, 1f), roundedPanelSprite);
            TextMeshProUGUI mistakes = CreateStatCard(statisticsRow.transform, "MistakesCard", "ERRORS", "0", new Vector2(106f, 0f), new Color(0.92f, 0.45f, 0.18f, 1f), roundedPanelSprite);

            TextMeshProUGUI ratingStars = CreateUIText(completionCard.transform, "Rating", "⭐⭐⭐⭐⭐", 24, TextAlignmentOptions.Center, new Vector2(260f, 32f), new Vector2(0f, -102f), new Vector2(0.5f, 0.5f));
            ratingStars.color = new Color(1f, 0.68f, 0.12f, 1f);
            TextMeshProUGUI ratingLabel = CreateUIText(completionCard.transform, "RatingText", "Excellent!", 18, TextAlignmentOptions.Center, new Vector2(220f, 24f), new Vector2(0f, -130f), new Vector2(0.5f, 0.5f));
            ratingLabel.color = new Color(0.11f, 0.15f, 0.20f, 1f);
            ratingLabel.fontStyle = FontStyles.Bold;

            Button backButton = GetOrAddChildButton(completionCard.transform, "BackToMenuButton", "BACK TO MENU", new Vector2(-105f, -175f), roundedPanelSprite);
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.sizeDelta = new Vector2(190f, 50f);

            Button nextButton = GetOrAddChildButton(completionCard.transform, "NextLevelButton", "NEXT LEVEL", new Vector2(105f, -175f), roundedPanelSprite);
            RectTransform nextRect = nextButton.GetComponent<RectTransform>();
            nextRect.sizeDelta = new Vector2(190f, 50f);
            nextButton.interactable = false;

            LevelUIController ui = GetOrAdd(gameplayHud, typeof(LevelUIController)) as LevelUIController;
            SetReference(ui, "objectiveText", objective);
            SetReference(ui, "scoreText", scoreText);
            SetReference(ui, "completionPanel", overlay);
            SetReference(ui, "completionGroup", completionGroup);
            SetReference(ui, "completionCard", completionRect);
            SetReference(ui, "finalScoreText", finalScore);
            SetReference(ui, "safeActionsText", safeActions);
            SetReference(ui, "mistakesText", mistakes);
            SetReference(ui, "ratingStarsText", ratingStars);
            SetReference(ui, "ratingText", ratingLabel);
            SetReference(ui, "backButton", backButton);
            SetReference(ui, "scoreManager", score);
            SetReference(ui, "sceneLoader", Object.FindAnyObjectByType<SceneLoader>());

            UnityEventTools.AddPersistentListener(backButton.onClick, ui.BackToMenu);

            gameplayHud.SetActive(true);
            overlay.SetActive(false);
            return ui;
        }

        private static Canvas FindReusableCanvas()
        {
            Canvas selectedCanvas = null;
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int index = 0; index < canvases.Length; index++)
            {
                Canvas canvas = canvases[index];
                if (selectedCanvas == null || canvas.name == "UI")
                {
                    selectedCanvas = canvas;
                }
            }
            return selectedCanvas;
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Color color, Sprite sprite = null)
        {
            GameObject objectRoot = FindOrCreateChild(parent, name);
            EnsureRectTransform(objectRoot);
            Image image = GetOrAdd(objectRoot, typeof(Image)) as Image;
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            return objectRoot;
        }

        private static TextMeshProUGUI CreateUIText(Transform parent, string name, string content, float fontSize, TextAlignmentOptions alignment, Vector2 size, Vector2 position, Vector2 anchor)
        {
            GameObject objectRoot = FindOrCreateChild(parent, name);
            RectTransform rect = EnsureRectTransform(objectRoot);
            SetRect(rect, anchor, anchor, position, size);
            TextMeshProUGUI text = GetOrAdd(objectRoot, typeof(TextMeshProUGUI)) as TextMeshProUGUI;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            return text;
        }

        private static TextMeshProUGUI CreateStatCard(Transform parent, string name, string label, string value, Vector2 position, Color accentColor, Sprite sprite)
        {
            GameObject card = CreateUIPanel(parent, name, new Color(0.92f, 0.94f, 0.96f, 1f), sprite);
            SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(180f, 76f));
            CreateWorldSprite(card.transform, "AccentLine", new Vector3(-80f, 0f, 0f), new Vector3(0.08f, 1f, 1f), accentColor, 0, false);
            CreateUIText(card.transform, "Label", label, 11, TextAlignmentOptions.Left, new Vector2(140f, 18f), new Vector2(12f, 18f), new Vector2(0.5f, 0.5f)).color = new Color(0.45f, 0.50f, 0.58f, 1f);
            TextMeshProUGUI valueText = CreateUIText(card.transform, "Value", value, 24, TextAlignmentOptions.Left, new Vector2(140f, 32f), new Vector2(12f, -10f), new Vector2(0.5f, 0.5f));
            valueText.color = new Color(0.10f, 0.15f, 0.22f, 1f);
            valueText.fontStyle = FontStyles.Bold;
            return valueText;
        }

        private static Button GetOrAddChildButton(Transform parent, string name, string label, Vector2 position, Sprite sprite)
        {
            GameObject buttonObject = CreateUIPanel(parent, name, new Color(0.12f, 0.55f, 0.84f, 1f), sprite);
            SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(250f, 60f));
            GetOrAdd(buttonObject, typeof(Shadow));
            Button button = GetOrAdd(buttonObject, typeof(Button)) as Button;
            GetOrAdd(buttonObject, typeof(AnimatedUIButton));
            TextMeshProUGUI labelText = CreateUIText(buttonObject.transform, "Text", label, 18, TextAlignmentOptions.Center, new Vector2(230f, 40f), Vector2.zero, new Vector2(0.5f, 0.5f));
            labelText.color = Color.white;
            labelText.fontStyle = FontStyles.Bold;
            return button;
        }

        private static GameObject CreateZone(Transform parent, string name, Vector3 position, Vector2 size, System.Type componentType)
        {
            GameObject objectRoot = FindOrCreateChild(parent, name);
            objectRoot.transform.position = position;
            BoxCollider2D collider = GetOrAdd(objectRoot, typeof(BoxCollider2D)) as BoxCollider2D;
            collider.isTrigger = true;
            collider.size = size;
            GetOrAdd(objectRoot, componentType);
            return objectRoot;
        }

        private static void EnsureCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 4.8f;
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainCamera.backgroundColor = new Color(0.48f, 0.68f, 0.84f, 1f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            GetOrAdd(mainCamera.gameObject, typeof(AudioListener));
            UniversalAdditionalCameraData cameraData = GetOrAdd(mainCamera.gameObject, typeof(UniversalAdditionalCameraData)) as UniversalAdditionalCameraData;
            cameraData.renderShadows = false;
        }

        private static void EnsureGlobalLight()
        {
            Light2D light = Object.FindAnyObjectByType<Light2D>();
            if (light == null)
            {
                GameObject lightObject = new GameObject("Global Light 2D");
                light = lightObject.AddComponent<Light2D>();
            }

            light.lightType = Light2D.LightType.Global;
            light.color = Color.white;
            light.intensity = 1f;
        }

        private static void EnsureAssetFolders()
        {
            string[] folders = { "Assets/Scenes", "Assets/Prefabs", "Assets/Sprites", "Assets/Sprites/Generated", "Assets/Animations", "Assets/Animations/Player", "Assets/UI" };
            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
        }

        private static GameObject FindOrCreate(string name)
        {
            GameObject found = GameObject.Find(name);
            return found != null ? found : new GameObject(name);
        }

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child.gameObject;
            GameObject created = new GameObject(name);
            created.transform.SetParent(parent, false);
            return created;
        }

        private static GameObject ResetVisualGroup(Transform parent, string name)
        {
            GameObject group = FindOrCreateChild(parent, name);
            ClearChildren(group.transform);
            return group;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int childIndex = parent.childCount - 1; childIndex >= 0; childIndex--)
            {
                Undo.DestroyObjectImmediate(parent.GetChild(childIndex).gameObject);
            }
        }

        private static GameObject CreateWorldSprite(Transform parent, string name, Vector3 position, Vector3 scale, Color color, int sortingOrder, bool circle)
        {
            GameObject objectRoot = FindOrCreateChild(parent, name);
            objectRoot.transform.localPosition = position;
            objectRoot.transform.localScale = scale;
            SpriteRenderer renderer = GetOrAdd(objectRoot, typeof(SpriteRenderer)) as SpriteRenderer;
            renderer.sprite = EnsureTrafficLightSprite(circle ? WorldCircleSpritePath : WorldSquareSpritePath, circle);
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return objectRoot;
        }

        private static GameObject CreateVisual(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject objectRoot = parent == null ? new GameObject(name) : FindOrCreateChild(parent, name);
            objectRoot.transform.localPosition = position;
            objectRoot.transform.localScale = scale;
            MeshRenderer renderer = GetOrAdd(objectRoot, typeof(MeshRenderer)) as MeshRenderer;
            MeshFilter filter = GetOrAdd(objectRoot, typeof(MeshFilter)) as MeshFilter;
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            filter.sharedMesh = cube.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(cube);
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            renderer.sharedMaterial.color = color;
            return objectRoot;
        }

        private static SpriteRenderer CreateSignalLight(Transform parent, string name, string legacyName, Vector3 position, Color color)
        {
            Transform lightTransform = parent.Find(name);
            if (lightTransform == null && !string.IsNullOrEmpty(legacyName))
            {
                lightTransform = parent.Find(legacyName);
                if (lightTransform != null) lightTransform.name = name;
            }

            GameObject lightObject = lightTransform == null ? new GameObject(name) : lightTransform.gameObject;
            if (lightTransform == null) lightObject.transform.SetParent(parent);
            lightObject.transform.localPosition = position;
            lightObject.transform.localScale = Vector3.one * 0.45f;

            SpriteRenderer spriteRenderer = GetOrAdd(lightObject, typeof(SpriteRenderer)) as SpriteRenderer;
            spriteRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 5;
            return spriteRenderer;
        }

        private static Sprite EnsureTrafficLightSprite(string assetPath, bool circle)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null) return sprite;

            const int textureSize = 128;
            string directory = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[textureSize * textureSize];
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 clear = new Color32(255, 255, 255, 0);
            float center = (textureSize - 1) * 0.5f;
            float radius = textureSize * 0.46f;
            float radiusSquared = radius * radius;

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    bool visible = true;
                    if (circle)
                    {
                        float offsetX = x - center;
                        float offsetY = y - center;
                        visible = offsetX * offsetX + offsetY * offsetY <= radiusSquared;
                    }
                    pixels[y * textureSize + x] = visible ? white : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(assetPath, png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Component GetOrAdd(GameObject gameObject, System.Type componentType)
        {
            Component component = gameObject.GetComponent(componentType);
            if (component == null)
            {
                component = Undo.AddComponent(gameObject, componentType);
            }
            return component;
        }

        private static void SetReference(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
                EditorUtility.SetDirty(target as Object);
            }
        }

        private static void SetFloat(object target, string fieldName, float value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
                EditorUtility.SetDirty(target as Object);
            }
        }

        private static void SetInt(object target, string fieldName, int value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
                EditorUtility.SetDirty(target as Object);
            }
        }

        private static void SetObjectArray(object target, string fieldName, Object[] values)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                System.Array destinationArray = System.Array.CreateInstance(field.FieldType.GetElementType(), values.Length);
                System.Array.Copy(values, destinationArray, values.Length);
                field.SetValue(target, destinationArray);
                EditorUtility.SetDirty(target as Object);
            }
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static RectTransform EnsureRectTransform(GameObject objectRoot)
        {
            RectTransform rect = objectRoot.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = objectRoot.AddComponent<RectTransform>();
            }
            return rect;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
                eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        private static void EnsureBuildSettingsScenes()
        {
            string[] requiredScenePaths = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Level1.unity", "Assets/Scenes/Level2.unity" };
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (string path in requiredScenePaths)
            {
                if (!scenes.Exists(scene => scene.path == path) && File.Exists(path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void RemoveUnwantedFloatingTextObjects(Transform root)
        {
            if (root == null) return;
            TextMesh[] textMeshes = root.GetComponentsInChildren<TextMesh>(true);
            for (int i = textMeshes.Length - 1; i >= 0; i--)
            {
                if (textMeshes[i] != null)
                {
                    Undo.DestroyObjectImmediate(textMeshes[i].gameObject);
                }
            }
        }
    }
}
#endif
