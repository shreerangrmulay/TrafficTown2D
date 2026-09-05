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
        private static void SetupMainMenu()
        {
            Canvas canvas = FindOrCreateCanvas();
            EnsureEventSystem();
            SceneLoader sceneLoader = FindOrCreateSceneLoader();
            FindOrCreateGameManager();

            CreateBackground(canvas.transform);
            CreateTitle(canvas.transform);
            CreateSubtitle(canvas.transform);
            Text messageText = CreateMessage(canvas.transform);
            MainMenuController controller = FindOrCreateController(canvas.gameObject);
            AssignControllerReferences(controller, sceneLoader, messageText);
            CreateButtons(canvas.transform, controller);
            EnsureBuildSettingsScenes();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
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
            string[] requiredScenePaths = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Level1.unity", "Assets/Scenes/Level2.unity" };
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

        private static void AssignControllerReferences(MainMenuController controller, SceneLoader sceneLoader, Text messageText)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("sceneLoader").objectReferenceValue = sceneLoader;
            serializedController.FindProperty("messageText").objectReferenceValue = messageText;
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
            string[] names = { "PlayButton", "LearnButton", "QuizButton", "SettingsButton", "ExitButton" };
            string[] labels = { "PLAY", "LEARN", "QUIZ", "SETTINGS", "EXIT" };
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
                    UnityEventTools.AddPersistentListener(button.onClick, controller.Settings);
                    break;
                case 4:
                    UnityEventTools.AddPersistentListener(button.onClick, controller.Exit);
                    break;
            }
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
    }
}
#endif
