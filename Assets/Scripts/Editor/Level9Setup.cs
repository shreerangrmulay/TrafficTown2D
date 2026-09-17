#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrafficTown2D.Core;
using TrafficTown2D.Gameplay;
using TrafficTown2D.Player;
using TrafficTown2D.UI;

namespace TrafficTown2D.Editor
{
    /// <summary>Level 9 – Roundabouts.</summary>
    public static class Level9Setup
    {
        private const string ScenePath = "Assets/Scenes/Level9.unity";

        [MenuItem("TrafficTown/Setup Level 9")]
        public static void SetupLevel9()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode first."); return; }
            LevelSetupHelpers.EnsureAssetFolders();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            LevelSetupHelpers.EnsureCamera(); LevelSetupHelpers.EnsureGlobalLight(); LevelSetupHelpers.EnsureEventSystem();

            GameObject services = LevelSetupHelpers.FindOrCreate("Services");
            SceneLoader sceneLoader = LevelSetupHelpers.GetOrAdd<SceneLoader>(services);
            LevelSetupHelpers.GetOrAdd<GameManager>(services);

            // Environment
            GameObject env = LevelSetupHelpers.FindOrCreate("Environment");
            CreateRoundaboutEnvironment(env.transform);

            // Traffic in roundabout
            GameObject traffic = LevelSetupHelpers.FindOrCreate("Traffic");
            LevelSetupHelpers.CreateDefaultTrafficLane(traffic.transform, "CircleLane", new Vector3(-4f, 1.5f, 0f), new Vector3(4f, 1.5f, 0f), 1f);

            // Roundabout zones
            GameObject zones = LevelSetupHelpers.FindOrCreate("RoundaboutZones");
            RoundaboutZone entryZone = CreateRoundaboutZone(zones.transform, "EntryZone", new Vector3(-3f, -0.5f, 0f), true);
            RoundaboutZone exitZone = CreateRoundaboutZone(zones.transform, "ExitZone", new Vector3(3f, -0.5f, 0f), false);

            // Player
            GameObject playerCar = LevelSetupHelpers.CreatePlayerCar(new Vector3(-7f, -1.1f, 0f));
            CarPlayerController carCtrl = LevelSetupHelpers.GetOrAdd<CarPlayerController>(playerCar);
            LevelSetupHelpers.SetBool(carCtrl, "allowVerticalMovement", true);

            // Gameplay
            GameObject gameplay = LevelSetupHelpers.FindOrCreate("Gameplay");
            ScoreManager score = LevelSetupHelpers.GetOrAdd<ScoreManager>(LevelSetupHelpers.FindOrCreateChild(gameplay.transform, "ScoreManager"));
            RoundaboutController ctrl = LevelSetupHelpers.GetOrAdd<RoundaboutController>(playerCar);
            LevelSetupHelpers.SetReference(ctrl, "playerCar", carCtrl);
            LevelSetupHelpers.SetReference(ctrl, "scoreManager", score);
            LevelSetupHelpers.SetReference(ctrl, "entryZone", entryZone);
            LevelSetupHelpers.SetReference(ctrl, "exitZone", exitZone);

            // UI
            LevelUIController ui = LevelSetupHelpers.CreateStandardUI(score, sceneLoader, "Yield before entering. Signal before exiting.");
            FeedbackController fc = Object.FindAnyObjectByType<FeedbackController>(FindObjectsInactive.Include);
            LevelSetupHelpers.SetReference(ctrl, "feedback", fc);
            LevelSetupHelpers.SetReference(ctrl, "levelUI", ui);
            LevelSetupHelpers.CreateIntroPanel("ROUNDABOUTS", "1. YIELD to traffic inside.\n2. Enter when clear.\n3. Press Q or E to SIGNAL before exiting.");

            LevelSetupHelpers.AddSceneToBuild(ScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Level 9 (Roundabouts) setup completed.");
        }

        private static void CreateRoundaboutEnvironment(Transform parent)
        {
            // Background
            LevelSetupHelpers.CreateSprite(parent, "Ground", new Vector3(0f, 0f, 5f), new Vector3(22f, 12f, 1f), new Color(0.85f, 0.92f, 0.88f, 1f), -10, false);

            // Approach road (left)
            LevelSetupHelpers.CreateSprite(parent, "LeftRoad", new Vector3(-6f, -0.5f, 0f), new Vector3(10f, 3f, 1f), new Color(0.10f, 0.11f, 0.13f, 1f), 0, false);
            // Exit road (right)
            LevelSetupHelpers.CreateSprite(parent, "RightRoad", new Vector3(6f, -0.5f, 0f), new Vector3(10f, 3f, 1f), new Color(0.10f, 0.11f, 0.13f, 1f), 0, false);

            // Roundabout circle (road ring)
            LevelSetupHelpers.CreateSprite(parent, "RoundaboutOuter", new Vector3(0f, 0f, 0f), new Vector3(6f, 6f, 1f), new Color(0.10f, 0.11f, 0.13f, 1f), 0, true);
            LevelSetupHelpers.CreateSprite(parent, "RoundaboutInner", new Vector3(0f, 0f, -0.01f), new Vector3(3f, 3f, 1f), new Color(0.35f, 0.65f, 0.40f, 1f), 1, true);

            // Center island
            LevelSetupHelpers.CreateSprite(parent, "CenterIsland", new Vector3(0f, 0f, -0.02f), new Vector3(2f, 2f, 1f), new Color(0.45f, 0.75f, 0.50f, 1f), 2, true);

            // Arrows showing circulation direction (clockwise)
            LevelSetupHelpers.CreateSprite(parent, "Arrow_N", new Vector3(0f, 2.2f, -0.03f), new Vector3(0.6f, 0.2f, 1f), new Color(1f, 1f, 1f, 0.5f), 3, false);
            LevelSetupHelpers.CreateSprite(parent, "Arrow_S", new Vector3(0f, -2.2f, -0.03f), new Vector3(0.6f, 0.2f, 1f), new Color(1f, 1f, 1f, 0.5f), 3, false);

            // Yield sign at entry
            GameObject yieldSign = LevelSetupHelpers.FindOrCreateChild(parent, "YieldSign");
            yieldSign.transform.localPosition = new Vector3(-3.5f, -2.4f, 0f);
            LevelSetupHelpers.CreateSprite(yieldSign.transform, "Post", new Vector3(0f, -0.5f, 0f), new Vector3(0.08f, 1f, 1f), new Color(0.29f, 0.31f, 0.32f, 1f), 12, false);
            GameObject face = LevelSetupHelpers.CreateSprite(yieldSign.transform, "Face", Vector3.zero, new Vector3(0.6f, 0.6f, 1f), new Color(0.85f, 0.12f, 0.12f, 1f), 13, false);
            face.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Sidewalks
            LevelSetupHelpers.CreateSprite(parent, "SidewalkTop", new Vector3(0f, 4.5f, 0f), new Vector3(22f, 3f, 1f), new Color(0.67f, 0.72f, 0.69f, 1f), -2, false);
            LevelSetupHelpers.CreateSprite(parent, "SidewalkBottom", new Vector3(0f, -4.5f, 0f), new Vector3(22f, 3f, 1f), new Color(0.67f, 0.72f, 0.69f, 1f), -2, false);
        }

        private static RoundaboutZone CreateRoundaboutZone(Transform parent, string name, Vector3 pos, bool isEntry)
        {
            GameObject go = LevelSetupHelpers.FindOrCreateChild(parent, name);
            go.transform.position = pos;
            BoxCollider2D col = LevelSetupHelpers.GetOrAdd<BoxCollider2D>(go);
            col.isTrigger = true; col.size = new Vector2(2f, 3f);
            RoundaboutZone zone = LevelSetupHelpers.GetOrAdd<RoundaboutZone>(go);
            LevelSetupHelpers.SetBool(zone, "isEntry", isEntry);
            return zone;
        }
    }
}
#endif
