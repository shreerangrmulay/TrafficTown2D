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
    /// <summary>Level 10 – The Ultimate Commute.</summary>
    public static class Level10Setup
    {
        private const string ScenePath = "Assets/Scenes/Level10.unity";

        [MenuItem("TrafficTown/Setup Level 10")]
        public static void SetupLevel10()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode first."); return; }
            LevelSetupHelpers.EnsureAssetFolders();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            LevelSetupHelpers.EnsureCamera(); LevelSetupHelpers.EnsureGlobalLight(); LevelSetupHelpers.EnsureEventSystem();

            Camera cam = Camera.main;
            if (cam != null) cam.orthographicSize = 5.5f; // Slightly larger for the long commute

            GameObject services = LevelSetupHelpers.FindOrCreate("Services");
            SceneLoader sceneLoader = LevelSetupHelpers.GetOrAdd<SceneLoader>(services);
            LevelSetupHelpers.GetOrAdd<GameManager>(services);

            // Environment – long road
            GameObject env = LevelSetupHelpers.FindOrCreate("Environment");
            CreateCommuteEnvironment(env.transform);

            // Traffic
            GameObject traffic = LevelSetupHelpers.FindOrCreate("Traffic");
            LevelSetupHelpers.CreateDefaultTrafficLane(traffic.transform, "Lane1", new Vector3(-12f, 1.1f, 0f), new Vector3(16f, 1.1f, 0f), 1f);

            // Checkpoints
            GameObject checkpointsParent = LevelSetupHelpers.FindOrCreate("Checkpoints");
            CheckpointZone cp1 = CreateCheckpoint(checkpointsParent.transform, "CP_1", new Vector3(-5f, -1.1f, 0f), 0, "cp_crossing", "Respect pedestrian crossings");
            CheckpointZone cp2 = CreateCheckpoint(checkpointsParent.transform, "CP_2", new Vector3(0f, -1.1f, 0f), 1, "cp_speed", "Watch your speed");
            CheckpointZone cp3 = CreateCheckpoint(checkpointsParent.transform, "CP_3", new Vector3(5f, -1.1f, 0f), 2, "cp_yield", "Yield to traffic");
            CheckpointZone cp4 = CreateCheckpoint(checkpointsParent.transform, "CP_4", new Vector3(10f, -1.1f, 0f), 3, "cp_signal", "Use your signals");

            // Player
            GameObject playerCar = LevelSetupHelpers.CreatePlayerCar(new Vector3(-9f, -1.1f, 0f));
            CarPlayerController carCtrl = LevelSetupHelpers.GetOrAdd<CarPlayerController>(playerCar);
            LevelSetupHelpers.SetBool(carCtrl, "allowVerticalMovement", true);

            // Gameplay
            GameObject gameplay = LevelSetupHelpers.FindOrCreate("Gameplay");
            ScoreManager score = LevelSetupHelpers.GetOrAdd<ScoreManager>(LevelSetupHelpers.FindOrCreateChild(gameplay.transform, "ScoreManager"));
            UltimateCommuteController ctrl = LevelSetupHelpers.GetOrAdd<UltimateCommuteController>(playerCar);
            LevelSetupHelpers.SetReference(ctrl, "playerCar", carCtrl);
            LevelSetupHelpers.SetReference(ctrl, "scoreManager", score);
            LevelSetupHelpers.SetObjectArray(ctrl, "checkpoints", new Object[] { cp1, cp2, cp3, cp4 });
            LevelSetupHelpers.SetFloat(ctrl, "destinationX", 14f);

            // UI
            LevelUIController ui = LevelSetupHelpers.CreateStandardUI(score, sceneLoader, "The Ultimate Commute — apply all rules!");
            FeedbackController fc = Object.FindAnyObjectByType<FeedbackController>(FindObjectsInactive.Include);
            LevelSetupHelpers.SetReference(ctrl, "feedback", fc);
            LevelSetupHelpers.SetReference(ctrl, "levelUI", ui);
            LevelSetupHelpers.CreateIntroPanel("THE ULTIMATE COMMUTE", "Apply everything you've learned!\nPass each checkpoint safely.\n3 strikes and you're out!");

            LevelSetupHelpers.AddSceneToBuild(ScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Level 10 (The Ultimate Commute) setup completed.");
        }

        private static void CreateCommuteEnvironment(Transform parent)
        {
            // Extended background for longer road
            LevelSetupHelpers.CreateSprite(parent, "Ground", new Vector3(2f, 0f, 5f), new Vector3(30f, 14f, 1f), new Color(0.82f, 0.90f, 0.85f, 1f), -10, false);

            // Long road
            LevelSetupHelpers.CreateSprite(parent, "RoadBase", new Vector3(2f, 0f, 0f), new Vector3(30f, 4.4f, 1f), new Color(0.10f, 0.11f, 0.13f, 1f), 0, false);
            LevelSetupHelpers.CreateSprite(parent, "TopEdge", new Vector3(2f, 2.15f, -0.01f), new Vector3(30f, 0.1f, 1f), new Color(0.92f, 0.88f, 0.62f, 1f), 1, false);
            LevelSetupHelpers.CreateSprite(parent, "BottomEdge", new Vector3(2f, -2.15f, -0.01f), new Vector3(30f, 0.1f, 1f), new Color(0.92f, 0.88f, 0.62f, 1f), 1, false);

            for (float x = -12f; x <= 16f; x += 1.2f)
                LevelSetupHelpers.CreateSprite(parent, "Dash_" + x.ToString("F1"), new Vector3(x, 0f, -0.01f), new Vector3(0.7f, 0.08f, 1f), new Color(1f, 0.94f, 0.45f, 1f), 1, false);

            LevelSetupHelpers.CreateSprite(parent, "SidewalkTop", new Vector3(2f, 3.25f, 0f), new Vector3(30f, 1.9f, 1f), new Color(0.67f, 0.72f, 0.69f, 1f), -2, false);
            LevelSetupHelpers.CreateSprite(parent, "SidewalkBottom", new Vector3(2f, -3.25f, 0f), new Vector3(30f, 1.9f, 1f), new Color(0.67f, 0.72f, 0.69f, 1f), -2, false);

            // Buildings along the route
            LevelSetupHelpers.CreateSprite(parent, "B1", new Vector3(-8f, 5f, 2f), new Vector3(3f, 2f, 1f), new Color(0.80f, 0.85f, 0.92f, 1f), -5, false);
            LevelSetupHelpers.CreateSprite(parent, "B2", new Vector3(-3f, 5.3f, 2f), new Vector3(4f, 2.5f, 1f), new Color(0.90f, 0.84f, 0.78f, 1f), -5, false);
            LevelSetupHelpers.CreateSprite(parent, "B3", new Vector3(3f, 5f, 2f), new Vector3(3.5f, 2.2f, 1f), new Color(0.78f, 0.88f, 0.82f, 1f), -5, false);
            LevelSetupHelpers.CreateSprite(parent, "B4", new Vector3(9f, 5.2f, 2f), new Vector3(3f, 2.8f, 1f), new Color(0.85f, 0.80f, 0.90f, 1f), -5, false);

            // Checkpoint visual markers on the road
            Color cpColor = new Color(0.2f, 0.8f, 0.9f, 0.3f);
            LevelSetupHelpers.CreateSprite(parent, "CPMark1", new Vector3(-5f, 0f, -0.02f), new Vector3(0.15f, 4.4f, 1f), cpColor, 2, false);
            LevelSetupHelpers.CreateSprite(parent, "CPMark2", new Vector3(0f, 0f, -0.02f), new Vector3(0.15f, 4.4f, 1f), cpColor, 2, false);
            LevelSetupHelpers.CreateSprite(parent, "CPMark3", new Vector3(5f, 0f, -0.02f), new Vector3(0.15f, 4.4f, 1f), cpColor, 2, false);
            LevelSetupHelpers.CreateSprite(parent, "CPMark4", new Vector3(10f, 0f, -0.02f), new Vector3(0.15f, 4.4f, 1f), cpColor, 2, false);

            // Finish flag
            LevelSetupHelpers.CreateSprite(parent, "FinishFlag", new Vector3(14f, 2.6f, -0.03f), new Vector3(0.8f, 0.6f, 1f), new Color(1f, 0.85f, 0.15f, 1f), 15, false);
        }

        private static CheckpointZone CreateCheckpoint(Transform parent, string name, Vector3 pos, int order, string id, string rule)
        {
            GameObject go = LevelSetupHelpers.FindOrCreateChild(parent, name);
            go.transform.position = pos;
            BoxCollider2D col = LevelSetupHelpers.GetOrAdd<BoxCollider2D>(go);
            col.isTrigger = true; col.size = new Vector2(1.5f, 4f);
            CheckpointZone zone = LevelSetupHelpers.GetOrAdd<CheckpointZone>(go);
            LevelSetupHelpers.SetInt(zone, "orderIndex", order);

            SerializedObject so = new SerializedObject(zone);
            SerializedProperty sp = so.FindProperty("checkpointId");
            if (sp != null) { sp.stringValue = id; }
            sp = so.FindProperty("ruleDescription");
            if (sp != null) { sp.stringValue = rule; }
            so.ApplyModifiedPropertiesWithoutUndo();
            return zone;
        }
    }
}
#endif
