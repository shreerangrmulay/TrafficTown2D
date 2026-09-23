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
            // 1. SW Avenue: X in [-49.5, -40.5], Y in [-42.0, -11.0]
            if (pos.x >= -49.5f && pos.x <= -40.5f && pos.y >= -42.0f && pos.y <= -11.0f) return true;

            // 2. South Connector & 90-Deg Corner: X in [-49.5, -11.0], Y in [-19.5, -10.5]
            if (pos.x >= -49.5f && pos.x <= -11.0f && pos.y >= -19.5f && pos.y <= -10.5f) return true;

            // 3. North Leg from T-Junction: X in [-19.5, -10.5], Y in [-19.5, 18.0]
            if (pos.x >= -19.5f && pos.x <= -10.5f && pos.y >= -19.5f && pos.y <= 18.0f) return true;

            // 4. Curved Road Valley from (-15, -15) to (25, -15):
            if (pos.x >= -15.0f && pos.x <= 25.0f && pos.y >= -26.0f && pos.y <= -10.5f)
            {
                float t = Mathf.Clamp01((pos.x + 15f) / 40f);
                float curveY = Mathf.Pow(1 - t, 2) * -15f + 2 * (1 - t) * t * -23f + Mathf.Pow(t, 2) * -15f;
                if (Mathf.Abs(pos.y - curveY) <= 5.0f) return true;
            }

            // 5. East Depot Pad: X in [19.0, 31.0], Y in [-20.5, -9.5]
            if (pos.x >= 19.0f && pos.x <= 31.0f && pos.y >= -20.5f && pos.y <= -9.5f) return true;

            // 6. Fork Approach: X in [20.5, 29.5], Y in [-15.0, 4.5]
            if (pos.x >= 20.5f && pos.x <= 29.5f && pos.y >= -15.0f && pos.y <= 4.5f) return true;

            // 7. Branch A (Flooded Diagonal Route) from (25, 0) to (0, 25):
            Vector2 p1 = new Vector2(25f, 0f);
            Vector2 p2 = new Vector2(0f, 25f);
            float distToDiag = DistancePointToSegment(pos, p1, p2);
            if (distToDiag <= 4.8f) return true;

            // 8. Branch B Detour:
            // CurveIn / East Detour: X in [25.0, 42.0], Y in [-2.0, 19.5]
            if (pos.x >= 25.0f && pos.x <= 42.0f && pos.y >= -2.0f && pos.y <= 19.5f) return true;
            // North Arm: X in [-5.0, 42.0], Y in [20.5, 29.5]
            if (pos.x >= -5.0f && pos.x <= 42.0f && pos.y >= 20.5f && pos.y <= 29.5f) return true;

            // 9. Roundabout at (-15, 25): outer radius 12.5m, inner island 5.0m
            float distToRb = Vector2.Distance(pos, new Vector2(-15f, 25f));
            if (distToRb <= 12.5f && distToRb >= 5.0f) return true;
            if (distToRb < 5.0f) return false;

            // 10. Roundabout West Arm & Logistics Hub: X in [-48.0, -15.0], Y in [20.5, 29.5]
            if (pos.x >= -48.0f && pos.x <= -15.0f && pos.y >= 20.5f && pos.y <= 29.5f) return true;

            // 11. Roundabout North Arm: X in [-19.5, -10.5], Y in [33.0, 49.0]
            if (pos.x >= -19.5f && pos.x <= -10.5f && pos.y >= 33.0f && pos.y <= 49.0f) return true;

            // 12. Northern Expressway: X in [-19.5, 52.0], Y in [40.5, 49.5]
            if (pos.x >= -19.5f && pos.x <= 52.0f && pos.y >= 40.5f && pos.y <= 49.5f) return true;

            // 13. U-Turn Loop at (30, 37.5): half-circle extending south from expressway
            float distToUt = Vector2.Distance(pos, new Vector2(30f, 37.5f));
            if (distToUt <= 8.5f && distToUt >= 3.0f && pos.y <= 45.0f) return true;

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
