using UnityEngine;
using UnityEngine.UI;

namespace TrafficTown2D.Level7
{
    public class Level7MiniMapController : MonoBehaviour
    {
        [Header("Map Rectangles")]
        [SerializeField] private RectTransform mapContainer;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private RectTransform destinationMarker;
        [SerializeField] private RectTransform safeStop1Marker;
        [SerializeField] private RectTransform safeStop2Marker;

        [Header("World Coordinates Bounds")]
        [SerializeField] private Vector2 worldMin = new Vector2(-42f, -28f);
        [SerializeField] private Vector2 worldMax = new Vector2(42f, 28f);

        private Level7PlayerCar playerCar;
        private Vector3 currentDestination;

        private void Start()
        {
            playerCar = FindAnyObjectByType<Level7PlayerCar>();

            // Setup static markers for safe stop bays
            SetMarkerWorldPos(safeStop1Marker, new Vector3(15f, -24.5f, 0f));
            SetMarkerWorldPos(safeStop2Marker, new Vector3(-39.5f, -10f, 0f));
        }

        public void SetDestination(Vector3 destWorldPos)
        {
            currentDestination = destWorldPos;
            SetMarkerWorldPos(destinationMarker, destWorldPos);
        }

        private void Update()
        {
            if (playerCar != null && playerMarker != null)
            {
                SetMarkerWorldPos(playerMarker, playerCar.transform.position);
                playerMarker.localRotation = Quaternion.Euler(0f, 0f, playerCar.transform.eulerAngles.z);
            }
        }

        private void SetMarkerWorldPos(RectTransform marker, Vector3 worldPos)
        {
            if (marker == null || mapContainer == null) return;

            float normX = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPos.x);
            float normY = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPos.y);

            Vector2 mapSize = mapContainer.rect.size;
            float mapX = (normX - 0.5f) * mapSize.x;
            float mapY = (normY - 0.5f) * mapSize.y;

            marker.anchoredPosition = new Vector2(mapX, mapY);
        }
    }
}
