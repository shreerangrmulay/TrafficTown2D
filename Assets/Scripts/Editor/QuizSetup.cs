#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using TrafficTown2D.Core;
using TrafficTown2D.Quiz;
using TrafficTown2D.UI;

namespace TrafficTown2D.Editor
{
    public static class QuizSetup
    {
        private const string QuizScenePath = "Assets/Scenes/Quiz.unity";
        private const string RoundedPanelSpritePath = "Assets/UI/RoundedPanel.png";
        private const string SignsFolderPath = "Assets/Resources/Signs";

        [MenuItem("TrafficTown/Setup Quiz")]
        public static void SetupQuiz()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before running TrafficTown -> Setup Quiz.");
                return;
            }

            EnsureAssetFolders();
            ConfigureSignSpriteImporters();

            Scene quizScene;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(QuizScenePath) == null)
            {
                quizScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(quizScene, QuizScenePath);
            }
            else
            {
                quizScene = EditorSceneManager.OpenScene(QuizScenePath, OpenSceneMode.Single);
            }

            if (!quizScene.IsValid())
            {
                Debug.LogError("Could not open Quiz scene at " + QuizScenePath);
                return;
            }

            EnsureCamera();
            EnsureEventSystem();
            SceneLoader sceneLoader = FindOrCreateSceneLoader();
            FindOrCreateGameManager();

            Canvas canvas = FindOrCreateCanvas();
            ClearChildren(canvas.transform);

            Sprite roundedPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPanelSpritePath);

            // 1. Background
            GameObject bg = CreateUIPanel(canvas.transform, "Background", new Color(0.08f, 0.12f, 0.18f, 1f));
            SetFullScreen(bg.GetComponent<RectTransform>());

            // 2. Header Panel
            GameObject headerPanel = CreateUIPanel(canvas.transform, "Header", new Color(0.12f, 0.18f, 0.26f, 0.96f), roundedPanelSprite);
            SetRect(headerPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(880f, 54f));
            AddShadow(headerPanel);

            TMP_Text titleText = CreateUIText(headerPanel.transform, "QuizTitle", "ROAD SAFETY QUIZ", 18, TextAlignmentOptions.MidlineLeft, new Vector2(260f, 36f), new Vector2(24f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            titleText.color = Color.white;
            titleText.fontStyle = FontStyles.Bold;

            TMP_Text counterText = CreateUIText(headerPanel.transform, "QuestionCounter", "QUESTION 1 / 10", 18, TextAlignmentOptions.Center, new Vector2(260f, 36f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            counterText.color = new Color(1f, 0.82f, 0.28f, 1f);
            counterText.fontStyle = FontStyles.Bold;

            TMP_Text scoreText = CreateUIText(headerPanel.transform, "ScoreText", "SCORE: 0", 18, TextAlignmentOptions.MidlineRight, new Vector2(200f, 36f), new Vector2(-24f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            scoreText.color = new Color(0.25f, 0.88f, 0.45f, 1f);
            scoreText.fontStyle = FontStyles.Bold;

            // 3. Progress Bar
            GameObject progressBg = CreateUIPanel(canvas.transform, "ProgressBarBg", new Color(0.14f, 0.20f, 0.28f, 1f), roundedPanelSprite);
            SetRect(progressBg.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(880f, 10f));
            GetOrAdd<RectMask2D>(progressBg);

            GameObject progressFillObj = CreateUIPanel(progressBg.transform, "Fill", new Color(0.22f, 0.82f, 0.42f, 1f), null);
            Image progressFillImage = progressFillObj.GetComponent<Image>();
            progressFillImage.sprite = null;
            progressFillImage.type = Image.Type.Filled;
            progressFillImage.fillMethod = Image.FillMethod.Horizontal;
            progressFillImage.fillAmount = 0.1f;
            SetFullScreen(progressFillObj.GetComponent<RectTransform>());

            // 4. Question Panel
            GameObject questionCard = CreateUIPanel(canvas.transform, "QuestionPanel", new Color(0.13f, 0.19f, 0.28f, 0.98f), roundedPanelSprite);
            SetRect(questionCard.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(880f, 180f));
            AddShadow(questionCard);

            // Category tag badge
            GameObject categoryBadge = CreateUIPanel(questionCard.transform, "CategoryBadge", new Color(0.18f, 0.28f, 0.40f, 0.75f), roundedPanelSprite);
            SetRect(categoryBadge.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -14f), new Vector2(220f, 26f));

            TMP_Text categoryText = CreateUIText(categoryBadge.transform, "Category", "TRAFFIC SIGNALS", 12, TextAlignmentOptions.Center, new Vector2(210f, 22f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            categoryText.color = new Color(0.60f, 0.85f, 1f, 1f);
            categoryText.fontStyle = FontStyles.Bold;

            // Sign container — top-right corner of question card
            GameObject signContainer = CreateUIPanel(questionCard.transform, "SignContainer", new Color(0.08f, 0.12f, 0.18f, 1f), roundedPanelSprite);
            SetRect(signContainer.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -14f), new Vector2(120f, 120f));

            GameObject signImgObj = FindOrCreateChild(signContainer.transform, "SignImage");
            Image signImage = GetOrAdd<Image>(signImgObj);
            signImage.preserveAspect = true;
            signImage.raycastTarget = false;
            SetRect(signImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(82f, 82f));

            TMP_Text signLabel = CreateUIText(signContainer.transform, "SignLabel", "", 10, TextAlignmentOptions.Center, new Vector2(110f, 18f), new Vector2(0f, 6f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            signLabel.color = new Color(1f, 0.85f, 0.30f, 1f);
            signLabel.fontStyle = FontStyles.Bold;

            // Question text
            TMP_Text questionText = CreateUIText(questionCard.transform, "QuestionText", "What should you do when approaching a red traffic signal?", 20, TextAlignmentOptions.TopLeft, new Vector2(700f, 120f), new Vector2(24f, -48f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            questionText.color = Color.white;
            questionText.fontStyle = FontStyles.Bold;

            // 5. Answer Panel (4 Option Buttons stacked vertically)
            GameObject answerPanel = FindOrCreateChild(canvas.transform, "AnswerPanel");
            SetRect(answerPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -286f), new Vector2(880f, 240f));

            QuizAnswerButton[] answerButtons = new QuizAnswerButton[4];
            float buttonHeight = 52f;
            float buttonSpacing = 8f;

            for (int i = 0; i < 4; i++)
            {
                float yPos = -i * (buttonHeight + buttonSpacing);
                GameObject btnObj = CreateUIPanel(answerPanel.transform, "AnswerButton" + (i + 1), new Color(0.12f, 0.36f, 0.56f, 1f), roundedPanelSprite);
                SetRect(btnObj.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, yPos), new Vector2(880f, buttonHeight));

                Image btnImg = btnObj.GetComponent<Image>();
                btnImg.raycastTarget = true;

                Button btn = GetOrAdd<Button>(btnObj);
                btn.targetGraphic = btnImg;
                btn.transition = Selectable.Transition.None;
                AddShadow(btnObj);

                // Letter Badge pill on left
                GameObject letterBadge = CreateUIPanel(btnObj.transform, "LetterBadge", new Color(0.08f, 0.16f, 0.26f, 0.85f), roundedPanelSprite);
                SetRect(letterBadge.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(36f, 36f));

                TMP_Text letterText = CreateUIText(letterBadge.transform, "LetterPrefix", ((char)('A' + i)).ToString(), 18, TextAlignmentOptions.Center, new Vector2(36f, 36f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                letterText.color = new Color(1f, 0.85f, 0.30f, 1f);
                letterText.fontStyle = FontStyles.Bold;

                // Option text inside the button
                TMP_Text optText = CreateUIText(btnObj.transform, "OptionText", "Option " + (i + 1), 16, TextAlignmentOptions.MidlineLeft, new Vector2(810f, 44f), new Vector2(58f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
                optText.color = Color.white;

                QuizAnswerButton answerBtnComponent = GetOrAdd<QuizAnswerButton>(btnObj);
                SetReference(answerBtnComponent, "buttonImage", btnImg);
                SetReference(answerBtnComponent, "letterBadgeImage", letterBadge.GetComponent<Image>());
                SetReference(answerBtnComponent, "optionLabelText", optText);
                SetReference(answerBtnComponent, "optionLetterText", letterText);
                answerButtons[i] = answerBtnComponent;
            }

            // 6. Feedback & Explanation Panel
            GameObject feedbackPanel = CreateUIPanel(canvas.transform, "FeedbackPanel", new Color(0.10f, 0.16f, 0.24f, 0.98f), roundedPanelSprite);
            SetRect(feedbackPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-125f, 26f), new Vector2(610f, 76f));
            AddShadow(feedbackPanel);

            TMP_Text feedbackIcon = CreateUIText(feedbackPanel.transform, "FeedbackIcon", "✓ Correct!", 16, TextAlignmentOptions.MidlineLeft, new Vector2(576f, 24f), new Vector2(18f, -10f), new Vector2(0f, 1f), new Vector2(0f, 1f));

            TMP_Text explanationText = CreateUIText(feedbackPanel.transform, "ExplanationText", "Explanation text goes here.", 13, TextAlignmentOptions.TopLeft, new Vector2(576f, 36f), new Vector2(18f, -36f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            explanationText.color = new Color(0.85f, 0.90f, 0.95f, 1f);

            // 7. Next Question Button
            GameObject nextBtnObj = CreateUIPanel(canvas.transform, "NextButton", new Color(0.92f, 0.52f, 0.15f, 1f), roundedPanelSprite);
            SetRect(nextBtnObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(325f, 26f), new Vector2(220f, 76f));
            
            Image nextBtnImg = nextBtnObj.GetComponent<Image>();
            nextBtnImg.raycastTarget = true;

            Button nextButton = GetOrAdd<Button>(nextBtnObj);
            nextButton.targetGraphic = nextBtnImg;
            nextButton.transition = Selectable.Transition.None;
            AddShadow(nextBtnObj);

            TMP_Text nextBtnLabel = CreateUIText(nextBtnObj.transform, "Label", "NEXT QUESTION", 17, TextAlignmentOptions.Center, new Vector2(200f, 44f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            nextBtnLabel.color = Color.white;
            nextBtnLabel.fontStyle = FontStyles.Bold;

            // 8. Completion Panel Modal Overlay
            GameObject completionOverlay = CreateUIPanel(canvas.transform, "CompletionPanel", new Color(0.04f, 0.06f, 0.10f, 0.88f));
            SetFullScreen(completionOverlay.GetComponent<RectTransform>());

            GameObject completionCard = CreateUIPanel(completionOverlay.transform, "CompletionCard", new Color(0.96f, 0.97f, 0.98f, 1f), roundedPanelSprite);
            SetRect(completionCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 510f));
            AddShadow(completionCard);

            TMP_Text completeHeader = CreateUIText(completionCard.transform, "Header", "🎉 QUIZ COMPLETE!", 28, TextAlignmentOptions.Center, new Vector2(480f, 40f), new Vector2(0f, 195f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            completeHeader.color = new Color(0.10f, 0.15f, 0.22f, 1f);
            completeHeader.fontStyle = FontStyles.Bold;

            TMP_Text finalScoreLabel = CreateUIText(completionCard.transform, "ScoreTitle", "YOUR FINAL SCORE", 13, TextAlignmentOptions.Center, new Vector2(260f, 20f), new Vector2(0f, 150f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            finalScoreLabel.color = new Color(0.45f, 0.50f, 0.58f, 1f);

            TMP_Text finalScoreText = CreateUIText(completionCard.transform, "FinalScore", "80 / 100", 42, TextAlignmentOptions.Center, new Vector2(300f, 50f), new Vector2(0f, 105f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            finalScoreText.color = new Color(0.10f, 0.15f, 0.22f, 1f);
            finalScoreText.fontStyle = FontStyles.Bold;

            GameObject statsRow = CreateUIPanel(completionCard.transform, "StatisticsRow", Color.clear);
            SetRect(statsRow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(430f, 75f));

            TMP_Text correctCountText = CreateStatCard(statsRow.transform, "CorrectCard", "CORRECT ANSWERS", "8", new Vector2(-110f, 0f), new Color(0.18f, 0.67f, 0.32f, 1f), roundedPanelSprite);
            TMP_Text wrongCountText = CreateStatCard(statsRow.transform, "WrongCard", "WRONG ANSWERS", "2", new Vector2(110f, 0f), new Color(0.92f, 0.45f, 0.18f, 1f), roundedPanelSprite);

            TMP_Text perfMsgText = CreateUIText(completionCard.transform, "PerformanceMessage", "Great job! Keep practicing your traffic rules.", 15, TextAlignmentOptions.Center, new Vector2(460f, 45f), new Vector2(0f, -45f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            perfMsgText.color = new Color(0.20f, 0.55f, 0.35f, 1f);
            perfMsgText.fontStyle = FontStyles.Bold;

            // Prominent "Review Answers / Question List" button
            Button reviewBtn = GetOrAddChildButton(completionCard.transform, "ReviewButton", "📋 REVIEW ALL ANSWERS", new Vector2(0f, -125f), roundedPanelSprite);
            RectTransform reviewR = reviewBtn.GetComponent<RectTransform>();
            reviewR.sizeDelta = new Vector2(340f, 46f);
            reviewBtn.GetComponent<Image>().color = new Color(0.14f, 0.52f, 0.70f, 1f);

            Button retryBtn = GetOrAddChildButton(completionCard.transform, "RetryButton", "RETRY QUIZ", new Vector2(-115f, -190f), roundedPanelSprite);
            RectTransform retryR = retryBtn.GetComponent<RectTransform>();
            retryR.sizeDelta = new Vector2(200f, 46f);
            retryBtn.GetComponent<Image>().color = new Color(0.92f, 0.50f, 0.15f, 1f);

            Button menuBtn = GetOrAddChildButton(completionCard.transform, "MainMenuButton", "MAIN MENU", new Vector2(115f, -190f), roundedPanelSprite);
            RectTransform menuR = menuBtn.GetComponent<RectTransform>();
            menuR.sizeDelta = new Vector2(200f, 46f);
            menuBtn.GetComponent<Image>().color = new Color(0.12f, 0.45f, 0.72f, 1f);

            // 9. Review Panel Modal Overlay
            GameObject reviewOverlay = CreateUIPanel(canvas.transform, "ReviewPanel", new Color(0.04f, 0.06f, 0.10f, 0.94f));
            SetFullScreen(reviewOverlay.GetComponent<RectTransform>());

            GameObject reviewCard = CreateUIPanel(reviewOverlay.transform, "ReviewCard", new Color(0.09f, 0.13f, 0.19f, 0.98f), roundedPanelSprite);
            SetRect(reviewCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940f, 640f));
            AddShadow(reviewCard);

            TMP_Text reviewTitle = CreateUIText(reviewCard.transform, "Title", "📋 QUESTION LIST & REVIEW", 22, TextAlignmentOptions.Center, new Vector2(500f, 32f), new Vector2(0f, 285f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            reviewTitle.color = Color.white;
            reviewTitle.fontStyle = FontStyles.Bold;

            TMP_Text reviewScoreSummary = CreateUIText(reviewCard.transform, "ScoreSummary", "Score: 0 / 100", 15, TextAlignmentOptions.Center, new Vector2(500f, 24f), new Vector2(0f, 255f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            reviewScoreSummary.color = new Color(1f, 0.85f, 0.30f, 1f);

            // Scroll View for question cards
            GameObject scrollView = FindOrCreateChild(reviewCard.transform, "ScrollView");
            SetRect(scrollView.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(900f, 460f));
            ScrollRect scrollRect = GetOrAdd<ScrollRect>(scrollView);
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            GameObject viewport = CreateUIPanel(scrollView.transform, "Viewport", Color.clear);
            SetFullScreen(viewport.GetComponent<RectTransform>());
            GetOrAdd<RectMask2D>(viewport);
            scrollRect.viewport = viewport.GetComponent<RectTransform>();

            // Content
            GameObject content = FindOrCreateChild(viewport.transform, "Content");
            RectTransform contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0f, 100f);

            VerticalLayoutGroup contentVLG = GetOrAdd<VerticalLayoutGroup>(content);
            contentVLG.padding = new RectOffset(12, 12, 12, 12);
            contentVLG.spacing = 10;
            contentVLG.childControlWidth = true;
            contentVLG.childControlHeight = false;
            contentVLG.childForceExpandWidth = true;
            contentVLG.childForceExpandHeight = false;

            ContentSizeFitter contentCSF = GetOrAdd<ContentSizeFitter>(content);
            contentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;

            // Bottom action buttons in review
            Button closeReviewBtn = GetOrAddChildButton(reviewCard.transform, "CloseReviewButton", "← BACK TO RESULTS", new Vector2(-130f, -285f), roundedPanelSprite);
            closeReviewBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 44f);
            closeReviewBtn.GetComponent<Image>().color = new Color(0.18f, 0.45f, 0.65f, 1f);

            Button revMenuBtn = GetOrAddChildButton(reviewCard.transform, "ReviewMenuButton", "MAIN MENU", new Vector2(130f, -285f), roundedPanelSprite);
            revMenuBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 44f);
            revMenuBtn.GetComponent<Image>().color = new Color(0.12f, 0.55f, 0.84f, 1f);

            reviewOverlay.SetActive(false);

            // 10. Wire Components on UI Controller and Quiz Manager
            QuizUIController uiController = GetOrAdd<QuizUIController>(canvas.gameObject);

            SetReference(uiController, "questionCounterText", counterText);
            SetReference(uiController, "scoreText", scoreText);
            SetReference(uiController, "progressBarFill", progressFillImage);
            SetReference(uiController, "categoryText", categoryText);
            SetReference(uiController, "questionText", questionText);
            SetReference(uiController, "signContainer", signContainer);
            SetReference(uiController, "signImage", signImage);
            SetReference(uiController, "signLabelText", signLabel);
            SetObjectArray(uiController, "answerButtons", answerButtons);
            SetReference(uiController, "feedbackPanel", feedbackPanel);
            SetReference(uiController, "feedbackIconText", feedbackIcon);
            SetReference(uiController, "explanationText", explanationText);
            SetReference(uiController, "nextButton", nextButton);
            SetReference(uiController, "completionOverlay", completionOverlay);
            SetReference(uiController, "finalScoreText", finalScoreText);
            SetReference(uiController, "correctCountText", correctCountText);
            SetReference(uiController, "wrongCountText", wrongCountText);
            SetReference(uiController, "performanceMessageText", perfMsgText);
            SetReference(uiController, "reviewButton", reviewBtn);
            SetReference(uiController, "retryButton", retryBtn);
            SetReference(uiController, "mainMenuButton", menuBtn);

            SetReference(uiController, "reviewOverlay", reviewOverlay);
            SetReference(uiController, "reviewScoreSummaryText", reviewScoreSummary);
            SetReference(uiController, "reviewContentContainer", content.transform);
            SetReference(uiController, "closeReviewButton", closeReviewBtn);
            SetReference(uiController, "reviewRetryButton", retryBtn);
            SetReference(uiController, "reviewMainMenuButton", revMenuBtn);

            QuizManager quizManager = GetOrAdd<QuizManager>(FindOrCreate("QuizManager"));
            SetReference(quizManager, "uiController", uiController);
            SetReference(quizManager, "sceneLoader", sceneLoader);

            completionOverlay.SetActive(false);
            reviewOverlay.SetActive(false);

            EnsureBuildSettingsScenes();

            EditorSceneManager.MarkSceneDirty(quizScene);
            EditorSceneManager.SaveScene(quizScene);
            Selection.activeGameObject = canvas.gameObject;
            Debug.Log("TrafficTown Quiz setup completed successfully!");
        }

        private static void EnsureCamera()
        {
            Camera camera = Object.FindAnyObjectByType<Camera>();
            if (camera == null) camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.10f, 0.15f, 0.22f, 1f);
            camera.tag = "MainCamera";
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            var inputSystemModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (inputSystemModule == null && standaloneModule == null)
            {
                eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        private static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            canvas.name = "Canvas";
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            return canvas;
        }

        private static SceneLoader FindOrCreateSceneLoader()
        {
            SceneLoader sceneLoader = Object.FindAnyObjectByType<SceneLoader>();
            if (sceneLoader != null) return sceneLoader;

            GameObject services = GameObject.Find("MainMenu Services");
            if (services == null) services = new GameObject("MainMenu Services");
            return services.AddComponent<SceneLoader>();
        }

        private static GameManager FindOrCreateGameManager()
        {
            GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
            if (gameManager != null) return gameManager;

            GameObject services = GameObject.Find("MainMenu Services");
            if (services == null) services = new GameObject("MainMenu Services");
            return services.AddComponent<GameManager>();
        }

        private static void EnsureBuildSettingsScenes()
        {
            BuildSettingsUtility.EnsureAllScenesInBuildSettings();
        }

        private static GameObject FindOrCreateChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null) return child.gameObject;

            GameObject childObject = new GameObject(childName, typeof(RectTransform));
            childObject.transform.SetParent(parent, false);
            return childObject;
        }

        private static GameObject FindOrCreate(string name)
        {
            GameObject found = GameObject.Find(name);
            return found != null ? found : new GameObject(name);
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Color color, Sprite sprite = null)
        {
            GameObject panel = FindOrCreateChild(parent, name);
            Image image = GetOrAdd<Image>(panel);
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return panel;
        }

        private static TextMeshProUGUI CreateUIText(Transform parent, string name, string value, int size, TextAlignmentOptions alignment, Vector2 dimensions, Vector2 position, Vector2 anchor, Vector2 pivot)
        {
            GameObject textObject = FindOrCreateChild(parent, name);
            TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(textObject);
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            SetRect(text.rectTransform, anchor, anchor, pivot, position, dimensions);
            return text;
        }

        private static TextMeshProUGUI CreateUIText(Transform parent, string name, string value, int size, TextAlignmentOptions alignment, Vector2 dimensions, Vector2 position, Vector2 anchor)
        {
            return CreateUIText(parent, name, value, size, alignment, dimensions, position, anchor, anchor);
        }

        private static TMP_Text CreateStatCard(Transform parent, string name, string label, string value, Vector2 position, Color accentColor, Sprite sprite)
        {
            GameObject card = CreateUIPanel(parent, name, new Color(0.92f, 0.95f, 0.98f, 1f), sprite);
            SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(190f, 75f));

            TMP_Text labelText = CreateUIText(card.transform, "Label", label, 12, TextAlignmentOptions.Center, new Vector2(170f, 20f), new Vector2(0f, 15f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            labelText.color = accentColor;
            labelText.fontStyle = FontStyles.Bold;

            TMP_Text valueText = CreateUIText(card.transform, "Value", value, 28, TextAlignmentOptions.Center, new Vector2(120f, 32f), new Vector2(0f, -14f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            valueText.color = new Color(0.10f, 0.15f, 0.22f, 1f);
            valueText.fontStyle = FontStyles.Bold;
            return valueText;
        }

        private static Button GetOrAddChildButton(Transform parent, string name, string label, Vector2 position, Sprite sprite)
        {
            GameObject btnObj = CreateUIPanel(parent, name, new Color(0.12f, 0.55f, 0.84f, 1f), sprite);
            SetRect(btnObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(200f, 48f));
            
            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.raycastTarget = true;

            Button button = GetOrAdd<Button>(btnObj);
            button.targetGraphic = btnImg;
            button.transition = Selectable.Transition.None;
            AddShadow(btnObj);

            TMP_Text labelText = CreateUIText(btnObj.transform, "Text", label, 15, TextAlignmentOptions.Center, new Vector2(180f, 36f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            labelText.color = Color.white;
            labelText.fontStyle = FontStyles.Bold;
            return button;
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void SetFullScreen(RectTransform rectTransform)
        {
            SetRect(rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            rectTransform.localScale = Vector3.one;
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            SetRect(rectTransform, anchorMin, anchorMax, new Vector2(anchorMin.x, anchorMin.y), position, size);
        }

        private static void AddShadow(GameObject objectRoot)
        {
            Shadow shadow = GetOrAdd<Shadow>(objectRoot);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.20f);
            shadow.effectDistance = new Vector2(0f, -4f);
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static void EnsureAssetFolders()
        {
            string[] folders = { "Assets/Scenes", "Assets/JSON", "Assets/Resources", "Assets/Resources/Signs", "Assets/UI" };
            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
        }

        private static void ConfigureSignSpriteImporters()
        {
            if (!Directory.Exists(SignsFolderPath)) return;
            string[] signFiles = Directory.GetFiles(SignsFolderPath, "*.png");
            bool modified = false;
            foreach (string file in signFiles)
            {
                string assetPath = file.Replace('\\', '/');
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                    modified = true;
                }
            }
            if (modified)
            {
                AssetDatabase.Refresh();
            }
        }

        private static void SetReference(Object target, string fieldName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty prop = serialized.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetObjectArray(Object target, string fieldName, Object[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty prop = serialized.FindProperty(fieldName);
            if (prop != null)
            {
                prop.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
