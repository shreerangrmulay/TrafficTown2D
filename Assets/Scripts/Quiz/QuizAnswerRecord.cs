using System;

namespace TrafficTown2D.Quiz
{
    [Serializable]
    public sealed class QuizAnswerRecord
    {
        public int questionNumber;
        public string questionText;
        public string category;
        public string signSprite;
        public string[] options;
        public int selectedOptionIndex;
        public int correctOptionIndex;
        public bool isCorrect;
        public string explanation;

        public string SelectedOptionText => (options != null && selectedOptionIndex >= 0 && selectedOptionIndex < options.Length)
            ? options[selectedOptionIndex]
            : "No Answer";

        public string CorrectOptionText => (options != null && correctOptionIndex >= 0 && correctOptionIndex < options.Length)
            ? options[correctOptionIndex]
            : "";
    }
}
