#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrafficTown2D.Editor
{
    public static class Level6SetupAutoRun
    {
        private const string FlagPath = "Temp/RunLevel6Setup.flag";
        private const string ResultPath = "Temp/Level6SetupAutoRun.log";

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
                    Debug.Log("[Level6SetupAutoRun] Running Level6Setup.SetupLevel6()...");
                    Level6Setup.SetupLevel6();
                    MainMenuSetup.SetupMainMenu();
                    File.WriteAllText(ResultPath, "OK");
                    Debug.Log("[Level6SetupAutoRun] SetupLevel6 & MainMenu completed successfully!");
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
// Trigger reload: 2026-09-23T18:05:00
