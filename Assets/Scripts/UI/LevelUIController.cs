using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using TrafficTown2D.Core;
using TrafficTown2D.Gameplay;

namespace TrafficTown2D.UI
{
    public sealed class LevelUIController : MonoBehaviour
    {
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private CanvasGroup completionGroup;
        [SerializeField] private RectTransform completionCard;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text safeActionsText;
        [SerializeField] private TMP_Text mistakesText;
        [SerializeField] private TMP_Text ratingStarsText;
        [SerializeField] private TMP_Text ratingText;
        [SerializeField] private Button backButton;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private SceneLoader sceneLoader;

        private Coroutine completionRoutine;

        private void Awake()
        {
            HideCompletion();
        }

        private void OnEnable()
        {
            if (scoreManager != null) scoreManager.ScoreChanged += UpdateScore;
        }

        private void Start()
        {
            GameManager.Instance?.SetState(GameState.Playing);
            if (objectiveText != null) objectiveText.text = "Cross the road safely";
            HideCompletion();
            UpdateScore(scoreManager != null ? scoreManager.CurrentScore : 0);
        }

        private void OnDisable()
        {
            if (scoreManager != null) scoreManager.ScoreChanged -= UpdateScore;
        }

        public void ShowCompletion()
        {
            if (scoreManager != null)
            {
                if (finalScoreText != null) finalScoreText.text = scoreManager.CurrentScore.ToString();
                if (safeActionsText != null) safeActionsText.text = scoreManager.SafeActions.ToString();
                if (mistakesText != null) mistakesText.text = scoreManager.Mistakes.ToString();

                int starCount = scoreManager.Mistakes == 0 ? 5 : scoreManager.Mistakes < 3 ? 4 : 3;
                if (ratingStarsText != null) ratingStarsText.text = new string('★', starCount);
                if (ratingText != null) ratingText.text = starCount == 5 ? "Excellent!" : "Great effort!";
            }

            if (completionPanel == null) return;

            completionPanel.SetActive(true);
            if (completionRoutine != null) StopCoroutine(completionRoutine);
            completionRoutine = StartCoroutine(AnimateCompletion());
        }

        public void BackToMenu()
        {
            if (sceneLoader != null)
            {
                sceneLoader.LoadMainMenu();
                return;
            }

            SceneLoader.Instance?.LoadMainMenu();
        }

        private void UpdateScore(int score)
        {
            if (scoreText != null) scoreText.text = score.ToString();
        }

        private void HideCompletion()
        {
            if (completionRoutine != null)
            {
                StopCoroutine(completionRoutine);
                completionRoutine = null;
            }

            if (completionGroup != null)
            {
                completionGroup.alpha = 0f;
                completionGroup.interactable = false;
                completionGroup.blocksRaycasts = false;
            }

            if (completionCard != null) completionCard.localScale = Vector3.one * 0.9f;
            if (backButton != null) backButton.interactable = false;
            if (completionPanel != null) completionPanel.SetActive(false);
        }

        private System.Collections.IEnumerator AnimateCompletion()
        {
            const float duration = 0.35f;
            float elapsed = 0f;

            if (completionGroup != null)
            {
                completionGroup.alpha = 0f;
                completionGroup.interactable = false;
                completionGroup.blocksRaycasts = true;
            }

            if (completionCard != null) completionCard.localScale = Vector3.one * 0.9f;
            if (backButton != null) backButton.interactable = false;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);

                if (completionGroup != null) completionGroup.alpha = eased;
                if (completionCard != null) completionCard.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, eased);
                yield return null;
            }

            if (completionGroup != null)
            {
                completionGroup.alpha = 1f;
                completionGroup.interactable = true;
            }

            if (completionCard != null) completionCard.localScale = Vector3.one;
            if (backButton != null) backButton.interactable = true;
            completionRoutine = null;
        }
    }

    [RequireComponent(typeof(RectTransform))]
    public sealed class AnimatedUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.04f;
        [SerializeField] private float pressedScale = 0.96f;
        [SerializeField] private float animationSeconds = 0.08f;
        [SerializeField] private Image targetImage;
        [SerializeField] private Color normalColor = new Color(0.12f, 0.55f, 0.84f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.16f, 0.64f, 0.94f, 1f);
        [SerializeField] private Color pressedColor = new Color(0.08f, 0.44f, 0.72f, 1f);

        private RectTransform rectTransform;
        private Coroutine animationRoutine;
        private bool hovering;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (targetImage == null) targetImage = GetComponent<Image>();
            Apply(1f, normalColor);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            AnimateTo(hoverScale, hoverColor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            AnimateTo(1f, normalColor);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            AnimateTo(pressedScale, pressedColor);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateTo(hovering ? hoverScale : 1f, hovering ? hoverColor : normalColor);
        }

        private void AnimateTo(float scale, Color color)
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(Animate(scale, color));
        }

        private System.Collections.IEnumerator Animate(float targetScale, Color targetColor)
        {
            Vector3 startScale = rectTransform.localScale;
            Color startColor = targetImage != null ? targetImage.color : targetColor;
            float elapsed = 0f;

            while (elapsed < animationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / animationSeconds);
                Apply(Mathf.Lerp(startScale.x, targetScale, progress), Color.Lerp(startColor, targetColor, progress));
                yield return null;
            }

            Apply(targetScale, targetColor);
            animationRoutine = null;
        }

        private void Apply(float scale, Color color)
        {
            rectTransform.localScale = Vector3.one * scale;
            if (targetImage != null) targetImage.color = color;
        }
    }
}
