#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TrafficTown2D.Editor
{
    public static class BuildSettingsUtility
    {
        public static readonly string[] AllProjectScenePaths = new[]
        {
            "Assets/Scenes/SplashScreen.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Quiz.unity",
            "Assets/Scenes/Level1.unity",
            "Assets/Scenes/Level2.unity",
            "Assets/Scenes/Level3.unity",
            "Assets/Scenes/Level4.unity",
            "Assets/Scenes/Level5.unity",
            "Assets/Scenes/Level6.unity",
            "Assets/Scenes/Level7.unity",
            "Assets/Scenes/Level8.unity",
            "Assets/Scenes/Level9.unity",
            "Assets/Scenes/Level10.unity"
        };

        [MenuItem("TrafficTown/Sync Build Settings & Profiles")]
        public static void EnsureAllScenesInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenesList = new List<EditorBuildSettingsScene>();
            HashSet<string> addedPaths = new HashSet<string>();

            foreach (string requiredPath in AllProjectScenePaths)
            {
                if (File.Exists(requiredPath) && !addedPaths.Contains(requiredPath))
                {
                    scenesList.Add(new EditorBuildSettingsScene(requiredPath, true));
                    addedPaths.Add(requiredPath);
                }
            }

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing != null && File.Exists(existing.path) && !addedPaths.Contains(existing.path))
                {
                    scenesList.Add(new EditorBuildSettingsScene(existing.path, existing.enabled));
                    addedPaths.Add(existing.path);
                }
            }

            EditorBuildSettings.scenes = scenesList.ToArray();
            SyncUnity6BuildProfiles(scenesList.ToArray());
            Debug.Log("[BuildSettingsUtility] Synchronized build settings and profiles for all scenes.");
        }

        private static void SyncUnity6BuildProfiles(EditorBuildSettingsScene[] scenes)
        {
            try
            {
                Type buildProfileType = null;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly asm in assemblies)
                {
                    Type t = asm.GetType("UnityEditor.Build.Profile.BuildProfile");
                    if (t != null)
                    {
                        buildProfileType = t;
                        break;
                    }
                }

                if (buildProfileType != null)
                {
                    PropertyInfo activeProp = buildProfileType.GetProperty("activeBuildProfile", BindingFlags.Public | BindingFlags.Static);
                    if (activeProp != null)
                    {
                        object activeProfile = activeProp.GetValue(null);
                        if (activeProfile != null)
                        {
                            MethodInfo setScenes = activeProfile.GetType().GetMethod("SetScenes", BindingFlags.Public | BindingFlags.Instance);
                            if (setScenes != null)
                            {
                                setScenes.Invoke(activeProfile, new object[] { scenes });
                            }

                            MethodInfo saveMethod = activeProfile.GetType().GetMethod("Save", BindingFlags.Public | BindingFlags.Instance);
                            if (saveMethod != null)
                            {
                                saveMethod.Invoke(activeProfile, null);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildSettingsUtility] BuildProfile sync notice: {ex.Message}");
            }
        }
    }
}
#endif
