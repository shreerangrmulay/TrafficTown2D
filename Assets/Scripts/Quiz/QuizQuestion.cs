using System;
using UnityEngine;

namespace TrafficTown2D.Quiz
{
    [Serializable]
    public class QuizQuestionData
    {
        public int id;
        public string category;
        public string question;
        public string signSprite;
        public string[] options;
        public int correctAnswer; // 0-based index
        public string explanation;
    }

    [Serializable]
    public class QuizQuestionDatabaseData
    {
        public QuizQuestionData[] questions;
    }
}
