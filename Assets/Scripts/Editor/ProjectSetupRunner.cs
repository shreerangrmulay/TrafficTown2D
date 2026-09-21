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

            SetupAllScenes();
        }

        [MenuItem("TrafficTown/Setup All Scenes")]
        public static void SetupAllScenes()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            try
            {
                Debug.Log("[ProjectSetupRunner] === STARTING COMPLETE SCENE SETUP ===");

                // 1. Setup MainMenu
                Debug.Log("[ProjectSetupRunner] 1/12: Setting up MainMenu...");
                MainMenuSetup.SetupMainMenu();

                // 2. Setup Quiz
                Debug.Log("[ProjectSetupRunner] 2/12: Setting up Quiz...");
                QuizSetup.SetupQuiz();

                // 3. Setup Level 1
                Debug.Log("[ProjectSetupRunner] 3/12: Setting up Level 1...");
                Level1Setup.SetupLevel1();

                // 4. Setup Level 2
                Debug.Log("[ProjectSetupRunner] 4/12: Setting up Level 2...");
                Level2Setup.SetupLevel2();

                // 5. Setup Level 3
                Debug.Log("[ProjectSetupRunner] 5/12: Setting up Level 3...");
                Level3Setup.SetupLevel3();

                // 6. Setup Level 4
                Debug.Log("[ProjectSetupRunner] 6/12: Setting up Level 4...");
                Level4Setup.SetupLevel4();

                // 7. Setup Level 5
                Debug.Log("[ProjectSetupRunner] 7/12: Setting up Level 5...");
                Level5Setup.SetupLevel5();

                // 8. Setup Level 6
                Debug.Log("[ProjectSetupRunner] 8/12: Setting up Level 6...");
                Level6Setup.SetupLevel6();

                // 9. Setup Level 7
                Debug.Log("[ProjectSetupRunner] 9/12: Setting up Level 7...");
                Level7Setup.SetupLevel7();

                // 10. Setup Level 8
                Debug.Log("[ProjectSetupRunner] 10/12: Setting up Level 8...");
                Level8Setup.SetupLevel8();

                // 11. Setup Level 9
                Debug.Log("[ProjectSetupRunner] 11/12: Setting up Level 9...");
                Level9Setup.SetupLevel9();

                // 12. Setup Level 10
                Debug.Log("[ProjectSetupRunner] 12/12: Setting up Level 10...");
                Level10Setup.SetupLevel10();

                // Ensure Build Settings & Build Profiles have all scenes
                BuildSettingsUtility.EnsureAllScenesInBuildSettings();

                // Switch back to MainMenu scene so player is ready at the start
                EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

                File.WriteAllText(ResultPath, "SUCCESS");
                Debug.Log("[ProjectSetupRunner] === ALL SCENES, QUIZ & LEVELS SETUP COMPLETED SUCCESSFULLY! ===");
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
    }
}
#endif
