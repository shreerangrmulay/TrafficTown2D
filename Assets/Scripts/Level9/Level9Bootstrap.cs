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

namespace TrafficTown2D.Level9
{
    /// <summary>
    /// Self-contained runtime bootstrapper and builder for Level 9 — ROUNDABOUT RULES.
    /// Builds the interconnected 3-roundabout road network, AI circulating traffic,
    /// pedestrian crossings, player car, HUD, and mission architecture.
    /// </summary>
    public class Level9Bootstrap : MonoBehaviour
    {
        private const string WorldSquareSpritePath = "Assets/Sprites/Generated/WorldSquare.png";
        private const string WorldCircleSpritePath = "Assets/Sprites/Generated/WorldCircle.png";
        private const string CarBlueTopDownPath = "Assets/Sprites/Vehicles/CarBlueTopDown.png";
        private const string CarRedTopDownPath = "Assets/Sprites/Vehicles/CarRedTopDown.png";
        private const string TaxiTopDownPath = "Assets/Sprites/Vehicles/TaxiTopDown.png";
        private const string BusTopDownPath = "Assets/Sprites/Vehicles/BusTopDown.png";
        private const string PedestrianBoyPath = "Assets/Sprites/Characters/ChildBoyTopDown.png";
        private const string PedestrianDarkPath = "Assets/Sprites/Characters/AdultPedestrianDarkTopDown.png";
        private const string CheckeredFinishPath = "Assets/Sprites/Environment/CheckeredFinish.png";
        private const string ZebraCrossingPath = "Assets/Sprites/Environment/ZebraCrossingStripes.png";
        private const string RoundedPanelPath = "Assets/UI/RoundedPanel.png";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitSceneListener()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Level9" || scene.name == "Level9_Roundabout")
            {
                EnsureLevel9Scene();
            }
        }

        private void Awake()
        {
            EnsureLevel9Scene();
        }

        public static void EnsureLevel9Scene()
        {
            Camera cam = EnsureCamera();
            EnsureGlobalLight();
            EnsureEventSystem();
            CleanLegacyElements();

            GameObject services = FindOrCreate("Services");
            GetOrAdd<SceneLoader>(services);
            GetOrAdd<GameManager>(services);

            // 1. Environment & 3-Roundabout Road Network
            GameObject env = FindOrCreate("Environment");
            Transform existingRoad = env.transform.Find("RoadNetwork");
            if (existingRoad == null)
            {
                BuildCompleteRoadNetwork(env.transform);
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
            Level9CameraFollow camFollow = cam.GetComponent<Level9CameraFollow>();
            if (camFollow == null) camFollow = cam.gameObject.AddComponent<Level9CameraFollow>();
            camFollow.SetTarget(playerCar.transform);
            camFollow.SetBounds(-22f, 62f, -25f, 105f);

            // 3. Gameplay Managers
            GameObject gameplay = FindOrCreate("Gameplay");
            Level9RoundaboutManager mgr = GetOrAdd<Level9RoundaboutManager>(gameplay);
            RoundaboutSafetyEvaluator evaluator = GetOrAdd<RoundaboutSafetyEvaluator>(gameplay);
            RoundaboutMissionController missionCtrl = GetOrAdd<RoundaboutMissionController>(gameplay);

            evaluator.BindPlayer(playerCar);
            missionCtrl.BindDependencies(playerCar, evaluator);

            // 4. Connect All Zone Triggers to Evaluator and Mission Controller
            ConnectZoneTriggers(evaluator, missionCtrl);

            // 5. Spawn AI Traffic & Pedestrians
            SpawnTrafficAndPedestrians(env.transform, evaluator);

            // 6. UI Hierarchy
            Level9UIController uiCtrl = EnsureUI();

            // 7. Educational Tutorial Intro Panel
            EnsureIntroPanel(playerCar);

            // 8. Turn Indicator HUD
            CarIndicatorHUD.EnsureHUD(indCtrl);

            // 9. Bootstrap Component
            GameObject bsObj = FindOrCreate("Level9Bootstrap");
            GetOrAdd<Level9Bootstrap>(bsObj);

            Debug.Log("[Level9Bootstrap] Level 9 — ROUNDABOUT RULES initialized successfully.");
        }

        #region Setup Helpers
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
            cam.orthographicSize = 8.5f;
            cam.transform.position = new Vector3(0f, -10f, -10f);
            cam.backgroundColor = new Color(0.20f, 0.48f, 0.24f, 1f); // Rich grass green
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

        private static void CleanLegacyElements()
        {
            string[] legacyNames = new string[] { "Traffic", "RoundaboutZones", "Ground", "LeftRoad", "RightRoad", "RoundaboutOuter", "RoundaboutInner", "CenterIsland", "CircleLane", "YieldSign", "SidewalkTop", "SidewalkBottom", "UI" };
            foreach (var n in legacyNames)
            {
                GameObject g = GameObject.Find(n);
                if (g != null && g.transform.parent == null)
                {
                    UnityEngine.Object.DestroyImmediate(g);
                }
            }

            GameObject oldEnv = GameObject.Find("Environment");
            if (oldEnv != null && oldEnv.transform.Find("RoadNetwork") == null)
            {
                UnityEngine.Object.DestroyImmediate(oldEnv);
            }

            GameObject oldGameplay = GameObject.Find("Gameplay");
            if (oldGameplay != null && oldGameplay.GetComponent<Level9RoundaboutManager>() == null)
            {
                UnityEngine.Object.DestroyImmediate(oldGameplay);
            }

            GameObject oldPlayer = GameObject.Find("PlayerCar");
            if (oldPlayer != null && oldPlayer.GetComponent<Level3PlayerCar>() == null)
            {
                UnityEngine.Object.DestroyImmediate(oldPlayer);
            }
        }
        #endregion

        #region Road Network Construction
        private static void BuildCompleteRoadNetwork(Transform env)
        {
            GameObject network = new GameObject("RoadNetwork");
            network.transform.SetParent(env, false);

            Sprite square = LoadSpriteAsset(WorldSquareSpritePath);
            Sprite circle = LoadSpriteAsset(WorldCircleSpritePath);
            Sprite zebra = LoadSpriteAsset(ZebraCrossingPath);
            Sprite finish = LoadSpriteAsset(CheckeredFinishPath);

            Color roadCol = new Color(0.12f, 0.13f, 0.15f, 1f);
            Color shoulderCol = new Color(0.36f, 0.38f, 0.40f, 1f);
            Color curbCol = new Color(0.78f, 0.80f, 0.82f, 1f);
            Color islandCol = new Color(0.25f, 0.60f, 0.30f, 1f);
            Color lineCol = new Color(0.92f, 0.90f, 0.85f, 0.95f);
            Color giveWayCol = new Color(1f, 0.85f, 0.15f, 1f);

            // ==========================================
            // 1. ROUNDABOUT 1 — LEARN THE BASICS (Center: 0, 0)
            // ==========================================
            BuildRoundaboutGeometry(network.transform, "Roundabout_1", new Vector2(0f, 0f), 8.5f, 3.8f, roadCol, shoulderCol, curbCol, islandCol, lineCol, circle, square);

            // South Approach Road (Player Spawn Corridor: Y = -24 to -8.5, X = 0)
            BuildRoadCorridor(network.transform, "R1_SouthApproach", new Vector2(0f, -16.25f), new Vector2(7.2f, 15.5f), roadCol, shoulderCol, curbCol, lineCol, square, true);

            // Give-Way Line at South Entry (Y = -8.5)
            CreateGiveWayLine(network.transform, "R1_GiveWayLine", new Vector2(0f, -8.5f), 7.0f, giveWayCol, square);

            // Triggers for Roundabout 1
            CreateZoneTrigger(network.transform, "R1_ApproachZone", new Vector2(0f, -13f), new Vector2(7.2f, 8f), RoundaboutID.Roundabout1_Basics, RoundaboutZoneType.Approach, 1, 25f);
            CreateZoneTrigger(network.transform, "R1_GiveWayZone", new Vector2(0f, -8.5f), new Vector2(7.2f, 2.5f), RoundaboutID.Roundabout1_Basics, RoundaboutZoneType.GiveWayLine, 1, 25f);
            CreateCirculationTrigger(network.transform, "R1_CirculationZone", new Vector2(0f, 0f), 8.5f, RoundaboutID.Roundabout1_Basics);
            CreateZoneTrigger(network.transform, "R1_ExitEastZone", new Vector2(9.5f, 0f), new Vector2(3f, 7.2f), RoundaboutID.Roundabout1_Basics, RoundaboutZoneType.ExitArm, 1, 35f);
            CreateCheckpointTrigger(network.transform, "Trigger_R1_ExitEast", new Vector2(10.5f, 0f), new Vector2(2f, 7.2f), "R1_ExitEast");

            // Road signs around R1
            CreateSignPost(network.transform, "Sign_R1_Ahead", new Vector3(4.5f, -18f, 0f), "ROUNDABOUT\nAHEAD", new Color(0.9f, 0.45f, 0.1f, 1f));
            CreateSignPost(network.transform, "Sign_R1_GiveWay", new Vector3(4.5f, -9f, 0f), "GIVE WAY\nTO RIGHT", new Color(0.85f, 0.15f, 0.15f, 1f));

            // ==========================================
            // 2. CONNECTING ROAD 1 (R1 East Exit to R2 South Approach)
            // Starts at (8.5, 0), runs East to (34, 0), then curves North to (42, 12.5)
            // ==========================================
            BuildRoadCorridor(network.transform, "ConnRoad1_Straight", new Vector2(21.25f, 0f), new Vector2(25.5f, 7.2f), roadCol, shoulderCol, curbCol, lineCol, square, false);

            // Curved junction from (34, 0) bending North to (42, 10)
            BuildRoadCorner(network.transform, "ConnRoad1_Corner", new Vector2(34f, 0f), new Vector2(42f, 10f), 7.2f, roadCol, shoulderCol, curbCol, lineCol, square);
            BuildRoadCorridor(network.transform, "ConnRoad1_ToR2", new Vector2(42f, 11f), new Vector2(7.2f, 4f), roadCol, shoulderCol, curbCol, lineCol, square, true);

            // ==========================================
            // 3. ROUNDABOUT 2 — BUSY TRAFFIC (Center: 42, 22)
            // ==========================================
            BuildRoundaboutGeometry(network.transform, "Roundabout_2", new Vector2(42f, 22f), 9.5f, 4.4f, roadCol, shoulderCol, curbCol, islandCol, lineCol, circle, square);

            // Give-Way Line at R2 South Entry (Y = 12.5)
            CreateGiveWayLine(network.transform, "R2_GiveWayLine", new Vector2(42f, 12.5f), 7.0f, giveWayCol, square);

            // R2 Triggers
            CreateZoneTrigger(network.transform, "R2_ApproachZone", new Vector2(42f, 10f), new Vector2(7.2f, 4.5f), RoundaboutID.Roundabout2_BusyTraffic, RoundaboutZoneType.Approach, 1, 25f);
            CreateZoneTrigger(network.transform, "R2_GiveWayZone", new Vector2(42f, 12.5f), new Vector2(7.2f, 2.5f), RoundaboutID.Roundabout2_BusyTraffic, RoundaboutZoneType.GiveWayLine, 1, 25f);
            CreateCirculationTrigger(network.transform, "R2_CirculationZone", new Vector2(42f, 22f), 9.5f, RoundaboutID.Roundabout2_BusyTraffic);
            CreateCheckpointTrigger(network.transform, "Trigger_R2_Entered", new Vector2(42f, 15f), new Vector2(7.2f, 2f), "R2_Entered");

            // R2 North Exit Road (Y = 31.5 to 45, X = 42)
            BuildRoadCorridor(network.transform, "R2_NorthExit", new Vector2(42f, 38.25f), new Vector2(7.2f, 13.5f), roadCol, shoulderCol, curbCol, lineCol, square, true);

            // Zebra Crossing on R2 North Exit (Y = 36.5)
            CreateZebraCrossing(network.transform, "R2_ZebraCrossing", new Vector2(42f, 36.5f), new Vector2(7.2f, 2.6f), zebra);
            CreateZoneTrigger(network.transform, "R2_ZebraZone", new Vector2(42f, 36.5f), new Vector2(7.2f, 4.0f), RoundaboutID.Roundabout2_BusyTraffic, RoundaboutZoneType.ZebraCrossing, 1, 15f);
            CreateZoneTrigger(network.transform, "R2_ExitZone", new Vector2(42f, 33f), new Vector2(7.2f, 3.0f), RoundaboutID.Roundabout2_BusyTraffic, RoundaboutZoneType.ExitArm, 1, 30f);
            CreateCheckpointTrigger(network.transform, "Trigger_R2_PedestrianCleared", new Vector2(42f, 40f), new Vector2(7.2f, 2.0f), "R2_PedestrianCleared");

            CreateSignPost(network.transform, "Sign_R2_Pedestrian", new Vector3(46.5f, 35f, 0f), "PEDESTRIAN\nCROSSING", new Color(0.15f, 0.45f, 0.85f, 1f));

            // ==========================================
            // 4. CONNECTING ROAD 2 (R2 North Exit to R3 South Approach)
            // From (42, 45) curving West to (0, 56)
            // ==========================================
            BuildRoadCorner(network.transform, "ConnRoad2_Corner1", new Vector2(42f, 45f), new Vector2(34f, 53f), 7.2f, roadCol, shoulderCol, curbCol, lineCol, square);
            BuildRoadCorridor(network.transform, "ConnRoad2_StraightWest", new Vector2(17f, 53f), new Vector2(34f, 7.2f), roadCol, shoulderCol, curbCol, lineCol, square, false);
            BuildRoadCorner(network.transform, "ConnRoad2_Corner2", new Vector2(0f, 53f), new Vector2(0f, 61f), 7.2f, roadCol, shoulderCol, curbCol, lineCol, square);

            CreateSignPost(network.transform, "Sign_R3_Route", new Vector3(18f, 57.5f, 0f), "TOWN CENTER\n(3RD EXIT) ↑", new Color(0.12f, 0.65f, 0.35f, 1f));

            // ==========================================
            // 5. ROUNDABOUT 3 — FINAL CHALLENGE (Center: 0, 72)
            // ==========================================
            BuildRoundaboutGeometry(network.transform, "Roundabout_3", new Vector2(0f, 72f), 11.0f, 5.0f, roadCol, shoulderCol, curbCol, islandCol, lineCol, circle, square);

            // Give-Way Line at R3 South Entry (Y = 61.0)
            CreateGiveWayLine(network.transform, "R3_GiveWayLine", new Vector2(0f, 61.0f), 7.0f, giveWayCol, square);

            // R3 Triggers
            CreateZoneTrigger(network.transform, "R3_ApproachZone", new Vector2(0f, 58.5f), new Vector2(7.2f, 4f), RoundaboutID.Roundabout3_FinalChallenge, RoundaboutZoneType.Approach, 3, 25f);
            CreateZoneTrigger(network.transform, "R3_GiveWayZone", new Vector2(0f, 61.0f), new Vector2(7.2f, 2.5f), RoundaboutID.Roundabout3_FinalChallenge, RoundaboutZoneType.GiveWayLine, 3, 25f);
            CreateCirculationTrigger(network.transform, "R3_CirculationZone", new Vector2(0f, 72f), 11.0f, RoundaboutID.Roundabout3_FinalChallenge);
            CreateCheckpointTrigger(network.transform, "Trigger_R3_Approach", new Vector2(0f, 58.5f), new Vector2(7.2f, 2.5f), "R3_Approach");

            // R3 Other arms (Decorative / Stub arms)
            // 1st Exit (East):
            BuildRoadCorridor(network.transform, "R3_Exit1_East", new Vector2(15f, 72f), new Vector2(8f, 7.2f), roadCol, shoulderCol, curbCol, lineCol, square, false);
            CreateSignPost(network.transform, "Sign_R3_Exit1", new Vector3(16f, 76.5f, 0f), "EXIT 1:\nINDUSTRIAL", new Color(0.45f, 0.45f, 0.45f, 1f));

            // 2nd Exit (West):
            BuildRoadCorridor(network.transform, "R3_Exit2_West", new Vector2(-15f, 72f), new Vector2(8f, 7.2f), roadCol, shoulderCol, curbCol, lineCol, square, false);
            CreateSignPost(network.transform, "Sign_R3_Exit2", new Vector3(-16f, 76.5f, 0f), "EXIT 2:\nRESIDENTIAL", new Color(0.25f, 0.5f, 0.75f, 1f));

            // 3rd Exit (North to Town Center / Finish):
            BuildRoadCorridor(network.transform, "R3_Exit3_North", new Vector2(0f, 90f), new Vector2(7.2f, 16f), roadCol, shoulderCol, curbCol, lineCol, square, true);
            CreateZoneTrigger(network.transform, "R3_ExitZone", new Vector2(0f, 84f), new Vector2(7.2f, 3.5f), RoundaboutID.Roundabout3_FinalChallenge, RoundaboutZoneType.ExitArm, 3, 30f);

            // Zebra Crossing on Final Stretch (Y = 88)
            CreateZebraCrossing(network.transform, "R3_ZebraCrossing", new Vector2(0f, 88f), new Vector2(7.2f, 2.6f), zebra);
            CreateZoneTrigger(network.transform, "R3_ZebraZone", new Vector2(0f, 88f), new Vector2(7.2f, 4.0f), RoundaboutID.Roundabout3_FinalChallenge, RoundaboutZoneType.ZebraCrossing, 3, 15f);

            // Checkered Finish Line (Y = 96)
            GameObject finObj = CreateSprite(network.transform, "FinishLineVisual", new Vector3(0f, 96f, 0f), new Vector3(7.2f, 1.8f, 1f), Color.white, 5, finish);
            CreateCheckpointTrigger(network.transform, "Trigger_FinishLine", new Vector2(0f, 96f), new Vector2(7.2f, 2.5f), "FinishLine");

            // Roadside Scenery (Trees, Street Lamps)
            CreateEnvironmentScenery(network.transform, square, circle);
        }

        private static void BuildRoundaboutGeometry(Transform parent, string name, Vector2 center, float outerRadius, float innerRadius, Color roadCol, Color shoulderCol, Color curbCol, Color grassCol, Color lineCol, Sprite circle, Sprite square)
        {
            GameObject rb = new GameObject(name);
            rb.transform.SetParent(parent, false);
            rb.transform.position = new Vector3(center.x, center.y, 0f);

            // Shoulder ring
            CreateSprite(rb.transform, "OuterShoulder", Vector3.zero, new Vector3((outerRadius + 0.9f) * 2f, (outerRadius + 0.9f) * 2f, 1f), shoulderCol, -2, circle);
            // Road circle
            CreateSprite(rb.transform, "RoadCircle", Vector3.zero, new Vector3(outerRadius * 2f, outerRadius * 2f, 1f), roadCol, 0, circle);
            // Outer curb ring
            CreateSprite(rb.transform, "OuterCurb", Vector3.zero, new Vector3((outerRadius + 0.3f) * 2f, (outerRadius + 0.3f) * 2f, 1f), curbCol, 1, circle);

            // Center dash ring
            float midRadius = (outerRadius + innerRadius) * 0.5f;
            int dashes = 16;
            for (int i = 0; i < dashes; i++)
            {
                float ang = i * (360f / dashes) * Mathf.Deg2Rad;
                Vector3 dPos = new Vector3(Mathf.Cos(ang) * midRadius, Mathf.Sin(ang) * midRadius, 0f);
                GameObject dash = CreateSprite(rb.transform, $"Dash_{i}", dPos, new Vector3(1.2f, 0.16f, 1f), lineCol, 2, square);
                dash.transform.rotation = Quaternion.Euler(0f, 0f, (i * (360f / dashes)) + 90f);
            }

            // Central island curb and grass
            CreateSprite(rb.transform, "InnerCurb", Vector3.zero, new Vector3((innerRadius + 0.35f) * 2f, (innerRadius + 0.35f) * 2f, 1f), curbCol, 1, circle);
            CreateSprite(rb.transform, "CenterIslandGrass", Vector3.zero, new Vector3(innerRadius * 2f, innerRadius * 2f, 1f), grassCol, 2, circle);

            // Central Monument / Flowerbed
            CreateSprite(rb.transform, "IslandMonument", Vector3.zero, new Vector3(innerRadius * 0.7f, innerRadius * 0.7f, 1f), new Color(0.85f, 0.78f, 0.65f, 1f), 3, circle);
            CreateSprite(rb.transform, "MonumentStatue", Vector3.zero, new Vector3(innerRadius * 0.35f, innerRadius * 0.35f, 1f), new Color(0.45f, 0.50f, 0.55f, 1f), 4, square);

            // Central Island Solid Collider
            CircleCollider2D col = rb.AddComponent<CircleCollider2D>();
            col.radius = innerRadius;
            col.isTrigger = false;
        }

        private static void BuildRoadCorridor(Transform parent, string name, Vector2 center, Vector2 size, Color roadCol, Color shoulderCol, Color curbCol, Color lineCol, Sprite square, bool isVertical)
        {
            GameObject rd = new GameObject(name);
            rd.transform.SetParent(parent, false);
            rd.transform.position = new Vector3(center.x, center.y, 0f);

            // Shoulder
            CreateSprite(rd.transform, "Shoulder", Vector3.zero, new Vector3(size.x + 1.2f, size.y + (isVertical ? 0f : 1.2f), 1f), shoulderCol, -2, square);
            // Asphalt
            CreateSprite(rd.transform, "Asphalt", Vector3.zero, new Vector3(size.x, size.y, 1f), roadCol, 0, square);

            if (isVertical)
            {
                // Curbs on left and right
                float halfW = size.x * 0.5f;
                GameObject cLeft = CreateSprite(rd.transform, "CurbLeft", new Vector3(-halfW - 0.15f, 0f, 0f), new Vector3(0.3f, size.y, 1f), curbCol, 1, square);
                BoxCollider2D lCol = cLeft.AddComponent<BoxCollider2D>();
                lCol.size = new Vector2(0.3f, size.y);

                GameObject cRight = CreateSprite(rd.transform, "CurbRight", new Vector3(halfW + 0.15f, 0f, 0f), new Vector3(0.3f, size.y, 1f), curbCol, 1, square);
                BoxCollider2D rCol = cRight.AddComponent<BoxCollider2D>();
                rCol.size = new Vector2(0.3f, size.y);

                // Dashed Centerline
                for (float y = -size.y * 0.45f; y <= size.y * 0.45f; y += 3.2f)
                {
                    CreateSprite(rd.transform, $"Dash_{y:F0}", new Vector3(0f, y, 0f), new Vector3(0.18f, 1.6f, 1f), lineCol, 2, square);
                }
            }
            else
            {
                // Curbs on top and bottom
                float halfH = size.y * 0.5f;
                GameObject cTop = CreateSprite(rd.transform, "CurbTop", new Vector3(0f, halfH + 0.15f, 0f), new Vector3(size.x, 0.3f, 1f), curbCol, 1, square);
                BoxCollider2D tCol = cTop.AddComponent<BoxCollider2D>();
                tCol.size = new Vector2(size.x, 0.3f);

                GameObject cBot = CreateSprite(rd.transform, "CurbBottom", new Vector3(0f, -halfH - 0.15f, 0f), new Vector3(size.x, 0.3f, 1f), curbCol, 1, square);
                BoxCollider2D bCol = cBot.AddComponent<BoxCollider2D>();
                bCol.size = new Vector2(size.x, 0.3f);

                // Dashed Centerline
                for (float x = -size.x * 0.45f; x <= size.x * 0.45f; x += 3.2f)
                {
                    CreateSprite(rd.transform, $"Dash_{x:F0}", new Vector3(x, 0f, 0f), new Vector3(1.6f, 0.18f, 1f), lineCol, 2, square);
                }
            }
        }

        private static void BuildRoadCorner(Transform parent, string name, Vector2 from, Vector2 to, float width, Color roadCol, Color shoulderCol, Color curbCol, Color lineCol, Sprite square)
        {
            // Simple connector patch between two road segments
            GameObject corner = new GameObject(name);
            corner.transform.SetParent(parent, false);
            Vector2 mid = (from + to) * 0.5f;
            corner.transform.position = new Vector3(mid.x, mid.y, 0f);

            Vector2 dir = to - from;
            float len = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            corner.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            CreateSprite(corner.transform, "Shoulder", Vector3.zero, new Vector3(len + 1.2f, width + 1.2f, 1f), shoulderCol, -2, square);
            CreateSprite(corner.transform, "Asphalt", Vector3.zero, new Vector3(len + 0.5f, width, 1f), roadCol, 0, square);
            CreateSprite(corner.transform, "Dash", Vector3.zero, new Vector3(len * 0.7f, 0.18f, 1f), lineCol, 2, square);
        }

        private static void CreateGiveWayLine(Transform parent, string name, Vector2 pos, float width, Color col, Sprite square)
        {
            GameObject gw = new GameObject(name);
            gw.transform.SetParent(parent, false);
            gw.transform.position = new Vector3(pos.x, pos.y, 0f);

            // Double dashed line indicating Give-Way
            for (float x = -width * 0.4f; x <= width * 0.4f; x += 1.2f)
            {
                CreateSprite(gw.transform, $"D1_{x:F1}", new Vector3(x, 0.15f, 0f), new Vector3(0.7f, 0.18f, 1f), col, 3, square);
                CreateSprite(gw.transform, $"D2_{x:F1}", new Vector3(x, -0.15f, 0f), new Vector3(0.7f, 0.18f, 1f), col, 3, square);
            }
        }

        private static void CreateZebraCrossing(Transform parent, string name, Vector2 pos, Vector2 size, Sprite zebra)
        {
            GameObject z = new GameObject(name);
            z.transform.SetParent(parent, false);
            z.transform.position = new Vector3(pos.x, pos.y, 0f);

            if (zebra != null)
            {
                CreateSprite(z.transform, "ZebraStripes", Vector3.zero, new Vector3(size.x, size.y, 1f), Color.white, 3, zebra);
            }
            else
            {
                // Procedural fallback
                Sprite sq = LoadSpriteAsset(WorldSquareSpritePath);
                int stripes = 6;
                float step = size.x / stripes;
                for (int i = 0; i < stripes; i++)
                {
                    float x = -size.x * 0.5f + step * (i + 0.5f);
                    CreateSprite(z.transform, $"Stripe_{i}", new Vector3(x, 0f, 0f), new Vector3(step * 0.6f, size.y, 1f), Color.white, 3, sq);
                }
            }
        }

        private static void CreateEnvironmentScenery(Transform parent, Sprite square, Sprite circle)
        {
            GameObject scenery = new GameObject("Scenery");
            scenery.transform.SetParent(parent, false);

            // Roadside Trees
            Vector2[] treeSpots = new Vector2[]
            {
                new Vector2(-6f, -14f), new Vector2(6f, -14f),
                new Vector2(-6f, -6f),  new Vector2(6f, -6f),
                new Vector2(-12f, 2f),  new Vector2(-12f, -4f),
                new Vector2(18f, 4f),   new Vector2(26f, 4f),
                new Vector2(18f, -4f),  new Vector2(26f, -4f),
                new Vector2(34f, -4f),  new Vector2(34f, 6f),
                new Vector2(36f, 14f),  new Vector2(48f, 14f),
                new Vector2(54f, 22f),  new Vector2(54f, 28f),
                new Vector2(48f, 42f),  new Vector2(36f, 42f),
                new Vector2(26f, 50f),  new Vector2(12f, 50f),
                new Vector2(-8f, 64f),  new Vector2(8f, 64f),
                new Vector2(-14f, 80f), new Vector2(14f, 80f),
                new Vector2(-6f, 92f),  new Vector2(6f, 92f)
            };

            foreach (var spot in treeSpots)
            {
                CreateSprite(scenery.transform, $"Tree_{spot.x:F0}_{spot.y:F0}", new Vector3(spot.x, spot.y, 0f), new Vector3(2.2f, 2.2f, 1f), new Color(0.16f, 0.42f, 0.20f, 1f), 6, circle);
            }
        }
        #endregion

        #region Trigger Helpers
        private static RoundaboutZoneTrigger CreateZoneTrigger(Transform parent, string name, Vector2 pos, Vector2 size, RoundaboutID rId, RoundaboutZoneType zType, int exitNum, float limit)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = new Vector3(pos.x, pos.y, 0f);

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;

            RoundaboutZoneTrigger trigger = obj.AddComponent<RoundaboutZoneTrigger>();
            trigger.roundaboutId = rId;
            trigger.zoneType = zType;
            trigger.exitNumber = exitNum;
            trigger.zoneName = name;
            trigger.approachSpeedLimitKmh = limit;

            return trigger;
        }

        private static RoundaboutZoneTrigger CreateCirculationTrigger(Transform parent, string name, Vector2 center, float radius, RoundaboutID rId)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = new Vector3(center.x, center.y, 0f);

            CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
            col.radius = radius;
            col.isTrigger = true;

            RoundaboutZoneTrigger trigger = obj.AddComponent<RoundaboutZoneTrigger>();
            trigger.roundaboutId = rId;
            trigger.zoneType = RoundaboutZoneType.Circulation;
            trigger.centerPoint = center;
            trigger.zoneName = name;

            return trigger;
        }

        private static void CreateCheckpointTrigger(Transform parent, string name, Vector2 pos, Vector2 size, string checkpointId)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = new Vector3(pos.x, pos.y, 0f);

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;

            RoundaboutCheckpointTrigger cp = obj.AddComponent<RoundaboutCheckpointTrigger>();
            cp.checkpointId = checkpointId;
        }

        private static void ConnectZoneTriggers(RoundaboutSafetyEvaluator evaluator, RoundaboutMissionController missionCtrl)
        {
            RoundaboutZoneTrigger[] triggers = UnityEngine.Object.FindObjectsByType<RoundaboutZoneTrigger>(FindObjectsSortMode.None);
            foreach (var t in triggers)
            {
                t.PlayerEntered += (zone, col) => evaluator.OnZoneEntered(zone);
                t.PlayerExited += (zone, col) => evaluator.OnZoneExited(zone);
            }

            RoundaboutCheckpointTrigger[] cps = UnityEngine.Object.FindObjectsByType<RoundaboutCheckpointTrigger>(FindObjectsSortMode.None);
            foreach (var cp in cps)
            {
                cp.CheckpointReached += (id) => missionCtrl.OnCheckpointReached(id);
            }
        }
        #endregion

        #region Player Car Construction
        private static GameObject BuildPlayerCar(Transform env)
        {
            GameObject car = new GameObject("PlayerCar");
            car.transform.SetParent(env, false);
            // Spawn at South approach of Roundabout 1, in the left-hand traffic driving lane (X = 1.6, Y = -18)
            car.transform.position = new Vector3(1.6f, -18f, 0f);
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

            car.AddComponent<Level3PlayerCar>();
            car.AddComponent<CarIndicatorController>();

            return car;
        }
        #endregion

        #region Traffic & Pedestrian Spawning
        private static void SpawnTrafficAndPedestrians(Transform env, RoundaboutSafetyEvaluator evaluator)
        {
            Transform existingTraffic = env.Find("TrafficAndPedestrians");
            if (existingTraffic != null)
            {
                RoundaboutTrafficVehicle[] existingVehicles = existingTraffic.GetComponentsInChildren<RoundaboutTrafficVehicle>();
                foreach (var v in existingVehicles) evaluator.RegisterTraffic(v);
                RoundaboutPedestrian[] existingPeds = existingTraffic.GetComponentsInChildren<RoundaboutPedestrian>();
                foreach (var p in existingPeds) evaluator.RegisterPedestrian(p);
                return;
            }

            GameObject trafficRoot = new GameObject("TrafficAndPedestrians");
            trafficRoot.transform.SetParent(env, false);

            Sprite redCar = LoadSpriteAsset(CarRedTopDownPath);
            Sprite taxiCar = LoadSpriteAsset(TaxiTopDownPath);
            Sprite bus = LoadSpriteAsset(BusTopDownPath);
            Sprite boyPed = LoadSpriteAsset(PedestrianBoyPath);
            Sprite darkPed = LoadSpriteAsset(PedestrianDarkPath);

            // ==========================================
            // Roundabout 1 Traffic (1 circulating vehicle)
            // ==========================================
            List<Vector2> r1Path = GenerateCircularPath(new Vector2(0f, 0f), 6.15f, 16, true);
            RoundaboutTrafficVehicle r1Car = SpawnAIPlaneVehicle(trafficRoot.transform, "R1_TrafficCar", r1Path, redCar, 3.8f);
            evaluator.RegisterTraffic(r1Car);

            // ==========================================
            // Roundabout 2 Traffic (2 circulating vehicles spaced out)
            // ==========================================
            List<Vector2> r2Path = GenerateCircularPath(new Vector2(42f, 22f), 7.0f, 18, true);
            RoundaboutTrafficVehicle r2Car1 = SpawnAIPlaneVehicle(trafficRoot.transform, "R2_TrafficCar_1", r2Path, taxiCar, 4.0f);
            evaluator.RegisterTraffic(r2Car1);

            // Shift second car along path
            List<Vector2> r2PathShifted = ShiftPath(r2Path, 9);
            RoundaboutTrafficVehicle r2Car2 = SpawnAIPlaneVehicle(trafficRoot.transform, "R2_TrafficCar_2", r2PathShifted, redCar, 4.0f);
            evaluator.RegisterTraffic(r2Car2);

            // ==========================================
            // Roundabout 2 Pedestrian (Zebra Crossing at Y = 36.5)
            // ==========================================
            RoundaboutPedestrian r2Ped = SpawnPedestrian(trafficRoot.transform, "R2_CrossingPedestrian", new Vector2(38.8f, 36.5f), new Vector2(45.2f, 36.5f), boyPed, 1.4f);
            evaluator.RegisterPedestrian(r2Ped);

            // ==========================================
            // Roundabout 3 Traffic (3 circulating vehicles)
            // ==========================================
            List<Vector2> r3Path = GenerateCircularPath(new Vector2(0f, 72f), 8.1f, 20, true);
            RoundaboutTrafficVehicle r3Car1 = SpawnAIPlaneVehicle(trafficRoot.transform, "R3_TrafficCar_1", r3Path, bus, 3.6f);
            evaluator.RegisterTraffic(r3Car1);

            List<Vector2> r3PathShifted1 = ShiftPath(r3Path, 7);
            RoundaboutTrafficVehicle r3Car2 = SpawnAIPlaneVehicle(trafficRoot.transform, "R3_TrafficCar_2", r3PathShifted1, taxiCar, 4.2f);
            evaluator.RegisterTraffic(r3Car2);

            List<Vector2> r3PathShifted2 = ShiftPath(r3Path, 14);
            RoundaboutTrafficVehicle r3Car3 = SpawnAIPlaneVehicle(trafficRoot.transform, "R3_TrafficCar_3", r3PathShifted2, redCar, 4.0f);
            evaluator.RegisterTraffic(r3Car3);

            // ==========================================
            // Roundabout 3 Pedestrian (Zebra Crossing at Y = 88.0)
            // ==========================================
            RoundaboutPedestrian r3Ped = SpawnPedestrian(trafficRoot.transform, "R3_CrossingPedestrian", new Vector2(-3.4f, 88.0f), new Vector2(3.4f, 88.0f), darkPed, 1.3f);
            evaluator.RegisterPedestrian(r3Ped);
        }

        private static List<Vector2> GenerateCircularPath(Vector2 center, float radius, int steps, bool clockwise)
        {
            List<Vector2> pts = new List<Vector2>();
            for (int i = 0; i < steps; i++)
            {
                // In clockwise circulation, angle decreases as t increases
                float t = (float)i / steps;
                float angle = (clockwise ? (1f - t) : t) * Mathf.PI * 2f;
                pts.Add(center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
            }
            return pts;
        }

        private static List<Vector2> ShiftPath(List<Vector2> path, int shift)
        {
            List<Vector2> shifted = new List<Vector2>();
            for (int i = 0; i < path.Count; i++)
            {
                shifted.Add(path[(i + shift) % path.Count]);
            }
            return shifted;
        }

        private static RoundaboutTrafficVehicle SpawnAIPlaneVehicle(Transform parent, string name, List<Vector2> path, Sprite sprite, float speed)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 9;
            obj.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.6f, 3.2f);
            col.isTrigger = false;

            RoundaboutTrafficVehicle vehicle = obj.AddComponent<RoundaboutTrafficVehicle>();
            vehicle.SetupPath(path, true, speed);

            return vehicle;
        }

        private static RoundaboutPedestrian SpawnPedestrian(Transform parent, string name, Vector2 a, Vector2 b, Sprite sprite, float speed)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = new Vector3(a.x, a.y, 0f);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 8;
            obj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
            col.radius = 0.65f;
            col.isTrigger = true;

            RoundaboutPedestrian ped = obj.AddComponent<RoundaboutPedestrian>();
            ped.Setup(a, b, speed);

            return ped;
        }
        #endregion

        #region UI Construction
        private static Level9UIController EnsureUI()
        {
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Level9_Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            Transform oldHUD = canvas.transform.Find("HUD");
            if (oldHUD != null)
            {
                UnityEngine.Object.DestroyImmediate(oldHUD.gameObject);
            }

            Level9UIController ui = canvas.GetComponent<Level9UIController>();
            if (ui != null) return ui;

            ui = canvas.gameObject.AddComponent<Level9UIController>();

            // 1. Mission Card (Top-Left: 380 x 125)
            GameObject mCard = CreateUIPanel(canvas.transform, "MissionCard", new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(380f, 125f), new Color(0.08f, 0.12f, 0.18f, 0.94f));
            TMP_Text mBadge = CreateUIText(mCard.transform, "Badge", "MISSION 1 / 5", 13f, TextAlignmentOptions.Left, new Vector2(240f, 20f), new Vector2(14f, -12f), new Vector2(0f, 1f));
            TMP_Text mTitle = CreateUIText(mCard.transform, "Title", "ROUNDABOUT BASICS", 16f, TextAlignmentOptions.Left, new Vector2(350f, 24f), new Vector2(14f, -34f), new Vector2(0f, 1f));
            TMP_Text mObj = CreateUIText(mCard.transform, "Objective", "Reduce speed, yield to circulating traffic, and take 1st exit East.", 12f, TextAlignmentOptions.Left, new Vector2(350f, 44f), new Vector2(14f, -60f), new Vector2(0f, 1f));
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
            TMP_Text statsVal = CreateUIText(sCard.transform, "StatsText", "Roundabouts: 0/3  |  Safe Yields: 0", 12f, TextAlignmentOptions.Left, new Vector2(310f, 20f), new Vector2(14f, -70f), new Vector2(0f, 1f));

            // 3. Roundabout Status Pill (Top-Center: 340 x 48)
            GameObject pill = CreateUIPanel(canvas.transform, "StatusPill", new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(340f, 48f), new Color(0.15f, 0.75f, 0.35f, 0.95f));
            Image pillBg = pill.GetComponent<Image>();
            TMP_Text pTitle = CreateUIText(pill.transform, "PillTitle", "[ROAD CLEAR]", 14.5f, TextAlignmentOptions.Center, new Vector2(320f, 20f), new Vector2(0f, -6f), new Vector2(0.5f, 1f));
            pTitle.fontStyle = FontStyles.Bold;
            TMP_Text pReason = CreateUIText(pill.transform, "PillReason", "Navigating Safely", 11.5f, TextAlignmentOptions.Center, new Vector2(320f, 18f), new Vector2(0f, -26f), new Vector2(0.5f, 1f));

            // 4. Controls Card (Bottom-Left: 270 x 65)
            GameObject ctrlCard = CreateUIPanel(canvas.transform, "ControlsCard", new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(270f, 65f), new Color(0.08f, 0.12f, 0.18f, 0.88f));
            CreateUIText(ctrlCard.transform, "CtrlTitle", "ROUNDABOUT CONTROLS", 11.5f, TextAlignmentOptions.Left, new Vector2(240f, 16f), new Vector2(14f, -8f), new Vector2(0f, 1f));
            CreateUIText(ctrlCard.transform, "CtrlBody", "W/S : Drive/Brake | A/D : Steer\nQ / E : Turn Signals | H : Hazard", 11.5f, TextAlignmentOptions.Left, new Vector2(240f, 34f), new Vector2(14f, -26f), new Vector2(0f, 1f));

            // 5. Feedback Banner (Bottom-Center: 580 x 44, positioned at Y = 78 above the indicator HUD)
            GameObject fBanner = CreateUIPanel(canvas.transform, "FeedbackBanner", new Vector2(0.5f, 0f), new Vector2(0f, 78f), new Vector2(580f, 44f), new Color(0.06f, 0.09f, 0.14f, 0.94f));
            CanvasGroup fGroup = fBanner.AddComponent<CanvasGroup>();
            TMP_Text fText = CreateUIText(fBanner.transform, "Text", "Slow down and give way to traffic on your right.", 13.5f, TextAlignmentOptions.Center, new Vector2(560f, 32f), Vector2.zero, new Vector2(0.5f, 0.5f));

            // 6. Completion Modal (Overlay)
            GameObject compModal = CreateUIPanel(canvas.transform, "CompletionModal", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 360f), new Color(0.06f, 0.09f, 0.14f, 0.98f));
            CanvasGroup compGroup = compModal.AddComponent<CanvasGroup>();
            TMP_Text cTitle = CreateUIText(compModal.transform, "Title", "LEVEL 9 COMPLETED!", 24f, TextAlignmentOptions.Center, new Vector2(480f, 36f), new Vector2(0f, -25f), new Vector2(0.5f, 1f));
            cTitle.fontStyle = FontStyles.Bold;
            TMP_Text cSub = CreateUIText(compModal.transform, "Sub", "Master of Roundabout Rules", 14f, TextAlignmentOptions.Center, new Vector2(480f, 22f), new Vector2(0f, -62f), new Vector2(0.5f, 1f));
            TMP_Text cStars = CreateUIText(compModal.transform, "Stars", "RATING: 3 / 3 STARS (PERFECT)", 17f, TextAlignmentOptions.Center, new Vector2(480f, 30f), new Vector2(0f, -90f), new Vector2(0.5f, 1f));
            cStars.color = new Color(1f, 0.82f, 0.15f, 1f);
            TMP_Text cScore = CreateUIText(compModal.transform, "Score", "FINAL SCORE: 155", 18f, TextAlignmentOptions.Center, new Vector2(480f, 26f), new Vector2(0f, -135f), new Vector2(0.5f, 1f));
            TMP_Text cStats = CreateUIText(compModal.transform, "Stats", "Safety: 100% | Roundabouts: 3/3 | Safe Yields: 3", 13.5f, TextAlignmentOptions.Center, new Vector2(480f, 40f), new Vector2(0f, -170f), new Vector2(0.5f, 1f));

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

        private static void EnsureIntroPanel(Level3PlayerCar playerCar)
        {
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            Transform existingIntro = canvas.transform.Find("Level9_IntroPanel");
            GameObject modal = existingIntro != null ? existingIntro.gameObject : null;
            Button gotItBtn = null;

            if (modal == null)
            {
                modal = CreateUIPanel(canvas.transform, "Level9_IntroPanel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(580f, 420f), new Color(0.06f, 0.09f, 0.14f, 0.98f));

                TMP_Text title = CreateUIText(modal.transform, "Title", "ROUNDABOUT RULES", 22f, TextAlignmentOptions.Center, new Vector2(520f, 32f), new Vector2(0f, -22f), new Vector2(0.5f, 1f));
                title.fontStyle = FontStyles.Bold;

                TMP_Text sub = CreateUIText(modal.transform, "Sub", "Slow Down. Give Way. Choose Your Exit.", 13.5f, TextAlignmentOptions.Center, new Vector2(520f, 22f), new Vector2(0f, -54f), new Vector2(0.5f, 1f));
                sub.color = new Color(0.4f, 0.75f, 1f, 1f);

                string body = "<b>HOW TO NAVIGATE ROUNDABOUTS SAFELY:</b>\n\n" +
                              "1. <b>REDUCE SPEED:</b> Slow down before entering (< 25 km/h).\n" +
                              "2. <b>GIVE WAY TO RIGHT:</b> Vehicles already circulating have priority. Wait for a safe opening.\n" +
                              "3. <b>CIRCULATE CLOCKWISE:</b> Follow traffic around the central island.\n" +
                              "4. <b>SIGNAL BEFORE EXITING:</b> Press <b>Q</b> or <b>E</b> to indicate before turning off.\n" +
                              "5. <b>YIELD TO PEDESTRIANS:</b> Stop at zebra crossings on entry and exit arms.";

                TMP_Text bodyText = CreateUIText(modal.transform, "Body", body, 13f, TextAlignmentOptions.Left, new Vector2(520f, 230f), new Vector2(30f, -85f), new Vector2(0f, 1f));

                gotItBtn = CreateUIButton(modal.transform, "GotItBtn", "LET'S DRIVE!", new Vector2(0f, 35f), new Vector2(170f, 44f), new Color(0.18f, 0.65f, 0.35f, 1f));

                LevelIntroController introCtrl = canvas.GetComponent<LevelIntroController>();
                if (introCtrl == null) introCtrl = canvas.gameObject.AddComponent<LevelIntroController>();
                SetField(introCtrl, "introPanel", modal);
                SetField(introCtrl, "gotItButton", gotItBtn);
                SetField(introCtrl, "titleText", title);
                SetField(introCtrl, "messageText", bodyText);
            }
            else
            {
                gotItBtn = modal.GetComponentInChildren<Button>(true);
            }

            modal.SetActive(true);

            if (gotItBtn != null)
            {
                Graphic[] childGraphics = gotItBtn.GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < childGraphics.Length; i++)
                {
                    if (childGraphics[i].gameObject != gotItBtn.gameObject)
                    {
                        childGraphics[i].raycastTarget = false;
                    }
                }

                gotItBtn.onClick.RemoveAllListeners();
                gotItBtn.onClick.AddListener(() =>
                {
                    modal.SetActive(false);
                    Time.timeScale = 1f;
                    if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
                    if (playerCar != null)
                    {
                        playerCar.SetControlEnabled(true);
                    }
                });
            }

            if (playerCar != null)
            {
                playerCar.SetControlEnabled(false);
            }
        }
        #endregion

        #region UI Component Helpers
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

        private static void CreateSignPost(Transform parent, string name, Vector3 pos, string text, Color badgeColor)
        {
            GameObject signObj = new GameObject(name);
            signObj.transform.SetParent(parent, false);
            signObj.transform.position = pos;

            Sprite square = LoadSpriteAsset(WorldSquareSpritePath);

            // Post
            CreateSprite(signObj.transform, "Post", new Vector3(0f, -0.4f, 0f), new Vector3(0.12f, 0.9f, 1f), new Color(0.4f, 0.45f, 0.5f, 1f), 11, square);

            // Badge
            GameObject badge = CreateSprite(signObj.transform, "Badge", new Vector3(0f, 0.4f, 0f), new Vector3(1.8f, 0.85f, 1f), badgeColor, 12, square);

            // Text
            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(badge.transform, false);
            txtObj.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            TextMeshPro tmp = txtObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 2.2f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            if (txtObj.TryGetComponent<MeshRenderer>(out var mr)) mr.sortingOrder = 14;
            tmp.rectTransform.sizeDelta = new Vector2(1.8f, 0.85f);
        }
        #endregion

        #region General Helpers
        public static GameObject CreateSprite(Transform parent, string name, Vector3 localPos, Vector3 localScale, Color color, int sortingOrder, Sprite sprite)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;

            return obj;
        }

        public static Sprite LoadSpriteAsset(string path)
        {
#if UNITY_EDITOR
            Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;
#endif
            return Resources.Load<Sprite>(path);
        }

        public static GameObject FindOrCreate(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go == null) go = new GameObject(name);
            return go;
        }

        public static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
        #endregion
    }

    /// <summary>
    /// Simple trigger component for mission progression checkpoints.
    /// </summary>
    public class RoundaboutCheckpointTrigger : MonoBehaviour
    {
        public string checkpointId;
        public event Action<string> CheckpointReached;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<Level3PlayerCar>() != null)
            {
                if (CheckpointReached != null)
                {
                    CheckpointReached.Invoke(checkpointId);
                }
                else
                {
                    RoundaboutMissionController missionCtrl = UnityEngine.Object.FindAnyObjectByType<RoundaboutMissionController>();
                    if (missionCtrl != null)
                    {
                        missionCtrl.OnCheckpointReached(checkpointId);
                    }
                }
            }
        }
    }
}
