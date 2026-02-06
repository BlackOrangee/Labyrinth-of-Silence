using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts.Localization
{
    /// <summary>
    /// Localizable text component that works with both Unity UI Text and TextMeshProUGUI
    /// Stores text variants for each language and automatically updates on language change
    /// </summary>
    public class LocalizedText : LocalizedComponentBase
    {
        [Header("Localized Text Variants")]
        [TextArea(2, 5)]
        [Tooltip("Text to display when English is selected")]
        public string englishText;

        [TextArea(2, 5)]
        [Tooltip("Text to display when Ukrainian is selected")]
        public string ukrainianText;

        [Header("Component Settings")]
        [Tooltip("Use TextMeshPro instead of legacy UI Text")]
        public bool useTextMeshPro = true;

        private Text textComponent;
        private TextMeshProUGUI tmpComponent;

        private void Awake()
        {
            if (useTextMeshPro)
            {
                tmpComponent = GetComponent<TextMeshProUGUI>();
            }
            else
            {
                textComponent = GetComponent<Text>();
            }
        }

        /// <summary>
        /// Apply the localized text based on the selected language
        /// </summary>
        protected override void ApplyLocalization(Language newLanguage)
        {
            string localizedText = GetLocalizedText(newLanguage);

            if (tmpComponent != null)
            {
                tmpComponent.text = localizedText;
            }
            else if (textComponent != null)
            {
                textComponent.text = localizedText;
            }
        }

        /// <summary>
        /// Gets the appropriate text for the given language
        /// </summary>
        private string GetLocalizedText(Language language)
        {
            switch (language)
            {
                case Language.Ukrainian:
                    return string.IsNullOrEmpty(ukrainianText) ? englishText : ukrainianText;
                case Language.English:
                default:
                    return englishText;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (GetComponent<TextMeshProUGUI>() != null)
            {
                useTextMeshPro = true;
            }
            else if (GetComponent<Text>() != null)
            {
                useTextMeshPro = false;
            }
        }
#endif
    }
}
