using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using TrafficTown2D.Core;

namespace TrafficTown2D.Level6
{
    /// <summary>
    /// Runtime safety bootstrap for Level 6 (Extreme Road Conditions).
    /// Self-heals the entire Level 6 scene upon load if any components, roads, hazards,
    /// or UI elements are missing, ensuring 100% playable reliability in all environments.
    /// </summary>
    public class Level6Bootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == "Level6")
            {
                EnsureLevel6Scene();
            }
        }

        private void Awake()
        {
            EnsureLevel6Scene();
        }

        public static void EnsureLevel6Scene()
        {
            // 1. Camera
            Camera cam = EnsureCamera();

            // Check if scene is already populated
            GameObject env = GameObject.Find("Environment");
            Level6PlayerCar existingCar = UnityEngine.Object.FindFirstObjectByType<Level6PlayerCar>();

            if (env != null && existingCar != null && Level6MissionManager.Instance != null)
            {
                // Ensure camera follow is bound
                Level6CameraFollow cf = cam.GetComponent<Level6CameraFollow>();
                if (cf == null) cf = cam.gameObject.AddComponent<Level6CameraFollow>();
                cf.SetTarget(existingCar.transform);
                return;
            }

            Debug.Log("[Level6Bootstrap] Bootstrapping Level 6 runtime scene...");

            // 2. Global Light & Event System
            Light2D globalLight = EnsureGlobalLight();
            EnsureEventSystem();

            // 3. Environment Root
            if (env == null)
            {
                env = new GameObject("Environment");
                env.transform.position = Vector3.zero;
            }

            // 4. Construct Road Network
            BuildRoadNetwork(env.transform);

            // 5. Detection Zones
            BuildDetectionZones(env.transform);

            // 6. Hazards
            EnvironmentalHazardController hazardCtrl = BuildHazards(env.transform);

            // 7. Weather
            WeatherController weatherCtrl = BuildWeather(env.transform, globalLight, cam);

            // 8. Destination
            Level6Destination destination = BuildDestination(env.transform);

            // 9. Player Car
            GameObject playerObj = BuildPlayerCar(env.transform);
            Level6PlayerCar playerCar = playerObj.GetComponent<Level6PlayerCar>();
            weatherCtrl.SetPlayerCar(playerCar);

            Level6CameraFollow camFollow = cam.GetComponent<Level6CameraFollow>();
            if (camFollow == null) camFollow = cam.gameObject.AddComponent<Level6CameraFollow>();
            camFollow.SetTarget(playerCar.transform);

            // 10. Controllers
            GameObject ctrlObj = GameObject.Find("Controllers");
            if (ctrlObj == null) ctrlObj = new GameObject("Controllers");
            ctrlObj.transform.position = Vector3.zero;

            Level6SafetyManager safetyMgr = ctrlObj.GetComponent<Level6SafetyManager>();
            if (safetyMgr == null) safetyMgr = ctrlObj.AddComponent<Level6SafetyManager>();

            Level6RoadNetwork roadNetwork = ctrlObj.GetComponent<Level6RoadNetwork>();
            if (roadNetwork == null) roadNetwork = ctrlObj.AddComponent<Level6RoadNetwork>();

            Level6GPSCompass gpsCompass = ctrlObj.GetComponent<Level6GPSCompass>();
            if (gpsCompass == null) gpsCompass = ctrlObj.AddComponent<Level6GPSCompass>();

            // 11. UI
            Level6UIController uiCtrl = BuildUI(playerCar, destination);
            gpsCompass.BindReferences(playerCar, destination, null, null);

            // 12. Mission Manager
            Level6MissionManager missionMgr = ctrlObj.GetComponent<Level6MissionManager>();
            if (missionMgr == null) missionMgr = ctrlObj.AddComponent<Level6MissionManager>();

            SetRef(missionMgr, "playerCar", playerCar);
            SetRef(missionMgr, "destinationGoal", destination);
            SetRef(missionMgr, "weatherController", weatherCtrl);
            SetRef(missionMgr, "hazardController", hazardCtrl);
            SetRef(missionMgr, "uiController", uiCtrl);

            Debug.Log("[Level6Bootstrap] Level 6 scene bootstrap complete!");
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
            cam.orthographicSize = 9.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.22f, 0.36f, 0.22f, 1f);

            UniversalAdditionalCameraData camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderType = CameraRenderType.Base;
            camData.renderPostProcessing = false;

            return cam;
        }

        private static Light2D EnsureGlobalLight()
        {
            Light2D[] lights = UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include);
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

            globalLight.color = new Color(0.85f, 0.90f, 0.95f, 1f);
            globalLight.intensity = 0.85f;
            return globalLight;
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

        #region Road Network Generation
        private static void BuildRoadNetwork(Transform world)
        {
            Color roadCol = new Color(0.14f, 0.15f, 0.18f, 1f);
            Color curbCol = new Color(0.72f, 0.74f, 0.78f, 1f);
            Color yellowLineCol = new Color(1f, 0.88f, 0.18f, 0.95f);
            Color whiteLineCol = new Color(0.95f, 0.95f, 0.95f, 0.90f);
            Color grassCol = new Color(0.18f, 0.32f, 0.18f, 1f);
            Color shoulderCol = new Color(0.38f, 0.40f, 0.44f, 1f);

            // Ground Grass
            CreateSprite(world, "GroundGrass", Vector3.zero, new Vector3(180f, 130f, 1f), grassCol, -10);

            GameObject roads = new GameObject("RoadNetwork");
            roads.transform.SetParent(world, false);

            // Structure 1: South-West Avenue (Mission 1 Spawn) - stops cleanly before 90-deg turn at Y=-18.6
            CreateRoadSegment(roads.transform, "Road_SW_Avenue", new Vector3(-45f, -31.8f, 0f), new Vector2(7.2f, 26.4f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, false, true, true);

            // Structure 2: 90-Degree Turn at (-45, -15) - covers (-48.6 to -41.4, -18.6 to -11.4)
            CreateSprite(roads.transform, "Corner_Shoulder", new Vector3(-45f, -15f, 0f), new Vector3(8.6f, 8.6f, 1f), shoulderCol, -2);
            CreateSprite(roads.transform, "Corner_90Deg", new Vector3(-45f, -15f, 0f), new Vector3(7.4f, 7.4f, 1f), roadCol, 0);
            CreateSprite(roads.transform, "Corner_TopCurb", new Vector3(-45f, -11.22f, 0f), new Vector3(7.4f, 0.35f, 1f), curbCol, 1);
            CreateSprite(roads.transform, "Corner_LeftCurb", new Vector3(-48.78f, -15f, 0f), new Vector3(0.35f, 7.4f, 1f), curbCol, 1);

            // Structure 2b: South Connector from (-41.4, -15) to (-18.6, -15) (length 22.8, width 7.2)
            CreateRoadSegment(roads.transform, "Road_South_Connector", new Vector3(-30f, -15f, 0f), new Vector2(22.8f, 7.2f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, true, true, true);

            // Structure 3: T-Junction Box at (-15, -15)
            CreateSprite(roads.transform, "TJunction_Shoulder", new Vector3(-15f, -15f, 0f), new Vector3(8.6f, 8.6f, 1f), shoulderCol, -2);
            CreateSprite(roads.transform, "TJunction_Box", new Vector3(-15f, -15f, 0f), new Vector3(7.4f, 7.4f, 1f), roadCol, 0);
            CreateSprite(roads.transform, "TJunction_BottomCurb", new Vector3(-15f, -18.78f, 0f), new Vector3(7.4f, 0.35f, 1f), curbCol, 1);

            // Structure 3b: T-Junction Leg N connecting north to roundabout: from Y=-11.4 up to Y=13.5 (length 24.9, width 7.2)
            CreateRoadSegment(roads.transform, "Road_TJunction_Leg_N", new Vector3(-15f, 1.05f, 0f), new Vector2(7.2f, 24.9f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, false, true, true);

            // Structure 4: Curved Road (S-curve) across southern valley
            BuildCurvedRoad(roads.transform, new Vector2(-11.4f, -15f), new Vector2(20f, -15f), 7.2f, roadCol, curbCol, whiteLineCol);

            // East Service Depot (Destination 1)
            CreateSprite(roads.transform, "EastDepot_Shoulder", new Vector3(25f, -15f, 0f), new Vector3(11.2f, 11.2f, 1f), shoulderCol, -2);
            CreateSprite(roads.transform, "EastDepot_Pad", new Vector3(25f, -15f, 0f), new Vector3(10f, 10f, 1f), new Color(0.22f, 0.25f, 0.28f, 1f), 0);
            CreateSprite(roads.transform, "EastDepot_CurbS", new Vector3(25f, -20.18f, 0f), new Vector3(10.4f, 0.35f, 1f), curbCol, 1);
            CreateSprite(roads.transform, "EastDepot_CurbE", new Vector3(30.18f, -15f, 0f), new Vector3(0.35f, 10.4f, 1f), curbCol, 1);

            // 4. Approach to Fork & Decision Fork
            CreateRoadSegment(roads.transform, "Road_Fork_Approach", new Vector3(25f, -6f, 0f), new Vector2(7.2f, 8f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, false, true, true);

            // Structure 5: Decision Fork Junction at (25, 0)
            CreateSprite(roads.transform, "Fork_Junction_Shoulder", new Vector3(26f, 2f, 0f), new Vector3(15f, 10f, 1f), shoulderCol, -2);
            CreateSprite(roads.transform, "Fork_Junction_Pad", new Vector3(26f, 2f, 0f), new Vector3(14f, 9f, 1f), roadCol, 0);

            // Traffic Splitter Island between Branch A and Branch B
            CreateSprite(roads.transform, "Fork_Splitter_Shoulder", new Vector3(26f, 3.5f, 0f), new Vector3(3.6f, 4.5f, 1f), shoulderCol, 0);
            CreateSprite(roads.transform, "Fork_Splitter_Curb", new Vector3(26f, 3.5f, 0f), new Vector3(3.2f, 4.0f, 1f), curbCol, 1);
            CreateSprite(roads.transform, "Fork_Splitter_Center", new Vector3(26f, 3.5f, 0f), new Vector3(2.5f, 3.2f, 1f), grassCol, 2);

            // Branch A: Direct Hazardous / Flooded Route from (25, 0) diagonally to (0, 25)
            // Midpoint: (12.5, 12.5), Length: 35.35m, Angle: -45 deg, Width: 7.2m
            CreateRoadSegment(roads.transform, "Road_Fork_HazardBranch", new Vector3(12.5f, 12.5f, 0f), new Vector2(7.2f, 35.35f), -45f, roadCol, curbCol, yellowLineCol, whiteLineCol, false, true, false);
            Transform branchAT = roads.transform.Find("Road_Fork_HazardBranch");
            if (branchAT != null) CreateSprite(branchAT, "Curb_Right_Short", new Vector3(3.78f, 0f, 0f), new Vector3(0.35f, 16f, 1f), curbCol, 1);

            // Branch B: Elevated Safe Detour Route: (25, 0) -> (38, 5) -> (38, 25) -> (0, 25)
            CreateRoadSegment(roads.transform, "Road_Detour_CurveIn", new Vector3(31.5f, 2.5f, 0f), new Vector2(14f, 7.2f), 21f, roadCol, curbCol, yellowLineCol, whiteLineCol, true, false, true);
            CreateRoadSegment(roads.transform, "Road_Detour_East", new Vector3(38f, 15f, 0f), new Vector2(7.2f, 20f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, false, false, true);
            Transform detourEastT = roads.transform.Find("Road_Detour_East");
            if (detourEastT != null) CreateSprite(detourEastT, "Curb_Left_Short", new Vector3(-3.78f, 0f, 0f), new Vector3(0.35f, 12f, 1f), curbCol, 1);
            CreateRoadSegment(roads.transform, "Road_Detour_NorthArm", new Vector3(19f, 25f, 0f), new Vector2(38f, 7.2f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, true, true, false);
            Transform detourNorthT = roads.transform.Find("Road_Detour_NorthArm");
            if (detourNorthT != null) CreateSprite(detourNorthT, "Curb_Bottom_Short", new Vector3(2f, -3.78f, 0f), new Vector3(24f, 0.35f, 1f), curbCol, 1);

            // Merge Junction at (0, 25) where Branch A and Branch B meet before Roundabout:
            CreateSprite(roads.transform, "Merge_Junction_Shoulder", new Vector3(0f, 25f, 0f), new Vector3(10f, 9.5f, 1f), shoulderCol, -2);
            CreateSprite(roads.transform, "Merge_Junction_Pad", new Vector3(0f, 25f, 0f), new Vector3(9f, 8.5f, 1f), roadCol, 0);

            // 5. Roundabout at (-15, 25)
            BuildRoundabout(roads.transform, new Vector2(-15f, 25f), 11.5f, 5.5f, roadCol, curbCol, grassCol, whiteLineCol);
            CreateRoadSegment(roads.transform, "Road_Roundabout_EastArm", new Vector3(-1.75f, 25f, 0f), new Vector2(3.5f, 7.2f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, true, false, false);
            CreateRoadSegment(roads.transform, "Road_Roundabout_WestArm", new Vector3(-31.75f, 25f, 0f), new Vector2(10.5f, 7.2f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, true);
            CreateSprite(roads.transform, "WestLogistics_Shoulder", new Vector3(-42f, 25f, 0f), new Vector3(11.2f, 11.2f, 1f), shoulderCol, -2);
            CreateSprite(roads.transform, "WestLogistics_Pad", new Vector3(-42f, 25f, 0f), new Vector3(10f, 10f, 1f), new Color(0.22f, 0.25f, 0.28f, 1f), 0);
            CreateSprite(roads.transform, "WestLogistics_CurbW", new Vector3(-47.18f, 25f, 0f), new Vector3(0.35f, 10.4f, 1f), curbCol, 1);
            CreateRoadSegment(roads.transform, "Road_Roundabout_NorthArm", new Vector3(-15f, 38.95f, 0f), new Vector2(7.2f, 4.9f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol);

            // 6. Northern Expressway & U-Turn Loop
            CreateRoadSegment(roads.transform, "Road_Northern_Expressway", new Vector3(16.5f, 45f, 0f), new Vector2(63f, 7.2f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, true, true, false);
            CreateSprite(roads.transform, "Exp_BottomCurb_West", new Vector3(3.75f, 41.22f, 0f), new Vector3(37.5f, 0.35f, 1f), curbCol, 1);
            CreateSprite(roads.transform, "Exp_BottomCurb_East", new Vector3(42.75f, 41.22f, 0f), new Vector3(10.5f, 0.35f, 1f), curbCol, 1);

            BuildUTurnLoop(roads.transform, new Vector2(30f, 45f), 7.5f, roadCol, curbCol, whiteLineCol);

            // Direction Signs & Road Arrows
            BuildRoadsideSignsAndArrows(world);

            // Boundaries
            BuildWorldBounds(world);
        }

        private static void CreateRoadSegment(Transform parent, string name, Vector3 pos, Vector2 size, float rotZ, Color roadCol, Color curbCol, Color yellowCol, Color whiteCol, bool isHorizontal = false, bool hasCurb1 = true, bool hasCurb2 = true)
        {
            GameObject seg = new GameObject(name);
            seg.transform.SetParent(parent, false);
            seg.transform.position = pos;
            seg.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);

            Color shoulderCol = new Color(0.38f, 0.40f, 0.44f, 1f);
            CreateSprite(seg.transform, "ShoulderBed", Vector3.zero, new Vector3(size.x + (isHorizontal ? 0f : 1.2f), size.y + (isHorizontal ? 1.2f : 0f), 1f), shoulderCol, -2);
            CreateSprite(seg.transform, "Asphalt", Vector3.zero, new Vector3(size.x, size.y, 1f), roadCol, 0);

            if (isHorizontal)
            {
                CreateSprite(seg.transform, "YellowLine1", new Vector3(0f, 0.10f, 0f), new Vector3(size.x, 0.10f, 1f), yellowCol, 2);
                CreateSprite(seg.transform, "YellowLine2", new Vector3(0f, -0.10f, 0f), new Vector3(size.x, 0.10f, 1f), yellowCol, 2);
                if (hasCurb1) CreateSprite(seg.transform, "Curb_Top", new Vector3(0f, (size.y * 0.5f) + 0.18f, 0f), new Vector3(size.x, 0.35f, 1f), curbCol, 1);
                if (hasCurb2) CreateSprite(seg.transform, "Curb_Bottom", new Vector3(0f, -(size.y * 0.5f) - 0.18f, 0f), new Vector3(size.x, 0.35f, 1f), curbCol, 1);
            }
            else
            {
                CreateSprite(seg.transform, "YellowLine1", new Vector3(0.10f, 0f, 0f), new Vector3(0.10f, size.y, 1f), yellowCol, 2);
                CreateSprite(seg.transform, "YellowLine2", new Vector3(-0.10f, 0f, 0f), new Vector3(0.10f, size.y, 1f), yellowCol, 2);
                if (hasCurb1) CreateSprite(seg.transform, "Curb_Left", new Vector3(-(size.x * 0.5f) - 0.18f, 0f, 0f), new Vector3(0.35f, size.y, 1f), curbCol, 1);
                if (hasCurb2) CreateSprite(seg.transform, "Curb_Right", new Vector3((size.x * 0.5f) + 0.18f, 0f, 0f), new Vector3(0.35f, size.y, 1f), curbCol, 1);
            }
        }

        private static void BuildCurvedRoad(Transform parent, Vector2 start, Vector2 end, float width, Color roadCol, Color curbCol, Color lineCol)
        {
            GameObject curve = new GameObject("Curved_Road_Spline");
            curve.transform.SetParent(parent, false);

            int steps = 20;
            Vector2 p0 = start;
            Vector2 p1 = new Vector2((start.x + end.x) * 0.5f, start.y - 8f);
            Vector2 p2 = end;

            Vector2 prev = p0;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 curr = Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;

                Vector2 mid = (prev + curr) * 0.5f;
                Vector2 dir = curr - prev;
                float len = dir.magnitude;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                GameObject piece = new GameObject($"CurveSeg_{i}");
                piece.transform.SetParent(curve.transform, false);
                piece.transform.position = new Vector3(mid.x, mid.y, 0f);
                piece.transform.rotation = Quaternion.Euler(0f, 0f, angle);

                Color shoulderCol = new Color(0.38f, 0.40f, 0.44f, 1f);
                CreateSprite(piece.transform, "ShoulderBed", Vector3.zero, new Vector3(len + 0.5f, width + 1.2f, 1f), shoulderCol, -2);
                CreateSprite(piece.transform, "Asphalt", Vector3.zero, new Vector3(len + 0.5f, width, 1f), roadCol, 0);
                CreateSprite(piece.transform, "Curb_Top", new Vector3(0f, (width * 0.5f) + 0.18f, 0f), new Vector3(len + 0.5f, 0.35f, 1f), curbCol, 1);
                CreateSprite(piece.transform, "Curb_Bottom", new Vector3(0f, -(width * 0.5f) - 0.18f, 0f), new Vector3(len + 0.5f, 0.35f, 1f), curbCol, 1);
                CreateSprite(piece.transform, "CenterDash", Vector3.zero, new Vector3(len * 0.6f, 0.15f, 1f), lineCol, 2);

                prev = curr;
            }
        }

        private static void BuildRoundabout(Transform parent, Vector2 center, float outerRadius, float innerRadius, Color roadCol, Color curbCol, Color grassCol, Color lineCol)
        {
            GameObject rb = new GameObject("Roundabout_Geometry");
            rb.transform.SetParent(parent, false);
            rb.transform.position = new Vector3(center.x, center.y, 0f);

            Color shoulderCol = new Color(0.38f, 0.40f, 0.44f, 1f);
            CreateSprite(rb.transform, "Outer_Shoulder_Ring", Vector3.zero, new Vector3((outerRadius + 1.0f) * 2f, (outerRadius + 1.0f) * 2f, 1f), shoulderCol, -2, true);
            CreateSprite(rb.transform, "Outer_Road_Circle", Vector3.zero, new Vector3(outerRadius * 2f, outerRadius * 2f, 1f), roadCol, 0, true);
            CreateSprite(rb.transform, "Outer_Curb_Ring", Vector3.zero, new Vector3((outerRadius + 0.35f) * 2f, (outerRadius + 0.35f) * 2f, 1f), curbCol, 1, true);

            float midRadius = (outerRadius + innerRadius) * 0.5f;
            for (int i = 0; i < 16; i++)
            {
                float ang = i * (360f / 16f) * Mathf.Deg2Rad;
                Vector3 dashPos = new Vector3(Mathf.Cos(ang) * midRadius, Mathf.Sin(ang) * midRadius, 0f);
                GameObject dash = CreateSprite(rb.transform, $"Dash_{i}", dashPos, new Vector3(1.3f, 0.18f, 1f), lineCol, 2);
                dash.transform.rotation = Quaternion.Euler(0f, 0f, (i * (360f / 16f)) + 90f);
            }

            CreateSprite(rb.transform, "Inner_Curb_Ring", Vector3.zero, new Vector3((innerRadius + 0.35f) * 2f, (innerRadius + 0.35f) * 2f, 1f), curbCol, 1, true);
            CreateSprite(rb.transform, "Inner_Grass_Island", Vector3.zero, new Vector3(innerRadius * 2f, innerRadius * 2f, 1f), grassCol, 2, true);
            CreateSprite(rb.transform, "Island_Monument", Vector3.zero, new Vector3(3.0f, 3.0f, 1f), new Color(0.68f, 0.72f, 0.78f, 1f), 3, true);

            CircleCollider2D islandCol = rb.AddComponent<CircleCollider2D>();
            islandCol.radius = innerRadius;
            islandCol.isTrigger = false;
        }

        private static void BuildUTurnLoop(Transform parent, Vector2 rootPos, float radius, Color roadCol, Color curbCol, Color lineCol)
        {
            GameObject ut = new GameObject("UTurn_Loop_Geometry");
            ut.transform.SetParent(parent, false);
            ut.transform.position = Vector3.zero;

            int steps = 36;
            float startAngle = 0f;
            float endAngle = -180f;
            Vector2 loopCenter = new Vector2(rootPos.x, 41.4f);
            Color shoulderCol = new Color(0.38f, 0.40f, 0.44f, 1f);

            Vector2 prevPos = loopCenter + new Vector2(Mathf.Cos(startAngle * Mathf.Deg2Rad) * radius, Mathf.Sin(startAngle * Mathf.Deg2Rad) * radius);
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float angleDeg = Mathf.Lerp(startAngle, endAngle, t);
                float rad = angleDeg * Mathf.Deg2Rad;
                Vector2 currPos = loopCenter + new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);

                Vector2 mid = (prevPos + currPos) * 0.5f;
                Vector2 dir = currPos - prevPos;
                float segLen = dir.magnitude;
                float rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                GameObject piece = new GameObject($"UTurnSeg_{i}");
                piece.transform.SetParent(ut.transform, false);
                piece.transform.position = new Vector3(mid.x, mid.y, 0f);
                piece.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);

                CreateSprite(piece.transform, "ShoulderBed", Vector3.zero, new Vector3(segLen + 0.35f, 8.4f, 1f), shoulderCol, -2);
                CreateSprite(piece.transform, "Asphalt", Vector3.zero, new Vector3(segLen + 0.35f, 6.8f, 1f), roadCol, 0);

                if (i % 3 != 0)
                {
                    CreateSprite(piece.transform, "CenterDash", Vector3.zero, new Vector3(segLen * 0.8f, 0.16f, 1f), lineCol, 2);
                }

                prevPos = currPos;
            }

            // Smooth Continuous Outer Curb Arc: radius = 7.5 + 3.4 = 10.9
            GameObject outerCurbs = new GameObject("OuterCurbs");
            outerCurbs.transform.SetParent(ut.transform, false);
            int curbSteps = 45;
            float outerR = radius + 3.4f;
            Vector2 prevOuter = loopCenter + new Vector2(Mathf.Cos(startAngle * Mathf.Deg2Rad) * outerR, Mathf.Sin(startAngle * Mathf.Deg2Rad) * outerR);
            for (int i = 1; i <= curbSteps; i++)
            {
                float t = i / (float)curbSteps;
                float angleDeg = Mathf.Lerp(startAngle, endAngle, t);
                float rad = angleDeg * Mathf.Deg2Rad;
                Vector2 currOuter = loopCenter + new Vector2(Mathf.Cos(rad) * outerR, Mathf.Sin(rad) * outerR);

                Vector2 mid = (prevOuter + currOuter) * 0.5f;
                Vector2 dir = currOuter - prevOuter;
                float len = dir.magnitude;
                float rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                GameObject cPiece = new GameObject($"OuterCurb_{i}");
                cPiece.transform.SetParent(outerCurbs.transform, false);
                cPiece.transform.position = new Vector3(mid.x, mid.y, 0f);
                cPiece.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
                CreateSprite(cPiece.transform, "Curb", Vector3.zero, new Vector3(len + 0.12f, 0.35f, 1f), curbCol, 1);

                prevOuter = currOuter;
            }

            // Smooth Continuous Inner Curb Arc: radius = 7.5 - 3.4 = 4.1
            GameObject innerCurbs = new GameObject("InnerCurbs");
            innerCurbs.transform.SetParent(ut.transform, false);
            float innerR = radius - 3.4f;
            Vector2 prevInner = loopCenter + new Vector2(Mathf.Cos(startAngle * Mathf.Deg2Rad) * innerR, Mathf.Sin(startAngle * Mathf.Deg2Rad) * innerR);
            for (int i = 1; i <= curbSteps; i++)
            {
                float t = i / (float)curbSteps;
                float angleDeg = Mathf.Lerp(startAngle, endAngle, t);
                float rad = angleDeg * Mathf.Deg2Rad;
                Vector2 currInner = loopCenter + new Vector2(Mathf.Cos(rad) * innerR, Mathf.Sin(rad) * innerR);

                Vector2 mid = (prevInner + currInner) * 0.5f;
                Vector2 dir = currInner - prevInner;
                float len = dir.magnitude;
                float rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                GameObject cPiece = new GameObject($"InnerCurb_{i}");
                cPiece.transform.SetParent(innerCurbs.transform, false);
                cPiece.transform.position = new Vector3(mid.x, mid.y, 0f);
                cPiece.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
                CreateSprite(cPiece.transform, "Curb", Vector3.zero, new Vector3(len + 0.12f, 0.35f, 1f), curbCol, 1);

                prevInner = currInner;
            }
        }

        private static void BuildRoadsideSignsAndArrows(Transform world)
        {
            GameObject signsContainer = new GameObject("RoadsideSigns");
            signsContainer.transform.SetParent(world, false);

            CreateSignboard(signsContainer.transform, "Sign_SpeedLimit_40", new Vector3(-41f, -38f, 0f), "SPEED LIMIT");
            CreateSignboard(signsContainer.transform, "Sign_RainRoad", new Vector3(-41f, -34f, 0f), "RAIN ROAD");
            CreateSignboard(signsContainer.transform, "Sign_SharpCurve_90", new Vector3(-41f, -22f, 0f), "SHARP CURVE");

            CreateAsphaltArrow(signsContainer.transform, "Arrow_SW_Ahead", new Vector3(-45f, -36f, 0f), 0f);
            CreateAsphaltArrow(signsContainer.transform, "Arrow_SW_TurnRight", new Vector3(-45f, -22f, 0f), 0f, true);

            CreateAsphaltArrow(signsContainer.transform, "Arrow_Connector_E1", new Vector3(-34f, -15f, 0f), -90f);
            CreateAsphaltArrow(signsContainer.transform, "Arrow_Connector_E2", new Vector3(-24f, -15f, 0f), -90f);

            CreateSignboard(signsContainer.transform, "Sign_Fork_Caution", new Vector3(31.5f, -6f, 0f), "RAIN ROAD");
            CreateSignboard(signsContainer.transform, "Sign_BranchA_Flood", new Vector3(20f, 3.5f, 0f), "RAIN ROAD");
            CreateSignboard(signsContainer.transform, "Sign_BranchB_Detour", new Vector3(41.5f, 15f, 0f), "SPEED LIMIT");

            CreateAsphaltArrow(signsContainer.transform, "Arrow_Approach_ForkL", new Vector3(23.5f, -5.5f, 0f), 25f);
            CreateAsphaltArrow(signsContainer.transform, "Arrow_Approach_ForkR", new Vector3(26.5f, -5.5f, 0f), -25f);

            CreateSignboard(signsContainer.transform, "Sign_Expressway_SpeedLimit", new Vector3(0f, 49.2f, 0f), "SPEED LIMIT");
            CreateSignboard(signsContainer.transform, "Sign_Blockage_NoEntry", new Vector3(36f, 49.2f, 0f), "NO ENTRY");
            CreateSignboard(signsContainer.transform, "Sign_Blockage_Stop", new Vector3(38f, 49.2f, 0f), "STOP");

            CreateAsphaltArrow(signsContainer.transform, "Arrow_UTurn_Entrance", new Vector3(24f, 43.5f, 0f), 180f);
            CreateAsphaltArrow(signsContainer.transform, "Arrow_UTurn_Exit", new Vector3(36f, 45f, 0f), 90f);
        }

        private static void CreateSignboard(Transform parent, string name, Vector3 pos, string signResourceName)
        {
            GameObject signObj = new GameObject(name);
            signObj.transform.SetParent(parent, false);
            signObj.transform.position = pos;

            CreateSprite(signObj.transform, "Post", new Vector3(0f, -0.6f, 0f), new Vector3(0.16f, 1.2f, 1f), new Color(0.35f, 0.38f, 0.42f, 1f), 12);
            CreateSprite(signObj.transform, "Base", new Vector3(0f, -1.2f, 0f), new Vector3(0.7f, 0.2f, 1f), new Color(0.28f, 0.30f, 0.32f, 1f), 12);

            Sprite signSprite = Resources.Load<Sprite>($"Signs/{signResourceName}");
            GameObject face = new GameObject("Face");
            face.transform.SetParent(signObj.transform, false);
            face.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            face.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            SpriteRenderer sr = face.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 14;
            if (signSprite != null)
            {
                sr.sprite = signSprite;
                sr.color = Color.white;
            }
            else
            {
                sr.sprite = GetOrCreateSquare();
                sr.color = new Color(0.95f, 0.85f, 0.15f, 1f);
            }
        }

        private static void CreateAsphaltArrow(Transform parent, string name, Vector3 pos, float rotationZ, bool isTurnRight = false)
        {
            GameObject arrowObj = new GameObject(name);
            arrowObj.transform.SetParent(parent, false);
            arrowObj.transform.position = pos;
            arrowObj.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);

            Color arrowCol = new Color(0.92f, 0.94f, 0.96f, 0.88f);

            if (!isTurnRight)
            {
                CreateSprite(arrowObj.transform, "Shaft", new Vector3(0f, -0.35f, 0f), new Vector3(0.28f, 1.3f, 1f), arrowCol, 3);
                GameObject leftBarb = CreateSprite(arrowObj.transform, "LeftBarb", new Vector3(-0.24f, 0.28f, 0f), new Vector3(0.24f, 0.75f, 1f), arrowCol, 3);
                leftBarb.transform.localRotation = Quaternion.Euler(0f, 0f, 36f);
                GameObject rightBarb = CreateSprite(arrowObj.transform, "RightBarb", new Vector3(0.24f, 0.28f, 0f), new Vector3(0.24f, 0.75f, 1f), arrowCol, 3);
                rightBarb.transform.localRotation = Quaternion.Euler(0f, 0f, -36f);
                CreateSprite(arrowObj.transform, "Tip", new Vector3(0f, 0.58f, 0f), new Vector3(0.32f, 0.35f, 1f), arrowCol, 3);
            }
            else
            {
                CreateSprite(arrowObj.transform, "ShaftV", new Vector3(-0.35f, -0.3f, 0f), new Vector3(0.28f, 1.1f, 1f), arrowCol, 3);
                CreateSprite(arrowObj.transform, "ShaftCorner", new Vector3(-0.1f, 0.25f, 0f), new Vector3(0.6f, 0.28f, 1f), arrowCol, 3);
                CreateSprite(arrowObj.transform, "ShaftH", new Vector3(0.35f, 0.25f, 0f), new Vector3(0.8f, 0.28f, 1f), arrowCol, 3);
                GameObject topBarb = CreateSprite(arrowObj.transform, "TopBarb", new Vector3(0.62f, 0.45f, 0f), new Vector3(0.24f, 0.65f, 1f), arrowCol, 3);
                topBarb.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                GameObject bottomBarb = CreateSprite(arrowObj.transform, "BottomBarb", new Vector3(0.62f, 0.05f, 0f), new Vector3(0.24f, 0.65f, 1f), arrowCol, 3);
                bottomBarb.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
                CreateSprite(arrowObj.transform, "Tip", new Vector3(0.85f, 0.25f, 0f), new Vector3(0.32f, 0.35f, 1f), arrowCol, 3);
            }
        }

        private static void BuildWorldBounds(Transform world)
        {
            GameObject bounds = new GameObject("WorldBounds");
            bounds.transform.SetParent(world, false);

            CreateWall(bounds.transform, "Wall_N", new Vector3(0f, 60f, 0f), new Vector2(170f, 4f));
            CreateWall(bounds.transform, "Wall_S", new Vector3(0f, -60f, 0f), new Vector2(170f, 4f));
            CreateWall(bounds.transform, "Wall_E", new Vector3(80f, 0f, 0f), new Vector2(4f, 130f));
            CreateWall(bounds.transform, "Wall_W", new Vector3(-80f, 0f, 0f), new Vector2(4f, 130f));

            // Roadside Containment Colliders along curbs
            CreateWall(bounds.transform, "CurbCol_SW_Left", new Vector3(-49.1f, -31.8f, 0f), new Vector2(0.8f, 28f));
            CreateWall(bounds.transform, "CurbCol_SW_Corner_Top", new Vector3(-45f, -11.0f, 0f), new Vector2(8.5f, 0.8f));
            CreateWall(bounds.transform, "CurbCol_Connector_Bottom", new Vector3(-30f, -19.1f, 0f), new Vector2(24f, 0.8f));
            CreateWall(bounds.transform, "CurbCol_Connector_Top", new Vector3(-30f, -11.0f, 0f), new Vector2(24f, 0.8f));
            CreateWall(bounds.transform, "CurbCol_TJunction_Bottom", new Vector3(-15f, -19.1f, 0f), new Vector2(8.5f, 0.8f));
            CreateWall(bounds.transform, "CurbCol_TJunction_LegN_Left", new Vector3(-19.1f, 1.05f, 0f), new Vector2(0.8f, 25f));
            CreateWall(bounds.transform, "CurbCol_TJunction_LegN_Right", new Vector3(-10.9f, 1.05f, 0f), new Vector2(0.8f, 25f));

            // Fork Approach & Detour Outer Colliders
            CreateWall(bounds.transform, "CurbCol_ForkApproach_Left", new Vector3(20.9f, -6f, 0f), new Vector2(0.8f, 8f));
            CreateWall(bounds.transform, "CurbCol_ForkApproach_Right", new Vector3(29.1f, -6f, 0f), new Vector2(0.8f, 8f));
            CreateWall(bounds.transform, "CurbCol_Detour_East", new Vector3(42.1f, 15f, 0f), new Vector2(0.8f, 22f));
            CreateWall(bounds.transform, "CurbCol_Detour_North", new Vector3(19f, 29.1f, 0f), new Vector2(40f, 0.8f));

            CreateWall(bounds.transform, "CurbCol_Expressway_North", new Vector3(16.5f, 49.1f, 0f), new Vector2(64f, 0.8f));
            CreateWall(bounds.transform, "CurbCol_Expressway_DeadEnd", new Vector3(48.5f, 45f, 0f), new Vector2(0.8f, 8f));
            CreateWall(bounds.transform, "CurbCol_Expressway_SouthWest", new Vector3(3.75f, 40.9f, 0f), new Vector2(37.5f, 0.8f));
            CreateWall(bounds.transform, "CurbCol_Expressway_SouthEast", new Vector3(42.75f, 40.9f, 0f), new Vector2(10.5f, 0.8f));

            CreateWall(bounds.transform, "CurbCol_UTurn_OuterApex", new Vector3(30f, 29.8f, 0f), new Vector2(16f, 0.8f));
            CreateWall(bounds.transform, "CurbCol_UTurn_OuterLeft", new Vector3(18.5f, 36f, 0f), new Vector2(0.8f, 12f));
            CreateWall(bounds.transform, "CurbCol_UTurn_OuterRight", new Vector3(41.5f, 36f, 0f), new Vector2(0.8f, 12f));
        }

        private static void CreateWall(Transform parent, string name, Vector3 pos, Vector2 size)
        {
            GameObject w = new GameObject(name);
            w.transform.SetParent(parent, false);
            w.transform.position = pos;
            BoxCollider2D col = w.AddComponent<BoxCollider2D>();
            col.size = size;
        }
        #endregion

        #region Detection Zones
        private static void BuildDetectionZones(Transform world)
        {
            GameObject zones = new GameObject("DetectionZones");
            zones.transform.SetParent(world, false);

            // U-Turn zone
            GameObject utObj = new GameObject("UTurn_DetectionZone");
            utObj.transform.SetParent(zones.transform, false);
            utObj.transform.position = new Vector3(30f, 36f, 0f);
            CircleCollider2D utCol = utObj.AddComponent<CircleCollider2D>();
            utCol.radius = 7.0f;
            utCol.isTrigger = true;
            utObj.AddComponent<UTurnDetectionZone>();

            // Roundabout zone
            GameObject rbObj = new GameObject("Roundabout_DetectionZone");
            rbObj.transform.SetParent(zones.transform, false);
            rbObj.transform.position = new Vector3(-15f, 25f, 0f);
            CircleCollider2D rbCol = rbObj.AddComponent<CircleCollider2D>();
            rbCol.radius = 12.5f;
            rbCol.isTrigger = true;
            RoundaboutDetectionZone rbZone = rbObj.AddComponent<RoundaboutDetectionZone>();
            SetRef(rbZone, "roundaboutCenter", new Vector2(-15f, 25f));

            // Fork zone
            GameObject forkObj = new GameObject("Fork_DetectionZone");
            forkObj.transform.SetParent(zones.transform, false);
            forkObj.transform.position = new Vector3(25f, -3f, 0f);
            BoxCollider2D forkCol = forkObj.AddComponent<BoxCollider2D>();
            forkCol.size = new Vector2(9f, 6f);
            forkCol.isTrigger = true;
            JunctionDetectionZone forkZone = forkObj.AddComponent<JunctionDetectionZone>();
            SetRef(forkZone, "junctionName", "Decision Fork");
            SetRef(forkZone, "approachAdvice", "FORK AHEAD: Left route flooded. Detour via Right elevated route!");

            // T-Junction zone
            GameObject tjObj = new GameObject("TJunction_DetectionZone");
            tjObj.transform.SetParent(zones.transform, false);
            tjObj.transform.position = new Vector3(-15f, -9f, 0f);
            BoxCollider2D tjCol = tjObj.AddComponent<BoxCollider2D>();
            tjCol.size = new Vector2(8f, 6f);
            tjCol.isTrigger = true;
            JunctionDetectionZone tjZone = tjObj.AddComponent<JunctionDetectionZone>();
            SetRef(tjZone, "junctionName", "South-Central T-Junction");
            SetRef(tjZone, "approachAdvice", "⊥ T-Junction ahead: Yield to cross traffic & slow down in rain!");
        }
        #endregion

        #region Hazards
        private static EnvironmentalHazardController BuildHazards(Transform world)
        {
            GameObject hazardObj = new GameObject("Hazards");
            hazardObj.transform.SetParent(world, false);
            EnvironmentalHazardController ctrl = hazardObj.AddComponent<EnvironmentalHazardController>();

            // Flood
            GameObject flood = new GameObject("FloodZone_BranchA");
            flood.transform.SetParent(hazardObj.transform, false);
            flood.transform.position = new Vector3(13f, 12f, 0f);
            flood.transform.rotation = Quaternion.Euler(0f, 0f, -45f);
            BoxCollider2D fCol = flood.AddComponent<BoxCollider2D>();
            fCol.size = new Vector2(7.2f, 14f);
            fCol.isTrigger = true;

            // Organic smooth flooded road water surface
            GameObject waterVis = new GameObject("WaterVisual");
            waterVis.transform.SetParent(flood.transform, false);
            Color waterBaseCol = new Color(0.12f, 0.42f, 0.68f, 0.72f);
            Color waterDeepCol = new Color(0.08f, 0.30f, 0.52f, 0.88f);
            Color waterCrestCol = new Color(0.65f, 0.88f, 1.0f, 0.40f);

            CreateSprite(waterVis.transform, "WaterSheet_Base", Vector3.zero, new Vector3(7.2f, 14.5f, 1f), waterBaseCol, 4);
            CreateSprite(waterVis.transform, "WaterSheet_Deep", Vector3.zero, new Vector3(5.6f, 11.5f, 1f), waterDeepCol, 5);
            CreateSprite(waterVis.transform, "Ripple_1", new Vector3(0f, 3.5f, 0f), new Vector3(4.8f, 0.45f, 1f), waterCrestCol, 6);
            CreateSprite(waterVis.transform, "Ripple_2", new Vector3(0f, 0f, 0f), new Vector3(5.2f, 0.50f, 1f), waterCrestCol, 6);
            CreateSprite(waterVis.transform, "Ripple_3", new Vector3(0f, -3.5f, 0f), new Vector3(4.5f, 0.45f, 1f), waterCrestCol, 6);
            FloodHazardZone fZone = flood.AddComponent<FloodHazardZone>();

            // Mud
            GameObject mud1 = new GameObject("MudZone_1");
            mud1.transform.SetParent(hazardObj.transform, false);
            mud1.transform.position = new Vector3(5f, -22f, 0f);
            BoxCollider2D mCol1 = mud1.AddComponent<BoxCollider2D>();
            mCol1.size = new Vector2(14f, 5f);
            mCol1.isTrigger = true;
            GameObject mudVis1 = new GameObject("MudVisual");
            mudVis1.transform.SetParent(mud1.transform, false);
            BuildOrganicMudPuddle(mudVis1.transform, new Vector2(14f, 5f), true);
            MudHazardZone mZone1 = mud1.AddComponent<MudHazardZone>();

            GameObject mud2 = new GameObject("MudZone_2");
            mud2.transform.SetParent(hazardObj.transform, false);
            mud2.transform.position = new Vector3(41f, 12f, 0f);
            BoxCollider2D mCol2 = mud2.AddComponent<BoxCollider2D>();
            mCol2.size = new Vector2(5f, 12f);
            mCol2.isTrigger = true;
            GameObject mudVis2 = new GameObject("MudVisual");
            mudVis2.transform.SetParent(mud2.transform, false);
            BuildOrganicMudPuddle(mudVis2.transform, new Vector2(5f, 12f), false);
            MudHazardZone mZone2 = mud2.AddComponent<MudHazardZone>();

            // Fallen Tree Blockage
            GameObject tree = new GameObject("FallenTree_NorthernBlockage");
            tree.transform.SetParent(hazardObj.transform, false);
            tree.transform.position = new Vector3(42f, 45f, 0f);
            BoxCollider2D tCol = tree.AddComponent<BoxCollider2D>();
            tCol.size = new Vector2(4f, 7.5f);
            tCol.isTrigger = false;
            CreateSprite(tree.transform, "Trunk", Vector3.zero, new Vector3(3.5f, 7.5f, 1f), new Color(0.35f, 0.22f, 0.12f, 1f), 5);
            CreateSprite(tree.transform, "Leaves", new Vector3(0.5f, 0.5f, 0f), new Vector3(5f, 6.5f, 1f), new Color(0.18f, 0.38f, 0.18f, 0.9f), 6, true);
            FallenTreeBlockage tBlock = tree.AddComponent<FallenTreeBlockage>();

            // Barricade
            GameObject barrier = new GameObject("Barricade_NorthernExpressway");
            barrier.transform.SetParent(hazardObj.transform, false);
            barrier.transform.position = new Vector3(38f, 45f, 0f);
            BoxCollider2D bCol = barrier.AddComponent<BoxCollider2D>();
            bCol.size = new Vector2(1.5f, 7.2f);
            bCol.isTrigger = false;
            CreateSprite(barrier.transform, "BarWood", Vector3.zero, new Vector3(1.2f, 7.2f, 1f), new Color(0.85f, 0.45f, 0.1f, 1f), 5);
            RoadClosureBarrier bBarrier = barrier.AddComponent<RoadClosureBarrier>();

            // Debris
            GameObject debris = new GameObject("Debris_Highway");
            debris.transform.SetParent(hazardObj.transform, false);
            debris.transform.position = new Vector3(10f, 45.5f, 0f);
            CircleCollider2D debCol = debris.AddComponent<CircleCollider2D>();
            debCol.radius = 1.2f;
            debCol.isTrigger = true;
            CreateSprite(debris.transform, "DebrisVisual", Vector3.zero, new Vector3(2f, 1.2f, 1f), new Color(0.3f, 0.3f, 0.3f, 0.8f), 5);
            DebrisHazard debHazard = debris.AddComponent<DebrisHazard>();

            ctrl.BindHazards(
                new FloodHazardZone[] { fZone },
                new MudHazardZone[] { mZone1, mZone2 },
                new FallenTreeBlockage[] { tBlock },
                new RoadClosureBarrier[] { bBarrier },
                new DebrisHazard[] { debHazard }
            );

            return ctrl;
        }

        private static void BuildOrganicMudPuddle(Transform parent, Vector2 size, bool isHorizontal)
        {
            Color mudDark = new Color(0.24f, 0.15f, 0.08f, 0.88f);
            Color mudMedium = new Color(0.35f, 0.23f, 0.13f, 0.78f);
            Color mudLight = new Color(0.44f, 0.30f, 0.18f, 0.60f);

            int count = isHorizontal ? Mathf.RoundToInt(size.x / 2.2f) : Mathf.RoundToInt(size.y / 2.2f);
            for (int i = 0; i < count; i++)
            {
                float t = (i / (float)Mathf.Max(1, count - 1)) - 0.5f;
                float primaryPos = t * (isHorizontal ? (size.x - 2f) : (size.y - 2f));
                float jitter = Mathf.Sin(i * 1.7f) * 0.7f;
                Vector3 pos = isHorizontal ? new Vector3(primaryPos, jitter, 0f) : new Vector3(jitter, primaryPos, 0f);
                float radius = 3.0f + Mathf.Sin(i * 1.3f) * 0.6f;

                CreateSprite(parent, $"MudDisc_{i}", pos, new Vector3(radius, radius, 1f), mudMedium, 4, true);
                CreateSprite(parent, $"MudCore_{i}", pos + new Vector3(0.15f, -0.1f, 0f), new Vector3(radius * 0.6f, radius * 0.6f, 1f), mudDark, 5, true);

                Vector3 spPos1 = pos + new Vector3(Mathf.Cos(i) * 1.2f, Mathf.Sin(i) * 1.2f, 0f);
                CreateSprite(parent, $"MudSplatA_{i}", spPos1, new Vector3(0.6f, 0.6f, 1f), mudLight, 4, true);
                Vector3 spPos2 = pos + new Vector3(Mathf.Sin(i * 2) * 1.1f, -Mathf.Cos(i * 2) * 1.1f, 0f);
                CreateSprite(parent, $"MudSplatB_{i}", spPos2, new Vector3(0.45f, 0.45f, 1f), mudDark, 5, true);
            }
        }
        #endregion

        #region Weather & Destination
        private static WeatherController BuildWeather(Transform world, Light2D light, Camera cam)
        {
            GameObject weather = new GameObject("WeatherSystem");
            weather.transform.SetParent(world, false);
            WeatherController wc = weather.AddComponent<WeatherController>();
            wc.BindGlobalLight(light);

            Shader rainShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            Material rainMat = new Material(rainShader);
            rainMat.color = new Color(0.72f, 0.82f, 0.94f, 0.75f);

            GameObject rainObj = new GameObject("RainParticles");
            rainObj.transform.SetParent(cam.transform, false);
            rainObj.transform.localPosition = new Vector3(0f, 0f, 5f);
            ParticleSystem ps = rainObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 1.2f;
            main.startSpeed = 18f;
            main.startSize = 0.35f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 350;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.65f, 0.76f, 0.88f, 0.60f),
                new Color(0.78f, 0.86f, 0.95f, 0.80f)
            );

            var emission = ps.emission;
            emission.rateOverTime = 140f;
            emission.enabled = true;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(25f, 2f, 1f);
            shape.position = new Vector3(0f, 12f, 0f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-3f);
            vel.y = new ParticleSystem.MinMaxCurve(-18f);

            var psRenderer = rainObj.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null)
            {
                psRenderer.sharedMaterial = rainMat;
                psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                psRenderer.velocityScale = 0.05f;
                psRenderer.lengthScale = 1.8f;
                psRenderer.sortingOrder = 15;
            }

            wc.BindRainParticles(ps);

            // Subtle fog atmosphere overlay
            GameObject fogObj = new GameObject("FogAtmosphereOverlay");
            fogObj.transform.SetParent(cam.transform, false);
            fogObj.transform.localPosition = new Vector3(0f, 0f, 2f);
            fogObj.transform.localScale = new Vector3(36f, 24f, 1f);
            SpriteRenderer fogSr = fogObj.AddComponent<SpriteRenderer>();
            fogSr.sprite = GetOrCreateSquare();
            fogSr.color = new Color(0.72f, 0.78f, 0.85f, 0f);
            fogSr.sortingOrder = 12;

            wc.BindFogOverlay(fogSr);

            return wc;
        }

        private static Level6Destination BuildDestination(Transform world)
        {
            GameObject dest = new GameObject("DestinationGoal");
            dest.transform.SetParent(world, false);
            dest.transform.position = new Vector3(25f, -15f, 0f);

            CircleCollider2D col = dest.AddComponent<CircleCollider2D>();
            col.radius = 2.5f;
            col.isTrigger = true;

            GameObject halo = CreateSprite(dest.transform, "Halo", Vector3.zero, new Vector3(2.4f, 2.4f, 1f), new Color(0.2f, 0.8f, 1f, 0.45f), 3, true);
            CreateSprite(dest.transform, "Pin", Vector3.zero, new Vector3(1.2f, 1.2f, 1f), new Color(1f, 0.90f, 0.2f, 0.9f), 3, true);

            Level6Destination ld = dest.AddComponent<Level6Destination>();
            ld.BindVisual(halo.transform);
            return ld;
        }
        #endregion

        #region Player Car
        private static GameObject BuildPlayerCar(Transform world)
        {
            GameObject car = new GameObject("PlayerCar");
            car.transform.SetParent(world, false);
            car.transform.position = new Vector3(-45f, -35f, 0f);
            car.tag = "Player";

            SpriteRenderer sr = car.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Vehicles/CarBlueTopDown") ?? GetOrCreateCarSprite();
            sr.sortingOrder = 10;
            car.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            Rigidbody2D rb = car.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D col = car.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.6f, 3.2f);

            Level6PlayerCar pc = car.AddComponent<Level6PlayerCar>();

            // Headlights
            CreateHeadlight(car.transform, "HL_L", new Vector3(-0.45f, 1.4f, 0f));
            CreateHeadlight(car.transform, "HL_R", new Vector3(0.45f, 1.4f, 0f));

            SpriteRenderer bl1 = CreateBrakeLight(car.transform, "BL_L", new Vector3(-0.65f, -1.6f, 0f));
            SpriteRenderer bl2 = CreateBrakeLight(car.transform, "BL_R", new Vector3(0.65f, -1.6f, 0f));
            SetObjectArray(pc, "brakeLights", new[] { bl1, bl2 });

            return car;
        }

        private static void CreateHeadlight(Transform parent, string name, Vector3 pos)
        {
            GameObject hl = new GameObject(name);
            hl.transform.SetParent(parent, false);
            hl.transform.localPosition = pos;
            hl.transform.localScale = new Vector3(1.2f, 2.5f, 1f);
            SpriteRenderer sr = hl.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Environment/HeadlightBeamCone") ?? GetOrCreateSquare();
            sr.color = new Color(1f, 1f, 0.85f, 0.09f);
            sr.sortingOrder = 11;
        }

        private static SpriteRenderer CreateBrakeLight(Transform parent, string name, Vector3 pos)
        {
            GameObject bl = new GameObject(name);
            bl.transform.SetParent(parent, false);
            bl.transform.localPosition = pos;
            bl.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            SpriteRenderer sr = bl.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateCircle();
            sr.color = new Color(0.6f, 0.1f, 0.1f, 0.6f);
            sr.sortingOrder = 12;
            return sr;
        }
        #endregion

        #region UI
        private static Level6UIController BuildUI(Level6PlayerCar car, Level6Destination dest)
        {
            GameObject canvasObj = new GameObject("Level6_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 1. Mission Card
            GameObject mCard = CreateUIPanel(canvasObj.transform, "MissionCard", new Vector2(0f, 1f), new Vector2(25f, -25f), new Vector2(460f, 165f), new Color(0.08f, 0.10f, 0.14f, 0.90f));
            TMP_Text mBadge = CreateUIText(mCard.transform, "Badge", "MISSION 1 / 5", 14f, TextAlignmentOptions.Left, new Vector2(280f, 22f), new Vector2(15f, -12f), new Vector2(0f, 1f));
            mBadge.color = new Color(1f, 0.85f, 0.2f, 1f);
            TMP_Text mTimer = CreateUIText(mCard.transform, "Timer", "TIME: 01:30", 15f, TextAlignmentOptions.Right, new Vector2(140f, 22f), new Vector2(-15f, -12f), new Vector2(1f, 1f));
            mTimer.fontStyle = FontStyles.Bold;
            TMP_Text mTitle = CreateUIText(mCard.transform, "Title", "RAIN SLICK ROADS", 17f, TextAlignmentOptions.Left, new Vector2(430f, 26f), new Vector2(15f, -38f), new Vector2(0f, 1f));
            mTitle.fontStyle = FontStyles.Bold;
            TMP_Text mObj = CreateUIText(mCard.transform, "Objective", "Control speed through the 90° turn and curved road.", 12.5f, TextAlignmentOptions.Left, new Vector2(430f, 44f), new Vector2(15f, -66f), new Vector2(0f, 1f));
            mObj.color = new Color(0.85f, 0.88f, 0.92f, 1f);
            TMP_Text mGps = CreateUIText(mCard.transform, "GPSGuidance", "GPS: ↑ EAST DEPOT (85m)", 13.5f, TextAlignmentOptions.Left, new Vector2(430f, 24f), new Vector2(15f, -125f), new Vector2(0f, 1f));
            mGps.fontStyle = FontStyles.Bold;
            mGps.color = new Color(1f, 0.9f, 0.3f, 1f);

            // 2. Telemetry Status
            GameObject tel = CreateUIPanel(canvasObj.transform, "Telemetry", new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(440f, 75f), new Color(0.08f, 0.10f, 0.14f, 0.88f));
            TMP_Text wStatus = CreateUIText(tel.transform, "WStatus", "HEAVY RAIN", 16f, TextAlignmentOptions.Center, new Vector2(200f, 25f), new Vector2(-105f, -12f), new Vector2(0.5f, 1f));
            wStatus.fontStyle = FontStyles.Bold;
            TMP_Text wWind = CreateUIText(tel.transform, "Wind", "WIND: 14 km/h E", 13f, TextAlignmentOptions.Center, new Vector2(200f, 20f), new Vector2(-105f, -42f), new Vector2(0.5f, 1f));
            TMP_Text gVal = CreateUIText(tel.transform, "GripVal", "Grip: 45%", 13f, TextAlignmentOptions.Center, new Vector2(180f, 20f), new Vector2(105f, -12f), new Vector2(0.5f, 1f));

            GameObject gBarBg = CreateUIPanel(tel.transform, "GripBg", new Vector2(0.5f, 1f), new Vector2(105f, -42f), new Vector2(160f, 14f), new Color(0.2f, 0.2f, 0.2f, 0.8f));
            GameObject gFillObj = new GameObject("Fill");
            gFillObj.transform.SetParent(gBarBg.transform, false);
            SetFull(gFillObj.AddComponent<RectTransform>());
            Image gFill = gFillObj.AddComponent<Image>();
            gFill.color = new Color(0.9f, 0.8f, 0.2f, 1f);
            TMP_Text hNotice = CreateUIText(tel.transform, "HNotice", "", 12f, TextAlignmentOptions.Center, new Vector2(400f, 18f), new Vector2(0f, -56f), new Vector2(0.5f, 1f));

            // 3. Safety & Score Card
            GameObject sCard = CreateUIPanel(canvasObj.transform, "ScoreCard", new Vector2(1f, 1f), new Vector2(-25f, -25f), new Vector2(360f, 130f), new Color(0.08f, 0.10f, 0.14f, 0.88f));
            TMP_Text sVal = CreateUIText(sCard.transform, "ScoreVal", "Score: 0", 17f, TextAlignmentOptions.Right, new Vector2(330f, 25f), new Vector2(-15f, -12f), new Vector2(1f, 1f));
            sVal.fontStyle = FontStyles.Bold;

            TMP_Text sfVal = CreateUIText(sCard.transform, "SafeVal", "Safety: 100%", 14f, TextAlignmentOptions.Left, new Vector2(160f, 20f), new Vector2(15f, -44f), new Vector2(0f, 1f));
            GameObject sfBg = CreateUIPanel(sCard.transform, "SafeBg", new Vector2(1f, 1f), new Vector2(-15f, -46f), new Vector2(160f, 14f), new Color(0.2f, 0.2f, 0.2f, 0.8f));
            GameObject sfFillObj = new GameObject("Fill");
            sfFillObj.transform.SetParent(sfBg.transform, false);
            SetFull(sfFillObj.AddComponent<RectTransform>());
            Image sfFill = sfFillObj.AddComponent<Image>();
            sfFill.type = Image.Type.Filled;
            sfFill.fillMethod = Image.FillMethod.Horizontal;
            sfFill.fillAmount = 1f;
            sfFill.color = new Color(0.2f, 0.85f, 0.3f, 1f);

            TMP_Text rsVal = CreateUIText(sCard.transform, "RouteVal", "Route Rating: 100%", 14f, TextAlignmentOptions.Left, new Vector2(160f, 20f), new Vector2(15f, -76f), new Vector2(0f, 1f));
            GameObject rsBg = CreateUIPanel(sCard.transform, "RouteBg", new Vector2(1f, 1f), new Vector2(-15f, -78f), new Vector2(160f, 14f), new Color(0.2f, 0.2f, 0.2f, 0.8f));
            GameObject rsFillObj = new GameObject("Fill");
            rsFillObj.transform.SetParent(rsBg.transform, false);
            SetFull(rsFillObj.AddComponent<RectTransform>());
            Image rsFill = rsFillObj.AddComponent<Image>();
            rsFill.type = Image.Type.Filled;
            rsFill.fillMethod = Image.FillMethod.Horizontal;
            rsFill.fillAmount = 1f;
            rsFill.color = new Color(0.3f, 0.7f, 1f, 1f);

            // 4. Speedometer
            GameObject spPanel = CreateUIPanel(canvasObj.transform, "Speedo", new Vector2(0f, 0f), new Vector2(25f, 25f), new Vector2(210f, 130f), new Color(0.08f, 0.10f, 0.14f, 0.88f));
            TMP_Text cSpeed = CreateUIText(spPanel.transform, "Speed", "0", 38f, TextAlignmentOptions.Center, new Vector2(120f, 50f), new Vector2(15f, 54f), new Vector2(0f, 0f));
            cSpeed.fontStyle = FontStyles.Bold;
            TMP_Text spKmh = CreateUIText(spPanel.transform, "KmhUnit", "KM/H", 12f, TextAlignmentOptions.Center, new Vector2(120f, 20f), new Vector2(15f, 30f), new Vector2(0f, 0f));
            spKmh.color = new Color(0.7f, 0.75f, 0.8f, 1f);
            TMP_Text spBadge = CreateUIText(spPanel.transform, "Badge", "MAX 40", 13f, TextAlignmentOptions.Center, new Vector2(70f, 24f), new Vector2(-15f, 68f), new Vector2(1f, 0f));
            spBadge.fontStyle = FontStyles.Bold;
            spBadge.color = new Color(1f, 0.85f, 0.2f, 1f);
            TMP_Text skWarning = CreateUIText(spPanel.transform, "Skid", "[!] SKIDDING!", 12f, TextAlignmentOptions.Center, new Vector2(190f, 20f), new Vector2(10f, 24f), new Vector2(0f, 0f));
            skWarning.fontStyle = FontStyles.Bold;
            skWarning.color = new Color(1f, 0.3f, 0.3f, 1f);
            TMP_Text offRoadWarn = CreateUIText(spPanel.transform, "OffRoad", "[!] OFF-ROAD! RETURN", 11.5f, TextAlignmentOptions.Center, new Vector2(190f, 20f), new Vector2(10f, 4f), new Vector2(0f, 0f));
            offRoadWarn.fontStyle = FontStyles.Bold;
            offRoadWarn.color = new Color(1f, 0.25f, 0.25f, 1f);
            offRoadWarn.gameObject.SetActive(false);

            // 5. Mini-Map
            GameObject mmPanel = CreateUIPanel(canvasObj.transform, "MiniMap", new Vector2(1f, 0f), new Vector2(-25f, 25f), new Vector2(220f, 160f), new Color(0.08f, 0.12f, 0.08f, 0.90f));

            GameObject pBlip = new GameObject("PlayerBlip");
            pBlip.transform.SetParent(mmPanel.transform, false);
            RectTransform pBlipRect = pBlip.AddComponent<RectTransform>();
            pBlipRect.sizeDelta = new Vector2(10f, 14f);
            Image pBlipImg = pBlip.AddComponent<Image>();
            pBlipImg.color = new Color(0.2f, 0.9f, 0.3f, 1f);

            GameObject dBlip = new GameObject("DestBlip");
            dBlip.transform.SetParent(mmPanel.transform, false);
            RectTransform dBlipRect = dBlip.AddComponent<RectTransform>();
            dBlipRect.sizeDelta = new Vector2(12f, 12f);
            Image dBlipImg = dBlip.AddComponent<Image>();
            dBlipImg.color = new Color(1f, 0.85f, 0.2f, 1f);

            Level6MiniMapController miniMapCtrl = mmPanel.AddComponent<Level6MiniMapController>();
            miniMapCtrl.BindReferences(car, dest, mmPanel.GetComponent<RectTransform>(), pBlipRect, dBlipRect);

            // 6. Feedback Banner
            GameObject fBanner = CreateUIPanel(canvasObj.transform, "FeedbackBanner", new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(650f, 45f), new Color(0.06f, 0.08f, 0.12f, 0.94f));
            CanvasGroup fGroup = fBanner.AddComponent<CanvasGroup>();
            TMP_Text fMsg = CreateUIText(fBanner.transform, "Msg", "Drive cautiously under rain.", 15f, TextAlignmentOptions.Center, new Vector2(630f, 35f), Vector2.zero, new Vector2(0.5f, 0.5f));

            // 7. Modals
            GameObject failModal = CreateFailModal(canvasObj.transform, out TMP_Text fReason, out Button fRetry, out Button fMenu);
            GameObject compModal = CreateCompletionModal(canvasObj.transform,
                out TMP_Text cTitle, out TMP_Text cSub, out TMP_Text cStars, out TMP_Text cScore,
                out TMP_Text cSafety, out TMP_Text cRoute, out TMP_Text cCollisions,
                out TMP_Text cHazards, out TMP_Text cUTurns, out Button cReplay, out Button cMenu2);

            Level6UIController uiCtrl = canvasObj.AddComponent<Level6UIController>();
            uiCtrl.BindDynamicReferences(
                mBadge, mTitle, mObj, mTimer,
                wStatus, gFill, gVal, wWind, hNotice,
                sVal, sfVal, sfFill, rsVal, rsFill,
                cSpeed, spBadge, skWarning,
                fBanner, fGroup, fMsg,
                failModal, fReason, fRetry, fMenu,
                compModal, cTitle, cSub, cStars, cScore,
                cSafety, cRoute, cCollisions, cHazards, cUTurns,
                cReplay, cMenu2,
                mGps, offRoadWarn
            );

            return uiCtrl;
        }

        private static GameObject CreateFailModal(Transform canvas, out TMP_Text fReason, out Button fRetry, out Button fMenu)
        {
            GameObject modal = CreateUIPanel(canvas, "MissionFailModal", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 320f), new Color(0.12f, 0.06f, 0.06f, 0.96f));
            TMP_Text title = CreateUIText(modal.transform, "Title", "[!] MISSION FAILED", 24f, TextAlignmentOptions.Center, new Vector2(460f, 40f), new Vector2(0f, -25f), new Vector2(0.5f, 1f));
            title.color = new Color(1f, 0.35f, 0.35f, 1f);
            title.fontStyle = FontStyles.Bold;

            fReason = CreateUIText(modal.transform, "Reason", "Extreme conditions overwhelmed the journey.", 15f, TextAlignmentOptions.Center, new Vector2(440f, 80f), new Vector2(0f, -80f), new Vector2(0.5f, 1f));
            fRetry = CreateUIButton(modal.transform, "RetryBtn", "RETRY MISSION", new Vector2(0f, -185f), new Vector2(220f, 45f), new Color(0.85f, 0.3f, 0.2f, 1f));
            fMenu = CreateUIButton(modal.transform, "MenuBtn", "MAIN MENU", new Vector2(0f, -245f), new Vector2(220f, 45f), new Color(0.25f, 0.3f, 0.38f, 1f));

            modal.SetActive(false);
            return modal;
        }

        private static GameObject CreateCompletionModal(Transform canvas,
            out TMP_Text cTitle, out TMP_Text cSub, out TMP_Text cStars, out TMP_Text cScore,
            out TMP_Text cSafety, out TMP_Text cRoute, out TMP_Text cCollisions,
            out TMP_Text cHazards, out TMP_Text cUTurns, out Button cReplay, out Button cMenu)
        {
            GameObject modal = CreateUIPanel(canvas, "CompletionCertificateModal", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 540f), new Color(0.08f, 0.12f, 0.18f, 0.98f));
            cTitle = CreateUIText(modal.transform, "Title", "LEVEL 6 COMPLETED", 24f, TextAlignmentOptions.Center, new Vector2(560f, 35f), new Vector2(0f, -25f), new Vector2(0.5f, 1f));
            cTitle.color = new Color(1f, 0.85f, 0.2f, 1f);
            cTitle.fontStyle = FontStyles.Bold;

            cSub = CreateUIText(modal.transform, "Sub", "EXTREME ROAD CONDITIONS MASTER CERTIFICATE", 13f, TextAlignmentOptions.Center, new Vector2(560f, 25f), new Vector2(0f, -60f), new Vector2(0.5f, 1f));
            cSub.color = new Color(0.7f, 0.85f, 1f, 1f);

            cStars = CreateUIText(modal.transform, "Stars", "***", 42f, TextAlignmentOptions.Center, new Vector2(560f, 50f), new Vector2(0f, -95f), new Vector2(0.5f, 1f));
            cStars.color = new Color(1f, 0.85f, 0.2f, 1f);

            cScore = CreateUIText(modal.transform, "FinalScore", "Final Score: 450", 18f, TextAlignmentOptions.Center, new Vector2(500f, 30f), new Vector2(0f, -160f), new Vector2(0.5f, 1f));
            cScore.fontStyle = FontStyles.Bold;

            cSafety = CreateUIText(modal.transform, "FinalSafety", "Overall Safety: 95%", 15f, TextAlignmentOptions.Left, new Vector2(250f, 25f), new Vector2(65f, -205f), new Vector2(0f, 1f));
            cRoute = CreateUIText(modal.transform, "FinalRoute", "Route Selection: 100%", 15f, TextAlignmentOptions.Left, new Vector2(250f, 25f), new Vector2(330f, -205f), new Vector2(0f, 1f));
            cCollisions = CreateUIText(modal.transform, "FinalCollisions", "Collisions: 0", 15f, TextAlignmentOptions.Left, new Vector2(250f, 25f), new Vector2(65f, -240f), new Vector2(0f, 1f));
            cHazards = CreateUIText(modal.transform, "FinalHazards", "Hazards Avoided: 5", 15f, TextAlignmentOptions.Left, new Vector2(250f, 25f), new Vector2(330f, -240f), new Vector2(0f, 1f));
            cUTurns = CreateUIText(modal.transform, "FinalUTurns", "Safe U-Turns: 1", 15f, TextAlignmentOptions.Center, new Vector2(500f, 25f), new Vector2(0f, -275f), new Vector2(0.5f, 1f));

            cReplay = CreateUIButton(modal.transform, "ReplayBtn", "REPLAY LEVEL", new Vector2(-125f, -440f), new Vector2(210f, 48f), new Color(0.18f, 0.55f, 0.32f, 1f));
            cMenu = CreateUIButton(modal.transform, "MenuBtn", "MAIN MENU", new Vector2(125f, -440f), new Vector2(210f, 48f), new Color(0.22f, 0.38f, 0.58f, 1f));

            modal.SetActive(false);
            return modal;
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color col)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = panel.AddComponent<Image>();
            img.color = col;
            return panel;
        }

        private static Button CreateUIButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color col)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = col;

            Button btn = btnObj.AddComponent<Button>();

            TMP_Text txt = CreateUIText(btnObj.transform, "Text", text, 15f, TextAlignmentOptions.Center, size, Vector2.zero, new Vector2(0.5f, 0.5f));
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;

            return btn;
        }

        private static TMP_Text CreateUIText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align, Vector2 size, Vector2 anchoredPos, Vector2 pivot)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            RectTransform rt = tmp.rectTransform;
            rt.anchorMin = pivot;
            rt.anchorMax = pivot;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            return tmp;
        }

        private static void SetFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
        #endregion

        #region Helpers & Texture Synthesis
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

        private static Sprite squareCache;
        private static Sprite circleCache;
        private static Sprite carCache;

        private static Sprite GetOrCreateSquare()
        {
            if (squareCache == null)
            {
                Texture2D tex = new Texture2D(2, 2);
                tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
                tex.Apply();
                squareCache = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f);
            }
            return squareCache;
        }

        private static Sprite GetOrCreateCircle()
        {
            if (circleCache == null)
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
                circleCache = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 1f);
            }
            return circleCache;
        }

        private static Sprite GetOrCreateCarSprite()
        {
            if (carCache == null)
            {
                int w = 24, h = 48;
                Texture2D tex = new Texture2D(w, h);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Color c = new Color(0.2f, 0.5f, 0.9f, 1f); // Blue car
                        // Windshield
                        if (y >= 26 && y <= 34 && x >= 4 && x <= 19) c = new Color(0.1f, 0.15f, 0.25f, 1f);
                        // Rear window
                        if (y >= 10 && y <= 16 && x >= 4 && x <= 19) c = new Color(0.1f, 0.15f, 0.25f, 1f);
                        tex.SetPixel(x, y, c);
                    }
                }
                tex.Apply();
                carCache = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
            }
            return carCache;
        }

        private static Sprite LoadSprite(string path)
        {
            return Resources.Load<Sprite>(path);
        }

        private static void SetRef(object target, string fieldName, object value)
        {
            if (target == null) return;
            FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fi != null) fi.SetValue(target, value);
        }

        private static void SetObjectArray(object target, string fieldName, object[] value)
        {
            if (target == null) return;
            FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fi != null)
            {
                System.Array dest = System.Array.CreateInstance(fi.FieldType.GetElementType(), value.Length);
                for (int i = 0; i < value.Length; i++) dest.SetValue(value[i], i);
                fi.SetValue(target, dest);
            }
        }
        #endregion
    }
}
