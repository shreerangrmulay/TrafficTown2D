// This file is excluded from player builds and runs only from the Unity Editor menu.
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TrafficTown2D.Core;
using TrafficTown2D.UI;

namespace TrafficTown2D.Editor
{
    public static class MainMenuSetup
    {
        private const string CanvasName = "Canvas";
        private const string EventSystemName = "EventSystem";
        private const string ServicesName = "MainMenu Services";
        private const float ButtonWidth = 300f;
        private const float ButtonHeight = 54f;
        private const float ButtonSpacing = 12f;

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

            EnsureCamera();
            EnsureEventSystem();
            SceneLoader sceneLoader = FindOrCreateSceneLoader();
            FindOrCreateGameManager();

            // Clean any gameplay/level objects if they got into MainMenu by accident
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

            CreateBackground(canvas.transform);
            CreateTitle(canvas.transform);
            CreateSubtitle(canvas.transform);
            Text messageText = CreateMessage(canvas.transform);
            MainMenuController controller = FindOrCreateController(canvas.gameObject);
            GameObject levelSelect = CreateLevelSelectModal(canvas.transform, controller);
            AssignControllerReferences(controller, sceneLoader, messageText, levelSelect);
            CreateButtons(canvas.transform, controller);
            EnsureBuildSettingsScenes();

            EditorSceneManager.MarkSceneDirty(mainMenuScene);
            EditorSceneManager.SaveScene(mainMenuScene);
            Selection.activeGameObject = canvas.gameObject;
            Debug.Log("TrafficTown main menu setup completed.");
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
            string[] requiredScenePaths = {
                "Assets/Scenes/MainMenu.unity",
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
            EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(currentScenes);

            foreach (string scenePath in requiredScenePaths)
            {
                if (!System.IO.File.Exists(scenePath))
                {
                    continue;
                }

                bool alreadyIncluded = scenes.Exists(scene => scene.path == scenePath);
                if (!alreadyIncluded)
                {
                    scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
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

        private static void AssignControllerReferences(MainMenuController controller, SceneLoader sceneLoader, Text messageText, GameObject levelSelect)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("sceneLoader").objectReferenceValue = sceneLoader;
            serializedController.FindProperty("messageText").objectReferenceValue = messageText;
            SerializedProperty lsp = serializedController.FindProperty("levelSelectPanel");
            if (lsp != null) lsp.objectReferenceValue = levelSelect;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateBackground(Transform canvasTransform)
        {
            GameObject background = FindOrCreateChild(canvasTransform, "Background");
            Image image = GetOrAdd<Image>(background);
            image.color = new Color(0.78f, 0.91f, 0.94f, 1f);
            SetFullScreen(background.GetComponent<RectTransform>());
            background.transform.SetAsFirstSibling();
        }

        private static void CreateTitle(Transform canvasTransform)
        {
            Text title = GetOrAdd<Text>(FindOrCreateChild(canvasTransform, "Title"));
            title.text = "TRAFFIC TOWN 2D";
            title.alignment = TextAnchor.MiddleCenter;
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 58;
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.08f, 0.24f, 0.32f, 1f);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(680f, 90f));
        }

        private static void CreateSubtitle(Transform canvasTransform)
        {
            Text subtitle = GetOrAdd<Text>(FindOrCreateChild(canvasTransform, "Subtitle"));
            subtitle.text = "Learn Traffic Rules. Stay Safe. Have Fun!";
            subtitle.alignment = TextAnchor.MiddleCenter;
            subtitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subtitle.fontSize = 24;
            subtitle.color = new Color(0.16f, 0.35f, 0.39f, 1f);
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(700f, 48f));
        }

        private static Text CreateMessage(Transform canvasTransform)
        {
            Text message = GetOrAdd<Text>(FindOrCreateChild(canvasTransform, "Message"));
            message.text = string.Empty;
            message.alignment = TextAnchor.MiddleCenter;
            message.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            message.fontSize = 18;
            message.color = new Color(0.08f, 0.24f, 0.32f, 1f);
            SetRect(message.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(700f, 42f));
            return message;
        }

        private static void CreateButtons(Transform canvasTransform, MainMenuController controller)
        {
            string[] names = { "PlayButton", "LearnButton", "QuizButton", "LevelSelectButton", "ExitButton" };
            string[] labels = { "PLAY", "LEARN", "QUIZ", "LEVEL SELECT", "EXIT" };
            float firstY = 218f;

            for (int index = 0; index < names.Length; index++)
            {
                GameObject buttonObject = FindOrCreateChild(canvasTransform, names[index]);
                Button button = GetOrAdd<Button>(buttonObject);
                Image image = GetOrAdd<Image>(buttonObject);
                image.color = index == 0 ? new Color(0.98f, 0.65f, 0.20f, 1f) : new Color(0.20f, 0.59f, 0.61f, 1f);
                Text label = GetOrAddChildText(buttonObject.transform, "Label");
                label.text = labels[index];
                label.alignment = TextAnchor.MiddleCenter;
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 22;
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
                SetFullScreen(label.rectTransform);
                RectTransform buttonRect = GetOrAdd<RectTransform>(buttonObject);
                SetRect(buttonRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -firstY - index * (ButtonHeight + ButtonSpacing)), new Vector2(ButtonWidth, ButtonHeight));
                WireButton(button, controller, index);
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
                    UnityEventTools.AddPersistentListener(button.onClick, controller.OpenLevelSelect);
                    break;
                case 4:
                    UnityEventTools.AddPersistentListener(button.onClick, controller.Exit);
                    break;
            }
        }

        private static GameObject CreateLevelSelectModal(Transform canvasTransform, MainMenuController controller)
        {
            GameObject panel = FindOrCreateChild(canvasTransform, "LevelSelectPanel");
            Image bg = GetOrAdd<Image>(panel);
            bg.color = new Color(0.06f, 0.10f, 0.16f, 0.96f);
            SetFullScreen(panel.GetComponent<RectTransform>());

            Text title = GetOrAdd<Text>(FindOrCreateChild(panel.transform, "Title"));
            title.text = "SELECT A LEVEL";
            title.alignment = TextAnchor.MiddleCenter;
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 32;
            title.fontStyle = FontStyle.Bold;
            title.color = Color.white;
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(500f, 50f));

            string[] levelNames = new[]
            {
                "Level 1: Safe Crossing",
                "Level 2: Smart Crossing",
                "Level 3: Yield to Pedestrians",
                "Level 4: Bike Lane Awareness",
                "Level 5: The Stop Sign",
                "Level 6: One-Way Streets",
                "Level 7: School Zones & Speed",
                "Level 8: Emergency Vehicles",
                "Level 9: Roundabouts",
                "Level 10: The Ultimate Commute"
            };

            float colWidth = 280f;
            float colHeight = 44f;
            float spacingY = 10f;
            float startY = -130f;

            for (int i = 0; i < 10; i++)
            {
                int col = i < 5 ? 0 : 1;
                int row = i % 5;
                float x = col == 0 ? -155f : 155f;
                float y = startY - row * (colHeight + spacingY);

                int levelNum = i + 1;
                GameObject btnObj = FindOrCreateChild(panel.transform, "LevelBtn_" + levelNum);
                Button btn = GetOrAdd<Button>(btnObj);
                Image img = GetOrAdd<Image>(btnObj);
                img.color = new Color(0.16f, 0.48f, 0.68f, 1f);

                Text lbl = GetOrAddChildText(btnObj.transform, "Label");
                lbl.text = levelNames[i];
                lbl.alignment = TextAnchor.MiddleCenter;
                lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lbl.fontSize = 15;
                lbl.fontStyle = FontStyle.Bold;
                lbl.color = Color.white;
                SetFullScreen(lbl.rectTransform);

                RectTransform r = GetOrAdd<RectTransform>(btnObj);
                SetRect(r, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(colWidth, colHeight));

                btn.onClick.RemoveAllListeners();
                switch (levelNum)
                {
                    case 1: UnityEventTools.AddPersistentListener(btn.onClick, controller.Play); break;
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

            GameObject closeObj = FindOrCreateChild(panel.transform, "CloseButton");
            Button closeBtn = GetOrAdd<Button>(closeObj);
            Image closeImg = GetOrAdd<Image>(closeObj);
            closeImg.color = new Color(0.72f, 0.28f, 0.28f, 1f);

            Text closeLbl = GetOrAddChildText(closeObj.transform, "Label");
            closeLbl.text = "BACK TO MENU";
            closeLbl.alignment = TextAnchor.MiddleCenter;
            closeLbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeLbl.fontSize = 16;
            closeLbl.fontStyle = FontStyle.Bold;
            closeLbl.color = Color.white;
            SetFullScreen(closeLbl.rectTransform);

            RectTransform closeR = GetOrAdd<RectTransform>(closeObj);
            SetRect(closeR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -430f), new Vector2(220f, 44f));

            closeBtn.onClick.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(closeBtn.onClick, controller.CloseLevelSelect);

            panel.SetActive(false);
            return panel;
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

        private static Text GetOrAddChildText(Transform parent, string childName)
        {
            return GetOrAdd<Text>(FindOrCreateChild(parent, childName));
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
            camera.backgroundColor = new Color(0.18f, 0.24f, 0.32f, 1f);
            camera.tag = "MainCamera";
            if (camera.GetComponent<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }
        }
    }
}
#endif
