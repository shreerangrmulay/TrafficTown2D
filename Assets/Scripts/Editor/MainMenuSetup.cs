// This file is excluded from player builds and runs only from the Unity Editor menu.
#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using TrafficTown2D.Core;
using TrafficTown2D.UI;

namespace TrafficTown2D.Editor
{
    public static class MainMenuSetup
    {
        private const string CanvasName = "Canvas";
        private const string EventSystemName = "EventSystem";
        private const string ServicesName = "MainMenu Services";

        // Asset Paths
        private const string MainMenuBgPath = "Assets/UI/MainMenu/MainMenuBackground.png";
        private const string CardBasePath = "Assets/UI/MainMenu/Card_Rounded_Base.png";
        private const string ButtonBasePath = "Assets/UI/MainMenu/Button_Rounded_3D.png";
        private const string PillBadgePath = "Assets/UI/MainMenu/Pill_Badge.png";

        private const string IconPlayPath = "Assets/UI/MainMenu/Icons/Icon_Play.png";
        private const string IconBookPath = "Assets/UI/MainMenu/Icons/Icon_Book.png";
        private const string IconQuizPath = "Assets/UI/MainMenu/Icons/Icon_Quiz.png";
        private const string IconExitPath = "Assets/UI/MainMenu/Icons/Icon_Exit.png";
        private const string IconBackPath = "Assets/UI/MainMenu/Icons/Icon_Back.png";
        private const string IconStarPath = "Assets/UI/MainMenu/Icons/Icon_Star.png";
        private const string IconLockPath = "Assets/UI/MainMenu/Icons/Icon_Lock.png";

        [InitializeOnLoadMethod]
        private static void RegisterEditorExitHandler()
        {
            MainMenuController.ExitRequested -= StopPlayMode;
            MainMenuController.ExitRequested += StopPlayMode;
        }

        private static void StopPlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
        }

        private const string AutoRunSplashPrefKey = "TrafficTown_MainMenu_Splash_v2";

        [InitializeOnLoadMethod]
        private static void AutoSetupSplashOnce()
        {
            if (!EditorPrefs.GetBool(AutoRunSplashPrefKey, false))
            {
                EditorPrefs.SetBool(AutoRunSplashPrefKey, true);
                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication.isPlaying)
                    {
                        Debug.Log("[MainMenuSetup] Auto-running SetupMainMenu for modern UI setup...");
                        SetupMainMenu();
                    }
                };
            }
        }

        [MenuItem("TrafficTown/Setup Main Menu")]
        public static void SetupMainMenu()
        {
            const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) == null)
            {
                Debug.LogError("Main Menu scene was not found at " + MainMenuScenePath);
                return;
            }

            Scene mainMenuScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            if (!mainMenuScene.IsValid())
            {
                Debug.LogError("Could not open Main Menu scene at " + MainMenuScenePath);
                return;
            }

            // Ensure sprites exist before creating UI
            EnsureSpritesExist();

            EnsureCamera();
            EnsureEventSystem();
            SceneLoader sceneLoader = FindOrCreateSceneLoader();
            FindOrCreateGameManager();

            // Clean any unwanted gameplay/level objects if they got into MainMenu by accident
            string[] unwantedObjects = { "Environment", "Traffic", "Gameplay", "Player", "Pedestrians", "PlayerCar" };
            foreach (GameObject rootObject in mainMenuScene.GetRootGameObjects())
            {
                for (int i = 0; i < unwantedObjects.Length; i++)
                {
                    if (rootObject.name == unwantedObjects[i])
                    {
                        Object.DestroyImmediate(rootObject);
                        break;
                    }
                }
            }

            Canvas canvas = FindOrCreateCanvas();
            ClearChildren(canvas.transform);

            // 1. Fullscreen Environment Background + Soft Contrast Overlay
            CreateBackground(canvas.transform);

            // 2. Main Menu Content Root
            GameObject mainMenuContent = FindOrCreateChild(canvas.transform, "MainMenuContent");
            SetFullScreen(mainMenuContent.GetComponent<RectTransform>());

            // 3. Decorative Corner Widgets (Traffic light, road safety badge)
            CreateDecorations(mainMenuContent.transform);

            // 4. Polished Game Title & Subtitle
            CreateTitleAndSubtitle(mainMenuContent.transform);

            // 5. Message Text (TMP)
            TMP_Text messageText = CreateMessage(mainMenuContent.transform);

            // 6. Main Menu Buttons (Play, Learn, Quiz, Exit)
            MainMenuController controller = FindOrCreateController(canvas.gameObject);
            CreateButtons(mainMenuContent.transform, controller);

            // 7. Level Selection Modal (10 Level Cards Grid)
            GameObject levelSelect = CreateLevelSelectModal(canvas.transform, controller);
            levelSelect.transform.SetAsLastSibling();

            // 8. Made with Unity Splash Screen
            GameObject splash = CreateMadeWithUnitySplashScreen(canvas.transform);
            splash.transform.SetAsLastSibling();

            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = true;

            AssignControllerReferences(controller, sceneLoader, messageText, levelSelect, mainMenuContent);
            EnsureBuildSettingsScenes();

            EditorSceneManager.MarkSceneDirty(mainMenuScene);
            EditorSceneManager.SaveScene(mainMenuScene);
            Selection.activeGameObject = canvas.gameObject;
            Debug.Log("[MainMenuSetup] TrafficTown modern UI redesign setup completed successfully!");
        }

        private static void EnsureSpritesExist()
        {
            if (!File.Exists(CardBasePath) || !File.Exists(ButtonBasePath) || !File.Exists(IconPlayPath))
            {
                MainMenuAssetGenerator.GenerateAllSprites();
            }
        }

        private static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject(CanvasName);
                canvas = canvasObject.AddComponent<Canvas>();
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            canvas.name = CanvasName;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject(EventSystemName);
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null &&
                eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        private static void EnsureBuildSettingsScenes()
        {
            BuildSettingsUtility.EnsureAllScenesInBuildSettings();
        }

        private static SceneLoader FindOrCreateSceneLoader()
        {
            SceneLoader sceneLoader = Object.FindAnyObjectByType<SceneLoader>();
            if (sceneLoader != null)
            {
                return sceneLoader;
            }

            GameObject services = FindOrCreateServicesObject();
            return services.AddComponent<SceneLoader>();
        }

        private static GameManager FindOrCreateGameManager()
        {
            GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
            if (gameManager != null)
            {
                return gameManager;
            }

            GameObject services = FindOrCreateServicesObject();
            return services.AddComponent<GameManager>();
        }

        private static GameObject FindOrCreateServicesObject()
        {
            GameObject services = GameObject.Find(ServicesName);
            if (services == null)
            {
                services = new GameObject(ServicesName);
            }

            return services;
        }

        private static MainMenuController FindOrCreateController(GameObject canvasObject)
        {
            MainMenuController controller = Object.FindAnyObjectByType<MainMenuController>();
            if (controller != null)
            {
                return controller;
            }

            return canvasObject.AddComponent<MainMenuController>();
        }

        private static void AssignControllerReferences(MainMenuController controller, SceneLoader sceneLoader, TMP_Text messageText, GameObject levelSelect, GameObject mainMenuContent)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("sceneLoader").objectReferenceValue = sceneLoader;
            SerializedProperty msgTmp = serializedController.FindProperty("messageTextTmp");
            if (msgTmp != null) msgTmp.objectReferenceValue = messageText;
            SerializedProperty lsp = serializedController.FindProperty("levelSelectPanel");
            if (lsp != null) lsp.objectReferenceValue = levelSelect;
            SerializedProperty mmc = serializedController.FindProperty("mainMenuContent");
            if (mmc != null) mmc.objectReferenceValue = mainMenuContent;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateBackground(Transform canvasTransform)
        {
            GameObject background = FindOrCreateChild(canvasTransform, "Background");
            Image image = GetOrAdd<Image>(background);

            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuBgPath);
            if (bgSprite != null)
            {
                image.sprite = bgSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.18f, 0.45f, 0.65f, 1f);
            }
            SetFullScreen(background.GetComponent<RectTransform>());
            background.transform.SetAsFirstSibling();

            // Soft contrast overlay to ensure text and buttons stand out crisply
            GameObject overlay = FindOrCreateChild(canvasTransform, "BackgroundOverlay");
            Image overlayImg = GetOrAdd<Image>(overlay);
            overlayImg.color = new Color(0.04f, 0.08f, 0.14f, 0.42f);
            SetFullScreen(overlay.GetComponent<RectTransform>());
            overlay.transform.SetSiblingIndex(1);
        }

        private static void CreateDecorations(Transform parent)
        {
            GameObject decoRoot = FindOrCreateChild(parent, "Decorations");
            SetFullScreen(decoRoot.GetComponent<RectTransform>());

            // 1. Top-Left: Decorative Traffic Light
            GameObject tlObj = FindOrCreateChild(decoRoot.transform, "TrafficLightWidget");
            RectTransform tlRect = GetOrAdd<RectTransform>(tlObj);
            SetRect(tlRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -80f), new Vector2(44f, 110f));

            Image tlBg = GetOrAdd<Image>(tlObj);
            tlBg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonBasePath) ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            tlBg.type = Image.Type.Sliced;
            tlBg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

            Color[] lightColors = {
                new Color(1f, 0.22f, 0.20f, 1f), // Red
                new Color(1f, 0.82f, 0.15f, 1f), // Yellow
                new Color(0.20f, 0.85f, 0.35f, 1f) // Green
            };
            float[] lightY = { 32f, 0f, -32f };
            for (int i = 0; i < 3; i++)
            {
                GameObject lens = FindOrCreateChild(tlObj.transform, "Lens_" + i);
                RectTransform lr = GetOrAdd<RectTransform>(lens);
                SetRect(lr, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, lightY[i]), new Vector2(24f, 24f));
                Image lensImg = GetOrAdd<Image>(lens);
                lensImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/TrafficLightLens.png") ??
                                 AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/MainMenu/Pill_Badge.png");
                lensImg.color = lightColors[i];
            }

            // 2. Top-Right: Safety Badge
            GameObject badgeObj = FindOrCreateChild(decoRoot.transform, "SafetyBadge");
            RectTransform badgeRect = GetOrAdd<RectTransform>(badgeObj);
            SetRect(badgeRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-70f, -60f), new Vector2(110f, 40f));

            Image badgeImg = GetOrAdd<Image>(badgeObj);
            badgeImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PillBadgePath) ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = new Color(0.95f, 0.72f, 0.18f, 0.95f);

            TMP_Text badgeText = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(badgeObj.transform, "Text"));
            badgeText.text = "SAFETY FIRST!";
            badgeText.alignment = TextAlignmentOptions.Center;
            badgeText.font = GetTMPFont();
            badgeText.fontSize = 11;
            badgeText.fontStyle = FontStyles.Bold;
            badgeText.color = new Color(0.12f, 0.16f, 0.22f, 1f);
            SetFullScreen(badgeText.rectTransform);
        }

        private static void CreateTitleAndSubtitle(Transform parent)
        {
            GameObject titleContainer = FindOrCreateChild(parent, "TitleContainer");
            RectTransform tcRect = GetOrAdd<RectTransform>(titleContainer);
            SetRect(tcRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -115f), new Vector2(760f, 130f));

            // Category Pill: "★ ROAD SAFETY ACADEMY ★"
            GameObject pillObj = FindOrCreateChild(titleContainer.transform, "CategoryPill");
            RectTransform pillRect = GetOrAdd<RectTransform>(pillObj);
            SetRect(pillRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 15f), new Vector2(250f, 28f));

            Image pillImg = GetOrAdd<Image>(pillObj);
            pillImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PillBadgePath) ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            pillImg.type = Image.Type.Sliced;
            pillImg.color = new Color(0.98f, 0.65f, 0.18f, 1f);

            TMP_Text pillText = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(pillObj.transform, "Label"));
            pillText.text = "★ ROAD SAFETY ACADEMY ★";
            pillText.alignment = TextAlignmentOptions.Center;
            pillText.font = GetTMPFont();
            pillText.fontSize = 12;
            pillText.fontStyle = FontStyles.Bold;
            pillText.color = new Color(0.10f, 0.14f, 0.20f, 1f);
            SetFullScreen(pillText.rectTransform);

            // Title: "TRAFFIC TOWN 2D"
            TMP_Text title = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(titleContainer.transform, "Title"));
            title.text = "TRAFFIC TOWN 2D";
            title.alignment = TextAlignmentOptions.Center;
            title.font = GetTMPFont();
            title.fontSize = 56;
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(
                Color.white, Color.white,
                new Color(1f, 0.92f, 0.55f, 1f), new Color(1f, 0.85f, 0.35f, 1f)
            );
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(740f, 65f));

            // Subtitle: "Learn Traffic Rules. Stay Safe. Have Fun!"
            TMP_Text subtitle = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(titleContainer.transform, "Subtitle"));
            subtitle.text = "Learn Traffic Rules. Stay Safe. Have Fun!";
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.font = GetTMPFont();
            subtitle.fontSize = 20;
            subtitle.fontStyle = FontStyles.Normal;
            subtitle.color = new Color(0.85f, 0.94f, 1f, 0.95f);
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -34f), new Vector2(700f, 32f));
        }

        private static TMP_Text CreateMessage(Transform parent)
        {
            TMP_Text message = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(parent, "Message"));
            message.text = string.Empty;
            message.alignment = TextAlignmentOptions.Center;
            message.font = GetTMPFont();
            message.fontSize = 16;
            message.color = new Color(0.85f, 0.92f, 1f, 1f);
            SetRect(message.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(700f, 30f));
            return message;
        }

        private static void CreateButtons(Transform parent, MainMenuController controller)
        {
            GameObject btnContainer = FindOrCreateChild(parent, "ButtonContainer");
            RectTransform bcRect = GetOrAdd<RectTransform>(btnContainer);
            SetRect(bcRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -65f), new Vector2(400f, 320f));

            string[] names = { "PlayButton", "LearnButton", "QuizButton", "ExitButton" };
            string[] labels = { "PLAY", "LEARN", "ROAD SAFETY QUIZ", "EXIT" };
            string[] iconPaths = { IconPlayPath, IconBookPath, IconQuizPath, IconExitPath };

            Color[] normalColors = {
                new Color(1f, 0.62f, 0.12f, 1f),   // Golden Amber Orange
                new Color(0.12f, 0.65f, 0.62f, 1f), // Vibrant Teal
                new Color(0.18f, 0.70f, 0.36f, 1f), // Emerald Green
                new Color(0.82f, 0.24f, 0.22f, 1f)  // Coral Red
            };
            Color[] hoverColors = {
                new Color(1f, 0.72f, 0.22f, 1f),
                new Color(0.16f, 0.75f, 0.72f, 1f),
                new Color(0.22f, 0.80f, 0.42f, 1f),
                new Color(0.90f, 0.30f, 0.28f, 1f)
            };
            Color[] pressedColors = {
                new Color(0.85f, 0.50f, 0.08f, 1f),
                new Color(0.08f, 0.52f, 0.50f, 1f),
                new Color(0.12f, 0.58f, 0.28f, 1f),
                new Color(0.68f, 0.18f, 0.16f, 1f)
            };

            float[] btnY = { 95f, 25f, -45f, -115f };
            float[] btnHeights = { 66f, 56f, 56f, 54f };
            float[] btnWidths = { 340f, 310f, 310f, 310f };
            float[] fontSizes = { 24f, 20f, 18f, 18f };

            Sprite btnBaseSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonBasePath) ??
                                   AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            for (int i = 0; i < names.Length; i++)
            {
                GameObject btnObj = FindOrCreateChild(btnContainer.transform, names[i]);
                Button button = GetOrAdd<Button>(btnObj);
                Image img = GetOrAdd<Image>(btnObj);
                img.sprite = btnBaseSprite;
                img.type = Image.Type.Sliced;
                img.color = normalColors[i];

                RectTransform rect = GetOrAdd<RectTransform>(btnObj);
                SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, btnY[i]), new Vector2(btnWidths[i], btnHeights[i]));

                // Icon on the left
                GameObject iconObj = FindOrCreateChild(btnObj.transform, "Icon");
                RectTransform iconRect = GetOrAdd<RectTransform>(iconObj);
                float iconSize = (i == 0) ? 26f : 22f;
                float iconX = -(btnWidths[i] * 0.5f) + 36f;
                SetRect(iconRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(iconX, 0f), new Vector2(iconSize, iconSize));
                Image iconImg = GetOrAdd<Image>(iconObj);
                Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPaths[i]);
                if (iconSprite != null)
                {
                    iconImg.sprite = iconSprite;
                    iconImg.color = Color.white;
                    iconImg.raycastTarget = false;
                }
                else
                {
                    iconObj.SetActive(false);
                }

                // Label Text
                TMP_Text label = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(btnObj.transform, "Label"));
                label.text = labels[i];
                label.alignment = TextAlignmentOptions.Center;
                label.font = GetTMPFont();
                label.fontSize = fontSizes[i];
                label.fontStyle = FontStyles.Bold;
                label.color = Color.white;
                label.raycastTarget = false;
                SetFullScreen(label.rectTransform);

                // Attach AnimatedUIButton for juicy micro-interactions
                AnimatedUIButton anim = GetOrAdd<AnimatedUIButton>(btnObj);
                float hoverScale = (i == 0) ? 1.05f : 1.04f;
                anim.Configure(normalColors[i], hoverColors[i], pressedColors[i], hoverScale, 0.95f, img);

                WireButton(button, controller, i);
            }
        }

        private static void WireButton(Button button, MainMenuController controller, int buttonIndex)
        {
            button.onClick.RemoveAllListeners();
            switch (buttonIndex)
            {
                case 0:
                    UnityEventTools.AddPersistentListener(button.onClick, controller.Play);
                    break;
                case 1:
                    UnityEventTools.AddPersistentListener(button.onClick, controller.Learn);
                    break;
                case 2:
                    UnityEventTools.AddPersistentListener(button.onClick, controller.Quiz);
                    break;
                case 3:
                    UnityEventTools.AddPersistentListener(button.onClick, controller.Exit);
                    break;
            }
        }

        private static GameObject CreateLevelSelectModal(Transform canvasTransform, MainMenuController controller)
        {
            GameObject panel = FindOrCreateChild(canvasTransform, "LevelSelectPanel");
            Image bg = GetOrAdd<Image>(panel);
            bg.color = new Color(0.05f, 0.08f, 0.14f, 0.94f);
            SetFullScreen(panel.GetComponent<RectTransform>());

            // Modal Header Container
            GameObject header = FindOrCreateChild(panel.transform, "Header");
            RectTransform hr = GetOrAdd<RectTransform>(header);
            SetRect(hr, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(800f, 80f));

            TMP_Text title = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(header.transform, "Title"));
            title.text = "SELECT LEVEL";
            title.alignment = TextAlignmentOptions.Center;
            title.font = GetTMPFont();
            title.fontSize = 38;
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(
                Color.white, Color.white,
                new Color(1f, 0.88f, 0.35f, 1f), new Color(1f, 0.82f, 0.25f, 1f)
            );
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(600f, 44f));

            TMP_Text subtitle = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(header.transform, "Subtitle"));
            subtitle.text = "Choose a level and start your road safety journey!";
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.font = GetTMPFont();
            subtitle.fontSize = 16;
            subtitle.color = new Color(0.72f, 0.85f, 0.95f, 1f);
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(600f, 26f));

            string[] levelTitles = new[]
            {
                "Safe Crossing",
                "Smart Crossing",
                "Yield Right",
                "Night Driving",
                "Traffic Controller",
                "Extreme Roads",
                "School Zone",
                "Emergency",
                "Roundabout",
                "Final Commute"
            };

            // Thematic accent colors per level
            Color[] accentColors = new[]
            {
                new Color(0.18f, 0.80f, 0.44f, 1f), // 1. Green
                new Color(0.95f, 0.77f, 0.12f, 1f), // 2. Yellow
                new Color(0.92f, 0.52f, 0.15f, 1f), // 3. Orange
                new Color(0.20f, 0.60f, 0.88f, 1f), // 4. Blue
                new Color(0.90f, 0.30f, 0.25f, 1f), // 5. Red
                new Color(0.12f, 0.78f, 0.72f, 1f), // 6. Cyan
                new Color(0.18f, 0.72f, 0.38f, 1f), // 7. Emerald
                new Color(0.85f, 0.22f, 0.25f, 1f), // 8. Crimson
                new Color(0.62f, 0.36f, 0.82f, 1f), // 9. Purple
                new Color(0.96f, 0.68f, 0.12f, 1f)  // 10. Gold
            };

            float cardWidth = 190f;
            float cardHeight = 142f;
            float[] colPositions = { -420f, -210f, 0f, 210f, 420f };
            float[] rowPositions = { 40f, -118f };

            Sprite cardBaseSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardBasePath) ??
                                    AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite pillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PillBadgePath) ??
                                AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite starSprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconStarPath);

            // Container for Cards Grid
            GameObject grid = FindOrCreateChild(panel.transform, "GridContainer");
            RectTransform gridRect = GetOrAdd<RectTransform>(grid);
            SetFullScreen(gridRect);

            for (int i = 0; i < 10; i++)
            {
                int col = i % 5;
                int row = i / 5;
                float x = colPositions[col];
                float y = rowPositions[row];
                int levelNum = i + 1;

                GameObject btnObj = FindOrCreateChild(grid.transform, "LevelBtn_" + levelNum);
                Button btn = GetOrAdd<Button>(btnObj);
                Image cardBg = GetOrAdd<Image>(btnObj);
                cardBg.sprite = cardBaseSprite;
                cardBg.type = Image.Type.Sliced;
                cardBg.color = new Color(0.10f, 0.15f, 0.24f, 0.95f);

                RectTransform r = GetOrAdd<RectTransform>(btnObj);
                SetRect(r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(cardWidth, cardHeight));

                // Colored Accent Top/Border Strip
                GameObject borderObj = FindOrCreateChild(btnObj.transform, "AccentBorder");
                RectTransform borderRect = GetOrAdd<RectTransform>(borderObj);
                SetRect(borderRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -3f), new Vector2(0f, 6f));
                Image borderImg = GetOrAdd<Image>(borderObj);
                borderImg.sprite = pillSprite;
                borderImg.type = Image.Type.Sliced;
                borderImg.color = accentColors[i];

                // Level Thumbnail (Upper 60%)
                GameObject thumbObj = FindOrCreateChild(btnObj.transform, "Thumbnail");
                RectTransform thumbRect = GetOrAdd<RectTransform>(thumbObj);
                SetRect(thumbRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(cardWidth - 8f, 76f));
                Image thumbImg = GetOrAdd<Image>(thumbObj);
                string thumbPath = $"Assets/UI/MainMenu/Thumbnails/Thumbnail_Level{levelNum}.png";
                Sprite thumbSprite = AssetDatabase.LoadAssetAtPath<Sprite>(thumbPath);
                if (thumbSprite != null)
                {
                    thumbImg.sprite = thumbSprite;
                    thumbImg.type = Image.Type.Simple;
                    thumbImg.preserveAspect = false;
                    thumbImg.color = Color.white;
                }
                else
                {
                    thumbImg.color = new Color(accentColors[i].r * 0.35f, accentColors[i].g * 0.35f, accentColors[i].b * 0.35f, 1f);
                }

                // Level Number Badge (Floating Pill on top-left of card)
                GameObject numPillObj = FindOrCreateChild(btnObj.transform, "NumberBadge");
                RectTransform npRect = GetOrAdd<RectTransform>(numPillObj);
                SetRect(npRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -18f), new Vector2(38f, 22f));
                Image npImg = GetOrAdd<Image>(numPillObj);
                npImg.sprite = pillSprite;
                npImg.type = Image.Type.Sliced;
                npImg.color = accentColors[i];

                TMP_Text numText = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(numPillObj.transform, "Text"));
                numText.text = levelNum < 10 ? $"0{levelNum}" : levelNum.ToString();
                numText.alignment = TextAlignmentOptions.Center;
                numText.font = GetTMPFont();
                numText.fontSize = 12;
                numText.fontStyle = FontStyles.Bold;
                numText.color = new Color(0.08f, 0.12f, 0.18f, 1f);
                SetFullScreen(numText.rectTransform);

                // Lower Info Card Footer
                GameObject footerObj = FindOrCreateChild(btnObj.transform, "Footer");
                RectTransform footerRect = GetOrAdd<RectTransform>(footerObj);
                SetRect(footerRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 28f), new Vector2(0f, 56f));
                Image footerImg = GetOrAdd<Image>(footerObj);
                footerImg.color = new Color(0.06f, 0.10f, 0.16f, 0.85f);

                // Level Title
                TMP_Text titleText = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(footerObj.transform, "Label"));
                titleText.text = levelTitles[i];
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.font = GetTMPFont();
                titleText.fontSize = 13;
                titleText.fontStyle = FontStyles.Bold;
                titleText.color = Color.white;
                SetRect(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(cardWidth - 12f, 24f));

                // Star Rating Row (Visualizes progression / PlayerPrefs stars if completed)
                GameObject starsObj = FindOrCreateChild(footerObj.transform, "StarsRow");
                RectTransform starsRect = GetOrAdd<RectTransform>(starsObj);
                SetRect(starsRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(60f, 14f));

                int stars = PlayerPrefs.GetInt($"Level_{levelNum}_Stars", 0);
                bool isCompleted = PlayerPrefs.GetInt($"Level_{levelNum}_Completed", 0) == 1 || stars > 0;
                float[] starX = { -16f, 0f, 16f };

                for (int s = 0; s < 3; s++)
                {
                    GameObject starObj = FindOrCreateChild(starsObj.transform, "Star_" + s);
                    RectTransform sr = GetOrAdd<RectTransform>(starObj);
                    SetRect(sr, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(starX[s], 0f), new Vector2(12f, 12f));
                    Image sImg = GetOrAdd<Image>(starObj);
                    if (starSprite != null) sImg.sprite = starSprite;
                    sImg.color = (s < stars || (isCompleted && stars == 0 && s == 0))
                        ? new Color(1f, 0.82f, 0.15f, 1f)
                        : new Color(1f, 1f, 1f, 0.25f);
                }

                // Attach AnimatedUIButton for juicy card hover
                AnimatedUIButton anim = GetOrAdd<AnimatedUIButton>(btnObj);
                Color normalBg = new Color(0.10f, 0.15f, 0.24f, 0.95f);
                Color hoverBg = new Color(0.14f, 0.22f, 0.35f, 1f);
                Color pressedBg = new Color(0.08f, 0.12f, 0.20f, 1f);
                anim.Configure(normalBg, hoverBg, pressedBg, 1.04f, 0.97f, cardBg);

                btn.onClick.RemoveAllListeners();
                switch (levelNum)
                {
                    case 1: UnityEventTools.AddPersistentListener(btn.onClick, controller.PlayLevel1); break;
                    case 2: UnityEventTools.AddPersistentListener(btn.onClick, controller.PlayLevel2); break;
                    case 3: UnityEventTools.AddPersistentListener(btn.onClick, controller.PlayLevel3); break;
                    case 4: UnityEventTools.AddPersistentListener(btn.onClick, controller.PlayLevel4); break;
                    case 5: UnityEventTools.AddPersistentListener(btn.onClick, controller.PlayLevel5); break;
                    case 6: UnityEventTools.AddPersistentListener(btn.onClick, controller.PlayLevel6); break;
                    case 7: UnityEventTools.AddPersistentListener(btn.onClick, controller.PlayLevel7); break;
                    case 8: UnityEventTools.AddPersistentListener(btn.onClick, controller.PlayLevel8); break;
                    case 9: UnityEventTools.AddPersistentListener(btn.onClick, controller.PlayLevel9); break;
                    case 10: UnityEventTools.AddPersistentListener(btn.onClick, controller.PlayLevel10); break;
                }
            }

            // Close / "BACK TO MENU" Button
            GameObject closeObj = FindOrCreateChild(panel.transform, "CloseButton");
            Button closeBtn = GetOrAdd<Button>(closeObj);
            Image closeImg = GetOrAdd<Image>(closeObj);
            closeImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonBasePath) ??
                             AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            closeImg.type = Image.Type.Sliced;
            Color closeNormal = new Color(0.16f, 0.24f, 0.34f, 1f);
            Color closeHover = new Color(0.22f, 0.32f, 0.44f, 1f);
            Color closePressed = new Color(0.12f, 0.18f, 0.26f, 1f);
            closeImg.color = closeNormal;

            RectTransform closeR = GetOrAdd<RectTransform>(closeObj);
            SetRect(closeR, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -235f), new Vector2(240f, 46f));

            // Back Arrow Icon
            GameObject backIconObj = FindOrCreateChild(closeObj.transform, "BackIcon");
            RectTransform biRect = GetOrAdd<RectTransform>(backIconObj);
            SetRect(biRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-80f, 0f), new Vector2(18f, 18f));
            Image biImg = GetOrAdd<Image>(backIconObj);
            Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconBackPath);
            if (backSprite != null)
            {
                biImg.sprite = backSprite;
                biImg.color = Color.white;
                biImg.raycastTarget = false;
            }
            else
            {
                backIconObj.SetActive(false);
            }

            TMP_Text closeLbl = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(closeObj.transform, "Label"));
            closeLbl.text = "BACK TO MENU";
            closeLbl.alignment = TextAlignmentOptions.Center;
            closeLbl.font = GetTMPFont();
            closeLbl.fontSize = 16;
            closeLbl.fontStyle = FontStyles.Bold;
            closeLbl.color = Color.white;
            SetFullScreen(closeLbl.rectTransform);

            AnimatedUIButton closeAnim = GetOrAdd<AnimatedUIButton>(closeObj);
            closeAnim.Configure(closeNormal, closeHover, closePressed, 1.04f, 0.96f, closeImg);

            closeBtn.onClick.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(closeBtn.onClick, controller.CloseLevelSelect);

            panel.SetActive(false);
            return panel;
        }

        private static TMP_FontAsset GetTMPFont()
        {
            return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ??
                   TMP_Settings.defaultFontAsset;
        }

        private static GameObject FindOrCreateChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child.gameObject;
            }

            GameObject childObject = new GameObject(childName, typeof(RectTransform));
            childObject.transform.SetParent(parent, false);
            return childObject;
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void SetFullScreen(RectTransform rectTransform)
        {
            SetRect(rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static void EnsureCamera()
        {
            Camera camera = Object.FindAnyObjectByType<Camera>();
            if (camera == null) camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.12f, 0.16f, 0.24f, 1f);
            camera.tag = "MainCamera";
            if (camera.GetComponent<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }
        }

        private static GameObject CreateMadeWithUnitySplashScreen(Transform parent)
        {
            if (!File.Exists(UnityLogoAssetCreator.UnityLogoPath))
            {
                UnityLogoAssetCreator.GenerateLogoPng();
            }

            GameObject splashObj = FindOrCreateChild(parent, "MadeWithUnitySplashScreen");
            RectTransform splashRect = GetOrAdd<RectTransform>(splashObj);
            SetFullScreen(splashRect);

            Image bg = GetOrAdd<Image>(splashObj);
            bg.color = new Color(0.04f, 0.05f, 0.07f, 1f);
            bg.raycastTarget = true;

            CanvasGroup cg = GetOrAdd<CanvasGroup>(splashObj);
            cg.alpha = 0f;
            cg.blocksRaycasts = true;

            GameObject content = FindOrCreateChild(splashObj.transform, "Content");
            RectTransform contentRect = GetOrAdd<RectTransform>(content);
            SetRect(contentRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 320f));

            GameObject logoObj = FindOrCreateChild(content.transform, "UnityLogo");
            RectTransform logoRect = GetOrAdd<RectTransform>(logoObj);
            SetRect(logoRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(160f, 160f));

            Image logoImg = GetOrAdd<Image>(logoObj);
            Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(UnityLogoAssetCreator.UnityLogoPath);
            if (logoSprite != null)
            {
                logoImg.sprite = logoSprite;
            }
            logoImg.color = Color.white;
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;

            GameObject textObj = FindOrCreateChild(content.transform, "MadeWithText");
            RectTransform textRect = GetOrAdd<RectTransform>(textObj);
            SetRect(textRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(480f, 50f));

            Text txt = GetOrAdd<Text>(textObj);
            txt.text = "Made with Unity";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 32;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.96f, 0.96f, 0.96f, 1f);
            txt.raycastTarget = false;

            UnitySplashScreen splashComp = GetOrAdd<UnitySplashScreen>(splashObj);
            splashComp.Configure(cg, contentRect, logoImg, txt, 0.2f, 0.8f, 1.8f, 0.7f, true, true, false, "MainMenu");

            return splashObj;
        }
    }
}
#endif
