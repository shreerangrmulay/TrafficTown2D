using UnityEngine;

namespace TrafficTown2D.Visuals
{
    [DisallowMultipleComponent]
    public sealed class Level2ModelDecorator : MonoBehaviour
    {
        private bool built;

        public void Build()
        {
            if (built) return;
            built = true;

            Sprite sprite = ResolveSprite();
            if (sprite == null) return;

            UpgradePlayerModel(sprite);
            CreateStreetModels(sprite);
        }

        private Sprite ResolveSprite()
        {
            SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
            if (playerRenderer != null && playerRenderer.sprite != null) return playerRenderer.sprite;

            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index].name == "Shirt" && renderers[index].sprite != null) return renderers[index].sprite;
            }

            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index].sprite != null) return renderers[index].sprite;
            }

            return null;
        }

        private void UpgradePlayerModel(Sprite sprite)
        {
            if (transform.Find("CharacterVisual") != null) return;

            SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
            if (playerRenderer != null) playerRenderer.enabled = false;

            GameObject visual = new GameObject("CharacterVisual");
            visual.transform.SetParent(transform, false);
            CreatePiece(visual.transform, "Shadow", sprite, new Vector2(0f, -0.53f), new Vector2(0.68f, 0.12f), new Color(0f, 0f, 0f, 0.22f), 20);
            CreatePiece(visual.transform, "LeftLeg", sprite, new Vector2(-0.14f, -0.33f), new Vector2(0.16f, 0.40f), new Color(0.14f, 0.19f, 0.38f), 22);
            CreatePiece(visual.transform, "RightLeg", sprite, new Vector2(0.14f, -0.33f), new Vector2(0.16f, 0.40f), new Color(0.14f, 0.19f, 0.38f), 22);
            CreatePiece(visual.transform, "LeftShoe", sprite, new Vector2(-0.17f, -0.57f), new Vector2(0.24f, 0.09f), new Color(0.05f, 0.06f, 0.08f), 23);
            CreatePiece(visual.transform, "RightShoe", sprite, new Vector2(0.17f, -0.57f), new Vector2(0.24f, 0.09f), new Color(0.05f, 0.06f, 0.08f), 23);
            CreatePiece(visual.transform, "Torso", sprite, Vector2.zero, new Vector2(0.52f, 0.56f), new Color(0.16f, 0.56f, 0.90f), 24);
            CreatePiece(visual.transform, "LeftArm", sprite, new Vector2(-0.35f, -0.01f), new Vector2(0.13f, 0.46f), new Color(0.98f, 0.78f, 0.58f), 23);
            CreatePiece(visual.transform, "RightArm", sprite, new Vector2(0.35f, -0.01f), new Vector2(0.13f, 0.46f), new Color(0.98f, 0.78f, 0.58f), 23);
            CreatePiece(visual.transform, "Head", sprite, new Vector2(0f, 0.37f), new Vector2(0.36f, 0.36f), new Color(0.98f, 0.78f, 0.58f), 25);
            CreatePiece(visual.transform, "Hair", sprite, new Vector2(0f, 0.50f), new Vector2(0.36f, 0.13f), new Color(0.14f, 0.08f, 0.04f), 26);
        }

        private static void CreateStreetModels(Sprite sprite)
        {
            GameObject environment = GameObject.Find("Environment");
            if (environment == null || environment.transform.Find("Level2StreetModels") != null) return;

            GameObject models = new GameObject("Level2StreetModels");
            models.transform.SetParent(environment.transform, false);

            CreateBusStop(models.transform, sprite, new Vector2(6.2f, 2.85f));
            CreatePlanter(models.transform, sprite, "PlanterLeft", new Vector2(-6.1f, 2.7f));
            CreatePlanter(models.transform, sprite, "PlanterRight", new Vector2(4.6f, 2.7f));
            CreateWaitingPedestrian(models.transform, sprite, "PedestrianTop", new Vector2(4.1f, 2.9f), new Color(0.93f, 0.48f, 0.28f));
            CreateWaitingPedestrian(models.transform, sprite, "PedestrianBottom", new Vector2(-5.25f, -2.9f), new Color(0.46f, 0.42f, 0.82f));
            CreateCrosswalkBeacon(models.transform, sprite, "BeaconLeft", new Vector2(-2.25f, -2.35f));
            CreateCrosswalkBeacon(models.transform, sprite, "BeaconRight", new Vector2(2.25f, -2.35f));
        }

        private static void CreateBusStop(Transform parent, Sprite sprite, Vector2 position)
        {
            GameObject stop = new GameObject("BusStop");
            stop.transform.SetParent(parent, false);
            stop.transform.localPosition = position;
            Color frame = new Color(0.18f, 0.29f, 0.38f);
            CreatePiece(stop.transform, "Roof", sprite, new Vector2(0f, 0.68f), new Vector2(1.8f, 0.14f), frame, 6);
            CreatePiece(stop.transform, "LeftPost", sprite, new Vector2(-0.76f, 0.05f), new Vector2(0.09f, 1.24f), frame, 6);
            CreatePiece(stop.transform, "RightPost", sprite, new Vector2(0.76f, 0.05f), new Vector2(0.09f, 1.24f), frame, 6);
            CreatePiece(stop.transform, "GlassPanel", sprite, new Vector2(0f, 0.11f), new Vector2(1.42f, 0.94f), new Color(0.60f, 0.83f, 0.92f, 0.45f), 5);
            CreatePiece(stop.transform, "Bench", sprite, new Vector2(0f, -0.39f), new Vector2(1.18f, 0.16f), new Color(0.60f, 0.37f, 0.20f), 7);
            CreatePiece(stop.transform, "RouteSign", sprite, new Vector2(-0.95f, 0.67f), new Vector2(0.24f, 0.24f), new Color(0.16f, 0.58f, 0.87f), 8);
        }

        private static void CreatePlanter(Transform parent, Sprite sprite, string name, Vector2 position)
        {
            GameObject planter = new GameObject(name);
            planter.transform.SetParent(parent, false);
            planter.transform.localPosition = position;
            CreatePiece(planter.transform, "Pot", sprite, new Vector2(0f, -0.22f), new Vector2(0.58f, 0.30f), new Color(0.63f, 0.34f, 0.19f), 6);
            CreatePiece(planter.transform, "LeavesLeft", sprite, new Vector2(-0.19f, 0.08f), new Vector2(0.48f, 0.48f), new Color(0.18f, 0.57f, 0.30f), 7);
            CreatePiece(planter.transform, "LeavesRight", sprite, new Vector2(0.19f, 0.08f), new Vector2(0.48f, 0.48f), new Color(0.24f, 0.66f, 0.34f), 7);
        }

        private static void CreateWaitingPedestrian(Transform parent, Sprite sprite, string name, Vector2 position, Color shirtColor)
        {
            GameObject pedestrian = new GameObject(name);
            pedestrian.transform.SetParent(parent, false);
            pedestrian.transform.localPosition = position;
            CreatePiece(pedestrian.transform, "Legs", sprite, new Vector2(0f, -0.26f), new Vector2(0.24f, 0.42f), new Color(0.12f, 0.16f, 0.26f), 8);
            CreatePiece(pedestrian.transform, "Torso", sprite, new Vector2(0f, 0f), new Vector2(0.34f, 0.40f), shirtColor, 9);
            CreatePiece(pedestrian.transform, "Head", sprite, new Vector2(0f, 0.31f), new Vector2(0.27f, 0.27f), new Color(0.93f, 0.70f, 0.51f), 10);
        }

        private static void CreateCrosswalkBeacon(Transform parent, Sprite sprite, string name, Vector2 position)
        {
            GameObject beacon = new GameObject(name);
            beacon.transform.SetParent(parent, false);
            beacon.transform.localPosition = position;
            CreatePiece(beacon.transform, "Post", sprite, new Vector2(0f, 0.24f), new Vector2(0.08f, 0.52f), new Color(0.22f, 0.25f, 0.29f), 8);
            CreatePiece(beacon.transform, "Light", sprite, new Vector2(0f, 0.54f), new Vector2(0.26f, 0.26f), new Color(1f, 0.67f, 0.12f), 9);
        }

        private static SpriteRenderer CreatePiece(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 scale, Color color, int sortingOrder)
        {
            GameObject piece = new GameObject(name);
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
