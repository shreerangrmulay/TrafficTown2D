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
    /// <summary>Level 6 – One-Way Streets.</summary>
    public static class Level6Setup
    {
        private const string ScenePath = "Assets/Scenes/Level6.unity";

        [MenuItem("TrafficTown/Setup Level 6")]
        public static void SetupLevel6()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode first."); return; }
            LevelSetupHelpers.EnsureAssetFolders();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            LevelSetupHelpers.EnsureCamera(); LevelSetupHelpers.EnsureGlobalLight(); LevelSetupHelpers.EnsureEventSystem();

            GameObject services = LevelSetupHelpers.FindOrCreate("Services");
            SceneLoader sceneLoader = LevelSetupHelpers.GetOrAdd<SceneLoader>(services);
            LevelSetupHelpers.GetOrAdd<GameManager>(services);

            // Environment: city grid with one-way streets
            GameObject env = LevelSetupHelpers.FindOrCreate("Environment");
            CreateCityGrid(env.transform);

            // Player
            GameObject playerCar = LevelSetupHelpers.CreatePlayerCar(new Vector3(-7f, -1.1f, 0f));
            CarPlayerController carCtrl = LevelSetupHelpers.GetOrAdd<CarPlayerController>(playerCar);
            LevelSetupHelpers.SetBool(carCtrl, "allowVerticalMovement", true);

            // Direction zones (one-way street triggers)
            GameObject zones = LevelSetupHelpers.FindOrCreate("DirectionZones");
            DirectionZone zone1 = CreateDirectionZone(zones.transform, "OneWay_MainSt", new Vector3(3f, 1.1f, 0f), Vector2.right, new Vector2(6f, 2f));
            DirectionZone zone2 = CreateDirectionZone(zones.transform, "OneWay_ElmSt", new Vector3(-3f, -1.1f, 0f), Vector2.left, new Vector2(6f, 2f));

            // Do Not Enter signs
            CreateDoNotEnterSign(env.transform, "DNE_1", new Vector3(0.5f, 2.6f, 0f));
            CreateDoNotEnterSign(env.transform, "DNE_2", new Vector3(-6f, -2.6f, 0f));

            // Gameplay
            GameObject gameplay = LevelSetupHelpers.FindOrCreate("Gameplay");
            ScoreManager score = LevelSetupHelpers.GetOrAdd<ScoreManager>(LevelSetupHelpers.FindOrCreateChild(gameplay.transform, "ScoreManager"));
            OneWayStreetController ctrl = LevelSetupHelpers.GetOrAdd<OneWayStreetController>(playerCar);
            LevelSetupHelpers.SetReference(ctrl, "playerCar", carCtrl);
            LevelSetupHelpers.SetReference(ctrl, "scoreManager", score);
            LevelSetupHelpers.SetObjectArray(ctrl, "directionZones", new Object[] { zone1, zone2 });

            // UI
            LevelUIController ui = LevelSetupHelpers.CreateStandardUI(score, sceneLoader, "Navigate without entering one-way streets wrong.");
            FeedbackController fc = Object.FindAnyObjectByType<FeedbackController>(FindObjectsInactive.Include);
            LevelSetupHelpers.SetReference(ctrl, "feedback", fc);
            LevelSetupHelpers.SetReference(ctrl, "levelUI", ui);
            LevelSetupHelpers.CreateIntroPanel("ONE-WAY STREETS", "Drive with A/D and W/S.\nWatch for DO NOT ENTER signs.\nReach the destination.");

            // Traffic
            GameObject traffic = LevelSetupHelpers.FindOrCreate("Traffic");
            LevelSetupHelpers.CreateDefaultTrafficLane(traffic.transform, "Lane1", new Vector3(-9.5f, 1.1f, 0f), new Vector3(9.5f, 1.1f, 0f), 1f);

            LevelSetupHelpers.AddSceneToBuild(ScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Level 6 (One-Way Streets) setup completed.");
        }

        private static void CreateCityGrid(Transform parent)
        {
            LevelSetupHelpers.CreateStandardRoad(parent);

            // One-way arrow indicators on road surface
            LevelSetupHelpers.CreateSprite(parent, "Arrow1", new Vector3(3f, 1.1f, -0.02f), new Vector3(0.8f, 0.3f, 1f), new Color(1f, 1f, 1f, 0.5f), 3, false);
            LevelSetupHelpers.CreateSprite(parent, "Arrow2", new Vector3(-3f, -1.1f, -0.02f), new Vector3(0.8f, 0.3f, 1f), new Color(1f, 1f, 1f, 0.5f), 3, false);
        }

        private static DirectionZone CreateDirectionZone(Transform parent, string name, Vector3 pos, Vector2 dir, Vector2 size)
        {
            GameObject go = LevelSetupHelpers.FindOrCreateChild(parent, name);
            go.transform.position = pos;
            BoxCollider2D col = LevelSetupHelpers.GetOrAdd<BoxCollider2D>(go);
            col.isTrigger = true;
            col.size = size;
            DirectionZone zone = LevelSetupHelpers.GetOrAdd<DirectionZone>(go);
            LevelSetupHelpers.SetVector2(zone, "allowedDirection", dir);
            return zone;
        }

        private static void CreateDoNotEnterSign(Transform parent, string name, Vector3 pos)
        {
            GameObject sign = LevelSetupHelpers.FindOrCreateChild(parent, name);
            sign.transform.localPosition = pos;
            LevelSetupHelpers.CreateSprite(sign.transform, "Post", new Vector3(0f, -0.5f, 0f), new Vector3(0.08f, 1f, 1f), new Color(0.29f, 0.31f, 0.32f, 1f), 12, false);
            LevelSetupHelpers.CreateSprite(sign.transform, "Face", Vector3.zero, new Vector3(0.65f, 0.65f, 1f), new Color(0.85f, 0.12f, 0.12f, 1f), 13, true);
            LevelSetupHelpers.CreateSprite(sign.transform, "Bar", Vector3.zero, new Vector3(0.45f, 0.12f, 1f), Color.white, 14, false);
        }
    }
}
#endif
