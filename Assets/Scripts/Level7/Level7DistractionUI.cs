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
        private bool isVisible = false;

        public bool IsVisible => isVisible && gameObject.activeInHierarchy;
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

            // Start hidden and deactivated
            SnapHidden();
            gameObject.SetActive(false);
        }

        public void SnapHidden()
        {
            isVisible = false;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (cardRect != null) cardRect.anchoredPosition = hiddenOffset;
        }

        public void SetupCard(DistractionEventData data)
        {
            currentData = data;
            if (data == null) return;

            if (titleText != null) titleText.text = data.headerTitle;
            if (messageText != null) messageText.text = data.messageText;
            if (actionButtonText != null) actionButtonText.text = data.actionButtonLabel;
            if (ignoreButtonText != null) ignoreButtonText.text = data.ignoreButtonLabel;
            if (countdownBar != null) countdownBar.fillAmount = 1f;
        }

        public void UpdateCountdownBar(float fillAmount)
        {
            if (countdownBar != null)
            {
                countdownBar.fillAmount = Mathf.Clamp01(fillAmount);
            }
        }

        public IEnumerator AnimateSlideRoutine(bool show)
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
            isVisible = show;
            activeAnimRoutine = null;
        }

        public void ShowDistraction(DistractionEventData data)
        {
            // Delegate directly to the persistent Level7DistractionManager if present
            if (Level7DistractionManager.Instance != null)
            {
                Level7DistractionManager.Instance.TriggerDistractionDirect(data);
                return;
            }

            // Standalone fallback: only start routine if strictly active in hierarchy
            currentData = data;
            if (data == null) return;

            gameObject.SetActive(true);
            SetupCard(data);

            if (gameObject.activeInHierarchy)
            {
                if (activeAnimRoutine != null) StopCoroutine(activeAnimRoutine);
                activeAnimRoutine = StartCoroutine(AnimateSlideRoutine(true));
            }
        }

        public void HideDistraction()
        {
            if (Level7DistractionManager.Instance != null && Level7DistractionManager.Instance.IsDistractionActive)
            {
                Level7DistractionManager.Instance.DismissDistraction(false);
                return;
            }

            if (activeAnimRoutine != null)
            {
                StopCoroutine(activeAnimRoutine);
                activeAnimRoutine = null;
            }

            SnapHidden();
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        public void TriggerTimeoutExpired(DistractionEventData data)
        {
            TimeoutExpired?.Invoke(data);
        }

        private void HandleActionClicked()
        {
            if (!isVisible) return;
            ActionClicked?.Invoke(currentData);
        }

        private void HandleIgnoreClicked()
        {
            if (!isVisible) return;
            IgnoreClicked?.Invoke(currentData);
        }
    }
}
