using System.Collections.Generic;
using UnityEngine;

namespace TrafficTown2D.Level6
{
    /// <summary>
    /// Holds road network segment metadata, waypoint definitions, and active roadway bounds for Level 6.
    /// Provides helper methods for querying road bounds, active route paths, and off-road detection.
    /// </summary>
    public class Level6RoadNetwork : MonoBehaviour
    {
        public static Level6RoadNetwork Instance { get; private set; }

        [Header("Key Road Landmarks (World Coordinates)")]
        public Vector2 spawnMission1 = new Vector2(-45f, -35f);
        public Vector2 corner90Deg = new Vector2(-45f, -15f);
        public Vector2 curveMidpoint = new Vector2(5f, -18f);
        public Vector2 eastDepot = new Vector2(25f, -15f);
        public Vector2 forkJunction = new Vector2(25f, 0f);
        public Vector2 floodedZoneCenter = new Vector2(12f, 8f);
        public Vector2 detourApex = new Vector2(38f, 15f);
        public Vector2 roundaboutCenter = new Vector2(-15f, 25f);
        public Vector2 northernStation = new Vector2(0f, 45f);
        public Vector2 stormBlockagePos = new Vector2(40f, 45f);
        public Vector2 uTurnLoopCenter = new Vector2(30f, 37f);
        public Vector2 tJunctionPos = new Vector2(-15f, -15f);
        public Vector2 finalRescueCenter = new Vector2(45f, 40f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Queries whether a given world coordinate is within the paved road network corridors.
        /// Used by Level6PlayerCar for off-road surface drag, speed capping, and safety infractions.
        /// </summary>
        public bool IsPointOnRoad(Vector2 pos)
        {
            // 1. SW Avenue: X in [-50.0, -40.0], Y in [-47.0, -10.5]
            if (pos.x >= -50.0f && pos.x <= -40.0f && pos.y >= -47.0f && pos.y <= -10.5f) return true;

            // 2. South Connector & 90-Deg Corner & T-Junction Box:
            if (pos.x >= -50.0f && pos.x <= -10.0f && pos.y >= -20.0f && pos.y <= -10.0f) return true;

            // 3. North Leg from T-Junction to Roundabout:
            if (pos.x >= -20.0f && pos.x <= -10.0f && pos.y >= -19.5f && pos.y <= 18.0f) return true;

            // 4. Curved Road Valley from (-11.4, -15) to (20, -15) (exact spline sampling):
            if (pos.x >= -14.0f && pos.x <= 23.0f && pos.y >= -28.0f && pos.y <= -8.0f)
            {
                Vector2 c0 = new Vector2(-11.4f, -15f);
                Vector2 c1 = new Vector2(4.3f, -23f);
                Vector2 c2 = new Vector2(20f, -15f);
                Vector2 prevPt = c0;
                float minSqDist = float.MaxValue;
                for (int i = 1; i <= 24; i++)
                {
                    float t = i / 24f;
                    Vector2 currPt = (1f - t) * (1f - t) * c0 + 2f * (1f - t) * t * c1 + t * t * c2;
                    float d = DistancePointToSegment(pos, prevPt, currPt);
                    if (d < minSqDist) minSqDist = d;
                    prevPt = currPt;
                }
                if (minSqDist <= 5.5f) return true;
            }

            // 5. East Depot Pad:
            if (pos.x >= 18.0f && pos.x <= 32.0f && pos.y >= -21.5f && pos.y <= -8.5f) return true;

            // 6. Fork Approach:
            if (pos.x >= 19.5f && pos.x <= 30.5f && pos.y >= -16.0f && pos.y <= 6.0f) return true;

            // 7. Branch A (Flooded Diagonal Route) from (25, 0) to (0, 25):
            Vector2 p1 = new Vector2(25f, 0f);
            Vector2 p2 = new Vector2(0f, 25f);
            float distToDiag = DistancePointToSegment(pos, p1, p2);
            if (distToDiag <= 5.5f) return true;

            // 8. Branch B Detour:
            // CurveIn / East Detour:
            if (pos.x >= 23.5f && pos.x <= 43.5f && pos.y >= -3.0f && pos.y <= 23.0f) return true;
            // North Arm:
            if (pos.x >= -6.0f && pos.x <= 43.5f && pos.y >= 19.5f && pos.y <= 30.5f) return true;

            // 9. Roundabout at (-15, 25): outer radius 11.5m, inner island 5.5m
            float distToRb = Vector2.Distance(pos, new Vector2(-15f, 25f));
            if (distToRb <= 14.0f && distToRb >= 4.2f) return true;
            if (distToRb < 4.2f) return false;

            // 10. Roundabout West Arm & Logistics Hub:
            if (pos.x >= -49.0f && pos.x <= -14.0f && pos.y >= 19.5f && pos.y <= 30.5f) return true;

            // 11. Roundabout North Arm & NW Corner:
            if (pos.x >= -20.0f && pos.x <= -10.0f && pos.y >= 32.0f && pos.y <= 50.0f) return true;

            // 12. Northern Expressway & NW Corner:
            if (pos.x >= -20.0f && pos.x <= 53.0f && pos.y >= 39.5f && pos.y <= 50.5f) return true;

            // 13. U-Turn Loop at (30, 37.5):
            float distToUt = Vector2.Distance(pos, new Vector2(30f, 37.5f));
            if (distToUt <= 11.8f && distToUt >= 3.0f && pos.y <= 46.5f) return true;

            return false;
        }

        private static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(point - a, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            Vector2 closest = a + t * ab;
            return Vector2.Distance(point, closest);
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize road network landmark points in Editor
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnMission1, 2f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(corner90Deg, 3f);
            Gizmos.DrawWireSphere(curveMidpoint, 3f);
            Gizmos.DrawWireSphere(eastDepot, 2.5f);
            Gizmos.DrawWireSphere(forkJunction, 3f);
            Gizmos.DrawWireSphere(detourApex, 3f);
            Gizmos.DrawWireSphere(roundaboutCenter, 11f);
            Gizmos.DrawWireSphere(uTurnLoopCenter, 7f);
            Gizmos.DrawWireSphere(tJunctionPos, 3f);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(floodedZoneCenter, new Vector3(12f, 8f, 0f));

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(stormBlockagePos, new Vector3(8f, 6f, 0f));

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(northernStation, 2.5f);
            Gizmos.DrawWireSphere(finalRescueCenter, 2.5f);
        }
    }
}
