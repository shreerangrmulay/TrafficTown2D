#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TrafficTown2D.Editor;

namespace TrafficTown2D.Editor
{
    public static class ProjectSetupRunner
    {
        private const string FlagPath = "Temp/RunProjectSetup.flag";
        private const string ResultPath = "Temp/ProjectSetup.log";

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.update -= CheckFlag;
            EditorApplication.update += CheckFlag;
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

                try
                {
                    Debug.Log("[ProjectSetupRunner] === STARTING COMPLETE SCENE SETUP ===");

                    // 1. Setup MainMenu
                    Debug.Log("[ProjectSetupRunner] 1/4: Setting up MainMenu...");
                    MainMenuSetup.SetupMainMenu();

                    // 2. Setup Level 1
                    Debug.Log("[ProjectSetupRunner] 2/4: Setting up Level 1...");
                    Level1Setup.SetupLevel1();

                    // 3. Setup Level 2
                    Debug.Log("[ProjectSetupRunner] 3/4: Setting up Level 2...");
                    Level2Setup.SetupLevel2();

                    // 4. Setup Level 3
                    Debug.Log("[ProjectSetupRunner] 4/4: Setting up Level 3...");
                    Level3Setup.SetupLevel3();

                    // Ensure Build Settings has all 4 scenes in order
                    string[] buildScenePaths = new[]
                    {
                        "Assets/Scenes/MainMenu.unity",
                        "Assets/Scenes/Level1.unity",
                        "Assets/Scenes/Level2.unity",
                        "Assets/Scenes/Level3.unity"
                    };

                    List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
                    foreach (string path in buildScenePaths)
                    {
                        if (File.Exists(path))
                        {
                            buildScenes.Add(new EditorBuildSettingsScene(path, true));
                        }
                    }
                    EditorBuildSettings.scenes = buildScenes.ToArray();

                    // Switch back to MainMenu scene so player is ready at the start
                    EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

                    File.WriteAllText(ResultPath, "SUCCESS");
                    Debug.Log("[ProjectSetupRunner] === ALL SCENES & LEVELS SETUP COMPLETED SUCCESSFULLY! ===");
                }
                catch (Exception ex)
                {
                    File.WriteAllText(ResultPath, "ERROR: " + ex.ToString());
                    Debug.LogError("[ProjectSetupRunner] Error during setup: " + ex);
                }
                finally
                {
                    if (File.Exists(FlagPath))
                    {
                        File.Delete(FlagPath);
                    }
                }
        }

        [MenuItem("TrafficTown/Setup All Scenes")]
        public static void TriggerSetupAllScenes()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            File.WriteAllText(FlagPath, "1");
            AssetDatabase.Refresh();
        }
    }
}
#endif
