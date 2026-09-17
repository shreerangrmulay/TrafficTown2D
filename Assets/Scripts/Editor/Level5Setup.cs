#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using TrafficTown2D.Core;
using TrafficTown2D.Gameplay;
using TrafficTown2D.Player;
using TrafficTown2D.Traffic;
using TrafficTown2D.UI;

namespace TrafficTown2D.Editor
{
    /// <summary>Level 5 – The Stop Sign (4-way stop intersection).</summary>
    public static class Level5Setup
    {
        private const string ScenePath = "Assets/Scenes/Level5.unity";

        [MenuItem("TrafficTown/Setup Level 5")]
        public static void SetupLevel5()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode first."); return; }
            LevelSetupHelpers.EnsureAssetFolders();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            LevelSetupHelpers.EnsureCamera(); LevelSetupHelpers.EnsureGlobalLight(); LevelSetupHelpers.EnsureEventSystem();

            // Services
            GameObject services = LevelSetupHelpers.FindOrCreate("Services");
            SceneLoader sceneLoader = LevelSetupHelpers.GetOrAdd<SceneLoader>(services);
            LevelSetupHelpers.GetOrAdd<GameManager>(services);

            // Environment
            GameObject env = LevelSetupHelpers.FindOrCreate("Environment");
            CreateIntersection(env.transform);

            // Traffic
            GameObject traffic = LevelSetupHelpers.FindOrCreate("Traffic");
            LevelSetupHelpers.CreateDefaultTrafficLane(traffic.transform, "WestLane", new Vector3(-9.5f, 0.8f, 0f), new Vector3(9.5f, 0.8f, 0f), 1f);
            LevelSetupHelpers.CreateDefaultTrafficLane(traffic.transform, "NorthLane", new Vector3(0.8f, 6f, 0f), new Vector3(0.8f, -6f, 0f), -1f);

            // Player
            GameObject playerCar = LevelSetupHelpers.CreatePlayerCar(new Vector3(-7f, -1.1f, 0f));
            CarPlayerController carCtrl = LevelSetupHelpers.GetOrAdd<CarPlayerController>(playerCar);

            // Stop line & intersection center markers
            GameObject stopLine = LevelSetupHelpers.FindOrCreateChild(env.transform, "StopLine");
            stopLine.transform.position = new Vector3(-2.5f, -1.1f, 0f);
            LevelSetupHelpers.CreateSprite(stopLine.transform, "Line", Vector3.zero, new Vector3(0.15f, 1.2f, 1f), Color.white, 3, false);

            GameObject intCenter = LevelSetupHelpers.FindOrCreateChild(env.transform, "IntersectionCenter");
            intCenter.transform.position = new Vector3(0f, 0f, 0f);

            // Stop signs at each approach
            CreateStopSign(env.transform, "StopSignWest", new Vector3(-3f, -2.6f, 0f));
            CreateStopSign(env.transform, "StopSignEast", new Vector3(3f, 2.6f, 0f));
            CreateStopSign(env.transform, "StopSignNorth", new Vector3(2.6f, -3f, 0f));
            CreateStopSign(env.transform, "StopSignSouth", new Vector3(-2.6f, 3f, 0f));

            // Gameplay
            GameObject gameplay = LevelSetupHelpers.FindOrCreate("Gameplay");
            ScoreManager score = LevelSetupHelpers.GetOrAdd<ScoreManager>(LevelSetupHelpers.FindOrCreateChild(gameplay.transform, "ScoreManager"));
            StopSignController ctrl = LevelSetupHelpers.GetOrAdd<StopSignController>(playerCar);
            LevelSetupHelpers.SetReference(ctrl, "playerCar", carCtrl);
            LevelSetupHelpers.SetReference(ctrl, "stopLine", stopLine.transform);
            LevelSetupHelpers.SetReference(ctrl, "intersectionCenter", intCenter.transform);
            LevelSetupHelpers.SetReference(ctrl, "scoreManager", score);

            // UI
            LevelUIController ui = LevelSetupHelpers.CreateStandardUI(score, sceneLoader, "Stop at the STOP sign, then yield.");
            FeedbackController fc = Object.FindAnyObjectByType<FeedbackController>(FindObjectsInactive.Include);
            LevelSetupHelpers.SetReference(ctrl, "feedback", fc);
            LevelSetupHelpers.SetReference(ctrl, "levelUI", ui);
            LevelSetupHelpers.CreateIntroPanel("THE STOP SIGN", "Come to a COMPLETE stop.\nYield to vehicles that arrived first.\nProceed when clear.");

            LevelSetupHelpers.AddSceneToBuild(ScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Level 5 (The Stop Sign) setup completed.");
        }

        private static void CreateIntersection(Transform parent)
        {
            GameObject bg = LevelSetupHelpers.FindOrCreateChild(parent, "Background");
            LevelSetupHelpers.CreateSprite(bg.transform, "Ground", new Vector3(0f, 0f, 5f), new Vector3(22f, 12f, 1f), new Color(0.85f, 0.92f, 0.88f, 1f), -10, false);

            // Horizontal road
            LevelSetupHelpers.CreateSprite(parent, "HRoad", Vector3.zero, new Vector3(22f, 4.4f, 1f), new Color(0.10f, 0.11f, 0.13f, 1f), 0, false);
            // Vertical road
            LevelSetupHelpers.CreateSprite(parent, "VRoad", Vector3.zero, new Vector3(4.4f, 12f, 1f), new Color(0.10f, 0.11f, 0.13f, 1f), 0, false);

            // Intersection box (slightly lighter)
            LevelSetupHelpers.CreateSprite(parent, "IntersectionBox", Vector3.zero, new Vector3(4.4f, 4.4f, 1f), new Color(0.14f, 0.15f, 0.17f, 1f), 1, false);

            // Road edges
            LevelSetupHelpers.CreateSprite(parent, "HTopEdge", new Vector3(0f, 2.15f, -0.01f), new Vector3(22f, 0.1f, 1f), new Color(0.92f, 0.88f, 0.62f, 1f), 2, false);
            LevelSetupHelpers.CreateSprite(parent, "HBottomEdge", new Vector3(0f, -2.15f, -0.01f), new Vector3(22f, 0.1f, 1f), new Color(0.92f, 0.88f, 0.62f, 1f), 2, false);

            // Sidewalks in corners
            LevelSetupHelpers.CreateSprite(parent, "SW_NW", new Vector3(-5f, 5f, 0f), new Vector3(12f, 5f, 1f), new Color(0.67f, 0.72f, 0.69f, 1f), -2, false);
            LevelSetupHelpers.CreateSprite(parent, "SW_NE", new Vector3(5f, 5f, 0f), new Vector3(12f, 5f, 1f), new Color(0.67f, 0.72f, 0.69f, 1f), -2, false);
            LevelSetupHelpers.CreateSprite(parent, "SW_SW", new Vector3(-5f, -5f, 0f), new Vector3(12f, 5f, 1f), new Color(0.67f, 0.72f, 0.69f, 1f), -2, false);
            LevelSetupHelpers.CreateSprite(parent, "SW_SE", new Vector3(5f, -5f, 0f), new Vector3(12f, 5f, 1f), new Color(0.67f, 0.72f, 0.69f, 1f), -2, false);
        }

        private static void CreateStopSign(Transform parent, string name, Vector3 position)
        {
            GameObject sign = LevelSetupHelpers.FindOrCreateChild(parent, name);
            sign.transform.localPosition = position;
            LevelSetupHelpers.CreateSprite(sign.transform, "Post", new Vector3(0f, -0.5f, 0f), new Vector3(0.08f, 1f, 1f), new Color(0.29f, 0.31f, 0.32f, 1f), 12, false);
            GameObject face = LevelSetupHelpers.CreateSprite(sign.transform, "Face", new Vector3(0f, 0.15f, 0f), new Vector3(0.6f, 0.6f, 1f), new Color(0.85f, 0.12f, 0.12f, 1f), 13, false);
            face.transform.localRotation = Quaternion.Euler(0f, 0f, 22.5f);
        }
    }
}
#endif
