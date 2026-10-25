using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using TrafficTown2D.Core;
using TrafficTown2D.UI;

namespace TrafficTown2D.Player
{
    public enum IndicatorState
    {
        Off = 0,
        Left = -1,
        Right = 1,
        Hazard = 2
    }

    /// <summary>
    /// Manages player car turn indicators (visual corner lights, overhead indicator marker,
    /// audio relay clicks, user controls Q/E/H, and HUD synchronization).
    /// Works with both top-down and side-view player cars.
    /// </summary>
    public sealed class CarIndicatorController : MonoBehaviour
    {
        [Header("Indicator State")]
        [SerializeField] private IndicatorState currentState = IndicatorState.Off;
        [SerializeField, Range(0.1f, 1f)] private float blinkRate = 0.33f; // seconds per half-cycle (~1.5 Hz)

        [Header("Light Renderers (Auto-created if empty)")]
        [SerializeField] private SpriteRenderer frontLeftLight;
        [SerializeField] private SpriteRenderer frontRightLight;
        [SerializeField] private SpriteRenderer rearLeftLight;
        [SerializeField] private SpriteRenderer rearRightLight;

        [Header("Overhead Player Marker")]
        [SerializeField] private bool showOverheadMarker = true;
        [SerializeField] private Transform overheadMarkerRoot;
        [SerializeField] private TMP_Text overheadLeftChevron;
        [SerializeField] private TMP_Text overheadRightChevron;
        [SerializeField] private TMP_Text overheadPlayerLabel;

        [Header("Audio")]
        [SerializeField] private bool playAudioClicks = true;
        [SerializeField] private AudioSource indicatorAudio;

        // Visual Colors
        private static readonly Color AmberActiveColor = new Color(1.0f, 0.72f, 0.05f, 1.0f);
        private static readonly Color AmberDimColor = new Color(0.28f, 0.18f, 0.02f, 0.20f);
        private static readonly Color ChevronInactiveColor = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color ChevronActiveColor = new Color(1.0f, 0.78f, 0.10f, 1.0f);

        // Runtime State
        private float blinkTimer = 0f;
        private bool isBlinkPhaseOn = false;
        private bool isSideView = false;
        private Vector3 initialOverheadOffset = new Vector3(0f, 2.2f, -0.2f);
        private static Sprite circleSpriteCache;
        private static AudioClip clickOnClip;
        private static AudioClip clickOffClip;

        // Public Accessors
        public IndicatorState State => currentState;
        public bool LeftSignalActive => currentState == IndicatorState.Left || currentState == IndicatorState.Hazard;
        public bool RightSignalActive => currentState == IndicatorState.Right || currentState == IndicatorState.Hazard;
        public bool HazardActive => currentState == IndicatorState.Hazard;
        public bool IsAnyActive => currentState != IndicatorState.Off;
        public bool BlinkState => isBlinkPhaseOn && IsAnyActive;

        // Events
        public event Action<IndicatorState> StateChanged;
        public event Action<int> TurnSignalChanged; // -1 = Left, 1 = Right, 0 = Off, 2 = Hazard

        private void Awake()
        {
            DetectCarOrientation();
            EnsureLightRenderers();
            EnsureOverheadMarker();
            EnsureAudio();
        }

        private void Start()
        {
            ApplyVisualState();
            // Automatically ensure that the HUD exists on the active canvas
            CarIndicatorHUD.EnsureHUD(this);
        }

        private void Update()
        {
            HandleUserInput();
            UpdateBlinking();
        }

        private void LateUpdate()
        {
            UpdateOverheadMarkerTransform();
        }

        #region User Controls
        private void HandleUserInput()
        {
            bool qPressed = false;
            bool ePressed = false;
            bool hPressed = false;

            // 1. Modern Input System
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.qKey.wasPressedThisFrame) qPressed = true;
                if (kb.eKey.wasPressedThisFrame) ePressed = true;
                if (kb.hKey.wasPressedThisFrame) hPressed = true;
            }

            // 2. Legacy Input Fallback
            #if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM_ONLY
            try
            {
                if (Input.GetKeyDown(KeyCode.Q)) qPressed = true;
                if (Input.GetKeyDown(KeyCode.E)) ePressed = true;
                if (Input.GetKeyDown(KeyCode.H)) hPressed = true;
            }
            catch { }
            #endif

            if (qPressed) ToggleLeft();
            else if (ePressed) ToggleRight();
            else if (hPressed) ToggleHazard();
        }

        public void ToggleLeft()
        {
            if (currentState == IndicatorState.Left)
                SetState(IndicatorState.Off);
            else
                SetState(IndicatorState.Left);
        }

        public void ToggleRight()
        {
            if (currentState == IndicatorState.Right)
                SetState(IndicatorState.Off);
            else
                SetState(IndicatorState.Right);
        }

        public void ToggleHazard()
        {
            if (currentState == IndicatorState.Hazard)
                SetState(IndicatorState.Off);
            else
                SetState(IndicatorState.Hazard);
        }

        public void CancelSignals()
        {
            if (currentState != IndicatorState.Off)
            {
                SetState(IndicatorState.Off);
            }
        }

        public void SetSignal(int signal)
        {
            switch (signal)
            {
                case -1: SetState(IndicatorState.Left); break;
                case 1:  SetState(IndicatorState.Right); break;
                case 2:  SetState(IndicatorState.Hazard); break;
                default: SetState(IndicatorState.Off); break;
            }
        }

        private void SetState(IndicatorState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            blinkTimer = 0f;
            isBlinkPhaseOn = (newState != IndicatorState.Off);

            if (isBlinkPhaseOn)
            {
                PlayClick(true);
            }

            ApplyVisualState();

            StateChanged?.Invoke(currentState);
            TurnSignalChanged?.Invoke((int)currentState);
        }
        #endregion

        #region Blinking & Visuals
        private void UpdateBlinking()
        {
            if (currentState == IndicatorState.Off)
            {
                if (isBlinkPhaseOn)
                {
                    isBlinkPhaseOn = false;
                    ApplyVisualState();
                }
                return;
            }

            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkRate)
            {
                blinkTimer -= blinkRate;
                isBlinkPhaseOn = !isBlinkPhaseOn;
                PlayClick(isBlinkPhaseOn);
                ApplyVisualState();
            }
        }

        private void ApplyVisualState()
        {
            bool leftLit = (currentState == IndicatorState.Left || currentState == IndicatorState.Hazard) && isBlinkPhaseOn;
            bool rightLit = (currentState == IndicatorState.Right || currentState == IndicatorState.Hazard) && isBlinkPhaseOn;

            // Corner lights
            if (isSideView)
            {
                // In side-view, front and rear lights blink together for lane changes/turns
                bool sideLit = (currentState != IndicatorState.Off) && isBlinkPhaseOn;
                SetLight(frontLeftLight, sideLit);
                SetLight(rearLeftLight, sideLit);
            }
            else
            {
                // Top-down mode: separate left / right pairs
                SetLight(frontLeftLight, leftLit);
                SetLight(rearLeftLight, leftLit);
                SetLight(frontRightLight, rightLit);
                SetLight(rearRightLight, rightLit);
            }

            // Overhead chevrons
            if (overheadLeftChevron != null)
            {
                overheadLeftChevron.color = leftLit ? ChevronActiveColor : ChevronInactiveColor;
            }
            if (overheadRightChevron != null)
            {
                overheadRightChevron.color = rightLit ? ChevronActiveColor : ChevronInactiveColor;
            }
        }

        private void SetLight(SpriteRenderer sr, bool active)
        {
            if (sr == null) return;
            sr.color = active ? AmberActiveColor : AmberDimColor;
            float s = active ? 0.38f : 0.28f;
            sr.transform.localScale = new Vector3(s, s, 1f);
        }

        private void UpdateOverheadMarkerTransform()
        {
            if (overheadMarkerRoot == null || !showOverheadMarker) return;

            // Keep upright in world space so "YOU" and pointer stay readable regardless of car rotation
            float bob = Mathf.Sin(Time.time * 3.8f) * 0.08f;
            overheadMarkerRoot.position = transform.position + initialOverheadOffset + new Vector3(0f, bob, 0f);
            overheadMarkerRoot.rotation = Quaternion.identity;
        }
        #endregion

        #region Setup & Auto-Detection
        private void DetectCarOrientation()
        {
            // If car collider is wider than tall, it's a side-view car (Levels 8, 9, 10)
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null && col.size.x > col.size.y)
            {
                isSideView = true;
                initialOverheadOffset = new Vector3(0f, 1.35f, -0.2f);
            }
            else
            {
                isSideView = false;
                initialOverheadOffset = new Vector3(0f, 2.1f, -0.2f);
            }
        }

        private void EnsureLightRenderers()
        {
            Sprite circle = GetOrCreateCircleSprite();

            if (isSideView)
            {
                if (frontLeftLight == null)
                    frontLeftLight = CreateLightChild("SideLight_Front", new Vector3(0.74f, 0.08f, -0.05f), circle);
                if (rearLeftLight == null)
                    rearLeftLight = CreateLightChild("SideLight_Rear", new Vector3(-0.74f, 0.08f, -0.05f), circle);
            }
            else
            {
                if (frontLeftLight == null)
                    frontLeftLight = CreateLightChild("IndicatorLight_FL", new Vector3(-0.62f, 1.45f, -0.05f), circle);
                if (frontRightLight == null)
                    frontRightLight = CreateLightChild("IndicatorLight_FR", new Vector3(0.62f, 1.45f, -0.05f), circle);
                if (rearLeftLight == null)
                    rearLeftLight = CreateLightChild("IndicatorLight_RL", new Vector3(-0.62f, -1.55f, -0.05f), circle);
                if (rearRightLight == null)
                    rearRightLight = CreateLightChild("IndicatorLight_RR", new Vector3(0.62f, -1.55f, -0.05f), circle);
            }
        }

        private SpriteRenderer CreateLightChild(string childName, Vector3 localPos, Sprite sprite)
        {
            Transform existing = transform.Find(childName);
            GameObject obj;
            if (existing != null)
            {
                obj = existing.gameObject;
            }
            else
            {
                obj = new GameObject(childName);
                obj.transform.SetParent(transform, false);
                obj.transform.localPosition = localPos;
                obj.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
            }

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = AmberDimColor;
            sr.sortingOrder = 16;
            return sr;
        }

        private void EnsureOverheadMarker()
        {
            if (!showOverheadMarker) return;

            string markerName = "CarIndicatorMarker";
            Transform existing = transform.Find(markerName);
            GameObject markerObj;
            if (existing != null)
            {
                markerObj = existing.gameObject;
            }
            else
            {
                markerObj = new GameObject(markerName);
                // Parented to car initially for lifecycle, but positioned independently in LateUpdate
                markerObj.transform.SetParent(transform, false);
                markerObj.transform.localPosition = initialOverheadOffset;
            }
            overheadMarkerRoot = markerObj.transform;

            // 1. Downward Pointer / Chevron (World sprite)
            Transform pointerTransform = markerObj.transform.Find("Pointer");
            if (pointerTransform == null)
            {
                GameObject pointerObj = new GameObject("Pointer");
                pointerObj.transform.SetParent(markerObj.transform, false);
                pointerObj.transform.localPosition = new Vector3(0f, -0.25f, 0f);
                pointerObj.transform.localScale = new Vector3(0.22f, 0.22f, 1f);
                SpriteRenderer sr = pointerObj.AddComponent<SpriteRenderer>();
                sr.sprite = GetOrCreateCircleSprite();
                sr.color = new Color(0.18f, 0.72f, 1.0f, 0.95f);
                sr.sortingOrder = 25;
            }

            // 2. Player Label ("YOU")
            Transform labelTransform = markerObj.transform.Find("PlayerLabel");
            if (labelTransform == null)
            {
                GameObject labelObj = new GameObject("PlayerLabel");
                labelObj.transform.SetParent(markerObj.transform, false);
                labelObj.transform.localPosition = Vector3.zero;
                TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
                tmp.text = "YOU";
                tmp.fontSize = 3.6f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(0.20f, 0.85f, 1.0f, 1.0f);
                if (labelObj.TryGetComponent<MeshRenderer>(out var mr)) mr.sortingOrder = 26;
                tmp.rectTransform.sizeDelta = new Vector2(2f, 1f);
                overheadPlayerLabel = tmp;
            }
            else
            {
                overheadPlayerLabel = labelTransform.GetComponent<TMP_Text>();
            }

            // 3. Left Turn Chevron (◄)
            Transform leftChev = markerObj.transform.Find("LeftChevron");
            if (leftChev == null)
            {
                GameObject lObj = new GameObject("LeftChevron");
                lObj.transform.SetParent(markerObj.transform, false);
                lObj.transform.localPosition = new Vector3(-0.85f, 0f, 0f);
                TextMeshPro tmp = lObj.AddComponent<TextMeshPro>();
                tmp.text = "◄";
                tmp.fontSize = 4.2f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = ChevronInactiveColor;
                if (lObj.TryGetComponent<MeshRenderer>(out var mr)) mr.sortingOrder = 26;
                tmp.rectTransform.sizeDelta = new Vector2(1f, 1f);
                overheadLeftChevron = tmp;
            }
            else
            {
                overheadLeftChevron = leftChev.GetComponent<TMP_Text>();
            }

            // 4. Right Turn Chevron (►)
            Transform rightChev = markerObj.transform.Find("RightChevron");
            if (rightChev == null)
            {
                GameObject rObj = new GameObject("RightChevron");
                rObj.transform.SetParent(markerObj.transform, false);
                rObj.transform.localPosition = new Vector3(0.85f, 0f, 0f);
                TextMeshPro tmp = rObj.AddComponent<TextMeshPro>();
                tmp.text = "►";
                tmp.fontSize = 4.2f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = ChevronInactiveColor;
                if (rObj.TryGetComponent<MeshRenderer>(out var mr)) mr.sortingOrder = 26;
                tmp.rectTransform.sizeDelta = new Vector2(1f, 1f);
                overheadRightChevron = tmp;
            }
            else
            {
                overheadRightChevron = rightChev.GetComponent<TMP_Text>();
            }
        }

        private void EnsureAudio()
        {
            if (!playAudioClicks) return;

            if (indicatorAudio == null)
            {
                indicatorAudio = GetComponent<AudioSource>();
                if (indicatorAudio == null)
                {
                    indicatorAudio = gameObject.AddComponent<AudioSource>();
                    indicatorAudio.playOnAwake = false;
                    indicatorAudio.loop = false;
                    indicatorAudio.spatialBlend = 0f; // 2D clean sound
                    indicatorAudio.volume = 0.28f;
                }
            }

            EnsureProceduralClips();
        }

        private void PlayClick(bool isOn)
        {
            if (!playAudioClicks || indicatorAudio == null) return;
            EnsureProceduralClips();
            AudioClip clip = isOn ? clickOnClip : clickOffClip;
            if (clip != null)
            {
                indicatorAudio.PlayOneShot(clip, 0.35f);
            }
        }

        private static void EnsureProceduralClips()
        {
            if (clickOnClip != null && clickOffClip != null) return;

            int sampleRate = 44100;
            int samples = (int)(sampleRate * 0.035f); // 35ms crisp relay tick

            // High tick for ON (~900Hz)
            float[] dataOn = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 140f);
                dataOn[i] = Mathf.Sin(2f * Mathf.PI * 920f * t) * decay * 0.45f;
            }
            clickOnClip = AudioClip.Create("IndicatorRelayOn", samples, 1, sampleRate, false);
            clickOnClip.SetData(dataOn, 0);

            // Slightly lower pitch tick for OFF (~680Hz)
            float[] dataOff = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 140f);
                dataOff[i] = Mathf.Sin(2f * Mathf.PI * 680f * t) * decay * 0.38f;
            }
            clickOffClip = AudioClip.Create("IndicatorRelayOff", samples, 1, sampleRate, false);
            clickOffClip.SetData(dataOff, 0);
        }

        public static Sprite GetOrCreateCircleSprite()
        {
            if (circleSpriteCache != null) return circleSpriteCache;

            #if UNITY_EDITOR
            Sprite loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Generated/WorldCircle.png");
            if (loaded != null)
            {
                circleSpriteCache = loaded;
                return circleSpriteCache;
            }
            #endif

            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32 fill = new Color32(255, 255, 255, 255);
            Color32 clear = new Color32(255, 255, 255, 0);
            float center = (size - 1) * 0.5f;
            float rSq = (size * 0.46f) * (size * 0.46f);
            Color32[] px = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    px[y * size + x] = (dx * dx + dy * dy <= rSq) ? fill : clear;
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            circleSpriteCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
            return circleSpriteCache;
        }
        #endregion
    }
}
