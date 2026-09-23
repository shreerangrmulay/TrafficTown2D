using UnityEngine;
using UnityEngine.UI;

namespace TrafficTown2D.Level6
{
    /// <summary>
    /// Lightweight UI Mini-Map that tracks the player car, destination, and active hazards
    /// across the Level 6 road network bounds [-75, 75], [-55, 55].
    /// </summary>
    public class Level6MiniMapController : MonoBehaviour
    {
        [Header("UI Rectangles")]
        [SerializeField] private RectTransform mapRect;
        [SerializeField] private RectTransform playerBlip;
        [SerializeField] private RectTransform destinationBlip;

        [Header("World Space Bounds")]
        [SerializeField] private Vector2 mapMinWorld = new Vector2(-60f, -50f);
        [SerializeField] private Vector2 mapMaxWorld = new Vector2(55f, 55f);

        [Header("References")]
        [SerializeField] private Level6PlayerCar playerCar;
        [SerializeField] private Level6Destination destination;

        private void Start()
        {
            if (mapRect == null)
            {
                mapRect = GetComponent<RectTransform>();
            }

            if (playerCar == null)
            {
                playerCar = FindFirstObjectByType<Level6PlayerCar>();
            }

            if (destination == null)
            {
                destination = FindFirstObjectByType<Level6Destination>();
            }

            BuildRoadsGraphic();
        }

        private void BuildRoadsGraphic()
        {
            if (mapRect == null) return;

            Transform existing = mapRect.Find("RoadLinesContainer");
            if (existing != null) Destroy(existing.gameObject);

            GameObject roadsObj = new GameObject("RoadLinesContainer");
            roadsObj.transform.SetParent(mapRect, false);
            roadsObj.transform.SetAsFirstSibling();

            Color roadCol = new Color(0.40f, 0.48f, 0.58f, 0.95f); // Crisp road line on dark minimap
            Color floodCol = new Color(0.20f, 0.55f, 0.90f, 0.95f); // Flooded branch
            Color blockedCol = new Color(0.92f, 0.25f, 0.20f, 0.95f); // Blocked highway

            // 1. South-West Avenue
            DrawMapLine(roadsObj.transform, new Vector2(-45f, -38f), new Vector2(-45f, -15f), roadCol, 4.5f);
            // 2. South Connector
            DrawMapLine(roadsObj.transform, new Vector2(-45f, -15f), new Vector2(-15f, -15f), roadCol, 4.5f);
            // 3. T-Junction North to Roundabout
            DrawMapLine(roadsObj.transform, new Vector2(-15f, -15f), new Vector2(-15f, 13.5f), roadCol, 4.5f);

            // 4. Curved Road (valley arc)
            Vector2 p0 = new Vector2(-15f, -15f);
            Vector2 p1 = new Vector2(5f, -22f);
            Vector2 p2 = new Vector2(25f, -15f);
            Vector2 prev = p0;
            for (int i = 1; i <= 8; i++)
            {
                float t = i / 8f;
                Vector2 curr = Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;
                DrawMapLine(roadsObj.transform, prev, curr, roadCol, 4.5f);
                prev = curr;
            }

            // 5. East Depot approach & Decision Fork
            DrawMapLine(roadsObj.transform, new Vector2(25f, -15f), new Vector2(25f, 0f), roadCol, 4.5f);

            // 6. Branch A (Flooded route from Fork to Roundabout entry)
            DrawMapLine(roadsObj.transform, new Vector2(25f, 0f), new Vector2(0f, 25f), floodCol, 4.5f);

            // 7. Branch B (Elevated safe detour route from Fork around hazard)
            DrawMapLine(roadsObj.transform, new Vector2(25f, 0f), new Vector2(38f, 5f), roadCol, 4.5f);
            DrawMapLine(roadsObj.transform, new Vector2(38f, 5f), new Vector2(38f, 25f), roadCol, 4.5f);
            DrawMapLine(roadsObj.transform, new Vector2(38f, 25f), new Vector2(-3.5f, 25f), roadCol, 4.5f);

            // 8. 2-Lane Roundabout Circle at (-15, 25)
            DrawMapCircle(roadsObj.transform, new Vector2(-15f, 25f), 11.5f, roadCol, 3.5f);

            // West Arm to Logistics Hub
            DrawMapLine(roadsObj.transform, new Vector2(-26.5f, 25f), new Vector2(-42f, 25f), roadCol, 4.5f);

            // North Arm to Northern Expressway
            DrawMapLine(roadsObj.transform, new Vector2(-15f, 36.5f), new Vector2(-15f, 45f), roadCol, 4.5f);

            // 9. Northern Expressway
            DrawMapLine(roadsObj.transform, new Vector2(-15f, 45f), new Vector2(36f, 45f), roadCol, 4.5f);
            DrawMapLine(roadsObj.transform, new Vector2(36f, 45f), new Vector2(46f, 45f), blockedCol, 5.0f); // Blocked highway

            // 10. Dedicated U-Turn Loop at (30, 45)
            DrawMapArc(roadsObj.transform, new Vector2(30f, 37.5f), 7.5f, 90f, -90f, roadCol, 4.5f);
        }

        private void DrawMapLine(Transform parent, Vector2 pA, Vector2 pB, Color col, float thickness)
        {
            Vector2 normA = WorldToNormalized(pA);
            Vector2 normB = WorldToNormalized(pB);

            float mapW = mapRect.rect.width;
            float mapH = mapRect.rect.height;

            Vector2 uiA = new Vector2((normA.x - 0.5f) * mapW, (normA.y - 0.5f) * mapH);
            Vector2 uiB = new Vector2((normB.x - 0.5f) * mapW, (normB.y - 0.5f) * mapH);

            Vector2 dir = uiB - uiA;
            float len = dir.magnitude;
            if (len < 0.5f) return;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            GameObject lineObj = new GameObject("MapLine");
            lineObj.transform.SetParent(parent, false);

            RectTransform rt = lineObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(len, thickness);
            rt.anchoredPosition = (uiA + uiB) * 0.5f;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);

            Image img = lineObj.AddComponent<Image>();
            img.color = col;
            img.raycastTarget = false;
        }

        private void DrawMapCircle(Transform parent, Vector2 center, float radius, Color col, float thickness)
        {
            int segments = 16;
            for (int i = 0; i < segments; i++)
            {
                float a1 = i * (360f / segments) * Mathf.Deg2Rad;
                float a2 = (i + 1) * (360f / segments) * Mathf.Deg2Rad;
                Vector2 p1 = center + new Vector2(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius);
                Vector2 p2 = center + new Vector2(Mathf.Cos(a2) * radius, Mathf.Sin(a2) * radius);
                DrawMapLine(parent, p1, p2, col, thickness);
            }
        }

        private void DrawMapArc(Transform parent, Vector2 center, float radius, float startDeg, float endDeg, Color col, float thickness)
        {
            int steps = 8;
            for (int i = 0; i < steps; i++)
            {
                float a1 = Mathf.Lerp(startDeg, endDeg, i / (float)steps) * Mathf.Deg2Rad;
                float a2 = Mathf.Lerp(startDeg, endDeg, (i + 1) / (float)steps) * Mathf.Deg2Rad;
                Vector2 p1 = center + new Vector2(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius);
                Vector2 p2 = center + new Vector2(Mathf.Cos(a2) * radius, Mathf.Sin(a2) * radius);
                DrawMapLine(parent, p1, p2, col, thickness);
            }
        }

        private void LateUpdate()
        {
            if (mapRect == null) return;

            float mapWidth = mapRect.rect.width;
            float mapHeight = mapRect.rect.height;

            // 1. Update Player Blip
            if (playerCar != null && playerBlip != null)
            {
                Vector2 playerPos = playerCar.transform.position;
                Vector2 normPos = WorldToNormalized(playerPos);
                playerBlip.anchoredPosition = new Vector2(
                    (normPos.x - 0.5f) * mapWidth,
                    (normPos.y - 0.5f) * mapHeight
                );

                // Rotate player blip to match car heading
                playerBlip.localRotation = Quaternion.Euler(0f, 0f, playerCar.transform.eulerAngles.z);
            }

            // 2. Update Destination Blip
            if (destination != null && destinationBlip != null)
            {
                if (destination.gameObject.activeInHierarchy)
                {
                    destinationBlip.gameObject.SetActive(true);
                    Vector2 destPos = destination.transform.position;
                    Vector2 normDest = WorldToNormalized(destPos);
                    destinationBlip.anchoredPosition = new Vector2(
                        (normDest.x - 0.5f) * mapWidth,
                        (normDest.y - 0.5f) * mapHeight
                    );

                    // Pulsing scale for destination
                    float pulse = 1f + 0.18f * Mathf.Sin(Time.time * 6f);
                    destinationBlip.localScale = new Vector3(pulse, pulse, 1f);
                }
                else
                {
                    destinationBlip.gameObject.SetActive(false);
                }
            }
        }

        private Vector2 WorldToNormalized(Vector2 worldPos)
        {
            float normX = Mathf.InverseLerp(mapMinWorld.x, mapMaxWorld.x, worldPos.x);
            float normY = Mathf.InverseLerp(mapMinWorld.y, mapMaxWorld.y, worldPos.y);
            return new Vector2(Mathf.Clamp01(normX), Mathf.Clamp01(normY));
        }

        public void BindReferences(Level6PlayerCar car, Level6Destination dest, RectTransform mRect, RectTransform pBlip, RectTransform dBlip)
        {
            playerCar = car;
            destination = dest;
            mapRect = mRect;
            playerBlip = pBlip;
            destinationBlip = dBlip;
        }
    }
}
