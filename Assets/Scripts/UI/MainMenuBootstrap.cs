using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace TrafficTown2D.UI
{
    /// <summary>
    /// Runtime bootstrapper for MainMenu scene. Enforces responsive 1920x1080 Canvas Scaler,
    /// constructs or polishes the flat yellow children's theme, and ensures the horizontal
    /// Winding Road Map Level Select is fully initialized and operational.
    /// </summary>
    public static class MainMenuBootstrap
    {
        private static readonly string[] LevelTitles = new string[]
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

        private static readonly Vector2[] AnchorPositions = new Vector2[]
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == "MainMenu")
            {
                ConfigureRuntimeMenu();
            }
        }

        public static void ConfigureRuntimeMenu()
        {
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // 1. Responsive Canvas Scaler (1920x1080, match 0.5)
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 2. GraphicRaycaster
            GraphicRaycaster gr = canvas.GetComponent<GraphicRaycaster>();
            if (gr == null) gr = canvas.gameObject.AddComponent<GraphicRaycaster>();
            gr.enabled = true;

            // 3. EventSystem
            var es = UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // 4. Remove dark overlays
            Transform oldOverlay = canvas.transform.Find("BackgroundOverlay");
            if (oldOverlay != null) oldOverlay.gameObject.SetActive(false);

            // 5. Background (#FFF21A or custom title_scene)
            Transform bgT = canvas.transform.Find("Background");
            if (bgT != null)
            {
                Image bgImg = bgT.GetComponent<Image>();
                if (bgImg != null)
                {
                    Sprite s = MenuAssetProvider.GetTitleSceneBackground();
                    if (s != null)
                    {
                        bgImg.sprite = s;
                        bgImg.color = Color.white;
                    }
                    else
                    {
                        bgImg.sprite = null;
                        bgImg.color = ThemeColors.PrimaryYellow;
                    }
                }
            }

            // 6. Controller
            MainMenuController controller = canvas.GetComponent<MainMenuController>() ?? UnityEngine.Object.FindAnyObjectByType<MainMenuController>();

            // 7. Polish Main Menu Content
            Transform content = canvas.transform.Find("MainMenuContent");
            if (content != null)
            {
                PolishMainMenuContent(content, controller);
            }

            // 8. Ensure Winding Road Map in LevelSelectPanel
            Transform panel = canvas.transform.Find("LevelSelectPanel");
            if (panel != null)
            {
                EnsureWindingRoadMap(panel, controller);
            }

            // 9. Unblock raycast targets
            UnblockRaycasts(canvas);
        }

        private static void PolishMainMenuContent(Transform content, MainMenuController controller)
        {
            // Title & Subtitle styling
            Transform titleContainer = content.Find("TitleContainer");
            if (titleContainer != null)
            {
                TMP_Text title = titleContainer.Find("Title")?.GetComponent<TMP_Text>();
                if (title != null)
                {
                    title.text = "Traffic Town 2D";
                    title.color = ThemeColors.InkBlack;
                    title.enableVertexGradient = false;
                }

                TMP_Text sub = titleContainer.Find("Subtitle")?.GetComponent<TMP_Text>();
                if (sub != null)
                {
                    sub.text = "Learn traffic rules. Stay safe.";
                    sub.color = ThemeColors.InkBlack;
                }

                // Hide category pill if present for flat clean look
                Transform pill = titleContainer.Find("CategoryPill");
                if (pill != null) pill.gameObject.SetActive(false);
            }

            // Traffic light top-left decoration
            Transform deco = content.Find("Decorations");
            if (deco != null)
            {
                Transform badge = deco.Find("SafetyBadge");
                if (badge != null) badge.gameObject.SetActive(false);
            }

            // Ensure 4 vertically stacked buttons: Play, Learn, Quiz, Exit
            Transform btnContainer = content.Find("ButtonStack") ?? content.Find("ButtonContainer");
            if (btnContainer != null)
            {
                StyleButton(btnContainer.Find("PlayButton"), "PLAY", 140f, 520f, 40f, isOutline: false, () => controller?.Play());
                StyleButton(btnContainer.Find("LearnButton"), "LEARN", 120f, 480f, 32f, isOutline: false, () => controller?.Learn());
                StyleButton(btnContainer.Find("QuizButton"), "ROAD SAFETY QUIZ", 120f, 480f, 28f, isOutline: false, () => controller?.Quiz());
                StyleButton(btnContainer.Find("ExitButton"), "EXIT", 120f, 480f, 32f, isOutline: true, () => controller?.Exit());
            }
        }

        private static void StyleButton(Transform btnT, string labelText, float h, float w, float fontSize, bool isOutline, Action onClick)
        {
            if (btnT == null) return;

            RectTransform rt = btnT.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(w, h);
            }

            Image img = btnT.GetComponent<Image>();
            if (img != null)
            {
                img.type = Image.Type.Sliced;
                if (isOutline)
                {
                    img.sprite = MenuAssetProvider.GetButtonOutline9Slice();
                    img.color = Color.white;
                }
                else
                {
                    img.sprite = MenuAssetProvider.GetButton9Slice();
                    img.color = ThemeColors.AccentPurple;
                }
                img.raycastTarget = true;
            }

            // Label
            TMP_Text lbl = btnT.Find("Label")?.GetComponent<TMP_Text>();
            if (lbl != null)
            {
                lbl.text = labelText;
                lbl.color = isOutline ? ThemeColors.InkBlack : ThemeColors.White;
                lbl.fontSize = fontSize;
                lbl.fontStyle = FontStyles.Bold;
                lbl.raycastTarget = false;
            }

            // Hide old icon if present
            Transform ico = btnT.Find("Icon");
            if (ico != null) ico.gameObject.SetActive(false);

            // Bounce component
            if (btnT.GetComponent<MenuButtonBounce>() == null)
            {
                btnT.gameObject.AddComponent<MenuButtonBounce>();
            }

            Button btn = btnT.GetComponent<Button>();
            if (btn != null && onClick != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => onClick());
            }
        }

        private static void EnsureWindingRoadMap(Transform panel, MainMenuController controller)
        {
            // Hide any legacy 5x2 grid
            Transform legacyGrid = panel.Find("GridContainer");
            if (legacyGrid != null) legacyGrid.gameObject.SetActive(false);

            Image panelBg = panel.GetComponent<Image>();
            if (panelBg != null)
            {
                Sprite s = MenuAssetProvider.GetRoadMapBackground();
                if (s != null)
                {
                    panelBg.sprite = s;
                    panelBg.color = Color.white;
                }
                else
                {
                    panelBg.sprite = null;
                    panelBg.color = ThemeColors.PrimaryYellow;
                }
            }

            // Title
            Transform header = panel.Find("Header");
            if (header != null)
            {
                TMP_Text t = header.Find("Title")?.GetComponent<TMP_Text>();
                if (t != null)
                {
                    t.text = "LEVEL SELECT";
                    t.color = ThemeColors.InkBlack;
                    t.enableVertexGradient = false;
                }
                Transform sub = header.Find("Subtitle");
                if (sub != null) sub.gameObject.SetActive(false);
            }

            // Check if HorizontalScrollRect already exists
            Transform scrollObj = panel.Find("HorizontalScrollRect");
            if (scrollObj != null)
            {
                SanitizeExistingScrollMap(panel, scrollObj, controller);
                return;
            }

            // Build HorizontalScrollRect at runtime
            GameObject scrollGo = new GameObject("HorizontalScrollRect", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel, false);
            SetFullScreen(scrollGo.GetComponent<RectTransform>());

            ScrollRect scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            // Viewport - MUST use RectMask2D (never Mask with Color.clear which causes 100% alpha stencil clipping)
            GameObject vpObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            vpObj.transform.SetParent(scrollGo.transform, false);
            SetFullScreen(vpObj.GetComponent<RectTransform>());
            RectTransform vpRect = vpObj.GetComponent<RectTransform>();

            // Content
            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(vpObj.transform, false);
            RectTransform contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.pivot = new Vector2(0f, 0.5f);
            contentRect.anchorMin = new Vector2(0f, 0.5f);
            contentRect.anchorMax = new Vector2(0f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(3800f, 1080f);

            scrollRect.viewport = vpRect;
            scrollRect.content = contentRect;

            // Anchors
            GameObject anchorsRoot = new GameObject("RoadAnchors", typeof(RectTransform));
            anchorsRoot.transform.SetParent(contentGo.transform, false);
            RectTransform arRoot = anchorsRoot.GetComponent<RectTransform>();
            SetRect(arRoot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3800f, 1080f));
            arRoot.pivot = new Vector2(0f, 0.5f);

            List<RectTransform> anchorList = new List<RectTransform>();
            for (int i = 0; i < AnchorPositions.Length; i++)
            {
                GameObject aObj = new GameObject($"Anchor_Level_{i + 1}", typeof(RectTransform));
                aObj.transform.SetParent(anchorsRoot.transform, false);
                RectTransform ar = aObj.GetComponent<RectTransform>();
                SetRect(ar, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), AnchorPositions[i], new Vector2(20f, 20f));
                ar.pivot = new Vector2(0.5f, 0.5f);
                anchorList.Add(ar);
            }

            // Road Renderer
            GameObject roadGo = new GameObject("WindingRoad", typeof(RectTransform), typeof(RoadMapCurveRenderer));
            roadGo.transform.SetParent(contentGo.transform, false);
            RectTransform roadRt = roadGo.GetComponent<RectTransform>();
            SetRect(roadRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3800f, 1080f));
            roadRt.pivot = new Vector2(0f, 0.5f);
            roadGo.transform.SetAsFirstSibling();

            RoadMapCurveRenderer roadCurve = roadGo.GetComponent<RoadMapCurveRenderer>();
            roadCurve.SetAnchors(anchorList);
            roadCurve.EnsureSubRenderers();
            roadCurve.RebuildRoad();

            // Nodes
            GameObject nodesRoot = new GameObject("LevelNodes", typeof(RectTransform));
            nodesRoot.transform.SetParent(contentGo.transform, false);
            RectTransform nrRoot = nodesRoot.GetComponent<RectTransform>();
            SetRect(nrRoot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3800f, 1080f));
            nrRoot.pivot = new Vector2(0f, 0.5f);

            List<LevelNode> nodeList = new List<LevelNode>();
            Sprite circle = MenuAssetProvider.GetNodeCircle();
            Sprite starF = MenuAssetProvider.GetStarFilled();
            Sprite starE = MenuAssetProvider.GetStarEmpty();
            Sprite lockS = MenuAssetProvider.GetLockSprite();
            Sprite barrierS = MenuAssetProvider.GetBarrierSprite();

            for (int i = 0; i < 10; i++)
            {
                int levelNum = i + 1;
                GameObject nodeObj = new GameObject($"LevelNode_{levelNum}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LevelNode), typeof(MenuButtonBounce));
                nodeObj.transform.SetParent(nodesRoot.transform, false);

                RectTransform nr = nodeObj.GetComponent<RectTransform>();
                nr.anchoredPosition = AnchorPositions[i];
                nr.sizeDelta = new Vector2(140f, 140f);

                Image btnImg = nodeObj.GetComponent<Image>();
                btnImg.sprite = circle;
                btnImg.color = ThemeColors.AccentPurple;
                btnImg.raycastTarget = true;

                Button btn = nodeObj.GetComponent<Button>();

                // Number
                GameObject numObj = new GameObject("NumberText", typeof(RectTransform), typeof(TextMeshProUGUI));
                numObj.transform.SetParent(nodeObj.transform, false);
                TMP_Text numTmp = numObj.GetComponent<TMP_Text>();
                numTmp.text = levelNum.ToString();
                numTmp.alignment = TextAlignmentOptions.Center;
                numTmp.fontSize = 42;
                numTmp.fontStyle = FontStyles.Bold;
                numTmp.color = ThemeColors.White;
                numTmp.raycastTarget = false;
                SetRect(numTmp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 60f));

                // Lock Overlay
                GameObject lockObj = new GameObject("LockOverlay", typeof(RectTransform), typeof(Image));
                lockObj.transform.SetParent(nodeObj.transform, false);
                SetRect(lockObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f));
                Image lockImg = lockObj.GetComponent<Image>();
                lockImg.sprite = lockS;
                lockImg.color = ThemeColors.LockedDark;
                lockImg.raycastTarget = false;
                lockObj.SetActive(false);

                // Barrier
                GameObject barObj = new GameObject("RoadBarrier", typeof(RectTransform), typeof(Image));
                barObj.transform.SetParent(nodeObj.transform, false);
                SetRect(barObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 65f), new Vector2(90f, 36f));
                Image barImg = barObj.GetComponent<Image>();
                barImg.sprite = barrierS;
                barImg.color = Color.white;
                barImg.raycastTarget = false;
                barObj.SetActive(false);

                // Title Label
                GameObject labelObj = new GameObject("NameLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObj.transform.SetParent(nodeObj.transform, false);
                TMP_Text labelTmp = labelObj.GetComponent<TMP_Text>();
                labelTmp.text = LevelTitles[i];
                labelTmp.alignment = TextAlignmentOptions.Center;
                labelTmp.fontSize = 20;
                labelTmp.fontStyle = FontStyles.Bold;
                labelTmp.color = ThemeColors.InkBlack;
                labelTmp.raycastTarget = false;
                SetRect(labelTmp.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -32f), new Vector2(260f, 34f));

                // Stars
                GameObject starsObj = new GameObject("StarsRow", typeof(RectTransform));
                starsObj.transform.SetParent(nodeObj.transform, false);
                SetRect(starsObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -62f), new Vector2(100f, 26f));

                Image[] starImgs = new Image[3];
                float[] sx = { -28f, 0f, 28f };
                for (int s = 0; s < 3; s++)
                {
                    GameObject sObj = new GameObject($"Star_{s}", typeof(RectTransform), typeof(Image));
                    sObj.transform.SetParent(starsObj.transform, false);
                    SetRect(sObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(sx[s], 0f), new Vector2(24f, 24f));
                    Image sImg = sObj.GetComponent<Image>();
                    sImg.sprite = starF;
                    sImg.color = ThemeColors.StarFilled;
                    sImg.raycastTarget = false;
                    starImgs[s] = sImg;
                }

                LevelNode ln = nodeObj.GetComponent<LevelNode>();
                // Initialize via reflection or fields if needed
                SetField(ln, "levelNumber", levelNum);
                SetField(ln, "levelTitle", LevelTitles[i]);
                SetField(ln, "button", btn);
                SetField(ln, "buttonBgImage", btnImg);
                SetField(ln, "numberText", numTmp);
                SetField(ln, "titleLabelText", labelTmp);
                SetField(ln, "lockOverlayObject", lockObj);
                SetField(ln, "barrierObject", barObj);
                SetField(ln, "starFilledSprite", starF);
                SetField(ln, "starEmptySprite", starE);
                SetField(ln, "lockSprite", lockS);
                SetField(ln, "starImages", starImgs);

                int capturedNum = levelNum;
                btn.onClick.AddListener(() => controller?.PlayLevel(capturedNum));
                nodeList.Add(ln);
            }

            // Flag
            GameObject flagGo = new GameObject("CheckeredFinishFlag", typeof(RectTransform), typeof(Image));
            flagGo.transform.SetParent(contentGo.transform, false);
            RectTransform flagRect = flagGo.GetComponent<RectTransform>();
            SetRect(flagRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), AnchorPositions[9] + new Vector2(75f, 25f), new Vector2(64f, 64f));
            Image flagImg = flagGo.GetComponent<Image>();
            flagImg.sprite = MenuAssetProvider.GetFlagSprite();
            flagImg.color = Color.white;
            flagImg.raycastTarget = false;

            // Car
            GameObject carGo = new GameObject("PlayerCarOnMap", typeof(RectTransform), typeof(Image));
            carGo.transform.SetParent(contentGo.transform, false);
            RectTransform carRect = carGo.GetComponent<RectTransform>();
            SetRect(carRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), AnchorPositions[0] + new Vector2(0f, 15f), new Vector2(48f, 76f));
            Image carImg = carGo.GetComponent<Image>();
            carImg.sprite = MenuAssetProvider.GetCarSprite();
            carImg.color = Color.white;
            carImg.raycastTarget = false;
            carGo.transform.SetAsLastSibling();

            // Back to Menu Button (bottom-left)
            GameObject backGo = new GameObject("BackToMenuButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonBounce));
            backGo.transform.SetParent(panel, false);
            RectTransform backRect = backGo.GetComponent<RectTransform>();
            SetRect(backRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(170f, 75f), new Vector2(250f, 75f));

            Image backImg = backGo.GetComponent<Image>();
            backImg.sprite = MenuAssetProvider.GetButtonOutline9Slice();
            backImg.type = Image.Type.Sliced;
            backImg.color = Color.white;
            backImg.raycastTarget = true;

            GameObject backLblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            backLblGo.transform.SetParent(backGo.transform, false);
            TMP_Text backLbl = backLblGo.GetComponent<TMP_Text>();
            backLbl.text = "BACK TO MENU";
            backLbl.alignment = TextAlignmentOptions.Center;
            backLbl.fontSize = 22;
            backLbl.fontStyle = FontStyles.Bold;
            backLbl.color = ThemeColors.InkBlack;
            backLbl.raycastTarget = false;
            SetFullScreen(backLbl.rectTransform);

            Button backBtn = backGo.GetComponent<Button>();
            backBtn.onClick.AddListener(() => controller?.CloseLevelSelect());

            // Controller
            LevelMapController mapController = panel.gameObject.AddComponent<LevelMapController>();
            SetField(mapController, "scrollRect", scrollRect);
            SetField(mapController, "viewportRect", vpRect);
            SetField(mapController, "contentRect", contentRect);
            SetField(mapController, "roadCurveRenderer", roadCurve);
            SetField(mapController, "playerCarRect", carRect);
            SetField(mapController, "playerCarImage", carImg);
            SetField(mapController, "checkeredFlagRect", flagRect);
            SetField(mapController, "backToMenuButton", backBtn);
            SetField(mapController, "levelAnchors", anchorList);
            if (panel.gameObject.activeInHierarchy)
            {
                mapController.RefreshMap();
            }
        }

        private static void SanitizeExistingScrollMap(Transform panel, Transform scrollObj, MainMenuController controller)
        {
            ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.horizontal = true;
                scrollRect.vertical = false;
            }

            // Viewport sanitization: remove legacy Mask and 0-alpha Image, replace with RectMask2D
            Transform vpT = scrollObj.Find("Viewport");
            if (vpT != null)
            {
                Mask mask = vpT.GetComponent<Mask>();
                if (mask != null) UnityEngine.Object.Destroy(mask);

                Image img = vpT.GetComponent<Image>();
                if (img != null) UnityEngine.Object.Destroy(img);

                RectMask2D rmask = vpT.GetComponent<RectMask2D>();
                if (rmask == null) rmask = vpT.gameObject.AddComponent<RectMask2D>();
                rmask.enabled = true;

                SetFullScreen(vpT.GetComponent<RectTransform>());
            }

            // Content sanitization: ensure left-anchored (0, 0.5) and 3800 width
            Transform contentT = vpT != null ? vpT.Find("Content") : scrollObj.Find("Content");
            if (contentT != null)
            {
                RectTransform contentRt = contentT.GetComponent<RectTransform>();
                contentRt.pivot = new Vector2(0f, 0.5f);
                contentRt.anchorMin = new Vector2(0f, 0.5f);
                contentRt.anchorMax = new Vector2(0f, 0.5f);
                contentRt.anchoredPosition = Vector2.zero;
                contentRt.sizeDelta = new Vector2(3800f, 1080f);

                // RoadAnchors
                Transform anchorsRoot = contentT.Find("RoadAnchors");
                List<RectTransform> anchorList = new List<RectTransform>();
                if (anchorsRoot != null)
                {
                    RectTransform arRt = anchorsRoot.GetComponent<RectTransform>();
                    arRt.pivot = new Vector2(0f, 0.5f);
                    arRt.anchorMin = new Vector2(0f, 0.5f);
                    arRt.anchorMax = new Vector2(0f, 0.5f);
                    arRt.anchoredPosition = Vector2.zero;
                    arRt.sizeDelta = new Vector2(3800f, 1080f);

                    for (int i = 0; i < AnchorPositions.Length; i++)
                    {
                        Transform a = anchorsRoot.Find($"Anchor_Level_{i + 1}");
                        if (a != null)
                        {
                            RectTransform art = a.GetComponent<RectTransform>();
                            SetRect(art, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), AnchorPositions[i], new Vector2(20f, 20f));
                            art.pivot = new Vector2(0.5f, 0.5f);
                            anchorList.Add(art);
                        }
                    }
                }

                // WindingRoad
                Transform roadT = contentT.Find("WindingRoad");
                if (roadT != null)
                {
                    RectTransform roadRt = roadT.GetComponent<RectTransform>();
                    roadRt.pivot = new Vector2(0f, 0.5f);
                    roadRt.anchorMin = new Vector2(0f, 0.5f);
                    roadRt.anchorMax = new Vector2(0f, 0.5f);
                    roadRt.anchoredPosition = Vector2.zero;
                    roadRt.sizeDelta = new Vector2(3800f, 1080f);
                    roadT.SetAsFirstSibling();

                    RoadMapCurveRenderer roadCurve = roadT.GetComponent<RoadMapCurveRenderer>();
                    if (roadCurve != null)
                    {
                        if (anchorList.Count > 0) roadCurve.SetAnchors(anchorList);
                        roadCurve.EnsureSubRenderers();
                        roadCurve.RebuildRoad();
                    }
                }

                // LevelNodes
                Transform nodesRoot = contentT.Find("LevelNodes");
                if (nodesRoot != null)
                {
                    RectTransform nrRt = nodesRoot.GetComponent<RectTransform>();
                    nrRt.pivot = new Vector2(0f, 0.5f);
                    nrRt.anchorMin = new Vector2(0f, 0.5f);
                    nrRt.anchorMax = new Vector2(0f, 0.5f);
                    nrRt.anchoredPosition = Vector2.zero;
                    nrRt.sizeDelta = new Vector2(3800f, 1080f);

                    for (int i = 0; i < 10; i++)
                    {
                        int levelNum = i + 1;
                        Transform n = nodesRoot.Find($"LevelNode_{levelNum}");
                        if (n != null)
                        {
                            RectTransform nrt = n.GetComponent<RectTransform>();
                            SetRect(nrt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), AnchorPositions[i], new Vector2(140f, 140f));
                            nrt.pivot = new Vector2(0.5f, 0.5f);

                            Button btn = n.GetComponent<Button>();
                            if (btn != null && controller != null)
                            {
                                int capturedNum = levelNum;
                                btn.onClick.RemoveAllListeners();
                                btn.onClick.AddListener(() => controller.PlayLevel(capturedNum));
                            }

                            LevelNode ln = n.GetComponent<LevelNode>();
                            if (ln != null)
                            {
                                ln.Initialize(levelNum, LevelTitles[i]);
                            }
                        }
                    }
                }

                // Finish Flag
                Transform flagT = contentT.Find("CheckeredFinishFlag");
                if (flagT != null)
                {
                    RectTransform flagRt = flagT.GetComponent<RectTransform>();
                    SetRect(flagRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), AnchorPositions[9] + new Vector2(75f, 25f), new Vector2(64f, 64f));
                    flagRt.pivot = new Vector2(0.5f, 0.5f);
                }

                // Player Car
                Transform carT = contentT.Find("PlayerCarOnMap");
                if (carT != null)
                {
                    RectTransform carRt = carT.GetComponent<RectTransform>();
                    SetRect(carRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), AnchorPositions[0] + new Vector2(0f, 15f), new Vector2(48f, 76f));
                    carRt.pivot = new Vector2(0.5f, 0.5f);
                    carT.SetAsLastSibling();
                }
            }

            // Back to Menu Button
            Transform backT = panel.Find("BackToMenuButton");
            if (backT != null)
            {
                RectTransform backRt = backT.GetComponent<RectTransform>();
                SetRect(backRt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(170f, 75f), new Vector2(250f, 75f));
                Button backBtn = backT.GetComponent<Button>();
                if (backBtn != null && controller != null)
                {
                    backBtn.onClick.RemoveAllListeners();
                    backBtn.onClick.AddListener(() => controller.CloseLevelSelect());
                }
            }

            LevelMapController mapController = panel.GetComponent<LevelMapController>();
            if (mapController != null)
            {
                mapController.AutoFindReferences();
                if (panel.gameObject.activeInHierarchy)
                {
                    mapController.RefreshMap();
                }
            }
        }

        private static void UnblockRaycasts(Canvas canvas)
        {
            Button[] allButtons = canvas.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < allButtons.Length; i++)
            {
                Button btn = allButtons[i];
                Graphic btnGraphic = btn.GetComponent<Graphic>();
                if (btnGraphic != null) btnGraphic.raycastTarget = true;

                Graphic[] childGraphics = btn.GetComponentsInChildren<Graphic>(true);
                for (int c = 0; c < childGraphics.Length; c++)
                {
                    if (childGraphics[c].gameObject != btn.gameObject)
                    {
                        childGraphics[c].raycastTarget = false;
                    }
                }
            }
        }

        private static void SetField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private static void SetFullScreen(RectTransform rt)
        {
            SetRect(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
