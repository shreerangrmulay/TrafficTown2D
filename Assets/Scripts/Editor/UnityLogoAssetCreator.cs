#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrafficTown2D.Editor
{
    public static class UnityLogoAssetCreator
    {
        public const string UnityLogoPath = "Assets/UI/UnityLogo.png";

        [InitializeOnLoadMethod]
        private static void AutoCreateOnLoad()
        {
            if (!File.Exists(UnityLogoPath))
            {
                EditorApplication.delayCall += () =>
                {
                    GenerateLogoPng();
                };
            }
        }

        [MenuItem("TrafficTown/Generate Unity Logo Asset")]
        public static void GenerateLogoPng()
        {
            const int size = 512;
            const int super = 3; // 3x supersampling for ultra smooth anti-aliased edges
            float padding = size * 0.08f;
            float usable = size - (padding * 2f);
            float scale = usable / 100.0f;

            // Map SVG (0..100) to texture coordinates
            // SVG is top-left origin (0,0 is top-left), Texture2D is bottom-left origin (0,0 is bottom-left)
            Vector2 M(float x, float y)
            {
                float px = padding + (x * scale);
                float py = padding + ((100.0f - y) * scale);
                return new Vector2(px, py);
            }

            // 1. Right arrow wedge
            Vector2[] poly1 = new Vector2[]
            {
                M(82.775f, 79.842f),
                M(64.900f, 49.775f),
                M(82.775f, 19.708f),
                M(91.392f, 49.775f)
            };

            // 2. Bottom-left arrow wedge
            Vector2[] poly2 = new Vector2[]
            {
                M(42.992f, 76.908f),
                M(20.533f, 54.633f),
                M(56.283f, 54.633f),
                M(74.158f, 84.700f)
            };

            // 3. Top-left arrow wedge
            Vector2[] poly3 = new Vector2[]
            {
                M(42.992f, 22.550f),
                M(74.067f, 14.758f),
                M(56.192f, 44.825f),
                M(20.442f, 44.825f)
            };

            // 4. Outer framing hexagon
            Vector2[] poly4 = new Vector2[]
            {
                M(88.642f, 0.000f),
                M(48.033f, 10.542f),
                M(41.983f, 20.900f),
                M(29.792f, 20.808f),
                M(0.000f, 49.775f),
                M(29.792f, 78.742f),
                M(41.983f, 78.650f),
                M(48.033f, 89.008f),
                M(88.642f, 99.642f),
                M(99.550f, 60.133f),
                M(93.408f, 49.867f),
                M(99.550f, 39.600f)
            };

            Vector2[][] allPolys = new Vector2[][] { poly1, poly2, poly3, poly4 };

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];

            float step = 1f / super;
            float halfStep = step * 0.5f;

            for (int y = 0; y < size; y++)
            {
                int rowIdx = y * size;
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < super; sy++)
                    {
                        float py = y + (sy * step) + halfStep;
                        for (int sx = 0; sx < super; sx++)
                        {
                            float px = x + (sx * step) + halfStep;

                            int insideCount = 0;
                            for (int p = 0; p < allPolys.Length; p++)
                            {
                                if (PointInPoly(px, py, allPolys[p]))
                                {
                                    insideCount++;
                                }
                            }

                            // Even-Odd fill rule
                            if ((insideCount % 2) != 0)
                            {
                                hits++;
                            }
                        }
                    }

                    if (hits == 0)
                    {
                        pixels[rowIdx + x] = new Color32(255, 255, 255, 0);
                    }
                    else
                    {
                        byte a = (byte)((hits * 255) / (super * super));
                        pixels[rowIdx + x] = new Color32(255, 255, 255, a);
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            byte[] pngBytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string dir = Path.GetDirectoryName(UnityLogoPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllBytes(UnityLogoPath, pngBytes);
            AssetDatabase.ImportAsset(UnityLogoPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(UnityLogoPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            Debug.Log("[UnityLogoAssetCreator] Generated crisp official Unity Logo at: " + UnityLogoPath);
        }

        private static bool PointInPoly(float px, float py, Vector2[] poly)
        {
            int count = poly.Length;
            bool inside = false;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                float xi = poly[i].x, yi = poly[i].y;
                float xj = poly[j].x, yj = poly[j].y;

                bool intersect = ((yi > py) != (yj > py)) &&
                                 (px < (xj - xi) * (py - yi) / (yj - yi + 0.0000001f) + xi);
                if (intersect)
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}
#endif
