#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrafficTown2D.Core;
using TrafficTown2D.Level9;

namespace TrafficTown2D.Editor
{
    /// <summary>
    /// Editor setup script for Level 9 — ROUNDABOUT RULES.
    /// Bakes the interconnected 3-roundabout road network, AI circulating traffic,
    /// pedestrian crossings, signs, and UI into Assets/Scenes/Level9.unity.
    /// </summary>
    public static class Level9Setup
    {
        private const string ScenePath = "Assets/Scenes/Level9.unity";

        [MenuItem("TrafficTown/Setup Level 9")]
        public static void SetupLevel9()
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

            // Remove legacy placeholder objects to ensure a completely clean bake
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null)
                {
                    Object.DestroyImmediate(roots[i]);
                }
            }

            Level9Bootstrap.EnsureLevel9Scene();

            LevelSetupHelpers.AddSceneToBuild(ScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Level 9 (Roundabout Rules) setup completed and saved to Assets/Scenes/Level9.unity.");
        }
    }
}
#endif
