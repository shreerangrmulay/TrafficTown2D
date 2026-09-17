#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using TrafficTown2D.Core;
using TrafficTown2D.Gameplay;
using TrafficTown2D.Player;
using TrafficTown2D.Traffic;
using TrafficTown2D.UI;

namespace TrafficTown2D.Editor
{
    /// <summary>Shared helpers for Level 4–10 setup scripts.</summary>
    public static class LevelSetupHelpers
    {
        private const string WorldSquareSpritePath = "Assets/Sprites/Generated/WorldSquare.png";
        private const string WorldCircleSpritePath = "Assets/Sprites/Generated/WorldCircle.png";
        private const string RoundedPanelSpritePath = "Assets/UI/RoundedPanel.png";
        private const string CarBluePrefabPath = "Assets/Prefabs/CarBlue.prefab";
        private const string CarRedPrefabPath = "Assets/Prefabs/CarRed.prefab";
        private const string CarYellowPrefabPath = "Assets/Prefabs/CarYellow.prefab";

        // ─── Scene Infrastructure ──────────────────────────────────────────
        public static void EnsureCamera()
        {
            Camera cam = Camera.main ?? new GameObject("Main Camera").AddComponent<Camera>();
            cam.gameObject.tag = "MainCamera";
            cam.orthographic = true; cam.orthographicSize = 4.8f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.48f, 0.68f, 0.84f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            GetOrAdd<AudioListener>(cam.gameObject);
            GetOrAdd<UniversalAdditionalCameraData>(cam.gameObject).renderShadows = false;
        }

        public static void EnsureGlobalLight()
        {
            Light2D light = Object.FindAnyObjectByType<Light2D>();
            if (light == null) light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global; light.color = Color.white; light.intensity = 1f;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        public static void EnsureAssetFolders()
        {
            foreach (string f in new[] { "Assets/Scenes", "Assets/Prefabs", "Assets/Sprites", "Assets/Sprites/Generated", "Assets/UI" })
                if (!Directory.Exists(f)) Directory.CreateDirectory(f);
        }

        public static void AddSceneToBuild(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == scenePath) && File.Exists(scenePath))
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ─── GameObject Helpers ────────────────────────────────────────────
        public static GameObject FindOrCreate(string name)
        {
            GameObject go = GameObject.Find(name);
            return go != null ? go : new GameObject(name);
        }

        public static GameObject FindOrCreateChild(Transform parent, string childName)
        {
            Transform t = parent.Find(childName);
            if (t != null) return t.gameObject;
            GameObject go = parent is RectTransform ? new GameObject(childName, typeof(RectTransform)) : new GameObject(childName);
            go.transform.SetParent(parent, false);
            return go;
        }

        public static T GetOrAdd<T>(GameObject go) where T : Component
        {
            if (!go.TryGetComponent<T>(out var c)) c = go.AddComponent<T>();
            return c;
        }

        // ─── Sprites ──────────────────────────────────────────────────────
        public static GameObject CreateSprite(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int order, bool circle)
        {
            GameObject go = FindOrCreateChild(parent, name);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = EnsureSprite(circle ? WorldCircleSpritePath : WorldSquareSpritePath, circle);
            sr.color = color; sr.sortingOrder = order; return go;
        }

        public static Sprite EnsureSprite(string path, bool circle)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;
            const int sz = 128; string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[sz * sz];
            Color32 w = new Color32(255, 255, 255, 255);
            Color32 cl = new Color32(255, 255, 255, 0);
            float c = (sz - 1) * 0.5f; float rSq = (sz * 0.46f) * (sz * 0.46f);
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                { float dx = x - c, dy = y - c; px[y * sz + x] = (!circle || dx * dx + dy * dy <= rSq) ? w : cl; }
            tex.SetPixels32(px); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ─── Player Car ───────────────────────────────────────────────────
        public static GameObject CreatePlayerCar(Vector3 position)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarBluePrefabPath);
            GameObject car;
            if (prefab != null)
            {
                car = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                car.name = "PlayerCar";
                VehicleController vc = car.GetComponent<VehicleController>();
                if (vc != null) Object.DestroyImmediate(vc, true);
            }
            else
            {
                car = new GameObject("PlayerCar");
                CreateSprite(car.transform, "CarBody", Vector3.zero, new Vector3(1.6f, 0.8f, 1f), new Color(0.2f, 0.5f, 0.9f, 1f), 5, false);
                CreateSprite(car.transform, "CarTop", new Vector3(0f, 0.35f, -0.01f), new Vector3(1.0f, 0.55f, 1f), new Color(0.15f, 0.40f, 0.75f, 1f), 6, false);
                CreateSprite(car.transform, "WheelFL", new Vector3(-0.55f, -0.42f, -0.02f), new Vector3(0.3f, 0.3f, 1f), new Color(0.1f, 0.1f, 0.1f, 1f), 7, true);
                CreateSprite(car.transform, "WheelFR", new Vector3(0.55f, -0.42f, -0.02f), new Vector3(0.3f, 0.3f, 1f), new Color(0.1f, 0.1f, 0.1f, 1f), 7, true);
            }

            car.transform.position = position;
            car.tag = "Player";
            Rigidbody2D body = GetOrAdd<Rigidbody2D>(car);
            body.gravityScale = 0f; body.freezeRotation = true;
            BoxCollider2D col = GetOrAdd<BoxCollider2D>(car);
            col.size = new Vector2(1.5f, 0.75f);
            return car;
        }

        // ─── Default Traffic Lane ─────────────────────────────────────────
        public static void CreateDefaultTrafficLane(Transform parent, string name, Vector3 spawnPos, Vector3 exitPos, float direction)
        {
            VehicleController bluePrefab = AssetDatabase.LoadAssetAtPath<VehicleController>(CarBluePrefabPath);
            VehicleController redPrefab = AssetDatabase.LoadAssetAtPath<VehicleController>(CarRedPrefabPath);
            VehicleController yellowPrefab = AssetDatabase.LoadAssetAtPath<VehicleController>(CarYellowPrefabPath);

            GameObject lane = FindOrCreateChild(parent, name);
            Transform spawn = FindOrCreateChild(lane.transform, "SpawnPoint").transform; spawn.position = spawnPos;
            Transform stop = FindOrCreateChild(lane.transform, "StopPoint").transform; stop.position = Vector3.Lerp(spawnPos, exitPos, 0.35f);
            Transform exit = FindOrCreateChild(lane.transform, "ExitPoint").transform; exit.position = exitPos;

            VehicleSpawner spawner = GetOrAdd<VehicleSpawner>(FindOrCreateChild(lane.transform, "Spawner"));
            SetReference(spawner, "vehiclePrefab", bluePrefab);
            SetObjectArray(spawner, "vehiclePrefabs", new Object[] { bluePrefab, redPrefab, yellowPrefab });
            SetReference(spawner, "spawnPoint", spawn);
            SetReference(spawner, "carStopPoint", stop);
            SetReference(spawner, "carExitPoint", exit);
            SetFloat(spawner, "spawnInterval", 4f);
            SetInt(spawner, "maximumActiveVehicles", 3);
            SetFloat(spawner, "travelDirection", direction);
        }

        // ─── Standard UI (common to levels 4–10) ──────────────────────────
        public static LevelUIController CreateStandardUI(ScoreManager score, SceneLoader sceneLoader, string missionText)
        {
            EnsureEventSystem();
            Sprite roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPanelSpritePath);

            GameObject canvasGO = FindOrCreate("UI");
            Canvas canvas = GetOrAdd<Canvas>(canvasGO);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasGO);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            GetOrAdd<GraphicRaycaster>(canvasGO);

            GameObject hud = FindOrCreateChild(canvas.transform, "HUD");
            SetRect(EnsureRectTransform(hud), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject gameplayHud = FindOrCreateChild(hud.transform, "GameplayHUD");
            SetRect(EnsureRectTransform(gameplayHud), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Mission Card
            GameObject missionCard = CreateUIPanel(gameplayHud.transform, "MissionCard", new Color(0.10f, 0.15f, 0.22f, 0.88f), roundedSprite);
            SetRect(missionCard.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(170f, -48f), new Vector2(320f, 68f));
            CreateUIText(missionCard.transform, "Title", "MISSION", 13, TextAlignmentOptions.Left, new Vector2(290f, 20f), new Vector2(10f, 16f), new Vector2(0f, 0.5f));
            TextMeshProUGUI objective = CreateUIText(missionCard.transform, "Objective", missionText, 14, TextAlignmentOptions.Left, new Vector2(290f, 32f), new Vector2(10f, -10f), new Vector2(0f, 0.5f));

            // Score Card
            GameObject scoreCard = CreateUIPanel(gameplayHud.transform, "ScoreCard", new Color(0.10f, 0.15f, 0.22f, 0.88f), roundedSprite);
            SetRect(scoreCard.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-110f, -48f), new Vector2(180f, 68f));
            CreateUIText(scoreCard.transform, "Title", "⭐ SCORE", 13, TextAlignmentOptions.Right, new Vector2(150f, 20f), new Vector2(-10f, 16f), new Vector2(1f, 0.5f));
            TextMeshProUGUI scoreText = CreateUIText(scoreCard.transform, "Value", "100", 24, TextAlignmentOptions.Right, new Vector2(150f, 32f), new Vector2(-10f, -10f), new Vector2(1f, 0.5f));
            scoreText.fontStyle = FontStyles.Bold;

            // Feedback Banner
            GameObject feedbackBanner = CreateUIPanel(hud.transform, "FeedbackBanner", new Color(0.08f, 0.12f, 0.18f, 0.92f), roundedSprite);
            SetRect(feedbackBanner.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(560f, 54f));
            CanvasGroup feedbackGroup = GetOrAdd<CanvasGroup>(feedbackBanner);
            FeedbackController feedbackCtrl = GetOrAdd<FeedbackController>(feedbackBanner);
            TextMeshProUGUI feedbackText = CreateUIText(feedbackBanner.transform, "Message", "", 16, TextAlignmentOptions.Center, new Vector2(530f, 40f), Vector2.zero, new Vector2(0.5f, 0.5f));
            SetReference(feedbackCtrl, "bannerPanel", feedbackBanner);
            SetReference(feedbackCtrl, "messageText", feedbackText);
            SetReference(feedbackCtrl, "feedbackText", feedbackText);
            SetReference(feedbackCtrl, "canvasGroup", feedbackGroup);

            // Completion Panel
            GameObject overlay = CreateUIPanel(hud.transform, "CompletionPanel", new Color(0.04f, 0.06f, 0.10f, 0.65f), null);
            SetRect(overlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CanvasGroup completionGroup = GetOrAdd<CanvasGroup>(overlay);

            GameObject completionCard = CreateUIPanel(overlay.transform, "CompletionCard", new Color(0.96f, 0.97f, 0.98f, 1f), roundedSprite);
            RectTransform completionRect = completionCard.GetComponent<RectTransform>();
            SetRect(completionRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 440f));

            TextMeshProUGUI header = CreateUIText(completionCard.transform, "Header", "🎉 LEVEL COMPLETE!", 28, TextAlignmentOptions.Center, new Vector2(420f, 40f), new Vector2(0f, 170f), new Vector2(0.5f, 0.5f));
            header.fontStyle = FontStyles.Bold;
            CreateUIText(completionCard.transform, "Subtitle", "Great work!", 17, TextAlignmentOptions.Center, new Vector2(420f, 28f), new Vector2(0f, 134f), new Vector2(0.5f, 0.5f));
            CreateUIText(completionCard.transform, "ScoreTitle", "FINAL SCORE", 13, TextAlignmentOptions.Center, new Vector2(260f, 20f), new Vector2(0f, 80f), new Vector2(0.5f, 0.5f));
            TextMeshProUGUI finalScore = CreateUIText(completionCard.transform, "FinalScore", "100", 42, TextAlignmentOptions.Center, new Vector2(260f, 50f), new Vector2(0f, 40f), new Vector2(0.5f, 0.5f));
            finalScore.fontStyle = FontStyles.Bold;

            GameObject statsRow = CreateUIPanel(completionCard.transform, "StatisticsRow", new Color(0f, 0f, 0f, 0f));
            SetRect(statsRow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(412f, 76f));
            TextMeshProUGUI safeActions = CreateStatCard(statsRow.transform, "SafeActionsCard", "SAFE ACTIONS", "0", new Vector2(-106f, 0f), roundedSprite);
            TextMeshProUGUI mistakes = CreateStatCard(statsRow.transform, "MistakesCard", "ERRORS", "0", new Vector2(106f, 0f), roundedSprite);

            TextMeshProUGUI ratingStars = CreateUIText(completionCard.transform, "Rating", "⭐⭐⭐⭐⭐", 24, TextAlignmentOptions.Center, new Vector2(260f, 32f), new Vector2(0f, -102f), new Vector2(0.5f, 0.5f));
            TextMeshProUGUI ratingLabel = CreateUIText(completionCard.transform, "RatingText", "Excellent!", 18, TextAlignmentOptions.Center, new Vector2(220f, 24f), new Vector2(0f, -130f), new Vector2(0.5f, 0.5f));
            ratingLabel.fontStyle = FontStyles.Bold;

            Button backButton = GetOrAddChildButton(completionCard.transform, "BackToMenuButton", "BACK TO MENU", new Vector2(-105f, -175f), roundedSprite);
            backButton.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 50f);
            Button nextButton = GetOrAddChildButton(completionCard.transform, "NextLevelButton", "NEXT LEVEL", new Vector2(105f, -175f), roundedSprite);
            nextButton.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 50f);

            LevelUIController ui = GetOrAdd<LevelUIController>(gameplayHud);
            SetReference(ui, "objectiveText", objective);
            SetReference(ui, "scoreText", scoreText);
            SetReference(ui, "completionPanel", overlay);
            SetReference(ui, "completionGroup", completionGroup);
            SetReference(ui, "completionCard", completionRect);
            SetReference(ui, "finalScoreText", finalScore);
            SetReference(ui, "safeActionsText", safeActions);
            SetReference(ui, "mistakesText", mistakes);
            SetReference(ui, "ratingStarsText", ratingStars);
            SetReference(ui, "ratingText", ratingLabel);
            SetReference(ui, "backButton", backButton);
            SetReference(ui, "nextButton", nextButton);
            SetReference(ui, "scoreManager", score);
            SetReference(ui, "sceneLoader", sceneLoader);

            UnityEventTools.AddPersistentListener(backButton.onClick, ui.BackToMenu);
            UnityEventTools.AddPersistentListener(nextButton.onClick, ui.LoadNextOrReplay);

            overlay.SetActive(false);
            return ui;
        }

        public static void CreateIntroPanel(string title, string message)
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null) return;
            Sprite roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPanelSpritePath);

            GameObject introPanel = CreateUIPanel(canvas.transform, "LevelIntroPanel", new Color(0.04f, 0.06f, 0.10f, 0.85f), null);
            SetRect(introPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject card = CreateUIPanel(introPanel.transform, "IntroCard", new Color(0.96f, 0.97f, 0.98f, 1f), roundedSprite);
            SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 320f));

            TextMeshProUGUI titleTxt = CreateUIText(card.transform, "Title", title, 24, TextAlignmentOptions.Center, new Vector2(400f, 36f), new Vector2(0f, 110f), new Vector2(0.5f, 0.5f));
            TextMeshProUGUI msgTxt = CreateUIText(card.transform, "Message", message, 16, TextAlignmentOptions.Center, new Vector2(380f, 120f), new Vector2(0f, 10f), new Vector2(0.5f, 0.5f));

            Button gotItBtn = GetOrAddChildButton(card.transform, "GotItButton", "GOT IT!", new Vector2(0f, -120f), roundedSprite);
            LevelIntroController intro = GetOrAdd<LevelIntroController>(introPanel);
            SetReference(intro, "introPanel", introPanel);
            SetReference(intro, "gotItButton", gotItBtn);
            SetReference(intro, "titleText", titleTxt);
            SetReference(intro, "messageText", msgTxt);
        }

        // ─── Standard Road ────────────────────────────────────────────────
        public static void CreateStandardRoad(Transform parent, bool withBackground = true)
        {
            if (withBackground)
            {
                GameObject bg = FindOrCreateChild(parent, "Background");
                CreateSprite(bg.transform, "Ground", new Vector3(0f, 0f, 5f), new Vector3(22f, 12f, 1f), new Color(0.85f, 0.92f, 0.88f, 1f), -10, false);
                CreateSprite(bg.transform, "BuildingA", new Vector3(-6f, 5f, 2f), new Vector3(3.5f, 2.5f, 1f), new Color(0.80f, 0.85f, 0.92f, 1f), -5, false);
                CreateSprite(bg.transform, "BuildingB", new Vector3(1f, 5.3f, 2f), new Vector3(4f, 3f, 1f), new Color(0.90f, 0.84f, 0.78f, 1f), -5, false);
                CreateSprite(bg.transform, "BuildingC", new Vector3(7f, 4.8f, 2f), new Vector3(3.2f, 2.2f, 1f), new Color(0.78f, 0.88f, 0.82f, 1f), -5, false);
            }

            CreateSprite(parent, "RoadBase", Vector3.zero, new Vector3(22f, 4.4f, 1f), new Color(0.10f, 0.11f, 0.13f, 1f), 0, false);
            CreateSprite(parent, "TopEdge", new Vector3(0f, 2.15f, -0.01f), new Vector3(22f, 0.1f, 1f), new Color(0.92f, 0.88f, 0.62f, 1f), 1, false);
            CreateSprite(parent, "BottomEdge", new Vector3(0f, -2.15f, -0.01f), new Vector3(22f, 0.1f, 1f), new Color(0.92f, 0.88f, 0.62f, 1f), 1, false);

            for (float x = -10f; x <= 10f; x += 1.2f)
                CreateSprite(parent, "Dash_" + x.ToString("F1"), new Vector3(x, 0f, -0.01f), new Vector3(0.7f, 0.08f, 1f), new Color(1f, 0.94f, 0.45f, 1f), 1, false);

            CreateSprite(parent, "SidewalkTop", new Vector3(0f, 3.25f, 0f), new Vector3(22f, 1.9f, 1f), new Color(0.67f, 0.72f, 0.69f, 1f), -2, false);
            CreateSprite(parent, "SidewalkBottom", new Vector3(0f, -3.25f, 0f), new Vector3(22f, 1.9f, 1f), new Color(0.67f, 0.72f, 0.69f, 1f), -2, false);
        }

        // ─── UI Helpers ───────────────────────────────────────────────────
        public static GameObject CreateUIPanel(Transform parent, string name, Color color, Sprite sprite = null)
        {
            GameObject go = FindOrCreateChild(parent, name);
            EnsureRectTransform(go);
            Image img = GetOrAdd<Image>(go);
            img.sprite = sprite;
            img.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            return go;
        }

        public static TextMeshProUGUI CreateUIText(Transform parent, string name, string content, float fontSize, TextAlignmentOptions align, Vector2 size, Vector2 pos, Vector2 anchor)
        {
            GameObject go = FindOrCreateChild(parent, name);
            RectTransform r = EnsureRectTransform(go);
            SetRect(r, anchor, anchor, pos, size);
            TextMeshProUGUI t = GetOrAdd<TextMeshProUGUI>(go);
            t.text = content; t.fontSize = fontSize; t.alignment = align; t.color = Color.white;
            return t;
        }

        public static TextMeshProUGUI CreateStatCard(Transform parent, string name, string label, string value, Vector2 pos, Sprite sprite)
        {
            GameObject card = CreateUIPanel(parent, name, new Color(0.92f, 0.94f, 0.96f, 1f), sprite);
            SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(180f, 76f));
            CreateUIText(card.transform, "Label", label, 11, TextAlignmentOptions.Left, new Vector2(140f, 18f), new Vector2(12f, 18f), new Vector2(0.5f, 0.5f));
            TextMeshProUGUI val = CreateUIText(card.transform, "Value", value, 24, TextAlignmentOptions.Left, new Vector2(140f, 32f), new Vector2(12f, -10f), new Vector2(0.5f, 0.5f));
            val.fontStyle = FontStyles.Bold;
            return val;
        }

        public static Button GetOrAddChildButton(Transform parent, string name, string label, Vector2 pos, Sprite sprite)
        {
            GameObject go = CreateUIPanel(parent, name, new Color(0.12f, 0.55f, 0.84f, 1f), sprite);
            SetRect(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(250f, 60f));
            Button btn = GetOrAdd<Button>(go);
            GetOrAdd<AnimatedUIButton>(go);
            TextMeshProUGUI t = CreateUIText(go.transform, "Text", label, 18, TextAlignmentOptions.Center, new Vector2(230f, 40f), Vector2.zero, new Vector2(0.5f, 0.5f));
            t.fontStyle = FontStyles.Bold;
            return btn;
        }

        public static RectTransform EnsureRectTransform(GameObject go)
        {
            if (go.TryGetComponent<RectTransform>(out var rt)) return rt;
            return go.AddComponent<RectTransform>();
        }

        public static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        // ─── SerializedObject Helpers ─────────────────────────────────────
        public static void SetReference(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty sp = so.FindProperty(field);
            if (sp != null) { sp.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        public static void SetObjectArray(Object target, string field, Object[] values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty sp = so.FindProperty(field);
            if (sp == null) return;
            sp.ClearArray();
            for (int i = 0; i < values.Length; i++)
            { sp.InsertArrayElementAtIndex(i); sp.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(Object target, string field, float value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty sp = so.FindProperty(field);
            if (sp != null) { sp.floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        public static void SetInt(Object target, string field, int value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty sp = so.FindProperty(field);
            if (sp != null) { sp.intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        public static void SetBool(Object target, string field, bool value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty sp = so.FindProperty(field);
            if (sp != null) { sp.boolValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        public static void SetVector2(Object target, string field, Vector2 value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty sp = so.FindProperty(field);
            if (sp != null) { sp.vector2Value = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
    }
}
#endif
