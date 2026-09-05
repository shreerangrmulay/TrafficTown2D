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
    public static class Level1Setup
    {
        private const string Level1ScenePath = "Assets/Scenes/Level1.unity";
        private const string WorldSquareSpritePath = "Assets/Sprites/Generated/WorldSquare.png";
        private const string WorldCircleSpritePath = "Assets/Sprites/Generated/WorldCircle.png";
        private const string TrafficLightBodySpritePath = "Assets/Sprites/TrafficLightBody.png";
        private const string TrafficLightLensSpritePath = "Assets/Sprites/TrafficLightLens.png";
        private const string RoundedPanelSpritePath = "Assets/UI/RoundedPanel.png";
        private const string PlayerIdleClipPath = "Assets/Animations/Player/PlayerIdle.anim";
        private const string PlayerWalkClipPath = "Assets/Animations/Player/PlayerWalk.anim";
        private const string PlayerAnimatorPath = "Assets/Animations/Player/Player.controller";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string VehiclePrefabPath = "Assets/Prefabs/Vehicle.prefab";
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

        [MenuItem("TrafficTown/Setup Level 1")]
        public static void SetupLevel1()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before running TrafficTown -> Setup Level 1.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Level1ScenePath) == null)
            {
                Debug.LogError("Level 1 scene was not found at " + Level1ScenePath);
                return;
            }

            Scene level1Scene = EditorSceneManager.OpenScene(Level1ScenePath, OpenSceneMode.Single);
            if (!level1Scene.IsValid())
            {
                Debug.LogError("Could not open Level 1 scene at " + Level1ScenePath);
                return;
            }

            EnsureAssetFolders();
            EnsureCamera();
            EnsureGlobalLight();
            GameObject environment = FindOrCreate("Environment");
            CreateTownBackground(environment.transform);
            CreateRoadScene(environment.transform);
            CreateCrossing(environment.transform);
            CreateTrafficSigns(environment.transform);

            GameObject traffic = FindOrCreate("Traffic");
            TrafficLightController light = CreateTrafficLight(traffic.transform);
            PedestrianSignalController pedestrian = CreatePedestrianSignal(traffic.transform, light);
            VehicleController bluePrefab;
            VehicleController redPrefab;
            VehicleController yellowPrefab;
            CreateVehiclePrefabs(light, out bluePrefab, out redPrefab, out yellowPrefab);
            Transform spawn = CreateVisual(traffic.transform, "CarSpawnPoint", new Vector3(8f, -1.2f, 0f), new Vector3(0.2f, 0.2f, 0.2f), Color.clear).transform;
            VehicleSpawner spawner = GetOrAdd(FindOrCreateChild(traffic.transform, "Cars"), typeof(VehicleSpawner)) as VehicleSpawner;
            SetReference(spawner, "vehiclePrefab", bluePrefab);
            SetReference(spawner, "spawnPoint", spawn);
            SetReference(spawner, "trafficLight", light);
            Transform carStopPoint = CreateVisual(traffic.transform, "CarStopPoint", new Vector3(2.8f, -1.2f, -0.1f), new Vector3(0.15f, 0.15f, 0.15f), Color.clear).transform;
            SetReference(spawner, "carStopPoint", carStopPoint);
            Transform carExitPoint = CreateVisual(traffic.transform, "CarExitPoint", new Vector3(-10.5f, -1.2f, -0.1f), new Vector3(0.15f, 0.15f, 0.15f), Color.clear).transform;
            SetReference(spawner, "carExitPoint", carExitPoint);
            SetFloat(spawner, "spawnInterval", 3f);
            SetInt(spawner, "maximumActiveVehicles", 4);
            SetFloat(spawner, "vehicleSpeed", 2.5f);
            SetFloat(spawner, "stoppingPoint", 2.8f);
            CreateInitialCars(traffic.transform, bluePrefab, redPrefab, yellowPrefab);

            ScoreManager score = GetOrAdd(FindOrCreateChild(FindOrCreate("Gameplay").transform, "ScoreManager"), typeof(ScoreManager)) as ScoreManager;
            CrossingZone crossing = GetOrAdd(FindOrCreateChild(environment.transform, "ZebraCrossing"), typeof(CrossingZone)) as CrossingZone;
            CreateZone(environment.transform, "RoadZone", new Vector3(0f, 0f, -0.2f), new Vector2(16f, 4f), typeof(RoadZone));
            GameObject safeZoneObject = CreateZone(environment.transform, "SafeZone", new Vector3(0f, 3.3f, -0.2f), new Vector2(16f, 2.6f), typeof(SafeZone));

            GameObject playerObject = CreatePlayer();
            SafeCrossingController safety = GetOrAdd(playerObject, typeof(SafeCrossingController)) as SafeCrossingController;
            LevelUIController ui = CreateUI(score, light, pedestrian);
            SetReference(safety, "crossingZone", crossing);
            SetReference(safety, "trafficLight", light);
            SetReference(safety, "pedestrianSignal", pedestrian);
            SetReference(safety, "scoreManager", score);
            SetReference(safety, "feedback", ui.GetComponent<FeedbackController>());
            SetReference(safety, "levelUI", ui);
            if (safeZoneObject == null) Debug.LogWarning("SafeZone could not be created.");

            SavePrefabCopy(playerObject, PlayerPrefabPath);
            SavePrefabCopy(light.gameObject, TrafficLightPrefabPath);
            SavePrefabCopy(pedestrian.gameObject, PedestrianSignalPrefabPath);
            SavePrefabCopy(environment.transform.Find("TrafficSigns/CrossingSign")?.gameObject, CrossingSignPrefabPath);
            SavePrefabCopy(environment.transform.Find("TownBackground/LampLeft")?.gameObject, StreetLampPrefabPath);

            EnsureBuildSettingsScenes();
            RemoveUnwantedFloatingTextObjects(environment.transform);
            RemoveUnwantedFloatingTextObjects(traffic.transform);
            RemoveUnwantedFloatingTextObjects(FindOrCreate("Gameplay").transform);
            EditorSceneManager.MarkSceneDirty(level1Scene);
            EditorSceneManager.SaveScene(level1Scene);
            Selection.activeGameObject = playerObject;
            Debug.Log("TrafficTown Level 1 setup completed.");
        }

        [MenuItem("TrafficTown/Clean Main Menu Level Objects")]
        public static void CleanMainMenuLevelObjects()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before running TrafficTown -> Clean Main Menu Level Objects.");
                return;
            }

            const string mainMenuScenePath = "Assets/Scenes/MainMenu.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(mainMenuScenePath) == null)
            {
                Debug.LogError("Main Menu scene was not found at " + mainMenuScenePath);
                return;
            }

            Scene mainMenuScene = EditorSceneManager.OpenScene(mainMenuScenePath, OpenSceneMode.Single);
            string[] levelObjectNames = { "Environment", "Traffic", "Gameplay", "Player" };
            foreach (GameObject rootObject in mainMenuScene.GetRootGameObjects())
            {
                for (int index = 0; index < levelObjectNames.Length; index++)
                {
                    if (rootObject.name == levelObjectNames[index])
                    {
                        Object.DestroyImmediate(rootObject);
                        break;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(mainMenuScene);
            EditorSceneManager.SaveScene(mainMenuScene);
            Debug.Log("Removed Level 1 objects from Main Menu.");
        }

        private static void EnsureCamera()
        {
            Camera camera = Object.FindAnyObjectByType<Camera>();
            if (camera == null) camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.38f, 0.65f, 0.72f);
            camera.tag = "MainCamera";
        }

        private static void EnsureGlobalLight()
        {
            GameObject lightObject = FindOrCreate("Global Light 2D");
            Light2D light = GetOrAdd(lightObject, typeof(Light2D)) as Light2D;
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
        }

        private static void CreateTownBackground(Transform parent)
        {
            GameObject background = ResetVisualGroup(parent, "TownBackground");
            CreateWorldSprite(background.transform, "SkyBand", new Vector3(0f, 0f, 2f), new Vector3(18.5f, 12.5f, 1f), new Color(0.55f, 0.82f, 0.94f, 1f), -30, false);
            CreateWorldSprite(background.transform, "HorizonBand", new Vector3(0f, 4.1f, 1.95f), new Vector3(18.5f, 0.55f, 1f), new Color(0.78f, 0.91f, 0.90f, 1f), -18, false);
            CreateWorldSprite(background.transform, "GrassTop", new Vector3(0f, 4.58f, 1.9f), new Vector3(16f, 0.28f, 1f), new Color(0.42f, 0.72f, 0.45f, 1f), -8, false);
            CreateWorldSprite(background.transform, "GrassBottom", new Vector3(0f, -4.58f, 1.9f), new Vector3(16f, 0.28f, 1f), new Color(0.42f, 0.72f, 0.45f, 1f), -8, false);

            CreateCloud(background.transform, "CloudLeft", new Vector3(-5.9f, 5.15f, 1.7f), 0.9f);
            CreateCloud(background.transform, "CloudRight", new Vector3(5.1f, 5.28f, 1.7f), 0.72f);
            CreateBuilding(background.transform, "School", new Vector3(-6.2f, 4.15f, 1.8f), new Vector3(1.5f, 1.25f, 1f), new Color(0.96f, 0.72f, 0.42f, 1f));
            CreateBuilding(background.transform, "Library", new Vector3(-4.2f, 4.1f, 1.8f), new Vector3(1.25f, 1.1f, 1f), new Color(0.48f, 0.67f, 0.90f, 1f));
            CreateBuilding(background.transform, "Clinic", new Vector3(4.2f, 4.06f, 1.8f), new Vector3(1.18f, 1.05f, 1f), new Color(0.73f, 0.88f, 0.70f, 1f));
            CreateBuilding(background.transform, "Shop", new Vector3(6.2f, 4.05f, 1.8f), new Vector3(1.55f, 1.05f, 1f), new Color(0.88f, 0.52f, 0.64f, 1f));
            CreateTree(background.transform, "TreeTopLeft", new Vector3(-7.35f, 3.75f, 1.6f));
            CreateTree(background.transform, "TreeTopMiddle", new Vector3(0.35f, 3.95f, 1.6f));
            CreateTree(background.transform, "TreeTopRight", new Vector3(7.45f, 3.75f, 1.6f));
            CreateTree(background.transform, "TreeBottomLeft", new Vector3(-7.25f, -4.0f, 1.6f));
            CreateTree(background.transform, "TreeBottomMiddle", new Vector3(2.35f, -4.0f, 1.6f));
            CreateTree(background.transform, "TreeBottomRight", new Vector3(7.25f, -4.0f, 1.6f));
            CreateStreetLamp(background.transform, "LampLeft", new Vector3(-5.1f, 2.2f, 1.4f));
            CreateStreetLamp(background.transform, "LampCrossing", new Vector3(-1.35f, -2.35f, 1.4f));
            CreateStreetLamp(background.transform, "LampRight", new Vector3(5.7f, 2.2f, 1.4f));
            CreateBench(background.transform, "BenchTop", new Vector3(1.75f, 2.72f, 1.4f));
            CreateBush(background.transform, "BushBottomLeft", new Vector3(-4.7f, -4.16f, 1.5f));
            CreateBush(background.transform, "BushBottomRight", new Vector3(5.05f, -4.16f, 1.5f));
            CreateFlowerBed(background.transform, "FlowerBed", new Vector3(-0.7f, -4.18f, 1.5f));
        }

        private static void CreateRoadScene(Transform parent)
        {
            CreateWorldSprite(parent, "Road", new Vector3(0f, 0f, 1f), new Vector3(16f, 4f, 1f), RoadColor, 0, false);
            CreateWorldSprite(parent, "SidewalkTop", new Vector3(0f, 3.3f, 1f), new Vector3(16f, 2.6f, 1f), SidewalkColor, -3, false);
            CreateWorldSprite(parent, "SidewalkBottom", new Vector3(0f, -3.3f, 1f), new Vector3(16f, 2.6f, 1f), SidewalkColor, -3, false);

            GameObject details = ResetVisualGroup(parent, "RoadDetails");
            CreateWorldSprite(details.transform, "TopCurb", new Vector3(0f, 2.03f, 0f), new Vector3(16f, 0.08f, 1f), RoadEdgeColor, 3, false);
            CreateWorldSprite(details.transform, "BottomCurb", new Vector3(0f, -2.03f, 0f), new Vector3(16f, 0.08f, 1f), RoadEdgeColor, 3, false);
            CreateWorldSprite(details.transform, "CenterLine", new Vector3(0f, 0f, 0f), new Vector3(16f, 0.05f, 1f), new Color(0.25f, 0.27f, 0.28f, 1f), 2, false);
            CreateWorldSprite(details.transform, "StopLine", new Vector3(3.05f, -1.2f, 0f), new Vector3(0.12f, 1.55f, 1f), new Color(0.98f, 0.98f, 0.93f, 1f), 5, false);

            for (int index = 0; index < 8; index++)
            {
                float x = -7f + index * 2f;
                CreateWorldSprite(details.transform, "LaneDash" + index, new Vector3(x, 0f, 0f), new Vector3(0.95f, 0.08f, 1f), LaneMarkColor, 4, false);
            }

            for (int index = 0; index < 18; index++)
            {
                float x = -7.65f + index * 0.9f;
                float y = index % 2 == 0 ? -0.82f : 0.88f;
                CreateWorldSprite(details.transform, "AsphaltSpeck" + index, new Vector3(x, y, 0f), new Vector3(0.05f, 0.025f, 1f), new Color(0.18f, 0.19f, 0.21f, 0.45f), 1, false);
            }

            for (int index = 0; index < 11; index++)
            {
                float x = -7.5f + index * 1.5f;
                CreateWorldSprite(details.transform, "TopSidewalkTile" + index, new Vector3(x, 3.3f, 0f), new Vector3(0.035f, 2.45f, 1f), SidewalkTileColor, -1, false);
                CreateWorldSprite(details.transform, "BottomSidewalkTile" + index, new Vector3(x, -3.3f, 0f), new Vector3(0.035f, 2.45f, 1f), SidewalkTileColor, -1, false);
            }

            CreateWorldSprite(details.transform, "TopInnerPath", new Vector3(0f, 2.75f, 0f), new Vector3(16f, 0.035f, 1f), SidewalkTileColor, -1, false);
            CreateWorldSprite(details.transform, "BottomInnerPath", new Vector3(0f, -2.75f, 0f), new Vector3(16f, 0.035f, 1f), SidewalkTileColor, -1, false);
        }

        private static void CreateTrafficSigns(Transform parent)
        {
            GameObject signs = ResetVisualGroup(parent, "TrafficSigns");
            CreateSignPost(signs.transform, "CrossingSign", new Vector3(-2.8f, 2.45f, 0f), new Color(0.12f, 0.46f, 0.86f, 1f), "XING", true);
            CreateSignPost(signs.transform, "SpeedLimitSign", new Vector3(6.4f, 2.45f, 0f), Color.white, "30", false);
            CreateSignPost(signs.transform, "StopSign", new Vector3(-6.4f, -2.45f, 0f), new Color(0.9f, 0.12f, 0.1f, 1f), "STOP", false);
        }

        private static void CreateBuilding(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject building = FindOrCreateChild(parent, name);
            building.transform.localPosition = position;
            building.transform.localScale = Vector3.one;
            CreateWorldSprite(building.transform, "Body", Vector3.zero, scale, color, -7, false);
            CreateWorldSprite(building.transform, "Roof", new Vector3(0f, scale.y * 0.55f, 0f), new Vector3(scale.x * 1.1f, 0.24f, 1f), new Color(0.44f, 0.25f, 0.24f, 1f), -6, false);
            for (int index = 0; index < 3; index++)
            {
                CreateWorldSprite(building.transform, "Window" + index, new Vector3(-scale.x * 0.28f + index * scale.x * 0.28f, 0.18f, 0f), new Vector3(0.22f, 0.25f, 1f), new Color(0.84f, 0.96f, 1f, 1f), -5, false);
            }
            CreateWorldSprite(building.transform, "Door", new Vector3(0f, -scale.y * 0.31f, 0f), new Vector3(0.28f, 0.42f, 1f), new Color(0.39f, 0.25f, 0.15f, 1f), -5, false);
        }

        private static void CreateTree(Transform parent, string name, Vector3 position)
        {
            GameObject tree = FindOrCreateChild(parent, name);
            tree.transform.localPosition = position;
            tree.transform.localScale = Vector3.one;
            CreateWorldSprite(tree.transform, "Trunk", new Vector3(0f, -0.32f, 0f), new Vector3(0.18f, 0.55f, 1f), new Color(0.45f, 0.27f, 0.12f, 1f), -4, false);
            CreateWorldSprite(tree.transform, "LeavesBack", new Vector3(-0.16f, 0.04f, 0f), new Vector3(0.62f, 0.62f, 1f), new Color(0.23f, 0.56f, 0.29f, 1f), -3, true);
            CreateWorldSprite(tree.transform, "LeavesFront", new Vector3(0.14f, 0.12f, 0f), new Vector3(0.68f, 0.68f, 1f), new Color(0.31f, 0.68f, 0.36f, 1f), -2, true);
        }

        private static void CreateStreetLamp(Transform parent, string name, Vector3 position)
        {
            GameObject lamp = FindOrCreateChild(parent, name);
            lamp.transform.localPosition = position;
            lamp.transform.localScale = Vector3.one;
            CreateWorldSprite(lamp.transform, "Pole", new Vector3(0f, -0.38f, 0f), new Vector3(0.08f, 0.95f, 1f), new Color(0.33f, 0.35f, 0.36f, 1f), 1, false);
            CreateWorldSprite(lamp.transform, "Arm", new Vector3(0.22f, 0.08f, 0f), new Vector3(0.48f, 0.07f, 1f), new Color(0.33f, 0.35f, 0.36f, 1f), 1, false);
            CreateWorldSprite(lamp.transform, "Glow", new Vector3(0.48f, 0.03f, 0f), new Vector3(0.26f, 0.18f, 1f), new Color(1f, 0.93f, 0.55f, 1f), 2, true);
        }

        private static void CreateCloud(Transform parent, string name, Vector3 position, float scale)
        {
            GameObject cloud = FindOrCreateChild(parent, name);
            cloud.transform.localPosition = position;
            cloud.transform.localScale = Vector3.one * scale;
            Color cloudColor = new Color(1f, 1f, 1f, 0.82f);
            CreateWorldSprite(cloud.transform, "PuffLeft", new Vector3(-0.36f, -0.02f, 0f), new Vector3(0.74f, 0.42f, 1f), cloudColor, -16, true);
            CreateWorldSprite(cloud.transform, "PuffMiddle", new Vector3(0f, 0.08f, 0f), new Vector3(0.86f, 0.52f, 1f), cloudColor, -15, true);
            CreateWorldSprite(cloud.transform, "PuffRight", new Vector3(0.42f, -0.04f, 0f), new Vector3(0.76f, 0.40f, 1f), cloudColor, -16, true);
        }

        private static void CreateBush(Transform parent, string name, Vector3 position)
        {
            GameObject bush = FindOrCreateChild(parent, name);
            bush.transform.localPosition = position;
            bush.transform.localScale = Vector3.one;
            CreateWorldSprite(bush.transform, "LeafLeft", new Vector3(-0.22f, 0f, 0f), new Vector3(0.5f, 0.32f, 1f), new Color(0.18f, 0.52f, 0.28f, 1f), -2, true);
            CreateWorldSprite(bush.transform, "LeafMiddle", new Vector3(0.08f, 0.08f, 0f), new Vector3(0.58f, 0.42f, 1f), new Color(0.25f, 0.64f, 0.34f, 1f), -1, true);
            CreateWorldSprite(bush.transform, "LeafRight", new Vector3(0.4f, 0f, 0f), new Vector3(0.5f, 0.32f, 1f), new Color(0.20f, 0.56f, 0.30f, 1f), -2, true);
        }

        private static void CreateBench(Transform parent, string name, Vector3 position)
        {
            GameObject bench = FindOrCreateChild(parent, name);
            bench.transform.localPosition = position;
            bench.transform.localScale = Vector3.one;
            Color wood = new Color(0.62f, 0.34f, 0.17f, 1f);
            Color metal = new Color(0.20f, 0.22f, 0.24f, 1f);
            CreateWorldSprite(bench.transform, "Seat", new Vector3(0f, 0f, 0f), new Vector3(0.95f, 0.14f, 1f), wood, 1, false);
            CreateWorldSprite(bench.transform, "Back", new Vector3(0f, 0.2f, 0f), new Vector3(0.95f, 0.12f, 1f), wood, 1, false);
            CreateWorldSprite(bench.transform, "LegLeft", new Vector3(-0.34f, -0.18f, 0f), new Vector3(0.08f, 0.34f, 1f), metal, 0, false);
            CreateWorldSprite(bench.transform, "LegRight", new Vector3(0.34f, -0.18f, 0f), new Vector3(0.08f, 0.34f, 1f), metal, 0, false);
        }

        private static void CreateFlowerBed(Transform parent, string name, Vector3 position)
        {
            GameObject bed = FindOrCreateChild(parent, name);
            bed.transform.localPosition = position;
            bed.transform.localScale = Vector3.one;
            CreateWorldSprite(bed.transform, "Soil", new Vector3(0f, -0.08f, 0f), new Vector3(1.35f, 0.18f, 1f), new Color(0.42f, 0.24f, 0.12f, 1f), -3, false);
            Color[] colors = { new Color(0.95f, 0.25f, 0.38f, 1f), new Color(1f, 0.82f, 0.22f, 1f), new Color(0.42f, 0.58f, 0.96f, 1f) };
            for (int index = 0; index < 5; index++)
            {
                float x = -0.48f + index * 0.24f;
                CreateWorldSprite(bed.transform, "Flower" + index, new Vector3(x, 0.08f, 0f), new Vector3(0.12f, 0.12f, 1f), colors[index % colors.Length], -1, true);
            }
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
            GameObject objectRoot = FindOrCreateOnlyChild(parent, "TrafficLight");
            objectRoot.transform.position = new Vector3(4f, 2.7f, 0f);
            RemoveUnexpectedTrafficLightChildren(objectRoot.transform);

            TrafficLightController controller = GetOrAdd(objectRoot, typeof(TrafficLightController)) as TrafficLightController;
            SetFloat(controller, "redDuration", 5f);
            SetFloat(controller, "greenDuration", 5f);
            SetFloat(controller, "yellowDuration", 2f);

            CreateTrafficLightBody(objectRoot.transform);
            CreateTrafficLightSupport(objectRoot.transform);
            SpriteRenderer red = CreateTrafficLightLens(objectRoot.transform, "RedLight", "Red", new Vector3(0f, 1.1f, 0f), Color.red, true);
            SpriteRenderer yellow = CreateTrafficLightLens(objectRoot.transform, "YellowLight", "Yellow", new Vector3(0f, 0f, 0f), new Color(1f, 0.8f, 0f), false);
            SpriteRenderer green = CreateTrafficLightLens(objectRoot.transform, "GreenLight", "Green", new Vector3(0f, -1.1f, 0f), Color.green, false);
            SetReference(controller, "redLight", red); SetReference(controller, "yellowLight", yellow); SetReference(controller, "greenLight", green);
            return controller;
        }

        private static void CreateTrafficLightBody(Transform parent)
        {
            GameObject body = FindOrCreateChild(parent, "TrafficLightBody");
            Undo.RecordObject(body, "Configure traffic light body");
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(1.5f, 4f, 1f);
            RemoveMeshVisuals(body);

            SpriteRenderer renderer = GetOrAdd(body, typeof(SpriteRenderer)) as SpriteRenderer;
            Undo.RecordObject(renderer, "Configure traffic light body sprite");
            renderer.sprite = EnsureTrafficLightSprite(TrafficLightBodySpritePath, false);
            renderer.color = TrafficLightBodyColor;
            renderer.sortingOrder = 20;
        }

        private static void CreateTrafficLightSupport(Transform parent)
        {
            CreateWorldSprite(parent, "TrafficLightPost", new Vector3(0f, -2.7f, 0f), new Vector3(0.16f, 1.55f, 1f), new Color(0.14f, 0.15f, 0.16f, 1f), 18, false);
            CreateWorldSprite(parent, "TrafficLightBase", new Vector3(0f, -3.5f, 0f), new Vector3(0.9f, 0.16f, 1f), new Color(0.14f, 0.15f, 0.16f, 1f), 18, false);
        }

        private static SpriteRenderer CreateTrafficLightLens(Transform parent, string name, string legacyName, Vector3 position, Color color, bool active)
        {
            Transform lensTransform = parent.Find(name);
            if (lensTransform == null && !string.IsNullOrEmpty(legacyName))
            {
                lensTransform = parent.Find(legacyName);
                if (lensTransform != null) lensTransform.name = name;
            }

            GameObject lensObject = lensTransform == null ? new GameObject(name) : lensTransform.gameObject;
            if (lensTransform == null) lensObject.transform.SetParent(parent);
            Undo.RecordObject(lensObject, "Configure traffic light lens");
            lensObject.transform.localPosition = position;
            lensObject.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
            RemoveMeshVisuals(lensObject);
            ClearChildren(lensObject.transform);

            SpriteRenderer spriteRenderer = GetOrAdd(lensObject, typeof(SpriteRenderer)) as SpriteRenderer;
            Undo.RecordObject(spriteRenderer, "Configure traffic light lens sprite");
            spriteRenderer.sprite = EnsureTrafficLightSprite(TrafficLightLensSpritePath, true);
            spriteRenderer.color = WithAlpha(color, active ? 1f : 0.15f);
            spriteRenderer.sortingOrder = 21;
            CreateWorldSprite(lensObject.transform, "Rim", Vector3.zero, new Vector3(1.18f, 1.18f, 1f), new Color(0.01f, 0.012f, 0.014f, 1f), 20, true);
            SpriteRenderer glow = CreateWorldSprite(lensObject.transform, "Glow", Vector3.zero, new Vector3(1.48f, 1.48f, 1f), WithAlpha(color, active ? 0.28f : 0f), 19, true).GetComponent<SpriteRenderer>();
            SpriteGlowFollower glowFollower = GetOrAdd(lensObject, typeof(SpriteGlowFollower)) as SpriteGlowFollower;
            SetReference(glowFollower, "source", spriteRenderer);
            SetReference(glowFollower, "glow", glow);
            return spriteRenderer;
        }

        private static PedestrianSignalController CreatePedestrianSignal(Transform parent, TrafficLightController light)
        {
            GameObject root = FindOrCreateChild(parent, "PedestrianSignal");
            root.transform.position = new Vector3(-3.6f, 2.35f, 0f);
            ClearChildren(root.transform);
            PedestrianSignalController signal = GetOrAdd(root, typeof(PedestrianSignalController)) as PedestrianSignalController;
            SetReference(signal, "trafficLight", light);
            CreateWorldSprite(root.transform, "Housing", new Vector3(0f, 0f, 0f), new Vector3(1.25f, 1.45f, 1f), new Color(0.06f, 0.065f, 0.07f, 1f), 18, false);
            CreateWorldSprite(root.transform, "Post", new Vector3(0f, -1.2f, 0f), new Vector3(0.12f, 1.0f, 1f), new Color(0.14f, 0.15f, 0.16f, 1f), 17, false);
            CreateWorldSprite(root.transform, "Base", new Vector3(0f, -1.72f, 0f), new Vector3(0.72f, 0.12f, 1f), new Color(0.14f, 0.15f, 0.16f, 1f), 17, false);
            SpriteRenderer signalRenderer = CreateWorldSprite(root.transform, "Signal", new Vector3(0f, 0f, 0f), new Vector3(1.05f, 1.15f, 1f), new Color(0.19f, 0.05f, 0.04f, 1f), 19, false).GetComponent<SpriteRenderer>();
            SpriteRenderer walkRenderer = CreateWorldSprite(root.transform, "WalkLight", new Vector3(0f, 0.34f, 0f), new Vector3(0.85f, 0.34f, 1f), new Color(0.2f, 0.8f, 0.35f, 1f), 20, false).GetComponent<SpriteRenderer>();
            SpriteRenderer dontWalkRenderer = CreateWorldSprite(root.transform, "DontWalkLight", new Vector3(0f, -0.34f, 0f), new Vector3(0.85f, 0.34f, 1f), new Color(0.19f, 0.05f, 0.04f, 1f), 20, false).GetComponent<SpriteRenderer>();
            PedestrianSignalVisual visual = GetOrAdd(root, typeof(PedestrianSignalVisual)) as PedestrianSignalVisual;
            SetReference(visual, "signal", signal);
            SetReference(visual, "walkLabel", null);
            SetReference(visual, "dontWalkLabel", null);
            SetReference(signal, "signalRenderer", signalRenderer);
            SetReference(signal, "walkRenderer", walkRenderer);
            SetReference(signal, "dontWalkRenderer", dontWalkRenderer);
            return signal;
        }

        private static void CreateCrossing(Transform parent)
        {
            GameObject crossing = FindOrCreateChild(parent, "ZebraCrossing");
            ClearChildren(crossing.transform);
            for (int index = 0; index < 8; index++)
            {
                CreateWorldSprite(crossing.transform, "Stripe" + index, new Vector3(-2.1f + index * 0.6f, 0f, 0f), new Vector3(0.34f, 3.75f, 1f), CrossingColor, 8, false);
            }
            BoxCollider2D collider = GetOrAdd(crossing, typeof(BoxCollider2D)) as BoxCollider2D;
            collider.isTrigger = true; collider.size = new Vector2(4.8f, 4.2f);
        }

        private static GameObject CreateZone(Transform parent, string name, Vector3 position, Vector2 size, System.Type componentType)
        {
            GameObject zone = FindOrCreateChild(parent, name);
            zone.transform.position = position;
            BoxCollider2D collider = GetOrAdd(zone, typeof(BoxCollider2D)) as BoxCollider2D;
            collider.isTrigger = true; collider.size = size;
            GetOrAdd(zone, componentType);
            return zone;
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = FindOrCreate("Player");
            player.transform.position = new Vector3(-6f, -3.3f, -1f);
            player.transform.localScale = Vector3.one;
            ClearChildren(player.transform);
            CreateCharacterVisual(player.transform);
            Rigidbody2D body = GetOrAdd(player, typeof(Rigidbody2D)) as Rigidbody2D;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            BoxCollider2D collider = GetOrAdd(player, typeof(BoxCollider2D)) as BoxCollider2D;
            collider.size = new Vector2(0.65f, 0.9f);
            GetOrAdd(player, typeof(PlayerController));
            Animator animator = GetOrAdd(player, typeof(Animator)) as Animator;
            animator.runtimeAnimatorController = EnsurePlayerAnimatorController();
            PlayerVisualAnimator visualAnimator = GetOrAdd(player, typeof(PlayerVisualAnimator)) as PlayerVisualAnimator;
            SetReference(visualAnimator, "observedBody", body);
            SetReference(visualAnimator, "animator", animator);
            return player;
        }

        private static void CreateCharacterVisual(Transform parent)
        {
            GameObject visual = FindOrCreateChild(parent, "CharacterVisual");
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one;
            CreateWorldSprite(visual.transform, "Shadow", new Vector3(0f, -0.5f, 0f), new Vector3(0.62f, 0.12f, 1f), new Color(0f, 0f, 0f, 0.2f), 30, true);
            CreateWorldSprite(visual.transform, "Head", new Vector3(0f, 0.34f, 0f), new Vector3(0.34f, 0.34f, 1f), new Color(0.98f, 0.78f, 0.58f, 1f), 33, true);
            CreateWorldSprite(visual.transform, "Hair", new Vector3(0f, 0.46f, 0f), new Vector3(0.33f, 0.14f, 1f), new Color(0.17f, 0.10f, 0.06f, 1f), 34, true);
            CreateWorldSprite(visual.transform, "Shirt", new Vector3(0f, -0.02f, 0f), new Vector3(0.48f, 0.52f, 1f), new Color(0.12f, 0.55f, 0.88f, 1f), 32, false);
            CreateWorldSprite(visual.transform, "LeftArm", new Vector3(-0.34f, -0.03f, 0f), new Vector3(0.12f, 0.46f, 1f), new Color(0.98f, 0.78f, 0.58f, 1f), 31, false);
            CreateWorldSprite(visual.transform, "RightArm", new Vector3(0.34f, -0.03f, 0f), new Vector3(0.12f, 0.46f, 1f), new Color(0.98f, 0.78f, 0.58f, 1f), 31, false);
            CreateWorldSprite(visual.transform, "LeftLeg", new Vector3(-0.13f, -0.42f, 0f), new Vector3(0.15f, 0.38f, 1f), new Color(0.18f, 0.22f, 0.45f, 1f), 31, false);
            CreateWorldSprite(visual.transform, "RightLeg", new Vector3(0.13f, -0.42f, 0f), new Vector3(0.15f, 0.38f, 1f), new Color(0.18f, 0.22f, 0.45f, 1f), 31, false);
            CreateWorldSprite(visual.transform, "LeftShoe", new Vector3(-0.16f, -0.62f, 0f), new Vector3(0.22f, 0.08f, 1f), new Color(0.06f, 0.07f, 0.08f, 1f), 32, false);
            CreateWorldSprite(visual.transform, "RightShoe", new Vector3(0.16f, -0.62f, 0f), new Vector3(0.22f, 0.08f, 1f), new Color(0.06f, 0.07f, 0.08f, 1f), 32, false);
        }

        private static RuntimeAnimatorController EnsurePlayerAnimatorController()
        {
            AnimationClip idleClip = EnsurePlayerAnimationClip(PlayerIdleClipPath, false);
            AnimationClip walkClip = EnsurePlayerAnimationClip(PlayerWalkClipPath, true);

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerAnimatorPath) != null)
            {
                AssetDatabase.DeleteAsset(PlayerAnimatorPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerAnimatorPath);
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = stateMachine.AddState("Idle");
            AnimatorState walkState = stateMachine.AddState("Walk");
            idleState.motion = idleClip;
            walkState.motion = walkClip;
            stateMachine.defaultState = idleState;

            AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.05f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, "Moving");

            AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.05f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");

            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip EnsurePlayerAnimationClip(string path, bool walking)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.frameRate = 12f;
            clip.ClearCurves();
            if (walking)
            {
                SetTransformPositionCurve(clip, "CharacterVisual/LeftArm", "x", -0.38f, -0.28f, -0.38f, 0.4f);
                SetTransformPositionCurve(clip, "CharacterVisual/RightArm", "x", 0.28f, 0.38f, 0.28f, 0.4f);
                SetTransformPositionCurve(clip, "CharacterVisual/LeftLeg", "x", -0.18f, -0.08f, -0.18f, 0.4f);
                SetTransformPositionCurve(clip, "CharacterVisual/RightLeg", "x", 0.08f, 0.18f, 0.08f, 0.4f);
                SetTransformPositionCurve(clip, "CharacterVisual", "y", 0f, 0.035f, 0f, 0.4f);
            }
            else
            {
                SetTransformPositionCurve(clip, "CharacterVisual", "y", 0f, 0.015f, 0f, 1.2f);
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void SetTransformPositionCurve(AnimationClip clip, string path, string axis, float start, float middle, float end, float duration)
        {
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, start),
                new Keyframe(duration * 0.5f, middle),
                new Keyframe(duration, end));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition." + axis), curve);
        }

        private static void CreateVehiclePrefabs(TrafficLightController light, out VehicleController bluePrefab, out VehicleController redPrefab, out VehicleController yellowPrefab)
        {
            bluePrefab = CreateVehiclePrefab(VehiclePrefabPath, "Vehicle", new Color(0.18f, 0.47f, 0.86f, 1f), light);
            CreateVehiclePrefab(CarBluePrefabPath, "CarBlue", new Color(0.18f, 0.47f, 0.86f, 1f), light);
            redPrefab = CreateVehiclePrefab(CarRedPrefabPath, "CarRed", new Color(0.92f, 0.22f, 0.18f, 1f), light);
            yellowPrefab = CreateVehiclePrefab(CarYellowPrefabPath, "CarYellow", new Color(1f, 0.75f, 0.16f, 1f), light);
        }

        private static VehicleController CreateVehiclePrefab(string path, string objectName, Color bodyColor, TrafficLightController light)
        {
            VehicleController prefab = AssetDatabase.LoadAssetAtPath<VehicleController>(path);
            if (prefab != null)
            {
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(path);
                prefabContents.name = objectName;
                RemoveMeshVisuals(prefabContents);
                prefabContents.transform.localScale = Vector3.one;
                ClearChildren(prefabContents.transform);
                ConfigureVehicleRoot(prefabContents, light);
                CreateVehicleVisual(prefabContents.transform, bodyColor);
                PrefabUtility.SaveAsPrefabAsset(prefabContents, path);
                PrefabUtility.UnloadPrefabContents(prefabContents);
                SetFloat(prefab, "stoppingPoint", 2.8f);
                SetFloat(prefab, "exitPoint", -10.5f);
                return prefab;
            }

            GameObject temporary = new GameObject(objectName);
            ConfigureVehicleRoot(temporary, light);
            CreateVehicleVisual(temporary.transform, bodyColor);
            VehicleController controller = temporary.GetComponent<VehicleController>();
            prefab = PrefabUtility.SaveAsPrefabAsset(temporary, path).GetComponent<VehicleController>();
            Object.DestroyImmediate(temporary);
            return prefab;
        }

        private static void ConfigureVehicleRoot(GameObject vehicle, TrafficLightController light)
        {
            Rigidbody2D body = GetOrAdd(vehicle, typeof(Rigidbody2D)) as Rigidbody2D;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            BoxCollider2D collider = GetOrAdd(vehicle, typeof(BoxCollider2D)) as BoxCollider2D;
            collider.size = new Vector2(1.55f, 0.75f);
            collider.offset = new Vector2(0f, 0.02f);
            VehicleController controller = GetOrAdd(vehicle, typeof(VehicleController)) as VehicleController;
            SetReference(controller, "trafficLight", light);
            SetFloat(controller, "stoppingPoint", 2.8f);
            SetFloat(controller, "exitPoint", -10.5f);
            VehicleWheelAnimator wheelAnimator = GetOrAdd(vehicle, typeof(VehicleWheelAnimator)) as VehicleWheelAnimator;
            SetReference(wheelAnimator, "observedBody", body);
        }

        private static void CreateVehicleVisual(Transform parent, Color bodyColor)
        {
            GameObject visual = FindOrCreateChild(parent, "VehicleVisual");
            visual.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            visual.transform.localScale = Vector3.one;
            CreateWorldSprite(visual.transform, "Shadow", new Vector3(0f, -0.48f, 0f), new Vector3(1.55f, 0.16f, 1f), new Color(0f, 0f, 0f, 0.22f), 29, true);
            CreateWorldSprite(visual.transform, "Body", new Vector3(0f, 0f, 0f), new Vector3(1.45f, 0.62f, 1f), bodyColor, 30, false);
            CreateWorldSprite(visual.transform, "Roof", new Vector3(0.08f, 0.3f, 0f), new Vector3(0.82f, 0.34f, 1f), bodyColor * 0.82f, 31, false);
            CreateWorldSprite(visual.transform, "Windshield", new Vector3(0.1f, 0.3f, -0.01f), new Vector3(0.54f, 0.22f, 1f), new Color(0.72f, 0.91f, 0.96f, 1f), 32, false);
            CreateWorldSprite(visual.transform, "RearWindow", new Vector3(-0.28f, 0.3f, -0.01f), new Vector3(0.18f, 0.22f, 1f), new Color(0.58f, 0.82f, 0.9f, 1f), 32, false);
            CreateWorldSprite(visual.transform, "FrontBumper", new Vector3(0.78f, -0.05f, -0.01f), new Vector3(0.08f, 0.25f, 1f), new Color(0.92f, 0.94f, 0.93f, 1f), 32, false);
            CreateWorldSprite(visual.transform, "RearBumper", new Vector3(-0.78f, -0.05f, -0.01f), new Vector3(0.08f, 0.25f, 1f), new Color(0.92f, 0.94f, 0.93f, 1f), 32, false);
            GameObject frontWheel = CreateWorldSprite(visual.transform, "FrontWheel", new Vector3(0.42f, -0.36f, 0f), new Vector3(0.30f, 0.30f, 1f), new Color(0.05f, 0.06f, 0.08f, 1f), 31, true);
            GameObject rearWheel = CreateWorldSprite(visual.transform, "RearWheel", new Vector3(-0.42f, -0.36f, 0f), new Vector3(0.30f, 0.30f, 1f), new Color(0.05f, 0.06f, 0.08f, 1f), 31, true);
            CreateWheelDetails(frontWheel.transform);
            CreateWheelDetails(rearWheel.transform);
            CreateWorldSprite(visual.transform, "Headlight", new Vector3(0.7f, 0.02f, -0.01f), new Vector3(0.1f, 0.14f, 1f), new Color(1f, 0.94f, 0.6f, 1f), 32, true);
            CreateWorldSprite(visual.transform, "RearLight", new Vector3(-0.7f, 0.02f, -0.01f), new Vector3(0.1f, 0.14f, 1f), new Color(0.98f, 0.12f, 0.10f, 1f), 32, true);

            VehicleWheelAnimator wheelAnimator = parent.GetComponent<VehicleWheelAnimator>();
            if (wheelAnimator != null)
            {
                SetReference(wheelAnimator, "frontWheel", frontWheel.transform);
                SetReference(wheelAnimator, "rearWheel", rearWheel.transform);
            }
        }

        private static void CreateWheelDetails(Transform wheel)
        {
            CreateWorldSprite(wheel, "Hub", Vector3.zero, new Vector3(0.42f, 0.42f, 1f), new Color(0.82f, 0.85f, 0.86f, 1f), 32, true);
            CreateWorldSprite(wheel, "SpokeHorizontal", Vector3.zero, new Vector3(0.68f, 0.10f, 1f), new Color(0.45f, 0.48f, 0.50f, 1f), 33, false);
            CreateWorldSprite(wheel, "SpokeVertical", Vector3.zero, new Vector3(0.10f, 0.68f, 1f), new Color(0.45f, 0.48f, 0.50f, 1f), 33, false);
        }

        private static void CreateInitialCars(Transform parent, VehicleController bluePrefab, VehicleController redPrefab, VehicleController yellowPrefab)
        {
            Transform cars = parent.Find("Cars");
            if (cars.Find("Car 1") == null) PrefabUtility.InstantiatePrefab(redPrefab, cars).name = "Car 1";
            if (cars.Find("Car 2") == null) PrefabUtility.InstantiatePrefab(yellowPrefab, cars).name = "Car 2";
            if (cars.Find("Car 3") == null) PrefabUtility.InstantiatePrefab(bluePrefab, cars).name = "Car 3";
            TrafficLightController light = parent.Find("TrafficLight").GetComponent<TrafficLightController>();
            SetReference(cars.Find("Car 1").GetComponent<VehicleController>(), "trafficLight", light);
            SetReference(cars.Find("Car 2").GetComponent<VehicleController>(), "trafficLight", light);
            SetReference(cars.Find("Car 3").GetComponent<VehicleController>(), "trafficLight", light);
            cars.Find("Car 1").position = new Vector3(6f, -1.2f, -1f);
            cars.Find("Car 2").position = new Vector3(1f, -1.2f, -1f);
            cars.Find("Car 3").position = new Vector3(-4f, -1.2f, -1f);
        }

        private static LevelUIController CreateUI(ScoreManager score, TrafficLightController light, PedestrianSignalController pedestrian)
        {
            Canvas canvas = FindReusableCanvas();
            canvas.name = "UI";
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler canvasScaler = GetOrAdd(canvas.gameObject, typeof(CanvasScaler)) as CanvasScaler;
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1280f, 720f);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            GetOrAdd(canvas.gameObject, typeof(GraphicRaycaster));
            ClearChildren(canvas.transform);
            EnsureEventSystem();

            Sprite roundedPanelSprite = EnsureRoundedSprite(RoundedPanelSpritePath, 30);

            GameObject gameplayHud = CreateUIPanel(canvas.transform, "GameplayHUD", new Color(0f, 0f, 0f, 0f));
            SetRect(gameplayHud.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject missionCard = CreateUIPanel(gameplayHud.transform, "MissionCard", new Color(1f, 1f, 1f, 0.94f), roundedPanelSprite);
            SetRect(missionCard.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(360f, 100f));
            AddShadow(missionCard);
            GameObject missionIcon = CreateUIPanel(missionCard.transform, "MissionIcon", new Color(0.08f, 0.10f, 0.12f, 1f), roundedPanelSprite);
            SetRect(missionIcon.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -24f), new Vector2(42f, 56f));
            CreateSignalDot(missionIcon.transform, "RedDot", new Vector2(0f, 15f), new Color(0.96f, 0.22f, 0.18f, 1f));
            CreateSignalDot(missionIcon.transform, "YellowDot", Vector2.zero, new Color(1f, 0.78f, 0.20f, 1f));
            CreateSignalDot(missionIcon.transform, "GreenDot", new Vector2(0f, -15f), new Color(0.20f, 0.74f, 0.35f, 1f));
            TextMeshProUGUI missionTitle = CreateUIText(missionCard.transform, "MissionTitle", "MISSION", 17, TextAlignmentOptions.Left, new Vector2(250f, 24f), new Vector2(82f, -22f), new Vector2(0f, 1f));
            missionTitle.color = new Color(0.09f, 0.45f, 0.62f, 1f);
            missionTitle.fontStyle = FontStyles.Bold;
            TextMeshProUGUI objective = CreateUIText(missionCard.transform, "MissionText", "Cross the road safely", 24, TextAlignmentOptions.Left, new Vector2(250f, 42f), new Vector2(82f, -52f), new Vector2(0f, 1f));
            objective.color = new Color(0.11f, 0.15f, 0.20f, 1f);

            GameObject scoreCard = CreateUIPanel(gameplayHud.transform, "ScoreCard", new Color(1f, 1f, 1f, 0.94f), roundedPanelSprite);
            SetRect(scoreCard.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -36f), new Vector2(170f, 100f));
            AddShadow(scoreCard);
            TextMeshProUGUI scoreIcon = CreateUIText(scoreCard.transform, "ScoreIcon", "*", 28, TextAlignmentOptions.Center, new Vector2(34f, 30f), new Vector2(22f, -18f), new Vector2(0f, 1f));
            scoreIcon.color = new Color(1f, 0.68f, 0.12f, 1f);
            TextMeshProUGUI scoreLabel = CreateUIText(scoreCard.transform, "ScoreTitle", "SCORE", 17, TextAlignmentOptions.Left, new Vector2(92f, 24f), new Vector2(58f, -22f), new Vector2(0f, 1f));
            scoreLabel.color = new Color(0.09f, 0.45f, 0.62f, 1f);
            scoreLabel.fontStyle = FontStyles.Bold;
            TextMeshProUGUI scoreText = CreateUIText(scoreCard.transform, "ScoreValue", "100", 36, TextAlignmentOptions.Center, new Vector2(130f, 42f), new Vector2(20f, -54f), new Vector2(0f, 1f));
            scoreText.color = new Color(0.10f, 0.15f, 0.22f, 1f);
            scoreText.fontStyle = FontStyles.Bold;

            GameObject feedbackObject = CreateUIPanel(gameplayHud.transform, "FeedbackNotification", new Color(1f, 1f, 1f, 0.96f), roundedPanelSprite);
            SetRect(feedbackObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(380f, 75f));
            AddShadow(feedbackObject);
            CanvasGroup feedbackGroup = GetOrAdd(feedbackObject, typeof(CanvasGroup)) as CanvasGroup;
            TextMeshProUGUI feedbackIcon = CreateUIText(feedbackObject.transform, "Icon", "v", 24, TextAlignmentOptions.Center, new Vector2(44f, 44f), new Vector2(22f, 0f), new Vector2(0f, 0.5f));
            feedbackIcon.color = new Color(0.18f, 0.67f, 0.32f, 1f);
            TextMeshProUGUI feedbackText = CreateUIText(feedbackObject.transform, "Message", "", 20, TextAlignmentOptions.Left, new Vector2(280f, 48f), new Vector2(78f, 0f), new Vector2(0f, 0.5f));
            feedbackText.color = new Color(0.11f, 0.15f, 0.20f, 1f);
            FeedbackController feedback = GetOrAdd(feedbackObject, typeof(FeedbackController)) as FeedbackController;
            SetReference(feedback, "iconText", feedbackIcon);
            SetReference(feedback, "feedbackText", feedbackText);
            SetReference(feedback, "canvasGroup", feedbackGroup);
            feedbackObject.SetActive(false);

            GameObject overlay = CreateUIPanel(canvas.transform, "LevelCompleteOverlay", new Color(0f, 0f, 0f, 0f));
            SetRect(overlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CanvasGroup completionGroup = GetOrAdd(overlay, typeof(CanvasGroup)) as CanvasGroup;
            GameObject dimBackground = CreateUIPanel(overlay.transform, "DimBackground", new Color(0.02f, 0.04f, 0.08f, 0.66f));
            SetRect(dimBackground.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject completion = CreateUIPanel(overlay.transform, "LevelCompleteCard", new Color(1f, 1f, 1f, 0.98f), roundedPanelSprite);
            RectTransform completionRect = completion.GetComponent<RectTransform>();
            SetRect(completionRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 480f));
            AddShadow(completion);
            TextMeshProUGUI celebration = CreateUIText(completion.transform, "CelebrationIcon", "*", 34, TextAlignmentOptions.Center, new Vector2(120f, 40f), new Vector2(0f, 196f), new Vector2(0.5f, 0.5f));
            celebration.color = new Color(1f, 0.68f, 0.12f, 1f);
            TextMeshProUGUI title = CreateUIText(completion.transform, "Title", "LEVEL COMPLETE!", 40, TextAlignmentOptions.Center, new Vector2(430f, 50f), new Vector2(0f, 150f), new Vector2(0.5f, 0.5f));
            title.color = new Color(0.08f, 0.31f, 0.47f, 1f);
            title.fontStyle = FontStyles.Bold;
            TextMeshProUGUI subtitle = CreateUIText(completion.transform, "Subtitle", "Great job! You followed the road safety rules.", 19, TextAlignmentOptions.Center, new Vector2(430f, 44f), new Vector2(0f, 108f), new Vector2(0.5f, 0.5f));
            subtitle.color = new Color(0.30f, 0.36f, 0.42f, 1f);
            TextMeshProUGUI finalScoreLabel = CreateUIText(completion.transform, "FinalScoreLabel", "FINAL SCORE", 16, TextAlignmentOptions.Center, new Vector2(220f, 22f), new Vector2(0f, 62f), new Vector2(0.5f, 0.5f));
            finalScoreLabel.color = new Color(0.09f, 0.45f, 0.62f, 1f);
            finalScoreLabel.fontStyle = FontStyles.Bold;
            TextMeshProUGUI finalScore = CreateUIText(completion.transform, "FinalScore", "100", 48, TextAlignmentOptions.Center, new Vector2(220f, 58f), new Vector2(0f, 24f), new Vector2(0.5f, 0.5f));
            finalScore.color = new Color(0.10f, 0.15f, 0.22f, 1f);
            finalScore.fontStyle = FontStyles.Bold;

            GameObject statisticsRow = CreateUIPanel(completion.transform, "StatisticsRow", new Color(0f, 0f, 0f, 0f));
            SetRect(statisticsRow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -56f), new Vector2(412f, 86f));
            TextMeshProUGUI safeActions = CreateStatCard(statisticsRow.transform, "SafeActionsCard", "SAFE ACTIONS", "0", new Vector2(-106f, 0f), new Color(0.18f, 0.67f, 0.32f, 1f), roundedPanelSprite);
            TextMeshProUGUI mistakes = CreateStatCard(statisticsRow.transform, "MistakesCard", "ERRORS", "0", new Vector2(106f, 0f), new Color(0.92f, 0.45f, 0.18f, 1f), roundedPanelSprite);
            TextMeshProUGUI ratingStars = CreateUIText(completion.transform, "Rating", "*****", 28, TextAlignmentOptions.Center, new Vector2(260f, 36f), new Vector2(0f, -126f), new Vector2(0.5f, 0.5f));
            ratingStars.color = new Color(1f, 0.68f, 0.12f, 1f);
            TextMeshProUGUI ratingLabel = CreateUIText(completion.transform, "RatingText", "Excellent!", 20, TextAlignmentOptions.Center, new Vector2(220f, 28f), new Vector2(0f, -158f), new Vector2(0.5f, 0.5f));
            ratingLabel.color = new Color(0.11f, 0.15f, 0.20f, 1f);
            ratingLabel.fontStyle = FontStyles.Bold;
            Button backButton = GetOrAddChildButton(completion.transform, "BackToMenuButton", "BACK TO MENU", new Vector2(0f, -210f), roundedPanelSprite);
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
                    if (selectedCanvas != null && selectedCanvas != canvas)
                    {
                        Undo.DestroyObjectImmediate(selectedCanvas.gameObject);
                    }

                    selectedCanvas = canvas;
                    continue;
                }

                Undo.DestroyObjectImmediate(canvas.gameObject);
            }

            return selectedCanvas != null ? selectedCanvas : new GameObject("UI").AddComponent<Canvas>();
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Color color, Sprite sprite = null)
        {
            GameObject panel = FindOrCreateChild(parent, name);
            EnsureRectTransform(panel);
            Image image = GetOrAdd(panel, typeof(Image)) as Image;
            image.sprite = sprite != null ? sprite : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return panel;
        }

        private static TextMeshProUGUI CreateUIText(Transform parent, string name, string value, int size, TextAlignmentOptions alignment, Vector2 dimensions, Vector2 position, Vector2 anchor)
        {
            GameObject textObject = FindOrCreateChild(parent, name);
            EnsureRectTransform(textObject);
            Text oldText = textObject.GetComponent<Text>();
            if (oldText != null) Undo.DestroyObjectImmediate(oldText);

            TextMeshProUGUI text = GetOrAdd(textObject, typeof(TextMeshProUGUI)) as TextMeshProUGUI;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            SetRect(text.rectTransform, anchor, anchor, position, dimensions);
            return text;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment, Vector2 dimensions, Vector2 position)
        {
            GameObject textObject = FindOrCreateChild(parent, name);
            EnsureRectTransform(textObject);
            Text text = GetOrAdd(textObject, typeof(Text)) as Text;
            text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.alignment = alignment; text.color = Color.white;
            SetRect(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), position, dimensions);
            return text;
        }

        private static Button GetOrAddChildButton(Transform parent, string name, string label, Vector2 position, Sprite sprite)
        {
            GameObject objectRoot = FindOrCreateChild(parent, name);
            EnsureRectTransform(objectRoot);
            Button button = GetOrAdd(objectRoot, typeof(Button)) as Button;
            Image image = GetOrAdd(objectRoot, typeof(Image)) as Image;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.12f, 0.55f, 0.84f, 1f);
            image.raycastTarget = true;
            button.targetGraphic = image;
            AddShadow(objectRoot);
            AnimatedUIButton animatedButton = GetOrAdd(objectRoot, typeof(AnimatedUIButton)) as AnimatedUIButton;
            SetReference(animatedButton, "targetImage", image);
            TextMeshProUGUI text = CreateUIText(objectRoot.transform, "Label", label, 20, TextAlignmentOptions.Center, new Vector2(250f, 60f), Vector2.zero, new Vector2(0.5f, 0.5f));
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            SetRect(EnsureRectTransform(objectRoot), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(250f, 60f));
            SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 60f));
            return button;
        }

        private static void CreateSignalDot(Transform parent, string name, Vector2 position, Color color)
        {
            GameObject dot = CreateUIPanel(parent, name, color, EnsureTrafficLightSprite(WorldCircleSpritePath, true));
            SetRect(dot.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(12f, 12f));
        }

        private static TextMeshProUGUI CreateStatCard(Transform parent, string name, string label, string value, Vector2 position, Color accentColor, Sprite sprite)
        {
            GameObject card = CreateUIPanel(parent, name, new Color(0.94f, 0.98f, 1f, 1f), sprite);
            SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(190f, 80f));

            TextMeshProUGUI labelText = CreateUIText(card.transform, "Label", label, 15, TextAlignmentOptions.Center, new Vector2(170f, 22f), new Vector2(0f, 16f), new Vector2(0.5f, 0.5f));
            labelText.color = accentColor;
            labelText.fontStyle = FontStyles.Bold;

            TextMeshProUGUI valueText = CreateUIText(card.transform, "Value", value, 30, TextAlignmentOptions.Center, new Vector2(120f, 36f), new Vector2(0f, -16f), new Vector2(0.5f, 0.5f));
            valueText.color = new Color(0.10f, 0.15f, 0.22f, 1f);
            valueText.fontStyle = FontStyles.Bold;
            return valueText;
        }

        private static void AddShadow(GameObject objectRoot)
        {
            Shadow shadow = GetOrAdd(objectRoot, typeof(Shadow)) as Shadow;
            shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            shadow.effectDistance = new Vector2(0f, -4f);
            shadow.useGraphicAlpha = true;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x, anchorMin.y);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static RectTransform EnsureRectTransform(GameObject objectRoot)
        {
            RectTransform rect = objectRoot.GetComponent<RectTransform>();
            if (rect == null)
            {
                Undo.RecordObject(objectRoot, "Add UI RectTransform");
                rect = objectRoot.AddComponent<RectTransform>();
            }

            return rect;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null) { eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>(); eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(); }
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
            RemoveMeshVisuals(objectRoot);
            SpriteRenderer renderer = GetOrAdd(objectRoot, typeof(SpriteRenderer)) as SpriteRenderer;
            renderer.sprite = EnsureTrafficLightSprite(circle ? WorldCircleSpritePath : WorldSquareSpritePath, circle);
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return objectRoot;
        }

        private static TextMesh CreateWorldText(Transform parent, string name, string value, Vector3 position, float characterSize, TextAnchor anchor, Color color, int sortingOrder)
        {
            GameObject objectRoot = FindOrCreateChild(parent, name);
            objectRoot.transform.localPosition = position;
            objectRoot.transform.localScale = Vector3.one;
            TextMesh text = GetOrAdd(objectRoot, typeof(TextMesh)) as TextMesh;
            text.text = value;
            text.characterSize = characterSize;
            text.anchor = anchor;
            text.alignment = TextAlignment.Center;
            text.color = color;
            text.fontSize = 64;
            text.GetComponent<Renderer>().sortingOrder = sortingOrder;
            return text;
        }

        private static GameObject CreateVisual(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject objectRoot = parent == null ? new GameObject(name) : FindOrCreateChild(parent, name);
            objectRoot.transform.localPosition = position; objectRoot.transform.localScale = scale;
            MeshRenderer renderer = GetOrAdd(objectRoot, typeof(MeshRenderer)) as MeshRenderer;
            MeshFilter filter = GetOrAdd(objectRoot, typeof(MeshFilter)) as MeshFilter;
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube); filter.sharedMesh = cube.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(cube);
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")); renderer.sharedMaterial.color = color;
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
            Undo.RecordObject(lightObject, "Configure signal light");
            lightObject.transform.localPosition = position;
            lightObject.transform.localScale = Vector3.one * 0.45f;

            MeshRenderer meshRenderer = lightObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Undo.DestroyObjectImmediate(meshRenderer);
            }
            MeshFilter meshFilter = lightObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Undo.DestroyObjectImmediate(meshFilter);
            }

            SpriteRenderer spriteRenderer = GetOrAdd(lightObject, typeof(SpriteRenderer)) as SpriteRenderer;
            Undo.RecordObject(spriteRenderer, "Configure signal sprite");
            spriteRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 5;
            return spriteRenderer;
        }

        private static void RemoveUnexpectedTrafficLightChildren(Transform trafficLight)
        {
            RenameLegacyTrafficLightChild(trafficLight, "Red", "RedLight");
            RenameLegacyTrafficLightChild(trafficLight, "Yellow", "YellowLight");
            RenameLegacyTrafficLightChild(trafficLight, "Green", "GreenLight");

            HashSet<string> keptNames = new HashSet<string>();
            for (int childIndex = trafficLight.childCount - 1; childIndex >= 0; childIndex--)
            {
                Transform child = trafficLight.GetChild(childIndex);
                bool expected = child.name == "TrafficLightBody" || child.name == "RedLight" || child.name == "YellowLight" || child.name == "GreenLight";
                if (!expected || keptNames.Contains(child.name))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    continue;
                }

                keptNames.Add(child.name);
            }
        }

        private static void RenameLegacyTrafficLightChild(Transform parent, string legacyName, string canonicalName)
        {
            Transform canonical = parent.Find(canonicalName);
            Transform legacy = parent.Find(legacyName);
            if (canonical == null && legacy != null)
            {
                legacy.name = canonicalName;
            }
        }

        private static void RemoveMeshVisuals(GameObject objectRoot)
        {
            MeshRenderer meshRenderer = objectRoot.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Undo.DestroyObjectImmediate(meshRenderer);
            }

            MeshFilter meshFilter = objectRoot.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Undo.DestroyObjectImmediate(meshFilter);
            }
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

        private static Sprite EnsureTrafficLightSprite(string assetPath, bool circle)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            const int textureSize = 128;
            string directory = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

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
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = textureSize;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Sprite EnsureRoundedSprite(string assetPath, int radius)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            const int textureSize = 96;
            string directory = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[textureSize * textureSize];
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 clear = new Color32(255, 255, 255, 0);

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float nearestX = Mathf.Clamp(x, radius, textureSize - radius - 1);
                    float nearestY = Mathf.Clamp(y, radius, textureSize - radius - 1);
                    float offsetX = x - nearestX;
                    float offsetY = y - nearestY;
                    bool inside = offsetX * offsetX + offsetY * offsetY <= radius * radius;
                    pixels[y * textureSize + x] = inside ? white : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = textureSize;
                importer.spriteBorder = Vector4.one * radius;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        private static void EnsureAssetFolders()
        {
            EnsureFolder("Assets/Sprites");
            EnsureFolder("Assets/Sprites/Environment");
            EnsureFolder("Assets/Sprites/Vehicles");
            EnsureFolder("Assets/Sprites/Player");
            EnsureFolder("Assets/Sprites/Traffic");
            EnsureFolder("Assets/Animations");
            EnsureFolder("Assets/Animations/Player");
            EnsureFolder("Assets/Animations/Vehicles");
            EnsureFolder("Assets/Prefabs");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folder);
        }

        private static void SavePrefabCopy(GameObject source, string path)
        {
            if (source == null)
            {
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(source, path);
        }

        private static GameObject FindOrCreate(string name) { GameObject found = GameObject.Find(name); return found != null ? found : new GameObject(name); }
        private static GameObject FindOrCreateOnlyChild(Transform parent, string name) { GameObject found = null; for (int childIndex = parent.childCount - 1; childIndex >= 0; childIndex--) { Transform child = parent.GetChild(childIndex); if (child.name != name) continue; if (found == null) { found = child.gameObject; } else { Undo.DestroyObjectImmediate(child.gameObject); } } if (found != null) return found; GameObject childObject = new GameObject(name); childObject.transform.SetParent(parent); return childObject; }
        private static GameObject FindOrCreateChild(Transform parent, string name) { Transform found = parent.Find(name); if (found != null) return found.gameObject; GameObject child = new GameObject(name); child.transform.SetParent(parent); return child; }
        private static Component GetOrAdd(GameObject objectRoot, System.Type type) { Component component = objectRoot.GetComponent(type); return component != null ? component : objectRoot.AddComponent(type); }
        private static void SetReference(Object target, string property, Object value) { SerializedObject serialized = new SerializedObject(target); SerializedProperty serializedProperty = serialized.FindProperty(property); if (serializedProperty == null) { Debug.LogWarning(target.name + " is missing serialized property " + property + "."); return; } serializedProperty.objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetFloat(Object target, string property, float value) { SerializedObject serialized = new SerializedObject(target); serialized.FindProperty(property).floatValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetInt(Object target, string property, int value) { SerializedObject serialized = new SerializedObject(target); serialized.FindProperty(property).intValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        private static void EnsureBuildSettingsScenes() { List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes); if (!scenes.Exists(scene => scene.path == "Assets/Scenes/Level1.unity")) scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Level1.unity", true)); EditorBuildSettings.scenes = scenes.ToArray(); }
    }
}
#endif
