using UnityEngine;
using System.Collections;

namespace Assets.Scripts.Localization
{
    /// <summary>
    /// Base class for all localizable components
    /// Handles subscription/unsubscription to language change events
    /// </summary>
    public abstract class LocalizedComponentBase : MonoBehaviour, ILocalizable
    {
        /// <summary>
        /// Subscribe to language change events and initialize with current language
        /// If you override this method, always call base.OnEnable() to maintain subscription logic
        /// </summary>
        protected virtual void OnEnable()
        {
            SettingsManager.OnLanguageChanged += UpdateLocalization;

            if (SettingsManager.Instance != null)
            {
                // Delay by one frame to avoid concurrent FontAsset access
                StartCoroutine(InitializeLocalizationDelayed());
            }
        }

        /// <summary>
        /// Unsubscribe from language change events to prevent memory leaks
        /// If you override this method, always call base.OnDisable() to maintain unsubscription logic
        /// </summary>
        protected virtual void OnDisable()
        {
            SettingsManager.OnLanguageChanged -= UpdateLocalization;
        }

        /// <summary>
        /// Initialize localization with a one-frame delay to prevent threading issues
        /// </summary>
        private IEnumerator InitializeLocalizationDelayed()
        {
            yield return null;
            UpdateLocalization(SettingsManager.Instance.GetCurrentLanguage());
        }

        /// <summary>
        /// Called when the language changes
        /// Implemented by ILocalizable interface
        /// </summary>
        public void UpdateLocalization(Language newLanguage)
        {
            ApplyLocalization(newLanguage);
        }

        /// <summary>
        /// Apply the localized content for the given language
        /// Derived classes must implement this method to handle their specific localization logic
        /// </summary>
        /// <param name="newLanguage">The new language to apply</param>
        protected abstract void ApplyLocalization(Language newLanguage);
    }
}
