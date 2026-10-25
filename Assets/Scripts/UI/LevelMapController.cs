using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TrafficTown2D.UI
{
    /// <summary>
    /// Controls the winding road level map inside a horizontal ScrollRect.
    /// Handles save data loading, node state visualization, player car positioning & drive animation,
    /// viewport auto-centering, and gamepad/keyboard navigation.
    /// </summary>
    public class LevelMapController : MonoBehaviour
    {
        [Header("Scroll Container")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewportRect;
        [SerializeField] private RectTransform contentRect;

        [Header("Road & Anchors")]
        [SerializeField] private RoadMapCurveRenderer roadCurveRenderer;
        [SerializeField] private List<RectTransform> levelAnchors = new List<RectTransform>();
        [SerializeField] private List<LevelNode> levelNodes = new List<LevelNode>();

        [Header("Car & Destination Elements")]
        [SerializeField] private RectTransform playerCarRect;
        [SerializeField] private Image playerCarImage;
        [SerializeField] private RectTransform checkeredFlagRect;
        [SerializeField] private Button backToMenuButton;

        [Header("Level Configuration")]
        [SerializeField] private float carDriveDuration = 1.5f;

        private static readonly string[] LevelTitles = new[]
        {
            "Safe Crossing",
            "Smart Crossing",
            "Yield Right",
            "Night Driving",
            "Traffic Controller",
            "Extreme Roads",
            "School Zone",
            "The Overtake",
            "Roundabout Rules",
            "Final Commute"
        };

        private int currentLevelIndex = 0; // 0-indexed (0 = Level 1)
        private Coroutine carAnimationCoroutine;
        private Coroutine autoScrollCoroutine;

        public int CurrentLevelIndex => currentLevelIndex;
        public List<LevelNode> LevelNodes => levelNodes;

        private void Awake()
        {
            AutoFindReferences();
        }

        public void AutoFindReferences()
        {
            if (scrollRect == null) scrollRect = GetComponentInChildren<ScrollRect>(true);
            if (scrollRect != null)
            {
                if (viewportRect == null) viewportRect = scrollRect.viewport;
                if (contentRect == null) contentRect = scrollRect.content;
            }

            if (roadCurveRenderer == null) roadCurveRenderer = GetComponentInChildren<RoadMapCurveRenderer>(true);

            if (levelNodes == null || levelNodes.Count == 0)
            {
                levelNodes = new List<LevelNode>(GetComponentsInChildren<LevelNode>(true));
                levelNodes.Sort((a, b) => a.LevelNumber.CompareTo(b.LevelNumber));
            }

            if (levelAnchors == null || levelAnchors.Count == 0)
            {
                levelAnchors = new List<RectTransform>();
                for (int i = 0; i < levelNodes.Count; i++)
                {
                    levelAnchors.Add(levelNodes[i].GetComponent<RectTransform>());
                }
            }

            if (roadCurveRenderer != null && (roadCurveRenderer.Anchors == null || roadCurveRenderer.Anchors.Count == 0))
            {
                roadCurveRenderer.SetAnchors(levelAnchors);
            }

            if (playerCarRect == null && contentRect != null)
            {
                Transform carT = contentRect.Find("PlayerCarOnMap");
                if (carT != null) playerCarRect = carT.GetComponent<RectTransform>();
            }

            if (checkeredFlagRect == null && contentRect != null)
            {
                Transform flagT = contentRect.Find("CheckeredFinishFlag");
                if (flagT != null) checkeredFlagRect = flagT.GetComponent<RectTransform>();
            }

            if (backToMenuButton == null)
            {
                Transform backT = transform.Find("BackToMenuButton");
                if (backT != null) backToMenuButton = backT.GetComponent<Button>();
            }
        }

        private void OnEnable()
        {
            RefreshMap();
        }

        /// <summary>
        /// Reads save data, updates all nodes, positions or animates the car,
        /// auto-centers the viewport, and configures gamepad navigation.
        /// </summary>
        public void RefreshMap()
        {
            AutoFindReferences();
            Canvas.ForceUpdateCanvases();

            // 1. Read Save Data & Determine Level States
            int unlockedCount = 1;
            int lastCompletedLevel = PlayerPrefs.GetInt("LastCompletedLevel", 0);

            for (int i = 1; i <= 10; i++)
            {
                bool isCompleted = PlayerPrefs.GetInt($"Level_{i}_Completed", 0) == 1;
                int stars = PlayerPrefs.GetInt($"Level_{i}_Stars", 0);
                if (isCompleted || stars > 0)
                {
                    unlockedCount = Mathf.Max(unlockedCount, i + 1);
                }
            }
            unlockedCount = Mathf.Clamp(unlockedCount, 1, 10);

            // Determine the "Current" level
            currentLevelIndex = unlockedCount - 1;

            // 2. Configure Nodes
            for (int i = 0; i < levelNodes.Count; i++)
            {
                if (levelNodes[i] == null) continue;

                int levelNum = i + 1;
                string title = i < LevelTitles.Length ? LevelTitles[i] : $"Level {levelNum}";
                levelNodes[i].Initialize(levelNum, title);

                bool isCompleted = PlayerPrefs.GetInt($"Level_{levelNum}_Completed", 0) == 1;
                int stars = PlayerPrefs.GetInt($"Level_{levelNum}_Stars", 0);

                if (i == currentLevelIndex)
                {
                    levelNodes[i].SetState(LevelNodeState.Current, stars);
                }
                else if (i < currentLevelIndex || isCompleted)
                {
                    levelNodes[i].SetState(LevelNodeState.Completed, stars);
                }
                else
                {
                    levelNodes[i].SetState(LevelNodeState.Locked, 0);
                }
            }

            // 3. Setup Explicit Gamepad / Keyboard Navigation
            SetupGamepadNavigation();

            // 4. Position or Animate Player Car
            int prevLevelIndex = lastCompletedLevel - 1;
            bool shouldAnimateCar = lastCompletedLevel > 0 &&
                                    prevLevelIndex >= 0 &&
                                    prevLevelIndex < currentLevelIndex &&
                                    prevLevelIndex < levelAnchors.Count;

            if (shouldAnimateCar)
            {
                // Clear LastCompletedLevel flag so it doesn't replay next time
                PlayerPrefs.SetInt("LastCompletedLevel", 0);
                PlayerPrefs.Save();

                if (carAnimationCoroutine != null) StopCoroutine(carAnimationCoroutine);
                if (gameObject.activeInHierarchy)
                {
                    carAnimationCoroutine = StartCoroutine(AnimateCarDrive(prevLevelIndex, currentLevelIndex));
                }
                else
                {
                    SnapCarToNode(currentLevelIndex);
                }
            }
            else
            {
                SnapCarToNode(currentLevelIndex);
            }

            // 5. Position Checkered Flag at Level 10
            if (checkeredFlagRect != null && levelAnchors.Count >= 10 && levelAnchors[9] != null)
            {
                Vector2 finishPos = levelAnchors[9].anchoredPosition + new Vector2(75f, 25f);
                checkeredFlagRect.anchoredPosition = finishPos;
            }

            // 6. Auto-Scroll to Center Current Level
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(DelayedAutoCenter(currentLevelIndex));
            }
        }

        private void SnapCarToNode(int nodeIndex)
        {
            if (playerCarRect == null || nodeIndex < 0 || nodeIndex >= levelAnchors.Count || levelAnchors[nodeIndex] == null)
            {
                return;
            }

            Vector2 nodePos = levelAnchors[nodeIndex].anchoredPosition;
            playerCarRect.anchoredPosition = nodePos + new Vector2(0f, 15f);
            playerCarRect.localRotation = Quaternion.Euler(0f, 0f, 0f);
            playerCarRect.SetAsLastSibling();
        }

        private IEnumerator AnimateCarDrive(int fromIndex, int toIndex)
        {
            if (playerCarRect == null || roadCurveRenderer == null)
            {
                SnapCarToNode(toIndex);
                yield break;
            }

            List<Vector2> path = roadCurveRenderer.GetRoadPathBetweenNodes(fromIndex, toIndex, samples: 36);
            if (path == null || path.Count < 2)
            {
                SnapCarToNode(toIndex);
                yield break;
            }

            playerCarRect.SetAsLastSibling();
            SnapCarToNode(fromIndex);

            float elapsed = 0f;
            int count = path.Count;

            while (elapsed < carDriveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float rawT = Mathf.Clamp01(elapsed / carDriveDuration);
                // Ease In-Out Sine
                float t = -(Mathf.Cos(Mathf.PI * rawT) - 1f) / 2f;

                float indexFloat = t * (count - 1);
                int seg = Mathf.FloorToInt(indexFloat);
                seg = Mathf.Clamp(seg, 0, count - 2);
                float segT = indexFloat - seg;

                Vector2 currentPos = Vector2.Lerp(path[seg], path[seg + 1], segT);
                Vector2 forward = (path[seg + 1] - path[seg]).normalized;

                playerCarRect.anchoredPosition = currentPos;

                if (forward.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
                    playerCarRect.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
                }

                yield return null;
            }

            SnapCarToNode(toIndex);
        }

        private IEnumerator DelayedAutoCenter(int targetIndex)
        {
            yield return null; // Wait for end of frame / layout pass
            Canvas.ForceUpdateCanvases();
            CenterOnLevel(targetIndex);
        }

        /// <summary>
        /// Centers the horizontal ScrollRect on the specified level node.
        /// </summary>
        public void CenterOnLevel(int nodeIndex)
        {
            if (scrollRect == null || contentRect == null || viewportRect == null) return;
            if (nodeIndex < 0 || nodeIndex >= levelAnchors.Count || levelAnchors[nodeIndex] == null) return;

            float contentWidth = contentRect.rect.width;
            float viewportWidth = viewportRect.rect.width;
            float scrollableWidth = contentWidth - viewportWidth;

            if (scrollableWidth <= 0.01f)
            {
                scrollRect.horizontalNormalizedPosition = 0f;
                return;
            }

            // Node position relative to the left of the content
            // Assuming content pivot is (0, 0.5) or (0.5, 0.5)
            float nodeX = levelAnchors[nodeIndex].anchoredPosition.x;
            if (contentRect.pivot.x > 0.1f)
            {
                nodeX += contentWidth * contentRect.pivot.x;
            }

            float targetX = nodeX - (viewportWidth * 0.5f);
            float normalized = Mathf.Clamp01(targetX / scrollableWidth);

            if (autoScrollCoroutine != null) StopCoroutine(autoScrollCoroutine);
            if (gameObject.activeInHierarchy)
            {
                autoScrollCoroutine = StartCoroutine(SmoothScrollTo(normalized, 0.35f));
            }
            else
            {
                scrollRect.horizontalNormalizedPosition = normalized;
            }
        }

        private IEnumerator SmoothScrollTo(float targetNormalized, float duration)
        {
            float start = scrollRect.horizontalNormalizedPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                scrollRect.horizontalNormalizedPosition = Mathf.Lerp(start, targetNormalized, t);
                yield return null;
            }

            scrollRect.horizontalNormalizedPosition = targetNormalized;
        }

        private void SetupGamepadNavigation()
        {
            for (int i = 0; i < levelNodes.Count; i++)
            {
                if (levelNodes[i] == null || levelNodes[i].Button == null) continue;

                Navigation nav = new Navigation
                {
                    mode = Navigation.Mode.Explicit
                };

                if (i > 0 && levelNodes[i - 1] != null && levelNodes[i - 1].Button != null)
                {
                    nav.selectOnLeft = levelNodes[i - 1].Button;
                }

                if (i < levelNodes.Count - 1 && levelNodes[i + 1] != null && levelNodes[i + 1].Button != null)
                {
                    nav.selectOnRight = levelNodes[i + 1].Button;
                }

                if (backToMenuButton != null)
                {
                    nav.selectOnDown = backToMenuButton;
                }

                levelNodes[i].Button.navigation = nav;
            }

            if (backToMenuButton != null && levelNodes.Count > 0 && levelNodes[0] != null)
            {
                Navigation backNav = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = levelNodes[currentLevelIndex].Button
                };
                backToMenuButton.navigation = backNav;
            }

            // Select the current level button by default
            if (EventSystem.current != null && currentLevelIndex < levelNodes.Count && levelNodes[currentLevelIndex] != null)
            {
                EventSystem.current.SetSelectedGameObject(levelNodes[currentLevelIndex].Button.gameObject);
            }
        }
    }
}
