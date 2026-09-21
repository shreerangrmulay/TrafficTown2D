using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TrafficTown2D.Quiz
{
    public static class QuizQuestionDatabase
    {
        private const string JsonRelativePath = "Assets/JSON/QuizQuestions.json";
        private const string ResourcePath = "QuizQuestions";

        public static List<QuizQuestionData> LoadAllQuestions()
        {
            string jsonText = string.Empty;

            TextAsset resourceAsset = Resources.Load<TextAsset>(ResourcePath);
            if (resourceAsset != null && !string.IsNullOrEmpty(resourceAsset.text))
            {
                jsonText = resourceAsset.text;
            }
            else
            {
                string fullPath = Path.Combine(Application.dataPath, "JSON/QuizQuestions.json");
                if (File.Exists(fullPath))
                {
                    jsonText = File.ReadAllText(fullPath);
                }
                else if (File.Exists(JsonRelativePath))
                {
                    jsonText = File.ReadAllText(JsonRelativePath);
                }
            }

            if (string.IsNullOrEmpty(jsonText))
            {
                Debug.LogError("[QuizQuestionDatabase] Could not load QuizQuestions JSON! Using hardcoded fallback questions.");
                return GetFallbackQuestions();
            }

            try
            {
                QuizQuestionDatabaseData dbData = JsonUtility.FromJson<QuizQuestionDatabaseData>(jsonText);
                if (dbData != null && dbData.questions != null && dbData.questions.Length > 0)
                {
                    return new List<QuizQuestionData>(dbData.questions);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuizQuestionDatabase] Error parsing JSON: {ex.Message}");
            }

            return GetFallbackQuestions();
        }

        public static List<QuizQuestionData> GetRandomQuestions(int count)
        {
            List<QuizQuestionData> pool = LoadAllQuestions();
            List<QuizQuestionData> selected = new List<QuizQuestionData>();

            if (pool == null || pool.Count == 0)
            {
                return selected;
            }

            // Fisher-Yates Shuffle
            System.Random rng = new System.Random();
            int n = pool.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = pool[k];
                pool[k] = pool[n];
                pool[n] = value;
            }

            int takeCount = Mathf.Min(count, pool.Count);
            for (int i = 0; i < takeCount; i++)
            {
                selected.Add(pool[i]);
            }

            return selected;
        }

        private static List<QuizQuestionData> GetFallbackQuestions()
        {
            return new List<QuizQuestionData>
            {
                new QuizQuestionData
                {
                    id = 1,
                    category = "Traffic Signals",
                    question = "What should you do when the pedestrian signal is RED?",
                    signSprite = "",
                    options = new[] { "Cross immediately", "Wait for green WALK signal", "Run fast", "Use mobile phone" },
                    correctAnswer = 1,
                    explanation = "Wait for the green WALK pedestrian signal before crossing."
                },
                new QuizQuestionData
                {
                    id = 2,
                    category = "Zebra Crossings",
                    question = "What is the primary purpose of a zebra crossing?",
                    signSprite = "",
                    options = new[] { "Parking spot", "Safe crossing zone for pedestrians", "Race track", "Street art" },
                    correctAnswer = 1,
                    explanation = "Zebra crossings provide a designated safe path across roads."
                },
                new QuizQuestionData
                {
                    id = 3,
                    category = "Pedestrian Safety",
                    question = "What is the golden rule before stepping onto a road?",
                    signSprite = "",
                    options = new[] { "Run without stopping", "STOP, LOOK left-right-left, and LISTEN", "Jump", "Whistle" },
                    correctAnswer = 1,
                    explanation = "Always stop, look left-right-left, and listen for traffic."
                }
            };
        }
    }
}
