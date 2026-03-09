using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class GeneratorIndicators : MonoBehaviour
    {
        public enum IndicatorState { Idle, Waiting, Success, Failed }
        [SerializeField] private IndicatorState currentState = IndicatorState.Idle;

        [Header("UI Images (Картинки лампочок)")]
        public Image redIndicator;
        public Image orangeIndicator;
        public Image greenIndicator;

        [Header("Physical Lights (Реальне світло - Опціонально)")]
        public Light redLight;
        public Light orangeLight;
        public Light greenLight;

        [Header("HDR Colors (Колір УВІМКНЕНОЇ лампочки)")]
        [ColorUsage(true, true)] public Color redGlowColor = Color.red * 2f;
        [ColorUsage(true, true)] public Color orangeGlowColor = new Color(1f, 0.5f, 0f) * 2f;
        [ColorUsage(true, true)] public Color greenGlowColor = Color.green * 2f;

        [Header("Off Colors (Колір ВИМКНЕНОЇ лампочки)")] // НОВИЙ БЛОК!
        public Color redOffColor = new Color(0.3f, 0f, 0f, 1f);       // Темно-бордовий
        public Color orangeOffColor = new Color(0.3f, 0.15f, 0f, 1f); // Темно-коричневий
        public Color greenOffColor = new Color(0f, 0.3f, 0f, 1f);     // Темно-зелений

        [Header("Налаштування пульсації та світла")]
        [Range(0.1f, 10f)] public float pulseSpeed = 3.0f;
        [Range(0f, 1f)] public float minIntensity = 0.2f;
        [Range(0f, 1f)] public float maxIntensity = 1.0f;
        [Range(0f, 5f)] public float lightMultiplier = 0.5f;

        void Start()
        {
            SetAllOff();
            SetState(currentState);
        }

        void Update()
        {
            if (currentState == IndicatorState.Idle) return;

            float wave = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) / 2f;
            float currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, wave);

            switch (currentState)
            {
                case IndicatorState.Waiting:
                    UpdateIndicator(orangeIndicator, orangeLight, orangeOffColor, orangeGlowColor, currentIntensity);
                    break;
                case IndicatorState.Success:
                    UpdateIndicator(greenIndicator, greenLight, greenOffColor, greenGlowColor, currentIntensity);
                    break;
                case IndicatorState.Failed:
                    UpdateIndicator(redIndicator, redLight, redOffColor, redGlowColor, currentIntensity);
                    break;
            }
        }

        public void SetState(IndicatorState newState)
        {
            currentState = newState;
            SetAllOff(); 
        }

        // Оновлено: тепер функція приймає індивідуальний offColor
        private void UpdateIndicator(Image img, Light pLight, Color offColor, Color hdrColor, float intensity)
        {
            if (img != null)
            {
                img.color = Color.Lerp(offColor, hdrColor, intensity);
            }

            if (pLight != null)
            {
                pLight.intensity = intensity * lightMultiplier; 
            }
        }

        private void SetAllOff()
        {
            if (redIndicator) redIndicator.color = redOffColor;
            if (orangeIndicator) orangeIndicator.color = orangeOffColor;
            if (greenIndicator) greenIndicator.color = greenOffColor;

            if (redLight) redLight.intensity = 0f;
            if (orangeLight) orangeLight.intensity = 0f;
            if (greenLight) greenLight.intensity = 0f;
        }
    }
}