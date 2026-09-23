using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrafficTown2D.Level7
{
    public class Level7DistractionUI : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform cardRect;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionButtonText;
        [SerializeField] private Button ignoreButton;
        [SerializeField] private TMP_Text ignoreButtonText;
        [SerializeField] private Image countdownBar;
        [SerializeField] private Image iconImage;

        [Header("Animation Settings")]
        [SerializeField] private float slideDuration = 0.28f;
        [SerializeField] private Vector2 hiddenOffset = new Vector2(380f, 0f);
        [SerializeField] private Vector2 shownOffset = Vector2.zero;

        private DistractionEventData currentData;
        private Coroutine activeAnimRoutine;
        private Coroutine activeTimerRoutine;
        private float remainingTime;
        private bool isVisible = false;

        public bool IsVisible => isVisible;
        public DistractionEventData CurrentData => currentData;

        public event Action<DistractionEventData> ActionClicked;
        public event Action<DistractionEventData> IgnoreClicked;
        public event Action<DistractionEventData> TimeoutExpired;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (cardRect == null) cardRect = GetComponent<RectTransform>();

            if (actionButton != null)
            {
                actionButton.onClick.AddListener(HandleActionClicked);
            }
            if (ignoreButton != null)
            {
                ignoreButton.onClick.AddListener(HandleIgnoreClicked);
            }

            // Start hidden
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (cardRect != null) cardRect.anchoredPosition = hiddenOffset;
            gameObject.SetActive(false);
        }

        public void ShowDistraction(DistractionEventData data)
        {
            currentData = data;
            if (data == null) return;

            gameObject.SetActive(true);

            if (titleText != null) titleText.text = data.headerTitle;
            if (messageText != null) messageText.text = data.messageText;
            if (actionButtonText != null) actionButtonText.text = data.actionButtonLabel;
            if (ignoreButtonText != null) ignoreButtonText.text = data.ignoreButtonLabel;

            if (countdownBar != null) countdownBar.fillAmount = 1f;

            if (activeAnimRoutine != null) StopCoroutine(activeAnimRoutine);
            if (activeTimerRoutine != null) StopCoroutine(activeTimerRoutine);

            activeAnimRoutine = StartCoroutine(AnimateCard(true));
            activeTimerRoutine = StartCoroutine(CountdownRoutine(data.displayDuration));
        }

        public void HideDistraction()
        {
            if (!isVisible && !gameObject.activeSelf) return;

            if (activeTimerRoutine != null)
            {
                StopCoroutine(activeTimerRoutine);
                activeTimerRoutine = null;
            }

            if (activeAnimRoutine != null) StopCoroutine(activeAnimRoutine);
            activeAnimRoutine = StartCoroutine(AnimateCard(false));
        }

        private IEnumerator CountdownRoutine(float duration)
        {
            remainingTime = duration;
            while (remainingTime > 0f)
            {
                remainingTime -= Time.deltaTime;
                if (countdownBar != null)
                {
                    countdownBar.fillAmount = Mathf.Clamp01(remainingTime / duration);
                }
                yield return null;
            }

            // Timed out: naturally ignored by driver!
            TimeoutExpired?.Invoke(currentData);
            HideDistraction();
        }

        private void HandleActionClicked()
        {
            if (!isVisible) return;
            ActionClicked?.Invoke(currentData);
            HideDistraction();
        }

        private void HandleIgnoreClicked()
        {
            if (!isVisible) return;
            IgnoreClicked?.Invoke(currentData);
            HideDistraction();
        }

        private IEnumerator AnimateCard(bool show)
        {
            isVisible = show;
            float elapsed = 0f;
            Vector2 startPos = cardRect != null ? cardRect.anchoredPosition : Vector2.zero;
            Vector2 endPos = show ? shownOffset : hiddenOffset;
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : (show ? 0f : 1f);
            float endAlpha = show ? 1f : 0f;

            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                // Ease out cubic
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                if (cardRect != null) cardRect.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, ease);
                yield return null;
            }

            if (cardRect != null) cardRect.anchoredPosition = endPos;
            if (canvasGroup != null) canvasGroup.alpha = endAlpha;

            if (!show)
            {
                gameObject.SetActive(false);
            }
            activeAnimRoutine = null;
        }
    }
}
