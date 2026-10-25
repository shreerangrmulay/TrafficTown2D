using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TrafficTown2D.UI
{
    /// <summary>
    /// Custom uGUI Graphic that renders a continuous road ribbon or dashed yellow line mesh.
    /// Resides in its own file so Unity can serialize it properly in scene assets without missing script warnings.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIRoadMesh : MaskableGraphic
    {
        private readonly List<UIVertex> vertices = new List<UIVertex>();
        private readonly List<int> indices = new List<int>();

        public void GenerateRibbon(List<Vector2> points, float width, bool isDashed, float dashLen, float gapLen)
        {
            vertices.Clear();
            indices.Clear();

            if (points == null || points.Count < 2)
            {
                SetVerticesDirty();
                return;
            }

            float halfWidth = width * 0.5f;

            if (!isDashed)
            {
                // Solid continuous ribbon
                for (int i = 0; i < points.Count; i++)
                {
                    Vector2 forward = Vector2.right;
                    if (i < points.Count - 1 && i > 0)
                    {
                        forward = (points[i + 1] - points[i - 1]).normalized;
                    }
                    else if (i < points.Count - 1)
                    {
                        forward = (points[i + 1] - points[i]).normalized;
                    }
                    else
                    {
                        forward = (points[i] - points[i - 1]).normalized;
                    }

                    Vector2 normal = new Vector2(-forward.y, forward.x);
                    Vector2 left = points[i] + normal * halfWidth;
                    Vector2 right = points[i] - normal * halfWidth;

                    AddVertex(left);
                    AddVertex(right);

                    if (i > 0)
                    {
                        int vertCount = vertices.Count;
                        int bl = vertCount - 4;
                        int br = vertCount - 3;
                        int tl = vertCount - 2;
                        int tr = vertCount - 1;

                        indices.Add(bl);
                        indices.Add(tl);
                        indices.Add(br);

                        indices.Add(br);
                        indices.Add(tl);
                        indices.Add(tr);
                    }
                }
            }
            else
            {
                // Dashed ribbon
                float currentDistance = 0f;
                float cycle = dashLen + gapLen;

                for (int i = 0; i < points.Count - 1; i++)
                {
                    Vector2 pA = points[i];
                    Vector2 pB = points[i + 1];
                    float segDist = Vector2.Distance(pA, pB);
                    if (segDist < 0.001f) continue;

                    Vector2 fwd = (pB - pA) / segDist;
                    Vector2 norm = new Vector2(-fwd.y, fwd.x);

                    float progressInSeg = 0f;
                    while (progressInSeg < segDist)
                    {
                        float cyclePos = (currentDistance + progressInSeg) % cycle;
                        if (cyclePos < dashLen)
                        {
                            float dashRemaining = dashLen - cyclePos;
                            float step = Mathf.Min(dashRemaining, segDist - progressInSeg);

                            Vector2 startPt = pA + fwd * progressInSeg;
                            Vector2 endPt = pA + fwd * (progressInSeg + step);

                            AddQuad(
                                startPt + norm * halfWidth,
                                endPt + norm * halfWidth,
                                startPt - norm * halfWidth,
                                endPt - norm * halfWidth
                            );

                            progressInSeg += step;
                        }
                        else
                        {
                            float gapRemaining = cycle - cyclePos;
                            float step = Mathf.Min(gapRemaining, segDist - progressInSeg);
                            progressInSeg += step;
                        }
                    }

                    currentDistance += segDist;
                }
            }

            SetVerticesDirty();
        }

        private void AddVertex(Vector2 position)
        {
            UIVertex vert = UIVertex.simpleVert;
            vert.position = position;
            vert.color = color;
            vertices.Add(vert);
        }

        private void AddQuad(Vector2 tl, Vector2 tr, Vector2 bl, Vector2 br)
        {
            int startIndex = vertices.Count;

            UIVertex v1 = UIVertex.simpleVert; v1.position = bl; v1.color = color;
            UIVertex v2 = UIVertex.simpleVert; v2.position = tl; v2.color = color;
            UIVertex v3 = UIVertex.simpleVert; v3.position = br; v3.color = color;
            UIVertex v4 = UIVertex.simpleVert; v4.position = tr; v4.color = color;

            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            vertices.Add(v4);

            indices.Add(startIndex);
            indices.Add(startIndex + 1);
            indices.Add(startIndex + 2);

            indices.Add(startIndex + 2);
            indices.Add(startIndex + 1);
            indices.Add(startIndex + 3);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (vertices.Count >= 3 && indices.Count >= 3)
            {
                vh.AddUIVertexStream(vertices, indices);
            }
        }
    }
}
