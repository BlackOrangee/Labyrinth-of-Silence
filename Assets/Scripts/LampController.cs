using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts
{
    public class LampController : MonoBehaviour
    {
        [Header("References")]
        public Light lampLight;
        public Light fillLight;
        public ParticleSystem fireEffect;

        [Header("Old UI (Optional)")]
        public Slider fuelSlider;
        public TextMeshProUGUI fuelPercentageText;

        [Header("Settings")]
        public float maxFuel = 100f;

        [Header("Fading & UI Settings")]
        [Range(0f, 1f)]
        public float fadeThreshold = 0.2f;

        public Color normalTextColor = Color.green;
        public Color warningTextColor = Color.red;

        [Header("Main Light Intensity")]
        public float dimIntensity = 3.0f;
        public float brightIntensity = 5.0f;
        public float dimRange = 8f;
        public float brightRange = 10f;

        [Header("Fill Light Intensity (Soft Glow)")]
        public float fillDimIntensity = 0.5f;
        public float fillBrightIntensity = 1.0f;

        [Header("Fuel Consumption")]
        public float dimBurnRate = 2f;
        public float brightBurnRate = 10f;

        private float currentFuel;
        private int lightMode = 0;

        private void Start()
        {
            currentFuel = maxFuel;

            if (fuelSlider != null)
            {
                fuelSlider.maxValue = maxFuel;
                fuelSlider.value = currentFuel;
            }

            UpdateLightState();
            UpdateUI();
        }

        private void Update()
        {
            HandleInput();
            ConsumeFuel();
            UpdateUI();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                lightMode++;
                if (lightMode > 2) lightMode = 0;
                UpdateLightState();
            }
        }

        private void ConsumeFuel()
        {
            if (lightMode == 0) return;

            float burnRate = (lightMode == 2) ? brightBurnRate : dimBurnRate;

            if (currentFuel > 0)
            {
                currentFuel -= burnRate * Time.deltaTime;
                UpdateLightState();
            }
            else
            {
                currentFuel = 0;
                lightMode = 0;
                UpdateLightState();
                Debug.Log("Fuel empty!");
            }
        }

        private void UpdateLightState()
        {
            bool isLightOn = (lightMode != 0);

            float fadeFactor = 1.0f;
            float thresholdValue = maxFuel * fadeThreshold;

            if (currentFuel < thresholdValue && currentFuel > 0)
            {
                fadeFactor = currentFuel / thresholdValue;
            }

            if (lampLight != null)
            {
                lampLight.enabled = isLightOn;
                if (isLightOn)
                {
                    float targetIntensity = (lightMode == 2) ? brightIntensity : dimIntensity;
                    float targetRange = (lightMode == 2) ? brightRange : dimRange;

                    lampLight.intensity = targetIntensity * fadeFactor;
                    lampLight.range = targetRange;
                }
            }

            if (fillLight != null)
            {
                fillLight.enabled = isLightOn;
                if (isLightOn)
                {
                    float targetFillIntensity = (lightMode == 2) ? fillBrightIntensity : fillDimIntensity;
                    fillLight.intensity = targetFillIntensity * fadeFactor;
                }
            }

            if (fireEffect != null)
            {
                if (!isLightOn)
                {
                    fireEffect.Stop();
                    fireEffect.Clear();
                }
                else
                {
                    if (!fireEffect.isPlaying) fireEffect.Play();
                    var main = fireEffect.main;
                    float targetSize = (lightMode == 2) ? 1.0f : 0.6f;
                    main.startSizeMultiplier = targetSize * fadeFactor;
                }
            }
        }

        private void UpdateUI()
        {
            if (fuelSlider != null)
            {
                fuelSlider.value = currentFuel;
            }

            if (fuelPercentageText != null)
            {
                float fraction = currentFuel / maxFuel;
                int percent = Mathf.RoundToInt(fraction * 100f);

                fuelPercentageText.text = $"{percent}%";

                if (fraction <= fadeThreshold)
                {
                    fuelPercentageText.color = warningTextColor;
                }
                else
                {
                    fuelPercentageText.color = normalTextColor;
                }
            }

            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.UpdateFuelUI(currentFuel, maxFuel);
            }
        }

        public void Refuel(float amount)
        {
            currentFuel += amount;
            if (currentFuel > maxFuel) currentFuel = maxFuel;

            UpdateUI();
            UpdateLightState();
        }

        public float GetCurrentFuel() => currentFuel;
        public float GetMaxFuel() => maxFuel;

        public bool IsLightOn()
        {
            return lightMode != 0 && currentFuel > 0;
        }
    }
}