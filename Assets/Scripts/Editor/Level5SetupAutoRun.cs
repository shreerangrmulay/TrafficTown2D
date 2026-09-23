#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrafficTown2D.Editor
{
    public static class Level5SetupAutoRun
    {
        private const string FlagPath = "Temp/RunLevel5Setup.flag";
        private const string ResultPath = "Temp/Level5SetupAutoRun.log";

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
                    Debug.Log("[Level5SetupAutoRun] Running Level5Setup.SetupLevel5()...");
                    Level5Setup.SetupLevel5();
                    MainMenuSetup.SetupMainMenu();
                    File.WriteAllText(ResultPath, "OK");
                    Debug.Log("[Level5SetupAutoRun] SetupLevel5 & MainMenu completed successfully!");
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
