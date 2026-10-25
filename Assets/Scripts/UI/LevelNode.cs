using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrafficTown2D.UI
{
    public enum LevelNodeState
    {
        Locked,
        Current,
        Completed
    }

    /// <summary>
    /// Visual and interactive representation of a single level on the Winding Road Map.
    /// Handles three distinct states: Completed, Current (pulsing), and Locked (grey with barrier).
    /// </summary>
    public class LevelNode : MonoBehaviour
    {
        [Header("Level Information")]
        [SerializeField] private int levelNumber = 1;
        [SerializeField] private string levelTitle = "Safe Crossing";

        [Header("UI Elements")]
        [SerializeField] private Button button;
        [SerializeField] private Image buttonBgImage;
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleLabelText;
        [SerializeField] private Image[] starImages = new Image[3];
        [SerializeField] private GameObject lockOverlayObject;
        [SerializeField] private GameObject barrierObject;

        [Header("Sprites")]
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starEmptySprite;
        [SerializeField] private Sprite lockSprite;

        private LevelNodeState currentState = LevelNodeState.Locked;
        private Coroutine pulseCoroutine;
        private Vector3 baseScale = Vector3.one;
        private RectTransform rectTransform;

        public int LevelNumber => levelNumber;
        public string LevelTitle => levelTitle;
        public LevelNodeState CurrentState => currentState;
        public Button Button => button;
        public RectTransform RectTransform => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            baseScale = rectTransform.localScale;
            if (baseScale == Vector3.zero) baseScale = Vector3.one;

            if (button == null) button = GetComponent<Button>();
            if (buttonBgImage == null) buttonBgImage = GetComponent<Image>();
            if (buttonBgImage != null)
            {
                if (buttonBgImage.sprite == null) buttonBgImage.sprite = MenuAssetProvider.GetNodeCircle();
                buttonBgImage.raycastTarget = true;
            }
            if (numberText == null) numberText = transform.Find("NumberText")?.GetComponent<TMP_Text>();
            if (titleLabelText == null) titleLabelText = transform.Find("NameLabel")?.GetComponent<TMP_Text>();
            if (lockOverlayObject == null) lockOverlayObject = transform.Find("LockOverlay")?.gameObject;
            if (barrierObject == null) barrierObject = transform.Find("RoadBarrier")?.gameObject;

            Transform starsRow = transform.Find("StarsRow");
            if (starsRow != null && (starImages == null || starImages.Length == 0 || starImages[0] == null))
            {
                starImages = new Image[3];
                for (int s = 0; s < 3; s++)
                {
                    starImages[s] = starsRow.Find($"Star_{s}")?.GetComponent<Image>();
                }
            }

            if (starFilledSprite == null) starFilledSprite = MenuAssetProvider.GetStarFilled();
            if (starEmptySprite == null) starEmptySprite = MenuAssetProvider.GetStarEmpty();
            if (lockSprite == null) lockSprite = MenuAssetProvider.GetLockSprite();
        }

        private void OnEnable()
        {
            if (currentState == LevelNodeState.Current && pulseCoroutine == null && gameObject.activeInHierarchy)
            {
                pulseCoroutine = StartCoroutine(AnimateCurrentPulse());
            }
        }

        public void Initialize(int num, string title)
        {
            levelNumber = num;
            levelTitle = title;

            if (numberText != null)
            {
                numberText.text = num.ToString();
                numberText.color = ThemeColors.White;
            }

            if (titleLabelText != null)
            {
                titleLabelText.text = title;
                titleLabelText.color = ThemeColors.InkBlack;
            }
        }

        public void SetState(LevelNodeState state, int earnedStars)
        {
            currentState = state;

            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }

            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            rectTransform.localScale = baseScale;

            switch (state)
            {
                case LevelNodeState.Completed:
                    ApplyCompletedState(earnedStars);
                    break;

                case LevelNodeState.Current:
                    ApplyCurrentState(earnedStars);
                    break;

                case LevelNodeState.Locked:
                    ApplyLockedState();
                    break;
            }
        }

        private void ApplyCompletedState(int earnedStars)
        {
            if (button != null) button.interactable = true;

            if (buttonBgImage != null)
            {
                buttonBgImage.color = ThemeColors.AccentPurple;
            }

            if (numberText != null)
            {
                numberText.gameObject.SetActive(true);
                numberText.color = ThemeColors.White;
            }

            if (lockOverlayObject != null) lockOverlayObject.SetActive(false);
            if (barrierObject != null) barrierObject.SetActive(false);

            // Update earned stars
            int starsToShow = Mathf.Clamp(earnedStars, 1, 3);
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] == null) continue;
                starImages[i].gameObject.SetActive(true);

                if (i < starsToShow)
                {
                    starImages[i].color = ThemeColors.StarFilled;
                    if (starFilledSprite != null) starImages[i].sprite = starFilledSprite;
                }
                else
                {
                    starImages[i].color = ThemeColors.StarEmpty;
                    if (starEmptySprite != null) starImages[i].sprite = starEmptySprite;
                }
            }

            if (titleLabelText != null)
            {
                titleLabelText.color = ThemeColors.InkBlack;
            }
        }

        private void ApplyCurrentState(int earnedStars)
        {
            if (button != null) button.interactable = true;

            if (buttonBgImage != null)
            {
                buttonBgImage.color = ThemeColors.AccentPurple;
            }

            if (numberText != null)
            {
                numberText.gameObject.SetActive(true);
                numberText.color = ThemeColors.White;
            }

            if (lockOverlayObject != null) lockOverlayObject.SetActive(false);
            if (barrierObject != null) barrierObject.SetActive(false);

            // Hide or show stars if any earned previously
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] == null) continue;
                if (earnedStars > 0 && i < earnedStars)
                {
                    starImages[i].gameObject.SetActive(true);
                    starImages[i].color = ThemeColors.StarFilled;
                    if (starFilledSprite != null) starImages[i].sprite = starFilledSprite;
                }
                else
                {
                    starImages[i].gameObject.SetActive(true);
                    starImages[i].color = ThemeColors.StarEmpty;
                    if (starEmptySprite != null) starImages[i].sprite = starEmptySprite;
                }
            }

            if (titleLabelText != null)
            {
                titleLabelText.color = ThemeColors.InkBlack;
            }

            // Start gentle pulsing animation (makes the current node larger and lively)
            if (gameObject.activeInHierarchy)
            {
                pulseCoroutine = StartCoroutine(AnimateCurrentPulse());
            }
        }

        private void ApplyLockedState()
        {
            if (button != null) button.interactable = false;

            if (buttonBgImage != null)
            {
                buttonBgImage.color = ThemeColors.LockedGrey;
            }

            if (numberText != null)
            {
                numberText.gameObject.SetActive(false);
            }

            if (lockOverlayObject != null)
            {
                lockOverlayObject.SetActive(true);
                Image lockImg = lockOverlayObject.GetComponent<Image>();
                if (lockImg != null) lockImg.color = ThemeColors.LockedDark;
            }

            if (barrierObject != null)
            {
                barrierObject.SetActive(true);
            }

            // Hide stars for locked levels
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null) starImages[i].gameObject.SetActive(false);
            }

            if (titleLabelText != null)
            {
                titleLabelText.color = new Color(ThemeColors.InkBlack.r, ThemeColors.InkBlack.g, ThemeColors.InkBlack.b, 0.45f);
            }
        }

        private IEnumerator AnimateCurrentPulse()
        {
            float timer = 0f;
            // Current node is slightly larger by default (1.12x) and pulses between 1.08x and 1.18x
            while (true)
            {
                timer += Time.unscaledDeltaTime * 2.8f;
                float scaleFactor = 1.12f + Mathf.Sin(timer) * 0.06f;
                if (rectTransform != null)
                {
                    rectTransform.localScale = baseScale * scaleFactor;
                }
                yield return null;
            }
        }

        private void OnDisable()
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
            if (rectTransform != null)
            {
                rectTransform.localScale = baseScale;
            }
        }
    }
}
