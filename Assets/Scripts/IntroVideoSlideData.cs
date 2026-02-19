using UnityEngine;
using UnityEngine.Video;
using Assets.Scripts.Localization;

namespace Assets.Scripts
{
    /// <summary>
    /// Data for a single intro slide
    /// Each slide consists of:
    /// - 1 background video (loops continuously, shared for all languages)
    /// - Running text (typewriter effect, language-specific: English/Ukrainian)
    /// </summary>
    [System.Serializable]
    public class IntroVideoSlideData
    {
        [Header("Background Video")]
        [Tooltip("Background video that loops continuously (shared for all languages)")]
        public VideoClip backgroundVideo;

        [Header("Running Text (Typewriter Effect)")]
        [Tooltip("Text for English - will be displayed with typewriter effect")]
        [TextArea(3, 10)]
        public string textEnglish;

        [Tooltip("Text for Ukrainian - will be displayed with typewriter effect")]
        [TextArea(3, 10)]
        public string textUkrainian;

        [Header("Second Running Text (Optional)")]
        [Tooltip("Enable a second independent running text component on this slide")]
        public bool hasSecondText = false;

        [Tooltip("Second text for English")]
        [TextArea(3, 10)]
        public string secondTextEnglish;

        [Tooltip("Second text for Ukrainian")]
        [TextArea(3, 10)]
        public string secondTextUkrainian;

        /// <summary>
        /// Get the primary text for the specified language
        /// </summary>
        public string GetText(Language language)
        {
            switch (language)
            {
                case Language.Ukrainian:
                    return !string.IsNullOrEmpty(textUkrainian) ? textUkrainian : textEnglish;
                case Language.English:
                default:
                    return textEnglish;
            }
        }

        /// <summary>
        /// Get the second text for the specified language
        /// </summary>
        public string GetSecondText(Language language)
        {
            switch (language)
            {
                case Language.Ukrainian:
                    return !string.IsNullOrEmpty(secondTextUkrainian) ? secondTextUkrainian : secondTextEnglish;
                case Language.English:
                default:
                    return secondTextEnglish;
            }
        }

        /// <summary>
        /// Check if this slide data is valid (has required data)
        /// </summary>
        public bool IsValid()
        {
            return backgroundVideo != null && !string.IsNullOrEmpty(textEnglish);
        }

        /// <summary>
        /// Get validation error message for debugging
        /// </summary>
        public string GetValidationError()
        {
            if (backgroundVideo == null)
                return "Background video is missing";
            if (string.IsNullOrEmpty(textEnglish))
                return "English text is missing";
            return "Valid";
        }
    }
}
