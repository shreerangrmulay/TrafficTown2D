#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrafficTown2D.Level7;

namespace TrafficTown2D.Editor
{
    public static class Level7Setup
    {
        private const string ScenePath = "Assets/Scenes/Level7.unity";
        private const string AutoRunPrefKey = "TrafficTown_Level7_AutoSetup_v3";
        private const string FlagPath = "Temp/RunLevel7Setup.flag";
        private const string ResultPath = "Temp/Level7SetupAutoRun.log";

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.update -= CheckFlag;
            EditorApplication.update += CheckFlag;

            if (!EditorPrefs.GetBool(AutoRunPrefKey, false))
            {
                EditorPrefs.SetBool(AutoRunPrefKey, true);
                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication.isPlaying)
                    {
                        Debug.Log("[Level7Setup] Auto-generating Level 7 scene on compile (v3)...");
                        SetupLevel7();
                    }
                };
            }
        }

        private static void CheckFlag()
        {
            if (!File.Exists(FlagPath)) return;
            EditorApplication.update -= CheckFlag;

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.update += CheckFlag;
                return;
            }

            EditorApplication.delayCall += () =>
            {
                try
                {
                    Debug.Log("[Level7Setup] Running Level7Setup.SetupLevel7 via flag...");
                    SetupLevel7();
                    File.WriteAllText(ResultPath, "OK");
                    Debug.Log("[Level7Setup] SetupLevel7 completed successfully via flag!");
                }
                catch (System.Exception ex)
                {
                    File.WriteAllText(ResultPath, ex.ToString());
                    Debug.LogException(ex);
                }
                finally
                {
                    if (File.Exists(FlagPath)) File.Delete(FlagPath);
                }
            };
        }

        [MenuItem("TrafficTown/Setup Level 7")]
        public static void SetupLevel7()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before running TrafficTown -> Setup Level 7.");
                return;
            }

            Scene level7Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(level7Scene, ScenePath);

            // Construct full environment, road network, vehicles, traffic lights, safe stop bays, and UI
            Level7Bootstrap.EnsureLevel7Scene();

            // Attach bootstrap component to scene root
            GameObject bootObj = GameObject.Find("Level7Bootstrap");
            if (bootObj == null) bootObj = new GameObject("Level7Bootstrap");
            if (bootObj.GetComponent<Level7Bootstrap>() == null) bootObj.AddComponent<Level7Bootstrap>();

            // Ensure Build Settings registration
            BuildSettingsUtility.EnsureAllScenesInBuildSettings();

            EditorSceneManager.MarkSceneDirty(level7Scene);
            EditorSceneManager.SaveScene(level7Scene);
            Debug.Log("[Level7Setup] Successfully built and saved Level 7 -- Distracted Driving Challenge!");
        }
    }
}
#endif
