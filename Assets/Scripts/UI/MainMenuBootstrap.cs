using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace TrafficTown2D.UI
{
    public static class MainMenuBootstrap
    {
        private const string BgResource = "UI/MainMenu/MainMenuBackground";
        private const string CardBaseResource = "UI/MainMenu/Card_Rounded_Base";
        private const string ButtonBaseResource = "UI/MainMenu/Button_Rounded_3D";
        private const string PillResource = "UI/MainMenu/Pill_Badge";

        private const string IconPlayResource = "UI/MainMenu/Icons/Icon_Play";
        private const string IconBookResource = "UI/MainMenu/Icons/Icon_Book";
        private const string IconQuizResource = "UI/MainMenu/Icons/Icon_Quiz";
        private const string IconExitResource = "UI/MainMenu/Icons/Icon_Exit";
        private const string IconBackResource = "UI/MainMenu/Icons/Icon_Back";
        private const string IconStarResource = "UI/MainMenu/Icons/Icon_Star";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == "MainMenu")
            {
                EnsureModernMainMenu();
            }
        }

        public static void EnsureModernMainMenu()
        {
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Check if modern background overlay is already present (scene already fully baked)
            Transform overlay = canvas.transform.Find("BackgroundOverlay");
            if (overlay != null)
            {
                // Scene is already modern and baked!
                return;
            }

            Debug.Log("[MainMenuBootstrap] Upgrading MainMenu UI to modern visual theme at runtime...");

            // 1. Background
            Transform bgT = canvas.transform.Find("Background");
            if (bgT != null)
            {
                Image bgImg = bgT.GetComponent<Image>();
                if (bgImg != null)
                {
                    Sprite s = Resources.Load<Sprite>(BgResource);
                    if (s != null)
                    {
                        bgImg.sprite = s;
                        bgImg.color = Color.white;
                    }
                }

                // Add dark contrast overlay
                GameObject ovObj = new GameObject("BackgroundOverlay", typeof(RectTransform));
                ovObj.transform.SetParent(canvas.transform, false);
                ovObj.transform.SetSiblingIndex(1);
                Image ovImg = ovObj.AddComponent<Image>();
                ovImg.color = new Color(0.04f, 0.08f, 0.14f, 0.42f);
                SetFullScreen(ovObj.GetComponent<RectTransform>());
            }

            // 2. Buttons Styling
            Transform content = canvas.transform.Find("MainMenuContent");
            if (content != null)
            {
                StyleButton(content.Find("PlayButton"), new Color(1f, 0.62f, 0.12f, 1f), new Color(1f, 0.72f, 0.22f, 1f), new Color(0.85f, 0.50f, 0.08f, 1f), 1.05f, IconPlayResource);
                StyleButton(content.Find("LearnButton"), new Color(0.12f, 0.65f, 0.62f, 1f), new Color(0.16f, 0.75f, 0.72f, 1f), new Color(0.08f, 0.52f, 0.50f, 1f), 1.04f, IconBookResource);
                StyleButton(content.Find("QuizButton"), new Color(0.18f, 0.70f, 0.36f, 1f), new Color(0.22f, 0.80f, 0.42f, 1f), new Color(0.12f, 0.58f, 0.28f, 1f), 1.04f, IconQuizResource);
                StyleButton(content.Find("ExitButton"), new Color(0.82f, 0.24f, 0.22f, 1f), new Color(0.90f, 0.30f, 0.28f, 1f), new Color(0.68f, 0.18f, 0.16f, 1f), 1.04f, IconExitResource);
            }

            // 3. Level Selection Panel
            Transform panel = canvas.transform.Find("LevelSelectPanel");
            if (panel != null)
            {
                Image pBg = panel.GetComponent<Image>();
                if (pBg != null) pBg.color = new Color(0.05f, 0.08f, 0.14f, 0.94f);

                Sprite cardBase = Resources.Load<Sprite>(CardBaseResource);
                Sprite pill = Resources.Load<Sprite>(PillResource);
                Sprite star = Resources.Load<Sprite>(IconStarResource);

                Color[] accents = {
                    new Color(0.18f, 0.80f, 0.44f, 1f),
                    new Color(0.95f, 0.77f, 0.12f, 1f),
                    new Color(0.92f, 0.52f, 0.15f, 1f),
                    new Color(0.20f, 0.60f, 0.88f, 1f),
                    new Color(0.90f, 0.30f, 0.25f, 1f),
                    new Color(0.12f, 0.78f, 0.72f, 1f),
                    new Color(0.18f, 0.72f, 0.38f, 1f),
                    new Color(0.85f, 0.22f, 0.25f, 1f),
                    new Color(0.62f, 0.36f, 0.82f, 1f),
                    new Color(0.96f, 0.68f, 0.12f, 1f)
                };

                for (int i = 1; i <= 10; i++)
                {
                    Transform btnT = panel.Find("LevelBtn_" + i);
                    if (btnT != null)
                    {
                        Image cardImg = btnT.GetComponent<Image>();
                        if (cardImg != null && cardBase != null)
                        {
                            cardImg.sprite = cardBase;
                            cardImg.type = Image.Type.Sliced;
                            cardImg.color = new Color(0.10f, 0.15f, 0.24f, 0.95f);
                        }

                        // Add animated hover
                        AnimatedUIButton anim = btnT.GetComponent<AnimatedUIButton>();
                        if (anim == null) anim = btnT.gameObject.AddComponent<AnimatedUIButton>();
                        anim.Configure(new Color(0.10f, 0.15f, 0.24f, 0.95f), new Color(0.14f, 0.22f, 0.35f, 1f), new Color(0.08f, 0.12f, 0.20f, 1f), 1.04f, 0.97f, cardImg);

                        // Attach thumbnail if missing
                        Transform thumbT = btnT.Find("Thumbnail");
                        if (thumbT == null)
                        {
                            GameObject thObj = new GameObject("Thumbnail", typeof(RectTransform));
                            thObj.transform.SetParent(btnT, false);
                            thObj.transform.SetAsFirstSibling();
                            RectTransform thR = thObj.GetComponent<RectTransform>();
                            SetRect(thR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(178f, 76f));
                            Image thImg = thObj.AddComponent<Image>();
                            Sprite ts = Resources.Load<Sprite>($"UI/MainMenu/Thumbnails/Thumbnail_Level{i}");
                            if (ts != null) thImg.sprite = ts;
                            else thImg.color = new Color(accents[i - 1].r * 0.35f, accents[i - 1].g * 0.35f, accents[i - 1].b * 0.35f, 1f);
                        }
                    }
                }

                // Close Button
                Transform closeT = panel.Find("CloseButton");
                if (closeT != null)
                {
                    Image cImg = closeT.GetComponent<Image>();
                    Sprite btnBase = Resources.Load<Sprite>(ButtonBaseResource);
                    if (cImg != null && btnBase != null)
                    {
                        cImg.sprite = btnBase;
                        cImg.type = Image.Type.Sliced;
                        cImg.color = new Color(0.16f, 0.24f, 0.34f, 1f);
                    }
                    AnimatedUIButton cAnim = closeT.GetComponent<AnimatedUIButton>();
                    if (cAnim == null) cAnim = closeT.gameObject.AddComponent<AnimatedUIButton>();
                    cAnim.Configure(new Color(0.16f, 0.24f, 0.34f, 1f), new Color(0.22f, 0.32f, 0.44f, 1f), new Color(0.12f, 0.18f, 0.26f, 1f), 1.04f, 0.96f, cImg);
                }
            }
        }

        private static void StyleButton(Transform btnT, Color normal, Color hover, Color pressed, float hScale, string iconPath)
        {
            if (btnT == null) return;
            Image img = btnT.GetComponent<Image>();
            Sprite btnBase = Resources.Load<Sprite>(ButtonBaseResource);
            if (img != null && btnBase != null)
            {
                img.sprite = btnBase;
                img.type = Image.Type.Sliced;
                img.color = normal;
            }

            AnimatedUIButton anim = btnT.GetComponent<AnimatedUIButton>();
            if (anim == null) anim = btnT.gameObject.AddComponent<AnimatedUIButton>();
            anim.Configure(normal, hover, pressed, hScale, 0.95f, img);

            // Add icon if missing
            Transform iconT = btnT.Find("Icon");
            if (iconT == null && !string.IsNullOrEmpty(iconPath))
            {
                Sprite ispr = Resources.Load<Sprite>(iconPath);
                if (ispr != null)
                {
                    GameObject ico = new GameObject("Icon", typeof(RectTransform));
                    ico.transform.SetParent(btnT, false);
                    RectTransform ir = ico.GetComponent<RectTransform>();
                    float w = btnT.GetComponent<RectTransform>().rect.width;
                    SetRect(ir, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-(w * 0.5f) + 36f, 0f), new Vector2(24f, 24f));
                    Image icoImg = ico.AddComponent<Image>();
                    icoImg.sprite = ispr;
                    icoImg.color = Color.white;
                    icoImg.raycastTarget = false;
                }
            }
        }

        private static void SetFullScreen(RectTransform rt)
        {
            SetRect(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
