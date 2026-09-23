using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using TrafficTown2D.Core;
using TrafficTown2D.Level3;
using TrafficTown2D.UI;

namespace TrafficTown2D.Level5
{
    public class Level5Bootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == "Level5")
            {
                EnsureLevel5Scene();
            }
        }

        private void Awake()
        {
            EnsureLevel5Scene();
        }

        public static void EnsureLevel5Scene()
        {
            // 1. Camera - Always ensure top-down orthographic camera size 13.5 and position (0, 0, -10)
            Camera cam = EnsureCamera();

            // 2. Check if scene already has the complete new inspector environment
            GameObject world = GameObject.Find("World");
            bool hasInspector = world != null && world.transform.Find("TrafficInspector") != null;

            // If already initialized with new layout, don't duplicate, but ensure road scale is up-to-date!
            if (TrafficPhaseController.Instance != null && hasInspector)
            {
                EnsureEnvironmentScale(world.transform);
                Level5UIController existingCtrl = Object.FindAnyObjectByType<Level5UIController>(FindObjectsInactive.Include);
                if (existingCtrl != null)
                {
                    existingCtrl.SanitizeLayout();
                    existingCtrl.InitializeButtons();
                }
                return;
            }

            Debug.Log("[Level5Bootstrap] Bootstrapping Level 5 Traffic Controller scene...");

            // 2. Global Daylight
            EnsureGlobalLight();

            // 3. EventSystem
            EnsureEventSystem();

            // 4. Services
            GameObject services = GameObject.Find("Services");
            if (services == null) services = new GameObject("Services");
            if (services.GetComponent<SceneLoader>() == null) services.AddComponent<SceneLoader>();
            if (services.GetComponent<GameManager>() == null)
            {
                GameManager gm = services.AddComponent<GameManager>();
                gm.SetState(GameState.Playing);
            }

            AudioSource sirenAudio = services.GetComponent<AudioSource>();
            if (sirenAudio == null) sirenAudio = services.AddComponent<AudioSource>();
            sirenAudio.playOnAwake = false;
            sirenAudio.loop = true;
            sirenAudio.volume = 0.5f;

            // 5. World & Environment
            if (world == null) world = GameObject.Find("World");
            if (world == null) world = new GameObject("World");
            BuildIntersectionEnvironment(world.transform);

            // 6. Traffic Lights
            Level3TrafficLight northLight, southLight, eastLight, westLight;
            BuildTrafficLights(world.transform, out northLight, out southLight, out eastLight, out westLight);

            // 7. Containers
            Transform vContainer = world.transform.Find("Vehicles");
            if (vContainer == null)
            {
                GameObject vObj = new GameObject("Vehicles");
                vObj.transform.SetParent(world.transform, false);
                vContainer = vObj.transform;
            }

            Transform pContainer = world.transform.Find("Pedestrians");
            if (pContainer == null)
            {
                GameObject pObj = new GameObject("Pedestrians");
                pObj.transform.SetParent(world.transform, false);
                pContainer = pObj.transform;
            }

            // 8. Controllers
            GameObject ctrlObj = GameObject.Find("GameplayControllers");
            if (ctrlObj == null) ctrlObj = new GameObject("GameplayControllers");

            TrafficPhaseController phaseCtrl = ctrlObj.GetComponent<TrafficPhaseController>();
            if (phaseCtrl == null) phaseCtrl = ctrlObj.AddComponent<TrafficPhaseController>();
            phaseCtrl.ConfigureLights(northLight, southLight, eastLight, westLight);

            TrafficQueueManager queueMgr = ctrlObj.GetComponent<TrafficQueueManager>();
            if (queueMgr == null) queueMgr = ctrlObj.AddComponent<TrafficQueueManager>();
            queueMgr.SetVehicleContainer(vContainer);
            queueMgr.SetObstacleLayers(LayerMask.GetMask("Default"));
            queueMgr.SetSprites(
                LoadSprite("CarBlueTopDown"),
                LoadSprite("CarRedTopDown"),
                LoadSprite("TaxiTopDown"),
                LoadSprite("BusTopDown"),
                LoadSprite("AmbulanceTopDown")
            );

            PedestrianIntersectionManager pedMgr = ctrlObj.GetComponent<PedestrianIntersectionManager>();
            if (pedMgr == null) pedMgr = ctrlObj.AddComponent<PedestrianIntersectionManager>();
            pedMgr.SetContainer(pContainer);
            pedMgr.SetSprites(
                LoadSprite("ChildBoyTopDown"),
                LoadSprite("ChildGirlTopDown"),
                LoadSprite("AdultPedestrianDarkTopDown"),
                LoadSprite("CrossingGuardTopDown")
            );

            EmergencyVehicleManager emgMgr = ctrlObj.GetComponent<EmergencyVehicleManager>();
            if (emgMgr == null) emgMgr = ctrlObj.AddComponent<EmergencyVehicleManager>();
            emgMgr.SetAudioSource(sirenAudio);

            Level5SafetyManager safetyMgr = ctrlObj.GetComponent<Level5SafetyManager>();
            if (safetyMgr == null) safetyMgr = ctrlObj.AddComponent<Level5SafetyManager>();

            // 9. UI
            Level5UIController uiCtrl = Object.FindAnyObjectByType<Level5UIController>(FindObjectsInactive.Include);
            if (uiCtrl == null || !hasInspector)
            {
                uiCtrl = BuildUI();
            }

            // 10. Mission Manager
            Level5MissionManager missionMgr = ctrlObj.GetComponent<Level5MissionManager>();
            if (missionMgr == null) missionMgr = ctrlObj.AddComponent<Level5MissionManager>();
            SetRef(missionMgr, "queueManager", queueMgr);
            SetRef(missionMgr, "pedestrianManager", pedMgr);
            SetRef(missionMgr, "emergencyManager", emgMgr);
            SetRef(missionMgr, "safetyManager", safetyMgr);
            SetRef(missionMgr, "uiController", uiCtrl);

            Time.timeScale = 1f;
            Debug.Log("[Level5Bootstrap] Level 5 scene successfully initialized!");
        }

        private static Camera EnsureCamera()
        {
            Camera cam = Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            cam.gameObject.tag = "MainCamera";
            cam.enabled = true;
            cam.gameObject.SetActive(true);
            cam.orthographic = true;
            cam.orthographicSize = 13.5f; // Perfect 4-way 9.6-wide framing
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.35f, 0.65f, 0.35f, 1f);
            cam.cullingMask = ~0;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            cam.depth = 0;
            cam.targetDisplay = 0;

            UniversalAdditionalCameraData camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderType = CameraRenderType.Base;
            camData.renderPostProcessing = false;

            return cam;
        }

        private static void EnsureGlobalLight()
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
            EventSystem es = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
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

        public static void EnsureEnvironmentScale(Transform world)
        {
            if (world == null) return;

            Transform ewRoad = world.Find("Road_EastWest");
            if (ewRoad != null) ewRoad.localScale = new Vector3(90f, 9.6f, 1f);

            Transform nsRoad = world.Find("Road_NorthSouth");
            if (nsRoad != null) nsRoad.localScale = new Vector3(9.6f, 60f, 1f);

            Transform grass = world.Find("GroundGrass");
            if (grass != null) grass.localScale = new Vector3(110f, 65f, 1f);

            // Sidewalk corners
            Transform swNW = world.Find("Sidewalk_NW");
            if (swNW != null) { swNW.localPosition = new Vector3(-24.8f, 15.8f, 0f); swNW.localScale = new Vector3(40f, 22f, 1f); }
            Transform swNE = world.Find("Sidewalk_NE");
            if (swNE != null) { swNE.localPosition = new Vector3(24.8f, 15.8f, 0f); swNE.localScale = new Vector3(40f, 22f, 1f); }
            Transform swSW = world.Find("Sidewalk_SW");
            if (swSW != null) { swSW.localPosition = new Vector3(-24.8f, -15.8f, 0f); swSW.localScale = new Vector3(40f, 22f, 1f); }
            Transform swSE = world.Find("Sidewalk_SE");
            if (swSE != null) { swSE.localPosition = new Vector3(24.8f, -15.8f, 0f); swSE.localScale = new Vector3(40f, 22f, 1f); }

            // Curbs
            Transform curbNWH = world.Find("Curb_NW_H");
            if (curbNWH != null) { curbNWH.localPosition = new Vector3(-24.8f, 4.95f, 0f); curbNWH.localScale = new Vector3(40f, 0.3f, 1f); }
            Transform curbNWV = world.Find("Curb_NW_V");
            if (curbNWV != null) { curbNWV.localPosition = new Vector3(-4.95f, 15.8f, 0f); curbNWV.localScale = new Vector3(0.3f, 22f, 1f); }

            Transform curbNEH = world.Find("Curb_NE_H");
            if (curbNEH != null) { curbNEH.localPosition = new Vector3(24.8f, 4.95f, 0f); curbNEH.localScale = new Vector3(40f, 0.3f, 1f); }
            Transform curbNEV = world.Find("Curb_NE_V");
            if (curbNEV != null) { curbNEV.localPosition = new Vector3(4.95f, 15.8f, 0f); curbNEV.localScale = new Vector3(0.3f, 22f, 1f); }

            Transform curbSWH = world.Find("Curb_SW_H");
            if (curbSWH != null) { curbSWH.localPosition = new Vector3(-24.8f, -4.95f, 0f); curbSWH.localScale = new Vector3(40f, 0.3f, 1f); }
            Transform curbSWV = world.Find("Curb_SW_V");
            if (curbSWV != null) { curbSWV.localPosition = new Vector3(-4.95f, -15.8f, 0f); curbSWV.localScale = new Vector3(0.3f, 22f, 1f); }

            Transform curbSEH = world.Find("Curb_SE_H");
            if (curbSEH != null) { curbSEH.localPosition = new Vector3(24.8f, -4.95f, 0f); curbSEH.localScale = new Vector3(40f, 0.3f, 1f); }
            Transform curbSEV = world.Find("Curb_SE_V");
            if (curbSEV != null) { curbSEV.localPosition = new Vector3(4.95f, -15.8f, 0f); curbSEV.localScale = new Vector3(0.3f, 22f, 1f); }

            // Yellow dividers
            Transform yE = world.Find("YellowDivider_E");
            if (yE != null) { yE.localPosition = new Vector3(25.9f, -0.07f, 0f); yE.localScale = new Vector3(36.2f, 0.08f, 1f); }
            Transform yE2 = world.Find("YellowDivider_E2");
            if (yE2 != null) { yE2.localPosition = new Vector3(25.9f, 0.07f, 0f); yE2.localScale = new Vector3(36.2f, 0.08f, 1f); }

            Transform yW = world.Find("YellowDivider_W");
            if (yW != null) { yW.localPosition = new Vector3(-25.9f, -0.07f, 0f); yW.localScale = new Vector3(36.2f, 0.08f, 1f); }
            Transform yW2 = world.Find("YellowDivider_W2");
            if (yW2 != null) { yW2.localPosition = new Vector3(-25.9f, 0.07f, 0f); yW2.localScale = new Vector3(36.2f, 0.08f, 1f); }

            Transform yN = world.Find("YellowDivider_N");
            if (yN != null) { yN.localPosition = new Vector3(-0.07f, 17.5f, 0f); yN.localScale = new Vector3(0.08f, 19.4f, 1f); }
            Transform yN2 = world.Find("YellowDivider_N2");
            if (yN2 != null) { yN2.localPosition = new Vector3(0.07f, 17.5f, 0f); yN2.localScale = new Vector3(0.08f, 19.4f, 1f); }

            Transform yS = world.Find("YellowDivider_S");
            if (yS != null) { yS.localPosition = new Vector3(-0.07f, -17.5f, 0f); yS.localScale = new Vector3(0.08f, 19.4f, 1f); }
            Transform yS2 = world.Find("YellowDivider_S2");
            if (yS2 != null) { yS2.localPosition = new Vector3(0.07f, -17.5f, 0f); yS2.localScale = new Vector3(0.08f, 19.4f, 1f); }
        }

        private static void BuildIntersectionEnvironment(Transform world)
        {
            // Clear existing if any
            for (int i = world.childCount - 1; i >= 0; i--)
            {
                string cname = world.GetChild(i).name;
                if (cname == "Vehicles" || cname == "Pedestrians") continue;
                Destroy(world.GetChild(i).gameObject);
            }

            Color roadCol = new Color(0.14f, 0.15f, 0.18f, 1f);
            Color sidewalkCol = new Color(0.72f, 0.76f, 0.74f, 1f);
            Color curbCol = new Color(0.55f, 0.58f, 0.56f, 1f);
            Color yellowLineCol = new Color(1f, 0.92f, 0.25f, 1f);
            Color whiteLineCol = new Color(0.95f, 0.95f, 0.95f, 1f);

            // Ground Grass (covers entire camera view in any aspect ratio)
            CreateSprite(world, "GroundGrass", Vector3.zero, new Vector3(110f, 65f, 1f), new Color(0.38f, 0.68f, 0.36f, 1f), -12);

            // Roads: North-South & East-West (width 9.6f fits 4 lanes total; length extends past screen edges)
            CreateSprite(world, "Road_NorthSouth", Vector3.zero, new Vector3(9.6f, 60f, 1f), roadCol, -10);
            CreateSprite(world, "Road_EastWest", Vector3.zero, new Vector3(90f, 9.6f, 1f), roadCol, -10);
            CreateSprite(world, "IntersectionBox", Vector3.zero, new Vector3(9.6f, 9.6f, 1f), roadCol, -9);

            // Double Yellow Center Divider Lines (spans outer roads, leaves intersection & crosswalks clear)
            // North Leg (Y = 7.8 to 27.2)
            CreateSprite(world, "YellowDivider_N", new Vector3(-0.07f, 17.5f, 0f), new Vector3(0.08f, 19.4f, 1f), yellowLineCol, -8);
            CreateSprite(world, "YellowDivider_N2", new Vector3(0.07f, 17.5f, 0f), new Vector3(0.08f, 19.4f, 1f), yellowLineCol, -8);
            // South Leg (Y = -7.8 to -27.2)
            CreateSprite(world, "YellowDivider_S", new Vector3(-0.07f, -17.5f, 0f), new Vector3(0.08f, 19.4f, 1f), yellowLineCol, -8);
            CreateSprite(world, "YellowDivider_S2", new Vector3(0.07f, -17.5f, 0f), new Vector3(0.08f, 19.4f, 1f), yellowLineCol, -8);
            // East Leg (X = 7.8 to 44.0)
            CreateSprite(world, "YellowDivider_E", new Vector3(25.9f, -0.07f, 0f), new Vector3(36.2f, 0.08f, 1f), yellowLineCol, -8);
            CreateSprite(world, "YellowDivider_E2", new Vector3(25.9f, 0.07f, 0f), new Vector3(36.2f, 0.08f, 1f), yellowLineCol, -8);
            // West Leg (X = -7.8 to -44.0)
            CreateSprite(world, "YellowDivider_W", new Vector3(-25.9f, -0.07f, 0f), new Vector3(36.2f, 0.08f, 1f), yellowLineCol, -8);
            CreateSprite(world, "YellowDivider_W2", new Vector3(-25.9f, 0.07f, 0f), new Vector3(36.2f, 0.08f, 1f), yellowLineCol, -8);

            // Dashed White Lane Dividers (at ±2.4f separating inner and outer lanes on each approach)
            for (float y = 8.5f; y <= 26.5f; y += 1.5f)
            {
                CreateSprite(world, $"LaneDash_N_L_{y:0.0}", new Vector3(-2.4f, y, 0f), new Vector3(0.08f, 0.7f, 1f), whiteLineCol, -8);
                CreateSprite(world, $"LaneDash_N_R_{y:0.0}", new Vector3(2.4f, y, 0f), new Vector3(0.08f, 0.7f, 1f), whiteLineCol, -8);
            }
            for (float y = -8.5f; y >= -26.5f; y -= 1.5f)
            {
                CreateSprite(world, $"LaneDash_S_L_{y:0.0}", new Vector3(-2.4f, y, 0f), new Vector3(0.08f, 0.7f, 1f), whiteLineCol, -8);
                CreateSprite(world, $"LaneDash_S_R_{y:0.0}", new Vector3(2.4f, y, 0f), new Vector3(0.08f, 0.7f, 1f), whiteLineCol, -8);
            }
            for (float x = 8.5f; x <= 43.5f; x += 1.5f)
            {
                CreateSprite(world, $"LaneDash_E_B_{x:0.0}", new Vector3(x, -2.4f, 0f), new Vector3(0.7f, 0.08f, 1f), whiteLineCol, -8);
                CreateSprite(world, $"LaneDash_E_T_{x:0.0}", new Vector3(x, 2.4f, 0f), new Vector3(0.7f, 0.08f, 1f), whiteLineCol, -8);
            }
            for (float x = -8.5f; x >= -43.5f; x -= 1.5f)
            {
                CreateSprite(world, $"LaneDash_W_B_{x:0.0}", new Vector3(x, -2.4f, 0f), new Vector3(0.7f, 0.08f, 1f), whiteLineCol, -8);
                CreateSprite(world, $"LaneDash_W_T_{x:0.0}", new Vector3(x, 2.4f, 0f), new Vector3(0.7f, 0.08f, 1f), whiteLineCol, -8);
            }

            // Stop Lines (at ±7.6f across incoming lanes, width 4.8f, thickness 0.28f)
            CreateSprite(world, "StopLine_North", new Vector3(-2.4f, 7.6f, 0f), new Vector3(4.8f, 0.28f, 1f), whiteLineCol, -7);
            CreateSprite(world, "StopLine_South", new Vector3(2.4f, -7.6f, 0f), new Vector3(4.8f, 0.28f, 1f), whiteLineCol, -7);
            CreateSprite(world, "StopLine_East", new Vector3(7.6f, 2.4f, 0f), new Vector3(0.28f, 4.8f, 1f), whiteLineCol, -7);
            CreateSprite(world, "StopLine_West", new Vector3(-7.6f, -2.4f, 0f), new Vector3(0.28f, 4.8f, 1f), whiteLineCol, -7);

            // Zebra Crosswalks (at ±6.2f, 11 crisp stripes spanning 9.6-wide road)
            BuildZebraCrossingGroup(world, "Crosswalk_North", new Vector3(0f, 6.2f, 0f), isHorizontalRoad: false);
            BuildZebraCrossingGroup(world, "Crosswalk_South", new Vector3(0f, -6.2f, 0f), isHorizontalRoad: false);
            BuildZebraCrossingGroup(world, "Crosswalk_East", new Vector3(6.2f, 0f, 0f), isHorizontalRoad: true);
            BuildZebraCrossingGroup(world, "Crosswalk_West", new Vector3(-6.2f, 0f, 0f), isHorizontalRoad: true);

            // Sidewalk Corners (4 quadrants, corner road edge is at ±4.8f; covers wide aspect ratios)
            CreateSprite(world, "Sidewalk_NW", new Vector3(-24.8f, 15.8f, 0f), new Vector3(40f, 22f, 1f), sidewalkCol, -11);
            CreateSprite(world, "Sidewalk_NE", new Vector3(24.8f, 15.8f, 0f), new Vector3(40f, 22f, 1f), sidewalkCol, -11);
            CreateSprite(world, "Sidewalk_SW", new Vector3(-24.8f, -15.8f, 0f), new Vector3(40f, 22f, 1f), sidewalkCol, -11);
            CreateSprite(world, "Sidewalk_SE", new Vector3(24.8f, -15.8f, 0f), new Vector3(40f, 22f, 1f), sidewalkCol, -11);

            // Curbs (darker borders along sidewalk boundaries at ±4.95f)
            CreateSprite(world, "Curb_NW_H", new Vector3(-24.8f, 4.95f, 0f), new Vector3(40f, 0.3f, 1f), curbCol, -10);
            CreateSprite(world, "Curb_NW_V", new Vector3(-4.95f, 15.8f, 0f), new Vector3(0.3f, 22f, 1f), curbCol, -10);

            CreateSprite(world, "Curb_NE_H", new Vector3(24.8f, 4.95f, 0f), new Vector3(40f, 0.3f, 1f), curbCol, -10);
            CreateSprite(world, "Curb_NE_V", new Vector3(4.95f, 15.8f, 0f), new Vector3(0.3f, 22f, 1f), curbCol, -10);

            CreateSprite(world, "Curb_SW_H", new Vector3(-24.8f, -4.95f, 0f), new Vector3(40f, 0.3f, 1f), curbCol, -10);
            CreateSprite(world, "Curb_SW_V", new Vector3(-4.95f, -15.8f, 0f), new Vector3(0.3f, 22f, 1f), curbCol, -10);

            CreateSprite(world, "Curb_SE_H", new Vector3(24.8f, -4.95f, 0f), new Vector3(40f, 0.3f, 1f), curbCol, -10);
            CreateSprite(world, "Curb_SE_V", new Vector3(4.95f, -15.8f, 0f), new Vector3(0.3f, 22f, 1f), curbCol, -10);

            // Central Safety Island Podium (at 0,0 - provides 1.5 - 2 vehicle widths clearance from inner lanes at ±1.6)
            CreateSprite(world, "Podium_Outer", Vector3.zero, new Vector3(2.7f, 2.7f, 1f), curbCol, -8, true);
            CreateSprite(world, "Podium_YellowRing", Vector3.zero, new Vector3(2.4f, 2.4f, 1f), yellowLineCol, -7, true);
            CreateSprite(world, "Podium_InnerPad", Vector3.zero, new Vector3(2.1f, 2.1f, 1f), new Color(0.20f, 0.22f, 0.26f, 1f), -6, true);

            // Central Police Inspector (stands safely in center island with smooth rotation & animation)
            CreatePoliceInspector(world);

            // Decorative Trees
            CreateTree(world, "Tree_NW_1", new Vector3(-9.5f, 9.5f, 0f), 1.6f);
            CreateTree(world, "Tree_NW_2", new Vector3(-13.0f, 8.5f, 0f), 1.4f);
            CreateTree(world, "Tree_NE_1", new Vector3(9.5f, 9.5f, 0f), 1.6f);
            CreateTree(world, "Tree_NE_2", new Vector3(13.0f, 8.5f, 0f), 1.4f);
            CreateTree(world, "Tree_SW_1", new Vector3(-9.5f, -9.5f, 0f), 1.6f);
            CreateTree(world, "Tree_SW_2", new Vector3(-13.0f, -8.5f, 0f), 1.4f);
            CreateTree(world, "Tree_SE_1", new Vector3(9.5f, -9.5f, 0f), 1.6f);
            CreateTree(world, "Tree_SE_2", new Vector3(13.0f, -8.5f, 0f), 1.4f);
        }

        private static void CreatePoliceInspector(Transform world)
        {
            GameObject inspectorObj = new GameObject("TrafficInspector");
            inspectorObj.transform.SetParent(world, false);
            inspectorObj.transform.position = Vector3.zero;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(inspectorObj.transform, false);
            visual.transform.localPosition = Vector3.zero;

            // Inspector shadow
            CreateSprite(visual.transform, "Shadow", new Vector3(0.08f, -0.08f, 0f), new Vector3(1.3f, 0.8f, 1f), new Color(0f, 0f, 0f, 0.35f), 4, true);

            // Inspector sprite
            GameObject body = new GameObject("Body");
            body.transform.SetParent(visual.transform, false);
            SpriteRenderer bsr = body.AddComponent<SpriteRenderer>();
            bsr.sprite = LoadSprite("CrossingGuardTopDown");
            bsr.sortingOrder = 5;
            body.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            CircleCollider2D col = inspectorObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.2f;

            TrafficInspectorController ctrl = inspectorObj.AddComponent<TrafficInspectorController>();
            ctrl.SetVisualRoot(visual.transform);
        }

        private static void BuildZebraCrossingGroup(Transform parent, string name, Vector3 centerPos, bool isHorizontalRoad)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            group.transform.position = centerPos;

            Color stripeCol = new Color(0.95f, 0.95f, 0.95f, 0.92f);
            float[] offsets = { -4.0f, -3.2f, -2.4f, -1.6f, -0.8f, 0f, 0.8f, 1.6f, 2.4f, 3.2f, 4.0f };

            if (isHorizontalRoad)
            {
                for (int i = 0; i < offsets.Length; i++)
                {
                    CreateSprite(group.transform, $"Stripe_{i}", new Vector3(0f, offsets[i], 0f), new Vector3(1.6f, 0.42f, 1f), stripeCol, -7);
                }
            }
            else
            {
                for (int i = 0; i < offsets.Length; i++)
                {
                    CreateSprite(group.transform, $"Stripe_{i}", new Vector3(offsets[i], 0f, 0f), new Vector3(0.42f, 1.6f, 1f), stripeCol, -7);
                }
            }
        }

        private static void BuildTrafficLights(Transform world, 
            out Level3TrafficLight northLight, 
            out Level3TrafficLight southLight, 
            out Level3TrafficLight eastLight, 
            out Level3TrafficLight westLight)
        {
            GameObject signalGroup = new GameObject("TrafficSignals");
            signalGroup.transform.SetParent(world, false);

            southLight = BuildPole(signalGroup.transform, "Signal_Southbound", new Vector3(-5.5f, 7.6f, 0f), 0f);
            northLight = BuildPole(signalGroup.transform, "Signal_Northbound", new Vector3(5.5f, -7.6f, 0f), 180f);
            westLight = BuildPole(signalGroup.transform, "Signal_Westbound", new Vector3(7.6f, 5.5f, 0f), -90f);
            eastLight = BuildPole(signalGroup.transform, "Signal_Eastbound", new Vector3(-7.6f, -5.5f, 0f), 90f);
        }

        private static Level3TrafficLight BuildPole(Transform parent, string name, Vector3 pos, float rotZ)
        {
            GameObject pole = new GameObject(name);
            pole.transform.SetParent(parent, false);
            pole.transform.position = pos;
            pole.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
            pole.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            Sprite bodySprite = LoadSprite("TrafficLightBody");
            Sprite lensSprite = LoadSprite("TrafficLightLens");

            // Housing
            GameObject body = new GameObject("Housing");
            body.transform.SetParent(pole.transform, false);
            SpriteRenderer bsr = body.AddComponent<SpriteRenderer>();
            bsr.sprite = bodySprite != null ? bodySprite : GetOrCreateSquare();
            bsr.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            bsr.sortingOrder = 10;
            body.transform.localScale = new Vector3(0.85f, 1.9f, 1f);

            // Red
            GameObject red = new GameObject("RedLens");
            red.transform.SetParent(pole.transform, false);
            red.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            SpriteRenderer rsr = red.AddComponent<SpriteRenderer>();
            rsr.sprite = lensSprite != null ? lensSprite : GetOrCreateCircle();
            rsr.color = new Color(1f, 0.15f, 0.15f, 1f);
            rsr.sortingOrder = 11;
            red.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

            // Yellow
            GameObject yellow = new GameObject("YellowLens");
            yellow.transform.SetParent(pole.transform, false);
            yellow.transform.localPosition = new Vector3(0f, 0f, 0f);
            SpriteRenderer ysr = yellow.AddComponent<SpriteRenderer>();
            ysr.sprite = lensSprite != null ? lensSprite : GetOrCreateCircle();
            ysr.color = new Color(0.3f, 0.25f, 0.05f, 0.5f);
            ysr.sortingOrder = 11;
            yellow.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

            // Green
            GameObject green = new GameObject("GreenLens");
            green.transform.SetParent(pole.transform, false);
            green.transform.localPosition = new Vector3(0f, -0.65f, 0f);
            SpriteRenderer gsr = green.AddComponent<SpriteRenderer>();
            gsr.sprite = lensSprite != null ? lensSprite : GetOrCreateCircle();
            gsr.color = new Color(0.05f, 0.3f, 0.1f, 0.5f);
            gsr.sortingOrder = 11;
            green.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

            Level3TrafficLight tl = pole.AddComponent<Level3TrafficLight>();
            SetRef(tl, "redLens", rsr);
            SetRef(tl, "yellowLens", ysr);
            SetRef(tl, "greenLens", gsr);

            return tl;
        }

        private static Level5UIController BuildUI()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                for (int i = canvas.transform.childCount - 1; i >= 0; i--)
                {
                    Object.Destroy(canvas.transform.GetChild(i).gameObject);
                }
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Sprite roundedSpr = LoadSprite("RoundedPanel");

            // Root HUD
            GameObject hud = CreatePanel(canvas.transform, "HUD", new Color(0f, 0f, 0f, 0f));
            SetFull(hud.GetComponent<RectTransform>());

            // Mission Card (Top-Left)
            GameObject mCard = CreatePanel(hud.transform, "MissionCard", TrafficTownTheme.CardDarkColor, roundedSpr);
            SetR(mCard.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(35f, -30f), new Vector2(460f, 160f));
            AddShadow(mCard);

            GameObject badgePill = CreatePanel(mCard.transform, "BadgePill", TrafficTownTheme.PrimaryColor, roundedSpr);
            SetR(badgePill.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -16f), new Vector2(140f, 28f));
            TMP_Text badgeText = CreateText(badgePill.transform, "BadgeText", "MISSION 1 OF 5", 12, TextAlignmentOptions.Center, new Vector2(130f, 24f), Vector2.zero, new Vector2(0.5f, 0.5f));
            badgeText.color = Color.white;
            badgeText.fontStyle = FontStyles.Bold;

            TMP_Text timerText = CreateText(mCard.transform, "TimerText", "Time: 50s", 14, TextAlignmentOptions.Right, new Vector2(120f, 24f), new Vector2(-18f, -18f), new Vector2(1f, 1f));
            timerText.color = TrafficTownTheme.AccentColor;
            timerText.fontStyle = FontStyles.Bold;

            TMP_Text titleText = CreateText(mCard.transform, "Title", "LEARN THE INTERSECTION", 15, TextAlignmentOptions.Left, new Vector2(424f, 26f), new Vector2(18f, -54f), new Vector2(0f, 1f));
            titleText.color = TrafficTownTheme.AccentColor;
            titleText.fontStyle = FontStyles.Bold;

            TMP_Text objText = CreateText(mCard.transform, "Objective", "Operate the traffic signals. Maintain safe transitions and keep traffic flowing smoothly.", 13, TextAlignmentOptions.TopLeft, new Vector2(424f, 60f), new Vector2(18f, -86f), new Vector2(0f, 1f));
            objText.color = TrafficTownTheme.TextPrimaryColor;

            // Score & Metrics Card (Top-Right)
            GameObject sCard = CreatePanel(hud.transform, "ScoreCard", TrafficTownTheme.CardDarkColor, roundedSpr);
            SetR(sCard.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-35f, -30f), new Vector2(400f, 160f));
            AddShadow(sCard);

            TMP_Text sLabel = CreateText(sCard.transform, "ScoreLabel", "SCORE", 12, TextAlignmentOptions.Left, new Vector2(100f, 20f), new Vector2(20f, -18f), new Vector2(0f, 1f));
            sLabel.color = TrafficTownTheme.TextSecondaryColor;
            sLabel.fontStyle = FontStyles.Bold;

            TMP_Text sVal = CreateText(sCard.transform, "ScoreVal", "0", 28, TextAlignmentOptions.Left, new Vector2(120f, 36f), new Vector2(20f, -44f), new Vector2(0f, 1f));
            sVal.color = Color.white;
            sVal.fontStyle = FontStyles.Bold;

            // Safety
            TMP_Text safLabel = CreateText(sCard.transform, "SafLabel", "SAFETY", 11, TextAlignmentOptions.Left, new Vector2(100f, 20f), new Vector2(160f, -18f), new Vector2(0f, 1f));
            safLabel.color = TrafficTownTheme.TextSecondaryColor;
            safLabel.fontStyle = FontStyles.Bold;

            TMP_Text safVal = CreateText(sCard.transform, "SafVal", "100%", 13, TextAlignmentOptions.Right, new Vector2(60f, 20f), new Vector2(-20f, -18f), new Vector2(1f, 1f));
            safVal.color = TrafficTownTheme.SuccessColor;
            safVal.fontStyle = FontStyles.Bold;

            GameObject safTrough = CreatePanel(sCard.transform, "SafTrough", new Color(0.15f, 0.20f, 0.28f, 0.9f), roundedSpr);
            SetR(safTrough.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160f, -42f), new Vector2(220f, 12f));
            GameObject safFillObj = CreatePanel(safTrough.transform, "Fill", TrafficTownTheme.SuccessColor, roundedSpr);
            SetR(safFillObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image safFill = safFillObj.GetComponent<Image>();
            safFill.type = Image.Type.Filled;
            safFill.fillMethod = Image.FillMethod.Horizontal;
            safFill.fillAmount = 1f;

            // Flow
            TMP_Text fLabel = CreateText(sCard.transform, "FlowLabel", "TRAFFIC FLOW", 11, TextAlignmentOptions.Left, new Vector2(120f, 20f), new Vector2(160f, -78f), new Vector2(0f, 1f));
            fLabel.color = TrafficTownTheme.TextSecondaryColor;
            fLabel.fontStyle = FontStyles.Bold;

            TMP_Text fVal = CreateText(sCard.transform, "FlowVal", "100%", 13, TextAlignmentOptions.Right, new Vector2(60f, 20f), new Vector2(-20f, -78f), new Vector2(1f, 1f));
            fVal.color = new Color(0.25f, 0.75f, 1f, 1f);
            fVal.fontStyle = FontStyles.Bold;

            GameObject fTrough = CreatePanel(sCard.transform, "FlowTrough", new Color(0.15f, 0.20f, 0.28f, 0.9f), roundedSpr);
            SetR(fTrough.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160f, -102f), new Vector2(220f, 12f));
            GameObject fFillObj = CreatePanel(fTrough.transform, "Fill", new Color(0.25f, 0.75f, 1f, 1f), roundedSpr);
            SetR(fFillObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image fFill = fFillObj.GetComponent<Image>();
            fFill.type = Image.Type.Filled;
            fFill.fillMethod = Image.FillMethod.Horizontal;
            fFill.fillAmount = 1f;

            // Feedback Toast (Top-Center)
            GameObject toast = CreatePanel(hud.transform, "FeedbackToast", new Color(0.08f, 0.12f, 0.18f, 0.95f), roundedSpr);
            SetR(toast.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(560f, 48f));
            CanvasGroup toastCg = toast.AddComponent<CanvasGroup>();
            TMP_Text toastMsg = CreateText(toast.transform, "Message", "✓ Safe phase transition completed.", 14, TextAlignmentOptions.Center, new Vector2(540f, 38f), Vector2.zero, new Vector2(0.5f, 0.5f));
            toastMsg.color = Color.white;
            toastMsg.fontStyle = FontStyles.Bold;
            toast.SetActive(false);

            // Traffic Control Panel (Bottom-Right)
            GameObject ctrlPanel = CreatePanel(hud.transform, "TrafficControlPanel", TrafficTownTheme.CardDarkColor, roundedSpr);
            SetR(ctrlPanel.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-35f, 30f), new Vector2(400f, 410f));
            AddShadow(ctrlPanel);

            TMP_Text pTitle = CreateText(ctrlPanel.transform, "Title", "TRAFFIC CONTROL", 16, TextAlignmentOptions.Left, new Vector2(220f, 26f), new Vector2(20f, -16f), new Vector2(0f, 1f));
            pTitle.color = TrafficTownTheme.AccentColor;
            pTitle.fontStyle = FontStyles.Bold;

            TMP_Text timerLbl = CreateText(ctrlPanel.transform, "PhaseTimer", "ACTIVE (18s)", 13, TextAlignmentOptions.Right, new Vector2(140f, 24f), new Vector2(-20f, -16f), new Vector2(1f, 1f));
            timerLbl.color = Color.white;
            timerLbl.fontStyle = FontStyles.Bold;

            TMP_Text nsStatus = CreateText(ctrlPanel.transform, "NSStatus", "NORTH / SOUTH:   [GREEN]", 14, TextAlignmentOptions.Left, new Vector2(360f, 24f), new Vector2(20f, -50f), new Vector2(0f, 1f));
            nsStatus.color = new Color(0.35f, 0.95f, 0.45f, 1f);

            TMP_Text ewStatus = CreateText(ctrlPanel.transform, "EWStatus", "EAST / WEST:          [RED]", 14, TextAlignmentOptions.Left, new Vector2(360f, 24f), new Vector2(20f, -76f), new Vector2(0f, 1f));
            ewStatus.color = new Color(1f, 0.35f, 0.35f, 1f);

            TMP_Text pedStatus = CreateText(ctrlPanel.transform, "PedStatus", "PEDESTRIANS:       [DON'T WALK]", 14, TextAlignmentOptions.Left, new Vector2(360f, 24f), new Vector2(20f, -102f), new Vector2(0f, 1f));
            pedStatus.color = new Color(1f, 0.35f, 0.35f, 1f);

            TMP_Text emgStatus = CreateText(ctrlPanel.transform, "EmgStatus", "EMERGENCY: None", 14, TextAlignmentOptions.Left, new Vector2(360f, 24f), new Vector2(20f, -128f), new Vector2(0f, 1f));
            emgStatus.color = new Color(0.6f, 0.85f, 0.7f, 1f);

            TMP_Text vehQueueText = CreateText(ctrlPanel.transform, "VehiclesWaiting", "Cars Waiting: 0", 12, TextAlignmentOptions.Left, new Vector2(175f, 22f), new Vector2(20f, -158f), new Vector2(0f, 1f));
            vehQueueText.color = TrafficTownTheme.TextSecondaryColor;

            TMP_Text pedQueueText = CreateText(ctrlPanel.transform, "PedestriansWaiting", "Peds Waiting: 0", 12, TextAlignmentOptions.Left, new Vector2(175f, 22f), new Vector2(205f, -158f), new Vector2(0f, 1f));
            pedQueueText.color = TrafficTownTheme.TextSecondaryColor;

            Button changePhaseBtn = BuildButton(ctrlPanel.transform, "ChangePhaseBtn", "CHANGE VEHICLE PHASE [Space]", new Vector2(0f, 78f), new Vector2(360f, 48f), TrafficTownTheme.ButtonPrimaryColor, roundedSpr);
            Button pedWalkBtn = BuildButton(ctrlPanel.transform, "PedWalkBtn", "PEDESTRIAN CROSSING [P]", new Vector2(0f, 20f), new Vector2(360f, 48f), new Color(0.18f, 0.65f, 0.38f, 1f), roundedSpr);

            // Pause Modal
            GameObject pauseObj = CreatePanel(canvas.transform, "PausePanel", new Color(0.05f, 0.08f, 0.12f, 0.94f), roundedSpr);
            SetR(pauseObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 300f));
            AddShadow(pauseObj);

            TMP_Text pauseTitle = CreateText(pauseObj.transform, "PauseTitle", "GAME PAUSED", 24, TextAlignmentOptions.Center, new Vector2(300f, 40f), new Vector2(0f, -20f), new Vector2(0.5f, 1f));
            pauseTitle.color = Color.white;
            pauseTitle.fontStyle = FontStyles.Bold;

            Button resumeBtn = BuildButton(pauseObj.transform, "ResumeBtn", "RESUME", new Vector2(0f, 140f), new Vector2(280f, 44f), TrafficTownTheme.ButtonPrimaryColor, roundedSpr);
            Button restartBtn = BuildButton(pauseObj.transform, "RestartBtn", "RESTART LEVEL", new Vector2(0f, 85f), new Vector2(280f, 44f), new Color(0.85f, 0.55f, 0.15f, 1f), roundedSpr);
            Button pauseMenuBtn = BuildButton(pauseObj.transform, "MenuBtn", "BACK TO MAIN MENU", new Vector2(0f, 30f), new Vector2(280f, 44f), new Color(0.75f, 0.25f, 0.25f, 1f), roundedSpr);
            pauseObj.SetActive(false);

            // Grand Finale Certificate Screen
            GameObject completeObj = CreatePanel(canvas.transform, "GrandFinaleCompletionPanel", new Color(0.06f, 0.09f, 0.14f, 0.98f), roundedSpr);
            SetFull(completeObj.GetComponent<RectTransform>());
            CanvasGroup compCg = completeObj.AddComponent<CanvasGroup>();

            GameObject certCard = CreatePanel(completeObj.transform, "CertificateCard", new Color(0.09f, 0.13f, 0.20f, 1f), roundedSpr);
            SetR(certCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 580f));
            AddShadow(certCard);

            Outline outline = certCard.AddComponent<Outline>();
            outline.effectColor = new Color(0.98f, 0.80f, 0.22f, 0.8f);
            outline.effectDistance = new Vector2(3f, 3f);

            TMP_Text compTitle = CreateText(certCard.transform, "Title", "🏆 TRAFFIC SAFETY MASTER 🏆", 28, TextAlignmentOptions.Center, new Vector2(700f, 44f), new Vector2(0f, -24f), new Vector2(0.5f, 1f));
            compTitle.color = TrafficTownTheme.AccentColor;
            compTitle.fontStyle = FontStyles.Bold;

            TMP_Text compSub = CreateText(certCard.transform, "Subtitle", "CONGRATULATIONS!\nYou have completed all 5 levels of TrafficTown 2D!", 15, TextAlignmentOptions.Center, new Vector2(680f, 44f), new Vector2(0f, -70f), new Vector2(0.5f, 1f));
            compSub.color = Color.white;

            // Checklist column
            GameObject checkCol = CreatePanel(certCard.transform, "ChecklistCol", new Color(0.12f, 0.17f, 0.26f, 0.8f), roundedSpr);
            SetR(checkCol.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 25f), new Vector2(330f, 260f));

            TMP_Text checkHeader = CreateText(checkCol.transform, "Header", "LEVELS COMPLETED", 13, TextAlignmentOptions.Left, new Vector2(300f, 24f), new Vector2(16f, -12f), new Vector2(0f, 1f));
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
                TMP_Text item = CreateText(checkCol.transform, $"Level_{i + 1}", levels[i], 12, TextAlignmentOptions.Left, new Vector2(300f, 22f), new Vector2(16f, -44f - i * 36f), new Vector2(0f, 1f));
                item.color = new Color(0.35f, 0.90f, 0.45f, 1f);
                item.fontStyle = FontStyles.Bold;
            }

            // Stats column
            GameObject statsCol = CreatePanel(certCard.transform, "StatsCol", new Color(0.12f, 0.17f, 0.26f, 0.8f), roundedSpr);
            SetR(statsCol.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, 25f), new Vector2(330f, 280f));

            TMP_Text statsHeader = CreateText(statsCol.transform, "Header", "FINAL PERFORMANCE", 13, TextAlignmentOptions.Left, new Vector2(300f, 24f), new Vector2(16f, -12f), new Vector2(0f, 1f));
            statsHeader.color = TrafficTownTheme.AccentColor;
            statsHeader.fontStyle = FontStyles.Bold;

            TMP_Text scoreStatVal = CreateStatRow(statsCol.transform, "ScoreRow", "Total Score", "2,450", -40f);
            TMP_Text safetyStatVal = CreateStatRow(statsCol.transform, "SafetyRow", "Safety Score", "95%", -76f);
            TMP_Text flowStatVal = CreateStatRow(statsCol.transform, "FlowRow", "Traffic Management", "88%", -112f);
            TMP_Text pedStatVal = CreateStatRow(statsCol.transform, "PedRow", "Pedestrians Safely Crossed", "18", -148f);
            TMP_Text emgStatVal = CreateStatRow(statsCol.transform, "EmgRow", "Emergencies Cleared", "4", -184f);
            TMP_Text collisionStatVal = CreateStatRow(statsCol.transform, "CollisionRow", "Traffic Collisions", "0", -220f);

            Button playAgainBtn = BuildButton(certCard.transform, "PlayAgainBtn", "PLAY LEVEL 5 AGAIN", new Vector2(-155f, 40f), new Vector2(280f, 50f), TrafficTownTheme.ButtonPrimaryColor, roundedSpr);
            Button menuBtn = BuildButton(certCard.transform, "MenuBtn", "BACK TO MAIN MENU", new Vector2(155f, 40f), new Vector2(280f, 50f), TrafficTownTheme.ButtonSuccessColor, roundedSpr);

            completeObj.SetActive(false);

            // Wire Controller
            Level5UIController ctrl = canvas.GetComponent<Level5UIController>();
            if (ctrl == null) ctrl = canvas.gameObject.AddComponent<Level5UIController>();
            SetRef(ctrl, "missionBadgeText", badgeText);
            SetRef(ctrl, "missionTitleText", titleText);
            SetRef(ctrl, "missionObjectiveText", objText);
            SetRef(ctrl, "missionTimerText", timerText);

            SetRef(ctrl, "scoreValueText", sVal);
            SetRef(ctrl, "safetyValueText", safVal);
            SetRef(ctrl, "safetyMeterFill", safFill);
            SetRef(ctrl, "trafficFlowValueText", fVal);
            SetRef(ctrl, "trafficFlowMeterFill", fFill);

            SetRef(ctrl, "feedbackBanner", toast);
            SetRef(ctrl, "feedbackGroup", toastCg);
            SetRef(ctrl, "feedbackMessageText", toastMsg);

            SetRef(ctrl, "northSouthStatusText", nsStatus);
            SetRef(ctrl, "eastWestStatusText", ewStatus);
            SetRef(ctrl, "pedestrianStatusText", pedStatus);
            SetRef(ctrl, "emergencyStatusText", emgStatus);
            SetRef(ctrl, "phaseTimerText", timerLbl);
            SetRef(ctrl, "vehiclesWaitingText", vehQueueText);
            SetRef(ctrl, "pedestriansWaitingText", pedQueueText);
            SetRef(ctrl, "changePhaseButton", changePhaseBtn);
            SetRef(ctrl, "pedestrianWalkButton", pedWalkBtn);

            SetRef(ctrl, "pausePanel", pauseObj);
            SetRef(ctrl, "resumeButton", resumeBtn);
            SetRef(ctrl, "restartButton", restartBtn);
            SetRef(ctrl, "pauseMenuButton", pauseMenuBtn);

            SetRef(ctrl, "completionPanel", completeObj);
            SetRef(ctrl, "completionGroup", compCg);
            SetRef(ctrl, "completionTitleText", compTitle);
            SetRef(ctrl, "completionSubtitleText", compSub);
            SetRef(ctrl, "finalScoreText", scoreStatVal);
            SetRef(ctrl, "safetyScoreText", safetyStatVal);
            SetRef(ctrl, "flowScoreText", flowStatVal);
            SetRef(ctrl, "pedestriansCrossedText", pedStatVal);
            SetRef(ctrl, "emergenciesClearedText", emgStatVal);
            SetRef(ctrl, "collisionsText", collisionStatVal);
            SetRef(ctrl, "playAgainButton", playAgainBtn);
            SetRef(ctrl, "backToMenuButton", menuBtn);

            ctrl.InitializeButtons();

            return ctrl;
        }

        private static TMP_Text CreateStatRow(Transform parent, string name, string label, string valStr, float y)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(parent, false);
            SetR(row.AddComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 26f));

            TMP_Text lbl = CreateText(row.transform, "Label", label, 12, TextAlignmentOptions.Left, new Vector2(200f, 24f), new Vector2(16f, 0f), new Vector2(0f, 0.5f));
            lbl.color = TrafficTownTheme.TextSecondaryColor;

            TMP_Text val = CreateText(row.transform, "Val", valStr, 13, TextAlignmentOptions.Right, new Vector2(100f, 24f), new Vector2(-16f, 0f), new Vector2(1f, 0.5f));
            val.color = Color.white;
            val.fontStyle = FontStyles.Bold;

            return val;
        }

        private static Button BuildButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color col, Sprite spr)
        {
            GameObject btnObj = CreatePanel(parent, name, col, spr);
            SetR(btnObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), pos, size);
            Image img = btnObj.GetComponent<Image>();
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;

            TMP_Text txt = CreateText(btnObj.transform, "Label", label, 12, TextAlignmentOptions.Center, new Vector2(size.x - 10f, size.y - 6f), Vector2.zero, new Vector2(0.5f, 0.5f));
            txt.color = Color.white;
            txt.fontStyle = FontStyles.Bold;

            return btn;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color, Sprite spr = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            if (spr != null)
            {
                img.sprite = spr;
                img.type = Image.Type.Sliced;
            }
            return go;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align, Vector2 size, Vector2 pos, Vector2 pivot, Vector2? anchor = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
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
            rt.anchoredPosition = pos;

            return tmp;
        }

        private static void AddShadow(GameObject obj)
        {
            Shadow s = obj.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.45f);
            s.effectDistance = new Vector2(2f, -3f);
        }

        private static void SetFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetR(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static GameObject CreateSprite(Transform parent, string name, Vector3 pos, Vector3 scale, Color col, int sortingOrder, bool isCircle = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = isCircle ? GetOrCreateCircle() : GetOrCreateSquare();
            sr.color = col;
            sr.sortingOrder = sortingOrder;

            return go;
        }

        private static void CreateCrosswalk(Transform parent, string name, Vector3 pos, Vector3 scale, Sprite spr, Color col)
        {
            GameObject cw = new GameObject(name);
            cw.transform.SetParent(parent, false);
            cw.transform.position = pos;
            cw.transform.localScale = scale;
            SpriteRenderer sr = cw.AddComponent<SpriteRenderer>();
            sr.sprite = spr != null ? spr : GetOrCreateSquare();
            sr.color = col;
            sr.sortingOrder = -7;
        }

        private static void CreateTree(Transform parent, string name, Vector3 pos, float scale)
        {
            GameObject tree = new GameObject(name);
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;
            tree.transform.localScale = new Vector3(scale, scale, 1f);

            CreateSprite(tree.transform, "Shadow", new Vector3(0.1f, -0.1f, 0f), Vector3.one * 1.05f, new Color(0.1f, 0.2f, 0.1f, 0.35f), -6, true);
            CreateSprite(tree.transform, "FoliageOuter", Vector3.zero, Vector3.one, new Color(0.22f, 0.52f, 0.25f, 1f), -5, true);
            CreateSprite(tree.transform, "FoliageInner", new Vector3(-0.05f, 0.05f, 0f), Vector3.one * 0.72f, new Color(0.32f, 0.65f, 0.35f, 1f), -4, true);
        }

        private static Sprite squareSpriteCache;
        private static Sprite circleSpriteCache;

        private static Sprite GetOrCreateSquare()
        {
            if (squareSpriteCache == null)
            {
                squareSpriteCache = LoadSprite("WorldSquare");
                if (squareSpriteCache == null)
                {
                    Texture2D tex = new Texture2D(2, 2);
                    tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
                    tex.Apply();
                    squareSpriteCache = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f);
                }
            }
            return squareSpriteCache;
        }

        private static Sprite GetOrCreateCircle()
        {
            if (circleSpriteCache == null)
            {
                circleSpriteCache = LoadSprite("WorldCircle");
                if (circleSpriteCache == null)
                {
                    int res = 32;
                    Texture2D tex = new Texture2D(res, res);
                    for (int y = 0; y < res; y++)
                    {
                        for (int x = 0; x < res; x++)
                        {
                            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(res / 2f, res / 2f));
                            tex.SetPixel(x, y, dist <= (res / 2f) ? Color.white : Color.clear);
                        }
                    }
                    tex.Apply();
                    circleSpriteCache = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 1f);
                }
            }
            return circleSpriteCache;
        }

        private static Sprite LoadSprite(string name)
        {
            Sprite s = Resources.Load<Sprite>("Level5/" + name);
            if (s != null) return s;
            s = Resources.Load<Sprite>(name);
            return s;
        }

        private static void SetRef(object target, string fieldName, object value)
        {
            if (target == null) return;
            FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fi != null) fi.SetValue(target, value);
        }
    }
}
