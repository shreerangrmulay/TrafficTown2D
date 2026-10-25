using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrafficTown2D.Player
{
    /// <summary>
    /// Universal guardian component that strictly prevents player cars from leaving the road surface
    /// and driving into grass, sidewalks, or off-road scenery across all driving levels.
    /// Combines strict geometric containment with physics velocity clamping.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class RoadBoundaryKeeper : MonoBehaviour
    {
        private Rigidbody2D rb;
        private Vector2 lastValidRoadPosition;
        private bool hasValidPosition = false;
        private string activeSceneName;

        [Header("Car Dimensions")]
        [SerializeField] private float carHalfWidth = 0.75f;
        [SerializeField] private float carHalfLength = 1.4f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitSceneListener()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachToActivePlayerCars();
        }

        /// <summary>
        /// Automatically attaches RoadBoundaryKeeper to any active player car in the current scene.
        /// </summary>
        public static void AttachActivePlayerCar(GameObject playerCarObj)
        {
            if (playerCarObj != null && playerCarObj.GetComponent<RoadBoundaryKeeper>() == null)
            {
                playerCarObj.AddComponent<RoadBoundaryKeeper>();
            }
        }

        private static void AttachToActivePlayerCars()
        {
            // Level 3 / 4 / 8 / 9 Player Car
            var l3Cars = UnityEngine.Object.FindObjectsByType<Level3.Level3PlayerCar>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < l3Cars.Length; i++)
            {
                AttachActivePlayerCar(l3Cars[i].gameObject);
            }

            // Level 6 Player Car
            var l6Cars = UnityEngine.Object.FindObjectsByType<Level6.Level6PlayerCar>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < l6Cars.Length; i++)
            {
                AttachActivePlayerCar(l6Cars[i].gameObject);
            }

            // Level 7 Player Car
            var l7Cars = UnityEngine.Object.FindObjectsByType<Level7.Level7PlayerCar>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < l7Cars.Length; i++)
            {
                AttachActivePlayerCar(l7Cars[i].gameObject);
            }

            // Level 10 Player Car
            var cpCars = UnityEngine.Object.FindObjectsByType<CarPlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < cpCars.Length; i++)
            {
                AttachActivePlayerCar(cpCars[i].gameObject);
            }
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            activeSceneName = SceneManager.GetActiveScene().name;

            if (rb != null)
            {
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        private void Start()
        {
            activeSceneName = SceneManager.GetActiveScene().name;
            lastValidRoadPosition = transform.position;
            hasValidPosition = true;
        }

        private void FixedUpdate()
        {
            EnforceContainment();
        }

        private void LateUpdate()
        {
            EnforceContainment();
        }

        private void EnforceContainment()
        {
            if (rb == null) return;

            string scene = string.IsNullOrEmpty(activeSceneName) ? SceneManager.GetActiveScene().name : activeSceneName;
            Vector2 currentPos = rb.position;
            Vector2 velocity = rb.linearVelocity;

            Vector2 clampedPos = currentPos;
            bool isInsideRoad = CheckAndClampPosition(scene, currentPos, ref clampedPos, ref velocity);

            if (isInsideRoad)
            {
                lastValidRoadPosition = clampedPos;
                hasValidPosition = true;
            }
            else
            {
                // Car attempted to leave road surface into grass!
                // Force back strictly to last valid road position or clamped position
                clampedPos = hasValidPosition ? lastValidRoadPosition : clampedPos;
                velocity = Vector2.zero;
            }

            if ((clampedPos - currentPos).sqrMagnitude > 0.0001f)
            {
                rb.position = clampedPos;
                transform.position = new Vector3(clampedPos.x, clampedPos.y, transform.position.z);
                rb.linearVelocity = velocity;
            }
        }

        private bool CheckAndClampPosition(string scene, Vector2 pos, ref Vector2 clamped, ref Vector2 vel)
        {
            switch (scene)
            {
                case "Level3":
                    return ClampLevel3(pos, ref clamped, ref vel);

                case "Level4":
                    return ClampLevel4(pos, ref clamped, ref vel);

                case "Level8":
                    return ClampLevel8(pos, ref clamped, ref vel);

                case "Level10":
                    return ClampLevel10(pos, ref clamped, ref vel);

                case "Level9":
                case "Level9_Roundabout":
                    return ClampLevel9(pos, ref clamped, ref vel);

                case "Level7":
                    return ClampLevel7(pos, ref clamped, ref vel);

                case "Level6":
                    return ClampLevel6(pos, ref clamped, ref vel);

                default:
                    return true;
            }
        }

        #region Level-Specific Road Boundaries
        /// <summary>
        /// Level 3: Vertical straight road.
        /// Road width = 6.2 (asphalt X: -3.1 to +3.1, Y: -20 to 190).
        /// </summary>
        private bool ClampLevel3(Vector2 pos, ref Vector2 clamped, ref Vector2 vel)
        {
            float minX = -3.1f + carHalfWidth;
            float maxX = 3.1f - carHalfWidth;
            float minY = -18.5f;
            float maxY = 185.0f;

            bool valid = pos.x >= minX && pos.x <= maxX && pos.y >= minY && pos.y <= maxY;
            clamped.x = Mathf.Clamp(pos.x, minX, maxX);
            clamped.y = Mathf.Clamp(pos.y, minY, maxY);

            if (pos.x <= minX && vel.x < 0f) vel.x = 0f;
            if (pos.x >= maxX && vel.x > 0f) vel.x = 0f;
            if (pos.y <= minY && vel.y < 0f) vel.y = 0f;
            if (pos.y >= maxY && vel.y > 0f) vel.y = 0f;

            return valid;
        }

        /// <summary>
        /// Level 4: Night driving vertical road.
        /// Road width = 6.6 (asphalt X: -3.3 to +3.3, Y: -20 to 200).
        /// </summary>
        private bool ClampLevel4(Vector2 pos, ref Vector2 clamped, ref Vector2 vel)
        {
            float minX = -3.3f + carHalfWidth;
            float maxX = 3.3f - carHalfWidth;
            float minY = -18.5f;
            float maxY = 195.0f;

            bool valid = pos.x >= minX && pos.x <= maxX && pos.y >= minY && pos.y <= maxY;
            clamped.x = Mathf.Clamp(pos.x, minX, maxX);
            clamped.y = Mathf.Clamp(pos.y, minY, maxY);

            if (pos.x <= minX && vel.x < 0f) vel.x = 0f;
            if (pos.x >= maxX && vel.x > 0f) vel.x = 0f;
            if (pos.y <= minY && vel.y < 0f) vel.y = 0f;
            if (pos.y >= maxY && vel.y > 0f) vel.y = 0f;

            return valid;
        }

        /// <summary>
        /// Level 8: The Overtake.
        /// Main 2-lane road: width = 6.6 (asphalt X: -3.3 to +3.3, Y: -20 to 330).
        /// </summary>
        private bool ClampLevel8(Vector2 pos, ref Vector2 clamped, ref Vector2 vel)
        {
            float minX = -3.3f + carHalfWidth;
            float maxX = 3.3f - carHalfWidth;
            float minY = -18.5f;
            float maxY = 326.0f;

            bool valid = pos.x >= minX && pos.x <= maxX && pos.y >= minY && pos.y <= maxY;
            clamped.x = Mathf.Clamp(pos.x, minX, maxX);
            clamped.y = Mathf.Clamp(pos.y, minY, maxY);

            if (pos.x <= minX && vel.x < 0f) vel.x = 0f;
            if (pos.x >= maxX && vel.x > 0f) vel.x = 0f;
            if (pos.y <= minY && vel.y < 0f) vel.y = 0f;
            if (pos.y >= maxY && vel.y > 0f) vel.y = 0f;

            return valid;
        }

        /// <summary>
        /// Level 10: Final Commute.
        /// Horizontal road: Y centered at 0, height 4.4 (asphalt Y: -2.2 to +2.2, X: -13 to +17).
        /// </summary>
        private bool ClampLevel10(Vector2 pos, ref Vector2 clamped, ref Vector2 vel)
        {
            float minY = -2.15f + 0.65f;
            float maxY = 2.15f - 0.65f;
            float minX = -12.2f;
            float maxX = 16.2f;

            bool valid = pos.x >= minX && pos.x <= maxX && pos.y >= minY && pos.y <= maxY;
            clamped.x = Mathf.Clamp(pos.x, minX, maxX);
            clamped.y = Mathf.Clamp(pos.y, minY, maxY);

            if (pos.x <= minX && vel.x < 0f) vel.x = 0f;
            if (pos.x >= maxX && vel.x > 0f) vel.x = 0f;
            if (pos.y <= minY && vel.y < 0f) vel.y = 0f;
            if (pos.y >= maxY && vel.y > 0f) vel.y = 0f;

            return valid;
        }

        /// <summary>
        /// Level 9: Roundabout Rules.
        /// 3 interconnected roundabouts and corridors.
        /// </summary>
        private bool ClampLevel9(Vector2 pos, ref Vector2 clamped, ref Vector2 vel)
        {
            // 1. South Approach Corridor (X: -1.7 to 4.9, Y: -21 to 0)
            if (pos.x >= -1.7f && pos.x <= 4.9f && pos.y >= -21.0f && pos.y <= 1.0f)
            {
                clamped = pos;
                return true;
            }

            // 2. Roundabout 1: Center (0, 0), radius 3.3 to 8.8
            float distR1 = pos.magnitude;
            if (distR1 >= 3.2f && distR1 <= 8.8f)
            {
                clamped = pos;
                return true;
            }

            // 3. Connecting Road 1: X: 0 to 42, Y: -3.2 to 3.2
            if (pos.x >= -0.5f && pos.x <= 42.5f && pos.y >= -3.2f && pos.y <= 3.2f)
            {
                clamped = pos;
                return true;
            }

            // 4. Roundabout 2: Center (42, 22), radius 4.0 to 10.3
            float distR2 = (pos - new Vector2(42f, 22f)).magnitude;
            if (distR2 >= 3.9f && distR2 <= 10.3f)
            {
                clamped = pos;
                return true;
            }

            // 5. Connecting Road 2: X: -1.7 to 45.2, Y: 20 to 74
            if (pos.x >= -2.0f && pos.x <= 45.2f && pos.y >= 20.0f && pos.y <= 74.0f)
            {
                // Can be either east side connecting or central connector
                if ((pos.x >= 38.6f && pos.x <= 45.2f) || (pos.x >= -2.0f && pos.x <= 5.0f && pos.y >= 50f))
                {
                    clamped = pos;
                    return true;
                }
            }

            // 6. Roundabout 3: Center (0, 72), radius 4.8 to 11.8
            float distR3 = (pos - new Vector2(0f, 72f)).magnitude;
            if (distR3 >= 4.7f && distR3 <= 11.8f)
            {
                clamped = pos;
                return true;
            }

            // 7. North Exit & Finish Corridor: X: -3.3 to 3.3, Y: 70 to 98
            if (pos.x >= -3.3f && pos.x <= 3.3f && pos.y >= 70.0f && pos.y <= 98.0f)
            {
                clamped = pos;
                return true;
            }

            // Outside valid road network!
            return false;
        }

        /// <summary>
        /// Level 7: Distracted Driving city street network.
        /// </summary>
        private bool ClampLevel7(Vector2 pos, ref Vector2 clamped, ref Vector2 vel)
        {
            // South Avenue (Y: -23.5 to -16.5, X: -38.5 to 38.5)
            if (pos.x >= -38.5f && pos.x <= 38.5f && pos.y >= -23.5f && pos.y <= -16.5f) { clamped = pos; return true; }
            // SafeStop 1 Bay (South Ave East)
            if (pos.x >= 12.0f && pos.x <= 23.0f && pos.y >= -26.5f && pos.y <= -16.5f) { clamped = pos; return true; }

            // East Boulevard (X: 31.5 to 38.5, Y: -23.5 to 23.5)
            if (pos.x >= 31.5f && pos.x <= 38.5f && pos.y >= -23.5f && pos.y <= 23.5f) { clamped = pos; return true; }
            // SafeStop 3 Bay (East Blvd)
            if (pos.x >= 31.5f && pos.x <= 42.0f && pos.y >= 5.5f && pos.y <= 14.5f) { clamped = pos; return true; }

            // North Avenue (Y: 16.5 to 23.5, X: -38.5 to 38.5)
            if (pos.x >= -38.5f && pos.x <= 38.5f && pos.y >= 16.5f && pos.y <= 23.5f) { clamped = pos; return true; }

            // West Avenue (X: -38.5 to -31.5, Y: -23.5 to 23.5)
            if (pos.x >= -38.5f && pos.x <= -31.5f && pos.y >= -23.5f && pos.y <= 23.5f) { clamped = pos; return true; }
            // SafeStop 2 Bay (West Ave)
            if (pos.x >= -42.0f && pos.x <= -31.5f && pos.y >= -4.5f && pos.y <= 4.5f) { clamped = pos; return true; }

            // Central Street (X: -3.5 to 3.5, Y: -23.5 to 23.5)
            if (pos.x >= -3.5f && pos.x <= 3.5f && pos.y >= -23.5f && pos.y <= 23.5f) { clamped = pos; return true; }

            // Side Road (Y: -3.5 to 3.5, X: -38.5 to 3.5)
            if (pos.x >= -38.5f && pos.x <= 3.5f && pos.y >= -3.5f && pos.y <= 3.5f) { clamped = pos; return true; }

            // Curved Park Way spline area
            if (pos.x >= -36f && pos.x <= -8f && pos.y >= 4f && pos.y <= 22f)
            {
                // Quadratic curve test from (-35, 5) to (-10, 20) with width ~7.2
                float t = Mathf.Clamp01((pos.x + 35f) / 25f);
                float curveCenterY = Mathf.Lerp(5f, 20f, t);
                if (Mathf.Abs(pos.y - curveCenterY) <= 4.2f)
                {
                    clamped = pos;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Level 6: Extreme Road Conditions.
        /// </summary>
        private bool ClampLevel6(Vector2 pos, ref Vector2 clamped, ref Vector2 vel)
        {
            // SW Road
            if (pos.x >= -49.5f && pos.x <= -40.5f && pos.y >= -45f && pos.y <= -11f) { clamped = pos; return true; }
            // Connector Bottom
            if (pos.x >= -45f && pos.x <= -11f && pos.y >= -19.5f && pos.y <= -10.5f) { clamped = pos; return true; }
            // TJunction North
            if (pos.x >= -19.5f && pos.x <= -10.5f && pos.y >= -11f && pos.y <= 27f) { clamped = pos; return true; }
            // Fork Approach
            if (pos.x >= 20.5f && pos.x <= 29.5f && pos.y >= -10f && pos.y <= 2f) { clamped = pos; return true; }
            // Detour East
            if (pos.x >= 20.5f && pos.x <= 42.5f && pos.y >= -1f && pos.y <= 30f) { clamped = pos; return true; }
            // Expressway
            if (pos.x >= -20f && pos.x <= 49f && pos.y >= 40.5f && pos.y <= 49.5f) { clamped = pos; return true; }
            // North curve connector
            if (pos.x >= -19f && pos.x <= 25f && pos.y >= 20f && pos.y <= 49f) { clamped = pos; return true; }
            // U-Turn Loop
            float uDist = (pos - new Vector2(30f, 41.4f)).magnitude;
            if (uDist >= 3.8f && uDist <= 11.5f && pos.y <= 42f) { clamped = pos; return true; }

            return false;
        }
        #endregion
    }
}
