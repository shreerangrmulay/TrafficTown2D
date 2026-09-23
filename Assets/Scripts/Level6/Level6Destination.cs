using System;
using UnityEngine;

namespace TrafficTown2D.Level6
{
    public class Level6Destination : MonoBehaviour
    {
        [Header("Destination Info")]
        [SerializeField] private string destinationName = "City Hospital";
        [SerializeField] private string iconSymbol = "[+]";
        [SerializeField] private SpriteRenderer markerHalo;

        private bool isReached = false;

        public string DestinationName => destinationName;
        public string IconSymbol => iconSymbol;
        public bool IsReached => isReached;

        public event Action<Level6Destination> DestinationReached;
        public event Action PlayerArrived;

        public static Level6Destination Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (markerHalo != null && !isReached)
            {
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 4f);
                markerHalo.transform.localScale = new Vector3(pulse * 2.4f, pulse * 2.4f, 1f);
            }
        }

        public void BindVisual(Transform visual)
        {
            if (visual != null)
            {
                markerHalo = visual.GetComponent<SpriteRenderer>();
            }
        }

        public void SetDestination(Vector2 position, string label)
        {
            Configure(label, "[GOAL]", position);
        }

        public void Configure(string name, string symbol, Vector3 position)
        {
            destinationName = name;
            iconSymbol = symbol;
            transform.position = position;
            isReached = false;
            gameObject.SetActive(true);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isReached) return;

            Level6PlayerCar car = collision.GetComponent<Level6PlayerCar>();
            if (car != null)
            {
                isReached = true;
                if (markerHalo != null)
                {
                    markerHalo.color = new Color(0.2f, 0.9f, 0.4f, 0.8f);
                }
                DestinationReached?.Invoke(this);
                PlayerArrived?.Invoke();
            }
        }

        public void ResetDestination()
        {
            isReached = false;
            if (markerHalo != null)
            {
                markerHalo.color = new Color(0.2f, 0.7f, 1f, 0.8f);
            }
        }
    }
}
