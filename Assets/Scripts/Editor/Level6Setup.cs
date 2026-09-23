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
using TrafficTown2D.Level6;
using TrafficTown2D.UI;

namespace TrafficTown2D.Editor
{
    public static class Level6Setup
    {
        private const string Level6ScenePath = "Assets/Scenes/Level6.unity";
        private const string WorldSquareSpritePath = "Assets/Sprites/Generated/WorldSquare.png";
        private const string WorldCircleSpritePath = "Assets/Sprites/Generated/WorldCircle.png";
        private const string CarBlueTopDownPath = "Assets/Sprites/Vehicles/CarBlueTopDown.png";
        private const string HeadlightBeamConePath = "Assets/Sprites/Environment/HeadlightBeamCone.png";
        private const string RainDropPath = "Assets/Sprites/Environment/RainDrop.png";
        private const string RainMaterialPath = "Assets/Materials/RainMaterial.mat";

        private const string AutoRunPrefKey = "TrafficTown_Level6_AutoSetup_v1";

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
                        Debug.Log("[Level6Setup] Auto-generating Level 6 scene on compile...");
                        SetupLevel6();
                    }
                };
            }
        }

        [MenuItem("TrafficTown/Setup Level 6")]
        public static void SetupLevel6()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before running TrafficTown -> Setup Level 6.");
                return;
            }

            EnsureAssetFolders();

            Scene level6Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(level6Scene, Level6ScenePath);

            // 1. Camera & Lighting
            Camera cam = EnsureCamera();
            Light2D globalLight = EnsureGlobalLight();
            EnsureEventSystem();

            // 2. World Root
            GameObject world = FindOrCreate("Environment");
            world.transform.position = Vector3.zero;

            // 3. Construct Road Network Geometry
            CreateRoadNetworkGeometry(world.transform);

            // 4. Construct Detection Zones (U-Turn, Roundabout, Junctions)
            CreateDetectionZones(world.transform);

            // 5. Construct Environmental Hazards (Floods, Mud, Fallen Trees, Barricades, Debris)
            EnvironmentalHazardController hazardCtrl = CreateEnvironmentalHazards(world.transform);

            // 6. Construct Weather Controller
            WeatherController weatherCtrl = CreateWeatherSystem(world.transform, globalLight, cam);

            // 7. Destination Marker
            Level6Destination destination = CreateDestination(world.transform);

            // 8. Player Car
            GameObject playerObj = CreatePlayerCar(world.transform);
            Level6PlayerCar playerCar = playerObj.GetComponent<Level6PlayerCar>();
            weatherCtrl.SetPlayerCar(playerCar);

            // Setup Camera Follow
            Level6CameraFollow camFollow = cam.gameObject.AddComponent<Level6CameraFollow>();
            camFollow.SetTarget(playerObj.transform);

            // 9. Safety Manager
            GameObject controllers = FindOrCreate("Controllers");
            controllers.transform.position = Vector3.zero;
            Level6SafetyManager safetyMgr = controllers.AddComponent<Level6SafetyManager>();
            Level6RoadNetwork roadNetwork = controllers.AddComponent<Level6RoadNetwork>();
            Level6GPSCompass gpsCompass = controllers.AddComponent<Level6GPSCompass>();

            // 10. UI Construction & Wiring
            Level6UIController uiCtrl = CreateUI(playerCar, destination);
            gpsCompass.BindReferences(playerCar, destination, null, null);

            // 11. Mission Manager
            Level6MissionManager missionMgr = controllers.AddComponent<Level6MissionManager>();
            SetReference(missionMgr, "playerCar", playerCar);
            SetReference(missionMgr, "destinationGoal", destination);
            SetReference(missionMgr, "weatherController", weatherCtrl);
            SetReference(missionMgr, "hazardController", hazardCtrl);
            SetReference(missionMgr, "uiController", uiCtrl);

            // 12. Add Runtime Bootstrap to scene
            GameObject bootObj = FindOrCreate("Level6Bootstrap");
            bootObj.AddComponent<Level6Bootstrap>();

            // 13. Sync build settings
            BuildSettingsUtility.EnsureAllScenesInBuildSettings();

            EditorSceneManager.MarkSceneDirty(level6Scene);
            EditorSceneManager.SaveScene(level6Scene);
            Debug.Log("[Level6Setup] Successfully built Level 6 -- Extreme Road Conditions");
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
            cam.transform.position = new Vector3(-45f, -35f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.22f, 0.36f, 0.22f, 1f); // Dark wet stormy landscape
            cam.cullingMask = ~0;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            cam.depth = 0;
            cam.targetDisplay = 0;
            cam.enabled = true;

            UniversalAdditionalCameraData camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderType = CameraRenderType.Base;
            camData.renderPostProcessing = false;

            return cam;
        }

        private static Light2D EnsureGlobalLight()
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

            globalLight.color = new Color(0.85f, 0.90f, 0.95f, 1f);
            globalLight.intensity = 0.85f;
            return globalLight;
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

        #region Road Network Construction
        private static void CreateRoadNetworkGeometry(Transform world)
        {
            Color roadCol = new Color(0.14f, 0.15f, 0.18f, 1f); // Deep rich asphalt
            Color curbCol = new Color(0.72f, 0.74f, 0.78f, 1f); // Crisp raised curbs for high contrast
            Color yellowLineCol = new Color(1f, 0.88f, 0.18f, 0.95f); // Vibrant double-yellow centerlines
            Color whiteLineCol = new Color(0.95f, 0.95f, 0.95f, 0.90f); // Clean white dashed markers
            Color grassCol = new Color(0.18f, 0.32f, 0.18f, 1f); // Natural green lawn
            Color shoulderCol = new Color(0.38f, 0.40f, 0.44f, 1f); // Road shoulder bed

            // 1. Massive Ground Grass Terrain
            CreateWorldSprite(world, "GroundGrass", Vector3.zero, new Vector3(180f, 130f, 1f), grassCol, -10);

            GameObject roadsContainer = FindOrCreateChild(world, "RoadNetwork");
            roadsContainer.transform.position = Vector3.zero;

            // Structure 1: South-West Avenue (Mission 1 Spawn) - stops cleanly before 90-deg turn at Y=-18.6
            CreateRoadSegment(roadsContainer.transform, "Road_SW_Avenue", new Vector3(-45f, -31.8f, 0f), new Vector2(7.2f, 26.4f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, shoulderCol, isHorizontal: false, hasCurb1: true, hasCurb2: true);

            // Structure 2: 90-Degree Turn at (-45, -15) - covers (-48.6 to -41.4, -18.6 to -11.4)
            CreateWorldSprite(roadsContainer.transform, "Corner_90Deg_Shoulder", new Vector3(-45f, -15f, 0f), new Vector3(8.6f, 8.6f, 1f), shoulderCol, -2);
            CreateWorldSprite(roadsContainer.transform, "Corner_90Deg", new Vector3(-45f, -15f, 0f), new Vector3(7.4f, 7.4f, 1f), roadCol, 0);
            CreateWorldSprite(roadsContainer.transform, "Corner_TopCurb", new Vector3(-45f, -11.22f, 0f), new Vector3(7.4f, 0.35f, 1f), curbCol, 1);
            CreateWorldSprite(roadsContainer.transform, "Corner_LeftCurb", new Vector3(-48.78f, -15f, 0f), new Vector3(0.35f, 7.4f, 1f), curbCol, 1);

            // Structure 2b: South Connector from (-41.4, -15) to (-18.6, -15) (length 22.8, width 7.2)
            CreateRoadSegment(roadsContainer.transform, "Road_South_Connector", new Vector3(-30f, -15f, 0f), new Vector2(22.8f, 7.2f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, shoulderCol, isHorizontal: true, hasCurb1: true, hasCurb2: true);

            // Structure 3: T-Junction Box at (-15, -15) - covers (-18.6 to -11.4, -18.6 to -11.4)
            CreateWorldSprite(roadsContainer.transform, "TJunction_Shoulder", new Vector3(-15f, -15f, 0f), new Vector3(8.6f, 8.6f, 1f), shoulderCol, -2);
            CreateWorldSprite(roadsContainer.transform, "TJunction_Box", new Vector3(-15f, -15f, 0f), new Vector3(7.4f, 7.4f, 1f), roadCol, 0);
            CreateWorldSprite(roadsContainer.transform, "TJunction_BottomCurb", new Vector3(-15f, -18.78f, 0f), new Vector3(7.4f, 0.35f, 1f), curbCol, 1);

            // Structure 3b: T-Junction Leg N connecting north to roundabout: from Y=-11.4 up to Y=13.5 (length 24.9, width 7.2)
            CreateRoadSegment(roadsContainer.transform, "Road_TJunction_Leg_N", new Vector3(-15f, 1.05f, 0f), new Vector2(7.2f, 24.9f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, shoulderCol, isHorizontal: false, hasCurb1: true, hasCurb2: true);

            // Structure 4: Curved Road (S-Curve) across Southern Valley from (-11.4, -15) to (20, -15)
            CreateCurvedRoadSegment(roadsContainer.transform, new Vector2(-11.4f, -15f), new Vector2(20f, -15f), 7.2f, roadCol, curbCol, whiteLineCol, shoulderCol);

            // East Service Depot (Destination 1) at (25, -15) - covers X: 20 to 30, Y: -20 to -10
            CreateWorldSprite(roadsContainer.transform, "EastDepot_Shoulder", new Vector3(25f, -15f, 0f), new Vector3(11.2f, 11.2f, 1f), shoulderCol, -2);
            CreateWorldSprite(roadsContainer.transform, "EastDepot_Pad", new Vector3(25f, -15f, 0f), new Vector3(10f, 10f, 1f), new Color(0.22f, 0.25f, 0.28f, 1f), 0);
            CreateWorldSprite(roadsContainer.transform, "EastDepot_CurbS", new Vector3(25f, -20.18f, 0f), new Vector3(10.4f, 0.35f, 1f), curbCol, 1);
            CreateWorldSprite(roadsContainer.transform, "EastDepot_CurbE", new Vector3(30.18f, -15f, 0f), new Vector3(0.35f, 10.4f, 1f), curbCol, 1);

            // Road from East Depot up to Fork: from (25, -10) to (25, -2) (length 8.0, width 7.2)
            CreateRoadSegment(roadsContainer.transform, "Road_Fork_Approach", new Vector3(25f, -6f, 0f), new Vector2(7.2f, 8f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, shoulderCol, isHorizontal: false, hasCurb1: true, hasCurb2: true);

            // Structure 5: Decision Fork Junction at (25, 0)
            CreateWorldSprite(roadsContainer.transform, "Fork_Junction_Shoulder", new Vector3(26f, 2f, 0f), new Vector3(15f, 10f, 1f), shoulderCol, -2);
            CreateWorldSprite(roadsContainer.transform, "Fork_Junction_Pad", new Vector3(26f, 2f, 0f), new Vector3(14f, 9f, 1f), roadCol, 0);

            // Traffic Splitter Island between Branch A and Branch B
            CreateWorldSprite(roadsContainer.transform, "Fork_Splitter_Shoulder", new Vector3(26f, 3.5f, 0f), new Vector3(3.6f, 4.5f, 1f), shoulderCol, 0);
            CreateWorldSprite(roadsContainer.transform, "Fork_Splitter_Curb", new Vector3(26f, 3.5f, 0f), new Vector3(3.2f, 4.0f, 1f), curbCol, 1);
            CreateWorldSprite(roadsContainer.transform, "Fork_Splitter_Center", new Vector3(26f, 3.5f, 0f), new Vector3(2.5f, 3.2f, 1f), grassCol, 2);

            // Branch A: Direct Hazardous / Flooded Route from (25, 0) diagonally to (0, 25)
            // Midpoint: (12.5, 12.5), Length: 35.35m, Angle: -45 deg, Width: 7.2m
            CreateRoadSegment(roadsContainer.transform, "Road_Fork_HazardBranch", new Vector3(12.5f, 12.5f, 0f), new Vector2(7.2f, 35.35f), -45f, roadCol, curbCol, yellowLineCol, whiteLineCol, shoulderCol, isHorizontal: false, hasCurb1: true, hasCurb2: false);
            GameObject branchAObj = FindOrCreateChild(roadsContainer.transform, "Road_Fork_HazardBranch");
            CreateWorldSprite(branchAObj.transform, "Curb_Right_Short", new Vector3(3.78f, 0f, 0f), new Vector3(0.35f, 16f, 1f), curbCol, 1);

            // Branch B: Elevated Safe Detour Route: (25, 0) -> (38, 5) -> (38, 25) -> (0, 25)
            CreateRoadSegment(roadsContainer.transform, "Road_Detour_CurveIn", new Vector3(31.5f, 2.5f, 0f), new Vector2(14f, 7.2f), 21f, roadCol, curbCol, yellowLineCol, whiteLineCol, shoulderCol, isHorizontal: true, hasCurb1: false, hasCurb2: true);
            CreateRoadSegment(roadsContainer.transform, "Road_Detour_East", new Vector3(38f, 15f, 0f), new Vector2(7.2f, 20f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, shoulderCol, isHorizontal: false, hasCurb1: false, hasCurb2: true);
            GameObject detourEastObj = FindOrCreateChild(roadsContainer.transform, "Road_Detour_East");
            CreateWorldSprite(detourEastObj.transform, "Curb_Left_Short", new Vector3(-3.78f, 0f, 0f), new Vector3(0.35f, 12f, 1f), curbCol, 1);
            CreateRoadSegment(roadsContainer.transform, "Road_Detour_NorthArm", new Vector3(19f, 25f, 0f), new Vector2(38f, 7.2f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, shoulderCol, isHorizontal: true, hasCurb1: true, hasCurb2: false);
            GameObject detourNorthObj = FindOrCreateChild(roadsContainer.transform, "Road_Detour_NorthArm");
            CreateWorldSprite(detourNorthObj.transform, "Curb_Bottom_Short", new Vector3(2f, -3.78f, 0f), new Vector3(24f, 0.35f, 1f), curbCol, 1);

            // Merge Junction at (0, 25) where Branch A and Branch B meet before Roundabout:
            CreateWorldSprite(roadsContainer.transform, "Merge_Junction_Shoulder", new Vector3(0f, 25f, 0f), new Vector3(10f, 9.5f, 1f), shoulderCol, -2);
            CreateWorldSprite(roadsContainer.transform, "Merge_Junction_Pad", new Vector3(0f, 25f, 0f), new Vector3(9f, 8.5f, 1f), roadCol, 0);

            // Structure 6: Roundabout at (-15, 25)
            CreateRoundaboutGeometry(roadsContainer.transform, new Vector2(-15f, 25f), 11.5f, 5.5f, roadCol, curbCol, grassCol, whiteLineCol, shoulderCol);

            // Roundabout East Leg connecting to (0, 25): from (-3.5, 25) to (0, 25)
            CreateRoadSegment(roadsContainer.transform, "Road_Roundabout_EastArm", new Vector3(-1.75f, 25f, 0f), new Vector2(3.5f, 7.2f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, shoulderCol, isHorizontal: true, hasCurb1: false, hasCurb2: false);

            // Roundabout West Leg connecting to Logistics Hub at (-40, 25): from (-26.5, 25) to (-37, 25)
            CreateRoadSegment(roadsContainer.transform, "Road_Roundabout_WestArm", new Vector3(-31.75f, 25f, 0f), new Vector2(10.5f, 7.2f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, shoulderCol, isHorizontal: true);
            CreateWorldSprite(roadsContainer.transform, "WestLogistics_Shoulder", new Vector3(-42f, 25f, 0f), new Vector3(11.2f, 11.2f, 1f), shoulderCol, -2);
            CreateWorldSprite(roadsContainer.transform, "WestLogistics_Pad", new Vector3(-42f, 25f, 0f), new Vector3(10f, 10f, 1f), new Color(0.22f, 0.25f, 0.28f, 1f), 0);
            CreateWorldSprite(roadsContainer.transform, "WestLogistics_CurbW", new Vector3(-47.18f, 25f, 0f), new Vector3(0.35f, 10.4f, 1f), curbCol, 1);

            // Roundabout North Leg connecting to Northern Expressway: from (-15, 36.5) to (-15, 41.4)
            CreateRoadSegment(roadsContainer.transform, "Road_Roundabout_NorthArm", new Vector3(-15f, 38.95f, 0f), new Vector2(7.2f, 4.9f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, shoulderCol);

            // Structure 7: Northern Expressway from (-15, 45) to (48, 45) (width 7.2, length 63)
            // Note: Top curb is continuous; Bottom curb has 15m gap for U-Turn mouth between X=22.5 and X=37.5
            CreateRoadSegment(roadsContainer.transform, "Road_Northern_Expressway", new Vector3(16.5f, 45f, 0f), new Vector2(63f, 7.2f), 0f, roadCol, curbCol, yellowLineCol, whiteLineCol, shoulderCol, isHorizontal: true, hasCurb1: true, hasCurb2: false);
            CreateWorldSprite(roadsContainer.transform, "Exp_BottomCurb_West", new Vector3(3.75f, 41.22f, 0f), new Vector3(37.5f, 0.35f, 1f), curbCol, 1);
            CreateWorldSprite(roadsContainer.transform, "Exp_BottomCurb_East", new Vector3(42.75f, 41.22f, 0f), new Vector3(10.5f, 0.35f, 1f), curbCol, 1);

            // Structure 8: Smooth Continuous U-Turn Loop at (30, 45)
            CreateUTurnLoopGeometry(roadsContainer.transform, new Vector2(30f, 45f), 7.5f, roadCol, curbCol, whiteLineCol, shoulderCol);

            // Roadside Direction Signs & Painted Asphalt Guidance Arrows
            CreateRoadsideSignsAndArrows(world);

            // Roadside Physical Boundary Colliders
            CreateBoundaryColliders(world);
        }

        private static void CreateRoadSegment(Transform parent, string name, Vector3 pos, Vector2 size, float rotationZ, Color roadCol, Color curbCol, Color yellowCol, Color whiteCol, Color shoulderCol, bool isHorizontal = false, bool hasCurb1 = true, bool hasCurb2 = true)
        {
            GameObject seg = FindOrCreateChild(parent, name);
            seg.transform.position = pos;
            seg.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);

            // Sidewalk shoulder bed
            Vector2 shoulderSize = isHorizontal ? new Vector2(size.x, size.y + 1.2f) : new Vector2(size.x + 1.2f, size.y);
            CreateWorldSprite(seg.transform, "Shoulder", Vector3.zero, new Vector3(shoulderSize.x, shoulderSize.y, 1f), shoulderCol, -2);

            // Dark Asphalt road surface
            CreateWorldSprite(seg.transform, "Asphalt", Vector3.zero, new Vector3(size.x, size.y, 1f), roadCol, 0);

            // Center double yellow lines & raised curbs
            if (isHorizontal)
            {
                CreateWorldSprite(seg.transform, "YellowLine1", new Vector3(0f, 0.10f, 0f), new Vector3(size.x, 0.10f, 1f), yellowCol, 2);
                CreateWorldSprite(seg.transform, "YellowLine2", new Vector3(0f, -0.10f, 0f), new Vector3(size.x, 0.10f, 1f), yellowCol, 2);

                if (hasCurb1) CreateWorldSprite(seg.transform, "Curb_Top", new Vector3(0f, (size.y * 0.5f) + 0.18f, 0f), new Vector3(size.x, 0.35f, 1f), curbCol, 1);
                if (hasCurb2) CreateWorldSprite(seg.transform, "Curb_Bottom", new Vector3(0f, -(size.y * 0.5f) - 0.18f, 0f), new Vector3(size.x, 0.35f, 1f), curbCol, 1);
            }
            else
            {
                CreateWorldSprite(seg.transform, "YellowLine1", new Vector3(0.10f, 0f, 0f), new Vector3(0.10f, size.y, 1f), yellowCol, 2);
                CreateWorldSprite(seg.transform, "YellowLine2", new Vector3(-0.10f, 0f, 0f), new Vector3(0.10f, size.y, 1f), yellowCol, 2);

                if (hasCurb1) CreateWorldSprite(seg.transform, "Curb_Left", new Vector3(-(size.x * 0.5f) - 0.18f, 0f, 0f), new Vector3(0.35f, size.y, 1f), curbCol, 1);
                if (hasCurb2) CreateWorldSprite(seg.transform, "Curb_Right", new Vector3((size.x * 0.5f) + 0.18f, 0f, 0f), new Vector3(0.35f, size.y, 1f), curbCol, 1);
            }
        }

        private static void CreateCurvedRoadSegment(Transform parent, Vector2 start, Vector2 end, float width, Color roadCol, Color curbCol, Color lineCol, Color shoulderCol)
        {
            GameObject curveObj = FindOrCreateChild(parent, "Curved_Road_Spline");
            curveObj.transform.position = Vector3.zero;

            int steps = 20;
            Vector2 p0 = start;
            Vector2 p1 = new Vector2((start.x + end.x) * 0.5f, start.y - 8f); // Organic valley curve
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

                GameObject piece = FindOrCreateChild(curveObj.transform, $"CurveSeg_{i}");
                piece.transform.position = new Vector3(mid.x, mid.y, 0f);
                piece.transform.rotation = Quaternion.Euler(0f, 0f, angle);

                CreateWorldSprite(piece.transform, "Shoulder", Vector3.zero, new Vector3(len + 0.5f, width + 1.2f, 1f), shoulderCol, -2);
                CreateWorldSprite(piece.transform, "Asphalt", Vector3.zero, new Vector3(len + 0.5f, width, 1f), roadCol, 0);
                CreateWorldSprite(piece.transform, "Curb_Top", new Vector3(0f, (width * 0.5f) + 0.18f, 0f), new Vector3(len + 0.5f, 0.35f, 1f), curbCol, 1);
                CreateWorldSprite(piece.transform, "Curb_Bottom", new Vector3(0f, -(width * 0.5f) - 0.18f, 0f), new Vector3(len + 0.5f, 0.35f, 1f), curbCol, 1);
                CreateWorldSprite(piece.transform, "CenterDash", Vector3.zero, new Vector3(len * 0.6f, 0.15f, 1f), lineCol, 2);

                prev = curr;
            }
        }

        private static void CreateRoundaboutGeometry(Transform parent, Vector2 center, float outerRadius, float innerRadius, Color roadCol, Color curbCol, Color grassCol, Color lineCol, Color shoulderCol)
        {
            GameObject rbObj = FindOrCreateChild(parent, "Roundabout_Geometry");
            rbObj.transform.position = new Vector3(center.x, center.y, 0f);

            // Shoulder bed
            CreateWorldSprite(rbObj.transform, "Roundabout_Shoulder", Vector3.zero, new Vector3((outerRadius + 1.0f) * 2f, (outerRadius + 1.0f) * 2f, 1f), shoulderCol, -2, isCircle: true);

            // Outer circular road
            CreateWorldSprite(rbObj.transform, "Outer_Road_Circle", Vector3.zero, new Vector3(outerRadius * 2f, outerRadius * 2f, 1f), roadCol, 0, isCircle: true);

            // Outer curb ring
            CreateWorldSprite(rbObj.transform, "Outer_Curb_Ring", Vector3.zero, new Vector3((outerRadius + 0.35f) * 2f, (outerRadius + 0.35f) * 2f, 1f), curbCol, 1, isCircle: true);

            // Dashed circular middle lane divider
            float midRadius = (outerRadius + innerRadius) * 0.5f;
            for (int i = 0; i < 16; i++)
            {
                float ang = i * (360f / 16f) * Mathf.Deg2Rad;
                Vector3 dashPos = new Vector3(Mathf.Cos(ang) * midRadius, Mathf.Sin(ang) * midRadius, 0f);
                GameObject dash = CreateWorldSprite(rbObj.transform, $"Dash_{i}", dashPos, new Vector3(1.3f, 0.18f, 1f), lineCol, 2);
                dash.transform.rotation = Quaternion.Euler(0f, 0f, (i * (360f / 16f)) + 90f);
            }

            // Inner Curb Ring
            CreateWorldSprite(rbObj.transform, "Inner_Curb_Ring", Vector3.zero, new Vector3((innerRadius + 0.35f) * 2f, (innerRadius + 0.35f) * 2f, 1f), curbCol, 1, isCircle: true);

            // Inner Grass Island
            CreateWorldSprite(rbObj.transform, "Inner_Grass_Island", Vector3.zero, new Vector3(innerRadius * 2f, innerRadius * 2f, 1f), grassCol, 2, isCircle: true);

            // Center Island Landmark Monument
            CreateWorldSprite(rbObj.transform, "Island_Monument", Vector3.zero, new Vector3(3.0f, 3.0f, 1f), new Color(0.68f, 0.72f, 0.78f, 1f), 3, isCircle: true);

            // Inner Island Solid Collider to prevent driving across the grass island
            CircleCollider2D islandCol = GetOrAdd<CircleCollider2D>(rbObj);
            islandCol.radius = innerRadius;
            islandCol.isTrigger = false;
        }

        private static void CreateUTurnLoopGeometry(Transform parent, Vector2 rootPos, float radius, Color roadCol, Color curbCol, Color lineCol, Color shoulderCol)
        {
            GameObject utObj = FindOrCreateChild(parent, "UTurn_Loop_Geometry");
            utObj.transform.position = Vector3.zero;
            ClearChildren(utObj.transform);

            // Sweeping smooth half-circle arc connecting to Expressway bottom curb at Y = 41.4f
            // Loop connects from X = 22.5 to X = 37.5 (center at X = 30f, Y = 41.4f, radius = 7.5f)
            int steps = 36;
            float startAngle = 0f;
            float endAngle = -180f;
            Vector2 loopCenter = new Vector2(rootPos.x, 41.4f);

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

                GameObject seg = FindOrCreateChild(utObj.transform, $"UTurnSeg_{i}");
                seg.transform.position = new Vector3(mid.x, mid.y, 0f);
                seg.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);

                CreateWorldSprite(seg.transform, "Shoulder", Vector3.zero, new Vector3(segLen + 0.35f, 8.4f, 1f), shoulderCol, -2);
                CreateWorldSprite(seg.transform, "Asphalt", Vector3.zero, new Vector3(segLen + 0.35f, 6.8f, 1f), roadCol, 0);

                if (i % 3 != 0)
                {
                    CreateWorldSprite(seg.transform, "CenterLine", Vector3.zero, new Vector3(segLen * 0.8f, 0.16f, 1f), lineCol, 2);
                }

                prevPos = currPos;
            }

            // Smooth Continuous Outer Curb Arc: radius = 7.5 + 3.4 = 10.9
            GameObject outerCurbs = FindOrCreateChild(utObj.transform, "OuterCurbs");
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

                GameObject cPiece = FindOrCreateChild(outerCurbs.transform, $"OuterCurb_{i}");
                cPiece.transform.position = new Vector3(mid.x, mid.y, 0f);
                cPiece.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
                CreateWorldSprite(cPiece.transform, "Curb", Vector3.zero, new Vector3(len + 0.12f, 0.35f, 1f), curbCol, 1);

                prevOuter = currOuter;
            }

            // Smooth Continuous Inner Curb Arc: radius = 7.5 - 3.4 = 4.1
            GameObject innerCurbs = FindOrCreateChild(utObj.transform, "InnerCurbs");
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

                GameObject cPiece = FindOrCreateChild(innerCurbs.transform, $"InnerCurb_{i}");
                cPiece.transform.position = new Vector3(mid.x, mid.y, 0f);
                cPiece.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
                CreateWorldSprite(cPiece.transform, "Curb", Vector3.zero, new Vector3(len + 0.12f, 0.35f, 1f), curbCol, 1);

                prevInner = currInner;
            }
        }

        private static void CreateRoadsideSignsAndArrows(Transform world)
        {
            GameObject signsContainer = FindOrCreateChild(world, "RoadsideSigns");
            signsContainer.transform.position = Vector3.zero;

            // 1. Mission 1: SW Avenue Signs & Arrows
            CreateSignboard(signsContainer.transform, "Sign_SpeedLimit_40", new Vector3(-41f, -38f, 0f), "SPEED LIMIT");
            CreateSignboard(signsContainer.transform, "Sign_RainRoad", new Vector3(-41f, -34f, 0f), "RAIN ROAD");
            CreateSignboard(signsContainer.transform, "Sign_SharpCurve_90", new Vector3(-41f, -22f, 0f), "SHARP CURVE");

            CreateAsphaltArrow(signsContainer.transform, "Arrow_SW_Ahead", new Vector3(-45f, -36f, 0f), 0f);
            CreateAsphaltArrow(signsContainer.transform, "Arrow_SW_TurnRight", new Vector3(-45f, -22f, 0f), 0f, isTurnRight: true);

            // 2. South Connector Arrows
            CreateAsphaltArrow(signsContainer.transform, "Arrow_Connector_E1", new Vector3(-34f, -15f, 0f), -90f);
            CreateAsphaltArrow(signsContainer.transform, "Arrow_Connector_E2", new Vector3(-24f, -15f, 0f), -90f);

            // 3. T-Junction Approach & Fork Signs
            CreateSignboard(signsContainer.transform, "Sign_Fork_Caution", new Vector3(31.5f, -6f, 0f), "RAIN ROAD");
            CreateSignboard(signsContainer.transform, "Sign_BranchA_Flood", new Vector3(20f, 3.5f, 0f), "RAIN ROAD");
            CreateSignboard(signsContainer.transform, "Sign_BranchB_Detour", new Vector3(41.5f, 15f, 0f), "SPEED LIMIT");

            CreateAsphaltArrow(signsContainer.transform, "Arrow_Approach_ForkL", new Vector3(23.5f, -5.5f, 0f), 25f);
            CreateAsphaltArrow(signsContainer.transform, "Arrow_Approach_ForkR", new Vector3(26.5f, -5.5f, 0f), -25f);

            // 4. Northern Expressway & Dead End Blockage Signs
            CreateSignboard(signsContainer.transform, "Sign_Expressway_SpeedLimit", new Vector3(0f, 49.2f, 0f), "SPEED LIMIT");
            CreateSignboard(signsContainer.transform, "Sign_Blockage_NoEntry", new Vector3(36f, 49.2f, 0f), "NO ENTRY");
            CreateSignboard(signsContainer.transform, "Sign_Blockage_Stop", new Vector3(38f, 49.2f, 0f), "STOP");

            // 5. U-Turn Guidance Arrows on Asphalt
            CreateAsphaltArrow(signsContainer.transform, "Arrow_UTurn_Entrance", new Vector3(24f, 43.5f, 0f), 180f);
            CreateAsphaltArrow(signsContainer.transform, "Arrow_UTurn_Exit", new Vector3(36f, 45f, 0f), 90f);
        }

        private static void CreateSignboard(Transform parent, string name, Vector3 pos, string signResourceName)
        {
            GameObject signObj = FindOrCreateChild(parent, name);
            signObj.transform.position = pos;
            signObj.transform.localScale = Vector3.one;

            CreateWorldSprite(signObj.transform, "Post", new Vector3(0f, -0.6f, 0f), new Vector3(0.16f, 1.2f, 1f), new Color(0.35f, 0.38f, 0.42f, 1f), 12);
            CreateWorldSprite(signObj.transform, "Base", new Vector3(0f, -1.2f, 0f), new Vector3(0.7f, 0.2f, 1f), new Color(0.28f, 0.30f, 0.32f, 1f), 12);

            Sprite signSprite = Resources.Load<Sprite>($"Signs/{signResourceName}");
            if (signSprite == null)
            {
#if UNITY_EDITOR
                signSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Signs/{signResourceName}.png");
#endif
            }

            GameObject face = FindOrCreateChild(signObj.transform, "Face");
            face.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            face.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(face);
            sr.sortingOrder = 14;
            if (signSprite != null)
            {
                sr.sprite = signSprite;
                sr.color = Color.white;
            }
            else
            {
                sr.sprite = LoadSpriteAsset(WorldSquareSpritePath);
                sr.color = new Color(0.95f, 0.85f, 0.15f, 1f);
            }
        }

        private static void CreateAsphaltArrow(Transform parent, string name, Vector3 pos, float rotationZ, bool isTurnRight = false)
        {
            GameObject arrowObj = FindOrCreateChild(parent, name);
            arrowObj.transform.position = pos;
            arrowObj.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);

            Color arrowCol = new Color(0.92f, 0.94f, 0.96f, 0.88f); // Crisp semi-gloss road paint

            if (!isTurnRight)
            {
                CreateWorldSprite(arrowObj.transform, "Shaft", new Vector3(0f, -0.35f, 0f), new Vector3(0.28f, 1.3f, 1f), arrowCol, 3);
                GameObject leftBarb = CreateWorldSprite(arrowObj.transform, "LeftBarb", new Vector3(-0.24f, 0.28f, 0f), new Vector3(0.24f, 0.75f, 1f), arrowCol, 3);
                leftBarb.transform.localRotation = Quaternion.Euler(0f, 0f, 36f);
                GameObject rightBarb = CreateWorldSprite(arrowObj.transform, "RightBarb", new Vector3(0.24f, 0.28f, 0f), new Vector3(0.24f, 0.75f, 1f), arrowCol, 3);
                rightBarb.transform.localRotation = Quaternion.Euler(0f, 0f, -36f);
                CreateWorldSprite(arrowObj.transform, "Tip", new Vector3(0f, 0.58f, 0f), new Vector3(0.32f, 0.35f, 1f), arrowCol, 3);
            }
            else
            {
                CreateWorldSprite(arrowObj.transform, "ShaftV", new Vector3(-0.35f, -0.3f, 0f), new Vector3(0.28f, 1.1f, 1f), arrowCol, 3);
                CreateWorldSprite(arrowObj.transform, "ShaftCorner", new Vector3(-0.1f, 0.25f, 0f), new Vector3(0.6f, 0.28f, 1f), arrowCol, 3);
                CreateWorldSprite(arrowObj.transform, "ShaftH", new Vector3(0.35f, 0.25f, 0f), new Vector3(0.8f, 0.28f, 1f), arrowCol, 3);
                GameObject topBarb = CreateWorldSprite(arrowObj.transform, "TopBarb", new Vector3(0.62f, 0.45f, 0f), new Vector3(0.24f, 0.65f, 1f), arrowCol, 3);
                topBarb.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                GameObject bottomBarb = CreateWorldSprite(arrowObj.transform, "BottomBarb", new Vector3(0.62f, 0.05f, 0f), new Vector3(0.24f, 0.65f, 1f), arrowCol, 3);
                bottomBarb.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
                CreateWorldSprite(arrowObj.transform, "Tip", new Vector3(0.85f, 0.25f, 0f), new Vector3(0.32f, 0.35f, 1f), arrowCol, 3);
            }
        }

        private static void CreateBoundaryColliders(Transform world)
        {
            GameObject boundsObj = FindOrCreateChild(world, "WorldBounds");
            boundsObj.transform.position = Vector3.zero;

            // Outer perimeter walls at X = ±80, Y = ±60
            CreateBoundWall(boundsObj.transform, "Wall_North", new Vector3(0f, 60f, 0f), new Vector2(170f, 4f));
            CreateBoundWall(boundsObj.transform, "Wall_South", new Vector3(0f, -60f, 0f), new Vector2(170f, 4f));
            CreateBoundWall(boundsObj.transform, "Wall_East", new Vector3(80f, 0f, 0f), new Vector2(4f, 130f));
            CreateBoundWall(boundsObj.transform, "Wall_West", new Vector3(-80f, 0f, 0f), new Vector2(4f, 130f));

            // Roadside Containment Colliders along curbs to keep player car on marked roadway
            CreateBoundWall(boundsObj.transform, "CurbCol_SW_Left", new Vector3(-49.1f, -31.8f, 0f), new Vector2(0.8f, 28f));
            CreateBoundWall(boundsObj.transform, "CurbCol_SW_Corner_Top", new Vector3(-45f, -11.0f, 0f), new Vector2(8.5f, 0.8f));
            CreateBoundWall(boundsObj.transform, "CurbCol_Connector_Bottom", new Vector3(-30f, -19.1f, 0f), new Vector2(24f, 0.8f));
            CreateBoundWall(boundsObj.transform, "CurbCol_Connector_Top", new Vector3(-30f, -11.0f, 0f), new Vector2(24f, 0.8f));
            CreateBoundWall(boundsObj.transform, "CurbCol_TJunction_Bottom", new Vector3(-15f, -19.1f, 0f), new Vector2(8.5f, 0.8f));
            CreateBoundWall(boundsObj.transform, "CurbCol_TJunction_LegN_Left", new Vector3(-19.1f, 1.05f, 0f), new Vector2(0.8f, 25f));
            CreateBoundWall(boundsObj.transform, "CurbCol_TJunction_LegN_Right", new Vector3(-10.9f, 1.05f, 0f), new Vector2(0.8f, 25f));

            // Fork Approach & Detour Outer Colliders
            CreateBoundWall(boundsObj.transform, "CurbCol_ForkApproach_Left", new Vector3(20.9f, -6f, 0f), new Vector2(0.8f, 8f));
            CreateBoundWall(boundsObj.transform, "CurbCol_ForkApproach_Right", new Vector3(29.1f, -6f, 0f), new Vector2(0.8f, 8f));
            CreateBoundWall(boundsObj.transform, "CurbCol_Detour_East", new Vector3(42.1f, 15f, 0f), new Vector2(0.8f, 22f));
            CreateBoundWall(boundsObj.transform, "CurbCol_Detour_North", new Vector3(19f, 29.1f, 0f), new Vector2(40f, 0.8f));

            // Expressway North & South border colliders
            CreateBoundWall(boundsObj.transform, "CurbCol_Expressway_North", new Vector3(16.5f, 49.1f, 0f), new Vector2(64f, 0.8f));
            CreateBoundWall(boundsObj.transform, "CurbCol_Expressway_DeadEnd", new Vector3(48.5f, 45f, 0f), new Vector2(0.8f, 8f));
            CreateBoundWall(boundsObj.transform, "CurbCol_Expressway_SouthWest", new Vector3(3.75f, 40.9f, 0f), new Vector2(37.5f, 0.8f));
            CreateBoundWall(boundsObj.transform, "CurbCol_Expressway_SouthEast", new Vector3(42.75f, 40.9f, 0f), new Vector2(10.5f, 0.8f));

            // U-Turn Outer Arc Containment Wall
            CreateBoundWall(boundsObj.transform, "CurbCol_UTurn_OuterApex", new Vector3(30f, 29.8f, 0f), new Vector2(16f, 0.8f));
            CreateBoundWall(boundsObj.transform, "CurbCol_UTurn_OuterLeft", new Vector3(18.5f, 36f, 0f), new Vector2(0.8f, 12f));
            CreateBoundWall(boundsObj.transform, "CurbCol_UTurn_OuterRight", new Vector3(41.5f, 36f, 0f), new Vector2(0.8f, 12f));
        }

        private static void CreateBoundWall(Transform parent, string name, Vector3 pos, Vector2 size)
        {
            GameObject wall = FindOrCreateChild(parent, name);
            wall.transform.position = pos;
            BoxCollider2D col = GetOrAdd<BoxCollider2D>(wall);
            col.size = size;
            col.isTrigger = false;
        }
        #endregion

        #region Detection Zones
        private static void CreateDetectionZones(Transform world)
        {
            GameObject zonesContainer = FindOrCreateChild(world, "DetectionZones");
            zonesContainer.transform.position = Vector3.zero;

            // 1. U-Turn Detection Zone placed across U-turn loop
            GameObject utZoneObj = FindOrCreateChild(zonesContainer.transform, "UTurn_DetectionZone");
            utZoneObj.transform.position = new Vector3(30f, 36f, 0f);
            CircleCollider2D utCol = GetOrAdd<CircleCollider2D>(utZoneObj);
            utCol.radius = 7.0f;
            utCol.isTrigger = true;
            GetOrAdd<UTurnDetectionZone>(utZoneObj);

            // 2. Roundabout Detection Zone
            GameObject rbZoneObj = FindOrCreateChild(zonesContainer.transform, "Roundabout_DetectionZone");
            rbZoneObj.transform.position = new Vector3(-15f, 25f, 0f);
            CircleCollider2D rbCol = GetOrAdd<CircleCollider2D>(rbZoneObj);
            rbCol.radius = 12.5f;
            rbCol.isTrigger = true;
            RoundaboutDetectionZone rbZone = GetOrAdd<RoundaboutDetectionZone>(rbZoneObj);
            SetVector2(rbZone, "roundaboutCenter", new Vector2(-15f, 25f));

            // 3. Junction Detection Zone: Fork
            GameObject forkZoneObj = FindOrCreateChild(zonesContainer.transform, "Fork_DetectionZone");
            forkZoneObj.transform.position = new Vector3(25f, -3f, 0f);
            BoxCollider2D forkCol = GetOrAdd<BoxCollider2D>(forkZoneObj);
            forkCol.size = new Vector2(9f, 6f);
            forkCol.isTrigger = true;
            JunctionDetectionZone forkZone = GetOrAdd<JunctionDetectionZone>(forkZoneObj);
            SetReference(forkZone, "junctionName", "Decision Fork");
            SetReference(forkZone, "approachAdvice", "FORK AHEAD: Left route flooded. Detour via Right elevated route!");

            // 4. Junction Detection Zone: T-Junction
            GameObject tjZoneObj = FindOrCreateChild(zonesContainer.transform, "TJunction_DetectionZone");
            tjZoneObj.transform.position = new Vector3(-15f, -9f, 0f);
            BoxCollider2D tjCol = GetOrAdd<BoxCollider2D>(tjZoneObj);
            tjCol.size = new Vector2(8f, 6f);
            tjCol.isTrigger = true;
            JunctionDetectionZone tjZone = GetOrAdd<JunctionDetectionZone>(tjZoneObj);
            SetReference(tjZone, "junctionName", "South-Central T-Junction");
            SetReference(tjZone, "approachAdvice", "⊥ T-Junction ahead: Yield to cross traffic & slow down in rain!");
        }
        #endregion

        #region Environmental Hazards
        private static EnvironmentalHazardController CreateEnvironmentalHazards(Transform world)
        {
            GameObject hazardContainer = FindOrCreateChild(world, "Hazards");
            hazardContainer.transform.position = Vector3.zero;

            EnvironmentalHazardController ctrl = GetOrAdd<EnvironmentalHazardController>(hazardContainer);

            // Hazard 1: Flooded Zone on Fork Branch A at (13, 12) (Angled -45 deg)
            GameObject floodObj = FindOrCreateChild(hazardContainer.transform, "FloodZone_BranchA");
            floodObj.transform.position = new Vector3(13f, 12f, 0f);
            floodObj.transform.rotation = Quaternion.Euler(0f, 0f, -45f);
            BoxCollider2D floodCol = GetOrAdd<BoxCollider2D>(floodObj);
            floodCol.size = new Vector2(7.2f, 14f);
            floodCol.isTrigger = true;

            // Organic smooth flooded road water surface
            Transform waterVis = FindOrCreateChild(floodObj.transform, "WaterVisual").transform;
            ClearChildren(waterVis);

            Color waterBaseCol = new Color(0.12f, 0.42f, 0.68f, 0.72f); // Smooth realistic water sheet
            Color waterDeepCol = new Color(0.08f, 0.30f, 0.52f, 0.88f); // Deep center pond
            Color waterCrestCol = new Color(0.65f, 0.88f, 1.0f, 0.40f); // Gentle surface ripples

            CreateWorldSprite(waterVis, "WaterSheet_Base", Vector3.zero, new Vector3(7.2f, 14.5f, 1f), waterBaseCol, 4);
            CreateWorldSprite(waterVis, "WaterSheet_Deep", Vector3.zero, new Vector3(5.6f, 11.5f, 1f), waterDeepCol, 5);
            CreateWorldSprite(waterVis, "Ripple_1", new Vector3(0f, 3.5f, 0f), new Vector3(4.8f, 0.45f, 1f), waterCrestCol, 6);
            CreateWorldSprite(waterVis, "Ripple_2", new Vector3(0f, 0f, 0f), new Vector3(5.2f, 0.50f, 1f), waterCrestCol, 6);
            CreateWorldSprite(waterVis, "Ripple_3", new Vector3(0f, -3.5f, 0f), new Vector3(4.5f, 0.45f, 1f), waterCrestCol, 6);
            FloodHazardZone floodZone = GetOrAdd<FloodHazardZone>(floodObj);

            // Hazard 2: Mud Zones on shoulders
            GameObject mudObj1 = FindOrCreateChild(hazardContainer.transform, "MudZone_CurvedShoulder");
            mudObj1.transform.position = new Vector3(5f, -22f, 0f);
            BoxCollider2D mudCol1 = GetOrAdd<BoxCollider2D>(mudObj1);
            mudCol1.size = new Vector2(14f, 5f);
            mudCol1.isTrigger = true;
            Transform mudVis1 = FindOrCreateChild(mudObj1.transform, "MudVisual").transform;
            ClearChildren(mudVis1);
            CreateOrganicMudPuddle(mudVis1, new Vector2(14f, 5f), isHorizontal: true);
            MudHazardZone mudZone1 = GetOrAdd<MudHazardZone>(mudObj1);

            GameObject mudObj2 = FindOrCreateChild(hazardContainer.transform, "MudZone_DetourShoulder");
            mudObj2.transform.position = new Vector3(41f, 12f, 0f);
            BoxCollider2D mudCol2 = GetOrAdd<BoxCollider2D>(mudObj2);
            mudCol2.size = new Vector2(5f, 12f);
            mudCol2.isTrigger = true;
            Transform mudVis2 = FindOrCreateChild(mudObj2.transform, "MudVisual").transform;
            ClearChildren(mudVis2);
            CreateOrganicMudPuddle(mudVis2, new Vector2(5f, 12f), isHorizontal: false);
            MudHazardZone mudZone2 = GetOrAdd<MudHazardZone>(mudObj2);

            // Hazard 3: Fallen Tree Blockage on Northern Expressway at (42, 45)
            GameObject treeObj = FindOrCreateChild(hazardContainer.transform, "FallenTree_NorthernBlockage");
            treeObj.transform.position = new Vector3(42f, 45f, 0f);
            BoxCollider2D treeCol = GetOrAdd<BoxCollider2D>(treeObj);
            treeCol.size = new Vector2(4f, 7.5f);
            treeCol.isTrigger = false;
            CreateWorldSprite(treeObj.transform, "TreeTrunk", Vector3.zero, new Vector3(3.5f, 7.5f, 1f), new Color(0.35f, 0.22f, 0.12f, 1f), 5);
            CreateWorldSprite(treeObj.transform, "TreeFoliage", new Vector3(0.5f, 0.5f, 0f), new Vector3(5f, 6.5f, 1f), new Color(0.18f, 0.38f, 0.18f, 0.9f), 6, isCircle: true);
            FallenTreeBlockage treeBlockage = GetOrAdd<FallenTreeBlockage>(treeObj);

            // Hazard 4: Road Closure Barricade at (38, 45) before tree
            GameObject barrierObj = FindOrCreateChild(hazardContainer.transform, "Barricade_NorthernExpressway");
            barrierObj.transform.position = new Vector3(38f, 45f, 0f);
            BoxCollider2D barCol = GetOrAdd<BoxCollider2D>(barrierObj);
            barCol.size = new Vector2(1.5f, 7.2f);
            barCol.isTrigger = false;
            CreateWorldSprite(barrierObj.transform, "BarricadeWood", Vector3.zero, new Vector3(1.2f, 7.2f, 1f), new Color(0.85f, 0.45f, 0.1f, 1f), 5);
            RoadClosureBarrier barrier = GetOrAdd<RoadClosureBarrier>(barrierObj);

            // Hazard 5: Debris on Highway
            GameObject debrisObj = FindOrCreateChild(hazardContainer.transform, "Debris_Highway");
            debrisObj.transform.position = new Vector3(10f, 45.5f, 0f);
            CircleCollider2D debCol = GetOrAdd<CircleCollider2D>(debrisObj);
            debCol.radius = 1.2f;
            debCol.isTrigger = true;
            CreateWorldSprite(debrisObj.transform, "DebrisVisual", Vector3.zero, new Vector3(2f, 1.2f, 1f), new Color(0.3f, 0.3f, 0.3f, 0.8f), 5);
            DebrisHazard debris = GetOrAdd<DebrisHazard>(debrisObj);

            // Bind to EnvironmentalHazardController
            ctrl.BindHazards(
                new FloodHazardZone[] { floodZone },
                new MudHazardZone[] { mudZone1, mudZone2 },
                new FallenTreeBlockage[] { treeBlockage },
                new RoadClosureBarrier[] { barrier },
                new DebrisHazard[] { debris }
            );

            return ctrl;
        }

        private static void CreateOrganicMudPuddle(Transform parent, Vector2 size, bool isHorizontal)
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

                CreateWorldSprite(parent, $"MudDisc_{i}", pos, new Vector3(radius, radius, 1f), mudMedium, 4, isCircle: true);
                CreateWorldSprite(parent, $"MudCore_{i}", pos + new Vector3(0.15f, -0.1f, 0f), new Vector3(radius * 0.6f, radius * 0.6f, 1f), mudDark, 5, isCircle: true);

                Vector3 spPos1 = pos + new Vector3(Mathf.Cos(i) * 1.2f, Mathf.Sin(i) * 1.2f, 0f);
                CreateWorldSprite(parent, $"MudSplatA_{i}", spPos1, new Vector3(0.6f, 0.6f, 1f), mudLight, 4, isCircle: true);
                Vector3 spPos2 = pos + new Vector3(Mathf.Sin(i * 2) * 1.1f, -Mathf.Cos(i * 2) * 1.1f, 0f);
                CreateWorldSprite(parent, $"MudSplatB_{i}", spPos2, new Vector3(0.45f, 0.45f, 1f), mudDark, 5, isCircle: true);
            }
        }
        #endregion

        #region Weather System
        private static WeatherController CreateWeatherSystem(Transform world, Light2D globalLight, Camera cam)
        {
            GameObject weatherObj = FindOrCreateChild(world, "WeatherSystem");
            weatherObj.transform.position = Vector3.zero;

            WeatherController wc = GetOrAdd<WeatherController>(weatherObj);
            wc.BindGlobalLight(globalLight);

            // Load / create dedicated rain material with URP 2D unlit sprite shader
            Material rainMat = AssetDatabase.LoadAssetAtPath<Material>(RainMaterialPath);
            if (rainMat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                rainMat = new Material(shader);
                rainMat.name = "RainMaterial";
                rainMat.color = new Color(0.72f, 0.82f, 0.94f, 0.75f);
                Sprite rainSprite = LoadSpriteAsset(RainDropPath);
                if (rainSprite != null) rainMat.mainTexture = rainSprite.texture;
                if (!Directory.Exists("Assets/Materials")) Directory.CreateDirectory("Assets/Materials");
                AssetDatabase.CreateAsset(rainMat, RainMaterialPath);
            }

            // Create Rain Particle System attached to camera
            GameObject rainObj = FindOrCreateChild(cam.transform, "RainParticles");
            rainObj.transform.localPosition = new Vector3(0f, 0f, 5f);
            rainObj.transform.localRotation = Quaternion.identity;

            ParticleSystem ps = GetOrAdd<ParticleSystem>(rainObj);
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

            // Subtle fog atmosphere overlay attached to camera (fades with weather intensity)
            GameObject fogObj = FindOrCreateChild(cam.transform, "FogAtmosphereOverlay");
            fogObj.transform.localPosition = new Vector3(0f, 0f, 2f);
            fogObj.transform.localScale = new Vector3(36f, 24f, 1f);
            SpriteRenderer fogSr = GetOrAdd<SpriteRenderer>(fogObj);
            fogSr.sprite = LoadSpriteAsset(WorldSquareSpritePath);
            fogSr.color = new Color(0.72f, 0.78f, 0.85f, 0f); // Initially 0 alpha
            fogSr.sortingOrder = 12;

            wc.BindFogOverlay(fogSr);

            return wc;
        }
        #endregion

        #region Destination Marker
        private static Level6Destination CreateDestination(Transform world)
        {
            GameObject destObj = FindOrCreateChild(world, "DestinationGoal");
            destObj.transform.position = new Vector3(25f, -15f, 0f);

            // Trigger collider
            CircleCollider2D col = GetOrAdd<CircleCollider2D>(destObj);
            col.radius = 2.5f;
            col.isTrigger = true;

            // Compact glowing halo under vehicle (sortingOrder = 3)
            GameObject halo = CreateWorldSprite(destObj.transform, "DestinationHalo", Vector3.zero, new Vector3(2.4f, 2.4f, 1f), new Color(0.2f, 0.8f, 1f, 0.45f), 3, isCircle: true);
            GameObject centerPin = CreateWorldSprite(destObj.transform, "CenterPin", Vector3.zero, new Vector3(1.2f, 1.2f, 1f), new Color(1f, 0.90f, 0.2f, 0.9f), 3, isCircle: true);

            Level6Destination dest = GetOrAdd<Level6Destination>(destObj);
            dest.BindVisual(halo.transform);
            return dest;
        }
        #endregion

        #region Player Car
        private static GameObject CreatePlayerCar(Transform world)
        {
            GameObject car = FindOrCreateChild(world, "PlayerCar");
            car.transform.position = new Vector3(-45f, -35f, 0f);
            car.transform.rotation = Quaternion.identity; // Facing North
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

            Level6PlayerCar playerCar = GetOrAdd<Level6PlayerCar>(car);

            // Headlight beams
            Sprite coneSprite = LoadSpriteAsset(HeadlightBeamConePath);
            if (coneSprite != null)
            {
                CreateHeadlight(car.transform, "Headlight_L", new Vector3(-0.45f, 1.4f, 0f), coneSprite);
                CreateHeadlight(car.transform, "Headlight_R", new Vector3(0.45f, 1.4f, 0f), coneSprite);
            }

            // Brake lights
            Sprite circleSprite = LoadSpriteAsset(WorldCircleSpritePath);
            SpriteRenderer blLeft = CreateBrakeLight(car.transform, "BrakeLight_L", new Vector3(-0.65f, -1.6f, 0f), circleSprite);
            SpriteRenderer blRight = CreateBrakeLight(car.transform, "BrakeLight_R", new Vector3(0.65f, -1.6f, 0f), circleSprite);
            SetObjectArray(playerCar, "brakeLights", new[] { blLeft, blRight });

            return car;
        }

        private static void CreateHeadlight(Transform parent, string name, Vector3 pos, Sprite coneSprite)
        {
            GameObject hl = FindOrCreateChild(parent, name);
            hl.transform.localPosition = pos;
            hl.transform.localScale = new Vector3(1.2f, 2.5f, 1f);
            hl.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(hl);
            sr.sprite = coneSprite;
            sr.color = new Color(1f, 1f, 0.85f, 0.09f);
            sr.sortingOrder = 11;
        }

        private static SpriteRenderer CreateBrakeLight(Transform parent, string name, Vector3 pos, Sprite circleSprite)
        {
            GameObject bl = FindOrCreateChild(parent, name);
            bl.transform.localPosition = pos;
            bl.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(bl);
            sr.sprite = circleSprite;
            sr.color = new Color(0.6f, 0.1f, 0.1f, 0.6f);
            sr.sortingOrder = 12;
            return sr;
        }
        #endregion

        #region UI Construction
        private static Level6UIController CreateUI(Level6PlayerCar playerCar, Level6Destination destination)
        {
            GameObject canvasObj = FindOrCreate("Level6_Canvas");
            Canvas canvas = GetOrAdd<Canvas>(canvasObj);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObj);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAdd<GraphicRaycaster>(canvasObj);

            // 1. Mission Card (Top-Left)
            GameObject missionCard = CreateUIPanel(canvasObj.transform, "MissionCard", new Vector2(0f, 1f), new Vector2(25f, -25f), new Vector2(460f, 165f), new Color(0.08f, 0.10f, 0.14f, 0.90f));

            TMP_Text mBadge = CreateUIText(missionCard.transform, "MissionBadge", "MISSION 1 / 5", 14f, TextAlignmentOptions.Left, new Vector2(280f, 22f), new Vector2(15f, -12f), new Vector2(0f, 1f));
            mBadge.color = new Color(1f, 0.85f, 0.2f, 1f);

            TMP_Text mTimer = CreateUIText(missionCard.transform, "MissionTimer", "TIME: 01:30", 15f, TextAlignmentOptions.Right, new Vector2(140f, 22f), new Vector2(-15f, -12f), new Vector2(1f, 1f));
            mTimer.fontStyle = FontStyles.Bold;

            TMP_Text mTitle = CreateUIText(missionCard.transform, "MissionTitle", "RAIN SLICK ROADS", 17f, TextAlignmentOptions.Left, new Vector2(430f, 26f), new Vector2(15f, -38f), new Vector2(0f, 1f));
            mTitle.fontStyle = FontStyles.Bold;

            TMP_Text mObj = CreateUIText(missionCard.transform, "MissionObjective", "Control speed through the 90° turn and curved road.", 12.5f, TextAlignmentOptions.Left, new Vector2(430f, 44f), new Vector2(15f, -66f), new Vector2(0f, 1f));
            mObj.color = new Color(0.85f, 0.88f, 0.92f, 1f);

            TMP_Text mGps = CreateUIText(missionCard.transform, "GPSGuidance", "GPS: ↑ EAST DEPOT (85m)", 13.5f, TextAlignmentOptions.Left, new Vector2(430f, 24f), new Vector2(15f, -125f), new Vector2(0f, 1f));
            mGps.fontStyle = FontStyles.Bold;
            mGps.color = new Color(1f, 0.9f, 0.3f, 1f);

            // 2. Telemetry Status (Top-Center)
            GameObject telemetryPanel = CreateUIPanel(canvasObj.transform, "TelemetryPanel", new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(440f, 75f), new Color(0.08f, 0.10f, 0.14f, 0.88f));

            TMP_Text wStatus = CreateUIText(telemetryPanel.transform, "WeatherStatus", "HEAVY RAIN", 16f, TextAlignmentOptions.Center, new Vector2(200f, 25f), new Vector2(-105f, -12f), new Vector2(0.5f, 1f));
            wStatus.fontStyle = FontStyles.Bold;

            TMP_Text wWind = CreateUIText(telemetryPanel.transform, "WindStatus", "Wind: 14 km/h East", 13f, TextAlignmentOptions.Center, new Vector2(200f, 20f), new Vector2(-105f, -42f), new Vector2(0.5f, 1f));
            wWind.color = new Color(0.7f, 0.85f, 1f, 1f);

            // Grip Meter
            TMP_Text gVal = CreateUIText(telemetryPanel.transform, "GripLabel", "Grip: 45%", 13f, TextAlignmentOptions.Center, new Vector2(180f, 20f), new Vector2(105f, -12f), new Vector2(0.5f, 1f));
            gVal.fontStyle = FontStyles.Bold;

            GameObject gripBarBg = CreateUIPanel(telemetryPanel.transform, "GripBarBg", new Vector2(0.5f, 1f), new Vector2(105f, -42f), new Vector2(160f, 14f), new Color(0.2f, 0.2f, 0.2f, 0.8f));

            GameObject gripFillObj = FindOrCreateChild(gripBarBg.transform, "Fill");
            SetFullScreen(GetOrAdd<RectTransform>(gripFillObj));
            Image gFill = GetOrAdd<Image>(gripFillObj);
            gFill.color = new Color(0.9f, 0.8f, 0.2f, 1f);

            TMP_Text hNotice = CreateUIText(telemetryPanel.transform, "HazardNotice", "", 12f, TextAlignmentOptions.Center, new Vector2(400f, 18f), new Vector2(0f, -56f), new Vector2(0.5f, 1f));
            hNotice.color = new Color(1f, 0.4f, 0.4f, 1f);

            // 3. Safety & Score Card (Top-Right)
            GameObject scoreCard = CreateUIPanel(canvasObj.transform, "ScoreCard", new Vector2(1f, 1f), new Vector2(-25f, -25f), new Vector2(360f, 130f), new Color(0.08f, 0.10f, 0.14f, 0.88f));

            TMP_Text sVal = CreateUIText(scoreCard.transform, "ScoreVal", "Score: 0", 17f, TextAlignmentOptions.Right, new Vector2(330f, 25f), new Vector2(-15f, -12f), new Vector2(1f, 1f));
            sVal.fontStyle = FontStyles.Bold;

            TMP_Text sfVal = CreateUIText(scoreCard.transform, "SafetyVal", "Safety: 100%", 14f, TextAlignmentOptions.Left, new Vector2(160f, 20f), new Vector2(15f, -44f), new Vector2(0f, 1f));
            GameObject sfBg = CreateUIPanel(scoreCard.transform, "SafetyBg", new Vector2(1f, 1f), new Vector2(-15f, -46f), new Vector2(160f, 14f), new Color(0.2f, 0.2f, 0.2f, 0.8f));
            GameObject sfFillObj = FindOrCreateChild(sfBg.transform, "Fill");
            SetFullScreen(GetOrAdd<RectTransform>(sfFillObj));
            Image sfFill = GetOrAdd<Image>(sfFillObj);
            sfFill.type = Image.Type.Filled;
            sfFill.fillMethod = Image.FillMethod.Horizontal;
            sfFill.fillAmount = 1f;
            sfFill.color = new Color(0.2f, 0.85f, 0.3f, 1f);

            TMP_Text rsVal = CreateUIText(scoreCard.transform, "RouteVal", "Route Rating: 100%", 14f, TextAlignmentOptions.Left, new Vector2(160f, 20f), new Vector2(15f, -76f), new Vector2(0f, 1f));
            GameObject rsBg = CreateUIPanel(scoreCard.transform, "RouteBg", new Vector2(1f, 1f), new Vector2(-15f, -78f), new Vector2(160f, 14f), new Color(0.2f, 0.2f, 0.2f, 0.8f));
            GameObject rsFillObj = FindOrCreateChild(rsBg.transform, "Fill");
            SetFullScreen(GetOrAdd<RectTransform>(rsFillObj));
            Image rsFill = GetOrAdd<Image>(rsFillObj);
            rsFill.type = Image.Type.Filled;
            rsFill.fillMethod = Image.FillMethod.Horizontal;
            rsFill.fillAmount = 1f;
            rsFill.color = new Color(0.3f, 0.7f, 1f, 1f);

            // 4. Speedometer (Bottom-Left)
            GameObject speedoPanel = CreateUIPanel(canvasObj.transform, "SpeedometerPanel", new Vector2(0f, 0f), new Vector2(25f, 25f), new Vector2(210f, 130f), new Color(0.08f, 0.10f, 0.14f, 0.88f));

            TMP_Text cSpeed = CreateUIText(speedoPanel.transform, "CurrentSpeed", "0", 38f, TextAlignmentOptions.Center, new Vector2(120f, 50f), new Vector2(15f, 54f), new Vector2(0f, 0f));
            cSpeed.fontStyle = FontStyles.Bold;

            TMP_Text spKmh = CreateUIText(speedoPanel.transform, "KmhUnit", "KM/H", 12f, TextAlignmentOptions.Center, new Vector2(120f, 20f), new Vector2(15f, 30f), new Vector2(0f, 0f));
            spKmh.color = new Color(0.7f, 0.75f, 0.8f, 1f);

            TMP_Text spBadge = CreateUIText(speedoPanel.transform, "LimitBadge", "MAX 40", 13f, TextAlignmentOptions.Center, new Vector2(70f, 24f), new Vector2(-15f, 68f), new Vector2(1f, 0f));
            spBadge.fontStyle = FontStyles.Bold;
            spBadge.color = new Color(1f, 0.85f, 0.2f, 1f);

            TMP_Text skWarning = CreateUIText(speedoPanel.transform, "SkidWarning", "SKIDDING! SLOW DOWN", 12f, TextAlignmentOptions.Center, new Vector2(190f, 20f), new Vector2(10f, 24f), new Vector2(0f, 0f));
            skWarning.fontStyle = FontStyles.Bold;
            skWarning.color = new Color(1f, 0.3f, 0.3f, 1f);

            TMP_Text offRoadWarn = CreateUIText(speedoPanel.transform, "OffRoadWarning", "[!] OFF-ROAD! RETURN", 11.5f, TextAlignmentOptions.Center, new Vector2(190f, 20f), new Vector2(10f, 4f), new Vector2(0f, 0f));
            offRoadWarn.fontStyle = FontStyles.Bold;
            offRoadWarn.color = new Color(1f, 0.25f, 0.25f, 1f);
            offRoadWarn.gameObject.SetActive(false);

            // 5. Mini-Map (Bottom-Right)
            GameObject miniMapPanel = CreateUIPanel(canvasObj.transform, "MiniMapPanel", new Vector2(1f, 0f), new Vector2(-25f, 25f), new Vector2(220f, 160f), new Color(0.08f, 0.12f, 0.08f, 0.90f));

            // Player Blip
            GameObject pBlipObj = FindOrCreateChild(miniMapPanel.transform, "PlayerBlip");
            RectTransform pBlipRect = GetOrAdd<RectTransform>(pBlipObj);
            pBlipRect.sizeDelta = new Vector2(10f, 14f);
            Image pBlipImg = GetOrAdd<Image>(pBlipObj);
            pBlipImg.color = new Color(0.2f, 0.9f, 0.3f, 1f);

            // Destination Blip
            GameObject dBlipObj = FindOrCreateChild(miniMapPanel.transform, "DestinationBlip");
            RectTransform dBlipRect = GetOrAdd<RectTransform>(dBlipObj);
            dBlipRect.sizeDelta = new Vector2(12f, 12f);
            Image dBlipImg = GetOrAdd<Image>(dBlipObj);
            dBlipImg.color = new Color(1f, 0.85f, 0.2f, 1f);

            Level6MiniMapController miniMapCtrl = GetOrAdd<Level6MiniMapController>(miniMapPanel);
            miniMapCtrl.BindReferences(playerCar, destination, miniMapPanel.GetComponent<RectTransform>(), pBlipRect, dBlipRect);

            // 6. Feedback Banner (Center-Bottom)
            GameObject feedbackBanner = CreateUIPanel(canvasObj.transform, "FeedbackBanner", new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(650f, 45f), new Color(0.06f, 0.08f, 0.12f, 0.94f));
            CanvasGroup fGroup = GetOrAdd<CanvasGroup>(feedbackBanner);

            TMP_Text fMsg = CreateUIText(feedbackBanner.transform, "FeedbackMessage", "Drive cautiously under rain.", 15f, TextAlignmentOptions.Center, new Vector2(630f, 35f), Vector2.zero, new Vector2(0.5f, 0.5f));
            fMsg.fontStyle = FontStyles.Bold;

            // 7. Modals: Fail Modal & Completion Certificate
            GameObject failModal = CreateFailModal(canvasObj.transform, out TMP_Text fReason, out Button fRetry, out Button fMenu);
            GameObject compModal = CreateCompletionCertificateModal(canvasObj.transform,
                out TMP_Text cTitle, out TMP_Text cSub, out TMP_Text cStars, out TMP_Text cScore,
                out TMP_Text cSafety, out TMP_Text cRoute, out TMP_Text cCollisions,
                out TMP_Text cHazards, out TMP_Text cUTurns, out Button cReplay, out Button cMenu2);

            Level6UIController uiCtrl = GetOrAdd<Level6UIController>(canvasObj);
            uiCtrl.BindDynamicReferences(
                mBadge, mTitle, mObj, mTimer,
                wStatus, gFill, gVal, wWind, hNotice,
                sVal, sfVal, sfFill, rsVal, rsFill,
                cSpeed, spBadge, skWarning,
                feedbackBanner, fGroup, fMsg,
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

            TMP_Text title = CreateUIText(modal.transform, "Title", "MISSION FAILED", 24f, TextAlignmentOptions.Center, new Vector2(460f, 40f), new Vector2(0f, -25f), new Vector2(0.5f, 1f));
            title.color = new Color(1f, 0.35f, 0.35f, 1f);
            title.fontStyle = FontStyles.Bold;

            fReason = CreateUIText(modal.transform, "Reason", "Extreme conditions overwhelmed the journey.", 15f, TextAlignmentOptions.Center, new Vector2(440f, 80f), new Vector2(0f, -80f), new Vector2(0.5f, 1f));

            fRetry = CreateButton(modal.transform, "RetryButton", "RETRY MISSION", new Vector2(0f, -185f), new Vector2(220f, 45f), new Color(0.85f, 0.3f, 0.2f, 1f));
            fMenu = CreateButton(modal.transform, "MenuButton", "MAIN MENU", new Vector2(0f, -245f), new Vector2(220f, 45f), new Color(0.25f, 0.3f, 0.38f, 1f));

            modal.SetActive(false);
            return modal;
        }

        private static GameObject CreateCompletionCertificateModal(Transform canvas,
            out TMP_Text cTitle, out TMP_Text cSub, out TMP_Text cStars, out TMP_Text cScore,
            out TMP_Text cSafety, out TMP_Text cRoute, out TMP_Text cCollisions,
            out TMP_Text cHazards, out TMP_Text cUTurns, out Button cReplay, out Button cMenu)
        {
            GameObject modal = CreateUIPanel(canvas, "CompletionCertificateModal", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 540f), new Color(0.08f, 0.12f, 0.18f, 0.98f));

            cTitle = CreateUIText(modal.transform, "CertTitle", "LEVEL 6 COMPLETED", 24f, TextAlignmentOptions.Center, new Vector2(560f, 35f), new Vector2(0f, -25f), new Vector2(0.5f, 1f));
            cTitle.color = new Color(1f, 0.85f, 0.2f, 1f);
            cTitle.fontStyle = FontStyles.Bold;

            cSub = CreateUIText(modal.transform, "CertSub", "EXTREME ROAD CONDITIONS MASTER CERTIFICATE", 13f, TextAlignmentOptions.Center, new Vector2(560f, 25f), new Vector2(0f, -60f), new Vector2(0.5f, 1f));
            cSub.color = new Color(0.7f, 0.85f, 1f, 1f);

            cStars = CreateUIText(modal.transform, "Stars", "3 OF 3 STARS", 32f, TextAlignmentOptions.Center, new Vector2(560f, 45f), new Vector2(0f, -95f), new Vector2(0.5f, 1f));
            cStars.color = new Color(1f, 0.85f, 0.2f, 1f);

            cScore = CreateUIText(modal.transform, "FinalScore", "Final Score: 450", 18f, TextAlignmentOptions.Center, new Vector2(500f, 30f), new Vector2(0f, -160f), new Vector2(0.5f, 1f));
            cScore.fontStyle = FontStyles.Bold;

            cSafety = CreateUIText(modal.transform, "FinalSafety", "Overall Safety: 95%", 15f, TextAlignmentOptions.Left, new Vector2(250f, 25f), new Vector2(65f, -205f), new Vector2(0f, 1f));
            cRoute = CreateUIText(modal.transform, "FinalRoute", "Route Selection: 100%", 15f, TextAlignmentOptions.Left, new Vector2(250f, 25f), new Vector2(330f, -205f), new Vector2(0f, 1f));

            cCollisions = CreateUIText(modal.transform, "FinalCollisions", "Collisions: 0", 15f, TextAlignmentOptions.Left, new Vector2(250f, 25f), new Vector2(65f, -240f), new Vector2(0f, 1f));
            cHazards = CreateUIText(modal.transform, "FinalHazards", "Hazards Avoided: 5", 15f, TextAlignmentOptions.Left, new Vector2(250f, 25f), new Vector2(330f, -240f), new Vector2(0f, 1f));

            cUTurns = CreateUIText(modal.transform, "FinalUTurns", "Safe U-Turns: 1", 15f, TextAlignmentOptions.Center, new Vector2(500f, 25f), new Vector2(0f, -275f), new Vector2(0.5f, 1f));

            cReplay = CreateButton(modal.transform, "ReplayBtn", "REPLAY LEVEL", new Vector2(-125f, -440f), new Vector2(210f, 48f), new Color(0.18f, 0.55f, 0.32f, 1f));
            cMenu = CreateButton(modal.transform, "MenuBtn", "MAIN MENU", new Vector2(125f, -440f), new Vector2(210f, 48f), new Color(0.22f, 0.38f, 0.58f, 1f));

            modal.SetActive(false);
            return modal;
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color col, Sprite sprite = null)
        {
            GameObject go = FindOrCreateChild(parent, name);
            RectTransform rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = GetOrAdd<Image>(go);
            img.color = col;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            return go;
        }

        private static Button CreateButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color btnColor)
        {
            GameObject btnObj = FindOrCreateChild(parent, name);
            RectTransform rt = GetOrAdd<RectTransform>(btnObj);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = GetOrAdd<Image>(btnObj);
            img.color = btnColor;

            Button btn = GetOrAdd<Button>(btnObj);

            TMP_Text txt = CreateUIText(btnObj.transform, "Text", text, 15f, TextAlignmentOptions.Center, size, Vector2.zero, new Vector2(0.5f, 0.5f));
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;

            return btn;
        }

        private static TMP_Text CreateUIText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align, Vector2 size, Vector2 anchoredPos, Vector2 pivot)
        {
            GameObject go = FindOrCreateChild(parent, name);
            RectTransform rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = pivot;
            rt.anchorMax = pivot;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            TMP_Text tmp = GetOrAdd<TextMeshProUGUI>(go);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            return tmp;
        }

        private static void SetFullScreen(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetRect(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            if (rt == null) return;
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
        #endregion

        #region Helpers & Reflection
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

        private static void ClearChildren(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(t.GetChild(i).gameObject);
            }
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static void SetReference(object target, string fieldName, object value)
        {
            if (target == null) return;
            FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fi != null) fi.SetValue(target, value);
        }

        private static void SetVector2(object target, string fieldName, Vector2 value)
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
#endif
