#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using TrafficTown2D.Core;
using TrafficTown2D.Level3;
using TrafficTown2D.Level5;
using TrafficTown2D.UI;

namespace TrafficTown2D.Editor
{
    public static class Level5Setup
    {
        private const string Level5ScenePath = "Assets/Scenes/Level5.unity";
        private const string WorldSquareSpritePath = "Assets/Sprites/Generated/WorldSquare.png";
        private const string WorldCircleSpritePath = "Assets/Sprites/Generated/WorldCircle.png";
        private const string RoundedPanelSpritePath = "Assets/UI/RoundedPanel.png";

        private const string CarBlueTopDownPath = "Assets/Sprites/Vehicles/CarBlueTopDown.png";
        private const string CarRedTopDownPath = "Assets/Sprites/Vehicles/CarRedTopDown.png";
        private const string TaxiTopDownPath = "Assets/Sprites/Vehicles/TaxiTopDown.png";
        private const string BusTopDownPath = "Assets/Sprites/Vehicles/BusTopDown.png";
        private const string AmbulanceTopDownPath = "Assets/Sprites/Vehicles/AmbulanceTopDown.png";

        private const string ZebraCrossingPath = "Assets/Sprites/Environment/ZebraCrossingStripes.png";
        private const string TrafficLightBodyPath = "Assets/Sprites/TrafficLightBody.png";
        private const string TrafficLightLensPath = "Assets/Sprites/TrafficLightLens.png";

        private const string ChildBoyTopDownPath = "Assets/Sprites/Characters/ChildBoyTopDown.png";
        private const string ChildGirlTopDownPath = "Assets/Sprites/Characters/ChildGirlTopDown.png";
        private const string AdultPedestrianDarkTopDownPath = "Assets/Sprites/Characters/AdultPedestrianDarkTopDown.png";
        private const string CrossingGuardTopDownPath = "Assets/Sprites/Characters/CrossingGuardTopDown.png";

        private const string AutoRunPrefKey = "TrafficTown_Level5_AutoSetup_v9";

        [InitializeOnLoadMethod]
        private static void AutoSetupOnce()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            if (!EditorPrefs.GetBool(AutoRunPrefKey, false))
            {
                EditorPrefs.SetBool(AutoRunPrefKey, true);
                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication.isPlaying)
                    {
                        Debug.Log("[Level5Setup] Auto-running SetupLevel5 on compile...");
                        SetupLevel5();
                    }
                };
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (SceneManager.GetActiveScene().name == "Level5")
                {
                    FocusGameView();
                }
            }
        }

        public static void FocusGameView()
        {
            System.Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType != null)
            {
                EditorWindow gw = EditorWindow.GetWindow(gameViewType);
                if (gw != null) gw.Focus();
            }
        }

        [MenuItem("TrafficTown/Setup Level 5")]
        public static void SetupLevel5()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before running TrafficTown -> Setup Level 5.");
                return;
            }

            EnsureAssetFolders();

            Scene level5Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(level5Scene, Level5ScenePath);

            // 1. Fixed Top-Down Camera & Global Light
            Camera cam = EnsureFixedCamera();
            EnsureGlobalDaylight();

            // 2. Services & EventSystem
            GameObject services = FindOrCreate("Services");
            SceneLoader sceneLoader = GetOrAdd<SceneLoader>(services);
            GameManager gm = GetOrAdd<GameManager>(services);
            SetInt(gm, "startingState", (int)GameState.Playing);
            gm.SetState(GameState.Playing);
            EditorUtility.SetDirty(gm);
            Time.timeScale = 1f;
            EnsureEventSystem();

            // 3. Audio & Siren
            AudioSource sirenAudio = services.AddComponent<AudioSource>();
            sirenAudio.playOnAwake = false;
            sirenAudio.loop = true;
            sirenAudio.volume = 0.5f;

            // 4. World Environment (4-way Intersection, Roads, Sidewalks, Zebra Crossings)
            GameObject world = FindOrCreate("World");
            ClearChildren(world.transform);
            CreateIntersectionEnvironment(world.transform);

            // 5. Traffic Lights (4 Corners)
            Level3TrafficLight northLight, southLight, eastLight, westLight;
            CreateTrafficLights(world.transform, out northLight, out southLight, out eastLight, out westLight);

            // 6. Containers
            GameObject vehicleContainer = FindOrCreateChild(world.transform, "Vehicles");
            GameObject pedestrianContainer = FindOrCreateChild(world.transform, "Pedestrians");

            // 7. Gameplay Controllers
            GameObject controllers = FindOrCreate("GameplayControllers");
            ClearChildren(controllers.transform);

            TrafficPhaseController phaseCtrl = controllers.AddComponent<TrafficPhaseController>();
            phaseCtrl.ConfigureLights(northLight, southLight, eastLight, westLight);

            TrafficQueueManager queueMgr = controllers.AddComponent<TrafficQueueManager>();
            queueMgr.SetVehicleContainer(vehicleContainer.transform);
            queueMgr.SetObstacleLayers(LayerMask.GetMask("Default"));
            queueMgr.SetSprites(
                LoadSpriteAsset(CarBlueTopDownPath),
                LoadSpriteAsset(CarRedTopDownPath),
                LoadSpriteAsset(TaxiTopDownPath),
                LoadSpriteAsset(BusTopDownPath),
                LoadSpriteAsset(AmbulanceTopDownPath)
            );

            PedestrianIntersectionManager pedMgr = controllers.AddComponent<PedestrianIntersectionManager>();
            pedMgr.SetContainer(pedestrianContainer.transform);
            pedMgr.SetSprites(
                LoadSpriteAsset(ChildBoyTopDownPath),
                LoadSpriteAsset(ChildGirlTopDownPath),
                LoadSpriteAsset(AdultPedestrianDarkTopDownPath),
                LoadSpriteAsset(CrossingGuardTopDownPath)
            );

            EmergencyVehicleManager emgMgr = controllers.AddComponent<EmergencyVehicleManager>();
            emgMgr.SetAudioSource(sirenAudio);

            Level5SafetyManager safetyMgr = controllers.AddComponent<Level5SafetyManager>();

            // 8. UI Construction
            Level5UIController uiCtrl = CreateUI();

            // 9. Mission Manager
            Level5MissionManager missionMgr = controllers.AddComponent<Level5MissionManager>();
            SetReference(missionMgr, "queueManager", queueMgr);
            SetReference(missionMgr, "pedestrianManager", pedMgr);
            SetReference(missionMgr, "emergencyManager", emgMgr);
            SetReference(missionMgr, "safetyManager", safetyMgr);
            SetReference(missionMgr, "uiController", uiCtrl);

            // 10. Sync build settings
            BuildSettingsUtility.EnsureAllScenesInBuildSettings();

            EditorSceneManager.MarkSceneDirty(level5Scene);
            EditorSceneManager.SaveScene(level5Scene);
            Debug.Log("[Level5Setup] Successfully built Level 5 — 'Traffic Controller' 🚦");
        }

        #region Camera & Lighting
        private static Camera EnsureFixedCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = 13.5f; // Perfect 4-way 9.6-wide intersection framing
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.35f, 0.65f, 0.35f, 1f); // Lush park grass
            cam.cullingMask = ~0;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            cam.depth = 0;
            cam.targetDisplay = 0;
            cam.enabled = true;

            // Universal Additional Camera Data
            UniversalAdditionalCameraData camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderType = CameraRenderType.Base;
            camData.renderPostProcessing = false;

            return cam;
        }

        private static void EnsureGlobalDaylight()
        {
            Light2D[] lights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include);
            Light2D globalLight = null;
            foreach (var l in lights)
            {
                if (l.lightType == Light2D.LightType.Global)
                {
                    globalLight = l;
                    break;
                }
            }

            if (globalLight == null)
            {
                GameObject lightGO = new GameObject("Global Daylight");
                globalLight = lightGO.AddComponent<Light2D>();
                globalLight.lightType = Light2D.LightType.Global;
            }

            globalLight.color = Color.white;
            globalLight.intensity = 1.0f;
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<EventSystem>();
            }

            if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null &&
                es.GetComponent<StandaloneInputModule>() == null)
            {
                es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }
        #endregion

        #region Environment Construction
        private static void CreateIntersectionEnvironment(Transform world)
        {
            Color roadCol = new Color(0.14f, 0.15f, 0.18f, 1f);
            Color sidewalkCol = new Color(0.72f, 0.76f, 0.74f, 1f);
            Color curbCol = new Color(0.55f, 0.58f, 0.56f, 1f);
            Color yellowLineCol = new Color(1f, 0.92f, 0.25f, 1f);
            Color whiteLineCol = new Color(0.95f, 0.95f, 0.95f, 1f);

            // Ground Grass (covers entire camera view in any aspect ratio)
            GameObject ground = CreateWorldSprite(world, "GroundGrass", Vector3.zero, new Vector3(110f, 65f, 1f), new Color(0.38f, 0.68f, 0.36f, 1f), -12);

            // Roads: North-South & East-West (width 9.6f fits 4 lanes total; length extends past screen edges)
            GameObject nsRoad = CreateWorldSprite(world, "Road_NorthSouth", Vector3.zero, new Vector3(9.6f, 60f, 1f), roadCol, -10);
            GameObject ewRoad = CreateWorldSprite(world, "Road_EastWest", Vector3.zero, new Vector3(90f, 9.6f, 1f), roadCol, -10);

            // Intersection Center Box
            CreateWorldSprite(world, "IntersectionBox", Vector3.zero, new Vector3(9.6f, 9.6f, 1f), roadCol, -9);

            // Double Yellow Center Divider Lines (spans outer roads, leaves intersection & crosswalks clear)
            // North Leg (Y = 7.8 to 27.2)
            CreateWorldSprite(world, "YellowDivider_N", new Vector3(-0.07f, 17.5f, 0f), new Vector3(0.08f, 19.4f, 1f), yellowLineCol, -8);
            CreateWorldSprite(world, "YellowDivider_N2", new Vector3(0.07f, 17.5f, 0f), new Vector3(0.08f, 19.4f, 1f), yellowLineCol, -8);
            // South Leg (Y = -7.8 to -27.2)
            CreateWorldSprite(world, "YellowDivider_S", new Vector3(-0.07f, -17.5f, 0f), new Vector3(0.08f, 19.4f, 1f), yellowLineCol, -8);
            CreateWorldSprite(world, "YellowDivider_S2", new Vector3(0.07f, -17.5f, 0f), new Vector3(0.08f, 19.4f, 1f), yellowLineCol, -8);
            // East Leg (X = 7.8 to 44.0)
            CreateWorldSprite(world, "YellowDivider_E", new Vector3(25.9f, -0.07f, 0f), new Vector3(36.2f, 0.08f, 1f), yellowLineCol, -8);
            CreateWorldSprite(world, "YellowDivider_E2", new Vector3(25.9f, 0.07f, 0f), new Vector3(36.2f, 0.08f, 1f), yellowLineCol, -8);
            // West Leg (X = -7.8 to -44.0)
            CreateWorldSprite(world, "YellowDivider_W", new Vector3(-25.9f, -0.07f, 0f), new Vector3(36.2f, 0.08f, 1f), yellowLineCol, -8);
            CreateWorldSprite(world, "YellowDivider_W2", new Vector3(-25.9f, 0.07f, 0f), new Vector3(36.2f, 0.08f, 1f), yellowLineCol, -8);

            // Dashed White Lane Dividers (at ±2.4f separating inner and outer lanes on each approach)
            for (float y = 8.5f; y <= 26.5f; y += 1.5f)
            {
                CreateWorldSprite(world, $"LaneDash_N_L_{y:0.0}", new Vector3(-2.4f, y, 0f), new Vector3(0.08f, 0.7f, 1f), whiteLineCol, -8);
                CreateWorldSprite(world, $"LaneDash_N_R_{y:0.0}", new Vector3(2.4f, y, 0f), new Vector3(0.08f, 0.7f, 1f), whiteLineCol, -8);
            }
            for (float y = -8.5f; y >= -26.5f; y -= 1.5f)
            {
                CreateWorldSprite(world, $"LaneDash_S_L_{y:0.0}", new Vector3(-2.4f, y, 0f), new Vector3(0.08f, 0.7f, 1f), whiteLineCol, -8);
                CreateWorldSprite(world, $"LaneDash_S_R_{y:0.0}", new Vector3(2.4f, y, 0f), new Vector3(0.08f, 0.7f, 1f), whiteLineCol, -8);
            }
            for (float x = 8.5f; x <= 43.5f; x += 1.5f)
            {
                CreateWorldSprite(world, $"LaneDash_E_B_{x:0.0}", new Vector3(x, -2.4f, 0f), new Vector3(0.7f, 0.08f, 1f), whiteLineCol, -8);
                CreateWorldSprite(world, $"LaneDash_E_T_{x:0.0}", new Vector3(x, 2.4f, 0f), new Vector3(0.7f, 0.08f, 1f), whiteLineCol, -8);
            }
            for (float x = -8.5f; x >= -43.5f; x -= 1.5f)
            {
                CreateWorldSprite(world, $"LaneDash_W_B_{x:0.0}", new Vector3(x, -2.4f, 0f), new Vector3(0.7f, 0.08f, 1f), whiteLineCol, -8);
                CreateWorldSprite(world, $"LaneDash_W_T_{x:0.0}", new Vector3(x, 2.4f, 0f), new Vector3(0.7f, 0.08f, 1f), whiteLineCol, -8);
            }

            // Stop Lines (at ±7.6f across incoming lanes, width 4.8f, thickness 0.28f)
            CreateWorldSprite(world, "StopLine_North", new Vector3(-2.4f, 7.6f, 0f), new Vector3(4.8f, 0.28f, 1f), whiteLineCol, -7);
            CreateWorldSprite(world, "StopLine_South", new Vector3(2.4f, -7.6f, 0f), new Vector3(4.8f, 0.28f, 1f), whiteLineCol, -7);
            CreateWorldSprite(world, "StopLine_East", new Vector3(7.6f, 2.4f, 0f), new Vector3(0.28f, 4.8f, 1f), whiteLineCol, -7);
            CreateWorldSprite(world, "StopLine_West", new Vector3(-7.6f, -2.4f, 0f), new Vector3(0.28f, 4.8f, 1f), whiteLineCol, -7);

            // Zebra Crosswalks (at ±6.2f, 11 crisp stripes spanning 9.6-wide road)
            CreateZebraCrossingGroup(world, "Crosswalk_North", new Vector3(0f, 6.2f, 0f), isHorizontalRoad: false);
            CreateZebraCrossingGroup(world, "Crosswalk_South", new Vector3(0f, -6.2f, 0f), isHorizontalRoad: false);
            CreateZebraCrossingGroup(world, "Crosswalk_East", new Vector3(6.2f, 0f, 0f), isHorizontalRoad: true);
            CreateZebraCrossingGroup(world, "Crosswalk_West", new Vector3(-6.2f, 0f, 0f), isHorizontalRoad: true);

            // Sidewalk Corners (4 quadrants, corner road edge is at ±4.8f; covers wide aspect ratios)
            CreateWorldSprite(world, "Sidewalk_NW", new Vector3(-24.8f, 15.8f, 0f), new Vector3(40f, 22f, 1f), sidewalkCol, -11);
            CreateWorldSprite(world, "Sidewalk_NE", new Vector3(24.8f, 15.8f, 0f), new Vector3(40f, 22f, 1f), sidewalkCol, -11);
            CreateWorldSprite(world, "Sidewalk_SW", new Vector3(-24.8f, -15.8f, 0f), new Vector3(40f, 22f, 1f), sidewalkCol, -11);
            CreateWorldSprite(world, "Sidewalk_SE", new Vector3(24.8f, -15.8f, 0f), new Vector3(40f, 22f, 1f), sidewalkCol, -11);

            // Curbs (darker borders along sidewalk boundaries at ±4.95f)
            CreateWorldSprite(world, "Curb_NW_H", new Vector3(-24.8f, 4.95f, 0f), new Vector3(40f, 0.3f, 1f), curbCol, -10);
            CreateWorldSprite(world, "Curb_NW_V", new Vector3(-4.95f, 15.8f, 0f), new Vector3(0.3f, 22f, 1f), curbCol, -10);

            CreateWorldSprite(world, "Curb_NE_H", new Vector3(24.8f, 4.95f, 0f), new Vector3(40f, 0.3f, 1f), curbCol, -10);
            CreateWorldSprite(world, "Curb_NE_V", new Vector3(4.95f, 15.8f, 0f), new Vector3(0.3f, 22f, 1f), curbCol, -10);

            CreateWorldSprite(world, "Curb_SW_H", new Vector3(-24.8f, -4.95f, 0f), new Vector3(40f, 0.3f, 1f), curbCol, -10);
            CreateWorldSprite(world, "Curb_SW_V", new Vector3(-4.95f, -15.8f, 0f), new Vector3(0.3f, 22f, 1f), curbCol, -10);

            CreateWorldSprite(world, "Curb_SE_H", new Vector3(24.8f, -4.95f, 0f), new Vector3(40f, 0.3f, 1f), curbCol, -10);
            CreateWorldSprite(world, "Curb_SE_V", new Vector3(4.95f, -15.8f, 0f), new Vector3(0.3f, 22f, 1f), curbCol, -10);

            // Central Safety Island Podium (at 0,0 - provides 1.5 - 2 vehicle widths clearance from inner lanes at ±1.6)
            CreateWorldSprite(world, "Podium_Outer", Vector3.zero, new Vector3(2.7f, 2.7f, 1f), curbCol, -8, true);
            CreateWorldSprite(world, "Podium_YellowRing", Vector3.zero, new Vector3(2.4f, 2.4f, 1f), yellowLineCol, -7, true);
            CreateWorldSprite(world, "Podium_InnerPad", Vector3.zero, new Vector3(2.1f, 2.1f, 1f), new Color(0.20f, 0.22f, 0.26f, 1f), -6, true);

            // Central Police Inspector (stands safely in center island with smooth rotation & animation)
            CreatePoliceInspector(world);

            // City Trees & Decorative Props in Corners
            CreateDecorativeTree(world, "Tree_NW_1", new Vector3(-9.5f, 9.5f, 0f), 1.6f);
            CreateDecorativeTree(world, "Tree_NW_2", new Vector3(-13.0f, 8.5f, 0f), 1.4f);
            CreateDecorativeTree(world, "Tree_NE_1", new Vector3(9.5f, 9.5f, 0f), 1.6f);
            CreateDecorativeTree(world, "Tree_NE_2", new Vector3(13.0f, 8.5f, 0f), 1.4f);
            CreateDecorativeTree(world, "Tree_SW_1", new Vector3(-9.5f, -9.5f, 0f), 1.6f);
            CreateDecorativeTree(world, "Tree_SW_2", new Vector3(-13.0f, -8.5f, 0f), 1.4f);
            CreateDecorativeTree(world, "Tree_SE_1", new Vector3(9.5f, -9.5f, 0f), 1.6f);
            CreateDecorativeTree(world, "Tree_SE_2", new Vector3(13.0f, -8.5f, 0f), 1.4f);
        }

        private static void CreatePoliceInspector(Transform world)
        {
            GameObject inspectorObj = FindOrCreateChild(world, "TrafficInspector");
            inspectorObj.transform.position = Vector3.zero;
            ClearChildren(inspectorObj.transform);

            GameObject visual = FindOrCreateChild(inspectorObj.transform, "Visual");
            visual.transform.localPosition = Vector3.zero;

            // Inspector shadow
            CreateWorldSprite(visual.transform, "Shadow", new Vector3(0.08f, -0.08f, 0f), new Vector3(1.3f, 0.8f, 1f), new Color(0f, 0f, 0f, 0.35f), 4, true);

            // Inspector sprite
            GameObject body = FindOrCreateChild(visual.transform, "Body");
            SpriteRenderer bsr = GetOrAdd<SpriteRenderer>(body);
            bsr.sprite = LoadSpriteAsset(CrossingGuardTopDownPath);
            bsr.sortingOrder = 5;
            body.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            CircleCollider2D col = GetOrAdd<CircleCollider2D>(inspectorObj);
            col.isTrigger = true;
            col.radius = 1.2f;

            TrafficInspectorController ctrl = GetOrAdd<TrafficInspectorController>(inspectorObj);
            ctrl.SetVisualRoot(visual.transform);
        }

        private static void CreateZebraCrossingGroup(Transform parent, string name, Vector3 centerPos, bool isHorizontalRoad)
        {
            GameObject group = FindOrCreateChild(parent, name);
            group.transform.position = centerPos;
            ClearChildren(group.transform);

            Color stripeCol = new Color(0.95f, 0.95f, 0.95f, 0.92f);
            float[] offsets = { -4.0f, -3.2f, -2.4f, -1.6f, -0.8f, 0f, 0.8f, 1.6f, 2.4f, 3.2f, 4.0f };

            if (isHorizontalRoad)
            {
                // East or West crosswalk across horizontal road:
                // Stripes run East-West (parallel to vehicular traffic flow)
                for (int i = 0; i < offsets.Length; i++)
                {
                    CreateWorldSprite(group.transform, $"Stripe_{i}", new Vector3(0f, offsets[i], 0f), new Vector3(1.6f, 0.42f, 1f), stripeCol, -7);
                }
            }
            else
            {
                // North or South crosswalk across vertical road:
                // Stripes run North-South (parallel to vehicular traffic flow)
                for (int i = 0; i < offsets.Length; i++)
                {
                    CreateWorldSprite(group.transform, $"Stripe_{i}", new Vector3(offsets[i], 0f, 0f), new Vector3(0.42f, 1.6f, 1f), stripeCol, -7);
                }
            }
        }

        private static void CreateDecorativeTree(Transform parent, string name, Vector3 pos, float scale)
        {
            GameObject tree = FindOrCreateChild(parent, name);
            tree.transform.position = pos;
            tree.transform.localScale = new Vector3(scale, scale, 1f);

            // Shadow
            CreateWorldSprite(tree.transform, "Shadow", new Vector3(0.1f, -0.1f, 0f), Vector3.one * 1.05f, new Color(0.1f, 0.2f, 0.1f, 0.35f), -6, true);
            // Foliage outer
            CreateWorldSprite(tree.transform, "FoliageOuter", Vector3.zero, Vector3.one, new Color(0.22f, 0.52f, 0.25f, 1f), -5, true);
            // Foliage inner
            CreateWorldSprite(tree.transform, "FoliageInner", new Vector3(-0.05f, 0.05f, 0f), Vector3.one * 0.72f, new Color(0.32f, 0.65f, 0.35f, 1f), -4, true);
        }
        #endregion

        #region Traffic Signals
        private static void CreateTrafficLights(Transform world, 
            out Level3TrafficLight northLight, 
            out Level3TrafficLight southLight, 
            out Level3TrafficLight eastLight, 
            out Level3TrafficLight westLight)
        {
            GameObject signalGroup = FindOrCreateChild(world, "TrafficSignals");

            // Signal for Southbound traffic (facing North, placed at NW corner stop line)
            southLight = BuildSignalPole(signalGroup.transform, "Signal_Southbound", new Vector3(-5.5f, 7.6f, 0f), 0f);

            // Signal for Northbound traffic (facing South, placed at SE corner stop line)
            northLight = BuildSignalPole(signalGroup.transform, "Signal_Northbound", new Vector3(5.5f, -7.6f, 0f), 180f);

            // Signal for Westbound traffic (facing East, placed at NE corner stop line)
            westLight = BuildSignalPole(signalGroup.transform, "Signal_Westbound", new Vector3(7.6f, 5.5f, 0f), -90f);

            // Signal for Eastbound traffic (facing West, placed at SW corner stop line)
            eastLight = BuildSignalPole(signalGroup.transform, "Signal_Eastbound", new Vector3(-7.6f, -5.5f, 0f), 90f);
        }

        private static Level3TrafficLight BuildSignalPole(Transform parent, string name, Vector3 pos, float rotationZ)
        {
            GameObject pole = FindOrCreateChild(parent, name);
            pole.transform.position = pos;
            pole.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
            pole.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            Sprite bodySprite = LoadSpriteAsset(TrafficLightBodyPath);
            Sprite lensSprite = LoadSpriteAsset(TrafficLightLensPath);

            // Base housing
            GameObject body = FindOrCreateChild(pole.transform, "Housing");
            SpriteRenderer bsr = GetOrAdd<SpriteRenderer>(body);
            bsr.sprite = bodySprite;
            bsr.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            bsr.sortingOrder = 10;
            body.transform.localScale = new Vector3(0.85f, 1.9f, 1f);

            // Red lens
            GameObject red = FindOrCreateChild(pole.transform, "RedLens");
            red.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            SpriteRenderer rsr = GetOrAdd<SpriteRenderer>(red);
            rsr.sprite = lensSprite;
            rsr.color = new Color(1f, 0.15f, 0.15f, 1f);
            rsr.sortingOrder = 11;
            red.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

            // Yellow lens
            GameObject yellow = FindOrCreateChild(pole.transform, "YellowLens");
            yellow.transform.localPosition = new Vector3(0f, 0f, 0f);
            SpriteRenderer ysr = GetOrAdd<SpriteRenderer>(yellow);
            ysr.sprite = lensSprite;
            ysr.color = new Color(0.3f, 0.25f, 0.05f, 0.5f);
            ysr.sortingOrder = 11;
            yellow.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

            // Green lens
            GameObject green = FindOrCreateChild(pole.transform, "GreenLens");
            green.transform.localPosition = new Vector3(0f, -0.65f, 0f);
            SpriteRenderer gsr = GetOrAdd<SpriteRenderer>(green);
            gsr.sprite = lensSprite;
            gsr.color = new Color(0.05f, 0.3f, 0.1f, 0.5f);
            gsr.sortingOrder = 11;
            green.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

            Level3TrafficLight tl = GetOrAdd<Level3TrafficLight>(pole);
            SetReference(tl, "redLens", rsr);
            SetReference(tl, "yellowLens", ysr);
            SetReference(tl, "greenLens", gsr);

            return tl;
        }
        #endregion

        #region UI Construction
        private static Level5UIController CreateUI()
        {
            Canvas canvas = FindOrCreateCanvas();
            ClearChildren(canvas.transform);

            Sprite roundedPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPanelSpritePath);

            // 1. HUD Root
            GameObject hud = CreateUIPanel(canvas.transform, "HUD", new Color(0f, 0f, 0f, 0f));
            SetFullScreen(hud.GetComponent<RectTransform>());

            // --- Top-Left: Mission Card ---
            GameObject missionCard = CreateUIPanel(hud.transform, "MissionCard", TrafficTownTheme.CardDarkColor, roundedPanelSprite);
            SetRect(missionCard.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(35f, -30f), new Vector2(460f, 160f));
            AddShadow(missionCard);

            GameObject badgePill = CreateUIPanel(missionCard.transform, "BadgePill", TrafficTownTheme.PrimaryColor, roundedPanelSprite);
            SetRect(badgePill.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -16f), new Vector2(140f, 28f));
            TMP_Text badgeText = CreateUIText(badgePill.transform, "BadgeText", "MISSION 1 OF 5", 12, TextAlignmentOptions.Center, new Vector2(130f, 24f), Vector2.zero, new Vector2(0.5f, 0.5f));
            badgeText.color = Color.white;
            badgeText.fontStyle = FontStyles.Bold;

            TMP_Text timerText = CreateUIText(missionCard.transform, "TimerText", "Time: 50s", 14, TextAlignmentOptions.Right, new Vector2(120f, 24f), new Vector2(-18f, -18f), new Vector2(1f, 1f));
            timerText.color = TrafficTownTheme.AccentColor;
            timerText.fontStyle = FontStyles.Bold;

            TMP_Text titleText = CreateUIText(missionCard.transform, "MissionTitle", "LEARN THE INTERSECTION", 15, TextAlignmentOptions.Left, new Vector2(424f, 26f), new Vector2(18f, -54f), new Vector2(0f, 1f));
            titleText.color = TrafficTownTheme.AccentColor;
            titleText.fontStyle = FontStyles.Bold;

            TMP_Text objectiveText = CreateUIText(missionCard.transform, "ObjectiveText", "Operate the traffic signals. Maintain safe transitions and keep traffic flowing smoothly.", 13, TextAlignmentOptions.TopLeft, new Vector2(424f, 60f), new Vector2(18f, -86f), new Vector2(0f, 1f));
            objectiveText.color = TrafficTownTheme.TextPrimaryColor;

            // --- Top-Right: Score & Metrics Card ---
            GameObject scoreCard = CreateUIPanel(hud.transform, "ScoreCard", TrafficTownTheme.CardDarkColor, roundedPanelSprite);
            SetRect(scoreCard.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-35f, -30f), new Vector2(400f, 160f));
            AddShadow(scoreCard);

            TMP_Text scoreLabel = CreateUIText(scoreCard.transform, "ScoreLabel", "SCORE", 12, TextAlignmentOptions.Left, new Vector2(100f, 20f), new Vector2(20f, -18f), new Vector2(0f, 1f));
            scoreLabel.color = TrafficTownTheme.TextSecondaryColor;
            scoreLabel.fontStyle = FontStyles.Bold;

            TMP_Text scoreVal = CreateUIText(scoreCard.transform, "ScoreValue", "0", 28, TextAlignmentOptions.Left, new Vector2(120f, 36f), new Vector2(20f, -44f), new Vector2(0f, 1f));
            scoreVal.color = Color.white;
            scoreVal.fontStyle = FontStyles.Bold;

            // Safety Meter
            TMP_Text safetyLabel = CreateUIText(scoreCard.transform, "SafetyLabel", "SAFETY", 11, TextAlignmentOptions.Left, new Vector2(100f, 20f), new Vector2(160f, -18f), new Vector2(0f, 1f));
            safetyLabel.color = TrafficTownTheme.TextSecondaryColor;
            safetyLabel.fontStyle = FontStyles.Bold;

            TMP_Text safetyVal = CreateUIText(scoreCard.transform, "SafetyVal", "100%", 13, TextAlignmentOptions.Right, new Vector2(60f, 20f), new Vector2(-20f, -18f), new Vector2(1f, 1f));
            safetyVal.color = TrafficTownTheme.SuccessColor;
            safetyVal.fontStyle = FontStyles.Bold;

            GameObject safetyTrough = CreateUIPanel(scoreCard.transform, "SafetyTrough", new Color(0.15f, 0.20f, 0.28f, 0.9f), roundedPanelSprite);
            SetRect(safetyTrough.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160f, -42f), new Vector2(220f, 12f));
            GameObject safetyFillObj = CreateUIPanel(safetyTrough.transform, "Fill", TrafficTownTheme.SuccessColor, roundedPanelSprite);
            SetRect(safetyFillObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image safetyFill = safetyFillObj.GetComponent<Image>();
            safetyFill.type = Image.Type.Filled;
            safetyFill.fillMethod = Image.FillMethod.Horizontal;
            safetyFill.fillAmount = 1f;

            // Traffic Flow Meter
            TMP_Text flowLabel = CreateUIText(scoreCard.transform, "FlowLabel", "TRAFFIC FLOW", 11, TextAlignmentOptions.Left, new Vector2(120f, 20f), new Vector2(160f, -78f), new Vector2(0f, 1f));
            flowLabel.color = TrafficTownTheme.TextSecondaryColor;
            flowLabel.fontStyle = FontStyles.Bold;

            TMP_Text flowVal = CreateUIText(scoreCard.transform, "FlowVal", "100%", 13, TextAlignmentOptions.Right, new Vector2(60f, 20f), new Vector2(-20f, -78f), new Vector2(1f, 1f));
            flowVal.color = new Color(0.25f, 0.75f, 1f, 1f);
            flowVal.fontStyle = FontStyles.Bold;

            GameObject flowTrough = CreateUIPanel(scoreCard.transform, "FlowTrough", new Color(0.15f, 0.20f, 0.28f, 0.9f), roundedPanelSprite);
            SetRect(flowTrough.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160f, -102f), new Vector2(220f, 12f));
            GameObject flowFillObj = CreateUIPanel(flowTrough.transform, "Fill", new Color(0.25f, 0.75f, 1f, 1f), roundedPanelSprite);
            SetRect(flowFillObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image flowFill = flowFillObj.GetComponent<Image>();
            flowFill.type = Image.Type.Filled;
            flowFill.fillMethod = Image.FillMethod.Horizontal;
            flowFill.fillAmount = 1f;

            // --- Top-Center: Feedback Toast Banner ---
            GameObject toast = CreateUIPanel(hud.transform, "FeedbackToast", new Color(0.08f, 0.12f, 0.18f, 0.95f), roundedPanelSprite);
            SetRect(toast.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(560f, 48f));
            CanvasGroup toastCg = toast.AddComponent<CanvasGroup>();
            TMP_Text toastMsg = CreateUIText(toast.transform, "Message", "✓ Safe phase transition completed.", 14, TextAlignmentOptions.Center, new Vector2(540f, 38f), Vector2.zero, new Vector2(0.5f, 0.5f));
            toastMsg.color = Color.white;
            toastMsg.fontStyle = FontStyles.Bold;
            toast.SetActive(false);

            // --- Bottom-Right: Control Panel ---
            GameObject ctrlPanel = CreateUIPanel(hud.transform, "TrafficControlPanel", TrafficTownTheme.CardDarkColor, roundedPanelSprite);
            SetRect(ctrlPanel.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-35f, 30f), new Vector2(400f, 410f));
            AddShadow(ctrlPanel);

            TMP_Text panelTitle = CreateUIText(ctrlPanel.transform, "Title", "TRAFFIC CONTROL", 16, TextAlignmentOptions.Left, new Vector2(220f, 26f), new Vector2(20f, -16f), new Vector2(0f, 1f));
            panelTitle.color = TrafficTownTheme.AccentColor;
            panelTitle.fontStyle = FontStyles.Bold;

            TMP_Text timerLbl = CreateUIText(ctrlPanel.transform, "PhaseTimer", "ACTIVE (18s)", 13, TextAlignmentOptions.Right, new Vector2(140f, 24f), new Vector2(-20f, -16f), new Vector2(1f, 1f));
            timerLbl.color = Color.white;
            timerLbl.fontStyle = FontStyles.Bold;

            TMP_Text nsStatus = CreateUIText(ctrlPanel.transform, "NSStatus", "NORTH / SOUTH:   [GREEN]", 14, TextAlignmentOptions.Left, new Vector2(360f, 24f), new Vector2(20f, -50f), new Vector2(0f, 1f));
            nsStatus.color = new Color(0.35f, 0.95f, 0.45f, 1f);

            TMP_Text ewStatus = CreateUIText(ctrlPanel.transform, "EWStatus", "EAST / WEST:          [RED]", 14, TextAlignmentOptions.Left, new Vector2(360f, 24f), new Vector2(20f, -76f), new Vector2(0f, 1f));
            ewStatus.color = new Color(1f, 0.35f, 0.35f, 1f);

            TMP_Text pedStatus = CreateUIText(ctrlPanel.transform, "PedStatus", "PEDESTRIANS:       [DON'T WALK]", 14, TextAlignmentOptions.Left, new Vector2(360f, 24f), new Vector2(20f, -102f), new Vector2(0f, 1f));
            pedStatus.color = new Color(1f, 0.35f, 0.35f, 1f);

            TMP_Text emgStatus = CreateUIText(ctrlPanel.transform, "EmgStatus", "EMERGENCY: None", 14, TextAlignmentOptions.Left, new Vector2(360f, 24f), new Vector2(20f, -128f), new Vector2(0f, 1f));
            emgStatus.color = new Color(0.6f, 0.85f, 0.7f, 1f);

            TMP_Text vehQueueText = CreateUIText(ctrlPanel.transform, "VehiclesWaiting", "Cars Waiting: 0", 12, TextAlignmentOptions.Left, new Vector2(175f, 22f), new Vector2(20f, -158f), new Vector2(0f, 1f));
            vehQueueText.color = TrafficTownTheme.TextSecondaryColor;

            TMP_Text pedQueueText = CreateUIText(ctrlPanel.transform, "PedestriansWaiting", "Peds Waiting: 0", 12, TextAlignmentOptions.Left, new Vector2(175f, 22f), new Vector2(205f, -158f), new Vector2(0f, 1f));
            pedQueueText.color = TrafficTownTheme.TextSecondaryColor;

            Button changePhaseBtn = CreateButton(ctrlPanel.transform, "ChangePhaseBtn", "CHANGE VEHICLE PHASE [Space]", new Vector2(0f, 78f), new Vector2(360f, 48f), TrafficTownTheme.ButtonPrimaryColor, roundedPanelSprite);
            Button pedWalkBtn = CreateButton(ctrlPanel.transform, "PedWalkBtn", "PEDESTRIAN CROSSING [P]", new Vector2(0f, 20f), new Vector2(360f, 48f), new Color(0.18f, 0.65f, 0.38f, 1f), roundedPanelSprite);

            // --- Pause Modal ---
            GameObject pauseObj = CreateUIPanel(canvas.transform, "PausePanel", new Color(0.05f, 0.08f, 0.12f, 0.94f), roundedPanelSprite);
            SetRect(pauseObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 300f));
            AddShadow(pauseObj);

            TMP_Text pTitle = CreateUIText(pauseObj.transform, "PauseTitle", "GAME PAUSED", 24, TextAlignmentOptions.Center, new Vector2(300f, 40f), new Vector2(0f, -20f), new Vector2(0.5f, 1f));
            pTitle.color = Color.white;
            pTitle.fontStyle = FontStyles.Bold;

            Button resumeBtn = CreateButton(pauseObj.transform, "ResumeBtn", "RESUME", new Vector2(0f, 140f), new Vector2(280f, 44f), TrafficTownTheme.ButtonPrimaryColor, roundedPanelSprite);
            Button restartBtn = CreateButton(pauseObj.transform, "RestartBtn", "RESTART LEVEL", new Vector2(0f, 85f), new Vector2(280f, 44f), new Color(0.85f, 0.55f, 0.15f, 1f), roundedPanelSprite);
            Button pauseMenuBtn = CreateButton(pauseObj.transform, "MenuBtn", "BACK TO MAIN MENU", new Vector2(0f, 30f), new Vector2(280f, 44f), new Color(0.75f, 0.25f, 0.25f, 1f), roundedPanelSprite);
            pauseObj.SetActive(false);

            // --- Grand Finale Master Certificate Screen ---
            GameObject completeObj = CreateUIPanel(canvas.transform, "GrandFinaleCompletionPanel", new Color(0.06f, 0.09f, 0.14f, 0.98f), roundedPanelSprite);
            SetFullScreen(completeObj.GetComponent<RectTransform>());
            CanvasGroup compCg = completeObj.AddComponent<CanvasGroup>();

            GameObject certCard = CreateUIPanel(completeObj.transform, "CertificateCard", new Color(0.09f, 0.13f, 0.20f, 1f), roundedPanelSprite);
            SetRect(certCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 580f));
            AddShadow(certCard);

            // Gold border
            Outline outline = certCard.AddComponent<Outline>();
            outline.effectColor = new Color(0.98f, 0.80f, 0.22f, 0.8f);
            outline.effectDistance = new Vector2(3f, 3f);

            TMP_Text compTitle = CreateUIText(certCard.transform, "Title", "🏆 TRAFFIC SAFETY MASTER 🏆", 28, TextAlignmentOptions.Center, new Vector2(700f, 44f), new Vector2(0f, -24f), new Vector2(0.5f, 1f));
            compTitle.color = TrafficTownTheme.AccentColor;
            compTitle.fontStyle = FontStyles.Bold;

            TMP_Text compSub = CreateUIText(certCard.transform, "Subtitle", "CONGRATULATIONS!\nYou have completed all 5 levels of TrafficTown 2D!", 15, TextAlignmentOptions.Center, new Vector2(680f, 44f), new Vector2(0f, -70f), new Vector2(0.5f, 1f));
            compSub.color = Color.white;

            // Levels Checklist Column (Left)
            GameObject checkCol = CreateUIPanel(certCard.transform, "ChecklistCol", new Color(0.12f, 0.17f, 0.26f, 0.8f), roundedPanelSprite);
            SetRect(checkCol.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 25f), new Vector2(330f, 260f));

            TMP_Text checkHeader = CreateUIText(checkCol.transform, "Header", "LEVELS COMPLETED", 13, TextAlignmentOptions.Left, new Vector2(300f, 24f), new Vector2(16f, -12f), new Vector2(0f, 1f));
            checkHeader.color = TrafficTownTheme.AccentColor;
            checkHeader.fontStyle = FontStyles.Bold;

            string[] levels = new string[]
            {
                "✓ Level 1: Pedestrian Basics",
                "✓ Level 2: Smart Crossing",
                "✓ Level 3: Safe Driving",
                "✓ Level 4: Night Driving",
                "✓ Level 5: Traffic Controller"
            };

            for (int i = 0; i < levels.Length; i++)
            {
                TMP_Text item = CreateUIText(checkCol.transform, $"Level_{i + 1}", levels[i], 12, TextAlignmentOptions.Left, new Vector2(300f, 22f), new Vector2(16f, -44f - i * 36f), new Vector2(0f, 1f));
                item.color = new Color(0.35f, 0.90f, 0.45f, 1f);
                item.fontStyle = FontStyles.Bold;
            }

            // Stats Column (Right)
            GameObject statsCol = CreateUIPanel(certCard.transform, "StatsCol", new Color(0.12f, 0.17f, 0.26f, 0.8f), roundedPanelSprite);
            SetRect(statsCol.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, 25f), new Vector2(330f, 280f));

            TMP_Text statsHeader = CreateUIText(statsCol.transform, "Header", "FINAL PERFORMANCE", 13, TextAlignmentOptions.Left, new Vector2(300f, 24f), new Vector2(16f, -12f), new Vector2(0f, 1f));
            statsHeader.color = TrafficTownTheme.AccentColor;
            statsHeader.fontStyle = FontStyles.Bold;

            TMP_Text scoreStatVal = CreateStatRow(statsCol.transform, "ScoreRow", "Total Score", "2,450", -40f);
            TMP_Text safetyStatVal = CreateStatRow(statsCol.transform, "SafetyRow", "Safety Score", "95%", -76f);
            TMP_Text flowStatVal = CreateStatRow(statsCol.transform, "FlowRow", "Traffic Management", "88%", -112f);
            TMP_Text pedStatVal = CreateStatRow(statsCol.transform, "PedRow", "Pedestrians Safely Crossed", "18", -148f);
            TMP_Text emgStatVal = CreateStatRow(statsCol.transform, "EmgRow", "Emergencies Cleared", "4", -184f);
            TMP_Text collisionStatVal = CreateStatRow(statsCol.transform, "CollisionRow", "Traffic Collisions", "0", -220f);

            // Action Buttons
            Button playAgainBtn = CreateButton(certCard.transform, "PlayAgainBtn", "PLAY LEVEL 5 AGAIN", new Vector2(-155f, 40f), new Vector2(280f, 50f), TrafficTownTheme.ButtonPrimaryColor, roundedPanelSprite);
            Button menuBtn = CreateButton(certCard.transform, "MenuBtn", "BACK TO MAIN MENU", new Vector2(155f, 40f), new Vector2(280f, 50f), TrafficTownTheme.ButtonSuccessColor, roundedPanelSprite);

            completeObj.SetActive(false);

            // Wire up UI controller
            Level5UIController controller = canvas.gameObject.AddComponent<Level5UIController>();
            SetReference(controller, "missionBadgeText", badgeText);
            SetReference(controller, "missionTitleText", titleText);
            SetReference(controller, "missionObjectiveText", objectiveText);
            SetReference(controller, "missionTimerText", timerText);

            SetReference(controller, "scoreValueText", scoreVal);
            SetReference(controller, "safetyValueText", safetyVal);
            SetReference(controller, "safetyMeterFill", safetyFill);
            SetReference(controller, "trafficFlowValueText", flowVal);
            SetReference(controller, "trafficFlowMeterFill", flowFill);

            SetReference(controller, "feedbackBanner", toast);
            SetReference(controller, "feedbackGroup", toastCg);
            SetReference(controller, "feedbackMessageText", toastMsg);

            SetReference(controller, "northSouthStatusText", nsStatus);
            SetReference(controller, "eastWestStatusText", ewStatus);
            SetReference(controller, "pedestrianStatusText", pedStatus);
            SetReference(controller, "emergencyStatusText", emgStatus);
            SetReference(controller, "phaseTimerText", timerLbl);
            SetReference(controller, "vehiclesWaitingText", vehQueueText);
            SetReference(controller, "pedestriansWaitingText", pedQueueText);
            SetReference(controller, "changePhaseButton", changePhaseBtn);
            SetReference(controller, "pedestrianWalkButton", pedWalkBtn);

            SetReference(controller, "pausePanel", pauseObj);
            SetReference(controller, "resumeButton", resumeBtn);
            SetReference(controller, "restartButton", restartBtn);
            SetReference(controller, "pauseMenuButton", pauseMenuBtn);

            SetReference(controller, "completionPanel", completeObj);
            SetReference(controller, "completionGroup", compCg);
            SetReference(controller, "completionTitleText", compTitle);
            SetReference(controller, "completionSubtitleText", compSub);
            SetReference(controller, "finalScoreText", scoreStatVal);
            SetReference(controller, "safetyScoreText", safetyStatVal);
            SetReference(controller, "flowScoreText", flowStatVal);
            SetReference(controller, "pedestriansCrossedText", pedStatVal);
            SetReference(controller, "emergenciesClearedText", emgStatVal);
            SetReference(controller, "collisionsText", collisionStatVal);
            SetReference(controller, "playAgainButton", playAgainBtn);
            SetReference(controller, "backToMenuButton", menuBtn);

            return controller;
        }

        private static TMP_Text CreateStatRow(Transform parent, string rowName, string label, string initVal, float y)
        {
            GameObject row = FindOrCreateChild(parent, rowName);
            RectTransform rt = GetOrAdd<RectTransform>(row);
            SetRect(rt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 26f));

            TMP_Text lbl = CreateUIText(row.transform, "Label", label, 12, TextAlignmentOptions.Left, new Vector2(200f, 24f), new Vector2(16f, 0f), new Vector2(0f, 0.5f));
            lbl.color = TrafficTownTheme.TextSecondaryColor;

            TMP_Text val = CreateUIText(row.transform, "Val", initVal, 13, TextAlignmentOptions.Right, new Vector2(100f, 24f), new Vector2(-16f, 0f), new Vector2(1f, 0.5f));
            val.color = Color.white;
            val.fontStyle = FontStyles.Bold;

            return val;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, Color col, Sprite sprite)
        {
            GameObject btnObj = CreateUIPanel(parent, name, col, sprite);
            SetRect(btnObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), anchoredPos, size);
            Image img = btnObj.GetComponent<Image>();
            img.raycastTarget = true;

            Button btn = GetOrAdd<Button>(btnObj);
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;

            TMP_Text txt = CreateUIText(btnObj.transform, "Label", label, 12, TextAlignmentOptions.Center, new Vector2(size.x - 10f, size.y - 6f), Vector2.zero, new Vector2(0.5f, 0.5f));
            txt.color = Color.white;
            txt.fontStyle = FontStyles.Bold;

            return btn;
        }
        #endregion

        #region Helpers
        private static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Color color, Sprite sprite = null)
        {
            GameObject go = FindOrCreateChild(parent, name);
            Image img = GetOrAdd<Image>(go);
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            return go;
        }

        private static TMP_Text CreateUIText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align, Vector2 size, Vector2 anchoredPos, Vector2 pivot, Vector2? anchor = null)
        {
            GameObject go = FindOrCreateChild(parent, name);
            TMP_Text tmp = GetOrAdd<TextMeshProUGUI>(go);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            RectTransform rt = tmp.rectTransform;
            Vector2 anc = anchor.HasValue ? anchor.Value : pivot;
            rt.anchorMin = anc;
            rt.anchorMax = anc;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            return tmp;
        }

        private static void AddShadow(GameObject obj)
        {
            Shadow s = obj.GetComponent<Shadow>();
            if (s == null) s = obj.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.45f);
            s.effectDistance = new Vector2(2f, -3f);
        }

        private static void SetFullScreen(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetRect(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
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

        private static void EnsureAssetFolders()
        {
            string[] folders = { "Assets/Scenes", "Assets/JSON", "Assets/Materials" };
            foreach (var f in folders)
            {
                if (!Directory.Exists(f)) Directory.CreateDirectory(f);
            }
        }

        private static GameObject FindOrCreate(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go == null) go = new GameObject(name);
            return go;
        }

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) return t.gameObject;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(t.GetChild(i).gameObject);
            }
        }

        private static void SetReference(object target, string fieldName, object value)
        {
            if (target == null) return;
            FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fi != null) fi.SetValue(target, value);
        }

        private static void SetInt(object target, string fieldName, int value)
        {
            if (target == null) return;
            FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fi != null) fi.SetValue(target, value);
        }
        #endregion
    }
}
#endif
