#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrafficTown2D.Core;
using TrafficTown2D.Level8;

namespace TrafficTown2D.Editor
{
    /// <summary>
    /// Editor setup script for Level 8 — THE OVERTAKE.
    /// Bakes the connected road environment, vehicles, signs, and UI into Assets/Scenes/Level8.unity.
    /// </summary>
    public static class Level8Setup
    {
        private const string ScenePath = "Assets/Scenes/Level8.unity";

        [MenuItem("TrafficTown/Setup Level 8")]
        public static void SetupLevel8()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode first."); return; }
            LevelSetupHelpers.EnsureAssetFolders();

            Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            catch
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            if (!scene.IsValid())
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            // Remove legacy placeholder objects if present
            GameObject oldEnv = GameObject.Find("Environment");
            if (oldEnv != null && oldEnv.transform.Find("ShoulderStripe") != null)
            {
                Object.DestroyImmediate(oldEnv);
            }
            GameObject oldGameplay = GameObject.Find("Gameplay");
            if (oldGameplay != null && oldGameplay.GetComponent<Level8OvertakeManager>() == null)
            {
                Object.DestroyImmediate(oldGameplay);
            }
            GameObject oldTraffic = GameObject.Find("Traffic");
            if (oldTraffic != null && oldTraffic.transform.Find("TopLane") != null)
            {
                Object.DestroyImmediate(oldTraffic);
            }
            GameObject uiObj = GameObject.Find("UI");
            if (uiObj != null)
            {
                Transform oldHud = uiObj.transform.Find("HUD");
                if (oldHud != null) Object.DestroyImmediate(oldHud.gameObject);
            }

            Level8Bootstrap.EnsureLevel8Scene();

            LevelSetupHelpers.AddSceneToBuild(ScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Level 8 (The Overtake) setup completed and saved to Assets/Scenes/Level8.unity.");
        }
    }
}
#endif
