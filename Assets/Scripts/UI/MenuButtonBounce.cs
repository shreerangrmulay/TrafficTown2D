using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrafficTown2D.UI
{
    /// <summary>
    /// Smooth coroutine-driven scale bounce on hover, press, and gamepad/keyboard navigation
    /// for TrafficTown 2D menu and level map buttons.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MenuButtonBounce : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [Header("Scale Settings")]
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float pressedScale = 0.94f;
        [SerializeField] private float transitionSpeed = 16f;

        private RectTransform rectTransform;
        private Vector3 initialScale = Vector3.one;
        private Coroutine scaleCoroutine;
        private bool isPointerOver = false;
        private bool isPressed = false;
        private bool isSelected = false;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                initialScale = rectTransform.localScale;
                if (initialScale == Vector3.zero)
                {
                    initialScale = Vector3.one;
                    rectTransform.localScale = Vector3.one;
                }
            }
        }

        private void OnEnable()
        {
            if (rectTransform != null)
            {
                rectTransform.localScale = initialScale;
            }
            isPointerOver = false;
            isPressed = false;
            isSelected = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;
            ApplyTargetScale();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            ApplyTargetScale();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            ApplyTargetScale();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            StartOvershootBounce();
        }

        public void OnSelect(BaseEventData eventData)
        {
            isSelected = true;
            ApplyTargetScale();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            ApplyTargetScale();
        }

        private void ApplyTargetScale()
        {
            if (!gameObject.activeInHierarchy) return;

            float target = 1.0f;
            if (isPressed)
            {
                target = pressedScale;
            }
            else if (isPointerOver || isSelected)
            {
                target = hoverScale;
            }

            if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
            scaleCoroutine = StartCoroutine(AnimateScale(initialScale * target, transitionSpeed));
        }

        private void StartOvershootBounce()
        {
            if (!gameObject.activeInHierarchy) return;
            if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
            scaleCoroutine = StartCoroutine(AnimateOvershoot());
        }

        private IEnumerator AnimateScale(Vector3 target, float speed)
        {
            while (rectTransform != null && Vector3.Distance(rectTransform.localScale, target) > 0.002f)
            {
                rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, target, Time.unscaledDeltaTime * speed);
                yield return null;
            }
            if (rectTransform != null)
            {
                rectTransform.localScale = target;
            }
        }

        private IEnumerator AnimateOvershoot()
        {
            // Bounce up slightly higher before settling back
            Vector3 overshoot = initialScale * (hoverScale * 1.03f);
            float elapsed = 0f;
            float duration = 0.12f;

            Vector3 startScale = rectTransform.localScale;
            while (elapsed < duration && rectTransform != null)
            {
                elapsed += Time.unscaledDeltaTime;
                rectTransform.localScale = Vector3.Lerp(startScale, overshoot, elapsed / duration);
                yield return null;
            }

            Vector3 settleTarget = (isPointerOver || isSelected) ? initialScale * hoverScale : initialScale;
            elapsed = 0f;
            duration = 0.14f;
            startScale = rectTransform != null ? rectTransform.localScale : overshoot;

            while (elapsed < duration && rectTransform != null)
            {
                elapsed += Time.unscaledDeltaTime;
                rectTransform.localScale = Vector3.Lerp(startScale, settleTarget, elapsed / duration);
                yield return null;
            }

            if (rectTransform != null)
            {
                rectTransform.localScale = settleTarget;
            }
        }
    }
}
