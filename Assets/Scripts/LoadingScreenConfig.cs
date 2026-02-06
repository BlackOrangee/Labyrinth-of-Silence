using UnityEngine;
using Assets.Scripts.Localization;

namespace Assets.Scripts
{
    /// <summary>
    /// Configuration for a specific loading screen
    /// </summary>
    [CreateAssetMenu(fileName = "LoadingScreenConfig", menuName = "Game/Loading Screen Config", order = 1)]
    public class LoadingScreenConfig : ScriptableObject
    {
        [Header("Visual Settings")]
        [Tooltip("Background sprite for this loading screen")]
        public Sprite backgroundSprite;

        [Tooltip("Background color (used if no sprite)")]
        public Color backgroundColor = Color.black;

        [Header("Loading Tip - English")]
        [Tooltip("Tip text to display during loading (English)")]
        [TextArea(2, 4)]
        public string tipText = "123";

        [Header("Loading Tip - Ukrainian")]
        [Tooltip("Tip text to display during loading (Ukrainian)")]
        [TextArea(2, 4)]
        public string tipTextUkr = "";

        [Header("Animation")]
        [Tooltip("Animation frames for loading icon (will cycle through)")]
        public Sprite[] animationFrames;

        [Tooltip("Time between animation frames (in seconds)")]
        public float frameRate = 0.2f;

        /// <summary>
        /// Get the localized tip text based on current language
        /// </summary>
        /// <returns>Localized tip text with fallback to English</returns>
        public string GetLocalizedTipText()
        {
            if (SettingsManager.Instance == null)
            {
                return tipText;
            }

            Language currentLanguage = SettingsManager.Instance.GetCurrentLanguage();

            switch (currentLanguage)
            {
                case Language.Ukrainian:
                    return !string.IsNullOrEmpty(tipTextUkr) ? tipTextUkr : tipText;

                case Language.English:
                default:
                    return tipText;
            }
        }
    }
}
