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
    /// <summary>Level 7 – School Zones & Speed Limits.</summary>
    public static class Level7Setup
    {
        private const string ScenePath = "Assets/Scenes/Level7.unity";

        [MenuItem("TrafficTown/Setup Level 7")]
        public static void SetupLevel7()
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

            // Speed zone markers
            CreateSpeedLimitSign(env.transform, "SpeedSign30", new Vector3(-7f, 2.6f, 0f), "30");
            CreateSpeedLimitSign(env.transform, "SpeedSign15", new Vector3(-1f, 2.6f, 0f), "15");
            CreateSpeedLimitSign(env.transform, "SpeedSign30_End", new Vector3(5f, 2.6f, 0f), "30");

            // School zone visual
            LevelSetupHelpers.CreateSprite(env.transform, "SchoolZoneRoad", new Vector3(2f, 0f, -0.02f), new Vector3(5f, 4.4f, 1f), new Color(1f, 0.9f, 0.3f, 0.15f), 2, false);
            CreateSchoolBuilding(env.transform, new Vector3(2f, 4.5f, 1f));

            // Speed zones (triggers)
            GameObject zones = LevelSetupHelpers.FindOrCreate("SpeedZones");
            SpeedZone zone1 = CreateSpeedZone(zones.transform, "StartZone", new Vector3(-5f, 0f, 0f), new Vector2(6f, 4.4f), 30f, "start_zone");
            SpeedZone zone2 = CreateSpeedZone(zones.transform, "SchoolZone", new Vector3(2f, 0f, 0f), new Vector2(5f, 4.4f), 15f, "school_zone");
            SpeedZone zone3 = CreateSpeedZone(zones.transform, "EndZone", new Vector3(7f, 0f, 0f), new Vector2(4f, 4.4f), 30f, "end_zone");

            // Player
            GameObject playerCar = LevelSetupHelpers.CreatePlayerCar(new Vector3(-8f, -1.1f, 0f));
            CarPlayerController carCtrl = LevelSetupHelpers.GetOrAdd<CarPlayerController>(playerCar);

            // Gameplay
            GameObject gameplay = LevelSetupHelpers.FindOrCreate("Gameplay");
            ScoreManager score = LevelSetupHelpers.GetOrAdd<ScoreManager>(LevelSetupHelpers.FindOrCreateChild(gameplay.transform, "ScoreManager"));
            SpeedZoneController ctrl = LevelSetupHelpers.GetOrAdd<SpeedZoneController>(playerCar);
            LevelSetupHelpers.SetReference(ctrl, "playerCar", carCtrl);
            LevelSetupHelpers.SetReference(ctrl, "scoreManager", score);
            LevelSetupHelpers.SetObjectArray(ctrl, "speedZones", new Object[] { zone1, zone2, zone3 });

            // UI
            LevelUIController ui = LevelSetupHelpers.CreateStandardUI(score, sceneLoader, "Obey speed limits! Slow down in school zones.");
            FeedbackController fc = Object.FindAnyObjectByType<FeedbackController>(FindObjectsInactive.Include);
            LevelSetupHelpers.SetReference(ctrl, "feedback", fc);
            LevelSetupHelpers.SetReference(ctrl, "levelUI", ui);
            LevelSetupHelpers.CreateIntroPanel("SCHOOL ZONES & SPEED LIMITS", "Obey speed limits.\nSLOW DOWN in school zones!\nDrive with A/D keys.");

            LevelSetupHelpers.AddSceneToBuild(ScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Level 7 (School Zones) setup completed.");
        }

        private static SpeedZone CreateSpeedZone(Transform parent, string name, Vector3 pos, Vector2 size, float limit, string zoneName)
        {
            GameObject go = LevelSetupHelpers.FindOrCreateChild(parent, name);
            go.transform.position = pos;
            BoxCollider2D col = LevelSetupHelpers.GetOrAdd<BoxCollider2D>(go);
            col.isTrigger = true; col.size = size;
            SpeedZone zone = LevelSetupHelpers.GetOrAdd<SpeedZone>(go);
            LevelSetupHelpers.SetFloat(zone, "speedLimit", limit);
            SerializedObject so = new SerializedObject(zone);
            SerializedProperty sp = so.FindProperty("zoneName");
            if (sp != null) { sp.stringValue = zoneName; so.ApplyModifiedPropertiesWithoutUndo(); }
            return zone;
        }

        private static void CreateSpeedLimitSign(Transform parent, string name, Vector3 pos, string limit)
        {
            GameObject sign = LevelSetupHelpers.FindOrCreateChild(parent, name);
            sign.transform.localPosition = pos;
            LevelSetupHelpers.CreateSprite(sign.transform, "Post", new Vector3(0f, -0.5f, 0f), new Vector3(0.08f, 1f, 1f), new Color(0.29f, 0.31f, 0.32f, 1f), 12, false);
            LevelSetupHelpers.CreateSprite(sign.transform, "Face", Vector3.zero, new Vector3(0.55f, 0.7f, 1f), Color.white, 13, false);
            LevelSetupHelpers.CreateSprite(sign.transform, "Border", Vector3.zero, new Vector3(0.6f, 0.75f, 1f), new Color(0.1f, 0.1f, 0.1f, 1f), 12, false);
        }

        private static void CreateSchoolBuilding(Transform parent, Vector3 pos)
        {
            GameObject school = LevelSetupHelpers.FindOrCreateChild(parent, "SchoolBuilding");
            school.transform.localPosition = pos;
            LevelSetupHelpers.CreateSprite(school.transform, "Wall", Vector3.zero, new Vector3(3.5f, 2f, 1f), new Color(0.92f, 0.82f, 0.68f, 1f), -4, false);
            LevelSetupHelpers.CreateSprite(school.transform, "Roof", new Vector3(0f, 1.2f, 0f), new Vector3(3.8f, 0.6f, 1f), new Color(0.55f, 0.25f, 0.18f, 1f), -3, false);
            LevelSetupHelpers.CreateSprite(school.transform, "Door", new Vector3(0f, -0.6f, -0.01f), new Vector3(0.5f, 0.8f, 1f), new Color(0.45f, 0.30f, 0.18f, 1f), -3, false);
        }
    }
}
#endif
