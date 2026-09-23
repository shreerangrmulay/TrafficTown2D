#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TrafficTown2D.UI;

namespace TrafficTown2D.Editor
{
    public static class SplashScreenSetup
    {
        public const string SplashScreenScenePath = "Assets/Scenes/SplashScreen.unity";
        private const string AutoRunSplashPrefKey = "TrafficTown_SplashScreen_Setup_v1";

        [InitializeOnLoadMethod]
        private static void AutoSetupOnce()
        {
            if (!EditorPrefs.GetBool(AutoRunSplashPrefKey, false))
            {
                EditorPrefs.SetBool(AutoRunSplashPrefKey, true);
                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication.isPlaying)
                    {
                        SetupSplashScreenScene();
                    }
                };
            }
        }

        [MenuItem("TrafficTown/Setup Splash Screen")]
        public static void SetupSplashScreenScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before running TrafficTown -> Setup Splash Screen.");
                return;
            }

            // Ensure logo sprite exists
            if (!File.Exists(UnityLogoAssetCreator.UnityLogoPath))
            {
                UnityLogoAssetCreator.GenerateLogoPng();
            }

            Scene splashScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(splashScene, SplashScreenScenePath);

            // 1. Camera
            Camera cam = EnsureCamera();

            // 2. EventSystem
            EnsureEventSystem();

            // 3. Canvas
            Canvas canvas = EnsureCanvas();

            // 4. Made with Unity Splash Overlay
            CreateSplashUI(canvas.transform);

            // 5. Build Settings registration
            BuildSettingsUtility.EnsureAllScenesInBuildSettings();

            EditorSceneManager.MarkSceneDirty(splashScene);
            EditorSceneManager.SaveScene(splashScene);
            Selection.activeGameObject = canvas.gameObject;
            Debug.Log("[SplashScreenSetup] 'Made with Unity' splash screen scene created successfully at: " + SplashScreenScenePath);
        }

        private static Camera EnsureCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.07f, 1f); // Sleek dark charcoal
            cam.clearFlags = CameraClearFlags.SolidColor;

            if (cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }

            return cam;
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        private static Canvas EnsureCanvas()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            return canvas;
        }

        private static void CreateSplashUI(Transform parent)
        {
            GameObject splashObj = new GameObject("MadeWithUnitySplashScreen", typeof(RectTransform));
            splashObj.transform.SetParent(parent, false);

            RectTransform rt = splashObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            Image bg = splashObj.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.07f, 1f);
            bg.raycastTarget = true;

            CanvasGroup cg = splashObj.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = true;

            // Content container
            GameObject contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(splashObj.transform, false);
            RectTransform contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.pivot = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = new Vector2(500f, 320f);
            contentRt.anchoredPosition = Vector2.zero;

            // Unity Logo Image
            GameObject logoObj = new GameObject("UnityLogo", typeof(RectTransform));
            logoObj.transform.SetParent(contentObj.transform, false);
            RectTransform logoRt = logoObj.GetComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0.5f, 0.5f);
            logoRt.anchorMax = new Vector2(0.5f, 0.5f);
            logoRt.pivot = new Vector2(0.5f, 0.5f);
            logoRt.sizeDelta = new Vector2(160f, 160f);
            logoRt.anchoredPosition = new Vector2(0f, 40f);

            Image logoImg = logoObj.AddComponent<Image>();
            Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(UnityLogoAssetCreator.UnityLogoPath);
            if (logoSprite != null)
            {
                logoImg.sprite = logoSprite;
            }
            logoImg.color = Color.white;
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;

            // "Made with Unity" Text
            GameObject textObj = new GameObject("MadeWithText", typeof(RectTransform));
            textObj.transform.SetParent(contentObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.5f, 0.5f);
            textRt.anchorMax = new Vector2(0.5f, 0.5f);
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.sizeDelta = new Vector2(480f, 50f);
            textRt.anchoredPosition = new Vector2(0f, -80f);

            Text txt = textObj.AddComponent<Text>();
            txt.text = "Made with Unity";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 32;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.96f, 0.96f, 0.96f, 1f);
            txt.raycastTarget = false;

            UnitySplashScreen splashComp = splashObj.AddComponent<UnitySplashScreen>();
            splashComp.Configure(cg, contentRt, logoImg, txt, 0.2f, 0.8f, 1.8f, 0.7f, true, false, true, "MainMenu");
        }
    }
}
#endif
