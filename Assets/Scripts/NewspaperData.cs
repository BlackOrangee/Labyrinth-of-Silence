using UnityEngine;
using UnityEngine.Serialization;
using Assets.Scripts.Localization;

namespace Assets.Scripts
{
    /// <summary>
    /// ScriptableObject that holds data for a newspaper
    /// </summary>
    [CreateAssetMenu(fileName = "NewspaperData", menuName = "Game/Newspaper Data")]
    public class NewspaperData : ScriptableObject
    {
        [Header("Newspaper Information")]
        [Tooltip("Unique identifier for this newspaper")]
        public string newspaperId;

        [Tooltip("Small icon for inventory display (not localized)")]
        public Sprite inventoryIcon;

        [Header("Images - English")]
        [Tooltip("Full newspaper image to display when reading (English)")]
        public Sprite newspaperImage;

        [Header("Images - Ukrainian")]
        [Tooltip("Full newspaper image to display when reading (Ukrainian)")]
        public Sprite newspaperImageUkr;

        [Header("Localized Content - English")]
        [Tooltip("Display name of the newspaper (English)")]
        public string newspaperName;

        [Tooltip("Title of the newspaper (English)")]
        public string title;

        [FormerlySerializedAs("description")]
        [Tooltip("Content of the newspaper (English)")]
        [TextArea(3, 10)]
        public string content;

        [Header("Localized Content - Ukrainian")]
        [Tooltip("Display name of the newspaper (Ukrainian)")]
        public string newspaperNameUkr;

        [Tooltip("Title of the newspaper (Ukrainian)")]
        public string titleUkr;

        [Tooltip("Content of the newspaper (Ukrainian)")]
        [TextArea(3, 10)]
        public string contentUkr;

        /// <summary>
        /// Get localized newspaper image based on current language settings
        /// </summary>
        public Sprite GetLocalizedImage()
        {
            Language currentLanguage = GetCurrentLanguage();

            switch (currentLanguage)
            {
                case Language.Ukrainian:
                    return newspaperImageUkr != null ? newspaperImageUkr : newspaperImage;
                case Language.English:
                default:
                    return newspaperImage;
            }
        }

        /// <summary>
        /// Get localized newspaper name based on current language settings
        /// </summary>
        public string GetLocalizedName()
        {
            Language currentLanguage = GetCurrentLanguage();

            switch (currentLanguage)
            {
                case Language.Ukrainian:
                    return string.IsNullOrEmpty(newspaperNameUkr) ? newspaperName : newspaperNameUkr;
                case Language.English:
                default:
                    return newspaperName;
            }
        }

        /// <summary>
        /// Get localized title based on current language settings
        /// </summary>
        public string GetLocalizedTitle()
        {
            Language currentLanguage = GetCurrentLanguage();

            switch (currentLanguage)
            {
                case Language.Ukrainian:
                    return string.IsNullOrEmpty(titleUkr) ? title : titleUkr;
                case Language.English:
                default:
                    return title;
            }
        }

        /// <summary>
        /// Get localized content based on current language settings
        /// </summary>
        public string GetLocalizedContent()
        {
            Language currentLanguage = GetCurrentLanguage();

            switch (currentLanguage)
            {
                case Language.Ukrainian:
                    return string.IsNullOrEmpty(contentUkr) ? content : contentUkr;
                case Language.English:
                default:
                    return content;
            }
        }

        /// <summary>
        /// Get current language from SettingsManager
        /// </summary>
        private Language GetCurrentLanguage()
        {
            if (SettingsManager.Instance != null)
            {
                return SettingsManager.Instance.GetCurrentLanguage();
            }

            return Language.English;
        }
    }
}
