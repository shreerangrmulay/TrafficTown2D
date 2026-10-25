using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrafficTown2D.Player;

namespace TrafficTown2D.UI
{
    /// <summary>
    /// Interactive On-Screen Turn Indicator HUD widget.
    /// Provides clickable buttons for Left (Q), Hazard (H), and Right (E) signals,
    /// and flashes synchronously with the player car's physical indicator lights.
    /// </summary>
    public sealed class CarIndicatorHUD : MonoBehaviour
    {
        [Header("Target Controller")]
        [SerializeField] private CarIndicatorController carIndicator;

        [Header("Buttons & Images")]
        [SerializeField] private Button leftButton;
        [SerializeField] private Image leftButtonBg;
        [SerializeField] private TMP_Text leftButtonText;

        [SerializeField] private Button hazardButton;
        [SerializeField] private Image hazardButtonBg;
        [SerializeField] private TMP_Text hazardButtonText;

        [SerializeField] private Button rightButton;
        [SerializeField] private Image rightButtonBg;
        [SerializeField] private TMP_Text rightButtonText;

        [SerializeField] private TMP_Text hintLabel;

        // Styling Colors
        private static readonly Color ContainerBgColor = new Color(0.06f, 0.09f, 0.14f, 0.90f);
        private static readonly Color ButtonIdleBgColor = new Color(0.12f, 0.16f, 0.24f, 0.88f);
        private static readonly Color ButtonAmberActiveColor = new Color(1.0f, 0.72f, 0.05f, 1.0f);
        private static readonly Color ButtonAmberDimColor = new Color(0.35f, 0.24f, 0.04f, 0.85f);
        private static readonly Color ButtonHazardActiveColor = new Color(0.95f, 0.25f, 0.18f, 1.0f);
        private static readonly Color ButtonHazardDimColor = new Color(0.38f, 0.10f, 0.08f, 0.85f);

        private static readonly Color TextIdleColor = new Color(0.85f, 0.92f, 1.0f, 1.0f);
        private static readonly Color TextDarkColor = new Color(0.08f, 0.08f, 0.08f, 1.0f);
        private static readonly Color TextWarmColor = new Color(1.0f, 0.88f, 0.40f, 1.0f);

        private void Awake()
        {
            EnsureButtonListeners();
        }

        private void Start()
        {
            if (carIndicator == null)
            {
                carIndicator = FindAnyObjectByType<CarIndicatorController>();
            }
        }

        private void Update()
        {
            if (carIndicator == null)
            {
                carIndicator = FindAnyObjectByType<CarIndicatorController>();
                if (carIndicator == null) return;
            }

            UpdateVisualFeedback();
        }

        public void BindCar(CarIndicatorController controller)
        {
            carIndicator = controller;
            EnsureButtonListeners();
        }

        private void EnsureButtonListeners()
        {
            if (leftButton != null)
            {
                leftButton.onClick.RemoveListener(OnLeftClicked);
                leftButton.onClick.AddListener(OnLeftClicked);
            }
            if (hazardButton != null)
            {
                hazardButton.onClick.RemoveListener(OnHazardClicked);
                hazardButton.onClick.AddListener(OnHazardClicked);
            }
            if (rightButton != null)
            {
                rightButton.onClick.RemoveListener(OnRightClicked);
                rightButton.onClick.AddListener(OnRightClicked);
            }
        }

        private void OnLeftClicked()
        {
            if (carIndicator != null) carIndicator.ToggleLeft();
        }

        private void OnHazardClicked()
        {
            if (carIndicator != null) carIndicator.ToggleHazard();
        }

        private void OnRightClicked()
        {
            if (carIndicator != null) carIndicator.ToggleRight();
        }

        private void UpdateVisualFeedback()
        {
            if (carIndicator == null) return;

            bool isBlink = carIndicator.BlinkState;
            IndicatorState state = carIndicator.State;

            // 1. Left Indicator Button
            bool leftActive = (state == IndicatorState.Left || state == IndicatorState.Hazard);
            if (leftButtonBg != null && leftButtonText != null)
            {
                if (leftActive)
                {
                    leftButtonBg.color = isBlink ? ButtonAmberActiveColor : ButtonAmberDimColor;
                    leftButtonText.color = isBlink ? TextDarkColor : TextWarmColor;
                }
                else
                {
                    leftButtonBg.color = ButtonIdleBgColor;
                    leftButtonText.color = TextIdleColor;
                }
            }

            // 2. Right Indicator Button
            bool rightActive = (state == IndicatorState.Right || state == IndicatorState.Hazard);
            if (rightButtonBg != null && rightButtonText != null)
            {
                if (rightActive)
                {
                    rightButtonBg.color = isBlink ? ButtonAmberActiveColor : ButtonAmberDimColor;
                    rightButtonText.color = isBlink ? TextDarkColor : TextWarmColor;
                }
                else
                {
                    rightButtonBg.color = ButtonIdleBgColor;
                    rightButtonText.color = TextIdleColor;
                }
            }

            // 3. Hazard Button
            bool hazardActive = (state == IndicatorState.Hazard);
            if (hazardButtonBg != null && hazardButtonText != null)
            {
                if (hazardActive)
                {
                    hazardButtonBg.color = isBlink ? ButtonHazardActiveColor : ButtonHazardDimColor;
                    hazardButtonText.color = isBlink ? Color.white : TextWarmColor;
                }
                else
                {
                    hazardButtonBg.color = ButtonIdleBgColor;
                    hazardButtonText.color = TextIdleColor;
                }
            }
        }

        /// <summary>
        /// Automatically ensures that a CarIndicatorHUD exists on the active Canvas.
        /// </summary>
        public static CarIndicatorHUD EnsureHUD(CarIndicatorController controller)
        {
            CarIndicatorHUD existing = FindAnyObjectByType<CarIndicatorHUD>();
            if (existing != null)
            {
                existing.BindCar(controller);
                return existing;
            }

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("IndicatorCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create HUD container
            GameObject hudObj = new GameObject("CarIndicatorHUD");
            hudObj.transform.SetParent(canvas.transform, false);

            RectTransform hudRect = hudObj.AddComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0.5f, 0f);
            hudRect.anchorMax = new Vector2(0.5f, 0f);
            hudRect.pivot = new Vector2(0.5f, 0f);
            hudRect.anchoredPosition = new Vector2(0f, 16f);
            hudRect.sizeDelta = new Vector2(236f, 44f);

            Image hudBg = hudObj.AddComponent<Image>();
            hudBg.color = ContainerBgColor;
            hudBg.raycastTarget = false;

            // Load rounded sprite if available
            #if UNITY_EDITOR
            Sprite roundedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/RoundedPanel.png");
            if (roundedSprite != null)
            {
                hudBg.sprite = roundedSprite;
                hudBg.type = Image.Type.Sliced;
            }
            #endif

            CarIndicatorHUD hud = hudObj.AddComponent<CarIndicatorHUD>();

            // Create Left Button: [Q] ◄
            hud.leftButton = CreateButton(hudObj.transform, "Btn_Left", new Vector2(-74f, 0f), new Vector2(68f, 32f), "◄  Q", out hud.leftButtonBg, out hud.leftButtonText);

            // Create Hazard Button: ⚠ H
            hud.hazardButton = CreateButton(hudObj.transform, "Btn_Hazard", new Vector2(0f, 0f), new Vector2(58f, 32f), "⚠ H", out hud.hazardButtonBg, out hud.hazardButtonText);

            // Create Right Button: E ►
            hud.rightButton = CreateButton(hudObj.transform, "Btn_Right", new Vector2(74f, 0f), new Vector2(68f, 32f), "E  ►", out hud.rightButtonBg, out hud.rightButtonText);

            hud.BindCar(controller);
            return hud;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 pos, Vector2 size, string label, out Image bgImage, out TMP_Text labelText)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            bgImage = btnObj.AddComponent<Image>();
            bgImage.color = ButtonIdleBgColor;

            #if UNITY_EDITOR
            Sprite roundedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/RoundedPanel.png");
            if (roundedSprite != null)
            {
                bgImage.sprite = roundedSprite;
                bgImage.type = Image.Type.Sliced;
            }
            #endif

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.24f, 0.32f, 0.45f, 1f);
            cb.pressedColor = new Color(0.18f, 0.24f, 0.36f, 1f);
            btn.colors = cb;

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(btnObj.transform, false);

            labelText = txtObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 13.5f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = TextIdleColor;
            labelText.raycastTarget = false;

            RectTransform txtRt = labelText.rectTransform;
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;
            txtRt.anchoredPosition = Vector2.zero;

            return btn;
        }
    }
}
