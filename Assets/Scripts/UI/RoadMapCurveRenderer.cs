using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TrafficTown2D.UI
{
    /// <summary>
    /// Renders the S-shaped curved winding road and dashed yellow center line
    /// through the 10 level node RectTransforms in the Level Select scroll container.
    /// Also provides smooth spline evaluation for the player car drive animation.
    /// </summary>
    [ExecuteAlways]
    public class RoadMapCurveRenderer : MonoBehaviour
    {
        [Header("Anchors")]
        [SerializeField] private List<RectTransform> levelAnchors = new List<RectTransform>();

        [Header("Road Styling")]
        [SerializeField] private float roadWidth = 84f;
        [SerializeField] private float centerLineWidth = 8f;
        [SerializeField] private float dashLength = 28f;
        [SerializeField] private float dashGap = 20f;
        [SerializeField] private int samplesPerSegment = 24;

        [Header("Sub-Renderers")]
        [SerializeField] private UIRoadMesh baseRoadMesh;
        [SerializeField] private UIRoadMesh centerDashesMesh;

        public List<RectTransform> Anchors => levelAnchors;
        public float RoadWidth => roadWidth;

        private void OnEnable()
        {
            EnsureSubRenderers();
            RebuildRoad();
        }

        private void OnValidate()
        {
            EnsureSubRenderers();
            RebuildRoad();
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying)
            {
                // In editor, if user drags anchors, update live
                RebuildRoad();
            }
        }
#endif

        public void SetAnchors(List<RectTransform> anchors)
        {
            levelAnchors = anchors;
            RebuildRoad();
        }

        public void EnsureSubRenderers()
        {
            if (baseRoadMesh == null)
            {
                Transform baseT = transform.Find("BaseRoadGraphic");
                if (baseT != null) baseRoadMesh = baseT.GetComponent<UIRoadMesh>();
                if (baseRoadMesh == null)
                {
                    GameObject baseObj = new GameObject("BaseRoadGraphic", typeof(RectTransform), typeof(CanvasRenderer));
                    baseObj.transform.SetParent(transform, false);
                    baseObj.transform.SetAsFirstSibling();
                    baseRoadMesh = baseObj.AddComponent<UIRoadMesh>();
                }
            }

            if (centerDashesMesh == null)
            {
                Transform dashT = transform.Find("CenterDashesGraphic");
                if (dashT != null) centerDashesMesh = dashT.GetComponent<UIRoadMesh>();
                if (centerDashesMesh == null)
                {
                    GameObject dashObj = new GameObject("CenterDashesGraphic", typeof(RectTransform), typeof(CanvasRenderer));
                    dashObj.transform.SetParent(transform, false);
                    dashObj.transform.SetSiblingIndex(1);
                    centerDashesMesh = dashObj.AddComponent<UIRoadMesh>();
                }
            }

            baseRoadMesh.color = ThemeColors.InkBlack;
            centerDashesMesh.color = ThemeColors.RoadDashedYellow;
            baseRoadMesh.raycastTarget = false;
            centerDashesMesh.raycastTarget = false;

            RectTransform baseRt = baseRoadMesh.rectTransform;
            baseRt.anchorMin = Vector2.zero;
            baseRt.anchorMax = Vector2.one;
            baseRt.pivot = new Vector2(0f, 0.5f);
            baseRt.sizeDelta = Vector2.zero;
            baseRt.anchoredPosition = Vector2.zero;

            RectTransform dashRt = centerDashesMesh.rectTransform;
            dashRt.anchorMin = Vector2.zero;
            dashRt.anchorMax = Vector2.one;
            dashRt.pivot = new Vector2(0f, 0.5f);
            dashRt.sizeDelta = Vector2.zero;
            dashRt.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Rebuilds the spline geometry for the black road surface and dashed yellow center line.
        /// </summary>
        public void RebuildRoad()
        {
            if (levelAnchors == null || levelAnchors.Count < 2) return;
            EnsureSubRenderers();

            // Extract local points relative to this transform
            List<Vector2> waypoints = new List<Vector2>(levelAnchors.Count);
            for (int i = 0; i < levelAnchors.Count; i++)
            {
                if (levelAnchors[i] == null) continue;
                Vector3 localPos = transform.InverseTransformPoint(levelAnchors[i].position);
                waypoints.Add(new Vector2(localPos.x, localPos.y));
            }

            if (waypoints.Count < 2) return;

            // Generate dense sampled spline points
            List<Vector2> splinePoints = GenerateCatmullRomSpline(waypoints, samplesPerSegment);
            if (splinePoints.Count < 2) return;

            // Build base black road ribbon
            baseRoadMesh.GenerateRibbon(splinePoints, roadWidth, isDashed: false, 0f, 0f);

            // Build dashed yellow center line ribbon
            centerDashesMesh.GenerateRibbon(splinePoints, centerLineWidth, isDashed: true, dashLength, dashGap);
        }

        /// <summary>
        /// Returns sampled points along the road spline specifically between two node indices.
        /// Used by LevelMapController to animate the player car driving between nodes.
        /// </summary>
        public List<Vector2> GetRoadPathBetweenNodes(int fromIndex, int toIndex, int samples = 30)
        {
            List<Vector2> path = new List<Vector2>();
            if (levelAnchors == null || fromIndex < 0 || toIndex < 0 || fromIndex >= levelAnchors.Count || toIndex >= levelAnchors.Count)
            {
                return path;
            }

            if (fromIndex == toIndex)
            {
                Vector3 p = transform.InverseTransformPoint(levelAnchors[fromIndex].position);
                path.Add(p);
                return path;
            }

            bool forward = toIndex > fromIndex;
            int start = Mathf.Min(fromIndex, toIndex);
            int end = Mathf.Max(fromIndex, toIndex);

            List<Vector2> segmentWaypoints = new List<Vector2>();
            // Add pre-start point for smooth tangent if available
            int preIndex = Mathf.Clamp(start - 1, 0, levelAnchors.Count - 1);
            segmentWaypoints.Add(transform.InverseTransformPoint(levelAnchors[preIndex].position));

            for (int i = start; i <= end; i++)
            {
                segmentWaypoints.Add(transform.InverseTransformPoint(levelAnchors[i].position));
            }

            // Add post-end point for smooth tangent if available
            int postIndex = Mathf.Clamp(end + 1, 0, levelAnchors.Count - 1);
            segmentWaypoints.Add(transform.InverseTransformPoint(levelAnchors[postIndex].position));

            List<Vector2> generated = GenerateCatmullRomSpline(segmentWaypoints, samples);

            // Find sub-slice exactly from start node to end node
            Vector2 startPos = transform.InverseTransformPoint(levelAnchors[start].position);
            Vector2 endPos = transform.InverseTransformPoint(levelAnchors[end].position);

            int startIndex = 0;
            float minStartDist = float.MaxValue;
            for (int i = 0; i < generated.Count; i++)
            {
                float d = Vector2.Distance(generated[i], startPos);
                if (d < minStartDist)
                {
                    minStartDist = d;
                    startIndex = i;
                }
            }

            int endIndex = generated.Count - 1;
            float minEndDist = float.MaxValue;
            for (int i = startIndex; i < generated.Count; i++)
            {
                float d = Vector2.Distance(generated[i], endPos);
                if (d < minEndDist)
                {
                    minEndDist = d;
                    endIndex = i;
                }
            }

            for (int i = startIndex; i <= endIndex; i++)
            {
                path.Add(generated[i]);
            }

            if (!forward)
            {
                path.Reverse();
            }

            return path;
        }

        private static List<Vector2> GenerateCatmullRomSpline(List<Vector2> controlPoints, int samplesPerSegment)
        {
            List<Vector2> result = new List<Vector2>();
            if (controlPoints.Count < 2) return result;

            int count = controlPoints.Count;
            for (int i = 0; i < count - 1; i++)
            {
                Vector2 p0 = i > 0 ? controlPoints[i - 1] : controlPoints[i] - (controlPoints[i + 1] - controlPoints[i]);
                Vector2 p1 = controlPoints[i];
                Vector2 p2 = controlPoints[i + 1];
                Vector2 p3 = i < count - 2 ? controlPoints[i + 2] : controlPoints[i + 1] + (controlPoints[i + 1] - controlPoints[i]);

                for (int s = 0; s < samplesPerSegment; s++)
                {
                    float t = s / (float)samplesPerSegment;
                    result.Add(EvaluateCatmullRom(p0, p1, p2, p3, t));
                }
            }

            result.Add(controlPoints[count - 1]);
            return result;
        }

        private static Vector2 EvaluateCatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }
    }
}

