using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public class GameHUDManager : MonoBehaviour
    {
        public static GameHUDManager Instance;

        [Header("MIND UI")]
        public Image mindRingImage; 
        public Image mindIconImage; 
        public Image mindColorFill; 
        public TextMeshProUGUI mindText;
        
        [Header("Mind Settings")]
        [Tooltip("Список картинок Мозку")]
        public List<Sprite> mindRingSprites; 
        public List<Sprite> mindIconSprites;
        [Range(0f, 1f)] public float mindCriticalThreshold = 0.3f;

        [Header("FUEL UI")]
        public Image fuelRingImage; 
        public Image fuelIconImage; 
        public Image fuelColorFill; 
        public TextMeshProUGUI fuelText;

        [Header("Fuel Settings")]
        [Tooltip("Список картинок Лампи")]
        public List<Sprite> fuelRingSprites; 
        public List<Sprite> fuelIconSprites;
        [Range(0f, 1f)] public float fuelCriticalThreshold = 0.2f;

        [Header("Global Text Colors")]
        public Color normalTextColor = Color.white;
        public Color criticalTextColor = Color.red;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void UpdateMindUI(float current, float max)
        {
            float percentage = Mathf.Clamp01(current / max);

            UpdateText(mindText, percentage, mindCriticalThreshold);
            
            UpdateSprite(mindRingImage, percentage, mindRingSprites);
            UpdateSprite(mindIconImage, percentage, mindIconSprites);

            if (mindColorFill != null) mindColorFill.fillAmount = percentage;
        }

        public void UpdateFuelUI(float current, float max)
        {
            float percentage = Mathf.Clamp01(current / max);

            UpdateText(fuelText, percentage, fuelCriticalThreshold);

            UpdateSprite(fuelRingImage, percentage, fuelRingSprites);
            UpdateSprite(fuelIconImage, percentage, fuelIconSprites);

            if (fuelColorFill != null) fuelColorFill.fillAmount = percentage;
        }

        private void UpdateText(TextMeshProUGUI textMesh, float percentage, float threshold)
        {
            if (textMesh != null)
            {
                textMesh.text = $"{Mathf.RoundToInt(percentage * 100)}%";

                if (percentage <= threshold)
                {
                    textMesh.color = criticalTextColor;
                }
                else
                {
                    textMesh.color = normalTextColor;
                }
            }
        }

        private void UpdateSprite(Image targetImage, float percentage, List<Sprite> sprites)
        {
            if (targetImage != null && sprites != null && sprites.Count > 0)
            {
                int spriteIndex = Mathf.Clamp(Mathf.FloorToInt(percentage * (sprites.Count)), 0, sprites.Count - 1);
                targetImage.sprite = sprites[spriteIndex];
            }
        }
    }
}