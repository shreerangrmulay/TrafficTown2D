#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrafficTown2D.Editor
{
    public static class MainMenuAssetGenerator
    {
        private const string RootDir = "Assets/UI/MainMenu";
        private const string IconsDir = "Assets/UI/MainMenu/Icons";

        [MenuItem("TrafficTown/Generate Main Menu UI Sprites")]
        public static void GenerateAllSprites()
        {
            if (!Directory.Exists(RootDir)) Directory.CreateDirectory(RootDir);
            if (!Directory.Exists(IconsDir)) Directory.CreateDirectory(IconsDir);

            GenerateCardBase();
            GenerateButtonBase();
            GeneratePillBadge();

            GeneratePlayIcon();
            GenerateBookIcon();
            GenerateQuizIcon();
            GenerateExitIcon();
            GenerateBackIcon();
            GenerateStarIcon();
            GenerateLockIcon();

            ConfigureThumbnailTextures();

            AssetDatabase.Refresh();
            Debug.Log("[MainMenuAssetGenerator] All Main Menu UI sprites & textures successfully generated and configured!");
        }

        private static void ConfigureThumbnailTextures()
        {
            string thumbDir = "Assets/UI/MainMenu/Thumbnails";
            if (!Directory.Exists(thumbDir)) return;

            string[] files = Directory.GetFiles(thumbDir, "*.png");
            foreach (string file in files)
            {
                string assetPath = file.Replace('\\', '/');
                TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (ti != null)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.spritePixelsPerUnit = 100;
                    ti.mipmapEnabled = false;
                    ti.filterMode = FilterMode.Bilinear;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.SaveAndReimport();
                }
            }

            // Configure Background as well
            string bgPath = $"{RootDir}/MainMenuBackground.png";
            if (File.Exists(bgPath))
            {
                TextureImporter bgTi = AssetImporter.GetAtPath(bgPath) as TextureImporter;
                if (bgTi != null)
                {
                    bgTi.textureType = TextureImporterType.Sprite;
                    bgTi.spriteImportMode = SpriteImportMode.Single;
                    bgTi.spritePixelsPerUnit = 100;
                    bgTi.mipmapEnabled = false;
                    bgTi.filterMode = FilterMode.Bilinear;
                    bgTi.wrapMode = TextureWrapMode.Clamp;
                    bgTi.SaveAndReimport();
                }
            }
        }

        private static void GenerateCardBase()
        {
            const int w = 128, h = 128;
            const float r = 24f;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = DistanceToRoundedRect(x, y, 4f, 4f, w - 8f, h - 8f, r);
                    if (d < 0f)
                    {
                        // Subtle vertical card gradient
                        float v = Mathf.Lerp(0.96f, 1.0f, y / (float)h);
                        px[y * w + x] = new Color(v, v, v, 1f);
                    }
                    else if (d <= 1.5f)
                    {
                        // Antialiased outer boundary
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
            string path = $"{RootDir}/Card_Rounded_Base.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            SetSpriteBorders(path, new Vector4(30, 30, 30, 30));
        }

        private static void GenerateButtonBase()
        {
            const int w = 128, h = 64;
            const float r = 20f;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = DistanceToRoundedRect(x, y, 2f, 2f, w - 4f, h - 4f, r);
                    if (d < 0f)
                    {
                        // Inner fill with top highlight and bottom shadow
                        float grad = Mathf.Lerp(0.85f, 1.05f, y / (float)h);
                        px[y * w + x] = new Color(grad, grad, grad, 1f);
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
            string path = $"{RootDir}/Button_Rounded_3D.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            SetSpriteBorders(path, new Vector4(24, 24, 24, 24));
        }

        private static void GeneratePillBadge()
        {
            const int w = 64, h = 32;
            const float r = 15f;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = DistanceToRoundedRect(x, y, 1f, 1f, w - 2f, h - 2f, r);
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
            string path = $"{RootDir}/Pill_Badge.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            SetSpriteBorders(path, new Vector4(16, 16, 16, 16));
        }

        private static void GeneratePlayIcon()
        {
            const int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[sz * sz];

            // Equilateral triangle pointing right: (18, 12), (18, 52), (50, 32)
            Vector2 p1 = new Vector2(20f, 14f);
            Vector2 p2 = new Vector2(20f, 50f);
            Vector2 p3 = new Vector2(50f, 32f);

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    if (PointInTriangle(pt, p1, p2, p3))
                    {
                        px[y * sz + x] = Color.white;
                    }
                    else
                    {
                        px[y * sz + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            SaveIcon(tex, $"{IconsDir}/Icon_Play.png");
        }

        private static void GenerateBookIcon()
        {
            const int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[sz * sz];

            // Open book shape: Left page and right page with spine
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    bool isSpine = (x >= 30 && x <= 33 && y >= 14 && y <= 50);
                    bool isLeftPage = (x >= 14 && x <= 29 && y >= 16 && y <= 48 && !(x <= 16 && y >= 46));
                    bool isRightPage = (x >= 34 && x <= 49 && y >= 16 && y <= 48 && !(x >= 47 && y >= 46));
                    if (isLeftPage || isRightPage || isSpine)
                    {
                        // Add page horizontal line cutouts
                        if ((y == 24 || y == 32 || y == 40) && ((x >= 18 && x <= 26) || (x >= 37 && x <= 45)))
                            px[y * sz + x] = new Color(0.85f, 0.85f, 0.85f, 0.35f);
                        else
                            px[y * sz + x] = Color.white;
                    }
                    else
                    {
                        px[y * sz + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            SaveIcon(tex, $"{IconsDir}/Icon_Book.png");
        }

        private static void GenerateQuizIcon()
        {
            const int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[sz * sz];

            // Question Mark inside rounded badge
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - 31.5f;
                    float dy = y - 31.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    // Outer circle ring
                    bool isRing = (r >= 22f && r <= 27f);

                    // Question mark shape
                    bool topArc = (Mathf.Abs(Mathf.Sqrt(dx * dx + (y - 38f) * (y - 38f)) - 8f) <= 3f && y >= 36f);
                    bool rightHook = (x >= 36 && x <= 42 && y >= 32 && y <= 38);
                    bool centerStem = (x >= 29 && x <= 34 && y >= 24 && y <= 32);
                    bool dot = (x >= 29 && x <= 34 && y >= 16 && y <= 21);

                    if (isRing || topArc || rightHook || centerStem || dot)
                    {
                        px[y * sz + x] = Color.white;
                    }
                    else
                    {
                        px[y * sz + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            SaveIcon(tex, $"{IconsDir}/Icon_Quiz.png");
        }

        private static void GenerateExitIcon()
        {
            const int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[sz * sz];

            // Power / Exit symbol: broken circle with vertical line at top
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - 31.5f;
                    float dy = y - 30f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    bool circleArc = (r >= 16f && r <= 22f && (dy < 6f || Mathf.Abs(dx) > 6f));
                    bool vertBar = (Mathf.Abs(dx) <= 3f && y >= 28 && y <= 50);

                    if (circleArc || vertBar)
                    {
                        px[y * sz + x] = Color.white;
                    }
                    else
                    {
                        px[y * sz + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            SaveIcon(tex, $"{IconsDir}/Icon_Exit.png");
        }

        private static void GenerateBackIcon()
        {
            const int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[sz * sz];

            // Left Chevron < with thick line
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float cy = Mathf.Abs(y - 31.5f);
                    float targetX = 20f + cy * 0.9f;
                    if (Mathf.Abs(x - targetX) <= 4f && y >= 14 && y <= 49)
                    {
                        px[y * sz + x] = Color.white;
                    }
                    else
                    {
                        px[y * sz + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            SaveIcon(tex, $"{IconsDir}/Icon_Back.png");
        }

        private static void GenerateStarIcon()
        {
            const int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[sz * sz];

            Vector2 center = new Vector2(31.5f, 31.5f);
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float angle = Mathf.Atan2(pt.y, pt.x);
                    if (angle < 0) angle += Mathf.PI * 2f;

                    // 5-point star formula
                    float m = Mathf.PI * 2f / 5f;
                    float a = (angle + Mathf.PI / 2f) % m;
                    if (a > m / 2f) a = m - a;
                    float rStar = 10f / (Mathf.Cos(a) + Mathf.Sin(a) * 1.376f);
                    float d = pt.magnitude;

                    if (d <= rStar * 1.55f && d <= 26f)
                    {
                        px[y * sz + x] = Color.white;
                    }
                    else
                    {
                        px[y * sz + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            SaveIcon(tex, $"{IconsDir}/Icon_Star.png");
        }

        private static void GenerateLockIcon()
        {
            const int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[sz * sz];

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    // Body: rect from x:18..46, y:12..34
                    bool body = (x >= 18 && x <= 46 && y >= 12 && y <= 34);
                    // Shackle: arch from y:34..52
                    float dx = x - 32f;
                    float dy = y - 36f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    bool shackle = (r >= 8f && r <= 14f && y >= 34);
                    // Keyhole cutout
                    bool keyhole = (Mathf.Abs(dx) <= 2.5f && y >= 20 && y <= 27);

                    if ((body || shackle) && !keyhole)
                    {
                        px[y * sz + x] = Color.white;
                    }
                    else
                    {
                        px[y * sz + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            SaveIcon(tex, $"{IconsDir}/Icon_Lock.png");
        }

        private static void SaveIcon(Texture2D tex, string path)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100;
                ti.mipmapEnabled = false;
                ti.filterMode = FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.SaveAndReimport();
            }
        }

        private static void SetSpriteBorders(string path, Vector4 borders)
        {
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100;
                ti.spriteBorder = borders;
                ti.mipmapEnabled = false;
                ti.filterMode = FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.SaveAndReimport();
            }
        }

        private static float DistanceToRoundedRect(float x, float y, float rx, float ry, float rw, float rh, float radius)
        {
            float cx = Mathf.Clamp(x, rx + radius, rx + rw - radius);
            float cy = Mathf.Clamp(y, ry + radius, ry + rh - radius);
            float dx = x - cx;
            float dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);

            bool insideBox = (x >= rx + radius && x <= rx + rw - radius && y >= ry && y <= ry + rh) ||
                             (x >= rx && x <= rx + rw && y >= ry + radius && y <= ry + rh - radius);

            if (insideBox)
            {
                float dEdge = Mathf.Min(x - rx, rx + rw - x, y - ry, ry + rh - y);
                return -dEdge;
            }

            return dist - radius;
        }

        private static bool PointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float d1 = Sign(pt, v1, v2);
            float d2 = Sign(pt, v2, v3);
            float d3 = Sign(pt, v3, v1);
            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}
#endif
