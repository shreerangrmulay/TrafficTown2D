#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrafficTown2D.Editor
{
    public static class Level1SetupAutoRun
    {
        private const string FlagPath = "Temp/RunLevel1Setup.flag";
        private const string ResultPath = "Temp/Level1SetupAutoRun.log";

        [InitializeOnLoadMethod]
        private static void RunWhenFlagged()
        {
            if (!File.Exists(FlagPath))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(FlagPath))
                {
                    return;
                }

                try
                {
                    Level1Setup.SetupLevel1();
                    File.WriteAllText(ResultPath, "OK");
                }
                catch (System.Exception exception)
                {
                    File.WriteAllText(ResultPath, exception.ToString());
                    Debug.LogException(exception);
                }
                finally
                {
                    File.Delete(FlagPath);
                }
            };
        }
    }
}
#endif
