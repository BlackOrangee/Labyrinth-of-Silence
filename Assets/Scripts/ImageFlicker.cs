using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    /// <summary>
    /// Smoothly oscillates the alpha of a UI Graphic (Image or RawImage) between two configurable limits
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class ImageFlicker : MonoBehaviour
    {
        [Header("Alpha Range")]
        [Range(0f, 1f)]
        [Tooltip("Minimum alpha value")]
        public float minAlpha = 0.2f;

        [Range(0f, 1f)]
        [Tooltip("Maximum alpha value")]
        public float maxAlpha = 1f;

        [Header("Speed")]
        [Range(0.1f, 10f)]
        [Tooltip("Flicker speed (oscillations per second)")]
        public float speed = 1f;

        private Graphic graphic;

        private void Awake()
        {
            graphic = GetComponent<Graphic>();
        }

        private void Update()
        {
            float t = (Mathf.Sin(Time.time * speed * Mathf.PI * 2f) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

            Color c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }
    }
}
