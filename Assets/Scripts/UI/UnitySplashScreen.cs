using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TrafficTown2D.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UnitySplashScreen : MonoBehaviour
    {
        public static event Action SplashFinished;

        private static bool hasShownThisSession = false;

        [Header("UI Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform contentTransform;
        [SerializeField] private Image logoImage;
        [SerializeField] private Text madeWithText;

        [Header("Timings (Seconds)")]
        [SerializeField] private float preWaitDuration = 0.2f;
        [SerializeField] private float fadeInDuration = 0.8f;
        [SerializeField] private float displayDuration = 1.8f;
        [SerializeField] private float fadeOutDuration = 0.7f;

        [Header("Configuration")]
        [SerializeField] private bool allowSkip = true;
        [SerializeField] private bool showOncePerSession = true;
        [SerializeField] private bool loadNextSceneOnFinish = false;
        [SerializeField] private string nextSceneName = "MainMenu";

        private Coroutine splashRoutine;
        private bool isFinished = false;
        private bool isFadingOut = false;
        private bool skipRequested = false;

        public static bool HasShownThisSession => hasShownThisSession;

        public static void ResetSession()
        {
            hasShownThisSession = false;
        }

        public void Configure(CanvasGroup cg, RectTransform content, Image logo, Text text, float preWait = 0.2f, float fadeIn = 0.8f, float display = 1.8f, float fadeOut = 0.7f, bool skip = true, bool once = true, bool loadNext = false, string nextScene = "MainMenu")
        {
            canvasGroup = cg;
            contentTransform = content;
            logoImage = logo;
            madeWithText = text;
            preWaitDuration = preWait;
            fadeInDuration = fadeIn;
            displayDuration = display;
            fadeOutDuration = fadeOut;
            allowSkip = skip;
            showOncePerSession = once;
            loadNextSceneOnFinish = loadNext;
            nextSceneName = nextScene;
        }

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (showOncePerSession && hasShownThisSession && !loadNextSceneOnFinish)
            {
                // Already shown once this session; skip immediately
                gameObject.SetActive(false);
                return;
            }

            hasShownThisSession = true;
        }

        private void Start()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = true;
            }

            splashRoutine = StartCoroutine(SplashSequence());
        }

        private void Update()
        {
            if (!allowSkip || isFinished || isFadingOut || skipRequested) return;

            bool skipped = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
                skipped = true;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                skipped = true;
#endif
            if (!skipped)
            {
                try
                {
                    if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
                        skipped = true;
                }
                catch
                {
                    // Fallback if classic input is disabled
                }
            }

            if (skipped)
            {
                skipRequested = true;
            }
        }

        private IEnumerator SplashSequence()
        {
            // Initial pre-wait
            yield return new WaitForSecondsRealtime(preWaitDuration);

            // 1. Fade In
            float elapsed = 0f;
            Vector3 startScale = new Vector3(0.92f, 0.92f, 1f);
            Vector3 midScale = Vector3.one;

            while (elapsed < fadeInDuration)
            {
                if (skipRequested) break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                // Smooth ease-out quad
                float ease = 1f - (1f - t) * (1f - t);

                if (canvasGroup != null) canvasGroup.alpha = ease;
                if (contentTransform != null) contentTransform.localScale = Vector3.Lerp(startScale, midScale, ease);

                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (contentTransform != null) contentTransform.localScale = midScale;

            // 2. Display Hold with very subtle zoom
            elapsed = 0f;
            Vector3 endScale = new Vector3(1.04f, 1.04f, 1f);

            while (elapsed < displayDuration)
            {
                if (skipRequested) break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / displayDuration);

                if (contentTransform != null)
                {
                    contentTransform.localScale = Vector3.Lerp(midScale, endScale, t);
                }

                yield return null;
            }

            // 3. Fade Out
            isFadingOut = true;
            elapsed = 0f;
            float currentAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                float ease = t * t; // Ease in

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(currentAlpha, 0f, ease);
                }

                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }

            isFinished = true;
            SplashFinished?.Invoke();

            if (loadNextSceneOnFinish && !string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Manually replays the splash animation (e.g. for testing or from menu options).
        /// </summary>
        public void Replay()
        {
            if (splashRoutine != null) StopCoroutine(splashRoutine);
            gameObject.SetActive(true);
            skipRequested = false;
            isFadingOut = false;
            isFinished = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = true;
            }
            splashRoutine = StartCoroutine(SplashSequence());
        }
    }
}
