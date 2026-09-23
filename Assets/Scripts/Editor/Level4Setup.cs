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
using TrafficTown2D.Level4;
using TrafficTown2D.UI;

namespace TrafficTown2D.Editor
{
    public static class Level4Setup
    {
        private const string Level4ScenePath = "Assets/Scenes/Level4.unity";
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
        private const string CrossingGuardTopDownPath = "Assets/Sprites/Characters/CrossingGuardTopDown.png";
        private const string AdultPedestrianDarkTopDownPath = "Assets/Sprites/Characters/AdultPedestrianDarkTopDown.png";

        private const string HeadlightBeamConePath = "Assets/Sprites/Environment/HeadlightBeamCone.png";
        private const string StreetLampGlowPath = "Assets/Sprites/Environment/StreetLampGlow.png";
        private const string RainDropPath = "Assets/Sprites/Environment/RainDrop.png";

        private const string SignsFolderPath = "Assets/Resources/Signs";

        // Night Theme Palette
        private static readonly Color NightGrassColor = new Color(0.08f, 0.11f, 0.16f, 1f);
        private static readonly Color NightRoadColor = new Color(0.12f, 0.13f, 0.17f, 1f);
        private static readonly Color NightSidewalkColor = new Color(0.18f, 0.20f, 0.26f, 1f);
        private static readonly Color NightCurbColor = new Color(0.26f, 0.30f, 0.38f, 1f);
        private static readonly Color NightMarkingColor = new Color(0.75f, 0.65f, 0.25f, 0.9f);
        private static readonly Color NightAmbientLightColor = new Color(0.15f, 0.18f, 0.32f, 1f);
        private static readonly Color StreetlampGlowColor = new Color(1f, 0.88f, 0.60f, 0.75f);

        private const string AutoRunPrefKey = "TrafficTown_Level4_AutoSetup_v2";

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
                        Debug.Log("[Level4Setup] Auto-running SetupLevel4 on compile...");
                        SetupLevel4();
                    }
                };
            }
        }

        [MenuItem("TrafficTown/Setup Level 4")]
        public static void SetupLevel4()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before running TrafficTown -> Setup Level 4.");
                return;
            }

            EnsureAssetFolders();
            ConfigureSpriteImporters();

            Scene level4Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(level4Scene, Level4ScenePath);

            // 1. Camera & Night Lighting
            Camera cam = EnsureCamera();
            Light2D globalLight = EnsureGlobalNightLight();

            // 2. Services & EventSystem
            GameObject services = FindOrCreate("Services");
            SceneLoader sceneLoader = GetOrAdd<SceneLoader>(services);
            GameManager gm = GetOrAdd<GameManager>(services);
            SetInt(gm, "startingState", (int)GameState.Playing);
            gm.SetState(GameState.Playing);
            EditorUtility.SetDirty(gm);
            Time.timeScale = 1f;
            EnsureEventSystem();

            // 3. World (Route: Y = -15 to Y = 200)
            GameObject world = FindOrCreate("World");
            ClearChildren(world.transform);
            List<Transform> streetlamps = new List<Transform>();
            CreateWorldEnvironment(world.transform, streetlamps);

            // 4. Player Car & Headlights
            GameObject playerCarGO = CreatePlayerCar();
            Level3PlayerCar playerCar = playerCarGO.GetComponent<Level3PlayerCar>();
            PlayerHeadlightController playerHeadlights = playerCarGO.GetComponent<PlayerHeadlightController>();

            // Setup Camera Follow
            TopDownCameraFollow camFollow = cam.GetComponent<TopDownCameraFollow>();
            if (camFollow == null) camFollow = cam.gameObject.AddComponent<TopDownCameraFollow>();
            camFollow.SetTarget(playerCarGO.transform);
            camFollow.SetBounds(-6f, 210f);
            SetReference(camFollow, "target", playerCarGO.transform);
            SetFloat(camFollow, "maxY", 210f);
            SetFloat(camFollow, "minY", -6f);

            // 5. Visibility Controller
            GameObject visCtrlGO = FindOrCreateChild(world.transform, "NightVisibilityController");
            NightVisibilityController visController = GetOrAdd<NightVisibilityController>(visCtrlGO);
            SetReference(visController, "headlightController", playerHeadlights);
            SetReference(visController, "playerTransform", playerCarGO.transform);
            visController.SetStreetlamps(streetlamps.ToArray());

            // 6. Rain Controller
            GameObject rainGO = FindOrCreateChild(cam.transform, "RainController");
            RainController rainController = CreateRainSystem(rainGO, visController, playerCar);

            // 7. Mission 1: Reflective Signs & Start Zone
            CreateMission1Elements(world.transform, playerHeadlights);

            // 8. Mission 2: Pedestrian Crossing (Dark-clothed & Guard)
            Level3Pedestrian darkPed, guardPed;
            CreateMission2Elements(world.transform, playerCarGO.transform, playerHeadlights, out darkPed, out guardPed);

            // 9. Mission 3: Headlight Discipline (Oncoming Traffic)
            OncomingTrafficVehicle oncomingCar = CreateMission3Elements(world.transform, playerCarGO.transform, playerHeadlights);

            // 10. Mission 4: City Avenue, Traffic Light & Ambulance
            Level3TrafficLight intersectionLight;
            GameObject[] crossTraffic;
            Level3TopDownVehicle ambulance = CreateMission4Elements(world.transform, out intersectionLight, out crossTraffic);

            // 11. Mission 5: Finish Line
            CreateMission5FinishElements(world.transform, playerHeadlights);

            // 12. UI Canvas
            Level4UIController ui = CreateUI(playerCar, playerHeadlights, visController);

            // 13. Mission Manager
            GameObject missionManagerGO = FindOrCreate("NightMissionManager");
            NightMissionManager missionMgr = GetOrAdd<NightMissionManager>(missionManagerGO);
            SetReference(missionMgr, "playerCar", playerCar);
            SetReference(missionMgr, "playerHeadlights", playerHeadlights);
            SetReference(missionMgr, "visibilityController", visController);
            SetReference(missionMgr, "rainController", rainController);
            SetReference(missionMgr, "cameraFollow", camFollow);
            SetReference(missionMgr, "uiController", ui);
            SetReference(missionMgr, "darkPedestrian", darkPed);
            SetReference(missionMgr, "guardPedestrian", guardPed);
            SetReference(missionMgr, "oncomingCar", oncomingCar);
            SetReference(missionMgr, "intersectionLight", intersectionLight);
            SetReference(missionMgr, "ambulanceVehicle", ambulance);
            SetObjectArray(missionMgr, "crossTrafficVehicles", crossTraffic);

            // 14. Build Settings Registration
            BuildSettingsUtility.EnsureAllScenesInBuildSettings();

            EditorSceneManager.MarkSceneDirty(level4Scene);
            EditorSceneManager.SaveScene(level4Scene);
            Selection.activeGameObject = playerCarGO;
            Debug.Log("[Level4Setup] TrafficTown Level 4: 'NIGHT DRIVING 🌙' created successfully!");
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
            cam.backgroundColor = NightGrassColor;
            cam.clearFlags = CameraClearFlags.SolidColor;

            UniversalAdditionalCameraData urpCam = cam.GetComponent<UniversalAdditionalCameraData>();
            if (urpCam == null) cam.gameObject.AddComponent<UniversalAdditionalCameraData>();

            return cam;
        }

        private static Light2D EnsureGlobalNightLight()
        {
            Light2D light = Object.FindAnyObjectByType<Light2D>();
            if (light == null)
            {
                GameObject lightObj = new GameObject("Global Night Light 2D");
                light = lightObj.AddComponent<Light2D>();
            }
            light.lightType = Light2D.LightType.Global;
            light.intensity = 0.26f; // Deep nighttime ambient
            light.color = NightAmbientLightColor;
            return light;
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
        private static void CreateWorldEnvironment(Transform world, List<Transform> streetlamps)
        {
            // 1. Large Ground
            CreateWorldSprite(world, "NightGround", new Vector3(0f, 95f, 5f), new Vector3(65f, 250f, 1f), NightGrassColor, -20);

            // 2. Main Vertical Road Corridor (Width = 6.6, Length = 240)
            CreateWorldSprite(world, "MainRoad", new Vector3(0f, 95f, 0f), new Vector3(6.6f, 240f, 1f), NightRoadColor, -10);

            // 3. Sidewalks
            CreateWorldSprite(world, "LeftSidewalk", new Vector3(-4.5f, 95f, 0f), new Vector3(2.4f, 240f, 1f), NightSidewalkColor, -12);
            CreateWorldSprite(world, "RightSidewalk", new Vector3(4.5f, 95f, 0f), new Vector3(2.4f, 240f, 1f), NightSidewalkColor, -12);

            // 4. Curbs
            CreateWorldSprite(world, "LeftCurb", new Vector3(-3.3f, 95f, 0f), new Vector3(0.18f, 240f, 1f), NightCurbColor, -9);
            CreateWorldSprite(world, "RightCurb", new Vector3(3.3f, 95f, 0f), new Vector3(0.18f, 240f, 1f), NightCurbColor, -9);

            // 5. Dashed Center Line (Yellow)
            GameObject centerLines = FindOrCreateChild(world, "CenterDashedLines");
            for (float y = -10f; y <= 195f; y += 4f)
            {
                // Skip intersections
                if (y >= 122f && y <= 130f) continue;
                CreateWorldSprite(centerLines.transform, $"Dash_{y}", new Vector3(0f, y, 0f), new Vector3(0.18f, 2.2f, 1f), NightMarkingColor, -8);
            }

            // 6. Road Shoulder Slowdown Triggers
            CreateRoadShoulderTrigger(world, "LeftShoulderTrigger", new Vector3(-3.8f, 95f, 0f), new Vector2(1.2f, 240f));
            CreateRoadShoulderTrigger(world, "RightShoulderTrigger", new Vector3(3.8f, 95f, 0f), new Vector2(1.2f, 240f));

            // 7. Streetlamps & Roadside Scenery
            CreateNightRoadsideScenery(world, streetlamps);
        }

        private static void CreateRoadShoulderTrigger(Transform parent, string name, Vector3 pos, Vector2 size)
        {
            GameObject triggerObj = FindOrCreateChild(parent, name);
            triggerObj.transform.position = pos;
            BoxCollider2D box = GetOrAdd<BoxCollider2D>(triggerObj);
            box.isTrigger = true;
            box.size = size;
            GetOrAdd<RoadShoulderTrigger>(triggerObj);
        }

        private static void CreateNightRoadsideScenery(Transform parent, List<Transform> streetlamps)
        {
            GameObject scenery = FindOrCreateChild(parent, "Scenery");

            // Streetlamps along sidewalks:
            // Notice: Mission 3 (Y = 70 to 108) is intentionally unlit/dark to enforce high beam usage!
            float[] lampYCoords = { -4f, 12f, 28f, 44f, 60f, 112f, 122f, 134f, 146f, 160f, 174f, 186f };
            for (int i = 0; i < lampYCoords.Length; i++)
            {
                float y = lampYCoords[i];
                bool leftSide = (i % 2 == 0);
                float x = leftSide ? -4.2f : 4.2f;

                GameObject lamp = CreateStreetlamp(scenery.transform, new Vector3(x, y, 0f), leftSide);
                streetlamps.Add(lamp.transform);
            }

            // Roadside Trees and Night Buildings
            for (float y = -8f; y <= 190f; y += 14f)
            {
                if ((y >= 50f && y <= 62f) || (y >= 120f && y <= 132f) || (y >= 180f && y <= 192f))
                    continue; // Leave room for crossings and intersections

                CreateNightTree(scenery.transform, new Vector3(-7f, y, 0f));
                CreateNightTree(scenery.transform, new Vector3(7f, y + 7f, 0f));

                if (y % 28 == 0)
                {
                    CreateNightBuilding(scenery.transform, new Vector3(-10f, y + 2f, 0f), true);
                    CreateNightBuilding(scenery.transform, new Vector3(10f, y + 6f, 0f), false);
                }
            }
        }

        private static GameObject CreateStreetlamp(Transform parent, Vector3 pos, bool leftSide)
        {
            GameObject lamp = FindOrCreateChild(parent, $"Streetlamp_{pos.y}");
            lamp.transform.position = pos;

            // Pole
            CreateWorldSprite(lamp.transform, "Pole", Vector3.zero, new Vector3(0.18f, 1.8f, 1f), new Color(0.35f, 0.40f, 0.48f, 1f), -2);
            // Arm & Lamp Head
            float armOffsetX = leftSide ? 0.4f : -0.4f;
            CreateWorldSprite(lamp.transform, "Head", new Vector3(armOffsetX, 0.9f, 0f), new Vector3(0.65f, 0.25f, 1f), new Color(0.5f, 0.55f, 0.65f, 1f), -1);

            // Amber Light Glow Sprite
            Sprite glowSprite = LoadSpriteAsset(StreetLampGlowPath);
            if (glowSprite != null)
            {
                GameObject glowObj = FindOrCreateChild(lamp.transform, "GlowSprite");
                glowObj.transform.localPosition = new Vector3(armOffsetX, 0.8f, 0f);
                glowObj.transform.localScale = new Vector3(5.5f, 5.5f, 1f);
                SpriteRenderer sr = GetOrAdd<SpriteRenderer>(glowObj);
                sr.sprite = glowSprite;
                sr.color = StreetlampGlowColor;
                sr.sortingOrder = 2;
            }

            // URP 2D Point Light
            GameObject lightObj = FindOrCreateChild(lamp.transform, "LampLight2D");
            lightObj.transform.localPosition = new Vector3(armOffsetX, 0.8f, 0f);
            Light2D light = GetOrAdd<Light2D>(lightObj);
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = 7.5f;
            light.intensity = 0.9f;
            light.color = new Color(1f, 0.92f, 0.70f, 1f);

            return lamp;
        }

        private static void CreateNightTree(Transform parent, Vector3 pos)
        {
            GameObject tree = FindOrCreateChild(parent, $"NightTree_{pos.y}");
            tree.transform.position = pos;
            CreateWorldSprite(tree.transform, "Trunk", new Vector3(0f, -0.4f, 0f), new Vector3(0.35f, 0.8f, 1f), new Color(0.20f, 0.16f, 0.14f, 1f), -4);
            CreateWorldSprite(tree.transform, "Foliage", new Vector3(0f, 0.3f, -0.01f), new Vector3(1.7f, 1.7f, 1f), new Color(0.10f, 0.22f, 0.16f, 1f), -3, true);
        }

        private static void CreateNightBuilding(Transform parent, Vector3 pos, bool warmWindows)
        {
            GameObject bld = FindOrCreateChild(parent, $"NightBuilding_{pos.y}");
            bld.transform.position = pos;
            CreateWorldSprite(bld.transform, "Walls", Vector3.zero, new Vector3(3.8f, 3.2f, 1f), new Color(0.14f, 0.16f, 0.22f, 1f), -5);

            // Windows: some lit warm yellow, some dark
            Color litWindow = new Color(1f, 0.88f, 0.45f, 0.9f);
            Color darkWindow = new Color(0.18f, 0.22f, 0.32f, 0.9f);

            CreateWorldSprite(bld.transform, "Win1", new Vector3(-0.9f, 0.6f, -0.02f), new Vector3(0.55f, 0.55f, 1f), warmWindows ? litWindow : darkWindow, -4);
            CreateWorldSprite(bld.transform, "Win2", new Vector3(0.9f, 0.6f, -0.02f), new Vector3(0.55f, 0.55f, 1f), darkWindow, -4);
            CreateWorldSprite(bld.transform, "Win3", new Vector3(-0.9f, -0.5f, -0.02f), new Vector3(0.55f, 0.55f, 1f), darkWindow, -4);
            CreateWorldSprite(bld.transform, "Win4", new Vector3(0.9f, -0.5f, -0.02f), new Vector3(0.55f, 0.55f, 1f), litWindow, -4);
        }
        #endregion

        #region Player Car & Headlights
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

            // Brake lights
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WorldCircleSpritePath);
            SpriteRenderer blLeft = CreateLightRenderer(car.transform, "BrakeLight_L", new Vector3(-0.65f, -1.6f, -0.02f), circleSprite);
            SpriteRenderer blRight = CreateLightRenderer(car.transform, "BrakeLight_R", new Vector3(0.65f, -1.6f, -0.02f), circleSprite);
            SetObjectArray(playerCar, "brakeLights", new[] { blLeft, blRight });

            // Front Headlight Bulbs
            SpriteRenderer hlLeft = CreateLightRenderer(car.transform, "Bulb_L", new Vector3(-0.65f, 1.6f, -0.02f), circleSprite);
            SpriteRenderer hlRight = CreateLightRenderer(car.transform, "Bulb_R", new Vector3(0.65f, 1.6f, -0.02f), circleSprite);
            SetObjectArray(playerCar, "headlights", new[] { hlLeft, hlRight });

            // Volumetric Beam Cones
            Sprite beamSprite = LoadSpriteAsset(HeadlightBeamConePath);
            SpriteRenderer coneL = CreateBeamCone(car.transform, "BeamCone_L", new Vector3(-0.65f, 1.8f, -0.01f), beamSprite);
            SpriteRenderer coneR = CreateBeamCone(car.transform, "BeamCone_R", new Vector3(0.65f, 1.8f, -0.01f), beamSprite);

            // Headlight URP 2D Point/Spot Lights
            Light2D spotL = CreateSpotLight(car.transform, "SpotLight_L", new Vector3(-0.65f, 1.8f, 0f));
            Light2D spotR = CreateSpotLight(car.transform, "SpotLight_R", new Vector3(0.65f, 1.8f, 0f));
            Light2D forwardLight = CreateSpotLight(car.transform, "ForwardBeamLight", new Vector3(0f, 2.2f, 0f));

            // Headlight Controller
            PlayerHeadlightController hlCtrl = GetOrAdd<PlayerHeadlightController>(car);
            SetReference(hlCtrl, "leftSpotLight", spotL);
            SetReference(hlCtrl, "rightSpotLight", spotR);
            SetReference(hlCtrl, "forwardBeamLight", forwardLight);
            SetReference(hlCtrl, "leftBeamCone", coneL);
            SetReference(hlCtrl, "rightBeamCone", coneR);
            SetObjectArray(hlCtrl, "bulbRenderers", new[] { hlLeft, hlRight });

            return car;
        }

        private static SpriteRenderer CreateLightRenderer(Transform parent, string name, Vector3 localPos, Sprite sprite)
        {
            GameObject obj = FindOrCreateChild(parent, name);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(obj);
            sr.sprite = sprite;
            sr.sortingOrder = 11;
            return sr;
        }

        private static SpriteRenderer CreateBeamCone(Transform parent, string name, Vector3 localPos, Sprite sprite)
        {
            GameObject cone = FindOrCreateChild(parent, name);
            cone.transform.localPosition = localPos;
            cone.transform.localScale = new Vector3(1.6f, 10f, 1f);
            cone.transform.localRotation = Quaternion.identity;
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(cone);
            sr.sprite = sprite;
            sr.sortingOrder = 9;
            Color c = new Color(1f, 0.96f, 0.82f, 0.35f);
            sr.color = c;
            return sr;
        }

        private static Light2D CreateSpotLight(Transform parent, string name, Vector3 localPos)
        {
            GameObject spotObj = FindOrCreateChild(parent, name);
            spotObj.transform.localPosition = localPos;
            spotObj.transform.localRotation = Quaternion.identity;
            Light2D light = GetOrAdd<Light2D>(spotObj);
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = 12f;
            light.pointLightOuterAngle = 55f;
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.96f, 0.82f, 1f);
            return light;
        }
        #endregion

        #region Rain System
        private static RainController CreateRainSystem(GameObject rainObj, NightVisibilityController vis, Level3PlayerCar car)
        {
            ParticleSystem ps = GetOrAdd<ParticleSystem>(rainObj);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 1.2f;
            main.startSpeed = 16f;
            main.startSize = 0.45f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 350;
            // Bluish-grey color gradient for rain
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.65f, 0.76f, 0.88f, 0.60f), // Soft bluish-grey
                new Color(0.78f, 0.86f, 0.95f, 0.80f)  // Light watery blue-grey
            );

            var emission = ps.emission;
            emission.rateOverTime = 160f;
            emission.enabled = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 2f, 1f);
            shape.position = new Vector3(0f, 8f, 0f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-2f);
            vel.y = new ParticleSystem.MinMaxCurve(-15f);

            // Create / load dedicated rain material with URP 2D unlit sprite shader
            Material rainMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RainMaterial.mat");
            if (rainMat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                rainMat = new Material(shader);
                rainMat.name = "RainMaterial";
                rainMat.color = new Color(0.72f, 0.82f, 0.94f, 0.75f); // Bluish-grey tint
                Sprite rainSprite = LoadSpriteAsset(RainDropPath);
                if (rainSprite != null)
                {
                    rainMat.mainTexture = rainSprite.texture;
                }
                if (!Directory.Exists("Assets/Materials")) Directory.CreateDirectory("Assets/Materials");
                AssetDatabase.CreateAsset(rainMat, "Assets/Materials/RainMaterial.mat");
            }

            var psRenderer = rainObj.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null)
            {
                psRenderer.sharedMaterial = rainMat;
                psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                psRenderer.velocityScale = 0.06f;
                psRenderer.lengthScale = 1.6f;
                psRenderer.sortingOrder = 15;
            }

            RainController rainCtrl = GetOrAdd<RainController>(rainObj);
            SetReference(rainCtrl, "rainParticleSystem", ps);
            SetReference(rainCtrl, "visibilityController", vis);
            SetReference(rainCtrl, "playerCar", car);

            return rainCtrl;
        }
        #endregion

        #region Mission 1 Elements
        private static void CreateMission1Elements(Transform world, PlayerHeadlightController headlights)
        {
            GameObject m1 = FindOrCreateChild(world, "Mission1_Elements");

            // Reflective Speed Limit 40 sign at Y = 10
            GameObject sign40 = CreateReflectiveSign(m1.transform, "Sign_Speed40", "SpeedLimit40", new Vector3(3.8f, 10f, 0f), headlights);

            // Reflective traffic cones marking lane boundaries
            for (float y = 2f; y <= 24f; y += 6f)
            {
                CreateReflectiveCone(m1.transform, $"Cone_L_{y}", new Vector3(-3.2f, y, 0f), headlights);
                CreateReflectiveCone(m1.transform, $"Cone_R_{y}", new Vector3(3.2f, y, 0f), headlights);
            }
        }

        private static GameObject CreateReflectiveSign(Transform parent, string name, string signSpriteName, Vector3 pos, PlayerHeadlightController hl)
        {
            GameObject sign = FindOrCreateChild(parent, name);
            sign.transform.position = pos;

            // Post
            Sprite postSprite = LoadSpriteAsset(SignPostBasePath);
            GameObject post = CreateWorldSprite(sign.transform, "Post", new Vector3(0f, -0.4f, 0f), new Vector3(0.14f, 1.0f, 1f), new Color(0.4f, 0.45f, 0.5f, 1f), 0);

            // Head / Plate
            Sprite signSprite = LoadSignSprite(signSpriteName);
            GameObject plate = FindOrCreateChild(sign.transform, "Plate");
            plate.transform.localPosition = new Vector3(0f, 0.35f, -0.02f);
            plate.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(plate);
            sr.sprite = signSprite;
            sr.sortingOrder = 2;

            ReflectiveElement refl = GetOrAdd<ReflectiveElement>(plate);
            SetReference(refl, "playerHeadlights", hl);

            return sign;
        }

        private static GameObject CreateReflectiveCone(Transform parent, string name, Vector3 pos, PlayerHeadlightController hl)
        {
            GameObject cone = FindOrCreateChild(parent, name);
            cone.transform.position = pos;
            cone.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WorldCircleSpritePath);
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(cone);
            sr.sprite = circleSprite;
            sr.color = new Color(0.95f, 0.45f, 0.1f, 1f);
            sr.sortingOrder = 1;

            ReflectiveElement refl = GetOrAdd<ReflectiveElement>(cone);
            SetReference(refl, "playerHeadlights", hl);

            return cone;
        }
        #endregion

        #region Mission 2 Elements (Pedestrians at Night)
        private static void CreateMission2Elements(Transform world, Transform playerTransform, PlayerHeadlightController hl, out Level3Pedestrian darkPed, out Level3Pedestrian guardPed)
        {
            GameObject m2 = FindOrCreateChild(world, "Mission2_Elements");

            // Zebra crossing stripes at Y = 56
            float crossingY = 56f;
            Sprite zebraSprite = LoadSpriteAsset(ZebraCrossingPath);
            GameObject zebra = FindOrCreateChild(m2.transform, "ZebraCrossing");
            zebra.transform.position = new Vector3(0f, crossingY, 0f);
            zebra.transform.localScale = new Vector3(6.4f, 3.2f, 1f);
            SpriteRenderer zsr = GetOrAdd<SpriteRenderer>(zebra);
            zsr.sprite = zebraSprite;
            zsr.color = new Color(0.92f, 0.92f, 0.95f, 0.9f);
            zsr.sortingOrder = -7;

            // Reflective studs on crossing edges
            for (float x = -2.8f; x <= 2.8f; x += 0.8f)
            {
                CreateReflectiveCone(m2.transform, $"Stud_S_{x}", new Vector3(x, crossingY - 1.8f, 0f), hl);
                CreateReflectiveCone(m2.transform, $"Stud_N_{x}", new Vector3(x, crossingY + 1.8f, 0f), hl);
            }

            // Pedestrian Crossing Sign at Y = 46
            CreateReflectiveSign(m2.transform, "Sign_PedCrossing", "PedestrianCrossing", new Vector3(3.8f, 46f, 0f), hl);

            // 1. Dark-clothed Pedestrian (Low contrast hazard!)
            GameObject darkObj = FindOrCreateChild(m2.transform, "DarkClothedPedestrian");
            darkObj.transform.position = new Vector3(-4.0f, crossingY - 0.4f, 0f);
            Sprite darkSprite = LoadSpriteAsset(AdultPedestrianDarkTopDownPath);
            if (darkSprite == null) darkSprite = LoadSpriteAsset(ChildBoyTopDownPath);
            SpriteRenderer dsr = GetOrAdd<SpriteRenderer>(darkObj);
            dsr.sprite = darkSprite;
            dsr.sortingOrder = 5;
            darkObj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            darkPed = GetOrAdd<Level3Pedestrian>(darkObj);
            SetVector3(darkPed, "startPosition", new Vector3(-4.0f, crossingY - 0.4f, 0f));
            SetVector3(darkPed, "targetPosition", new Vector3(4.2f, crossingY - 0.4f, 0f));
            SetFloat(darkPed, "walkSpeed", 1.4f);
            SetFloat(darkPed, "triggerDistance", 14f);
            SetReference(darkPed, "playerTransform", playerTransform);

            // 2. Crossing Guard (With high-visibility reflective vest)
            GameObject guardObj = FindOrCreateChild(m2.transform, "CrossingGuard");
            guardObj.transform.position = new Vector3(-4.6f, crossingY + 0.4f, 0f);
            Sprite guardSprite = LoadSpriteAsset(CrossingGuardTopDownPath);
            SpriteRenderer gsr = GetOrAdd<SpriteRenderer>(guardObj);
            gsr.sprite = guardSprite;
            gsr.sortingOrder = 5;
            guardObj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            ReflectiveElement gRefl = GetOrAdd<ReflectiveElement>(guardObj);
            SetReference(gRefl, "playerHeadlights", hl);

            guardPed = GetOrAdd<Level3Pedestrian>(guardObj);
            SetVector3(guardPed, "startPosition", new Vector3(-4.6f, crossingY + 0.4f, 0f));
            SetVector3(guardPed, "targetPosition", new Vector3(3.8f, crossingY + 0.4f, 0f));
            SetFloat(guardPed, "walkSpeed", 1.4f);
            SetFloat(guardPed, "triggerDistance", 14f);
            SetReference(guardPed, "playerTransform", playerTransform);
        }
        #endregion

        #region Mission 3 Elements (Oncoming Vehicle & Dazzle)
        private static OncomingTrafficVehicle CreateMission3Elements(Transform world, Transform playerTransform, PlayerHeadlightController hl)
        {
            GameObject m3 = FindOrCreateChild(world, "Mission3_Elements");

            // Road signs warning of unlit stretch / curve
            CreateReflectiveSign(m3.transform, "Sign_CurveRight", "SharpCurveRight", new Vector3(3.8f, 72f, 0f), hl);

            // Oncoming AI Car (in Left/Southbound lane, X = -1.2f, Y = 115f)
            GameObject carObj = FindOrCreateChild(m3.transform, "OncomingCar");
            carObj.transform.position = new Vector3(-1.2f, 114f, 0f);
            carObj.transform.rotation = Quaternion.Euler(0f, 0f, 180f); // Facing DOWN / South
            carObj.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            Sprite redCarSprite = LoadSpriteAsset(CarRedTopDownPath);
            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(carObj);
            sr.sprite = redCarSprite;
            sr.sortingOrder = 10;

            Rigidbody2D rb = GetOrAdd<Rigidbody2D>(carObj);
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            BoxCollider2D col = GetOrAdd<BoxCollider2D>(carObj);
            col.size = new Vector2(1.6f, 3.2f);

            // Headlight visuals for oncoming car
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WorldCircleSpritePath);
            SpriteRenderer hlL = CreateLightRenderer(carObj.transform, "HL_L", new Vector3(-0.65f, 1.6f, -0.02f), circleSprite);
            SpriteRenderer hlR = CreateLightRenderer(carObj.transform, "HL_R", new Vector3(0.65f, 1.6f, -0.02f), circleSprite);
            hlL.color = new Color(1f, 1f, 0.85f, 1f);
            hlR.color = new Color(1f, 1f, 0.85f, 1f);

            Sprite beamSprite = LoadSpriteAsset(HeadlightBeamConePath);
            SpriteRenderer oncomingCone = CreateBeamCone(carObj.transform, "OncomingBeam", new Vector3(0f, 1.8f, -0.01f), beamSprite);

            Light2D oncomingLight = CreateSpotLight(carObj.transform, "OncomingSpot", new Vector3(0f, 1.8f, 0f));

            OncomingTrafficVehicle oncoming = GetOrAdd<OncomingTrafficVehicle>(carObj);
            SetReference(oncoming, "playerTransform", playerTransform);
            SetReference(oncoming, "playerHeadlights", hl);
            SetReference(oncoming, "headlightLight", oncomingLight);
            SetReference(oncoming, "beamCone", oncomingCone);
            SetObjectArray(oncoming, "headlights", new[] { hlL, hlR });

            return oncoming;
        }
        #endregion

        #region Mission 4 Elements (City Avenue, Intersection & Ambulance)
        private static Level3TopDownVehicle CreateMission4Elements(Transform world, out Level3TrafficLight intersectionLight, out GameObject[] crossTraffic)
        {
            GameObject m4 = FindOrCreateChild(world, "Mission4_Elements");

            // Horizontal Cross Road at Y = 126
            float crossY = 126f;
            CreateWorldSprite(m4.transform, "CrossRoad", new Vector3(0f, crossY, 0f), new Vector3(32f, 6.2f, 1f), NightRoadColor, -11);

            // School Zone Sign (20 km/h) at Y = 112
            CreateReflectiveSign(m4.transform, "Sign_SchoolZone", "SchoolZone", new Vector3(3.8f, 112f, 0f), null);
            CreateReflectiveSign(m4.transform, "Sign_Speed20", "SpeedLimit20", new Vector3(3.8f, 116f, 0f), null);

            // Traffic Light at Right Curb (Y = 123.5)
            GameObject tlObj = FindOrCreateChild(m4.transform, "IntersectionTrafficLight");
            tlObj.transform.position = new Vector3(3.8f, 123.5f, 0f);
            tlObj.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            Sprite bodySprite = LoadSpriteAsset("Assets/Sprites/TrafficLightBody.png");
            Sprite lensSprite = LoadSpriteAsset("Assets/Sprites/TrafficLightLens.png");

            SpriteRenderer bsr = GetOrAdd<SpriteRenderer>(tlObj);
            bsr.sprite = bodySprite;
            bsr.sortingOrder = 5;

            SpriteRenderer rLens = CreateLightRenderer(tlObj.transform, "RedLens", new Vector3(0f, 0.5f, -0.02f), lensSprite);
            SpriteRenderer yLens = CreateLightRenderer(tlObj.transform, "YellowLens", new Vector3(0f, 0.0f, -0.02f), lensSprite);
            SpriteRenderer gLens = CreateLightRenderer(tlObj.transform, "GreenLens", new Vector3(0f, -0.5f, -0.02f), lensSprite);

            intersectionLight = GetOrAdd<Level3TrafficLight>(tlObj);
            SetReference(intersectionLight, "redLens", rLens);
            SetReference(intersectionLight, "yellowLens", yLens);
            SetReference(intersectionLight, "greenLens", gLens);

            // Cross-Traffic Vehicles
            GameObject busObj = CreateCrossVehicle(m4.transform, "CrossBus", BusTopDownPath, new Vector3(-16f, crossY + 1.4f, 0f), Vector2.right, VehicleCategory.Bus, 4.5f);
            GameObject taxiObj = CreateCrossVehicle(m4.transform, "CrossTaxi", TaxiTopDownPath, new Vector3(16f, crossY - 1.4f, 0f), Vector2.left, VehicleCategory.Taxi, 5.0f);
            crossTraffic = new[] { busObj, taxiObj };

            // Emergency Ambulance (spawns behind player at Y = 135)
            GameObject ambObj = FindOrCreateChild(m4.transform, "AmbulanceVehicle");
            ambObj.transform.position = new Vector3(1.1f, 120f, 0f);
            ambObj.transform.rotation = Quaternion.identity;
            ambObj.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            Sprite ambSprite = LoadSpriteAsset(AmbulanceTopDownPath);
            SpriteRenderer asr = GetOrAdd<SpriteRenderer>(ambObj);
            asr.sprite = ambSprite;
            asr.sortingOrder = 10;

            Rigidbody2D arb = GetOrAdd<Rigidbody2D>(ambObj);
            arb.gravityScale = 0f;
            arb.freezeRotation = true;

            BoxCollider2D acol = GetOrAdd<BoxCollider2D>(ambObj);
            acol.size = new Vector2(1.6f, 3.2f);

            // Emergency Beacons (Red and Blue flashing dots)
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WorldCircleSpritePath);
            SpriteRenderer bBlue = CreateLightRenderer(ambObj.transform, "BeaconBlue", new Vector3(-0.4f, 0.4f, -0.05f), circleSprite);
            SpriteRenderer bRed = CreateLightRenderer(ambObj.transform, "BeaconRed", new Vector3(0.4f, 0.4f, -0.05f), circleSprite);
            bBlue.color = Color.cyan;
            bRed.color = Color.red;

            Level3TopDownVehicle ambVehicle = GetOrAdd<Level3TopDownVehicle>(ambObj);
            SetInt(ambVehicle, "category", (int)VehicleCategory.Ambulance);
            SetFloat(ambVehicle, "speed", 7.0f);
            SetReference(ambVehicle, "beaconBlue", bBlue);
            SetReference(ambVehicle, "beaconRed", bRed);

            return ambVehicle;
        }

        private static GameObject CreateCrossVehicle(Transform parent, string name, string spritePath, Vector3 pos, Vector2 dir, VehicleCategory cat, float spd)
        {
            GameObject v = FindOrCreateChild(parent, name);
            v.transform.position = pos;
            v.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            SpriteRenderer sr = GetOrAdd<SpriteRenderer>(v);
            sr.sprite = LoadSpriteAsset(spritePath);
            sr.sortingOrder = 10;

            Rigidbody2D rb = GetOrAdd<Rigidbody2D>(v);
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            BoxCollider2D col = GetOrAdd<BoxCollider2D>(v);
            col.size = new Vector2(1.6f, 3.2f);

            Level3TopDownVehicle topVeh = GetOrAdd<Level3TopDownVehicle>(v);
            SetInt(topVeh, "category", (int)cat);
            SetFloat(topVeh, "speed", spd);
            SetVector2(topVeh, "moveDirection", dir);

            return v;
        }
        #endregion

        #region Mission 5 Elements (Rain & Finish Line)
        private static void CreateMission5FinishElements(Transform world, PlayerHeadlightController hl)
        {
            GameObject m5 = FindOrCreateChild(world, "Mission5_Elements");

            // Slippery Road Warning Sign at Y = 154
            CreateReflectiveSign(m5.transform, "Sign_SlipperyRoad", "SlipperyRoad", new Vector3(3.8f, 154f, 0f), hl);

            // Finish Line at Y = 186
            float finishY = 186f;
            Sprite checkeredSprite = LoadSpriteAsset(CheckeredFinishPath);
            GameObject finishLine = FindOrCreateChild(m5.transform, "FinishLineBanner");
            finishLine.transform.position = new Vector3(0f, finishY, 0f);
            finishLine.transform.localScale = new Vector3(6.4f, 1.8f, 1f);
            SpriteRenderer fsr = GetOrAdd<SpriteRenderer>(finishLine);
            fsr.sprite = checkeredSprite;
            fsr.sortingOrder = -6;

            // Floodlights illuminating the Finish Area
            GameObject floodlightL = CreateFinishFloodlight(m5.transform, "Floodlight_L", new Vector3(-4.4f, finishY + 2f, 0f));
            GameObject floodlightR = CreateFinishFloodlight(m5.transform, "Floodlight_R", new Vector3(4.4f, finishY + 2f, 0f));
        }

        private static GameObject CreateFinishFloodlight(Transform parent, string name, Vector3 pos)
        {
            GameObject flood = FindOrCreateChild(parent, name);
            flood.transform.position = pos;

            CreateWorldSprite(flood.transform, "Post", Vector3.zero, new Vector3(0.2f, 2.0f, 1f), new Color(0.7f, 0.75f, 0.85f, 1f), 0);

            Light2D light = GetOrAdd<Light2D>(flood);
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = 10f;
            light.intensity = 1.4f;
            light.color = new Color(0.9f, 0.95f, 1f, 1f);

            return flood;
        }
        #endregion

        #region UI Construction
        private static Level4UIController CreateUI(Level3PlayerCar playerCar, PlayerHeadlightController headlights, NightVisibilityController vis)
        {
            Canvas canvas = FindOrCreateCanvas();
            ClearChildren(canvas.transform);

            Sprite roundedPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPanelSpritePath);

            // 1. HUD Root
            GameObject hud = CreateUIPanel(canvas.transform, "HUD", new Color(0f, 0f, 0f, 0f));
            SetFullScreen(hud.GetComponent<RectTransform>());

            // --- Top-Left: Mission Card ---
            GameObject missionCard = CreateUIPanel(hud.transform, "MissionCard", TrafficTownTheme.CardDarkColor, roundedPanelSprite);
            SetRect(missionCard.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(340f, 92f));
            AddShadow(missionCard);

            GameObject badgePill = CreateUIPanel(missionCard.transform, "BadgePill", TrafficTownTheme.PrimaryColor, roundedPanelSprite);
            SetRect(badgePill.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -12f), new Vector2(120f, 24f));
            TMP_Text badgeText = CreateUIText(badgePill.transform, "BadgeText", "MISSION 1 / 5", 12, TextAlignmentOptions.Center, new Vector2(110f, 20f), Vector2.zero, new Vector2(0.5f, 0.5f));
            badgeText.color = Color.white;
            badgeText.fontStyle = FontStyles.Bold;

            TMP_Text titleText = CreateUIText(missionCard.transform, "MissionTitle", "LEARN THE NIGHT", 16, TextAlignmentOptions.Left, new Vector2(180f, 24f), new Vector2(144f, -12f), new Vector2(0f, 1f));
            titleText.color = TrafficTownTheme.AccentColor;
            titleText.fontStyle = FontStyles.Bold;

            TMP_Text objectiveText = CreateUIText(missionCard.transform, "ObjectiveText", "Turn headlights ON [H], keep speed under 40 km/h, and stay centered in your lane.", 12, TextAlignmentOptions.TopLeft, new Vector2(310f, 44f), new Vector2(14f, -42f), new Vector2(0f, 1f));
            objectiveText.color = TrafficTownTheme.TextPrimaryColor;

            // --- Top-Right: Score & Safety Card ---
            GameObject scoreCard = CreateUIPanel(hud.transform, "ScoreCard", TrafficTownTheme.CardDarkColor, roundedPanelSprite);
            SetRect(scoreCard.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(240f, 92f));
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

            GameObject meterTrough = CreateUIPanel(scoreCard.transform, "MeterTrough", new Color(0.15f, 0.20f, 0.28f, 0.9f), roundedPanelSprite);
            SetRect(meterTrough.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(208f, 14f));

            GameObject meterFillObj = CreateUIPanel(meterTrough.transform, "MeterFill", TrafficTownTheme.SuccessColor, roundedPanelSprite);
            SetRect(meterFillObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image meterFill = meterFillObj.GetComponent<Image>();
            meterFill.type = Image.Type.Filled;
            meterFill.fillMethod = Image.FillMethod.Horizontal;
            meterFill.fillAmount = 1f;

            // --- Bottom-Left: Night Status Card (Headlights & Visibility) ---
            GameObject nightStatusCard = CreateUIPanel(hud.transform, "NightStatusCard", TrafficTownTheme.CardDarkColor, roundedPanelSprite);
            SetRect(nightStatusCard.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(230f, 96f));
            AddShadow(nightStatusCard);

            // Headlight Badge Pill
            GameObject hlPill = CreateUIPanel(nightStatusCard.transform, "HeadlightPill", new Color(0.85f, 0.2f, 0.2f, 0.9f), roundedPanelSprite);
            SetRect(hlPill.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(200f, 28f));
            Image hlPillBg = hlPill.GetComponent<Image>();
            TMP_Text hlPillText = CreateUIText(hlPill.transform, "Label", "❌ LIGHTS OFF [H]", 12, TextAlignmentOptions.Center, new Vector2(190f, 24f), Vector2.zero, new Vector2(0.5f, 0.5f));
            hlPillText.color = Color.white;
            hlPillText.fontStyle = FontStyles.Bold;

            // Visibility Meter Text
            TMP_Text visLabel = CreateUIText(nightStatusCard.transform, "VisibilityText", "VISIBILITY: 70%", 12, TextAlignmentOptions.Center, new Vector2(200f, 18f), new Vector2(0f, -46f), new Vector2(0.5f, 1f));
            visLabel.color = TrafficTownTheme.TextSecondaryColor;
            visLabel.fontStyle = FontStyles.Bold;

            // Visibility Meter Trough
            GameObject visTrough = CreateUIPanel(nightStatusCard.transform, "VisTrough", new Color(0.15f, 0.20f, 0.28f, 0.9f), roundedPanelSprite);
            SetRect(visTrough.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(200f, 12f));

            GameObject visFillObj = CreateUIPanel(visTrough.transform, "VisFill", new Color(0.2f, 0.85f, 0.4f, 1f), roundedPanelSprite);
            SetRect(visFillObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image visFill = visFillObj.GetComponent<Image>();
            visFill.type = Image.Type.Filled;
            visFill.fillMethod = Image.FillMethod.Horizontal;
            visFill.fillAmount = 0.7f;

            // --- Bottom-Right: Speedometer ---
            GameObject speedCard = CreateUIPanel(hud.transform, "SpeedometerCard", TrafficTownTheme.CardDarkColor, roundedPanelSprite);
            SetRect(speedCard.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(190f, 88f));
            AddShadow(speedCard);

            TMP_Text speedVal = CreateUIText(speedCard.transform, "CurrentSpeed", "0", 28, TextAlignmentOptions.Center, new Vector2(170f, 36f), new Vector2(0f, 42f), new Vector2(0.5f, 0f));
            speedVal.color = TrafficTownTheme.TextPrimaryColor;
            speedVal.fontStyle = FontStyles.Bold;

            GameObject limitPill = CreateUIPanel(speedCard.transform, "LimitPill", TrafficTownTheme.PrimaryColor, roundedPanelSprite);
            SetRect(limitPill.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(150f, 24f));
            Image speedBadge = limitPill.GetComponent<Image>();
            TMP_Text limitVal = CreateUIText(limitPill.transform, "SpeedLimit", "LIMIT: 40 KM/H", 12, TextAlignmentOptions.Center, new Vector2(140f, 20f), Vector2.zero, new Vector2(0.5f, 0.5f));
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

            // --- Level Completion Screen ---
            GameObject completionOverlay = CreateUIPanel(canvas.transform, "CompletionPanel", new Color(0f, 0f, 0f, 0.85f));
            SetFullScreen(completionOverlay.GetComponent<RectTransform>());
            CanvasGroup compGroup = GetOrAdd<CanvasGroup>(completionOverlay);

            GameObject compCard = CreateUIPanel(completionOverlay.transform, "ModalCard", TrafficTownTheme.CardDarkColor, roundedPanelSprite);
            SetRect(compCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 440f));
            AddShadow(compCard);

            TMP_Text compTitle = CreateUIText(compCard.transform, "Title", "LEVEL COMPLETE!", 28, TextAlignmentOptions.Center, new Vector2(480f, 40f), new Vector2(0f, -24f), new Vector2(0.5f, 1f));
            compTitle.color = TrafficTownTheme.AccentColor;
            compTitle.fontStyle = FontStyles.Bold;

            TMP_Text compSub = CreateUIText(compCard.transform, "Subtitle", "NIGHT DRIVING MASTER!", 15, TextAlignmentOptions.Center, new Vector2(480f, 24f), new Vector2(0f, -66f), new Vector2(0.5f, 1f));
            compSub.color = TrafficTownTheme.TextPrimaryColor;
            compSub.fontStyle = FontStyles.Bold;

            TMP_Text starText = CreateUIText(compCard.transform, "Stars", "⭐⭐⭐", 34, TextAlignmentOptions.Center, new Vector2(480f, 44f), new Vector2(0f, -94f), new Vector2(0.5f, 1f));
            starText.color = new Color(1f, 0.85f, 0.2f, 1f);

            // Stats grid
            TMP_Text finalScore = CreateStatBox(compCard.transform, "ScoreBox", "FINAL SCORE", "100", new Vector2(-120f, -150f), TrafficTownTheme.PrimaryColor, roundedPanelSprite);
            TMP_Text safetyScore = CreateStatBox(compCard.transform, "SafetyBox", "SAFETY RATING", "100%", new Vector2(120f, -150f), TrafficTownTheme.SuccessColor, roundedPanelSprite);
            TMP_Text safeActions = CreateStatBox(compCard.transform, "ActionsBox", "SAFE ACTIONS", "5", new Vector2(-120f, -225f), TrafficTownTheme.AccentColor, roundedPanelSprite);
            TMP_Text violations = CreateStatBox(compCard.transform, "ViolationsBox", "VIOLATIONS", "0", new Vector2(120f, -225f), TrafficTownTheme.DangerColor, roundedPanelSprite);

            // Buttons
            Button nextBtn = CreateActionButton(compCard.transform, "NextBtn", "NEXT LEVEL  →", new Vector2(0f, 110f), TrafficTownTheme.SuccessColor, roundedPanelSprite);
            Button retryBtn = CreateActionButton(compCard.transform, "RetryBtn", "RETRY LEVEL  ↺", new Vector2(-115f, 50f), TrafficTownTheme.PrimaryColor, roundedPanelSprite);
            SetRect(retryBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-115f, 50f), new Vector2(210f, 44f));

            Button menuBtn = CreateActionButton(compCard.transform, "MenuBtn", "MAIN MENU  ⌂", new Vector2(115f, 50f), new Color(0.35f, 0.4f, 0.48f, 1f), roundedPanelSprite);
            SetRect(menuBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(115f, 50f), new Vector2(210f, 44f));

            // Wire UI Controller
            Level4UIController uiCtrl = GetOrAdd<Level4UIController>(canvas.gameObject);
            SetReference(uiCtrl, "missionBadgeText", badgeText);
            SetReference(uiCtrl, "missionTitleText", titleText);
            SetReference(uiCtrl, "missionObjectiveText", objectiveText);
            SetReference(uiCtrl, "scoreValueText", scoreVal);
            SetReference(uiCtrl, "safetyValueText", safetyVal);
            SetReference(uiCtrl, "safetyMeterFill", meterFill);
            SetReference(uiCtrl, "currentSpeedText", speedVal);
            SetReference(uiCtrl, "speedLimitText", limitVal);
            SetReference(uiCtrl, "speedometerBadge", speedBadge);
            SetReference(uiCtrl, "headlightBadgeText", hlPillText);
            SetReference(uiCtrl, "headlightBadgeBackground", hlPillBg);
            SetReference(uiCtrl, "visibilityText", visLabel);
            SetReference(uiCtrl, "visibilityMeterFill", visFill);
            SetReference(uiCtrl, "feedbackBanner", feedbackObj);
            SetReference(uiCtrl, "feedbackGroup", fbGroup);
            SetReference(uiCtrl, "feedbackIconText", fbIcon);
            SetReference(uiCtrl, "feedbackMessageText", fbMsg);
            SetReference(uiCtrl, "completionPanel", completionOverlay);
            SetReference(uiCtrl, "completionGroup", compGroup);
            SetReference(uiCtrl, "completionTitleText", compTitle);
            SetReference(uiCtrl, "completionSubtitleText", compSub);
            SetReference(uiCtrl, "finalScoreText", finalScore);
            SetReference(uiCtrl, "safetyScoreText", safetyScore);
            SetReference(uiCtrl, "safeActionsText", safeActions);
            SetReference(uiCtrl, "violationsText", violations);
            SetReference(uiCtrl, "starRatingText", starText);
            SetReference(uiCtrl, "nextLevelButton", nextBtn);
            SetReference(uiCtrl, "retryButton", retryBtn);
            SetReference(uiCtrl, "backToMenuButton", menuBtn);

            uiCtrl.SetDependencies(playerCar, headlights, vis);

            return uiCtrl;
        }

        private static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920f, 1080f);
                cs.matchWidthOrHeight = 0.5f;
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            return canvas;
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Color color, Sprite sprite = null)
        {
            GameObject panel = FindOrCreateChild(parent, name);
            Image img = GetOrAdd<Image>(panel);
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            return panel;
        }

        private static TMP_Text CreateUIText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align, Vector2 size, Vector2 pos, Vector2 pivot)
        {
            GameObject txtObj = FindOrCreateChild(parent, name);
            RectTransform rt = GetOrAdd<RectTransform>(txtObj);
            SetRect(rt, pivot, pivot, pivot, pos, size);

            TMP_Text tmp = GetOrAdd<TextMeshProUGUI>(txtObj);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        private static TMP_Text CreateStatBox(Transform parent, string name, string title, string val, Vector2 pos, Color accent, Sprite sprite)
        {
            GameObject box = CreateUIPanel(parent, name, new Color(0.12f, 0.16f, 0.22f, 0.95f), sprite);
            SetRect(box.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(220f, 64f));

            TMP_Text t = CreateUIText(box.transform, "Title", title, 11, TextAlignmentOptions.Center, new Vector2(200f, 16f), new Vector2(0f, -10f), new Vector2(0.5f, 1f));
            t.color = accent;
            t.fontStyle = FontStyles.Bold;

            TMP_Text v = CreateUIText(box.transform, "Value", val, 24, TextAlignmentOptions.Center, new Vector2(200f, 30f), new Vector2(0f, -30f), new Vector2(0.5f, 1f));
            v.color = Color.white;
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
            Sprite s = Resources.Load<Sprite>($"Signs/{signName}");
            if (s != null) return s;
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

        private static void EnsureAssetFolders()
        {
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
            if (!Directory.Exists("Assets/JSON")) Directory.CreateDirectory("Assets/JSON");
            if (!Directory.Exists("Assets/Scripts/Level4")) Directory.CreateDirectory("Assets/Scripts/Level4");
        }

        private static void ConfigureSpriteImporters()
        {
            string[] sprites = {
                HeadlightBeamConePath, StreetLampGlowPath, RainDropPath,
                AdultPedestrianDarkTopDownPath, CarBlueTopDownPath, CarRedTopDownPath
            };

            foreach (string path in sprites)
            {
                if (File.Exists(path))
                {
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null)
                    {
                        bool dirty = false;
                        if (importer.textureType != TextureImporterType.Sprite)
                        {
                            importer.textureType = TextureImporterType.Sprite;
                            dirty = true;
                        }
                        if (importer.spriteImportMode != SpriteImportMode.Single)
                        {
                            importer.spriteImportMode = SpriteImportMode.Single;
                            dirty = true;
                        }
                        if (dirty)
                        {
                            importer.SaveAndReimport();
                        }
                    }
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
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

        private static void SetRect(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void SetFullScreen(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AddShadow(GameObject obj)
        {
            Shadow s = GetOrAdd<Shadow>(obj);
            s.effectColor = new Color(0f, 0f, 0f, 0.45f);
            s.effectDistance = new Vector2(2f, -3f);
        }

        private static void SetReference(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null) field.SetValue(target, value);
        }

        private static void SetFloat(object target, string fieldName, float value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null) field.SetValue(target, value);
        }

        private static void SetInt(object target, string fieldName, int value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null) field.SetValue(target, value);
        }

        private static void SetVector2(object target, string fieldName, Vector2 value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null) field.SetValue(target, value);
        }

        private static void SetVector3(object target, string fieldName, Vector3 value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null) field.SetValue(target, value);
        }

        private static void SetObjectArray<T>(object target, string fieldName, T[] array)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null) field.SetValue(target, array);
        }
        #endregion
    }
}
#endif
