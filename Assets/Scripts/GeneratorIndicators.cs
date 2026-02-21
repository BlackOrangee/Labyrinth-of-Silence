using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class GeneratorIndicators : MonoBehaviour
    {
        #region UI References (Посилання на Image компоненти)
        
        [Header("Посилання на індикатори (перетягни з Canvas)")]
        [Tooltip("Image компонент червоного індикатора")]
        public Image redIndicator;
        
        [Tooltip("Image компонент оранжевого індикатора")]
        public Image orangeIndicator;
        
        [Tooltip("Image компонент зеленого індикатора")]
        public Image greenIndicator;
        
        #endregion

        #region Pulse Settings (Налаштування пульсації)
        
        [Header("Налаштування пульсації")]
        [Tooltip("Швидкість пульсації (1.0 = 1 спалах за секунду)")]
        [Range(0.1f, 5f)]
        public float pulseSpeed = 1.0f;
        
        [Tooltip("Мінімальна яскравість при пульсації (0 = повністю темно)")]
        [Range(0f, 0.9f)]
        public float minAlpha = 0.1f;
        
        [Tooltip("Максимальна яскравість при пульсації (1 = повна яскравість)")]
        [Range(0.1f, 1f)]
        public float maxAlpha = 1.0f;
        
        #endregion
        private float currentPulse = 0f;
        private IndicatorState currentState = IndicatorState.Idle;
        public enum IndicatorState
        {
            Idle,
            Waiting,
            Success,
            Failed
        }
        void Start()
        {
            SetState(IndicatorState.Idle);
        }
        void Update()
        {
            if (currentState == IndicatorState.Idle) return;

            currentPulse = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) / 2f;

            float alpha = Mathf.Lerp(minAlpha, maxAlpha, currentPulse);

            switch (currentState)
            {
                case IndicatorState.Waiting:
                    SetAlpha(orangeIndicator, alpha);
                    break;
                case IndicatorState.Success:
                    SetAlpha(greenIndicator, alpha);
                    break;
                case IndicatorState.Failed:
                    SetAlpha(redIndicator, alpha);
                    break;
            }
        }
        public void SetState(IndicatorState newState)
        {
            currentState = newState;

            if (redIndicator) redIndicator.gameObject.SetActive(false);
            if (orangeIndicator) orangeIndicator.gameObject.SetActive(false);
            if (greenIndicator) greenIndicator.gameObject.SetActive(false);

            switch (newState)
            {
                case IndicatorState.Waiting:
                    if (orangeIndicator) orangeIndicator.gameObject.SetActive(true);
                    break;
                case IndicatorState.Success:
                    if (greenIndicator) greenIndicator.gameObject.SetActive(true);
                    break;
                case IndicatorState.Failed:
                    if (redIndicator) redIndicator.gameObject.SetActive(true);
                    break;
                case IndicatorState.Idle:
                    break;
            }
        }
        private void SetAlpha(Image img, float alpha)
        {
            if (img == null) return;
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }
}