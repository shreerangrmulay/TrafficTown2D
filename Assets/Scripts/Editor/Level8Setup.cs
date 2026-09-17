#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrafficTown2D.Core;
using TrafficTown2D.Gameplay;
using TrafficTown2D.Player;
using TrafficTown2D.Traffic;
using TrafficTown2D.UI;

namespace TrafficTown2D.Editor
{
    /// <summary>Level 8 – Emergency Vehicles.</summary>
    public static class Level8Setup
    {
        private const string ScenePath = "Assets/Scenes/Level8.unity";

        [MenuItem("TrafficTown/Setup Level 8")]
        public static void SetupLevel8()
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
            LevelSetupHelpers.CreateStandardRoad(env.transform);

            // Road shoulder (pull-over area)
            LevelSetupHelpers.CreateSprite(env.transform, "ShoulderStripe", new Vector3(0f, -2.0f, -0.02f), new Vector3(22f, 0.2f, 1f), new Color(1f, 1f, 1f, 0.4f), 2, false);

            // Emergency spawn point
            GameObject emergencySpawn = LevelSetupHelpers.FindOrCreateChild(env.transform, "EmergencySpawnPoint");
            emergencySpawn.transform.position = new Vector3(-12f, -1.1f, 0f);

            // Traffic
            GameObject traffic = LevelSetupHelpers.FindOrCreate("Traffic");
            LevelSetupHelpers.CreateDefaultTrafficLane(traffic.transform, "TopLane", new Vector3(-9.5f, 1.1f, 0f), new Vector3(9.5f, 1.1f, 0f), 1f);

            // Player
            GameObject playerCar = LevelSetupHelpers.CreatePlayerCar(new Vector3(-7f, -1.1f, 0f));
            CarPlayerController carCtrl = LevelSetupHelpers.GetOrAdd<CarPlayerController>(playerCar);
            LevelSetupHelpers.SetBool(carCtrl, "allowVerticalMovement", true);

            // Gameplay
            GameObject gameplay = LevelSetupHelpers.FindOrCreate("Gameplay");
            ScoreManager score = LevelSetupHelpers.GetOrAdd<ScoreManager>(LevelSetupHelpers.FindOrCreateChild(gameplay.transform, "ScoreManager"));
            EmergencyVehicleController ctrl = LevelSetupHelpers.GetOrAdd<EmergencyVehicleController>(playerCar);
            LevelSetupHelpers.SetReference(ctrl, "playerCar", carCtrl);
            LevelSetupHelpers.SetReference(ctrl, "scoreManager", score);
            LevelSetupHelpers.SetReference(ctrl, "emergencySpawnPoint", emergencySpawn.transform);

            // UI
            LevelUIController ui = LevelSetupHelpers.CreateStandardUI(score, sceneLoader, "React to emergency vehicles! Pull over and stop.");
            FeedbackController fc = Object.FindAnyObjectByType<FeedbackController>(FindObjectsInactive.Include);
            LevelSetupHelpers.SetReference(ctrl, "feedback", fc);
            LevelSetupHelpers.SetReference(ctrl, "levelUI", ui);
            LevelSetupHelpers.CreateIntroPanel("EMERGENCY VEHICLES", "When you see flashing lights:\n1. Pull over (W/S).\n2. STOP completely.\n3. Wait for it to pass.");

            LevelSetupHelpers.AddSceneToBuild(ScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Level 8 (Emergency Vehicles) setup completed.");
        }
    }
}
#endif
