using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using TrafficTown2D.Core;
using TrafficTown2D.Gameplay;

namespace TrafficTown2D.Level7
{
    public class Level7Bootstrap : MonoBehaviour
    {
        private const string WorldSquareSpritePath = "Assets/Sprites/Generated/WorldSquare.png";
        private const string WorldCircleSpritePath = "Assets/Sprites/Generated/WorldCircle.png";
        private const string CarBlueTopDownPath = "Assets/Sprites/Vehicles/CarBlueTopDown.png";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == "Level7")
            {
                EnsureLevel7Scene();
            }
        }

        private void Awake()
        {
            EnsureLevel7Scene();
        }

        public static void EnsureLevel7Scene()
        {
            Camera cam = EnsureCamera();
            EnsureGlobalLight();
            EnsureEventSystem();

            GameObject env = GameObject.Find("Environment");
            Level7PlayerCar existingCar = UnityEngine.Object.FindFirstObjectByType<Level7PlayerCar>();

            // Detect legacy scene objects and self-heal automatically
            bool isOutdated = false;
            GameObject existingCanvas = GameObject.Find("Level7_Canvas");
            if (env != null)
            {
                if (env.transform.Find("CityBuildingsAndTrees") != null || env.transform.Find("RoadNetwork/Corner_NW_Pad") == null)
                {
                    isOutdated = true;
                }
            }
            if (existingCanvas != null && existingCanvas.transform.Find("CompletionOverlay") == null)
            {
                isOutdated = true;
            }

            if (isOutdated)
            {
                Debug.Log("[Level7Bootstrap] Outdated Level 7 environment detected. Purging and rebuilding with clean road network and scenery...");
                if (env != null) UnityEngine.Object.DestroyImmediate(env);
                GameObject oldCtrl = GameObject.Find("GameplayControllers");
                if (oldCtrl != null) UnityEngine.Object.DestroyImmediate(oldCtrl);
                GameObject oldCanvas = GameObject.Find("Level7_Canvas");
                if (oldCanvas != null) UnityEngine.Object.DestroyImmediate(oldCanvas);
                GameObject oldCar = GameObject.Find("PlayerCar");
                if (oldCar != null) UnityEngine.Object.DestroyImmediate(oldCar);
                env = null;
                existingCar = null;
            }

            if (env != null && existingCar != null && Level7MissionManager.Instance != null)
            {
                Level7CameraFollow cf = cam.GetComponent<Level7CameraFollow>();
                if (cf == null) cf = cam.gameObject.AddComponent<Level7CameraFollow>();
                cf.SetTarget(existingCar.transform);
                return;
            }

            Debug.Log("[Level7Bootstrap] Bootstrapping Level 7 environment, road network, and systems...");

            // 1. Core Services
            GameObject services = FindOrCreate("Services");
            GetOrAdd<SceneLoader>(services);
            GameManager gm = GetOrAdd<GameManager>(services);
            SetField(gm, "startingState", GameState.Playing);
            gm.SetState(GameState.Playing);
            GetOrAdd<ScoreManager>(services);
            Time.timeScale = 1f;

            // 2. Environment
            if (env == null) env = new GameObject("Environment");
            CreateRoadNetwork(env.transform);
            CreateEnvironmentDecorations(env.transform);

            // 3. Traffic Lights & Pedestrian
            CreateTrafficLightsAndCrossings(env.transform);

            // 4. Safe Stop Zones
            CreateSafeStopZones(env.transform);

            // 5. Destination Marker
            GameObject destMarker = CreateDestinationMarker(env.transform);

            // 6. Player Car
            GameObject playerCarObj = CreatePlayerCar(env.transform);
            Level7PlayerCar playerCar = playerCarObj.GetComponent<Level7PlayerCar>();

            // 7. Ambient Traffic & Ambulance
            CreateTrafficAndAmbulance(env.transform, playerCar);

            // 8. Bind Camera Follow
            Level7CameraFollow camFollow = cam.GetComponent<Level7CameraFollow>();
            if (camFollow == null) camFollow = cam.gameObject.AddComponent<Level7CameraFollow>();
            camFollow.SetTarget(playerCar.transform);

            // 9. Controllers & Managers
            GameObject controllers = FindOrCreate("GameplayControllers");
            Level7FocusManager focusMgr = GetOrAdd<Level7FocusManager>(controllers);
            Level7DistractionManager distMgr = GetOrAdd<Level7DistractionManager>(controllers);

            // 10. UI Canvas
            Level7UIController uiCtrl = CreateUIHierarchy(playerCar);

            // Set references on DistractionManager
            SetField(distMgr, "distractionUI", uiCtrl.DistractionUI);
            SetField(distMgr, "playerCar", playerCar);

            // 11. Mission Manager
            Level7MissionManager missionMgr = GetOrAdd<Level7MissionManager>(controllers);
            SetField(missionMgr, "playerCar", playerCar);
            SetField(missionMgr, "distractionManager", distMgr);
            SetField(missionMgr, "uiController", uiCtrl);
            SetField(missionMgr, "destinationMarker", destMarker.transform);

            Level7Ambulance amb = UnityEngine.Object.FindFirstObjectByType<Level7Ambulance>();
            if (amb != null) SetField(missionMgr, "ambulance", amb);

            Level7Pedestrian ped = UnityEngine.Object.FindFirstObjectByType<Level7Pedestrian>();
            if (ped != null) SetField(missionMgr, "pedestrian", ped);

            Debug.Log("[Level7Bootstrap] Level 7 Bootstrap completed successfully.");
        }

        #region Camera & Lighting
        private static Camera EnsureCamera()
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
            cam.orthographicSize = 8.5f;
            cam.transform.position = new Vector3(-25f, -20f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.24f, 0.42f, 0.28f, 1f); // Pleasant city park ground green

            UniversalAdditionalCameraData camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderType = CameraRenderType.Base;
            camData.renderPostProcessing = false;

            return cam;
        }

        private static void EnsureGlobalLight()
        {
            Light2D light = UnityEngine.Object.FindFirstObjectByType<Light2D>();
            if (light == null)
            {
                GameObject lightObj = new GameObject("GlobalLight2D");
                light = lightObj.AddComponent<Light2D>();
            }
            light.lightType = Light2D.LightType.Global;
            light.color = new Color(1f, 0.98f, 0.95f, 1f);
            light.intensity = 1.0f;
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
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

        #region Road Network Construction
        private static void CreateRoadNetwork(Transform env)
        {
            Color roadCol = new Color(0.15f, 0.16f, 0.18f, 1f); // Rich dark asphalt
            Color curbCol = new Color(0.72f, 0.75f, 0.78f, 1f); // Raised concrete curbs
            Color yellowLineCol = new Color(1f, 0.88f, 0.20f, 0.95f); // Double yellow centerline
            Color grassCol = new Color(0.24f, 0.44f, 0.28f, 1f); // Park lawn

            // Ground base
            CreateWorldSprite(env, "GroundTerrain", Vector3.zero, new Vector3(180f, 130f, 1f), grassCol, -10);

            GameObject roads = FindOrCreateChild(env, "RoadNetwork");

            // 1. South Avenue (Horizontal: X=-35 to X=+35, Y=-20, width 7.2)
            CreateRoadSection(roads.transform, "Road_SouthAve_West", new Vector3(-17.5f, -20f, 0f), new Vector2(35f, 7.2f), roadCol, curbCol, yellowLineCol, true, true, true);
            CreateRoadSection(roads.transform, "Road_SouthAve_East", new Vector3(17.5f, -20f, 0f), new Vector2(35f, 7.2f), roadCol, curbCol, yellowLineCol, true, true, false); // South curb open for SafeStop 1

            // 2. East Boulevard (Vertical: Y=-20 to Y=+20, X=+35, width 7.2)
            CreateRoadSection(roads.transform, "Road_EastBlvd", new Vector3(35f, 0f, 0f), new Vector2(7.2f, 40f), roadCol, curbCol, yellowLineCol, false, true, true);

            // 3. North Avenue (Horizontal: X=-35 to X=+35, Y=+20, width 7.2)
            CreateRoadSection(roads.transform, "Road_NorthAve_West", new Vector3(-17.5f, 20f, 0f), new Vector2(35f, 7.2f), roadCol, curbCol, yellowLineCol, true, true, true);
            CreateRoadSection(roads.transform, "Road_NorthAve_East", new Vector3(17.5f, 20f, 0f), new Vector2(35f, 7.2f), roadCol, curbCol, yellowLineCol, true, true, true);

            // 4. Central Street (Vertical: Y=-20 to Y=+20, X=0, width 7.2)
            CreateRoadSection(roads.transform, "Road_CentralSt_South", new Vector3(0f, -10f, 0f), new Vector2(7.2f, 20f), roadCol, curbCol, yellowLineCol, false, true, true);
            CreateRoadSection(roads.transform, "Road_CentralSt_North", new Vector3(0f, 10f, 0f), new Vector2(7.2f, 20f), roadCol, curbCol, yellowLineCol, false, true, true);

            // 5. West Avenue (Vertical: Y=-20 to Y=+20, X=-35, width 7.2, length 40)
            CreateRoadSection(roads.transform, "Road_WestAve", new Vector3(-35f, 0f, 0f), new Vector2(7.2f, 40f), roadCol, curbCol, yellowLineCol, false, false, true); // West curb open for SafeStop 2

            // 6. Side Road (Horizontal: X=-35 to X=0, Y=0, width 7.2)
            CreateRoadSection(roads.transform, "Road_SideRoad", new Vector3(-17.5f, 0f, 0f), new Vector2(35f, 7.2f), roadCol, curbCol, yellowLineCol, true, true, true);

            // 7. Curved Park Way (from (-35, 5) curving gently to (-10, 20))
            CreateCurvedConnector(roads.transform, new Vector2(-35f, 5f), new Vector2(-10f, 20f), 7.2f, roadCol, curbCol, yellowLineCol);

            // 8. Junction Pads (Seamless asphalt junction squares with zero slicing curbs)
            CreateWorldSprite(roads.transform, "Junction_CentralS_Pad", new Vector3(0f, -20f, 0f), new Vector3(7.4f, 7.4f, 1f), roadCol, 2);
            CreateWorldSprite(roads.transform, "Junction_CentralN_Pad", new Vector3(0f, 20f, 0f), new Vector3(7.4f, 7.4f, 1f), roadCol, 2);
            CreateWorldSprite(roads.transform, "Junction_Center_Pad", new Vector3(0f, 0f, 0f), new Vector3(7.4f, 7.4f, 1f), roadCol, 2);
            CreateWorldSprite(roads.transform, "Junction_WestSide_Pad", new Vector3(-35f, 0f, 0f), new Vector3(7.4f, 7.4f, 1f), roadCol, 2);
            CreateWorldSprite(roads.transform, "Junction_CurveWest_Pad", new Vector3(-35f, 5f, 0f), new Vector3(7.4f, 7.4f, 1f), roadCol, 2);
            CreateWorldSprite(roads.transform, "Junction_CurveNorth_Pad", new Vector3(-10f, 20f, 0f), new Vector3(7.4f, 7.4f, 1f), roadCol, 2);
            CreateWorldSprite(roads.transform, "Corner_SE_Pad", new Vector3(35f, -20f, 0f), new Vector3(7.4f, 7.4f, 1f), roadCol, 2);
            CreateWorldSprite(roads.transform, "Corner_NE_Pad", new Vector3(35f, 20f, 0f), new Vector3(7.4f, 7.4f, 1f), roadCol, 2);
            CreateWorldSprite(roads.transform, "Corner_SW_Pad", new Vector3(-35f, -20f, 0f), new Vector3(7.4f, 7.4f, 1f), roadCol, 2);
            CreateWorldSprite(roads.transform, "Corner_NW_Pad", new Vector3(-35f, 20f, 0f), new Vector3(7.4f, 7.4f, 1f), roadCol, 2);

            // Outer corner curbs
            CreateWorldSprite(roads.transform, "Corner_SE_CurbS", new Vector3(35f, -23.78f, 0f), new Vector3(7.4f, 0.35f, 1f), curbCol, 3);
            CreateWorldSprite(roads.transform, "Corner_SE_CurbE", new Vector3(38.78f, -20f, 0f), new Vector3(0.35f, 7.4f, 1f), curbCol, 3);

            CreateWorldSprite(roads.transform, "Corner_NE_CurbN", new Vector3(35f, 23.78f, 0f), new Vector3(7.4f, 0.35f, 1f), curbCol, 3);
            CreateWorldSprite(roads.transform, "Corner_NE_CurbE", new Vector3(38.78f, 20f, 0f), new Vector3(0.35f, 7.4f, 1f), curbCol, 3);

            CreateWorldSprite(roads.transform, "Corner_SW_CurbS", new Vector3(-35f, -23.78f, 0f), new Vector3(7.4f, 0.35f, 1f), curbCol, 3);
            CreateWorldSprite(roads.transform, "Corner_SW_CurbW", new Vector3(-38.78f, -20f, 0f), new Vector3(0.35f, 7.4f, 1f), curbCol, 3);

            CreateWorldSprite(roads.transform, "Corner_NW_CurbN", new Vector3(-35f, 23.78f, 0f), new Vector3(7.4f, 0.35f, 1f), curbCol, 3);
            CreateWorldSprite(roads.transform, "Corner_NW_CurbW", new Vector3(-38.78f, 20f, 0f), new Vector3(0.35f, 7.4f, 1f), curbCol, 3);
        }

        private static void CreateRoadSection(Transform parent, string name, Vector3 pos, Vector2 size, Color roadCol, Color curbCol, Color lineCol, bool isHorizontal, bool hasCurb1, bool hasCurb2)
        {
            GameObject seg = FindOrCreateChild(parent, name);
            seg.transform.position = pos;

            // Road surface
            CreateWorldSprite(seg.transform, "Asphalt", Vector3.zero, new Vector3(size.x, size.y, 1f), roadCol, 0);

            // Centerline dashes
            if (isHorizontal)
            {
                int dashes = Mathf.FloorToInt(size.x / 4.0f);
                for (int i = 0; i < dashes; i++)
                {
                    float x = -size.x * 0.5f + (i + 0.5f) * 4.0f;
                    CreateWorldSprite(seg.transform, $"Dash_{i}", new Vector3(x, 0f, 0f), new Vector3(1.8f, 0.22f, 1f), lineCol, 1);
                }

                if (hasCurb1) CreateWorldSprite(seg.transform, "Curb_Top", new Vector3(0f, size.y * 0.5f + 0.17f, 0f), new Vector3(size.x, 0.35f, 1f), curbCol, 1);
                if (hasCurb2) CreateWorldSprite(seg.transform, "Curb_Bottom", new Vector3(0f, -size.y * 0.5f - 0.17f, 0f), new Vector3(size.x, 0.35f, 1f), curbCol, 1);
            }
            else
            {
                int dashes = Mathf.FloorToInt(size.y / 4.0f);
                for (int i = 0; i < dashes; i++)
                {
                    float y = -size.y * 0.5f + (i + 0.5f) * 4.0f;
                    CreateWorldSprite(seg.transform, $"Dash_{i}", new Vector3(0f, y, 0f), new Vector3(0.22f, 1.8f, 1f), lineCol, 1);
                }

                if (hasCurb1) CreateWorldSprite(seg.transform, "Curb_Left", new Vector3(-size.x * 0.5f - 0.17f, 0f, 0f), new Vector3(0.35f, size.y, 1f), curbCol, 1);
                if (hasCurb2) CreateWorldSprite(seg.transform, "Curb_Right", new Vector3(size.x * 0.5f + 0.17f, 0f, 0f), new Vector3(0.35f, size.y, 1f), curbCol, 1);
            }
        }

        private static void CreateCurvedConnector(Transform parent, Vector2 start, Vector2 end, float width, Color roadCol, Color curbCol, Color lineCol)
        {
            GameObject curveObj = FindOrCreateChild(parent, "Curved_Park_Way");
            curveObj.transform.position = Vector3.zero;

            int steps = 18;
            Vector2 p0 = start;
            Vector2 p1 = new Vector2(start.x + 4f, end.y - 4f);
            Vector2 p2 = end;

            Vector2 prev = p0;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                // Quadratic bezier curve
                Vector2 curr = Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;

                Vector2 mid = (prev + curr) * 0.5f;
                Vector2 dir = curr - prev;
                float len = dir.magnitude;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                GameObject piece = FindOrCreateChild(curveObj.transform, $"CurveSeg_{i}");
                piece.transform.position = new Vector3(mid.x, mid.y, 0f);
                piece.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                piece.transform.localScale = Vector3.one;

                CreateWorldSprite(piece.transform, "Asphalt", Vector3.zero, new Vector3(len + 0.45f, width, 1f), roadCol, 0);
                CreateWorldSprite(piece.transform, "Curb_Top", new Vector3(0f, (width * 0.5f) + 0.17f, 0f), new Vector3(len + 0.45f, 0.35f, 1f), curbCol, 1);
                CreateWorldSprite(piece.transform, "Curb_Bottom", new Vector3(0f, -(width * 0.5f) - 0.17f, 0f), new Vector3(len + 0.45f, 0.35f, 1f), curbCol, 1);
                CreateWorldSprite(piece.transform, "CenterDash", Vector3.zero, new Vector3(len * 0.6f, 0.18f, 1f), lineCol, 2);

                prev = curr;
            }
        }
        #endregion

        #region Safe Stop Zones
        private static void CreateSafeStopZones(Transform env)
        {
            GameObject zones = FindOrCreateChild(env, "SafeStopZones");
            Color bayGreen = new Color(0.18f, 0.65f, 0.32f, 0.65f);
            Color borderGreen = new Color(0.12f, 0.85f, 0.36f, 0.95f);

            // Safe Stop Zone 1: Pull-over on South Ave at (15, -24.5), size 10m x 4.2m
            GameObject zone1 = FindOrCreateChild(zones.transform, "SafeStopZone_1_SouthAve");
            zone1.transform.position = new Vector3(15f, -24.5f, 0f);

            BoxCollider2D col1 = GetOrAdd<BoxCollider2D>(zone1);
            col1.size = new Vector2(10f, 4.2f);
            col1.isTrigger = true;

            Level7SafeStopZone ss1 = GetOrAdd<Level7SafeStopZone>(zone1);
            SetField(ss1, "zoneName", "South Avenue Pull-Over Bay");

            // Bay paint
            GameObject bay1Visual = CreateWorldSprite(zone1.transform, "BayVisual", Vector3.zero, new Vector3(10f, 4.2f, 1f), bayGreen, 1);
            SetField(ss1, "bayRenderer", bay1Visual.GetComponent<SpriteRenderer>());

            // Markings & Sign
            CreateWorldSprite(zone1.transform, "BorderN", new Vector3(0f, 2.05f, 0f), new Vector3(10f, 0.25f, 1f), borderGreen, 2);
            CreateWorldSprite(zone1.transform, "BorderS", new Vector3(0f, -2.05f, 0f), new Vector3(10f, 0.25f, 1f), borderGreen, 2);
            CreateWorldSprite(zone1.transform, "BorderE", new Vector3(4.9f, 0f, 0f), new Vector3(0.25f, 4.2f, 1f), borderGreen, 2);
            CreateWorldSprite(zone1.transform, "BorderW", new Vector3(-4.9f, 0f, 0f), new Vector3(0.25f, 4.2f, 1f), borderGreen, 2);

            // Safe Stop Zone 2: Pull-over on West Ave at (-39.5, -10), size 4.2m x 10m
            GameObject zone2 = FindOrCreateChild(zones.transform, "SafeStopZone_2_WestAve");
            zone2.transform.position = new Vector3(-39.5f, -10f, 0f);

            BoxCollider2D col2 = GetOrAdd<BoxCollider2D>(zone2);
            col2.size = new Vector2(4.2f, 10f);
            col2.isTrigger = true;

            Level7SafeStopZone ss2 = GetOrAdd<Level7SafeStopZone>(zone2);
            SetField(ss2, "zoneName", "West Avenue Pull-Over Bay");

            GameObject bay2Visual = CreateWorldSprite(zone2.transform, "BayVisual", Vector3.zero, new Vector3(4.2f, 10f, 1f), bayGreen, 1);
            SetField(ss2, "bayRenderer", bay2Visual.GetComponent<SpriteRenderer>());

            CreateWorldSprite(zone2.transform, "BorderW", new Vector3(-2.05f, 0f, 0f), new Vector3(0.25f, 10f, 1f), borderGreen, 2);
            CreateWorldSprite(zone2.transform, "BorderE", new Vector3(2.05f, 0f, 0f), new Vector3(0.25f, 10f, 1f), borderGreen, 2);
            CreateWorldSprite(zone2.transform, "BorderN", new Vector3(0f, 4.9f, 0f), new Vector3(4.2f, 0.25f, 1f), borderGreen, 2);
            CreateWorldSprite(zone2.transform, "BorderS", new Vector3(0f, -4.9f, 0f), new Vector3(4.2f, 0.25f, 1f), borderGreen, 2);
        }
        #endregion

        #region Traffic Signals & Pedestrian Crossings
        private static void CreateTrafficLightsAndCrossings(Transform env)
        {
            GameObject container = FindOrCreateChild(env, "TrafficSignalsAndCrossings");

            // Signal 1 at Intersection 1: (4.8, -16) controlling South Ave / Central St
            CreateSignalPost(container.transform, "TrafficSignal_1", new Vector3(4.8f, -16f, 0f), new Vector3(0f, -16f, 0f));

            // Signal 2 at Intersection 2: (4.8, 24) controlling North Ave / Central St
            CreateSignalPost(container.transform, "TrafficSignal_2", new Vector3(4.8f, 24f, 0f), new Vector3(0f, 20f, 0f));

            // Zebra Crossing on Central Street at (0, -8)
            GameObject crossing = FindOrCreateChild(container.transform, "ZebraCrossing");
            crossing.transform.position = new Vector3(0f, -8f, 0f);

            Color stripeCol = new Color(0.95f, 0.95f, 0.95f, 0.92f);
            for (int i = -3; i <= 3; i++)
            {
                CreateWorldSprite(crossing.transform, $"Stripe_{i}", new Vector3(i * 0.95f, 0f, 0f), new Vector3(0.55f, 2.8f, 1f), stripeCol, 2);
            }

            // Stop line
            CreateWorldSprite(crossing.transform, "StopLine_S", new Vector3(0f, -1.8f, 0f), new Vector3(6.8f, 0.35f, 1f), Color.white, 2);
            CreateWorldSprite(crossing.transform, "StopLine_N", new Vector3(0f, 1.8f, 0f), new Vector3(6.8f, 0.35f, 1f), Color.white, 2);

            // Pedestrian object
            GameObject pedObj = FindOrCreateChild(container.transform, "Pedestrian_CentralSt");
            pedObj.transform.position = new Vector3(-4.5f, -8f, 0f);

            Level7Pedestrian ped = GetOrAdd<Level7Pedestrian>(pedObj);
            SetField(ped, "startPosition", new Vector3(-4.5f, -8f, 0f));
            SetField(ped, "targetPosition", new Vector3(4.5f, -8f, 0f));

            GameObject visualRoot = FindOrCreateChild(pedObj.transform, "Visual");
            SetField(ped, "visualRoot", visualRoot.transform);

            // Pedestrian body (shirt + head)
            CreateWorldSprite(visualRoot.transform, "Body", Vector3.zero, new Vector3(0.55f, 0.7f, 1f), new Color(0.22f, 0.55f, 0.85f, 1f), 8);
            CreateWorldSprite(visualRoot.transform, "Head", new Vector3(0f, 0.45f, 0f), new Vector3(0.45f, 0.45f, 1f), new Color(0.95f, 0.82f, 0.70f, 1f), 9, isCircle: true);
        }

        private static GameObject CreateSignalPost(Transform parent, string name, Vector3 pos, Vector3 stopLinePos)
        {
            GameObject post = FindOrCreateChild(parent, name);
            post.transform.position = pos;

            // Pole
            CreateWorldSprite(post.transform, "Pole", new Vector3(0f, -0.6f, 0f), new Vector3(0.18f, 1.2f, 1f), new Color(0.25f, 0.28f, 0.32f, 1f), 5);
            // Housing
            CreateWorldSprite(post.transform, "Housing", Vector3.zero, new Vector3(0.85f, 1.9f, 1f), new Color(0.12f, 0.14f, 0.16f, 1f), 6);

            // Red, Yellow, Green Lenses
            GameObject red = CreateWorldSprite(post.transform, "RedLens", new Vector3(0f, 0.55f, 0f), new Vector3(0.42f, 0.42f, 1f), Color.red, 7, isCircle: true);
            GameObject yellow = CreateWorldSprite(post.transform, "YellowLens", new Vector3(0f, 0f, 0f), new Vector3(0.42f, 0.42f, 1f), Color.yellow, 7, isCircle: true);
            GameObject green = CreateWorldSprite(post.transform, "GreenLens", new Vector3(0f, -0.55f, 0f), new Vector3(0.42f, 0.42f, 1f), Color.green, 7, isCircle: true);

            // Stop line detector
            GameObject stopObj = FindOrCreateChild(post.transform, "StopLineDetector");
            stopObj.transform.position = stopLinePos;

            Level7TrafficLight light = GetOrAdd<Level7TrafficLight>(post);
            SetField(light, "redLens", red.GetComponent<SpriteRenderer>());
            SetField(light, "yellowLens", yellow.GetComponent<SpriteRenderer>());
            SetField(light, "greenLens", green.GetComponent<SpriteRenderer>());
            SetField(light, "stopLineTransform", stopObj.transform);

            return post;
        }
        #endregion

        #region Player Car & Vehicles
        private static GameObject CreatePlayerCar(Transform env)
        {
            GameObject car = FindOrCreateChild(env, "PlayerCar");
            car.transform.position = new Vector3(-25f, -20f, 0f);
            car.transform.rotation = Quaternion.Euler(0f, 0f, -90f); // Facing East on South Ave
            car.tag = "Player";

            Sprite carSprite = LoadSprite(CarBlueTopDownPath);
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

            Level7PlayerCar playerCar = GetOrAdd<Level7PlayerCar>(car);

            // Brake lights
            SpriteRenderer blL = CreateBrakeLight(car.transform, "Brake_L", new Vector3(-0.65f, -1.6f, 0f));
            SpriteRenderer blR = CreateBrakeLight(car.transform, "Brake_R", new Vector3(0.65f, -1.6f, 0f));
            SetField(playerCar, "brakeLights", new[] { blL, blR });

            return car;
        }

        private static SpriteRenderer CreateBrakeLight(Transform parent, string name, Vector3 pos)
        {
            GameObject bl = FindOrCreateChild(parent, name);
            bl.transform.localPosition = pos;
            bl.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(bl);
            sr.sprite = LoadSprite(WorldCircleSpritePath);
            sr.color = new Color(0.6f, 0.1f, 0.1f, 0.6f);
            sr.sortingOrder = 12;
            return sr;
        }

        private static void CreateTrafficAndAmbulance(Transform env, Level7PlayerCar player)
        {
            GameObject trafficRoot = FindOrCreateChild(env, "TrafficVehicles");

            // Ambient Car 1 on East Blvd
            GameObject car1 = FindOrCreateChild(trafficRoot.transform, "TrafficCar_1");
            car1.transform.position = new Vector3(35f, 18f, 0f);
            CreateWorldSprite(car1.transform, "Body", Vector3.zero, new Vector3(1.1f, 2.2f, 1f), new Color(0.85f, 0.35f, 0.25f, 1f), 8);
            Level7TrafficVehicle tv1 = GetOrAdd<Level7TrafficVehicle>(car1);
            tv1.SetWaypoints(new Vector3[] {
                new Vector3(35f, -18f, 0f),
                new Vector3(35f, 18f, 0f)
            }, loop: true);

            // Ambient Car 2 on North Ave
            GameObject car2 = FindOrCreateChild(trafficRoot.transform, "TrafficCar_2");
            car2.transform.position = new Vector3(30f, 20f, 0f);
            CreateWorldSprite(car2.transform, "Body", Vector3.zero, new Vector3(2.2f, 1.1f, 1f), new Color(0.25f, 0.65f, 0.45f, 1f), 8);
            Level7TrafficVehicle tv2 = GetOrAdd<Level7TrafficVehicle>(car2);
            tv2.SetWaypoints(new Vector3[] {
                new Vector3(-10f, 20f, 0f),
                new Vector3(30f, 20f, 0f)
            }, loop: true);

            // Ambulance (starts inactive, enabled in Mission 5)
            GameObject ambObj = FindOrCreateChild(trafficRoot.transform, "Ambulance");
            ambObj.transform.position = new Vector3(35f, 25f, 0f);

            CreateWorldSprite(ambObj.transform, "Body", Vector3.zero, new Vector3(1.3f, 2.6f, 1f), Color.white, 9);
            CreateWorldSprite(ambObj.transform, "StripeRed", Vector3.zero, new Vector3(1.35f, 0.5f, 1f), new Color(0.9f, 0.15f, 0.15f, 1f), 10);
            CreateWorldSprite(ambObj.transform, "CrossH", Vector3.zero, new Vector3(0.6f, 0.18f, 1f), Color.red, 11);
            CreateWorldSprite(ambObj.transform, "CrossV", Vector3.zero, new Vector3(0.18f, 0.6f, 1f), Color.red, 11);

            GameObject rStrobe = CreateWorldSprite(ambObj.transform, "RedStrobe", new Vector3(-0.35f, 0.8f, 0f), new Vector3(0.35f, 0.35f, 1f), Color.red, 12, isCircle: true);
            GameObject bStrobe = CreateWorldSprite(ambObj.transform, "BlueStrobe", new Vector3(0.35f, 0.8f, 0f), new Vector3(0.35f, 0.35f, 1f), Color.cyan, 12, isCircle: true);

            Level7Ambulance amb = GetOrAdd<Level7Ambulance>(ambObj);
            SetField(amb, "redStrobe", rStrobe.GetComponent<SpriteRenderer>());
            SetField(amb, "blueStrobe", bStrobe.GetComponent<SpriteRenderer>());
            ambObj.SetActive(false);
        }
        #endregion

        #region Destination Marker & Environment Props
        private static GameObject CreateDestinationMarker(Transform env)
        {
            GameObject dest = FindOrCreateChild(env, "DestinationMarker");
            dest.transform.position = new Vector3(25f, -20f, 0f);

            CreateWorldSprite(dest.transform, "HaloOuter", Vector3.zero, new Vector3(5.0f, 5.0f, 1f), new Color(0.18f, 0.72f, 0.95f, 0.45f), 4, isCircle: true);
            CreateWorldSprite(dest.transform, "HaloInner", Vector3.zero, new Vector3(2.8f, 2.8f, 1f), new Color(1f, 0.88f, 0.20f, 0.90f), 5, isCircle: true);
            GetOrAdd<Level7DestinationBeacon>(dest);

            return dest;
        }

        private static void CreateEnvironmentDecorations(Transform env)
        {
            GameObject dec = FindOrCreateChild(env, "CityParksAndDecorations");

            // Central Park Walking Paths (sand/gravel pathways through the green park between West Ave and Central St)
            Color pathCol = new Color(0.76f, 0.72f, 0.60f, 0.85f);
            CreateWorldSprite(dec.transform, "ParkPath_Main", new Vector3(-17.5f, 10f, 0f), new Vector3(20f, 1.8f, 1f), pathCol, -1);
            CreateWorldSprite(dec.transform, "ParkPath_Cross", new Vector3(-17.5f, 10f, 0f), new Vector3(1.8f, 12f, 1f), pathCol, -1);

            // Park Plaza Circle
            Color plazaCol = new Color(0.70f, 0.68f, 0.62f, 0.95f);
            CreateWorldSprite(dec.transform, "PlazaCircle", new Vector3(-17.5f, 10f, 0f), new Vector3(6f, 6f, 1f), plazaCol, 0, isCircle: true);
            CreateWorldSprite(dec.transform, "FountainRing", new Vector3(-17.5f, 10f, 0f), new Vector3(3.2f, 3.2f, 1f), new Color(0.20f, 0.60f, 0.85f, 0.90f), 1, isCircle: true);

            // South Park Plaza (between West Ave and Central St, south of Side Road)
            CreateWorldSprite(dec.transform, "SouthParkPath", new Vector3(-17.5f, -10f, 0f), new Vector3(18f, 1.8f, 1f), pathCol, -1);
            CreateWorldSprite(dec.transform, "SouthPlazaCircle", new Vector3(-17.5f, -10f, 0f), new Vector3(5f, 5f, 1f), plazaCol, 0, isCircle: true);

            // East Commercial Plazas (between Central St and East Blvd)
            CreateWorldSprite(dec.transform, "EastPlazaNorth", new Vector3(17.5f, 10f, 0f), new Vector3(16f, 10f, 1f), new Color(0.68f, 0.66f, 0.62f, 0.75f), -1);
            CreateWorldSprite(dec.transform, "EastPlazaSouth", new Vector3(17.5f, -10f, 0f), new Vector3(16f, 10f, 1f), new Color(0.68f, 0.66f, 0.62f, 0.75f), -1);

            // Park Trees (varied lush trees in clusters with nice layered canopies and soft shadows)
            Vector2[] treePositions = new Vector2[]
            {
                // North Park grove
                new Vector2(-25f, 14f), new Vector2(-22f, 16f), new Vector2(-12f, 15f), new Vector2(-10f, 13f),
                new Vector2(-24f, 6f), new Vector2(-11f, 6f), new Vector2(-26f, 10f),
                // South Park grove
                new Vector2(-26f, -6f), new Vector2(-23f, -14f), new Vector2(-12f, -6f), new Vector2(-11f, -15f),
                new Vector2(-26f, -10f), new Vector2(-9f, -10f),
                // East Avenue border trees
                new Vector2(10f, 14f), new Vector2(25f, 14f), new Vector2(10f, 6f), new Vector2(25f, 6f),
                new Vector2(10f, -6f), new Vector2(25f, -6f), new Vector2(10f, -14f), new Vector2(25f, -14f),
                // Outer perimeter trees
                new Vector2(-28f, -26f), new Vector2(-15f, -26f), new Vector2(25f, -26f),
                new Vector2(42f, -10f), new Vector2(42f, 0f), new Vector2(42f, 10f),
                new Vector2(25f, 26f), new Vector2(15f, 26f), new Vector2(0f, 26f), new Vector2(-25f, 26f)
            };

            for (int i = 0; i < treePositions.Length; i++)
            {
                Vector2 pos = treePositions[i];
                GameObject tree = FindOrCreateChild(dec.transform, $"Tree_{i}");
                tree.transform.position = (Vector3)pos;
                CreateWorldSprite(tree.transform, "Shadow", new Vector3(0.2f, -0.2f, 0f), new Vector3(2.6f, 2.6f, 1f), new Color(0f, 0f, 0f, 0.22f), 5, isCircle: true);
                CreateWorldSprite(tree.transform, "Canopy", Vector3.zero, new Vector3(2.5f, 2.5f, 1f), new Color(0.18f, 0.52f, 0.22f, 1f), 6, isCircle: true);
                CreateWorldSprite(tree.transform, "Core", new Vector3(-0.2f, 0.2f, 0f), new Vector3(1.5f, 1.5f, 1f), new Color(0.25f, 0.65f, 0.28f, 1f), 7, isCircle: true);
            }

            // Distant Skyline Buildings (positioned far outside the road network perimeter, never blocking driving view)
            CreateSkylineBuilding(dec.transform, "Skyline_North_1", new Vector3(-15f, 35f, 0f), new Vector2(22f, 10f), new Color(0.38f, 0.42f, 0.48f, 1f));
            CreateSkylineBuilding(dec.transform, "Skyline_North_2", new Vector3(15f, 35f, 0f), new Vector2(24f, 10f), new Color(0.42f, 0.45f, 0.52f, 1f));
            CreateSkylineBuilding(dec.transform, "Skyline_South_1", new Vector3(-15f, -35f, 0f), new Vector2(22f, 10f), new Color(0.40f, 0.44f, 0.50f, 1f));
            CreateSkylineBuilding(dec.transform, "Skyline_South_2", new Vector3(15f, -35f, 0f), new Vector2(24f, 10f), new Color(0.38f, 0.40f, 0.46f, 1f));
            CreateSkylineBuilding(dec.transform, "Skyline_East_1", new Vector3(52f, 0f, 0f), new Vector2(12f, 30f), new Color(0.35f, 0.38f, 0.44f, 1f));
            CreateSkylineBuilding(dec.transform, "Skyline_West_1", new Vector3(-52f, 0f, 0f), new Vector2(12f, 30f), new Color(0.35f, 0.38f, 0.44f, 1f));
        }

        private static void CreateSkylineBuilding(Transform parent, string name, Vector3 pos, Vector2 size, Color color)
        {
            GameObject bldg = FindOrCreateChild(parent, name);
            bldg.transform.position = pos;
            CreateWorldSprite(bldg.transform, "Roof", Vector3.zero, new Vector3(size.x, size.y, 1f), color, 4);
            CreateWorldSprite(bldg.transform, "Trim", Vector3.zero, new Vector3(size.x + 0.5f, size.y + 0.5f, 1f), new Color(0.24f, 0.26f, 0.30f, 1f), 3);
        }
        #endregion

        #region UI Construction
        private static Level7UIController CreateUIHierarchy(Level7PlayerCar playerCar)
        {
            GameObject canvasObj = FindOrCreate("Level7_Canvas");
            Canvas canvas = GetOrAdd<Canvas>(canvasObj);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObj);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAdd<GraphicRaycaster>(canvasObj);

            // 1. Mission Card (Top-Left)
            GameObject missionCard = CreateUIPanel(canvasObj.transform, "MissionCard", new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(380f, 125f), new Color(0.08f, 0.12f, 0.18f, 0.94f));

            TMP_Text mNum = CreateUIText(missionCard.transform, "MissionNumber", "MISSION 1 / 5", 13f, TextAlignmentOptions.Left, new Vector2(240f, 20f), new Vector2(14f, -12f), new Vector2(0f, 1f));
            mNum.color = new Color(1f, 0.85f, 0.2f, 1f);
            mNum.fontStyle = FontStyles.Bold;

            TMP_Text mTitle = CreateUIText(missionCard.transform, "MissionTitle", "LEARN TO IGNORE", 16f, TextAlignmentOptions.Left, new Vector2(350f, 24f), new Vector2(14f, -34f), new Vector2(0f, 1f));
            mTitle.fontStyle = FontStyles.Bold;

            TMP_Text mObj = CreateUIText(missionCard.transform, "MissionObjective", "Drive to South Avenue Depot. Ignore phone alerts.", 12.5f, TextAlignmentOptions.Left, new Vector2(350f, 48f), new Vector2(14f, -60f), new Vector2(0f, 1f));
            mObj.color = new Color(0.85f, 0.88f, 0.92f, 1f);

            // 2. Score & Focus Card (Top-Right)
            GameObject scoreCard = CreateUIPanel(canvasObj.transform, "ScoreFocusCard", new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(340f, 125f), new Color(0.08f, 0.12f, 0.18f, 0.94f));

            TMP_Text sVal = CreateUIText(scoreCard.transform, "ScoreText", "SCORE: 100", 16f, TextAlignmentOptions.Right, new Vector2(310f, 22f), new Vector2(-14f, -12f), new Vector2(1f, 1f));
            sVal.fontStyle = FontStyles.Bold;

            TMP_Text sfVal = CreateUIText(scoreCard.transform, "SafetyText", "SAFETY: 100%", 13.5f, TextAlignmentOptions.Left, new Vector2(140f, 20f), new Vector2(14f, -38f), new Vector2(0f, 1f));

            TMP_Text fcVal = CreateUIText(scoreCard.transform, "FocusText", "FOCUS: 100%", 13.5f, TextAlignmentOptions.Left, new Vector2(140f, 20f), new Vector2(14f, -62f), new Vector2(0f, 1f));
            fcVal.fontStyle = FontStyles.Bold;

            // Focus meter bar
            GameObject focusBarBg = CreateUIPanel(scoreCard.transform, "FocusBarBg", new Vector2(1f, 1f), new Vector2(-14f, -64f), new Vector2(160f, 14f), new Color(0.2f, 0.2f, 0.25f, 0.85f));
            GameObject focusFillObj = FindOrCreateChild(focusBarBg.transform, "Fill");
            SetFullScreen(GetOrAdd<RectTransform>(focusFillObj));
            Image fcFill = GetOrAdd<Image>(focusFillObj);
            fcFill.color = new Color(0.18f, 0.85f, 0.40f, 1f);

            TMP_Text ignVal = CreateUIText(scoreCard.transform, "IgnoredCounter", "Ignored: 0", 12.5f, TextAlignmentOptions.Left, new Vector2(140f, 20f), new Vector2(14f, -92f), new Vector2(0f, 1f));
            ignVal.color = new Color(0.4f, 0.9f, 0.5f, 1f);

            TMP_Text unsVal = CreateUIText(scoreCard.transform, "UnsafeCounter", "Unsafe: 0", 12.5f, TextAlignmentOptions.Right, new Vector2(140f, 20f), new Vector2(-14f, -92f), new Vector2(1f, 1f));
            unsVal.color = new Color(1f, 0.45f, 0.45f, 1f);

            // 3. Top Center Safe Stop Banner & Feedback Toast
            GameObject safeStopBanner = CreateUIPanel(canvasObj.transform, "SafeStopBanner", new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(440f, 44f), new Color(0.12f, 0.65f, 0.30f, 0.95f));
            TMP_Text safeStopTxt = CreateUIText(safeStopBanner.transform, "Label", "[SAFE STOP] VEHICLE IN BAY", 14.5f, TextAlignmentOptions.Center, new Vector2(420f, 36f), Vector2.zero, new Vector2(0.5f, 0.5f));
            safeStopTxt.fontStyle = FontStyles.Bold;
            safeStopBanner.SetActive(false); // Hidden until player stops safely in bay

            GameObject feedbackBanner = CreateUIPanel(canvasObj.transform, "FeedbackToast", new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(500f, 42f), new Color(0.14f, 0.18f, 0.24f, 0.95f));
            Image feedbackBg = feedbackBanner.GetComponent<Image>();
            TMP_Text feedbackTxt = CreateUIText(feedbackBanner.transform, "Label", "", 13.5f, TextAlignmentOptions.Center, new Vector2(480f, 34f), Vector2.zero, new Vector2(0.5f, 0.5f));
            feedbackBanner.SetActive(false); // Hidden until a feedback event triggers

            // 4. Bottom-Left Controls Hint
            GameObject ctrlCard = CreateUIPanel(canvasObj.transform, "ControlsCard", new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(270f, 65f), new Color(0.08f, 0.12f, 0.18f, 0.88f));
            CreateUIText(ctrlCard.transform, "CtrlTitle", "CONTROLS", 11.5f, TextAlignmentOptions.Left, new Vector2(240f, 16f), new Vector2(14f, -8f), new Vector2(0f, 1f));
            TMP_Text ctrlBody = CreateUIText(ctrlCard.transform, "CtrlBody", "W/S/A/D or Arrows : Drive\nSPACE : Brake / Stop", 12f, TextAlignmentOptions.Left, new Vector2(240f, 34f), new Vector2(14f, -26f), new Vector2(0f, 1f));
            ctrlBody.color = new Color(0.85f, 0.88f, 0.92f, 1f);

            // 5. Minimap (Bottom-Right)
            GameObject mapObj = CreateUIPanel(canvasObj.transform, "MiniMapPanel", new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(160f, 120f), new Color(0.08f, 0.12f, 0.18f, 0.92f));
            Level7MiniMapController miniMap = mapObj.AddComponent<Level7MiniMapController>();

            GameObject mapCont = FindOrCreateChild(mapObj.transform, "MapContainer");
            SetFullScreen(GetOrAdd<RectTransform>(mapCont));

            GameObject pMarker = CreateUIPanel(mapCont.transform, "PlayerMarker", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f), Color.yellow);
            GameObject dMarker = CreateUIPanel(mapCont.transform, "DestMarker", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 12f), new Color(0.2f, 0.8f, 1f, 1f));
            GameObject ss1Marker = CreateUIPanel(mapCont.transform, "SafeStop1", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f), Color.green);
            GameObject ss2Marker = CreateUIPanel(mapCont.transform, "SafeStop2", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f), Color.green);

            SetField(miniMap, "mapContainer", mapCont.GetComponent<RectTransform>());
            SetField(miniMap, "playerMarker", pMarker.GetComponent<RectTransform>());
            SetField(miniMap, "destinationMarker", dMarker.GetComponent<RectTransform>());
            SetField(miniMap, "safeStop1Marker", ss1Marker.GetComponent<RectTransform>());
            SetField(miniMap, "safeStop2Marker", ss2Marker.GetComponent<RectTransform>());

            // 6. Distraction Notification Card (Right edge: 360 x 140)
            GameObject distCard = CreateUIPanel(canvasObj.transform, "DistractionCard", new Vector2(1f, 0.5f), new Vector2(-25f, 0f), new Vector2(360f, 140f), new Color(0.10f, 0.14f, 0.20f, 0.96f));
            Level7DistractionUI distUI = distCard.AddComponent<Level7DistractionUI>();

            TMP_Text dTitle = CreateUIText(distCard.transform, "Title", "[MESSAGE] NEW TEXT", 15f, TextAlignmentOptions.Left, new Vector2(330f, 22f), new Vector2(15f, -12f), new Vector2(0f, 1f));
            dTitle.fontStyle = FontStyles.Bold;
            dTitle.color = new Color(1f, 0.85f, 0.2f, 1f);

            TMP_Text dMsg = CreateUIText(distCard.transform, "Message", "\"Hey! Are you on your way?\"", 13f, TextAlignmentOptions.Left, new Vector2(330f, 38f), new Vector2(15f, -38f), new Vector2(0f, 1f));

            // Buttons: Action & Ignore
            GameObject actBtnObj = CreateUIButton(distCard.transform, "ActionButton", "OPEN", new Vector2(0f, 0f), new Vector2(15f, 15f), new Vector2(150f, 36f), new Color(0.85f, 0.25f, 0.20f, 1f));
            GameObject ignBtnObj = CreateUIButton(distCard.transform, "IgnoreButton", "IGNORE", new Vector2(1f, 0f), new Vector2(-15f, 15f), new Vector2(150f, 36f), new Color(0.20f, 0.60f, 0.35f, 1f));

            // Countdown bar along top edge
            GameObject timerBar = CreateUIPanel(distCard.transform, "CountdownBar", new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(360f, 5f), new Color(0.95f, 0.75f, 0.15f, 1f));
            Image timerBarImg = timerBar.GetComponent<Image>();
            timerBarImg.type = Image.Type.Filled;
            timerBarImg.fillMethod = Image.FillMethod.Horizontal;

            SetField(distUI, "canvasGroup", GetOrAdd<CanvasGroup>(distCard));
            SetField(distUI, "cardRect", distCard.GetComponent<RectTransform>());
            SetField(distUI, "titleText", dTitle);
            SetField(distUI, "messageText", dMsg);
            SetField(distUI, "actionButton", actBtnObj.GetComponent<Button>());
            SetField(distUI, "actionButtonText", actBtnObj.GetComponentInChildren<TMP_Text>());
            SetField(distUI, "ignoreButton", ignBtnObj.GetComponent<Button>());
            SetField(distUI, "ignoreButtonText", ignBtnObj.GetComponentInChildren<TMP_Text>());
            SetField(distUI, "countdownBar", timerBarImg);

            // 7. Completion Modal (Centered overlay, hidden initially)
            GameObject overlayObj = FindOrCreateChild(canvasObj.transform, "CompletionOverlay");
            RectTransform overlayRt = GetOrAdd<RectTransform>(overlayObj);
            SetFullScreen(overlayRt);
            Image overlayImg = GetOrAdd<Image>(overlayObj);
            overlayImg.color = new Color(0.04f, 0.06f, 0.10f, 0.80f);
            overlayImg.raycastTarget = true;

            GameObject modalObj = CreateUIPanel(overlayObj.transform, "ModalCard", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 520f), new Color(0.08f, 0.12f, 0.18f, 0.98f));
            Level7CompletionModal modal = overlayObj.AddComponent<Level7CompletionModal>();

            TMP_Text cTitle = CreateUIText(modalObj.transform, "Title", "LEVEL 7 COMPLETED!", 25f, TextAlignmentOptions.Center, new Vector2(580f, 38f), new Vector2(0f, -24f), new Vector2(0.5f, 1f));
            cTitle.fontStyle = FontStyles.Bold;
            cTitle.color = new Color(1f, 0.85f, 0.20f, 1f);

            TMP_Text cSub = CreateUIText(modalObj.transform, "Subtitle", "DISTRACTED DRIVING CHALLENGE CERTIFICATE", 13.5f, TextAlignmentOptions.Center, new Vector2(580f, 22f), new Vector2(0f, -60f), new Vector2(0.5f, 1f));
            cSub.color = new Color(0.75f, 0.88f, 1f, 1f);

            TMP_Text cStars = CreateUIText(modalObj.transform, "Stars", "*** 3 / 3 STARS ***", 30f, TextAlignmentOptions.Center, new Vector2(580f, 44f), new Vector2(0f, -92f), new Vector2(0.5f, 1f));
            cStars.color = new Color(1f, 0.85f, 0.20f, 1f);

            TMP_Text cScore = CreateUIText(modalObj.transform, "ScoreVal", "FINAL SCORE: 500", 22f, TextAlignmentOptions.Center, new Vector2(400f, 32f), new Vector2(0f, -145f), new Vector2(0.5f, 1f));
            cScore.fontStyle = FontStyles.Bold;

            // Stats grid
            TMP_Text cSafety = CreateUIText(modalObj.transform, "SafetyVal", "95%", 18f, TextAlignmentOptions.Center, new Vector2(170f, 26f), new Vector2(-180f, -195f), new Vector2(0.5f, 1f));
            CreateUIText(modalObj.transform, "SafetyLbl", "Safety Score", 12f, TextAlignmentOptions.Center, new Vector2(170f, 18f), new Vector2(-180f, -220f), new Vector2(0.5f, 1f));

            TMP_Text cFocus = CreateUIText(modalObj.transform, "FocusValue", "92%", 18f, TextAlignmentOptions.Center, new Vector2(170f, 26f), new Vector2(0f, -195f), new Vector2(0.5f, 1f));
            CreateUIText(modalObj.transform, "FocusLbl", "Focus Rating", 12f, TextAlignmentOptions.Center, new Vector2(170f, 18f), new Vector2(0f, -220f), new Vector2(0.5f, 1f));

            TMP_Text cIgnored = CreateUIText(modalObj.transform, "IgnoredVal", "10", 18f, TextAlignmentOptions.Center, new Vector2(170f, 26f), new Vector2(180f, -195f), new Vector2(0.5f, 1f));
            CreateUIText(modalObj.transform, "IgnoredLbl", "Ignored Alerts", 12f, TextAlignmentOptions.Center, new Vector2(170f, 18f), new Vector2(180f, -220f), new Vector2(0.5f, 1f));

            TMP_Text cUnsafe = CreateUIText(modalObj.transform, "UnsafeVal", "0", 18f, TextAlignmentOptions.Center, new Vector2(170f, 26f), new Vector2(-110f, -255f), new Vector2(0.5f, 1f));
            CreateUIText(modalObj.transform, "UnsafeLbl", "Unsafe Interactions", 12f, TextAlignmentOptions.Center, new Vector2(170f, 18f), new Vector2(-110f, -280f), new Vector2(0.5f, 1f));

            TMP_Text cSafeStops = CreateUIText(modalObj.transform, "SafeStopsVal", "2", 18f, TextAlignmentOptions.Center, new Vector2(170f, 26f), new Vector2(110f, -255f), new Vector2(0.5f, 1f));
            CreateUIText(modalObj.transform, "SafeStopsLbl", "Safe Bay Stops", 12f, TextAlignmentOptions.Center, new Vector2(170f, 18f), new Vector2(110f, -280f), new Vector2(0.5f, 1f));

            TMP_Text cEdu = CreateUIText(modalObj.transform, "EduText", "<b>ROAD FIRST.</b>\nYour attention belongs on the road. If a distraction needs your attention, pull over somewhere safe before interacting with it.", 13.5f, TextAlignmentOptions.Center, new Vector2(560f, 65f), new Vector2(0f, -345f), new Vector2(0.5f, 1f));
            cEdu.color = new Color(1f, 0.92f, 0.65f, 1f);

            GameObject retryBtn = CreateUIButton(modalObj.transform, "PlayAgainBtn", "PLAY AGAIN", new Vector2(0.5f, 0f), new Vector2(-130f, 35f), new Vector2(210f, 48f), new Color(0.18f, 0.65f, 0.35f, 1f));
            GameObject menuBtn = CreateUIButton(modalObj.transform, "MenuBtn", "MAIN MENU", new Vector2(0.5f, 0f), new Vector2(130f, 35f), new Vector2(210f, 48f), new Color(0.22f, 0.45f, 0.70f, 1f));

            SetField(modal, "canvasGroup", GetOrAdd<CanvasGroup>(overlayObj));
            SetField(modal, "titleText", cTitle);
            SetField(modal, "subtitleText", cSub);
            SetField(modal, "starsText", cStars);
            SetField(modal, "scoreValueText", cScore);
            SetField(modal, "safetyValueText", cSafety);
            SetField(modal, "focusValueText", cFocus);
            SetField(modal, "ignoredValueText", cIgnored);
            SetField(modal, "unsafeValueText", cUnsafe);
            SetField(modal, "safeStopsValueText", cSafeStops);
            SetField(modal, "educationalText", cEdu);
            SetField(modal, "playAgainButton", retryBtn.GetComponent<Button>());
            SetField(modal, "mainMenuButton", menuBtn.GetComponent<Button>());
            modal.WireButtons();
            overlayObj.SetActive(false);

            // Wire UI Controller
            Level7UIController uiCtrl = GetOrAdd<Level7UIController>(canvasObj);
            SetField(uiCtrl, "missionNumberText", mNum);
            SetField(uiCtrl, "missionTitleText", mTitle);
            SetField(uiCtrl, "missionObjectiveText", mObj);
            SetField(uiCtrl, "scoreText", sVal);
            SetField(uiCtrl, "safetyText", sfVal);
            SetField(uiCtrl, "focusText", fcVal);
            SetField(uiCtrl, "focusMeterFill", fcFill);
            SetField(uiCtrl, "ignoredCountText", ignVal);
            SetField(uiCtrl, "unsafeCountText", unsVal);
            SetField(uiCtrl, "safeStopBanner", safeStopBanner);
            SetField(uiCtrl, "safeStopBannerText", safeStopTxt);
            SetField(uiCtrl, "feedbackBanner", feedbackBanner);
            SetField(uiCtrl, "feedbackText", feedbackTxt);
            SetField(uiCtrl, "feedbackBg", feedbackBg);
            SetField(uiCtrl, "miniMap", miniMap);
            SetField(uiCtrl, "distractionUI", distUI);
            SetField(uiCtrl, "completionModal", modal);

            return uiCtrl;
        }
        #endregion

        #region Helpers & Visual Builders
        private static GameObject FindOrCreate(string name)
        {
            GameObject obj = GameObject.Find(name);
            return obj != null ? obj : new GameObject(name);
        }

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child.gameObject;
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static T GetOrAdd<T>(GameObject obj) where T : Component
        {
            T comp = obj.GetComponent<T>();
            return comp != null ? comp : obj.AddComponent<T>();
        }

        private static GameObject CreateWorldSprite(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int sortingOrder, bool isCircle = false)
        {
            GameObject obj = FindOrCreateChild(parent, name);
            obj.transform.localPosition = pos;
            obj.transform.localScale = scale;
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(obj);
            sr.sprite = LoadSprite(isCircle ? WorldCircleSpritePath : WorldSquareSpritePath, isCircle);
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return obj;
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            GameObject obj = FindOrCreateChild(parent, name);
            RectTransform rt = GetOrAdd<RectTransform>(obj);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = GetOrAdd<Image>(obj);
            img.color = color;
            return obj;
        }

        private static TMP_Text CreateUIText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align, Vector2 size, Vector2 pos, Vector2 anchor)
        {
            GameObject obj = FindOrCreateChild(parent, name);
            RectTransform rt = GetOrAdd<RectTransform>(obj);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            TMP_Text tmp = GetOrAdd<TextMeshProUGUI>(obj);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static GameObject CreateUIButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color bgCol)
        {
            GameObject btnObj = CreateUIPanel(parent, name, anchor, pos, size, bgCol);
            Button btn = GetOrAdd<Button>(btnObj);
            TMP_Text lbl = CreateUIText(btnObj.transform, "Label", label, 14f, TextAlignmentOptions.Center, size, Vector2.zero, new Vector2(0.5f, 0.5f));
            lbl.fontStyle = FontStyles.Bold;
            return btnObj;
        }

        private static void SetFullScreen(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        private static Sprite fallbackSquare;
        private static Sprite fallbackCircle;

        private static Sprite LoadSprite(string path, bool isCircle = false)
        {
#if UNITY_EDITOR
            Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;
#endif
            Sprite res = Resources.Load<Sprite>(path);
            if (res != null) return res;

            if (isCircle)
            {
                if (fallbackCircle == null)
                {
                    Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                    for (int y = 0; y < 32; y++)
                    {
                        for (int x = 0; x < 32; x++)
                        {
                            float dx = (x - 15.5f) / 15.5f;
                            float dy = (y - 15.5f) / 15.5f;
                            tex.SetPixel(x, y, (dx * dx + dy * dy <= 1f) ? Color.white : Color.clear);
                        }
                    }
                    tex.Apply();
                    fallbackCircle = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
                }
                return fallbackCircle;
            }
            else
            {
                if (fallbackSquare == null)
                {
                    Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 4; x++)
                            tex.SetPixel(x, y, Color.white);
                    tex.Apply();
                    fallbackSquare = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                }
                return fallbackSquare;
            }
        }

        private static void SetField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
        #endregion
    }

    public class Level7DestinationBeacon : MonoBehaviour
    {
        private Vector3 initialScale;

        private void Start()
        {
            initialScale = transform.localScale;
            if (initialScale == Vector3.zero) initialScale = Vector3.one;
        }

        private void Update()
        {
            float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 3.5f);
            transform.localScale = initialScale * pulse;
        }
    }
}
