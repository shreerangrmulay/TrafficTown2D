using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrafficTown2D.Quiz
{
    [RequireComponent(typeof(Button))]
    public sealed class QuizAnswerButton : MonoBehaviour
    {
        [SerializeField] private Image buttonImage;
        [SerializeField] private Image letterBadgeImage;
        [SerializeField] private TMP_Text optionLabelText;
        [SerializeField] private TMP_Text optionLetterText;

        private Button button;
        private int optionIndex;
        private Action<int> onClickCallback;
        private string cachedLetter = "A";

        private static readonly Color NormalBtnColor = new Color(0.12f, 0.36f, 0.56f, 1f);
        private static readonly Color NormalBadgeColor = new Color(0.08f, 0.16f, 0.26f, 0.90f);
        private static readonly Color CorrectBtnColor = new Color(0.16f, 0.70f, 0.32f, 1f);
        private static readonly Color CorrectBadgeColor = new Color(0.09f, 0.44f, 0.20f, 1f);
        private static readonly Color WrongBtnColor = new Color(0.86f, 0.22f, 0.18f, 1f);
        private static readonly Color WrongBadgeColor = new Color(0.52f, 0.12f, 0.10f, 1f);
        private static readonly Color DimBtnColor = new Color(0.10f, 0.14f, 0.20f, 0.50f);
        private static readonly Color DimBadgeColor = new Color(0.08f, 0.10f, 0.14f, 0.50f);

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
            {
                button.transition = Selectable.Transition.None;
            }

            if (buttonImage == null) buttonImage = GetComponent<Image>();
            if (buttonImage != null) buttonImage.raycastTarget = true;

            if (letterBadgeImage == null)
            {
                Transform badgeT = transform.Find("LetterBadge");
                if (badgeT != null) letterBadgeImage = badgeT.GetComponent<Image>();
            }

            if (optionLabelText == null)
            {
                Transform labelT = transform.Find("OptionText");
                if (labelT != null) optionLabelText = labelT.GetComponent<TMP_Text>();
            }

            if (optionLetterText == null)
            {
                Transform letterT = transform.Find("LetterBadge/LetterPrefix");
                if (letterT == null) letterT = transform.Find("LetterPrefix");
                if (letterT != null) optionLetterText = letterT.GetComponent<TMP_Text>();
            }

            button.onClick.AddListener(OnButtonClicked);
        }

        public void Setup(int index, string text, Action<int> callback)
        {
            optionIndex = index;
            onClickCallback = callback;
            cachedLetter = ((char)('A' + index)).ToString();

            if (optionLabelText != null)
            {
                optionLabelText.text = text;
            }

            ResetState();
        }

        public void ResetState()
        {
            if (button != null)
            {
                button.interactable = true;
            }

            if (buttonImage != null)
            {
                buttonImage.color = NormalBtnColor;
                buttonImage.raycastTarget = true;
            }

            if (letterBadgeImage != null)
            {
                letterBadgeImage.color = NormalBadgeColor;
            }

            if (optionLetterText != null)
            {
                optionLetterText.text = cachedLetter;
                optionLetterText.color = new Color(1f, 0.85f, 0.25f, 1f);
            }

            if (optionLabelText != null)
            {
                optionLabelText.color = Color.white;
            }
        }

        public void SetState(bool isSelected, bool isCorrect, bool revealCorrect)
        {
            if (button != null)
            {
                button.interactable = false;
            }

            if (buttonImage == null) return;

            if (isSelected)
            {
                if (isCorrect)
                {
                    buttonImage.color = CorrectBtnColor;
                    if (letterBadgeImage != null) letterBadgeImage.color = CorrectBadgeColor;
                    if (optionLetterText != null)
                    {
                        optionLetterText.text = "✓";
                        optionLetterText.color = Color.white;
                    }
                    if (optionLabelText != null) optionLabelText.color = Color.white;
                }
                else
                {
                    buttonImage.color = WrongBtnColor;
                    if (letterBadgeImage != null) letterBadgeImage.color = WrongBadgeColor;
                    if (optionLetterText != null)
                    {
                        optionLetterText.text = "✗";
                        optionLetterText.color = Color.white;
                    }
                    if (optionLabelText != null) optionLabelText.color = Color.white;
                }
            }
            else if (revealCorrect && isCorrect)
            {
                // Highlight the actual correct answer in green
                buttonImage.color = CorrectBtnColor;
                if (letterBadgeImage != null) letterBadgeImage.color = CorrectBadgeColor;
                if (optionLetterText != null)
                {
                    optionLetterText.text = "✓";
                    optionLetterText.color = Color.white;
                }
                if (optionLabelText != null) optionLabelText.color = Color.white;
            }
            else
            {
                // Dim unselected answers
                buttonImage.color = DimBtnColor;
                if (letterBadgeImage != null) letterBadgeImage.color = DimBadgeColor;
                if (optionLetterText != null)
                {
                    optionLetterText.text = cachedLetter;
                    optionLetterText.color = new Color(0.55f, 0.62f, 0.70f, 0.60f);
                }
                if (optionLabelText != null) optionLabelText.color = new Color(0.55f, 0.62f, 0.70f, 0.60f);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null) button.interactable = interactable;
        }

        private void OnButtonClicked()
        {
            onClickCallback?.Invoke(optionIndex);
        }
    }
}
