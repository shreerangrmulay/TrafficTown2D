#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using TrafficTown2D.Core;
using TrafficTown2D.Level3;
using TrafficTown2D.UI;

namespace TrafficTown2D.Editor
{
    public static class Level3Setup
    {
        private const string Level3ScenePath = "Assets/Scenes/Level3.unity";
        private const string WorldSquareSpritePath = "Assets/Sprites/Generated/WorldSquare.png";
        private const string WorldCircleSpritePath = "Assets/Sprites/Generated/WorldCircle.png";
        private const string RoundedPanelSpritePath = "Assets/UI/RoundedPanel.png";

        private const string CarBlueTopDownPath = "Assets/Sprites/Vehicles/CarBlueTopDown.png";
        private const string CarRedTopDownPath = "Assets/Sprites/Vehicles/CarRedTopDown.png";
        private const string TaxiTopDownPath = "Assets/Sprites/Vehicles/TaxiTopDown.png";
        private const string BusTopDownPath = "Assets/Sprites/Vehicles/BusTopDown.png";
        private const string AmbulanceTopDownPath = "Assets/Sprites/Vehicles/AmbulanceTopDown.png";
        private const string CheckeredFinishPath = "Assets/Sprites/Environment/CheckeredFinish.png";
        private const string ZebraCrossingPath = "Assets/Sprites/Environment/ZebraCrossingStripes.png";
        private const string SignPostBasePath = "Assets/Sprites/Environment/SignPostBase.png";
        private const string ChildBoyTopDownPath = "Assets/Sprites/Characters/ChildBoyTopDown.png";
        private const string ChildGirlTopDownPath = "Assets/Sprites/Characters/ChildGirlTopDown.png";
        private const string CrossingGuardTopDownPath = "Assets/Sprites/Characters/CrossingGuardTopDown.png";

        private const string SignsFolderPath = "Assets/Resources/Signs";
        private const string AutoRunPrefKey = "TrafficTown_Level3_AutoSetup_v6";

        [InitializeOnLoadMethod]
        private static void AutoSetupOnce()
        {
            if (!EditorPrefs.GetBool(AutoRunPrefKey, false))
            {
                EditorPrefs.SetBool(AutoRunPrefKey, true);
                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication.isPlaying)
                    {
                        Debug.Log("[Level3Setup] Auto-running SetupLevel3 on compile...");
                        SetupLevel3();
                    }
                };
            }
        }

        [MenuItem("TrafficTown/Setup Level 3")]
        public static void SetupLevel3()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before running TrafficTown -> Setup Level 3.");
                return;
            }

            EnsureAssetFolders();
            ConfigureSpriteImporters();

            Scene level3Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(level3Scene, Level3ScenePath);

            // 1. Camera & Global 2D Light
            Camera cam = EnsureCamera();
            EnsureGlobalLight();

            // 2. Services & EventSystem
            GameObject services = FindOrCreate("Services");
            SceneLoader sceneLoader = GetOrAdd<SceneLoader>(services);
            GameManager gm = GetOrAdd<GameManager>(services);
            SetInt(gm, "startingState", (int)GameState.Playing);
            gm.SetState(GameState.Playing);
            EditorUtility.SetDirty(gm);
            Time.timeScale = 1f;
            EnsureEventSystem();

            // 3. World (Route: Y = -10 to Y = 135)
            GameObject world = FindOrCreate("World");
            ClearChildren(world.transform);
            CreateWorldEnvironment(world.transform);

            // 4. Player Car
            GameObject playerCarGO = CreatePlayerCar();
            Level3PlayerCar playerCar = playerCarGO.GetComponent<Level3PlayerCar>();

            // Setup Camera Follow
            TopDownCameraFollow camFollow = cam.GetComponent<TopDownCameraFollow>();
            if (camFollow == null) camFollow = cam.gameObject.AddComponent<TopDownCameraFollow>();
            camFollow.SetTarget(playerCarGO.transform);
            camFollow.SetBounds(-6f, 160f);
            SetReference(camFollow, "target", playerCarGO.transform);
            SetFloat(camFollow, "maxY", 160f);
            SetFloat(camFollow, "minY", -6f);

            // 5. Mission 1: School Zone Components
            Level3Pedestrian schoolPedestrian = CreateSchoolZone(world.transform, playerCarGO.transform);

            // 6. Mission 2: Intersection Components
            Level3TrafficLight intersectionLight;
            GameObject[] crossTraffic = CreateIntersection(world.transform, out intersectionLight);

            // 7. Mission 3: Emergency Vehicle (Ambulance)
            Level3TopDownVehicle ambulance = CreateAmbulance(world.transform);

            // 8. Mission 4: Scenery for Distraction Zone
            CreateDistractionZoneScenery(world.transform);

            // 9. Mission 5: Final Drive & Finish Line
            Level3Pedestrian finalPedestrian = CreateFinalDriveAndFinish(world.transform, playerCarGO.transform);

            // 10. UI Canvas
            Level3UIController ui = CreateUI(playerCar);

            // 11. Mission Manager
            GameObject missionManagerGO = FindOrCreate("MissionManager");
            Level3MissionManager missionMgr = GetOrAdd<Level3MissionManager>(missionManagerGO);
            SetReference(missionMgr, "playerCar", playerCar);
            SetReference(missionMgr, "cameraFollow", camFollow);
            SetReference(missionMgr, "uiController", ui);
            SetReference(missionMgr, "intersectionLight", intersectionLight);
            SetReference(missionMgr, "schoolPedestrian", schoolPedestrian);
            SetReference(missionMgr, "finalPedestrian", finalPedestrian);
            SetReference(missionMgr, "ambulanceVehicle", ambulance);
            SetObjectArray(missionMgr, "crossTrafficVehicles", crossTraffic);

            // 12. Build Settings Registration
            BuildSettingsUtility.EnsureAllScenesInBuildSettings();

            EditorSceneManager.MarkSceneDirty(level3Scene);
            EditorSceneManager.SaveScene(level3Scene);
            Selection.activeGameObject = playerCarGO;
            Debug.Log("[Level3Setup] TrafficTown Level 3: 'SAFE CITY DRIVER' created successfully!");
        }

        #region Camera & Lighting
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
            cam.backgroundColor = TrafficTownTheme.GrassColor;
            cam.clearFlags = CameraClearFlags.SolidColor;

            UniversalAdditionalCameraData urpCam = cam.GetComponent<UniversalAdditionalCameraData>();
            if (urpCam == null) cam.gameObject.AddComponent<UniversalAdditionalCameraData>();

            return cam;
        }

        private static void EnsureGlobalLight()
        {
            Light2D light = Object.FindAnyObjectByType<Light2D>();
            if (light == null)
            {
                GameObject lightObj = new GameObject("Global 2D Light");
                light = lightObj.AddComponent<Light2D>();
            }
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1.0f;
            light.color = Color.white;
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }
        #endregion

        #region World Construction
        private static void CreateWorldEnvironment(Transform world)
        {
            Sprite squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WorldSquareSpritePath);

            // 1. Large Grass Background (Centered at Y = 85, Length = 230, covers Y = -30 to Y = 200)
            GameObject ground = CreateWorldSprite(world, "GrassGround", new Vector3(0f, 85f, 5f), new Vector3(60f, 230f, 1f), TrafficTownTheme.GrassColor, -20);

            // 2. Main Vertical Road Corridor (Width = 6.2, Length = 210, covers Y = -20 to Y = 190)
            GameObject roadCorridor = CreateWorldSprite(world, "MainRoad", new Vector3(0f, 85f, 0f), new Vector3(6.2f, 210f, 1f), TrafficTownTheme.RoadColor, -10);

            // 3. Sidewalks
            CreateWorldSprite(world, "LeftSidewalk", new Vector3(-4.3f, 85f, 0f), new Vector3(2.4f, 210f, 1f), TrafficTownTheme.SidewalkColor, -12);
            CreateWorldSprite(world, "RightSidewalk", new Vector3(4.3f, 85f, 0f), new Vector3(2.4f, 210f, 1f), TrafficTownTheme.SidewalkColor, -12);

            // 4. Road Edge Curbs (White / Light Slate borders)
            CreateWorldSprite(world, "LeftCurb", new Vector3(-3.1f, 85f, 0f), new Vector3(0.18f, 210f, 1f), TrafficTownTheme.SidewalkCurbColor, -9);
            CreateWorldSprite(world, "RightCurb", new Vector3(3.1f, 85f, 0f), new Vector3(0.18f, 210f, 1f), TrafficTownTheme.SidewalkCurbColor, -9);

            // 5. Dashed Center Line (Yellow)
            GameObject centerLines = FindOrCreateChild(world, "CenterDashedLines");
            for (float y = -8f; y <= 175f; y += 4f)
            {
                // Skip intersections
                if (y >= 47f && y <= 57f) continue;
                CreateWorldSprite(centerLines.transform, $"Dash_{y}", new Vector3(0f, y, 0f), new Vector3(0.18f, 2.2f, 1f), TrafficTownTheme.RoadMarkingColor, -8);
            }

            // 6. Invisible Road Shoulder Slowdown Triggers
            CreateRoadShoulderTrigger(world, "LeftShoulderTrigger", new Vector3(-3.5f, 85f, 0f), new Vector2(1.2f, 210f));
            CreateRoadShoulderTrigger(world, "RightShoulderTrigger", new Vector3(3.5f, 85f, 0f), new Vector2(1.2f, 210f));

            // 7. Scenery along the road (Trees, Lamps, Buildings)
            CreateRoadsideScenery(world);
        }

        private static void CreateRoadShoulderTrigger(Transform parent, string name, Vector3 pos, Vector2 size)
        {
            GameObject triggerObj = FindOrCreateChild(parent, name);
            triggerObj.transform.position = pos;
            BoxCollider2D box = GetOrAdd<BoxCollider2D>(triggerObj);
            box.isTrigger = true;
            box.size = size;

            RoadShoulderTrigger trigger = GetOrAdd<RoadShoulderTrigger>(triggerObj);
        }

        private static void CreateRoadsideScenery(Transform parent)
        {
            GameObject scenery = FindOrCreateChild(parent, "Scenery");
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WorldCircleSpritePath);
            Sprite squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WorldSquareSpritePath);

            // Trees and houses along the road up to Y = 175
            for (float y = -6f; y <= 175f; y += 14f)
            {
                if ((y >= 20f && y <= 32f) || (y >= 46f && y <= 58f) || (y >= 72f && y <= 82f) || (y >= 124f && y <= 134f))
                    continue; // Leave room for mission buildings and finish line

                // Left side tree
                CreateTree(scenery.transform, new Vector3(-6.5f, y, 0f));
                // Right side tree
                CreateTree(scenery.transform, new Vector3(6.5f, y + 6f, 0f));

                // Houses set back
                if (y % 28 == 0)
                {
                    CreateCartoonHouse(scenery.transform, new Vector3(-9.5f, y + 2f, 0f), new Color(0.88f, 0.82f, 0.72f, 1f));
                    CreateCartoonHouse(scenery.transform, new Vector3(9.5f, y + 8f, 0f), new Color(0.72f, 0.82f, 0.90f, 1f));
                }
            }
        }

        private static void CreateTree(Transform parent, Vector3 pos)
        {
            GameObject tree = FindOrCreateChild(parent, $"Tree_{pos.y}");
            tree.transform.position = pos;
            // Trunk
            CreateWorldSprite(tree.transform, "Trunk", new Vector3(0f, -0.4f, 0f), new Vector3(0.35f, 0.8f, 1f), new Color(0.50f, 0.32f, 0.18f, 1f), -4);
            // Foliage circle
            CreateWorldSprite(tree.transform, "Foliage", new Vector3(0f, 0.3f, -0.01f), new Vector3(1.6f, 1.6f, 1f), new Color(0.25f, 0.65f, 0.30f, 1f), -3, true);
            CreateWorldSprite(tree.transform, "FoliageTop", new Vector3(-0.1f, 0.45f, -0.02f), new Vector3(1.2f, 1.2f, 1f), new Color(0.35f, 0.75f, 0.38f, 1f), -2, true);
        }

        private static void CreateCartoonHouse(Transform parent, Vector3 pos, Color wallColor)
        {
            GameObject house = FindOrCreateChild(parent, $"House_{pos.y}");
            house.transform.position = pos;
            // Walls
            CreateWorldSprite(house.transform, "Walls", Vector3.zero, new Vector3(3.4f, 2.8f, 1f), wallColor, -5);
            // Roof
            CreateWorldSprite(house.transform, "Roof", new Vector3(0f, 0.4f, -0.01f), new Vector3(3.6f, 2.2f, 1f), new Color(0.75f, 0.30f, 0.25f, 1f), -4);
            // Door
            CreateWorldSprite(house.transform, "Door", new Vector3(0.5f, -0.7f, -0.02f), new Vector3(0.7f, 1.0f, 1f), new Color(0.40f, 0.25f, 0.15f, 1f), -3);
        }
        #endregion

        #region Player Car
        private static GameObject CreatePlayerCar()
        {
            GameObject car = GameObject.Find("PlayerCar");
            if (car == null) car = new GameObject("PlayerCar");

            car.transform.position = new Vector3(1.1f, -4.0f, 0f); // Right lane
            car.transform.rotation = Quaternion.identity; // Facing UP
            car.tag = "Player";

            Sprite carSprite = LoadSpriteAsset(CarBlueTopDownPath);
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(car);
            sr.sprite = carSprite;
            sr.sortingOrder = 10;
            car.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            Rigidbody2D rb = GetOrAdd<Rigidbody2D>(car);
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D col = GetOrAdd<BoxCollider2D>(car);
            col.size = new Vector2(1.6f, 3.2f);
            col.offset = Vector2.zero;

            Level3PlayerCar playerCar = GetOrAdd<Level3PlayerCar>(car);

            // Add subtle Brake Light indicators at rear corners
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WorldCircleSpritePath);
            SpriteRenderer blLeft = CreateLightRenderer(car.transform, "BrakeLight_L", new Vector3(-0.65f, -1.6f, -0.02f), circleSprite);
            SpriteRenderer blRight = CreateLightRenderer(car.transform, "BrakeLight_R", new Vector3(0.65f, -1.6f, -0.02f), circleSprite);
            SetObjectArray(playerCar, "brakeLights", new[] { blLeft, blRight });

            return car;
        }

        private static SpriteRenderer CreateLightRenderer(Transform parent, string name, Vector3 localPos, Sprite circleSprite)
        {
            GameObject lightObj = FindOrCreateChild(parent, name);
            lightObj.transform.localPosition = localPos;
            lightObj.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(lightObj);
            sr.sprite = circleSprite;
            sr.color = new Color(0.6f, 0.1f, 0.1f, 0.6f);
            sr.sortingOrder = 12;
            return sr;
        }
        #endregion

        #region Mission 1: School Zone
        private static Level3Pedestrian CreateSchoolZone(Transform world, Transform player)
        {
            GameObject mission1 = FindOrCreateChild(world, "Mission1_SchoolZone");
            ClearChildren(mission1.transform);

            // School Campus on Left Sidewalk (X = -8.5f, Y = 28f)
            GameObject school = FindOrCreateChild(mission1.transform, "SchoolBuilding");
            school.transform.position = new Vector3(-8.5f, 28f, 0f);
            // School Main Hall
            CreateWorldSprite(school.transform, "MainHall", Vector3.zero, new Vector3(5.2f, 4.4f, 1f), new Color(0.92f, 0.85f, 0.65f, 1f), -5);
            // School Roof
            CreateWorldSprite(school.transform, "Roof", new Vector3(0f, 0.8f, -0.01f), new Vector3(5.5f, 3.2f, 1f), new Color(0.22f, 0.45f, 0.65f, 1f), -4);
            // Cupola / Bell Tower
            CreateWorldSprite(school.transform, "Tower", new Vector3(0f, 2.6f, -0.02f), new Vector3(1.4f, 1.2f, 1f), new Color(0.92f, 0.85f, 0.65f, 1f), -3);
            CreateWorldSprite(school.transform, "TowerRoof", new Vector3(0f, 3.4f, -0.03f), new Vector3(1.6f, 0.8f, 1f), new Color(0.85f, 0.28f, 0.22f, 1f), -2);
            // Clock Face
            CreateWorldSprite(school.transform, "Clock", new Vector3(0f, 2.6f, -0.03f), new Vector3(0.65f, 0.65f, 1f), Color.white, -2, true);
            // School Sign Board
            CreateWorldSprite(school.transform, "SignBadge", new Vector3(0f, 0.3f, -0.02f), new Vector3(3.4f, 0.8f, 1f), new Color(0.15f, 0.35f, 0.55f, 1f), -3);

            // Courtyard Trees & Picket Fence
            for (float fy = 21f; fy <= 35f; fy += 1.6f)
            {
                CreateWorldSprite(mission1.transform, $"Fence_{fy}", new Vector3(-5.8f, fy, 0f), new Vector3(0.12f, 1.2f, 1f), Color.white, -3);
            }
            CreateTree(mission1.transform, new Vector3(-7.5f, 22.0f, 0f));
            CreateTree(mission1.transform, new Vector3(-7.5f, 34.0f, 0f));

            // Roadside Signboards for School Zone
            // 1. Advance "SCHOOL AHEAD" sign on Left Sidewalk (Y = 12.0)
            CreateRoadSign(mission1.transform, "SchoolZoneSign_Advance_L", new Vector3(-4.2f, 12.0f, 0f), "SCHOOL AHEAD", 0.68f);
            // 2. "SPEED LIMIT" 20 sign on Right Sidewalk (Y = 14.5)
            CreateRoadSign(mission1.transform, "SpeedLimit20Sign", new Vector3(4.2f, 14.5f, 0f), "SPEED LIMIT", 0.62f);
            // 3. Near "SCHOOL AHEAD" sign on Left Sidewalk approaching crosswalk (Y = 22.5)
            CreateRoadSign(mission1.transform, "SchoolZoneSign_Near_L", new Vector3(-4.2f, 22.5f, 0f), "SCHOOL AHEAD", 0.68f);
            // 4. "PEDESTRIAN CROSSING" sign on Right Sidewalk approaching crosswalk (Y = 22.5)
            CreateRoadSign(mission1.transform, "PedestrianCrossingSign_R", new Vector3(4.2f, 22.5f, 0f), "PEDESTRIAN CROSSING", 0.62f);

            // Zebra Crossing at Y = 27.0 (Spans full road width: X = -3.0 to +3.0)
            GameObject crossing = FindOrCreateChild(mission1.transform, "SchoolCrosswalk");
            crossing.transform.position = new Vector3(0f, 27f, 0f);
            GameObject zebraObj = CreateWorldSprite(crossing.transform, "ZebraStripes", Vector3.zero, new Vector3(1.20f, 1.35f, 1f), Color.white, -7);
            Sprite zebraSprite = LoadSpriteAsset(ZebraCrossingPath);
            if (zebraSprite != null)
            {
                zebraObj.GetComponent<SpriteRenderer>().sprite = zebraSprite;
            }

            // Stop Line at Y = 24.5 across right driving lane (X = 0 to 3.0, centered at X = 1.5)
            CreateWorldSprite(mission1.transform, "StopLine", new Vector3(1.5f, 24.5f, 0f), new Vector3(3.0f, 0.35f, 1f), Color.white, -6);

            // School Pedestrian Group (Crossing Guard + School Boy + School Girl)
            GameObject pedGO = FindOrCreateChild(mission1.transform, "SchoolPedestrian");
            pedGO.transform.position = new Vector3(-4.5f, 27f, 0f);
            GameObject visual = FindOrCreateChild(pedGO.transform, "Visual");
            ClearChildren(visual.transform);

            // Crossing Guard (Leading with STOP sign paddle)
            CreatePersonSprite(visual.transform, "CrossingGuard", CrossingGuardTopDownPath, new Vector3(0.85f, 0.05f, 0f), new Vector3(0.48f, 0.48f, 1f), 7);
            // School Boy (Yellow shirt, navy cap, blue backpack)
            CreatePersonSprite(visual.transform, "SchoolBoy", ChildBoyTopDownPath, new Vector3(0.0f, 0.28f, 0f), new Vector3(0.42f, 0.42f, 1f), 6);
            // School Girl (Coral shirt, hair buns, purple backpack)
            CreatePersonSprite(visual.transform, "SchoolGirl", ChildGirlTopDownPath, new Vector3(-0.75f, -0.22f, 0f), new Vector3(0.40f, 0.40f, 1f), 6);

            BoxCollider2D pedCol = GetOrAdd<BoxCollider2D>(pedGO);
            pedCol.size = new Vector2(2.6f, 1.2f);
            pedCol.isTrigger = true;

            Level3Pedestrian ped = GetOrAdd<Level3Pedestrian>(pedGO);
            ped.Configure(new Vector3(-4.5f, 27f, 0f), new Vector3(4.5f, 27f, 0f), player);
            ped.SetVisualRoot(visual.transform);
            SetVector3(ped, "startPosition", new Vector3(-4.5f, 27f, 0f));
            SetVector3(ped, "targetPosition", new Vector3(4.5f, 27f, 0f));
            SetReference(ped, "playerTransform", player);
            SetReference(ped, "visualRoot", visual.transform);
            SetFloat(ped, "walkSpeed", 1.5f);
            SetFloat(ped, "triggerDistance", 10f);
            SetBool(ped, "triggerOnProximity", true);
            EditorUtility.SetDirty(ped);

            return ped;
        }

        private static GameObject CreateRoadSign(Transform parent, string name, Vector3 pos, string signNameOrPath, float visualScale = 0.65f)
        {
            GameObject signGO = FindOrCreateChild(parent, name);
            signGO.transform.position = pos;
            ClearChildren(signGO.transform);

            // Ground Shadow
            CreateWorldSprite(signGO.transform, "GroundShadow", new Vector3(0f, -0.65f, 0.05f), new Vector3(0.85f, 0.35f, 1f), new Color(0f, 0f, 0f, 0.28f), 1, true);

            // Base Footing
            CreateWorldSprite(signGO.transform, "Footing", new Vector3(0f, -0.58f, 0.04f), new Vector3(0.42f, 0.16f, 1f), new Color(0.28f, 0.30f, 0.34f, 1f), 2);

            // Vertical Pole / Post
            CreateWorldSprite(signGO.transform, "Post", new Vector3(0f, 0.05f, 0.03f), new Vector3(0.14f, 1.4f, 1f), new Color(0.50f, 0.52f, 0.56f, 1f), 3);

            // Backing Plate (creates contrast against road/grass)
            CreateWorldSprite(signGO.transform, "Backplate", new Vector3(0f, 0.75f, 0.02f), new Vector3(visualScale * 2.7f, visualScale * 2.7f, 1f), new Color(0.18f, 0.20f, 0.24f, 0.85f), 14);

            // Actual Sign Face
            Sprite signSprite = LoadSignSprite(signNameOrPath);
            GameObject face = FindOrCreateChild(signGO.transform, "Face");
            face.transform.localPosition = new Vector3(0f, 0.75f, 0.01f);
            face.transform.localScale = new Vector3(visualScale, visualScale, 1f);
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(face);
            sr.sprite = signSprite;
            sr.color = Color.white;
            sr.sortingOrder = 15;

            return signGO;
        }
        #endregion

        #region Mission 2: Intersection
        private static GameObject[] CreateIntersection(Transform world, out Level3TrafficLight trafficLight)
        {
            GameObject mission2 = FindOrCreateChild(world, "Mission2_Intersection");

            // Horizontal Cross Street (Length 40, Width 6.0, at Y = 52)
            CreateWorldSprite(mission2.transform, "CrossStreetRoad", new Vector3(0f, 52f, 0f), new Vector3(40f, 6.0f, 1f), TrafficTownTheme.RoadColor, -10);
            CreateWorldSprite(mission2.transform, "CrossStreetCurbsTop", new Vector3(0f, 55.1f, 0f), new Vector3(40f, 0.2f, 1f), TrafficTownTheme.SidewalkCurbColor, -9);
            CreateWorldSprite(mission2.transform, "CrossStreetCurbsBottom", new Vector3(0f, 48.9f, 0f), new Vector3(40f, 0.2f, 1f), TrafficTownTheme.SidewalkCurbColor, -9);

            // Vertical Stop line for player at Y = 46.5
            CreateWorldSprite(mission2.transform, "PlayerStopLine", new Vector3(1.5f, 46.5f, 0f), new Vector3(3.0f, 0.35f, 1f), Color.white, -7);

            // Traffic Light Gantry (Overhead / Corner at X = 3.6, Y = 51)
            GameObject lightGO = FindOrCreateChild(mission2.transform, "IntersectionTrafficLight");
            lightGO.transform.position = new Vector3(3.8f, 50.5f, 0f);

            // Housing box
            CreateWorldSprite(lightGO.transform, "Housing", Vector3.zero, new Vector3(0.9f, 2.2f, 1f), new Color(0.12f, 0.14f, 0.16f, 1f), 20);

            // Lenses (Red, Yellow, Green)
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WorldCircleSpritePath);
            SpriteRenderer redLens = CreateLens(lightGO.transform, "RedLens", new Vector3(0f, 0.65f, -0.01f), circleSprite, Color.red);
            SpriteRenderer yellowLens = CreateLens(lightGO.transform, "YellowLens", new Vector3(0f, 0.0f, -0.01f), circleSprite, Color.yellow);
            SpriteRenderer greenLens = CreateLens(lightGO.transform, "GreenLens", new Vector3(0f, -0.65f, -0.01f), circleSprite, Color.green);

            trafficLight = GetOrAdd<Level3TrafficLight>(lightGO);
            trafficLight.SetupLenses(redLens, yellowLens, greenLens);

            // Cross-Traffic AI Vehicles
            // Vehicle 1: Red Sedan driving Left->Right in bottom cross lane (Y = 50.5)
            GameObject crossCar1 = CreateAIVehicle(mission2.transform, "CrossCar1", CarRedTopDownPath, new Vector3(-16f, 50.5f, 0f), Vector2.right, 4.5f);
            // Vehicle 2: Taxi driving Right->Left in top cross lane (Y = 53.5)
            GameObject crossCar2 = CreateAIVehicle(mission2.transform, "CrossCar2", TaxiTopDownPath, new Vector3(16f, 53.5f, 0f), Vector2.left, 4.0f);

            return new[] { crossCar1, crossCar2 };
        }

        private static SpriteRenderer CreateLens(Transform parent, string name, Vector3 pos, Sprite sprite, Color col)
        {
            GameObject lensObj = FindOrCreateChild(parent, name);
            lensObj.transform.localPosition = pos;
            lensObj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(lensObj);
            sr.sprite = sprite;
            sr.color = col;
            sr.sortingOrder = 22;
            return sr;
        }

        private static GameObject CreateAIVehicle(Transform parent, string name, string spritePath, Vector3 startPos, Vector2 dir, float speed)
        {
            GameObject v = FindOrCreateChild(parent, name);
            v.transform.position = startPos;
            v.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            Sprite s = LoadSpriteAsset(spritePath);
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(v);
            sr.sprite = s;
            sr.sortingOrder = 9;

            Rigidbody2D rb = GetOrAdd<Rigidbody2D>(v);
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            BoxCollider2D col = GetOrAdd<BoxCollider2D>(v);
            col.size = new Vector2(1.6f, 3.2f);

            Level3TopDownVehicle vehicle = GetOrAdd<Level3TopDownVehicle>(v);
            vehicle.SetMovement(dir, speed);

            return v;
        }
        #endregion

        #region Mission 3: Emergency Vehicle
        private static Level3TopDownVehicle CreateAmbulance(Transform world)
        {
            GameObject mission3 = FindOrCreateChild(world, "Mission3_Emergency");

            // Hospital Building on right side (X = 8.5, Y = 76)
            GameObject hospital = FindOrCreateChild(mission3.transform, "HospitalBuilding");
            hospital.transform.position = new Vector3(8.5f, 76f, 0f);
            CreateWorldSprite(hospital.transform, "MainBlock", Vector3.zero, new Vector3(5.5f, 4.5f, 1f), new Color(0.95f, 0.96f, 0.98f, 1f), -5);
            CreateWorldSprite(hospital.transform, "Roof", new Vector3(0f, 0.5f, -0.01f), new Vector3(5.7f, 3.2f, 1f), new Color(0.30f, 0.55f, 0.75f, 1f), -4);
            CreateRoadSign(mission3.transform, "HospitalSign", new Vector3(4.2f, 72f, 0f), "HOSPITAL", 0.65f);

            // Ambulance Vehicle
            GameObject ambGO = FindOrCreateChild(mission3.transform, "AmbulanceVehicle");
            ambGO.transform.position = new Vector3(-1.0f, 58f, 0f); // Spawns in left lane
            ambGO.transform.localScale = new Vector3(0.68f, 0.68f, 1f);

            Sprite ambSprite = LoadSpriteAsset(AmbulanceTopDownPath);
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(ambGO);
            sr.sprite = ambSprite;
            sr.sortingOrder = 11;

            Rigidbody2D rb = GetOrAdd<Rigidbody2D>(ambGO);
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            BoxCollider2D col = GetOrAdd<BoxCollider2D>(ambGO);
            col.size = new Vector2(1.6f, 3.2f);

            // Flashing Beacons
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WorldCircleSpritePath);
            SpriteRenderer beaconRed = CreateLens(ambGO.transform, "BeaconRed", new Vector3(-0.35f, 0.7f, -0.02f), circleSprite, Color.red);
            SpriteRenderer beaconBlue = CreateLens(ambGO.transform, "BeaconBlue", new Vector3(0.35f, 0.7f, -0.02f), circleSprite, Color.cyan);

            Level3TopDownVehicle vehicle = GetOrAdd<Level3TopDownVehicle>(ambGO);
            SetInt(vehicle, "category", (int)VehicleCategory.Ambulance);
            vehicle.SetBeacons(beaconRed, beaconBlue);
            vehicle.SetMovement(Vector2.up, 6.5f);

            return vehicle;
        }
        #endregion

        #region Mission 4: Distraction Zone
        private static void CreateDistractionZoneScenery(Transform world)
        {
            GameObject mission4 = FindOrCreateChild(world, "Mission4_Distraction");
            // Avenue trees and signs
            CreateRoadSign(mission4.transform, "NoPhoneSign", new Vector3(4.2f, 90f, 0f), "NO PHONE", 0.65f);
            CreateTree(mission4.transform, new Vector3(-5.5f, 95f, 0f));
            CreateTree(mission4.transform, new Vector3(5.5f, 98f, 0f));
        }
        #endregion

        #region Mission 5: Final Drive & Finish Line
        private static Level3Pedestrian CreateFinalDriveAndFinish(Transform world, Transform player)
        {
            GameObject mission5 = FindOrCreateChild(world, "Mission5_FinalDrive");
            ClearChildren(mission5.transform);

            // Speed Limit 30 Sign
            CreateRoadSign(mission5.transform, "SpeedLimit30Sign", new Vector3(4.2f, 106f, 0f), "SPEED LIMIT", 0.62f);

            // Zebra crossing at Y = 116
            GameObject crossing = FindOrCreateChild(mission5.transform, "FinalCrosswalk");
            crossing.transform.position = new Vector3(0f, 116f, 0f);
            GameObject zebraObj = CreateWorldSprite(crossing.transform, "ZebraStripes", Vector3.zero, new Vector3(1.20f, 1.35f, 1f), Color.white, -7);
            Sprite zebraSprite = LoadSpriteAsset(ZebraCrossingPath);
            if (zebraSprite != null)
            {
                zebraObj.GetComponent<SpriteRenderer>().sprite = zebraSprite;
            }

            // Stop Line across right lane at Y = 113.5
            CreateWorldSprite(mission5.transform, "FinalStopLine", new Vector3(1.5f, 113.5f, 0f), new Vector3(3.0f, 0.35f, 1f), Color.white, -6);

            // Pedestrian NPC (Walking Right to Left)
            GameObject pedGO = FindOrCreateChild(mission5.transform, "FinalPedestrian");
            pedGO.transform.position = new Vector3(4.5f, 116f, 0f);
            GameObject visual = FindOrCreateChild(pedGO.transform, "Visual");
            ClearChildren(visual.transform);

            // Girl pedestrian sprite facing left (-X)
            CreatePersonSprite(visual.transform, "Pedestrian", ChildGirlTopDownPath, Vector3.zero, new Vector3(-0.44f, 0.44f, 1f), 6);

            BoxCollider2D pedCol = GetOrAdd<BoxCollider2D>(pedGO);
            pedCol.size = new Vector2(1.2f, 1.2f);
            pedCol.isTrigger = true;

            Level3Pedestrian ped = GetOrAdd<Level3Pedestrian>(pedGO);
            ped.Configure(new Vector3(4.5f, 116f, 0f), new Vector3(-4.5f, 116f, 0f), player);
            ped.SetVisualRoot(visual.transform);
            SetVector3(ped, "startPosition", new Vector3(4.5f, 116f, 0f));
            SetVector3(ped, "targetPosition", new Vector3(-4.5f, 116f, 0f));
            SetReference(ped, "playerTransform", player);
            SetReference(ped, "visualRoot", visual.transform);
            SetFloat(ped, "walkSpeed", 1.5f);
            SetFloat(ped, "triggerDistance", 10f);
            SetBool(ped, "triggerOnProximity", true);
            EditorUtility.SetDirty(ped);

            // Checkered Finish Line at Y = 128 (Spans X = -3.1 to +3.1)
            Sprite finishSprite = LoadSpriteAsset(CheckeredFinishPath);
            GameObject finishBanner = CreateWorldSprite(mission5.transform, "FinishLineBanner", new Vector3(0f, 128f, 0f), new Vector3(6.4f, 1.4f, 1f), Color.white, -6);
            if (finishSprite != null)
            {
                finishBanner.GetComponent<SpriteRenderer>().sprite = finishSprite;
            }

            // Overhead Gantry Finish Sign
            CreateWorldSprite(mission5.transform, "FinishGantryPillarL", new Vector3(-3.4f, 128f, 0f), new Vector3(0.35f, 2.4f, 1f), new Color(0.2f, 0.2f, 0.25f, 1f), 15);
            CreateWorldSprite(mission5.transform, "FinishGantryPillarR", new Vector3(3.4f, 128f, 0f), new Vector3(0.35f, 2.4f, 1f), new Color(0.2f, 0.2f, 0.25f, 1f), 15);
            CreateWorldSprite(mission5.transform, "FinishGantryTop", new Vector3(0f, 128.8f, 0f), new Vector3(7.2f, 0.8f, 1f), TrafficTownTheme.AccentColor, 16);

            // Finish Area Celebratory Cones / Pylons
            for (float py = 132f; py <= 148f; py += 4f)
            {
                CreateWorldSprite(mission5.transform, $"PylonL_{py}", new Vector3(-3.2f, py, 0f), new Vector3(0.4f, 0.4f, 1f), TrafficTownTheme.WarningColor, 1, true);
                CreateWorldSprite(mission5.transform, $"PylonR_{py}", new Vector3(3.2f, py, 0f), new Vector3(0.4f, 0.4f, 1f), TrafficTownTheme.WarningColor, 1, true);
            }

            // End-of-Road Safety Buffer Barrier at Y = 152 (prevents car from ever rolling into void)
            GameObject barrier = CreateWorldSprite(mission5.transform, "EndBarrier", new Vector3(0f, 152f, 0f), new Vector3(8.0f, 0.8f, 1f), TrafficTownTheme.DangerColor, 10);
            BoxCollider2D barrierCol = GetOrAdd<BoxCollider2D>(barrier);
            barrierCol.size = new Vector2(8.0f, 0.8f);

            return ped;
        }
        #endregion

        #region UI Construction
        private static Level3UIController CreateUI(Level3PlayerCar playerCar)
        {
            Canvas canvas = FindOrCreateCanvas();
            ClearChildren(canvas.transform);

            Sprite roundedPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPanelSpritePath);

            // 1. HUD Root
            GameObject hud = CreateUIPanel(canvas.transform, "HUD", new Color(0f, 0f, 0f, 0f));
            SetFullScreen(hud.GetComponent<RectTransform>());

            // --- Top-Left: Mission Card ---
            GameObject missionCard = CreateUIPanel(hud.transform, "MissionCard", TrafficTownTheme.CardDarkColor, roundedPanelSprite);
            SetRect(missionCard.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(330f, 88f));
            AddShadow(missionCard);

            // Mission Badge Pill
            GameObject badgePill = CreateUIPanel(missionCard.transform, "BadgePill", TrafficTownTheme.PrimaryColor, roundedPanelSprite);
            SetRect(badgePill.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -12f), new Vector2(120f, 24f));
            TMP_Text badgeText = CreateUIText(badgePill.transform, "BadgeText", "MISSION 1 / 5", 12, TextAlignmentOptions.Center, new Vector2(110f, 20f), Vector2.zero, new Vector2(0.5f, 0.5f));
            badgeText.color = Color.white;
            badgeText.fontStyle = FontStyles.Bold;

            TMP_Text titleText = CreateUIText(missionCard.transform, "MissionTitle", "SCHOOL ZONE", 16, TextAlignmentOptions.Left, new Vector2(170f, 24f), new Vector2(144f, -12f), new Vector2(0f, 1f));
            titleText.color = TrafficTownTheme.AccentColor;
            titleText.fontStyle = FontStyles.Bold;

            TMP_Text objectiveText = CreateUIText(missionCard.transform, "ObjectiveText", "Slow down to 20 km/h and stop for crossing pedestrians.", 12, TextAlignmentOptions.TopLeft, new Vector2(300f, 40f), new Vector2(14f, -42f), new Vector2(0f, 1f));
            objectiveText.color = TrafficTownTheme.TextPrimaryColor;

            // --- Top-Right: Score & Safety Card ---
            GameObject scoreCard = CreateUIPanel(hud.transform, "ScoreCard", TrafficTownTheme.CardDarkColor, roundedPanelSprite);
            SetRect(scoreCard.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(240f, 88f));
            AddShadow(scoreCard);

            TMP_Text scoreLabel = CreateUIText(scoreCard.transform, "ScoreLabel", "SCORE", 11, TextAlignmentOptions.Left, new Vector2(80f, 18f), new Vector2(16f, -12f), new Vector2(0f, 1f));
            scoreLabel.color = TrafficTownTheme.TextSecondaryColor;
            TMP_Text scoreVal = CreateUIText(scoreCard.transform, "ScoreValue", "100", 22, TextAlignmentOptions.Left, new Vector2(90f, 28f), new Vector2(16f, -32f), new Vector2(0f, 1f));
            scoreVal.color = Color.white;
            scoreVal.fontStyle = FontStyles.Bold;

            TMP_Text safetyLabel = CreateUIText(scoreCard.transform, "SafetyLabel", "SAFETY", 11, TextAlignmentOptions.Right, new Vector2(80f, 18f), new Vector2(-16f, -12f), new Vector2(1f, 1f));
            safetyLabel.color = TrafficTownTheme.TextSecondaryColor;
            TMP_Text safetyVal = CreateUIText(scoreCard.transform, "SafetyValue", "100%", 18, TextAlignmentOptions.Right, new Vector2(80f, 28f), new Vector2(-16f, -32f), new Vector2(1f, 1f));
            safetyVal.color = TrafficTownTheme.SuccessColor;
            safetyVal.fontStyle = FontStyles.Bold;

            // Safety Bar Trough
            GameObject meterTrough = CreateUIPanel(scoreCard.transform, "MeterTrough", new Color(0.15f, 0.20f, 0.28f, 0.9f), roundedPanelSprite);
            SetRect(meterTrough.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(208f, 14f));

            // Safety Bar Fill
            GameObject meterFillObj = CreateUIPanel(meterTrough.transform, "MeterFill", TrafficTownTheme.SuccessColor, roundedPanelSprite);
            SetRect(meterFillObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image meterFill = meterFillObj.GetComponent<Image>();
            meterFill.type = Image.Type.Filled;
            meterFill.fillMethod = Image.FillMethod.Horizontal;
            meterFill.fillAmount = 1f;

            // --- Bottom-Right: Speedometer ---
            GameObject speedCard = CreateUIPanel(hud.transform, "SpeedometerCard", TrafficTownTheme.CardDarkColor, roundedPanelSprite);
            SetRect(speedCard.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(190f, 84f));
            AddShadow(speedCard);

            TMP_Text speedVal = CreateUIText(speedCard.transform, "CurrentSpeed", "0 <size=14>km/h</size>", 26, TextAlignmentOptions.Center, new Vector2(170f, 36f), new Vector2(0f, 38f), new Vector2(0.5f, 0f));
            speedVal.color = TrafficTownTheme.TextPrimaryColor;
            speedVal.fontStyle = FontStyles.Bold;

            GameObject limitPill = CreateUIPanel(speedCard.transform, "LimitPill", TrafficTownTheme.PrimaryColor, roundedPanelSprite);
            SetRect(limitPill.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(150f, 24f));
            TMP_Text limitVal = CreateUIText(limitPill.transform, "SpeedLimit", "LIMIT: 40", 12, TextAlignmentOptions.Center, new Vector2(140f, 20f), Vector2.zero, new Vector2(0.5f, 0.5f));
            limitVal.color = Color.white;
            limitVal.fontStyle = FontStyles.Bold;

            // --- Bottom-Center: Feedback Banner ---
            GameObject feedbackObj = CreateUIPanel(hud.transform, "FeedbackBanner", TrafficTownTheme.CardDarkColor, roundedPanelSprite);
            SetRect(feedbackObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(560f, 52f));
            AddShadow(feedbackObj);
            CanvasGroup fbGroup = GetOrAdd<CanvasGroup>(feedbackObj);

            TMP_Text fbIcon = CreateUIText(feedbackObj.transform, "FeedbackIcon", "[OK]", 16, TextAlignmentOptions.Center, new Vector2(50f, 36f), new Vector2(12f, 0f), new Vector2(0f, 0.5f));
            fbIcon.color = TrafficTownTheme.SuccessColor;
            fbIcon.fontStyle = FontStyles.Bold;

            TMP_Text fbMsg = CreateUIText(feedbackObj.transform, "FeedbackMessage", "Great job!", 14, TextAlignmentOptions.Left, new Vector2(480f, 36f), new Vector2(65f, 0f), new Vector2(0f, 0.5f));
            fbMsg.color = Color.white;
            fbMsg.fontStyle = FontStyles.Bold;

            // --- Distraction Choice Modal ---
            GameObject modalOverlay = CreateUIPanel(canvas.transform, "DistractionModal", new Color(0f, 0f, 0f, 0.70f));
            SetFullScreen(modalOverlay.GetComponent<RectTransform>());

            GameObject modalCard = CreateUIPanel(modalOverlay.transform, "ModalCard", TrafficTownTheme.CardDarkColor, roundedPanelSprite);
            SetRect(modalCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 260f));
            AddShadow(modalCard);

            TMP_Text modalBadge = CreateUIText(modalCard.transform, "ModalBadge", "IN-CAR NOTIFICATION", 13, TextAlignmentOptions.Center, new Vector2(300f, 24f), new Vector2(0f, -18f), new Vector2(0.5f, 1f));
            modalBadge.color = TrafficTownTheme.AccentColor;
            modalBadge.fontStyle = FontStyles.Bold;

            TMP_Text modalTitle = CreateUIText(modalCard.transform, "ModalTitle", "New text message received!", 20, TextAlignmentOptions.Center, new Vector2(440f, 32f), new Vector2(0f, -48f), new Vector2(0.5f, 1f));
            modalTitle.color = Color.white;
            modalTitle.fontStyle = FontStyles.Bold;

            TMP_Text modalPrompt = CreateUIText(modalCard.transform, "ModalPrompt", "What is the safest choice while driving on the road?", 14, TextAlignmentOptions.Center, new Vector2(440f, 40f), new Vector2(0f, -90f), new Vector2(0.5f, 1f));
            modalPrompt.color = TrafficTownTheme.TextSecondaryColor;

            Button btnIgnore = CreateModalButton(modalCard.transform, "BtnIgnore", "IGNORE MESSAGE\n(+30 Safe Action)", new Vector2(-115f, 40f), TrafficTownTheme.SuccessColor, roundedPanelSprite);
            Button btnOpen = CreateModalButton(modalCard.transform, "BtnOpen", "OPEN MESSAGE\n(-25 Distraction)", new Vector2(115f, 40f), TrafficTownTheme.DangerColor, roundedPanelSprite);

            // --- Level Complete Dialog ---
            GameObject completionOverlay = CreateUIPanel(canvas.transform, "CompletionPanel", new Color(0f, 0f, 0f, 0.85f));
            SetFullScreen(completionOverlay.GetComponent<RectTransform>());
            CanvasGroup compGroup = GetOrAdd<CanvasGroup>(completionOverlay);

            GameObject compCard = CreateUIPanel(completionOverlay.transform, "CompletionCard", TrafficTownTheme.CardLightColor, roundedPanelSprite);
            SetRect(compCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 480f));
            AddShadow(compCard);

            TMP_Text compTitle = CreateUIText(compCard.transform, "Title", "SAFE CITY DRIVER", 15, TextAlignmentOptions.Center, new Vector2(300f, 24f), new Vector2(0f, -22f), new Vector2(0.5f, 1f));
            compTitle.color = TrafficTownTheme.PrimaryColor;
            compTitle.fontStyle = FontStyles.Bold;

            TMP_Text compSub = CreateUIText(compCard.transform, "Subtitle", "LEVEL COMPLETE!", 28, TextAlignmentOptions.Center, new Vector2(400f, 38f), new Vector2(0f, -50f), new Vector2(0.5f, 1f));
            compSub.color = TrafficTownTheme.TextDarkColor;
            compSub.fontStyle = FontStyles.Bold;

            TMP_Text stars = CreateUIText(compCard.transform, "RatingStars", "★ ★ ★", 36, TextAlignmentOptions.Center, new Vector2(300f, 44f), new Vector2(0f, -94f), new Vector2(0.5f, 1f));
            stars.color = TrafficTownTheme.AccentColor;
            stars.fontStyle = FontStyles.Bold;

            // Stat Cards (Final Score, Safety, Safe Actions, Violations)
            TMP_Text finalScore = CreateStatBox(compCard.transform, "FinalScoreBox", "FINAL SCORE", "200", new Vector2(-125f, -170f), TrafficTownTheme.PrimaryColor, roundedPanelSprite);
            TMP_Text safetyScore = CreateStatBox(compCard.transform, "SafetyScoreBox", "SAFETY SCORE", "100%", new Vector2(125f, -170f), TrafficTownTheme.SuccessColor, roundedPanelSprite);
            TMP_Text safeActions = CreateStatBox(compCard.transform, "SafeActionsBox", "SAFE ACTIONS", "4", new Vector2(-125f, -250f), TrafficTownTheme.SecondaryColor, roundedPanelSprite);
            TMP_Text violations = CreateStatBox(compCard.transform, "ViolationsBox", "VIOLATIONS", "0", new Vector2(125f, -250f), TrafficTownTheme.DangerColor, roundedPanelSprite);

            TMP_Text missionsDone = CreateUIText(compCard.transform, "MissionsCompleted", "Missions Completed: 5 / 5", 14, TextAlignmentOptions.Center, new Vector2(300f, 24f), new Vector2(0f, -310f), new Vector2(0.5f, 1f));
            missionsDone.color = TrafficTownTheme.TextDarkColor;

            // Navigation Buttons (RETRY & BACK TO MAIN MENU)
            Button btnRetry = CreateActionButton(compCard.transform, "RetryButton", "RETRY", new Vector2(-125f, 38f), TrafficTownTheme.ButtonPrimaryColor, roundedPanelSprite);
            Button btnMenu = CreateActionButton(compCard.transform, "MenuButton", "MAIN MENU", new Vector2(125f, 38f), TrafficTownTheme.TextDarkColor, roundedPanelSprite);

            // Wire UI Controller
            Level3UIController ui = GetOrAdd<Level3UIController>(hud);
            SetReference(ui, "missionBadgeText", badgeText);
            SetReference(ui, "missionTitleText", titleText);
            SetReference(ui, "missionObjectiveText", objectiveText);

            SetReference(ui, "scoreValueText", scoreVal);
            SetReference(ui, "safetyValueText", safetyVal);
            SetReference(ui, "safetyMeterFill", meterFill);

            SetReference(ui, "currentSpeedText", speedVal);
            SetReference(ui, "speedLimitText", limitVal);
            SetReference(ui, "speedometerBadge", limitPill.GetComponent<Image>());

            SetReference(ui, "feedbackBanner", feedbackObj);
            SetReference(ui, "feedbackGroup", fbGroup);
            SetReference(ui, "feedbackIconText", fbIcon);
            SetReference(ui, "feedbackMessageText", fbMsg);

            SetReference(ui, "distractionModal", modalOverlay);
            SetReference(ui, "ignoreButton", btnIgnore);
            SetReference(ui, "openButton", btnOpen);

            SetReference(ui, "completionPanel", completionOverlay);
            SetReference(ui, "completionGroup", compGroup);
            SetReference(ui, "completionTitleText", compTitle);
            SetReference(ui, "completionSubtitleText", compSub);
            SetReference(ui, "finalScoreText", finalScore);
            SetReference(ui, "safetyScoreText", safetyScore);
            SetReference(ui, "safeActionsText", safeActions);
            SetReference(ui, "violationsText", violations);
            SetReference(ui, "missionsCompletedText", missionsDone);
            SetReference(ui, "starRatingText", stars);
            SetReference(ui, "retryButton", btnRetry);
            SetReference(ui, "backToMenuButton", btnMenu);

            SetReference(ui, "playerCar", playerCar);

            modalOverlay.SetActive(false);
            completionOverlay.SetActive(false);
            feedbackObj.SetActive(false);

            return ui;
        }

        private static Button CreateModalButton(Transform parent, string name, string label, Vector2 pos, Color col, Sprite sprite)
        {
            GameObject btnObj = CreateUIPanel(parent, name, col, sprite);
            SetRect(btnObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), pos, new Vector2(210f, 52f));
            Image img = btnObj.GetComponent<Image>();
            img.raycastTarget = true;

            Button btn = GetOrAdd<Button>(btnObj);
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;

            TMP_Text txt = CreateUIText(btnObj.transform, "Label", label, 13, TextAlignmentOptions.Center, new Vector2(190f, 44f), Vector2.zero, new Vector2(0.5f, 0.5f));
            txt.color = Color.white;
            txt.fontStyle = FontStyles.Bold;

            return btn;
        }

        private static TMP_Text CreateStatBox(Transform parent, string name, string title, string val, Vector2 pos, Color accent, Sprite sprite)
        {
            GameObject box = CreateUIPanel(parent, name, new Color(0.93f, 0.95f, 0.97f, 1f), sprite);
            SetRect(box.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(220f, 64f));

            TMP_Text t = CreateUIText(box.transform, "Title", title, 11, TextAlignmentOptions.Center, new Vector2(200f, 16f), new Vector2(0f, -10f), new Vector2(0.5f, 1f));
            t.color = accent;
            t.fontStyle = FontStyles.Bold;

            TMP_Text v = CreateUIText(box.transform, "Value", val, 24, TextAlignmentOptions.Center, new Vector2(200f, 30f), new Vector2(0f, -30f), new Vector2(0.5f, 1f));
            v.color = TrafficTownTheme.TextDarkColor;
            v.fontStyle = FontStyles.Bold;

            return v;
        }

        private static Button CreateActionButton(Transform parent, string name, string label, Vector2 pos, Color col, Sprite sprite)
        {
            GameObject btnObj = CreateUIPanel(parent, name, col, sprite);
            SetRect(btnObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), pos, new Vector2(220f, 48f));
            Image img = btnObj.GetComponent<Image>();
            img.raycastTarget = true;

            Button btn = GetOrAdd<Button>(btnObj);
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;

            TMP_Text txt = CreateUIText(btnObj.transform, "Label", label, 15, TextAlignmentOptions.Center, new Vector2(200f, 36f), Vector2.zero, new Vector2(0.5f, 0.5f));
            txt.color = Color.white;
            txt.fontStyle = FontStyles.Bold;

            return btn;
        }
        #endregion

        #region Helpers
        private static Sprite LoadSpriteAsset(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets != null)
            {
                foreach (var obj in assets)
                {
                    if (obj is Sprite spr) return spr;
                }
            }
            return null;
        }

        private static Sprite LoadSignSprite(string signNameOrPath)
        {
            string signName = Path.GetFileNameWithoutExtension(signNameOrPath);

            // 1. Try Resources.Load
            Sprite s = Resources.Load<Sprite>($"Signs/{signName}");
            if (s != null) return s;

            // 2. Try AssetDatabase exact path
            string fullPath = $"{SignsFolderPath}/{signName}.png";
            return LoadSpriteAsset(fullPath);
        }

        private static GameObject CreateWorldSprite(Transform parent, string name, Vector3 pos, Vector3 scale, Color col, int sortingOrder, bool isCircle = false)
        {
            GameObject go = FindOrCreateChild(parent, name);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;

            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(go);
            string spritePath = isCircle ? WorldCircleSpritePath : WorldSquareSpritePath;
            sr.sprite = LoadSpriteAsset(spritePath);
            sr.color = col;
            sr.sortingOrder = sortingOrder;

            return go;
        }

        private static GameObject CreatePersonSprite(Transform parent, string name, string spritePath, Vector3 localPos, Vector3 scale, int sortingOrder)
        {
            GameObject go = FindOrCreateChild(parent, name);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            // Ground Shadow
            GameObject shadow = FindOrCreateChild(go.transform, "Shadow");
            shadow.transform.localPosition = new Vector3(0f, -0.1f, 0.02f);
            shadow.transform.localScale = new Vector3(1.1f, 0.5f, 1f);
            SpriteRenderer shadowSR = GetOrAdd<SpriteRenderer>(shadow);
            shadowSR.sprite = LoadSpriteAsset(WorldCircleSpritePath);
            shadowSR.color = new Color(0f, 0f, 0f, 0.28f);
            shadowSR.sortingOrder = sortingOrder - 1;

            // Character Body
            GameObject body = FindOrCreateChild(go.transform, "Sprite");
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = Vector3.one;
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(body);
            sr.sprite = LoadSpriteAsset(spritePath);
            sr.color = Color.white;
            sr.sortingOrder = sortingOrder;

            return go;
        }

        private static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("UI");
                canvas = canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            canvas.name = "UI";
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            return canvas;
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Color color, Sprite sprite = null)
        {
            GameObject panel = FindOrCreateChild(parent, name);
            Image image = GetOrAdd<Image>(panel);
            image.color = color;
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
            return panel;
        }

        private static TMP_Text CreateUIText(Transform parent, string name, string textVal, int fontSize, TextAlignmentOptions align, Vector2 size, Vector2 pos, Vector2 anchor)
        {
            GameObject textObj = FindOrCreateChild(parent, name);
            TextMeshProUGUI tmp = GetOrAdd<TextMeshProUGUI>(textObj);
            tmp.text = textVal;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            SetRect(tmp.rectTransform, anchor, anchor, anchor, pos, size);
            return tmp;
        }

        private static void SetFullScreen(RectTransform rt)
        {
            SetRect(rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }

        private static void AddShadow(GameObject obj)
        {
            Shadow shadow = GetOrAdd<Shadow>(obj);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            shadow.effectDistance = new Vector2(0f, -4f);
        }

        private static GameObject FindOrCreate(string name)
        {
            GameObject go = GameObject.Find(name);
            return go != null ? go : new GameObject(name);
        }

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child.gameObject;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            return comp != null ? comp : go.AddComponent<T>();
        }

        private static void EnsureAssetFolders()
        {
            string[] folders = { "Assets/Scenes", "Assets/JSON", "Assets/Resources", "Assets/Resources/Signs", "Assets/Sprites/Vehicles", "Assets/Sprites/Environment", "Assets/UI" };
            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            }
        }

        private static void ConfigureSpriteImporters()
        {
            string[] paths = {
                CarBlueTopDownPath, CarRedTopDownPath, TaxiTopDownPath, BusTopDownPath, AmbulanceTopDownPath, CheckeredFinishPath,
                ZebraCrossingPath, SignPostBasePath, ChildBoyTopDownPath, ChildGirlTopDownPath, CrossingGuardTopDownPath
            };
            bool modified = false;
            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.alphaIsTransparency = true;
                        importer.spritePixelsPerUnit = 100f;
                        importer.SaveAndReimport();
                        modified = true;
                    }
                }
            }

            if (Directory.Exists(SignsFolderPath))
            {
                foreach (string signFile in Directory.GetFiles(SignsFolderPath, "*.png"))
                {
                    string signAssetPath = signFile.Replace('\\', '/');
                    TextureImporter importer = AssetImporter.GetAtPath(signAssetPath) as TextureImporter;
                    if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.alphaIsTransparency = true;
                        importer.spritePixelsPerUnit = 100f;
                        importer.SaveAndReimport();
                        modified = true;
                    }
                }
            }

            if (modified) AssetDatabase.Refresh();
        }

        private static void SetReference(Object target, string fieldName, Object val)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = val;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetObjectArray(Object target, string fieldName, Object[] vals)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.arraySize = vals.Length;
                for (int i = 0; i < vals.Length; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = vals[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetInt(Object target, string fieldName, int val)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.intValue = val;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetFloat(Object target, string fieldName, float val)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.floatValue = val;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetVector3(Object target, string fieldName, Vector3 val)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.vector3Value = val;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetBool(Object target, string fieldName, bool val)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.boolValue = val;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        #endregion
    }

    /// <summary>
    /// Helper component on road shoulder trigger colliders to notify player car of off-road slowdown.
    /// </summary>
    public class RoadShoulderTrigger : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            Level3PlayerCar car = other.GetComponent<Level3PlayerCar>();
            if (car != null) car.SetOffRoad(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Level3PlayerCar car = other.GetComponent<Level3PlayerCar>();
            if (car != null) car.SetOffRoad(false);
        }
    }
}
#endif
