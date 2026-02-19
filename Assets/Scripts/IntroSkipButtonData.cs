using UnityEngine;
using Assets.Scripts.Localization;

namespace Assets.Scripts
{
    /// <summary>
    /// Localization data for intro skip button
    /// Similar to PopupLocalizationData but specific to intro video skip functionality
    /// </summary>
    [System.Serializable]
    public class IntroSkipButtonData
    {
        [Header("Normal State Sprites")]
        [Tooltip("Normal state sprite - English")]
        public Sprite spriteNormalEnglish;

        [Tooltip("Normal state sprite - Ukrainian")]
        public Sprite spriteNormalUkrainian;

        [Header("Pressed State Sprites")]
        [Tooltip("Pressed state sprite - English (when holding to skip)")]
        public Sprite spritePressedEnglish;

        [Tooltip("Pressed state sprite - Ukrainian (when holding to skip)")]
        public Sprite spritePressedUkrainian;

        /// <summary>
        /// Get the normal state sprite for the specified language
        /// </summary>
        /// <param name="language">Language to get sprite for</param>
        /// <returns>Normal sprite for the specified language, with fallback to English</returns>
        public Sprite GetNormalSprite(Language language)
        {
            switch (language)
            {
                case Language.Ukrainian:
                    return spriteNormalUkrainian != null ? spriteNormalUkrainian : spriteNormalEnglish;
                case Language.English:
                default:
                    return spriteNormalEnglish;
            }
        }

        /// <summary>
        /// Get the pressed state sprite for the specified language
        /// </summary>
        /// <param name="language">Language to get sprite for</param>
        /// <returns>Pressed sprite for the specified language, with fallback to English</returns>
        public Sprite GetPressedSprite(Language language)
        {
            switch (language)
            {
                case Language.Ukrainian:
                    return spritePressedUkrainian != null ? spritePressedUkrainian : spritePressedEnglish;
                case Language.English:
                default:
                    return spritePressedEnglish;
            }
        }

        /// <summary>
        /// Check if this button data is valid (has at least normal English sprite)
        /// </summary>
        public bool IsValid()
        {
            return spriteNormalEnglish != null;
        }

        /// <summary>
        /// Get validation error message for debugging
        /// </summary>
        public string GetValidationError()
        {
            if (spriteNormalEnglish == null)
                return "Normal English sprite is missing";
            if (spritePressedEnglish == null)
                return "Pressed English sprite is missing (recommended)";
            return "Valid";
        }
    }
}
