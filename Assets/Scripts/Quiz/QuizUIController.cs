using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrafficTown2D.Quiz
{
    public sealed class QuizUIController : MonoBehaviour
    {
        [Header("Header Elements")]
        [SerializeField] private TMP_Text questionCounterText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Image progressBarFill;

        [Header("Question Card")]
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private GameObject signContainer;
        [SerializeField] private Image signImage;
        [SerializeField] private TMP_Text signLabelText;

        [Header("Answer Buttons")]
        [SerializeField] private QuizAnswerButton[] answerButtons;

        [Header("Feedback & Explanation")]
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private TMP_Text feedbackIconText;
        [SerializeField] private TMP_Text explanationText;
        [SerializeField] private Button nextButton;

        [Header("Completion Modal")]
        [SerializeField] private GameObject completionOverlay;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text correctCountText;
        [SerializeField] private TMP_Text wrongCountText;
        [SerializeField] private TMP_Text performanceMessageText;
        [SerializeField] private Button reviewButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Review Modal")]
        [SerializeField] private GameObject reviewOverlay;
        [SerializeField] private TMP_Text reviewScoreSummaryText;
        [SerializeField] private Transform reviewContentContainer;
        [SerializeField] private Button closeReviewButton;
        [SerializeField] private Button reviewRetryButton;
        [SerializeField] private Button reviewMainMenuButton;

        private List<QuizAnswerRecord> currentAnswerRecords;
        private Action cachedRetryAction;
        private Action cachedMainMenuAction;

        private static readonly Color NextButtonNormalColor = new Color(0.92f, 0.52f, 0.15f, 1f);
        private static readonly Color NextButtonDimColor = new Color(0.40f, 0.30f, 0.20f, 0.60f);

        public void DisplayQuestion(QuizQuestionData questionData, int currentNumber, int totalQuestions, int currentScore)
        {
            if (completionOverlay != null) completionOverlay.SetActive(false);
            if (reviewOverlay != null) reviewOverlay.SetActive(false);

            if (questionCounterText != null)
            {
                questionCounterText.text = $"QUESTION {currentNumber} / {totalQuestions}";
            }

            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {currentScore}";
            }

            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = (float)currentNumber / totalQuestions;
            }

            if (categoryText != null)
            {
                categoryText.text = !string.IsNullOrEmpty(questionData.category) ? questionData.category.ToUpper() : "ROAD SAFETY";
            }

            // Handle Sign Image / Sign Container
            bool hasSign = !string.IsNullOrEmpty(questionData.signSprite);
            if (signContainer != null)
            {
                signContainer.SetActive(hasSign);
            }

            if (questionText != null)
            {
                questionText.text = questionData.question;
                questionText.rectTransform.sizeDelta = new Vector2(hasSign ? 700f : 830f, 120f);
            }

            if (hasSign)
            {
                Sprite loadedSprite = Resources.Load<Sprite>($"Signs/{questionData.signSprite}");
                if (loadedSprite == null)
                {
                    loadedSprite = Resources.Load<Sprite>(questionData.signSprite);
                }

                if (signImage != null)
                {
                    if (loadedSprite != null)
                    {
                        signImage.sprite = loadedSprite;
                        signImage.enabled = true;
                    }
                    else
                    {
                        signImage.enabled = false;
                    }
                }

                if (signLabelText != null)
                {
                    signLabelText.text = questionData.signSprite;
                }
            }

            // Hide feedback panel and disable Next button initially
            if (feedbackPanel != null) feedbackPanel.SetActive(false);
            if (nextButton != null)
            {
                nextButton.interactable = false;
                Image nextImg = nextButton.GetComponent<Image>();
                if (nextImg != null) nextImg.color = NextButtonDimColor;

                TMP_Text nextLabel = nextButton.GetComponentInChildren<TMP_Text>();
                if (nextLabel != null)
                {
                    nextLabel.text = currentNumber == totalQuestions ? "FINISH QUIZ" : "NEXT QUESTION";
                    nextLabel.color = new Color(0.80f, 0.80f, 0.80f, 0.70f);
                }
            }

            // Setup Answer Buttons
            if (answerButtons != null && questionData.options != null)
            {
                for (int i = 0; i < answerButtons.Length; i++)
                {
                    if (i < questionData.options.Length && answerButtons[i] != null)
                    {
                        answerButtons[i].gameObject.SetActive(true);
                        int optionIdx = i;
                        answerButtons[i].Setup(optionIdx, questionData.options[i], null);
                    }
                    else if (answerButtons[i] != null)
                    {
                        answerButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        public void BindAnswerCallbacks(Action<int> onOptionSelected)
        {
            if (answerButtons == null) return;
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] != null)
                {
                    int idx = i;
                    Transform optTextT = answerButtons[i].transform.Find("OptionText");
                    string currentText = optTextT != null ? optTextT.GetComponent<TMP_Text>()?.text : "";
                    answerButtons[i].Setup(idx, currentText, onOptionSelected);
                }
            }
        }

        public void DisplayFeedback(int selectedIndex, int correctIndex, bool isCorrect, string explanation, int updatedScore)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {updatedScore}";
            }

            // Update Answer Buttons visual state with immediate color change
            if (answerButtons != null)
            {
                for (int i = 0; i < answerButtons.Length; i++)
                {
                    if (answerButtons[i] != null)
                    {
                        answerButtons[i].SetState(i == selectedIndex, i == correctIndex, true);
                    }
                }
            }

            // Display Feedback & Explanation text
            if (feedbackPanel != null)
            {
                feedbackPanel.SetActive(true);
            }

            if (feedbackIconText != null)
            {
                if (isCorrect)
                {
                    feedbackIconText.text = "<color=#2ECC71>✓ Correct! (+10 pts)</color>";
                }
                else
                {
                    feedbackIconText.text = "<color=#E74C3C>✗ Not quite.</color>";
                }
            }

            if (explanationText != null)
            {
                explanationText.text = explanation;
            }

            // Enable Next Question Button with active glowing color
            if (nextButton != null)
            {
                nextButton.interactable = true;
                Image nextImg = nextButton.GetComponent<Image>();
                if (nextImg != null) nextImg.color = NextButtonNormalColor;

                TMP_Text nextLabel = nextButton.GetComponentInChildren<TMP_Text>();
                if (nextLabel != null)
                {
                    nextLabel.color = Color.white;
                }
            }
        }

        public void DisplayCompletion(int finalScore, int correctCount, int wrongCount, List<QuizAnswerRecord> records, Action onRetry, Action onMainMenu)
        {
            currentAnswerRecords = records;
            cachedRetryAction = onRetry;
            cachedMainMenuAction = onMainMenu;

            if (reviewOverlay != null) reviewOverlay.SetActive(false);

            if (completionOverlay != null)
            {
                completionOverlay.SetActive(true);
                completionOverlay.transform.SetAsLastSibling();
            }

            if (finalScoreText != null)
            {
                finalScoreText.text = $"{finalScore} / 100";
            }

            if (correctCountText != null)
            {
                correctCountText.text = correctCount.ToString();
            }

            if (wrongCountText != null)
            {
                wrongCountText.text = wrongCount.ToString();
            }

            if (performanceMessageText != null)
            {
                performanceMessageText.text = GetPerformanceMessage(finalScore);
            }

            if (reviewButton != null)
            {
                reviewButton.onClick.RemoveAllListeners();
                reviewButton.onClick.AddListener(OpenReviewPanel);
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(() => onRetry?.Invoke());
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(() => onMainMenu?.Invoke());
            }
        }

        public void OpenReviewPanel()
        {
            if (reviewOverlay == null) return;

            reviewOverlay.SetActive(true);
            reviewOverlay.transform.SetAsLastSibling();

            if (reviewScoreSummaryText != null)
            {
                int correct = 0;
                int total = currentAnswerRecords != null ? currentAnswerRecords.Count : 0;
                if (currentAnswerRecords != null)
                {
                    foreach (var r in currentAnswerRecords)
                    {
                        if (r.isCorrect) correct++;
                    }
                }
                reviewScoreSummaryText.text = $"Score: {correct * 10} / {total * 10}   ({correct} Correct, {total - correct} Wrong)";
            }

            if (closeReviewButton != null)
            {
                closeReviewButton.onClick.RemoveAllListeners();
                closeReviewButton.onClick.AddListener(() =>
                {
                    reviewOverlay.SetActive(false);
                });
            }

            if (reviewRetryButton != null)
            {
                reviewRetryButton.onClick.RemoveAllListeners();
                reviewRetryButton.onClick.AddListener(() =>
                {
                    reviewOverlay.SetActive(false);
                    cachedRetryAction?.Invoke();
                });
            }

            if (reviewMainMenuButton != null)
            {
                reviewMainMenuButton.onClick.RemoveAllListeners();
                reviewMainMenuButton.onClick.AddListener(() =>
                {
                    reviewOverlay.SetActive(false);
                    cachedMainMenuAction?.Invoke();
                });
            }

            PopulateReviewCards();
        }

        private void PopulateReviewCards()
        {
            if (reviewContentContainer == null || currentAnswerRecords == null) return;

            // Clear old children
            for (int i = reviewContentContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(reviewContentContainer.GetChild(i).gameObject);
            }

            Sprite roundedSprite = Resources.Load<Sprite>("UI/RoundedPanel");

            for (int i = 0; i < currentAnswerRecords.Count; i++)
            {
                QuizAnswerRecord record = currentAnswerRecords[i];
                CreateReviewCardItem(reviewContentContainer, record, i + 1, roundedSprite);
            }
        }

        private void CreateReviewCardItem(Transform parent, QuizAnswerRecord record, int questionNum, Sprite sprite)
        {
            GameObject cardObj = new GameObject($"ReviewCard_{questionNum}", typeof(RectTransform));
            cardObj.transform.SetParent(parent, false);

            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = sprite;
            cardImg.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            cardImg.color = new Color(0.12f, 0.17f, 0.25f, 0.98f);
            cardImg.raycastTarget = false;

            VerticalLayoutGroup vlg = cardObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 14, 14);
            vlg.spacing = 6;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = cardObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement cardLe = cardObj.AddComponent<LayoutElement>();
            cardLe.preferredWidth = 840f;

            // Header line: Question number + Category + Status Badge
            string statusBadge = record.isCorrect 
                ? "<color=#2ECC71><b>✓ CORRECT (+10)</b></color>" 
                : "<color=#E74C3C><b>✗ WRONG (0)</b></color>";
            string headerStr = $"<b>QUESTION {questionNum}</b>  •  <color=#85C1E9>{record.category.ToUpper()}</color>        {statusBadge}";
            CreateReviewText(cardObj.transform, "Header", headerStr, 14, FontStyles.Normal, Color.white);

            // Question Text
            CreateReviewText(cardObj.transform, "QuestionText", record.questionText, 16, FontStyles.Bold, Color.white);

            // User Answer line
            string userLetter = record.selectedOptionIndex >= 0 ? ((char)('A' + record.selectedOptionIndex)).ToString() : "-";
            string userAnsStr;
            if (record.isCorrect)
            {
                userAnsStr = $"<color=#2ECC71><b>Your Answer:</b>  {userLetter}. {record.SelectedOptionText}</color>";
            }
            else
            {
                userAnsStr = $"<color=#E74C3C><b>Your Answer:</b>  {userLetter}. {record.SelectedOptionText}</color>";
            }
            CreateReviewText(cardObj.transform, "UserAnswer", userAnsStr, 15, FontStyles.Normal, Color.white);

            // Correct Answer line (if wrong)
            if (!record.isCorrect)
            {
                string correctLetter = ((char)('A' + record.correctOptionIndex)).ToString();
                string correctAnsStr = $"<color=#2ECC71><b>Correct Answer:</b>  {correctLetter}. {record.CorrectOptionText}</color>";
                CreateReviewText(cardObj.transform, "CorrectAnswer", correctAnsStr, 15, FontStyles.Bold, Color.white);
            }

            // Explanation line
            if (!string.IsNullOrEmpty(record.explanation))
            {
                string explStr = $"<color=#AAB7B8><i>💡 Tip: {record.explanation}</i></color>";
                CreateReviewText(cardObj.transform, "Explanation", explStr, 13, FontStyles.Italic, Color.white);
            }
        }

        private TMP_Text CreateReviewText(Transform parent, string name, string content, float size, FontStyles style, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);

            TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.enableWordWrapping = true;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;

            LayoutElement le = obj.AddComponent<LayoutElement>();
            le.preferredWidth = 800f;

            return text;
        }

        public void BindNextButtonCallback(Action onNextClicked)
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(() => onNextClicked?.Invoke());
            }
        }

        private static string GetPerformanceMessage(int score)
        {
            if (score >= 90)
                return "Excellent! You understand road safety very well!";
            if (score >= 70)
                return "Great job! Keep practicing your traffic rules.";
            if (score >= 50)
                return "Good effort! Review the road safety rules and try again.";
            return "Keep learning! Practice the traffic rules and try again.";
        }
    }
}
