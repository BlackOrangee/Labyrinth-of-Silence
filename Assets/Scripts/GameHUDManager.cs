using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts
{
    public class GameHUDManager : MonoBehaviour
    {
        public static GameHUDManager Instance;

        [Header("Mind (Sanity) UI")]
        public Image mindFillImage; 
        public TextMeshProUGUI mindText;

        [Header("Fuel (Lamp) UI")]
        public Image fuelFillImage; 
        public TextMeshProUGUI fuelText;

        [Header("Text Settings")]
        public Color normalTextColor = Color.white;
        public Color criticalTextColor = Color.red;
        [Range(0f, 1f)]
        public float criticalThreshold = 0.2f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void UpdateFuelUI(float current, float max)
        {
            float percentage = Mathf.Clamp01(current / max);
            UpdateStat(fuelFillImage, fuelText, percentage);
        }

        public void UpdateMindUI(float current, float max)
        {
            float percentage = Mathf.Clamp01(current / max);
            UpdateStat(mindFillImage, mindText, percentage);
        }

        private void UpdateStat(Image fillImage, TextMeshProUGUI textMesh, float percentage)
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = percentage;
            }

            if (textMesh != null)
            {
                textMesh.text = $"{Mathf.RoundToInt(percentage * 100)}%";

                if (percentage <= criticalThreshold)
                {
                    textMesh.color = criticalTextColor;
                }
                else
                {
                    textMesh.color = normalTextColor;
                }
            }
        }
    }
}