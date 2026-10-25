using System.IO;
using UnityEngine;

namespace TrafficTown2D.UI
{
    /// <summary>
    /// Provides UI sprites and textures for the TrafficTown 2D redesign.
    /// Loads from Assets/Art if PNG files exist, or dynamically generates crisp flat sprites
    /// and saves them to disk so they are available in both editor and runtime.
    /// </summary>
    public static class MenuAssetProvider
    {
        private static string ArtDir => Application.isEditor ? "Assets/Art" : Path.Combine(Application.dataPath, "Art");
        private static string IconsDir => Path.Combine(ArtDir, "Icons");

        private static Sprite cachedButton9Slice;
        private static Sprite cachedButtonOutline9Slice;
        private static Sprite cachedNodeCircle;
        private static Sprite cachedCarSprite;
        private static Sprite cachedBarrierSprite;
        private static Sprite cachedFlagSprite;
        private static Sprite cachedLockSprite;
        private static Sprite cachedStarFilled;
        private static Sprite cachedStarEmpty;
        private static Sprite cachedTrafficLight;

        public static Sprite GetButton9Slice()
        {
            if (cachedButton9Slice != null) return cachedButton9Slice;
            string path = Path.Combine(ArtDir, "button_rounded_9slice.png");
            cachedButton9Slice = LoadOrCreateSprite(path, () => GenerateRoundedRectTexture(128, 128, 32f), new Vector4(36, 36, 36, 36));
            return cachedButton9Slice;
        }

        public static Sprite GetButtonOutline9Slice()
        {
            if (cachedButtonOutline9Slice != null) return cachedButtonOutline9Slice;
            string path = Path.Combine(ArtDir, "button_outline_9slice.png");
            cachedButtonOutline9Slice = LoadOrCreateSprite(path, () => GenerateRoundedRectOutlineTexture(128, 128, 32f, 10f, ThemeColors.InkBlack), new Vector4(36, 36, 36, 36));
            return cachedButtonOutline9Slice;
        }

        public static Sprite GetNodeCircle()
        {
            if (cachedNodeCircle != null) return cachedNodeCircle;
            string path = Path.Combine(ArtDir, "node_circle.png");
            cachedNodeCircle = LoadOrCreateSprite(path, () => GenerateCircleTexture(128, 58f), Vector4.zero);
            return cachedNodeCircle;
        }

        public static Sprite GetCarSprite()
        {
            if (cachedCarSprite != null) return cachedCarSprite;
            string path = Path.Combine(ArtDir, "car.png");
            cachedCarSprite = LoadOrCreateSprite(path, GenerateTopDownCarTexture, Vector4.zero);
            return cachedCarSprite;
        }

        public static Sprite GetBarrierSprite()
        {
            if (cachedBarrierSprite != null) return cachedBarrierSprite;
            string path = Path.Combine(ArtDir, "barrier.png");
            cachedBarrierSprite = LoadOrCreateSprite(path, GenerateBarrierTexture, Vector4.zero);
            return cachedBarrierSprite;
        }

        public static Sprite GetFlagSprite()
        {
            if (cachedFlagSprite != null) return cachedFlagSprite;
            string path = Path.Combine(ArtDir, "flag.png");
            cachedFlagSprite = LoadOrCreateSprite(path, GenerateFlagTexture, Vector4.zero);
            return cachedFlagSprite;
        }

        public static Sprite GetLockSprite()
        {
            if (cachedLockSprite != null) return cachedLockSprite;
            string path = Path.Combine(ArtDir, "lock.png");
            cachedLockSprite = LoadOrCreateSprite(path, GenerateLockTexture, Vector4.zero);
            return cachedLockSprite;
        }

        public static Sprite GetStarFilled()
        {
            if (cachedStarFilled != null) return cachedStarFilled;
            string path = Path.Combine(ArtDir, "star_filled.png");
            cachedStarFilled = LoadOrCreateSprite(path, () => GenerateStarTexture(48, ThemeColors.StarFilled), Vector4.zero);
            return cachedStarFilled;
        }

        public static Sprite GetStarEmpty()
        {
            if (cachedStarEmpty != null) return cachedStarEmpty;
            string path = Path.Combine(ArtDir, "star_empty.png");
            cachedStarEmpty = LoadOrCreateSprite(path, () => GenerateStarTexture(48, ThemeColors.StarEmpty), Vector4.zero);
            return cachedStarEmpty;
        }

        public static Sprite GetTrafficLight()
        {
            if (cachedTrafficLight != null) return cachedTrafficLight;
            string path = Path.Combine(ArtDir, "traffic_light.png");
            cachedTrafficLight = LoadOrCreateSprite(path, GenerateTrafficLightTexture, Vector4.zero);
            return cachedTrafficLight;
        }

        public static Sprite GetTitleSceneBackground()
        {
            string path = Path.Combine(ArtDir, "title_scene.png");
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            return null; // Return null so plain yellow Image with ThemeColors.PrimaryYellow is used
        }

        public static Sprite GetRoadMapBackground()
        {
            string path = Path.Combine(ArtDir, "road_map_bg.png");
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            return null;
        }

        public static Sprite GetLevelIcon(int levelNum)
        {
            string path = Path.Combine(IconsDir, $"Icon_Level{levelNum}.png");
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            return null;
        }

        private static Sprite LoadOrCreateSprite(string filePath, System.Func<Texture2D> generator, Vector4 border)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    byte[] data = File.ReadAllBytes(filePath);
                    Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (diskTex.LoadImage(data))
                    {
                        diskTex.filterMode = FilterMode.Bilinear;
                        diskTex.wrapMode = TextureWrapMode.Clamp;
                        return Sprite.Create(diskTex, new Rect(0, 0, diskTex.width, diskTex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MenuAssetProvider] Could not load from disk '{filePath}': {ex.Message}");
            }

            // Generate procedural texture
            Texture2D genTex = generator();
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(filePath, genTex.EncodeToPNG());
            }
            catch { }

            return Sprite.Create(genTex, new Rect(0, 0, genTex.width, genTex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        private static Texture2D GenerateRoundedRectTexture(int w, int h, float r)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = DistanceToRoundedRect(x, y, 4f, 4f, w - 8f, h - 8f, r);
                    if (d < 0f) px[y * w + x] = Color.white;
                    else if (d <= 1.5f) px[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d / 1.5f));
                    else px[y * w + x] = Color.clear;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D GenerateRoundedRectOutlineTexture(int w, int h, float r, float stroke, Color32 strokeColor)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float outerD = DistanceToRoundedRect(x, y, 4f, 4f, w - 8f, h - 8f, r);
                    float innerD = DistanceToRoundedRect(x, y, 4f + stroke, 4f + stroke, w - 8f - stroke * 2f, h - 8f - stroke * 2f, Mathf.Max(2f, r - stroke));

                    if (outerD < 0f && innerD >= 0f)
                    {
                        px[y * w + x] = strokeColor;
                    }
                    else if (outerD <= 1.5f && outerD >= 0f)
                    {
                        float alpha = Mathf.Clamp01(1f - outerD / 1.5f);
                        px[y * w + x] = new Color(strokeColor.r / 255f, strokeColor.g / 255f, strokeColor.b / 255f, alpha);
                    }
                    else if (innerD < 0f && innerD >= -1.5f)
                    {
                        float alpha = Mathf.Clamp01(-innerD / 1.5f);
                        px[y * w + x] = new Color(strokeColor.r / 255f, strokeColor.g / 255f, strokeColor.b / 255f, 1f - alpha);
                    }
                    else
                    {
                        px[y * w + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D GenerateCircleTexture(int size, float radius)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[size * size];
            float center = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    if (dist < radius - 1f) px[y * size + x] = Color.white;
                    else if (dist <= radius + 1f) px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - (dist - (radius - 1f)) / 2f));
                    else px[y * size + x] = Color.clear;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D GenerateTopDownCarTexture()
        {
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
                    bool isWheel = (x < 10 || x > 54) && ((y > 14 && y < 34) || (y > 62 && y < 82));
                    if (isWheel)
                    {
                        px[y * w + x] = tireColor;
                        continue;
                    }

                    float dBody = DistanceToRoundedRect(x, y, 8f, 6f, 48f, 84f, 16f);
                    if (dBody < 0f)
                    {
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
            return tex;
        }

        private static Texture2D GenerateBarrierTexture()
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
                    if (y < 8 && ((x > 10 && x < 22) || (x > 58 && x < 70)))
                    {
                        px[y * w + x] = black;
                        continue;
                    }

                    if (y >= 8 && y < 28 && x >= 4 && x < 76)
                    {
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
            return tex;
        }

        private static Texture2D GenerateFlagTexture()
        {
            const int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[w * h];

            Color32 black = ThemeColors.InkBlack;
            Color32 white = Color.white;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x >= 4 && x <= 9 && y >= 2 && y <= 62)
                    {
                        px[y * w + x] = black;
                        continue;
                    }

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
            return tex;
        }

        private static Texture2D GenerateLockTexture()
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[size * size];
            Color32 lockColor = ThemeColors.InkBlack;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dOuter = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 38f));
                    float dInner = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 38f));
                    bool inShackle = (dOuter <= 16f && dInner >= 9f && y >= 36);
                    float dBody = DistanceToRoundedRect(x, y, 14f, 10f, 36f, 30f, 6f);

                    if (inShackle || dBody < 0f) px[y * size + x] = lockColor;
                    else px[y * size + x] = Color.clear;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D GenerateStarTexture(int size, Color32 color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (IsInsideStar(x, y, size)) px[y * size + x] = color;
                    else px[y * size + x] = Color.clear;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D GenerateTrafficLightTexture()
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
            return tex;
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
    }
}
