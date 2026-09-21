#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrafficTown2D.Editor
{
    public static class Level3SetupAutoRun
    {
        private const string FlagPath = "Temp/RunLevel3Setup.flag";
        private const string ResultPath = "Temp/Level3SetupAutoRun.log";

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
                    Debug.Log("[Level3SetupAutoRun] Running Level3Setup.SetupLevel3()...");
                    Level3Setup.SetupLevel3();
                    File.WriteAllText(ResultPath, "OK");
                    Debug.Log("[Level3SetupAutoRun] SetupLevel3 completed successfully!");
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
