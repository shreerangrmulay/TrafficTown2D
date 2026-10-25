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
    /// <summary>
    /// Editor baker for the TrafficTown 2D Main Menu and Winding Road Level Select scene.
    /// Strictly complies with the children's road safety theme (ages 5-10):
    /// - Flat, bright colors reading exclusively from ThemeColors (Primary Yellow, Ink Black, Accent Purple).
    /// - 1920x1080 responsive Canvas Scaler (match 0.5).
    /// - 4 vertically stacked buttons (min height 120px) with scale bounce.
    /// - Winding road map inside horizontal ScrollRect with S-curve road and 10 draggable level anchors.
    /// - Player car navigation, star ratings, barrier lock states, checkered flag, and back button.
    /// </summary>
    public static class MainMenuSetup
    {
        private const string CanvasName = "Canvas";
        private const string EventSystemName = "EventSystem";
        private const string ServicesName = "MainMenu Services";

        private const string AutoRunPrefKey = "TrafficTown_MainMenu_Redesign_v3";

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
        [MenuItem("TrafficTown/Setup Main Menu (Redesign)")]
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

            // 1. Ensure all procedural / art assets are generated & imported
            MainMenuAssetGenerator.GenerateAllSprites();

            // 2. Ensure camera, services & EventSystem
            EnsureCamera();
            EnsureEventSystem();
            SceneLoader sceneLoader = FindOrCreateSceneLoader();
            FindOrCreateGameManager();

            // Clean any stray gameplay objects
            string[] strayObjects = { "Environment", "Traffic", "Gameplay", "Player", "Pedestrians", "PlayerCar" };
            foreach (GameObject rootObj in mainMenuScene.GetRootGameObjects())
            {
                for (int i = 0; i < strayObjects.Length; i++)
                {
                    if (rootObj.name == strayObjects[i])
                    {
                        Object.DestroyImmediate(rootObj);
                        break;
                    }
                }
            }

            // 3. Canvas Setup (1920x1080, match 0.5)
            Canvas canvas = FindOrCreateCanvas();
            ClearChildren(canvas.transform);

            // 4. Main Menu Background (Assets/Art/title_scene.png or flat #FFF21A)
            CreateMainMenuBackground(canvas.transform);

            // 5. Main Menu Content (Title, Subtitle, Traffic Light, 4 Stacked Buttons)
            GameObject mainMenuContent = CreateMainMenuContent(canvas.transform);

            // 6. Level Select Modal (Winding Road Map with horizontal ScrollRect)
            MainMenuController controller = FindOrCreateController(canvas.gameObject);
            GameObject levelSelectPanel = CreateLevelSelectMap(canvas.transform, controller);
            LevelMapController mapController = levelSelectPanel.GetComponent<LevelMapController>();

            // 7. Wire Buttons in MainMenuContent
            WireMainMenuButtons(mainMenuContent.transform, controller);

            // 8. Splash screen preservation
            GameObject splash = CreateMadeWithUnitySplashScreen(canvas.transform);
            splash.transform.SetAsLastSibling();

            // 9. Assign Controller references
            AssignControllerReferences(controller, sceneLoader, levelSelectPanel, mainMenuContent, mapController);
            EnsureBuildSettingsScenes();

            EditorSceneManager.MarkSceneDirty(mainMenuScene);
            EditorSceneManager.SaveScene(mainMenuScene);
            Selection.activeGameObject = canvas.gameObject;
            Debug.Log("[MainMenuSetup] TrafficTown 2D Menu & Level Select redesign successfully baked into MainMenu.unity!");
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
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GraphicRaycaster gr = canvas.GetComponent<GraphicRaycaster>();
            if (gr == null) gr = canvas.gameObject.AddComponent<GraphicRaycaster>();
            gr.enabled = true;

            return canvas;
        }

        private static void CreateMainMenuBackground(Transform canvasTransform)
        {
            GameObject background = FindOrCreateChild(canvasTransform, "Background");
            Image image = GetOrAdd<Image>(background);

            // TODO: User may provide custom illustration at Assets/Art/title_scene.png
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuAssetGenerator.TitleScenePath);
            if (bgSprite != null)
            {
                image.sprite = bgSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
                image.color = ThemeColors.PrimaryYellow;
            }

            SetFullScreen(background.GetComponent<RectTransform>());
            background.transform.SetAsFirstSibling();
        }

        private static GameObject CreateMainMenuContent(Transform canvasTransform)
        {
            GameObject content = FindOrCreateChild(canvasTransform, "MainMenuContent");
            SetFullScreen(content.GetComponent<RectTransform>());

            // 1. Traffic Light Decoration (Top-Left corner)
            CreateTrafficLightDecoration(content.transform);

            // 2. Title & Subtitle (Large at the top)
            CreateTitleAndSubtitle(content.transform);

            // 3. Four Vertically Stacked Centered Buttons (Min height 120px)
            CreateFourMenuButtons(content.transform);

            return content;
        }

        private static void CreateTrafficLightDecoration(Transform parent)
        {
            GameObject tlObj = FindOrCreateChild(parent, "TrafficLightIcon");
            RectTransform r = GetOrAdd<RectTransform>(tlObj);
            // Positioned neatly in top-left
            SetRect(r, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -80f), new Vector2(50f, 120f));

            Image img = GetOrAdd<Image>(tlObj);
            Sprite tlSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuAssetGenerator.TrafficLightPath);
            if (tlSprite != null)
            {
                img.sprite = tlSprite;
                img.color = Color.white;
            }
            else
            {
                img.color = ThemeColors.InkBlack;
            }
            img.raycastTarget = false;
        }

        private static void CreateTitleAndSubtitle(Transform parent)
        {
            GameObject titleContainer = FindOrCreateChild(parent, "TitleContainer");
            RectTransform tcRect = GetOrAdd<RectTransform>(titleContainer);
            SetRect(tcRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1000f, 160f));

            // Main Title: "Traffic Town 2D"
            TMP_Text title = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(titleContainer.transform, "Title"));
            title.text = "Traffic Town 2D";
            title.alignment = TextAlignmentOptions.Center;
            title.font = GetTMPFont();
            title.fontSize = 72;
            title.fontStyle = FontStyles.Bold;
            title.color = ThemeColors.InkBlack;
            title.raycastTarget = false;
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 25f), new Vector2(980f, 85f));

            // Subtitle: "Learn traffic rules. Stay safe."
            TMP_Text subtitle = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(titleContainer.transform, "Subtitle"));
            subtitle.text = "Learn traffic rules. Stay safe.";
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.font = GetTMPFont();
            subtitle.fontSize = 28;
            subtitle.fontStyle = FontStyles.Bold;
            subtitle.color = ThemeColors.InkBlack;
            subtitle.raycastTarget = false;
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -38f), new Vector2(800f, 40f));
        }

        private static void CreateFourMenuButtons(Transform parent)
        {
            GameObject container = FindOrCreateChild(parent, "ButtonStack");
            RectTransform stackRect = GetOrAdd<RectTransform>(container);
            // Centered on screen, vertically stacked
            SetRect(stackRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(560f, 620f));

            Sprite rounded9Slice = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuAssetGenerator.Button9SlicePath);
            Sprite outline9Slice = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuAssetGenerator.ButtonOutline9SlicePath);

            // Button configurations:
            // 1. Play (purple, largest, min height 120px -> height 140px, width 520px)
            // 2. Learn (purple, height 120px, width 480px)
            // 3. Road Safety Quiz (purple, height 120px, width 480px)
            // 4. Exit (black outline, height 120px, width 480px)
            string[] names = { "PlayButton", "LearnButton", "QuizButton", "ExitButton" };
            string[] labels = { "PLAY", "LEARN", "ROAD SAFETY QUIZ", "EXIT" };
            float[] heights = { 140f, 120f, 120f, 120f };
            float[] widths = { 520f, 480f, 480f, 480f };
            float[] yPositions = { 205f, 65f, -65f, -195f };
            float[] fontSizes = { 40f, 32f, 28f, 32f };
            bool[] isOutline = { false, false, false, true };

            for (int i = 0; i < names.Length; i++)
            {
                GameObject btnObj = FindOrCreateChild(container.transform, names[i]);
                RectTransform br = GetOrAdd<RectTransform>(btnObj);
                SetRect(br, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, yPositions[i]), new Vector2(widths[i], heights[i]));

                Button button = GetOrAdd<Button>(btnObj);
                Image img = GetOrAdd<Image>(btnObj);
                img.type = Image.Type.Sliced;
                img.raycastTarget = true;

                if (isOutline[i])
                {
                    img.sprite = outline9Slice;
                    img.color = Color.white; // outline sprite contains the #171717 stroke
                }
                else
                {
                    img.sprite = rounded9Slice;
                    img.color = ThemeColors.AccentPurple;
                }

                // Label
                TMP_Text label = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(btnObj.transform, "Label"));
                label.text = labels[i];
                label.alignment = TextAlignmentOptions.Center;
                label.font = GetTMPFont();
                label.fontSize = fontSizes[i];
                label.fontStyle = FontStyles.Bold;
                label.color = isOutline[i] ? ThemeColors.InkBlack : ThemeColors.White;
                label.raycastTarget = false;
                SetFullScreen(label.rectTransform);

                // Add scale bounce coroutine
                GetOrAdd<MenuButtonBounce>(btnObj);
            }
        }

        private static void WireMainMenuButtons(Transform contentTransform, MainMenuController controller)
        {
            Transform stack = contentTransform.Find("ButtonStack");
            if (stack == null) return;

            Transform playBtn = stack.Find("PlayButton");
            if (playBtn != null)
            {
                Button btn = playBtn.GetComponent<Button>();
                btn.onClick.RemoveAllListeners();
                UnityEventTools.AddPersistentListener(btn.onClick, controller.Play);
            }

            Transform learnBtn = stack.Find("LearnButton");
            if (learnBtn != null)
            {
                Button btn = learnBtn.GetComponent<Button>();
                btn.onClick.RemoveAllListeners();
                UnityEventTools.AddPersistentListener(btn.onClick, controller.Learn);
            }

            Transform quizBtn = stack.Find("QuizButton");
            if (quizBtn != null)
            {
                Button btn = quizBtn.GetComponent<Button>();
                btn.onClick.RemoveAllListeners();
                UnityEventTools.AddPersistentListener(btn.onClick, controller.Quiz);
            }

            Transform exitBtn = stack.Find("ExitButton");
            if (exitBtn != null)
            {
                Button btn = exitBtn.GetComponent<Button>();
                btn.onClick.RemoveAllListeners();
                UnityEventTools.AddPersistentListener(btn.onClick, controller.Exit);
            }
        }

        private static GameObject CreateLevelSelectMap(Transform canvasTransform, MainMenuController controller)
        {
            GameObject panel = FindOrCreateChild(canvasTransform, "LevelSelectPanel");
            SetFullScreen(panel.GetComponent<RectTransform>());

            Image bg = GetOrAdd<Image>(panel);
            // TODO: User may provide custom road map illustration at Assets/Art/road_map_bg.png
            Sprite mapBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuAssetGenerator.RoadMapBgPath);
            if (mapBgSprite != null)
            {
                bg.sprite = mapBgSprite;
                bg.color = Color.white;
            }
            else
            {
                bg.sprite = null;
                bg.color = ThemeColors.PrimaryYellow;
            }
            bg.raycastTarget = true;

            // 1. Header (Title: "CHOOSE YOUR MISSION" or "LEVEL SELECT")
            GameObject header = FindOrCreateChild(panel.transform, "Header");
            RectTransform hr = GetOrAdd<RectTransform>(header);
            SetRect(hr, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(800f, 90f));

            TMP_Text headerTitle = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(header.transform, "Title"));
            headerTitle.text = "LEVEL SELECT";
            headerTitle.alignment = TextAlignmentOptions.Center;
            headerTitle.font = GetTMPFont();
            headerTitle.fontSize = 54;
            headerTitle.fontStyle = FontStyles.Bold;
            headerTitle.color = ThemeColors.InkBlack;
            headerTitle.raycastTarget = false;
            SetFullScreen(headerTitle.rectTransform);

            // 2. Horizontal ScrollRect
            GameObject scrollObj = FindOrCreateChild(panel.transform, "HorizontalScrollRect");
            SetFullScreen(scrollObj.GetComponent<RectTransform>());
            ScrollRect scrollRect = GetOrAdd<ScrollRect>(scrollObj);
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            // Viewport
            GameObject viewportObj = FindOrCreateChild(scrollObj.transform, "Viewport");
            SetFullScreen(viewportObj.GetComponent<RectTransform>());
            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();

            Mask oldMask = viewportObj.GetComponent<Mask>();
            if (oldMask != null) Object.DestroyImmediate(oldMask);
            Image oldImg = viewportObj.GetComponent<Image>();
            if (oldImg != null) Object.DestroyImmediate(oldImg);

            GetOrAdd<RectMask2D>(viewportObj);

            // Content Container (Wide horizontal canvas for 10 winding levels)
            const float contentWidth = 3800f;
            const float contentHeight = 1080f;
            GameObject contentObj = FindOrCreateChild(viewportObj.transform, "Content");
            RectTransform contentRect = GetOrAdd<RectTransform>(contentObj);
            SetRect(contentRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(contentWidth, contentHeight));
            contentRect.pivot = new Vector2(0f, 0.5f);

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            // 3. Draggable RectTransform Anchors for S-shaped curve
            // 10 level positions defined as draggable anchors in editor
            Vector2[] anchorPositions = new Vector2[]
            {
                new Vector2(280f, -120f),   // 1. Safe Crossing
                new Vector2(620f, 80f),     // 2. Smart Crossing
                new Vector2(980f, 180f),    // 3. Yield Right
                new Vector2(1340f, 40f),    // 4. Night Driving
                new Vector2(1700f, -140f),  // 5. Traffic Controller
                new Vector2(2060f, -100f),  // 6. Extreme Roads
                new Vector2(2420f, 90f),    // 7. School Zone
                new Vector2(2780f, 190f),   // 8. The Overtake
                new Vector2(3140f, 60f),    // 9. Roundabout Rules
                new Vector2(3480f, -80f)    // 10. Final Commute
            };

            string[] levelTitles = new string[]
            {
                "Safe Crossing",
                "Smart Crossing",
                "Yield Right",
                "Night Driving",
                "Traffic Controller",
                "Extreme Roads",
                "School Zone",
                "The Overtake",
                "Roundabout Rules",
                "Final Commute"
            };

            GameObject anchorsRoot = FindOrCreateChild(contentObj.transform, "RoadAnchors");
            RectTransform arRoot = GetOrAdd<RectTransform>(anchorsRoot);
            SetRect(arRoot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(contentWidth, contentHeight));
            arRoot.pivot = new Vector2(0f, 0.5f);

            List<RectTransform> anchorList = new List<RectTransform>();
            for (int i = 0; i < anchorPositions.Length; i++)
            {
                GameObject aObj = FindOrCreateChild(anchorsRoot.transform, $"Anchor_Level_{i + 1}");
                RectTransform ar = GetOrAdd<RectTransform>(aObj);
                SetRect(ar, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), anchorPositions[i], new Vector2(20f, 20f));
                ar.pivot = new Vector2(0.5f, 0.5f);
                anchorList.Add(ar);
            }

            // 4. Winding Road Renderer (draws black road + dashed yellow line)
            GameObject roadObj = FindOrCreateChild(contentObj.transform, "WindingRoad");
            RectTransform roadRect = GetOrAdd<RectTransform>(roadObj);
            SetRect(roadRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(contentWidth, contentHeight));
            roadRect.pivot = new Vector2(0f, 0.5f);
            roadObj.transform.SetAsFirstSibling(); // Draw under the nodes

            RoadMapCurveRenderer roadCurve = GetOrAdd<RoadMapCurveRenderer>(roadObj);
            roadCurve.SetAnchors(anchorList);
            roadCurve.EnsureSubRenderers();
            roadCurve.RebuildRoad();

            // 5. Create 10 Level Nodes
            GameObject nodesRoot = FindOrCreateChild(contentObj.transform, "LevelNodes");
            RectTransform nrRoot = GetOrAdd<RectTransform>(nodesRoot);
            SetRect(nrRoot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(contentWidth, contentHeight));
            nrRoot.pivot = new Vector2(0f, 0.5f);

            List<LevelNode> nodeList = new List<LevelNode>();
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuAssetGenerator.NodeCirclePath);
            Sprite starFilled = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuAssetGenerator.StarFilledPath);
            Sprite starEmpty = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuAssetGenerator.StarEmptyPath);
            Sprite lockSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuAssetGenerator.LockSpritePath);
            Sprite barrierSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuAssetGenerator.BarrierSpritePath);

            for (int i = 0; i < 10; i++)
            {
                int levelNum = i + 1;
                GameObject nodeObj = FindOrCreateChild(nodesRoot.transform, $"LevelNode_{levelNum}");
                RectTransform nr = GetOrAdd<RectTransform>(nodeObj);
                SetRect(nr, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), anchorPositions[i], new Vector2(140f, 140f));
                nr.pivot = new Vector2(0.5f, 0.5f);

                // Round purple button
                Button nodeBtn = GetOrAdd<Button>(nodeObj);
                Image btnImg = GetOrAdd<Image>(nodeObj);
                btnImg.sprite = circleSprite;
                btnImg.color = ThemeColors.AccentPurple;
                btnImg.raycastTarget = true;

                // Level Number
                TMP_Text numTmp = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(nodeObj.transform, "NumberText"));
                numTmp.text = levelNum.ToString();
                numTmp.alignment = TextAlignmentOptions.Center;
                numTmp.font = GetTMPFont();
                numTmp.fontSize = 42;
                numTmp.fontStyle = FontStyles.Bold;
                numTmp.color = ThemeColors.White;
                numTmp.raycastTarget = false;
                SetRect(numTmp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 60f));

                // Optional icon inside node
                GameObject icoObj = FindOrCreateChild(nodeObj.transform, "Icon");
                RectTransform ir = GetOrAdd<RectTransform>(icoObj);
                SetRect(ir, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(36f, 36f));
                Image icoImg = GetOrAdd<Image>(icoObj);
                string iconPath = $"{MainMenuAssetGenerator.ArtIconsDir}/Icon_Level{levelNum}.png";
                Sprite icoSpr = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                if (icoSpr != null)
                {
                    icoImg.sprite = icoSpr;
                    icoImg.color = ThemeColors.White;
                }
                icoImg.raycastTarget = false;
                icoObj.SetActive(false); // keep uncluttered, number is large and legible

                // Lock Overlay (shown when locked)
                GameObject lockObj = FindOrCreateChild(nodeObj.transform, "LockOverlay");
                RectTransform lr = GetOrAdd<RectTransform>(lockObj);
                SetRect(lr, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f));
                Image lockImg = GetOrAdd<Image>(lockObj);
                lockImg.sprite = lockSprite;
                lockImg.color = ThemeColors.LockedDark;
                lockImg.raycastTarget = false;
                lockObj.SetActive(false);

                // Road Barrier Sprite (across the road on locked levels)
                GameObject barObj = FindOrCreateChild(nodeObj.transform, "RoadBarrier");
                RectTransform br = GetOrAdd<RectTransform>(barObj);
                SetRect(br, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 65f), new Vector2(90f, 36f));
                Image barImg = GetOrAdd<Image>(barObj);
                barImg.sprite = barrierSprite;
                barImg.color = Color.white;
                barImg.raycastTarget = false;
                barObj.SetActive(false);

                // Level Name Label below button
                TMP_Text labelTmp = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(nodeObj.transform, "NameLabel"));
                labelTmp.text = levelTitles[i];
                labelTmp.alignment = TextAlignmentOptions.Center;
                labelTmp.font = GetTMPFont();
                labelTmp.fontSize = 20;
                labelTmp.fontStyle = FontStyles.Bold;
                labelTmp.color = ThemeColors.InkBlack;
                labelTmp.raycastTarget = false;
                SetRect(labelTmp.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -32f), new Vector2(260f, 34f));

                // 3 Stars Row below label
                GameObject starsContainer = FindOrCreateChild(nodeObj.transform, "StarsRow");
                RectTransform scr = GetOrAdd<RectTransform>(starsContainer);
                SetRect(scr, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -62f), new Vector2(100f, 26f));

                Image[] starImgs = new Image[3];
                float[] sx = { -28f, 0f, 28f };
                for (int s = 0; s < 3; s++)
                {
                    GameObject sObj = FindOrCreateChild(starsContainer.transform, $"Star_{s}");
                    RectTransform sr = GetOrAdd<RectTransform>(sObj);
                    SetRect(sr, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(sx[s], 0f), new Vector2(24f, 24f));
                    Image sImg = GetOrAdd<Image>(sObj);
                    sImg.sprite = starFilled;
                    sImg.color = ThemeColors.StarFilled;
                    sImg.raycastTarget = false;
                    starImgs[s] = sImg;
                }

                // Attach LevelNode and Bounce components
                LevelNode levelNode = GetOrAdd<LevelNode>(nodeObj);
                SerializedObject serializedNode = new SerializedObject(levelNode);
                serializedNode.FindProperty("levelNumber").intValue = levelNum;
                serializedNode.FindProperty("levelTitle").stringValue = levelTitles[i];
                serializedNode.FindProperty("button").objectReferenceValue = nodeBtn;
                serializedNode.FindProperty("buttonBgImage").objectReferenceValue = btnImg;
                serializedNode.FindProperty("numberText").objectReferenceValue = numTmp;
                serializedNode.FindProperty("iconImage").objectReferenceValue = icoImg;
                serializedNode.FindProperty("titleLabelText").objectReferenceValue = labelTmp;
                serializedNode.FindProperty("lockOverlayObject").objectReferenceValue = lockObj;
                serializedNode.FindProperty("barrierObject").objectReferenceValue = barObj;
                serializedNode.FindProperty("starFilledSprite").objectReferenceValue = starFilled;
                serializedNode.FindProperty("starEmptySprite").objectReferenceValue = starEmpty;
                serializedNode.FindProperty("lockSprite").objectReferenceValue = lockSprite;

                SerializedProperty starsProp = serializedNode.FindProperty("starImages");
                for (int s = 0; s < 3; s++)
                {
                    starsProp.GetArrayElementAtIndex(s).objectReferenceValue = starImgs[s];
                }
                serializedNode.ApplyModifiedPropertiesWithoutUndo();

                GetOrAdd<MenuButtonBounce>(nodeObj);

                // Wire button click
                nodeBtn.onClick.RemoveAllListeners();
                switch (levelNum)
                {
                    case 1: UnityEventTools.AddPersistentListener(nodeBtn.onClick, controller.PlayLevel1); break;
                    case 2: UnityEventTools.AddPersistentListener(nodeBtn.onClick, controller.PlayLevel2); break;
                    case 3: UnityEventTools.AddPersistentListener(nodeBtn.onClick, controller.PlayLevel3); break;
                    case 4: UnityEventTools.AddPersistentListener(nodeBtn.onClick, controller.PlayLevel4); break;
                    case 5: UnityEventTools.AddPersistentListener(nodeBtn.onClick, controller.PlayLevel5); break;
                    case 6: UnityEventTools.AddPersistentListener(nodeBtn.onClick, controller.PlayLevel6); break;
                    case 7: UnityEventTools.AddPersistentListener(nodeBtn.onClick, controller.PlayLevel7); break;
                    case 8: UnityEventTools.AddPersistentListener(nodeBtn.onClick, controller.PlayLevel8); break;
                    case 9: UnityEventTools.AddPersistentListener(nodeBtn.onClick, controller.PlayLevel9); break;
                    case 10: UnityEventTools.AddPersistentListener(nodeBtn.onClick, controller.PlayLevel10); break;
                }

                nodeList.Add(levelNode);
            }

            // 6. Checkered Finish Flag at Level 10
            GameObject flagObj = FindOrCreateChild(contentObj.transform, "CheckeredFinishFlag");
            RectTransform flagRect = GetOrAdd<RectTransform>(flagObj);
            SetRect(flagRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), anchorPositions[9] + new Vector2(75f, 25f), new Vector2(64f, 64f));
            Image flagImg = GetOrAdd<Image>(flagObj);
            Sprite flagSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuAssetGenerator.FlagSpritePath);
            flagImg.sprite = flagSprite;
            flagImg.color = Color.white;
            flagImg.raycastTarget = false;

            // 7. Player Car Sprite (Assets/Art/car.png) sitting on current node
            GameObject carObj = FindOrCreateChild(contentObj.transform, "PlayerCarOnMap");
            RectTransform carRect = GetOrAdd<RectTransform>(carObj);
            SetRect(carRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), anchorPositions[0] + new Vector2(0f, 15f), new Vector2(48f, 76f));
            Image carImg = GetOrAdd<Image>(carObj);
            Sprite carSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuAssetGenerator.CarSpritePath);
            carImg.sprite = carSprite;
            carImg.color = Color.white;
            carImg.raycastTarget = false;
            carObj.transform.SetAsLastSibling();

            // 8. "Back to Menu" button in bottom-left corner (black outline style)
            GameObject backObj = FindOrCreateChild(panel.transform, "BackToMenuButton");
            RectTransform backRect = GetOrAdd<RectTransform>(backObj);
            // Bottom-left corner
            SetRect(backRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(170f, 75f), new Vector2(250f, 75f));

            Button backBtn = GetOrAdd<Button>(backObj);
            Image backImg = GetOrAdd<Image>(backObj);
            backImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuAssetGenerator.ButtonOutline9SlicePath);
            backImg.type = Image.Type.Sliced;
            backImg.color = Color.white;
            backImg.raycastTarget = true;

            TMP_Text backLabel = GetOrAdd<TextMeshProUGUI>(FindOrCreateChild(backObj.transform, "Label"));
            backLabel.text = "BACK TO MENU";
            backLabel.alignment = TextAlignmentOptions.Center;
            backLabel.font = GetTMPFont();
            backLabel.fontSize = 22;
            backLabel.fontStyle = FontStyles.Bold;
            backLabel.color = ThemeColors.InkBlack;
            backLabel.raycastTarget = false;
            SetFullScreen(backLabel.rectTransform);

            GetOrAdd<MenuButtonBounce>(backObj);

            backBtn.onClick.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(backBtn.onClick, controller.CloseLevelSelect);

            // 9. Attach and serialize LevelMapController
            LevelMapController mapController = GetOrAdd<LevelMapController>(panel);
            SerializedObject serializedMap = new SerializedObject(mapController);
            serializedMap.FindProperty("scrollRect").objectReferenceValue = scrollRect;
            serializedMap.FindProperty("viewportRect").objectReferenceValue = viewportRect;
            serializedMap.FindProperty("contentRect").objectReferenceValue = contentRect;
            serializedMap.FindProperty("roadCurveRenderer").objectReferenceValue = roadCurve;
            serializedMap.FindProperty("playerCarRect").objectReferenceValue = carRect;
            serializedMap.FindProperty("playerCarImage").objectReferenceValue = carImg;
            serializedMap.FindProperty("checkeredFlagRect").objectReferenceValue = flagRect;
            serializedMap.FindProperty("backToMenuButton").objectReferenceValue = backBtn;

            SerializedProperty anchorsProp = serializedMap.FindProperty("levelAnchors");
            anchorsProp.ClearArray();
            for (int a = 0; a < anchorList.Count; a++)
            {
                anchorsProp.InsertArrayElementAtIndex(a);
                anchorsProp.GetArrayElementAtIndex(a).objectReferenceValue = anchorList[a];
            }

            SerializedProperty nodesProp = serializedMap.FindProperty("levelNodes");
            nodesProp.ClearArray();
            for (int n = 0; n < nodeList.Count; n++)
            {
                nodesProp.InsertArrayElementAtIndex(n);
                nodesProp.GetArrayElementAtIndex(n).objectReferenceValue = nodeList[n];
            }

            serializedMap.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return panel;
        }

        private static void AssignControllerReferences(MainMenuController controller, SceneLoader sceneLoader, GameObject levelSelect, GameObject mainMenuContent, LevelMapController mapController)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("sceneLoader").objectReferenceValue = sceneLoader;
            serializedController.FindProperty("levelSelectPanel").objectReferenceValue = levelSelect;
            serializedController.FindProperty("mainMenuContent").objectReferenceValue = mainMenuContent;
            SerializedProperty mapProp = serializedController.FindProperty("levelMapController");
            if (mapProp != null) mapProp.objectReferenceValue = mapController;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_FontAsset GetTMPFont()
        {
            return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ??
                   TMP_Settings.defaultFontAsset;
        }

        private static void EnsureCamera()
        {
            Camera camera = Object.FindAnyObjectByType<Camera>();
            if (camera == null) camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = ThemeColors.PrimaryYellow;
            camera.tag = "MainCamera";
            if (camera.GetComponent<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }
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
            if (sceneLoader != null) return sceneLoader;

            GameObject services = FindOrCreateServicesObject();
            return services.AddComponent<SceneLoader>();
        }

        private static GameManager FindOrCreateGameManager()
        {
            GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
            if (gameManager != null) return gameManager;

            GameObject services = FindOrCreateServicesObject();
            return services.AddComponent<GameManager>();
        }

        private static GameObject FindOrCreateServicesObject()
        {
            GameObject services = GameObject.Find(ServicesName);
            if (services == null) services = new GameObject(ServicesName);
            return services;
        }

        private static MainMenuController FindOrCreateController(GameObject canvasObject)
        {
            MainMenuController controller = Object.FindAnyObjectByType<MainMenuController>();
            if (controller != null) return controller;
            return canvasObject.AddComponent<MainMenuController>();
        }

        private static GameObject FindOrCreateChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null) return child.gameObject;

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
