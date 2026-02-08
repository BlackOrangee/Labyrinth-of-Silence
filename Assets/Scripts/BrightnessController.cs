using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Assets.Scripts
{
    /// <summary>
    /// Controls screen brightness using multiple methods:
    /// 1. URP Volume (preferred)
    /// 2. UI Overlay (fallback)
    /// </summary>
    public class BrightnessController : MonoBehaviour
    {
        private static BrightnessController instance;
        public static BrightnessController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<BrightnessController>();
                }

                if (instance == null)
                {
                    GameObject go = new GameObject("BrightnessController");
                    instance = go.AddComponent<BrightnessController>();
                }

                return instance;
            }
        }

        [Header("Method Selection")]
        [Tooltip("Automatically choose best method")]
        public bool autoDetect = true;

        [Tooltip("Force use UI overlay method")]
        public bool forceUIOverlay = false;

        [Header("Volume Settings")]
        public Volume brightnessVolume;

        [Header("Brightness Range")]
        public float minBrightness = -1.5f;
        public float maxBrightness = 1.5f;

        private ColorAdjustments colorAdjustments;
        private bool volumeMethodAvailable = false;

        // UI Overlay fallback
        private Canvas overlayCanvas;
        private Image overlayImage;
        private float currentBrightness = 0.5f;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeBrightnessControl();
        }

        void Start()
        {
            if (SettingsManager.Instance != null)
            {
                ApplyBrightness(SettingsManager.Instance.currentSettings.brightness);
            }
        }

        private void InitializeBrightnessControl()
        {
            Debug.Log("[BrightnessController] Initializing brightness control...");

            if (!forceUIOverlay && TryInitializeVolumeMethod())
            {
                volumeMethodAvailable = true;
                Debug.Log("[BrightnessController] Using URP Volume method");
            }
            else
            {
                InitializeUIOverlay();
                Debug.Log("[BrightnessController] Using UI Overlay method");
            }
        }

        private bool TryInitializeVolumeMethod()
        {
            try
            {
                if (brightnessVolume == null)
                {
                    GameObject volumeGO = new GameObject("BrightnessVolume");
                    volumeGO.transform.SetParent(transform);
                    brightnessVolume = volumeGO.AddComponent<Volume>();
                    brightnessVolume.isGlobal = true;
                    brightnessVolume.priority = 1000;
                    brightnessVolume.weight = 1f;
                }

                if (brightnessVolume.profile == null)
                {
                    brightnessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
                }

                if (!brightnessVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
                {
                    colorAdjustments = brightnessVolume.profile.Add<ColorAdjustments>(true);
                }

                if (colorAdjustments != null)
                {
                    colorAdjustments.active = true;
                    colorAdjustments.postExposure.overrideState = true;
                    colorAdjustments.postExposure.value = 0f;
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BrightnessController] Failed to initialize Volume method: {e.Message}");
            }

            return false;
        }

        private void InitializeUIOverlay()
        {
            GameObject canvasGO = new GameObject("BrightnessOverlay");
            canvasGO.transform.SetParent(transform);
            overlayCanvas = canvasGO.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 32767; // Maximum sorting order

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            GameObject imageGO = new GameObject("BrightnessImage");
            imageGO.transform.SetParent(canvasGO.transform, false);
            overlayImage = imageGO.AddComponent<Image>();
            overlayImage.color = new Color(1, 1, 1, 0); // Start transparent

            RectTransform rt = overlayImage.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            overlayImage.raycastTarget = false;

            Debug.Log("[BrightnessController] UI Overlay initialized");
        }

        public void ApplyBrightness(float normalizedBrightness)
        {
            currentBrightness = Mathf.Clamp01(normalizedBrightness);

            if (volumeMethodAvailable && colorAdjustments != null && brightnessVolume != null)
            {
                ApplyVolumeMethod(normalizedBrightness);
            }
            else if (overlayImage != null)
            {
                ApplyUIOverlayMethod(normalizedBrightness);
            }
            else
            {
                Debug.LogWarning("[BrightnessController] No method available!");
            }
        }

        private void ApplyVolumeMethod(float normalizedBrightness)
        {
            float brightness = Mathf.Lerp(minBrightness, maxBrightness, normalizedBrightness);
            colorAdjustments.postExposure.value = brightness;
            brightnessVolume.enabled = true;
            brightnessVolume.weight = 1f;

            Debug.Log($"[BrightnessController] Volume: {brightness:F2} (normalized: {normalizedBrightness:F2})");
        }

        private void ApplyUIOverlayMethod(float normalizedBrightness)
        {
            if (normalizedBrightness < 0.5f)
            {
                float alpha = (0.5f - normalizedBrightness) * 1.5f; // 0 to 0.75
                overlayImage.color = new Color(0, 0, 0, alpha);
            }
            else if (normalizedBrightness > 0.5f)
            {
                float alpha = (normalizedBrightness - 0.5f) * 1.0f; // 0 to 0.5
                overlayImage.color = new Color(1, 1, 1, alpha);
            }
            else
            {
                overlayImage.color = new Color(1, 1, 1, 0);
            }

            Debug.Log($"[BrightnessController] UI Overlay: brightness={normalizedBrightness:F2}, color={overlayImage.color}");
        }

        public float GetCurrentBrightness()
        {
            return currentBrightness;
        }

        public void ResetBrightness()
        {
            ApplyBrightness(0.5f);
        }

        /// <summary>
        /// Toggle between Volume and UI Overlay methods
        /// </summary>
        public void ToggleMethod()
        {
            forceUIOverlay = !forceUIOverlay;
            InitializeBrightnessControl();
            ApplyBrightness(currentBrightness);
        }
    }
}
