#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrafficTown2D.Editor
{
    public static class MainMenuSetupAutoRun
    {
        private const string FlagPath = "Temp/RunMainMenuSetup.flag";
        private const string ResultPath = "Temp/MainMenuSetupAutoRun.log";

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

            EditorApplication.delayCall += () =>
            {
                try
                {
                    Debug.Log("[MainMenuSetupAutoRun] Generating sprites and running MainMenuSetup...");
                    MainMenuAssetGenerator.GenerateAllSprites();
                    MainMenuSetup.SetupMainMenu();
                    EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
                    File.WriteAllText(ResultPath, "OK");
                    Debug.Log("[MainMenuSetupAutoRun] MainMenu modern UI redesign completed successfully!");
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
    }
}
#endif
// Trigger reload: 2026-09-24T00:27:30
