using System.Collections.Generic;
using UnityEngine;
using TrafficTown2D.Core;

namespace TrafficTown2D.Quiz
{
    public sealed class QuizManager : MonoBehaviour
    {
        [SerializeField] private QuizUIController uiController;
        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private int totalQuizQuestions = 10;
        [SerializeField] private int pointsPerCorrectAnswer = 10;

        private List<QuizQuestionData> activeQuestions;
        private readonly List<QuizAnswerRecord> answerRecords = new List<QuizAnswerRecord>();
        private int currentQuestionIndex;
        private int currentScore;
        private int correctCount;
        private int wrongCount;
        private bool hasAnsweredCurrentQuestion;

        private void Start()
        {
            if (uiController == null)
            {
                uiController = FindAnyObjectByType<QuizUIController>();
            }

            if (sceneLoader == null)
            {
                sceneLoader = FindAnyObjectByType<SceneLoader>();
            }

            if (uiController != null)
            {
                uiController.BindNextButtonCallback(OnNextButtonClicked);
            }

            StartNewQuiz();
        }

        public void StartNewQuiz()
        {
            activeQuestions = QuizQuestionDatabase.GetRandomQuestions(totalQuizQuestions);
            answerRecords.Clear();
            currentQuestionIndex = 0;
            currentScore = 0;
            correctCount = 0;
            wrongCount = 0;
            hasAnsweredCurrentQuestion = false;

            if (activeQuestions == null || activeQuestions.Count == 0)
            {
                Debug.LogError("[QuizManager] No questions loaded from database!");
                return;
            }

            DisplayCurrentQuestion();
        }

        private void DisplayCurrentQuestion()
        {
            hasAnsweredCurrentQuestion = false;
            if (currentQuestionIndex < 0 || currentQuestionIndex >= activeQuestions.Count)
            {
                FinishQuiz();
                return;
            }

            QuizQuestionData currentData = activeQuestions[currentQuestionIndex];
            if (uiController != null)
            {
                uiController.DisplayQuestion(currentData, currentQuestionIndex + 1, activeQuestions.Count, currentScore);
                uiController.BindAnswerCallbacks(OnOptionSelected);
            }
        }

        private void OnOptionSelected(int selectedOptionIndex)
        {
            if (hasAnsweredCurrentQuestion) return;
            hasAnsweredCurrentQuestion = true;

            QuizQuestionData currentData = activeQuestions[currentQuestionIndex];
            bool isCorrect = selectedOptionIndex == currentData.correctAnswer;

            if (isCorrect)
            {
                currentScore += pointsPerCorrectAnswer;
                correctCount++;
            }
            else
            {
                wrongCount++;
            }

            QuizAnswerRecord record = new QuizAnswerRecord
            {
                questionNumber = currentQuestionIndex + 1,
                questionText = currentData.question,
                category = currentData.category,
                signSprite = currentData.signSprite,
                options = currentData.options,
                selectedOptionIndex = selectedOptionIndex,
                correctOptionIndex = currentData.correctAnswer,
                isCorrect = isCorrect,
                explanation = currentData.explanation
            };
            answerRecords.Add(record);

            if (uiController != null)
            {
                uiController.DisplayFeedback(selectedOptionIndex, currentData.correctAnswer, isCorrect, currentData.explanation, currentScore);
            }
        }

        private void OnNextButtonClicked()
        {
            if (!hasAnsweredCurrentQuestion) return;

            currentQuestionIndex++;
            if (currentQuestionIndex < activeQuestions.Count)
            {
                DisplayCurrentQuestion();
            }
            else
            {
                FinishQuiz();
            }
        }

        private void FinishQuiz()
        {
            if (uiController != null)
            {
                uiController.DisplayCompletion(currentScore, correctCount, wrongCount, answerRecords, StartNewQuiz, ReturnToMainMenu);
            }
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            if (sceneLoader != null)
            {
                sceneLoader.LoadMainMenu();
                return;
            }

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadMainMenu();
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneLoader.MainMenuSceneName);
        }
    }
}
