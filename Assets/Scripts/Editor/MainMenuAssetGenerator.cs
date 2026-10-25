#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using TrafficTown2D.UI;

namespace TrafficTown2D.Editor
{
    /// <summary>
    /// Generates flat, high-contrast, children's theme art assets and UI sprites for TrafficTown 2D.
    /// Places assets into Assets/Art and Assets/Art/Icons with correct 9-slice borders and sprite import settings.
    /// </summary>
    public static class MainMenuAssetGenerator
    {
        public const string ArtDir = "Assets/Art";
        public const string ArtIconsDir = "Assets/Art/Icons";

        // Asset Paths
        public const string TitleScenePath = "Assets/Art/title_scene.png";
        public const string RoadMapBgPath = "Assets/Art/road_map_bg.png";
        public const string Button9SlicePath = "Assets/Art/button_rounded_9slice.png";
        public const string ButtonOutline9SlicePath = "Assets/Art/button_outline_9slice.png";
        public const string NodeCirclePath = "Assets/Art/node_circle.png";
        public const string CarSpritePath = "Assets/Art/car.png";
        public const string BarrierSpritePath = "Assets/Art/barrier.png";
        public const string FlagSpritePath = "Assets/Art/flag.png";
        public const string LockSpritePath = "Assets/Art/lock.png";
        public const string StarFilledPath = "Assets/Art/star_filled.png";
        public const string StarEmptyPath = "Assets/Art/star_empty.png";
        public const string TrafficLightPath = "Assets/Art/traffic_light.png";

        [MenuItem("TrafficTown/Generate Redesign Art Assets")]
        public static void GenerateAllSprites()
        {
            if (!Directory.Exists(ArtDir)) Directory.CreateDirectory(ArtDir);
            if (!Directory.Exists(ArtIconsDir)) Directory.CreateDirectory(ArtIconsDir);

            GenerateButton9Slice();
            GenerateButtonOutline9Slice();
            GenerateNodeCircle();
            GenerateCarSprite();
            GenerateBarrierSprite();
            GenerateFlagSprite();
            GenerateLockSprite();
            GenerateStarSprites();
            GenerateTrafficLightSprite();
            GenerateFallbackBackgrounds();
            GenerateLevelIcons();

            AssetDatabase.Refresh();
            Debug.Log("[MainMenuAssetGenerator] All redesign art assets successfully generated in Assets/Art/!");
        }

        private static void GenerateButton9Slice()
        {
            const int w = 128, h = 128;
            const float r = 32f;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = DistanceToRoundedRect(x, y, 4f, 4f, w - 8f, h - 8f, r);
                    if (d < 0f)
                    {
                        px[y * w + x] = Color.white;
                    }
                    else if (d <= 1.5f)
                    {
                        float alpha = Mathf.Clamp01(1f - d / 1.5f);
                        px[y * w + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        px[y * w + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            SaveTextureAndConfigureSprite(tex, Button9SlicePath, new Vector4(36, 36, 36, 36));
        }

        private static void GenerateButtonOutline9Slice()
        {
            const int w = 128, h = 128;
            const float r = 32f;
            const float stroke = 10f;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[w * h];
            Color32 inkBlack = ThemeColors.InkBlack;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float outerD = DistanceToRoundedRect(x, y, 4f, 4f, w - 8f, h - 8f, r);
                    float innerD = DistanceToRoundedRect(x, y, 4f + stroke, 4f + stroke, w - 8f - stroke * 2f, h - 8f - stroke * 2f, Mathf.Max(2f, r - stroke));

                    if (outerD < 0f && innerD >= 0f)
                    {
                        px[y * w + x] = inkBlack;
                    }
                    else if (outerD <= 1.5f && outerD >= 0f)
                    {
                        float alpha = Mathf.Clamp01(1f - outerD / 1.5f);
                        px[y * w + x] = new Color(inkBlack.r / 255f, inkBlack.g / 255f, inkBlack.b / 255f, alpha);
                    }
                    else if (innerD < 0f && innerD >= -1.5f)
                    {
                        float alpha = Mathf.Clamp01(-innerD / 1.5f);
                        px[y * w + x] = new Color(inkBlack.r / 255f, inkBlack.g / 255f, inkBlack.b / 255f, 1f - alpha);
                    }
                    else
                    {
                        px[y * w + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            SaveTextureAndConfigureSprite(tex, ButtonOutline9SlicePath, new Vector4(36, 36, 36, 36));
        }

        private static void GenerateNodeCircle()
        {
            const int size = 128;
            const float radius = 58f;
            const float center = 64f;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    if (dist < radius - 1f)
                    {
                        px[y * size + x] = Color.white;
                    }
                    else if (dist <= radius + 1f)
                    {
                        float alpha = Mathf.Clamp01(1f - (dist - (radius - 1f)) / 2f);
                        px[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        px[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            SaveTextureAndConfigureSprite(tex, NodeCirclePath, Vector4.zero);
        }

        private static void GenerateCarSprite()
        {
            // If existing CarRedTopDown sprite exists, copy it as primary; else generate cute top-down car
            string existingCarPath = "Assets/Sprites/Vehicles/CarRedTopDown.png";
            if (File.Exists(existingCarPath))
            {
                File.Copy(existingCarPath, CarSpritePath, true);
                ConfigureSprite(CarSpritePath, Vector4.zero);
                return;
            }

            const int w = 64, h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[w * h];

            Color32 bodyColor = new Color32(0xEE, 0x33, 0x33, 0xFF);
            Color32 roofColor = new Color32(0xCC, 0x22, 0x22, 0xFF);
            Color32 glassColor = new Color32(0x88, 0xDD, 0xFF, 0xFF);
            Color32 tireColor = ThemeColors.InkBlack;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Wheels
                    bool isWheel = (x < 10 || x > 54) && ((y > 14 && y < 34) || (y > 62 && y < 82));
                    if (isWheel)
                    {
                        px[y * w + x] = tireColor;
                        continue;
                    }

                    // Car Body
                    float dBody = DistanceToRoundedRect(x, y, 8f, 6f, 48f, 84f, 16f);
                    if (dBody < 0f)
                    {
                        // Windshield / Cabin
                        float dCabin = DistanceToRoundedRect(x, y, 14f, 26f, 36f, 44f, 8f);
                        if (dCabin < 0f)
                        {
                            float dRoof = DistanceToRoundedRect(x, y, 17f, 34f, 30f, 26f, 6f);
                            px[y * w + x] = (dRoof < 0f) ? roofColor : glassColor;
                        }
                        else
                        {
                            px[y * w + x] = bodyColor;
                        }
                    }
                    else
                    {
                        px[y * w + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            SaveTextureAndConfigureSprite(tex, CarSpritePath, Vector4.zero);
        }

        private static void GenerateBarrierSprite()
        {
            const int w = 80, h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[w * h];

            Color32 red = new Color32(0xEE, 0x33, 0x33, 0xFF);
            Color32 white = Color.white;
            Color32 black = ThemeColors.InkBlack;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Feet
                    if (y < 8 && ((x > 10 && x < 22) || (x > 58 && x < 70)))
                    {
                        px[y * w + x] = black;
                        continue;
                    }

                    // Main Bar (y 8 to 28)
                    if (y >= 8 && y < 28 && x >= 4 && x < 76)
                    {
                        // Diagonal stripes
                        int stripe = ((x + y) / 10) % 2;
                        px[y * w + x] = (stripe == 0) ? red : white;
                    }
                    else
                    {
                        px[y * w + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            SaveTextureAndConfigureSprite(tex, BarrierSpritePath, Vector4.zero);
        }

        private static void GenerateFlagSprite()
        {
            string existingFinishPath = "Assets/Sprites/Environment/CheckeredFinish.png";
            if (File.Exists(existingFinishPath))
            {
                File.Copy(existingFinishPath, FlagSpritePath, true);
                ConfigureSprite(FlagSpritePath, Vector4.zero);
                return;
            }

            const int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[w * h];

            Color32 black = ThemeColors.InkBlack;
            Color32 white = Color.white;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Flag pole on left
                    if (x >= 4 && x <= 9 && y >= 2 && y <= 62)
                    {
                        px[y * w + x] = black;
                        continue;
                    }

                    // Checkered banner
                    if (x > 9 && x < 58 && y >= 24 && y <= 60)
                    {
                        int check = ((x / 8) + (y / 8)) % 2;
                        px[y * w + x] = (check == 0) ? black : white;
                    }
                    else
                    {
                        px[y * w + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            SaveTextureAndConfigureSprite(tex, FlagSpritePath, Vector4.zero);
        }

        private static void GenerateLockSprite()
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[size * size];

            Color32 lockColor = ThemeColors.InkBlack;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Shackle loop (top arch)
                    float dOuter = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 38f));
                    float dInner = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 38f));
                    bool inShackle = (dOuter <= 16f && dInner >= 9f && y >= 36);

                    // Body
                    float dBody = DistanceToRoundedRect(x, y, 14f, 10f, 36f, 30f, 6f);

                    if (inShackle || dBody < 0f)
                    {
                        px[y * size + x] = lockColor;
                    }
                    else
                    {
                        px[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            SaveTextureAndConfigureSprite(tex, LockSpritePath, Vector4.zero);
        }

        private static void GenerateStarSprites()
        {
            const int size = 48;
            // 1. Star Filled
            Texture2D texFilled = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pxFilled = new Color32[size * size];
            Color32 gold = ThemeColors.StarFilled;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (IsInsideStar(x, y, size))
                    {
                        pxFilled[y * size + x] = gold;
                    }
                    else
                    {
                        pxFilled[y * size + x] = Color.clear;
                    }
                }
            }
            texFilled.SetPixels32(pxFilled);
            texFilled.Apply();
            SaveTextureAndConfigureSprite(texFilled, StarFilledPath, Vector4.zero);

            // 2. Star Empty / Outline
            Texture2D texEmpty = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pxEmpty = new Color32[size * size];
            Color32 emptyColor = ThemeColors.StarEmpty;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (IsInsideStar(x, y, size))
                    {
                        pxEmpty[y * size + x] = emptyColor;
                    }
                    else
                    {
                        pxEmpty[y * size + x] = Color.clear;
                    }
                }
            }
            texEmpty.SetPixels32(pxEmpty);
            texEmpty.Apply();
            SaveTextureAndConfigureSprite(texEmpty, StarEmptyPath, Vector4.zero);
        }

        private static void GenerateTrafficLightSprite()
        {
            const int w = 64, h = 144;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[w * h];

            Color32 bodyColor = ThemeColors.InkBlack;
            Color32 red = ThemeColors.TrafficRed;
            Color32 amber = ThemeColors.TrafficAmber;
            Color32 green = ThemeColors.TrafficGreen;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dBody = DistanceToRoundedRect(x, y, 6f, 6f, w - 12f, h - 12f, 18f);
                    if (dBody < 0f)
                    {
                        // Check lights
                        float dRed = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 108f));
                        float dAmber = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 72f));
                        float dGreen = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 36f));

                        if (dRed < 14f) px[y * w + x] = red;
                        else if (dAmber < 14f) px[y * w + x] = amber;
                        else if (dGreen < 14f) px[y * w + x] = green;
                        else px[y * w + x] = bodyColor;
                    }
                    else
                    {
                        px[y * w + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            SaveTextureAndConfigureSprite(tex, TrafficLightPath, Vector4.zero);
        }

        private static void GenerateFallbackBackgrounds()
        {
            // TODO: Replace with custom illustration Assets/Art/title_scene.png if available
            if (!File.Exists(TitleScenePath))
            {
                Texture2D titleBg = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                Color32 yellow = ThemeColors.PrimaryYellow;
                Color32[] px = new Color32[64 * 64];
                for (int i = 0; i < px.Length; i++) px[i] = yellow;
                titleBg.SetPixels32(px);
                titleBg.Apply();
                SaveTextureAndConfigureSprite(titleBg, TitleScenePath, Vector4.zero);
            }

            // TODO: Replace with custom road map illustration Assets/Art/road_map_bg.png if available
            if (!File.Exists(RoadMapBgPath))
            {
                Texture2D mapBg = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                Color32 yellow = ThemeColors.PrimaryYellow;
                Color32[] px = new Color32[64 * 64];
                for (int i = 0; i < px.Length; i++) px[i] = yellow;
                mapBg.SetPixels32(px);
                mapBg.Apply();
                SaveTextureAndConfigureSprite(mapBg, RoadMapBgPath, Vector4.zero);
            }
        }

        private static void GenerateLevelIcons()
        {
            // Generates simple crisp flat icons for all 10 levels
            for (int i = 1; i <= 10; i++)
            {
                string iconPath = $"{ArtIconsDir}/Icon_Level{i}.png";
                if (File.Exists(iconPath)) continue;

                Texture2D ico = new Texture2D(48, 48, TextureFormat.RGBA32, false);
                Color32[] px = new Color32[48 * 48];
                Color32 white = Color.white;

                for (int y = 0; y < 48; y++)
                {
                    for (int x = 0; x < 48; x++)
                    {
                        // Clean inner symbol placeholder
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(24f, 24f));
                        px[y * 48 + x] = (d <= 16f) ? white : Color.clear;
                    }
                }

                ico.SetPixels32(px);
                ico.Apply();
                SaveTextureAndConfigureSprite(ico, iconPath, Vector4.zero);
            }
        }

        private static bool IsInsideStar(int px, int py, int size)
        {
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float x = px - cx;
            float y = py - cy;
            float r = Mathf.Sqrt(x * x + y * y);
            float angle = Mathf.Atan2(y, x) + Mathf.PI / 2f;
            if (angle < 0) angle += Mathf.PI * 2f;

            // 5-point star equation
            float p = (angle % (Mathf.PI * 2f / 5f)) / (Mathf.PI * 2f / 5f);
            float rMax = size * 0.44f;
            float rMin = size * 0.19f;
            float targetR = Mathf.Lerp(rMax, rMin, Mathf.Abs(p - 0.5f) * 2f);

            return r <= targetR;
        }

        private static float DistanceToRoundedRect(float x, float y, float rx, float ry, float rw, float rh, float radius)
        {
            float cx = Mathf.Clamp(x, rx + radius, rx + rw - radius);
            float cy = Mathf.Clamp(y, ry + radius, ry + rh - radius);
            return Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) - radius;
        }

        private static void SaveTextureAndConfigureSprite(Texture2D tex, string path, Vector4 border)
        {
            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
            ConfigureSprite(path, border);
        }

        public static void ConfigureSprite(string path, Vector4 border)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100;
                ti.filterMode = FilterMode.Bilinear;
                ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.alphaIsTransparency = true;
                if (border != Vector4.zero)
                {
                    ti.spriteBorder = border;
                }
                ti.SaveAndReimport();
            }
        }
    }
}
#endif
