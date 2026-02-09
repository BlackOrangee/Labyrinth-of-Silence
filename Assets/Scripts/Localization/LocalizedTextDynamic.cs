using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts.Localization
{
    /// <summary>
    /// Localizable text component for dynamic/formatted text (e.g., "Keys: {0}/{1}")
    /// Stores format string variants and provides API for updating values
    /// </summary>
    public class LocalizedTextDynamic : LocalizedComponentBase
    {
        [Header("Localized Format Strings")]
        [Tooltip("Format string for English (use {0}, {1}, etc. for values)")]
        public string englishFormat = "{0}";

        [Tooltip("Format string for Ukrainian (use {0}, {1}, etc. for values)")]
        public string ukrainianFormat = "{0}";

        [Header("Component Settings")]
        [Tooltip("Use TextMeshPro instead of legacy UI Text")]
        public bool useTextMeshPro = true;

        private Text textComponent;
        private TextMeshProUGUI tmpComponent;
        private string currentFormat;
        private object[] lastValues;

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
        /// Apply the localized format string based on the selected language
        /// Re-applies last values if any were previously set
        /// </summary>
        protected override void ApplyLocalization(Language newLanguage)
        {
            currentFormat = GetLocalizedFormat(newLanguage);

            if (lastValues != null && lastValues.Length > 0)
            {
                SetText(lastValues);
            }
        }

        /// <summary>
        /// Sets the formatted text with the provided values
        /// Example: SetText(3, 5) with format "Keys: {0}/{1}" results in "Keys: 3/5"
        /// </summary>
        public void SetText(params object[] values)
        {
            lastValues = values;

            if (string.IsNullOrEmpty(currentFormat))
            {
                currentFormat = GetLocalizedFormat(
                    SettingsManager.Instance != null
                        ? SettingsManager.Instance.GetCurrentLanguage()
                        : Language.English
                );
            }

            string formattedText = values.Length > 0
                ? string.Format(currentFormat, values)
                : currentFormat;

            if (tmpComponent != null)
            {
                tmpComponent.text = formattedText;
            }
            else if (textComponent != null)
            {
                textComponent.text = formattedText;
            }
        }

        /// <summary>
        /// Gets the appropriate format string for the given language
        /// </summary>
        private string GetLocalizedFormat(Language language)
        {
            switch (language)
            {
                case Language.Ukrainian:
                    return string.IsNullOrEmpty(ukrainianFormat) ? englishFormat : ukrainianFormat;
                case Language.English:
                default:
                    return englishFormat;
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
