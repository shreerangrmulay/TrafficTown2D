using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrafficTown2D.Core;
using UnityEngine.SceneManagement;

namespace TrafficTown2D.Level7
{
    public class Level7CompletionModal : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text starsText;
        [SerializeField] private TMP_Text scoreValueText;
        [SerializeField] private TMP_Text safetyValueText;
        [SerializeField] private TMP_Text focusValueText;
        [SerializeField] private TMP_Text ignoredValueText;
        [SerializeField] private TMP_Text unsafeValueText;
        [SerializeField] private TMP_Text safeStopsValueText;
        [SerializeField] private TMP_Text educationalText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button mainMenuButton;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            WireButtons();
            gameObject.SetActive(false);
        }

        public void WireButtons()
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveAllListeners();
                playAgainButton.onClick.AddListener(PlayAgain);
            }
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(BackToMainMenu);
            }
        }

        public void Show(int score, float safety, float focus, int ignored, int unsafeCount, int safeStops)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            WireButtons();

            if (titleText != null) titleText.text = "LEVEL 7 COMPLETED!";
            if (subtitleText != null) subtitleText.text = "DISTRACTED DRIVING CHALLENGE CERTIFICATE";

            int stars = 3;
            if (safety < 60f || focus < 60f) stars = 1;
            else if (safety < 80f || focus < 80f) stars = 2;

            if (starsText != null)
            {
                starsText.text = stars == 3 ? "*** 3 / 3 STARS ***" : (stars == 2 ? "** 2 / 3 STARS **" : "* 1 / 3 STARS *");
            }

            if (scoreValueText != null) scoreValueText.text = $"FINAL SCORE: {score}";
            if (safetyValueText != null) safetyValueText.text = $"{Mathf.RoundToInt(safety)}%";
            if (focusValueText != null) focusValueText.text = $"{Mathf.RoundToInt(focus)}%";
            if (ignoredValueText != null) ignoredValueText.text = ignored.ToString();
            if (unsafeValueText != null) unsafeValueText.text = unsafeCount.ToString();
            if (safeStopsValueText != null) safeStopsValueText.text = safeStops.ToString();

            if (educationalText != null)
            {
                educationalText.text = "<b>ROAD FIRST.</b>\nYour attention belongs on the road. If a distraction needs your attention, pull over somewhere safe before interacting with it.";
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            Debug.Log($"[Level7CompletionModal] Show called -- Score: {score}, Safety: {safety}%, Focus: {focus}%");
        }

        public void PlayAgain()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Level7");
        }

        public void BackToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
