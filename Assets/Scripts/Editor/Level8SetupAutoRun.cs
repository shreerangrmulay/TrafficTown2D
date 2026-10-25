#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrafficTown2D.Editor
{
    public static class Level8SetupAutoRun
    {
        private const string FlagPath = "Temp/RunLevel8Setup.flag";
        private const string ResultPath = "Temp/Level8SetupAutoRun.log";

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
                    Debug.Log("[Level8SetupAutoRun] Running MainMenuSetup.SetupMainMenu()...");
                    MainMenuSetup.SetupMainMenu();
                    Debug.Log("[Level8SetupAutoRun] Running Level8Setup.SetupLevel8()...");
                    Level8Setup.SetupLevel8();
                    Debug.Log("[Level8SetupAutoRun] Running Level9Setup.SetupLevel9()...");
                    Level9Setup.SetupLevel9();
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
                    File.WriteAllText(ResultPath, "OK");
                    Debug.Log("[Level8SetupAutoRun] SetupMainMenu, Level8 & Level9 completed successfully!");
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
