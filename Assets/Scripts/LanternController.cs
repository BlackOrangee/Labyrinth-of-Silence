using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Controls lantern light radius and intensity
    /// Attached to player or lantern object
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LanternController : MonoBehaviour
    {
        [Header("Light Range Settings")]
        [Tooltip("Minimum light range")]
        public float minRange = 3f;
        [Tooltip("Maximum light range")]
        public float maxRange = 12f;
        [Tooltip("Initial light range")]
        public float initialRange = 7f;
        [Tooltip("How fast range changes")]
        public float rangeChangeSpeed = 5f;

        [Header("Light Intensity Settings")]
        [Tooltip("Minimum light intensity")]
        public float minIntensity = 0.5f;
        [Tooltip("Maximum light intensity")]
        public float maxIntensity = 2.0f;
        [Tooltip("Initial light intensity")]
        public float initialIntensity = 1.0f;
        [Tooltip("Auto-adjust intensity based on range for realistic light reach")]
        public bool autoAdjustIntensity = true;
        [Tooltip("How much of the intensity range to use (0.0 = none, 1.0 = full range to maxIntensity)")]
        [Range(0f, 1f)]
        public float intensityScaleFactor = 0.8f;
        [Tooltip("Extend visual light range based on intensity (higher intensity = light reaches further)")]
        public bool extendRangeWithIntensity = true;
        [Tooltip("How much to extend range per intensity point (e.g., 0.3 = +30% range per intensity)")]
        [Range(0f, 1f)]
        public float rangeExtensionFactor = 0.3f;

        [Header("Input Settings")]
        [Tooltip("Key to increase light radius")]
        public KeyCode increaseKey = KeyCode.Q;
        [Tooltip("Key to decrease light radius")]
        public KeyCode decreaseKey = KeyCode.E;
        [Tooltip("Use mouse scroll for control")]
        public bool useMouseScroll = true;
        [Tooltip("Mouse scroll sensitivity")]
        public float scrollSensitivity = 2f;

        [Header("Light Modes")]
        [Tooltip("Available light intensity modes")]
        public float[] intensityModes = new float[] { 0.5f, 1.0f, 1.5f, 2.0f };
        [Tooltip("Key to toggle between intensity modes")]
        public KeyCode toggleModeKey = KeyCode.F;

        [Header("Visual Feedback")]
        [Tooltip("Show current light level in console")]
        public bool showDebugInfo = true;

        private Light lanternLight;
        private float targetRange;
        private int currentModeIndex = 1;

        public float CurrentRange => lanternLight ? lanternLight.range : 0f;
        public float CurrentIntensity => lanternLight ? lanternLight.intensity : 0f;
        public float NormalizedRange => lanternLight ? Mathf.InverseLerp(minRange, maxRange, lanternLight.range) : 0f;

        public float BaseRange => targetRange;

        public float EffectiveRange
        {
            get
            {
                if (!lanternLight || !extendRangeWithIntensity)
                {
                    return CurrentRange;
                }

                float intensityNormalized = Mathf.InverseLerp(minIntensity, maxIntensity, CurrentIntensity);
                float rangeExtension = targetRange * intensityNormalized * rangeExtensionFactor;
                return targetRange + rangeExtension;
            }
        }

        void Start()
        {
            lanternLight = GetComponent<Light>();

            if (lanternLight == null)
            {
                Debug.LogError("LanternController requires a Light component!");
                enabled = false;
                return;
            }

            lanternLight.range = initialRange;
            lanternLight.intensity = initialIntensity;
            targetRange = initialRange;

            if (lanternLight.type == LightType.Directional)
            {
                Debug.LogWarning("LanternController works best with Point or Spot lights. Directional light detected.");
            }

            for (int i = 0; i < intensityModes.Length; i++)
            {
                if (Mathf.Approximately(intensityModes[i], initialIntensity))
                {
                    currentModeIndex = i;
                    break;
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"Lantern initialized - Range: {initialRange}, Intensity: {initialIntensity}");
            }
        }

        void Update()
        {
            HandleInput();
            UpdateLightRange();
        }

        void HandleInput()
        {
            if (useMouseScroll)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    targetRange += scroll * scrollSensitivity;
                    targetRange = Mathf.Clamp(targetRange, minRange, maxRange);
                }
            }

            if (Input.GetKey(increaseKey))
            {
                targetRange += rangeChangeSpeed * Time.deltaTime;
                targetRange = Mathf.Clamp(targetRange, minRange, maxRange);
            }

            if (Input.GetKey(decreaseKey))
            {
                targetRange -= rangeChangeSpeed * Time.deltaTime;
                targetRange = Mathf.Clamp(targetRange, minRange, maxRange);
            }

            if (Input.GetKeyDown(toggleModeKey))
            {
                ToggleIntensityMode();
            }
        }

        void UpdateLightRange()
        {
            if (lanternLight == null)
            {
                return;
            }
            
            if (autoAdjustIntensity)
            {
                float rangeNormalized = Mathf.InverseLerp(minRange, maxRange, targetRange);

                float baseIntensity = intensityModes[currentModeIndex];

                float intensityRange = maxIntensity - baseIntensity;
                float intensityBoost = rangeNormalized * intensityRange * intensityScaleFactor;
                float adjustedIntensity = baseIntensity + intensityBoost;

                adjustedIntensity = Mathf.Clamp(adjustedIntensity, minIntensity, maxIntensity);

                lanternLight.intensity = adjustedIntensity;

                if (showDebugInfo && Mathf.Abs(lanternLight.intensity - adjustedIntensity) > 0.01f)
                {
                    Debug.Log($"[Lantern] Range: {targetRange:F1}, Base Intensity: {baseIntensity:F1}, Adjusted: {adjustedIntensity:F1}");
                }
            }

            float effectiveRange = extendRangeWithIntensity ? EffectiveRange : targetRange;
            lanternLight.range = Mathf.Lerp(lanternLight.range, effectiveRange, Time.deltaTime * 10f);
        }

        void ToggleIntensityMode()
        {
            if (intensityModes.Length == 0)
            {
                return;
            }

            currentModeIndex = (currentModeIndex + 1) % intensityModes.Length;
            float newIntensity = intensityModes[currentModeIndex];
            newIntensity = Mathf.Clamp(newIntensity, minIntensity, maxIntensity);

            if (lanternLight == null)
            {
                return;
            }
            
            if (!autoAdjustIntensity)
            {
                lanternLight.intensity = newIntensity;
            }

            if (showDebugInfo)
            {
                string intensityInfo = autoAdjustIntensity
                    ? $"Base: {newIntensity} (auto-adjusted by range)"
                    : $"{newIntensity}";
                Debug.Log($"Lantern mode changed - Intensity: {intensityInfo} (Mode {currentModeIndex + 1}/{intensityModes.Length})");
            }
        }

        public void SetRange(float range)
        {
            targetRange = Mathf.Clamp(range, minRange, maxRange);
        }

        public void SetIntensity(float intensity)
        {
            intensity = Mathf.Clamp(intensity, minIntensity, maxIntensity);
            if (lanternLight != null)
            {
                lanternLight.intensity = intensity;
            }
        }

        public bool IsAtMaxRange()
        {
            return Mathf.Approximately(CurrentRange, maxRange);
        }

        public bool IsAtMinRange()
        {
            return Mathf.Approximately(CurrentRange, minRange);
        }

        void OnDrawGizmosSelected()
        {
            Light light = GetComponent<Light>();
            if (light == null) return;

            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, targetRange);

            if (extendRangeWithIntensity)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
                Gizmos.DrawWireSphere(transform.position, EffectiveRange);
            }

            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, minRange);

            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, maxRange);
        }
    }
}
