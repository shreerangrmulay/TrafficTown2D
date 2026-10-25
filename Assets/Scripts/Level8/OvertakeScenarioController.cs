using System;
using System.Collections.Generic;
using UnityEngine;
using TrafficTown2D.Level3;

namespace TrafficTown2D.Level8
{
    /// <summary>
    /// Spawns, controls, and manages lead vehicles and oncoming traffic
    /// for each of the 5 overtaking missions in Level 8.
    /// </summary>
    public class OvertakeScenarioController : MonoBehaviour
    {
        [Header("Vehicle Prefabs / Templates")]
        [SerializeField] private GameObject vehicleTemplate;

        [Header("Active Scenario State")]
        [SerializeField] private int activeMissionNumber = 1;
        [SerializeField] private Transform currentLeadVehicle;
        [SerializeField] private Transform currentOncomingVehicle;

        private readonly List<GameObject> activeScenarioVehicles = new List<GameObject>();

        public Transform CurrentLeadVehicle => currentLeadVehicle;
        public Transform CurrentOncomingVehicle => currentOncomingVehicle;

        public void ConfigureForMission(int missionNumber)
        {
            activeMissionNumber = missionNumber;
            CleanupActiveVehicles();

            switch (missionNumber)
            {
                case 1:
                    SetupMission1();
                    break;
                case 2:
                    SetupMission2();
                    break;
                case 3:
                    SetupMission3();
                    break;
                case 4:
                    SetupMission4();
                    break;
                case 5:
                    SetupMission5();
                    break;
            }
        }

        public void ResetCurrentMission()
        {
            ConfigureForMission(activeMissionNumber);
        }

        private void CleanupActiveVehicles()
        {
            for (int i = 0; i < activeScenarioVehicles.Count; i++)
            {
                if (activeScenarioVehicles[i] != null)
                {
                    Destroy(activeScenarioVehicles[i]);
                }
            }
            activeScenarioVehicles.Clear();
            currentLeadVehicle = null;
            currentOncomingVehicle = null;
        }

        #region Mission Setups
        // Mission 1: KNOW YOUR ROAD (Open permitted straight, follow distance check)
        private void SetupMission1()
        {
            // Slow delivery truck ahead in right lane
            GameObject lead = CreateVehicle("Lead_DeliveryTruck_M1", new Vector3(1.5f, 16f, 0f), Vector2.up, 2.6f, "Vehicles/BusTopDown", new Color(0.25f, 0.65f, 0.90f, 1f));
            currentLeadVehicle = lead.transform;
            activeScenarioVehicles.Add(lead);

            // Far oncoming car (very far, clear road)
            GameObject oncoming = CreateVehicle("Oncoming_Sedan_M1", new Vector3(-1.5f, 75f, 0f), Vector2.down, 4.8f, "Vehicles/CarRedTopDown", Color.white);
            currentOncomingVehicle = oncoming.transform;
            activeScenarioVehicles.Add(oncoming);
        }

        // Mission 2: FIND THE SAFE GAP (Tight oncoming car, followed by safe open gap)
        private void SetupMission2()
        {
            // Slower car ahead in right lane
            GameObject lead = CreateVehicle("Lead_Sedan_M2", new Vector3(1.5f, 64f, 0f), Vector2.up, 2.7f, "Vehicles/TaxiTopDown", Color.white);
            currentLeadVehicle = lead.transform;
            activeScenarioVehicles.Add(lead);

            // Oncoming car creating an initial UNSAFE gap (~26m ahead)
            GameObject oncoming1 = CreateVehicle("Oncoming_TightGap_M2", new Vector3(-1.5f, 90f, 0f), Vector2.down, 5.5f, "Vehicles/CarRedTopDown", Color.white);
            currentOncomingVehicle = oncoming1.transform;
            activeScenarioVehicles.Add(oncoming1);

            // Second oncoming car creating a SAFE gap (~85m behind the first)
            GameObject oncoming2 = CreateVehicle("Oncoming_Delayed_M2", new Vector3(-1.5f, 160f, 0f), Vector2.down, 5.0f, "Vehicles/CarBlueTopDown", Color.white);
            activeScenarioVehicles.Add(oncoming2);
        }

        // Mission 3: KNOW WHEN NOT TO OVERTAKE (Blind bend, solid line, crossing)
        private void SetupMission3()
        {
            // Heavy slow truck in right lane navigating the blind bend
            GameObject lead = CreateVehicle("Lead_HeavyTruck_M3", new Vector3(1.5f, 124f, 0f), Vector2.up, 2.3f, "Vehicles/BusTopDown", new Color(0.85f, 0.45f, 0.15f, 1f));
            currentLeadVehicle = lead.transform;
            activeScenarioVehicles.Add(lead);

            // Oncoming car emerging around the bend at Y = 155
            GameObject oncoming = CreateVehicle("Oncoming_BlindBend_M3", new Vector3(-1.5f, 155f, 0f), Vector2.down, 4.5f, "Vehicles/CarRedTopDown", Color.white);
            currentOncomingVehicle = oncoming.transform;
            activeScenarioVehicles.Add(oncoming);
        }

        // Mission 4: COMPLETE THE OVERTAKE (Step-by-step indicator checklist)
        private void SetupMission4()
        {
            // Slow delivery van on straight section
            GameObject lead = CreateVehicle("Lead_Van_M4", new Vector3(1.5f, 184f, 0f), Vector2.up, 2.6f, "Vehicles/CarBlueTopDown", new Color(0.20f, 0.70f, 0.40f, 1f));
            currentLeadVehicle = lead.transform;
            activeScenarioVehicles.Add(lead);

            // Far oncoming car allowing plenty of time for the full 7-step sequence
            GameObject oncoming = CreateVehicle("Oncoming_Far_M4", new Vector3(-1.5f, 255f, 0f), Vector2.down, 4.0f, "Vehicles/TaxiTopDown", Color.white);
            currentOncomingVehicle = oncoming.transform;
            activeScenarioVehicles.Add(oncoming);
        }

        // Mission 5: THE OVERTAKING TEST (Comprehensive route)
        private void SetupMission5()
        {
            // Lead 1: Farm Tractor / Slow Van
            GameObject lead1 = CreateVehicle("Lead_Tractor_M5", new Vector3(1.5f, 242f, 0f), Vector2.up, 2.2f, "Vehicles/BusTopDown", new Color(0.35f, 0.75f, 0.30f, 1f));
            currentLeadVehicle = lead1.transform;
            activeScenarioVehicles.Add(lead1);

            // Oncoming 1: Close oncoming vehicle (tight gap)
            GameObject oncoming1 = CreateVehicle("Oncoming_Tight_M5", new Vector3(-1.5f, 268f, 0f), Vector2.down, 5.2f, "Vehicles/CarRedTopDown", Color.white);
            currentOncomingVehicle = oncoming1.transform;
            activeScenarioVehicles.Add(oncoming1);

            // Lead 2: Later slow car ahead of crossing
            GameObject lead2 = CreateVehicle("Lead_Car2_M5", new Vector3(1.5f, 282f, 0f), Vector2.up, 2.8f, "Vehicles/CarBlueTopDown", Color.white);
            activeScenarioVehicles.Add(lead2);

            // Oncoming 2: Far vehicle
            GameObject oncoming2 = CreateVehicle("Oncoming_Far_M5", new Vector3(-1.5f, 340f, 0f), Vector2.down, 5.0f, "Vehicles/TaxiTopDown", Color.white);
            activeScenarioVehicles.Add(oncoming2);
        }
        #endregion

        #region Vehicle Instantiation Helper
        private GameObject CreateVehicle(string name, Vector3 pos, Vector2 direction, float speed, string spritePath, Color tint)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = pos;

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = LoadVehicleSprite(spritePath);
            sr.color = tint;
            sr.sortingOrder = 10;
            obj.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            // Align rotation to movement direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            obj.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Dynamic;

            BoxCollider2D box = obj.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.6f, 3.2f);

            Level3TopDownVehicle vehicle = obj.AddComponent<Level3TopDownVehicle>();
            // Set fields via reflection if needed or public properties
            SetVehicleProperty(vehicle, "speed", speed);
            SetVehicleProperty(vehicle, "moveDirection", direction);

            return obj;
        }

        private Sprite LoadVehicleSprite(string path)
        {
            Sprite s = Resources.Load<Sprite>(path);
            if (s != null) return s;

            #if UNITY_EDITOR
            s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/" + path + ".png");
            if (s != null) return s;
            s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/" + path + ".png");
            if (s != null) return s;
            #endif

            return Resources.Load<Sprite>("Vehicles/CarRedTopDown");
        }

        private static void SetVehicleProperty(Level3TopDownVehicle v, string fieldName, object val)
        {
            var field = typeof(Level3TopDownVehicle).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null) field.SetValue(v, val);
        }
        #endregion
    }
}
