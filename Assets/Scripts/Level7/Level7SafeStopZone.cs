using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficTown2D.Level7
{
    [RequireComponent(typeof(Collider2D))]
    public class Level7SafeStopZone : MonoBehaviour
    {
        public static readonly List<Level7SafeStopZone> AllZones = new List<Level7SafeStopZone>();

        [Header("Zone Identity")]
        [SerializeField] private string zoneName = "Safe Stop Bay";

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer bayRenderer;
        [SerializeField] private Color normalColor = new Color(0.20f, 0.65f, 0.32f, 0.45f);
        [SerializeField] private Color activeStopColor = new Color(0.15f, 0.85f, 0.40f, 0.80f);

        private bool playerInside = false;
        private bool isCurrentlySafeStop = false;
        private Level7PlayerCar playerCar;

        public string ZoneName => zoneName;
        public bool PlayerInside => playerInside;
        public bool IsCurrentlySafeStop => isCurrentlySafeStop;

        public static event Action<bool, string> AnySafeStopChanged;

        private void OnEnable()
        {
            if (!AllZones.Contains(this)) AllZones.Add(this);
        }

        private void OnDisable()
        {
            AllZones.Remove(this);
            if (isCurrentlySafeStop)
            {
                isCurrentlySafeStop = false;
                AnySafeStopChanged?.Invoke(false, zoneName);
            }
        }

        public static bool IsPlayerInAnySafeStop()
        {
            for (int i = 0; i < AllZones.Count; i++)
            {
                if (AllZones[i] != null && AllZones[i].IsCurrentlySafeStop)
                    return true;
            }
            return false;
        }

        public static Level7SafeStopZone GetActiveSafeStopZone()
        {
            for (int i = 0; i < AllZones.Count; i++)
            {
                if (AllZones[i] != null && AllZones[i].IsCurrentlySafeStop)
                    return AllZones[i];
            }
            return null;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Level7PlayerCar car = collision.GetComponent<Level7PlayerCar>();
            if (car == null) car = collision.GetComponentInParent<Level7PlayerCar>();

            if (car != null)
            {
                playerInside = true;
                playerCar = car;
                UpdateSafeStatus();
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (playerInside && playerCar != null)
            {
                UpdateSafeStatus();
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            Level7PlayerCar car = collision.GetComponent<Level7PlayerCar>();
            if (car == null) car = collision.GetComponentInParent<Level7PlayerCar>();

            if (car != null)
            {
                playerInside = false;
                playerCar = null;
                SetSafeStopState(false);
            }
        }

        private void UpdateSafeStatus()
        {
            if (!playerInside || playerCar == null)
            {
                SetSafeStopState(false);
                return;
            }

            bool stopped = playerCar.IsStopped;
            SetSafeStopState(stopped);
        }

        private void SetSafeStopState(bool safe)
        {
            if (isCurrentlySafeStop != safe)
            {
                isCurrentlySafeStop = safe;
                if (bayRenderer != null)
                {
                    bayRenderer.color = isCurrentlySafeStop ? activeStopColor : normalColor;
                }
                AnySafeStopChanged?.Invoke(isCurrentlySafeStop, zoneName);
            }
        }
    }
}
