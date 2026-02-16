using UnityEngine;

namespace Assets.Scripts.Localization
{
    /// <summary>
    /// Localization data for popup images (hints/prompts)
    /// Popups are visual only (Image sprites), no text components
    /// </summary>
    [System.Serializable]
    public class PopupLocalizationData
    {
        [Header("Identification")]
        [Tooltip("Unique ID for this popup (e.g., 'door_hint', 'lever_prompt')")]
        public string popupID;

        [Header("Popup Image Sprites")]
        [Tooltip("Normal state sprite - English")]
        public Sprite spriteNormalEnglish;

        [Tooltip("Pressed/Activated state sprite - English")]
        public Sprite spritePressedEnglish;

        [Tooltip("Normal state sprite - Ukrainian")]
        public Sprite spriteNormalUkrainian;

        [Tooltip("Pressed/Activated state sprite - Ukrainian")]
        public Sprite spritePressedUkrainian;

        /// <summary>
        /// Get the normal state sprite for the specified language
        /// </summary>
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
        /// Check if this popup data is valid (has at least normal English sprite)
        /// </summary>
        public bool IsValid()
        {
            return spriteNormalEnglish != null;
        }
    }
}
