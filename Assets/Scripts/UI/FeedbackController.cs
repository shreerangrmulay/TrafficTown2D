using UnityEngine;
using TMPro;

namespace TrafficTown2D.UI
{
    public sealed class FeedbackController : MonoBehaviour
    {
        [SerializeField] private GameObject bannerPanel;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text iconText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float visibleSeconds = 2.4f;
        [SerializeField] private float fadeSeconds = 0.18f;

        private Coroutine feedbackRoutine;
        private GameObject targetPanel;
        private TMP_Text targetText;

        public void Show(string message)
        {
            ResolveReferences();
            if (targetText == null) return;

            targetPanel.SetActive(true);
            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);

            bool warning = IsWarning(message);
            if (iconText != null)
            {
                iconText.text = warning ? "!" : "✓";
                iconText.color = warning ? new Color(0.92f, 0.45f, 0.18f, 1f) : new Color(0.18f, 0.67f, 0.32f, 1f);
            }

            targetText.text = message;
            feedbackRoutine = StartCoroutine(ShowRoutine());
        }

        private void Awake()
        {
            ResolveReferences();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (targetText != null)
            {
                targetPanel.SetActive(false);
            }
        }

        private void ResolveReferences()
        {
            targetPanel = bannerPanel != null ? bannerPanel : gameObject;
            targetText = messageText != null ? messageText : feedbackText;
            if (canvasGroup == null)
            {
                canvasGroup = targetPanel.GetComponent<CanvasGroup>();
            }
        }

        private System.Collections.IEnumerator ShowRoutine()
        {
            yield return FadeTo(1f);
            yield return new WaitForSecondsRealtime(visibleSeconds);
            yield return FadeTo(0f);
            feedbackRoutine = null;
            targetPanel.SetActive(false);
        }

        private System.Collections.IEnumerator FadeTo(float targetAlpha)
        {
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        private static bool IsWarning(string message)
        {
            return message.Contains("wait") || message.Contains("Use") || message.Contains("careful") || message.Contains("right of way");
        }
    }
}
