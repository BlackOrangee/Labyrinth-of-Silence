using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Localization
{
    /// <summary>
    /// Centralized database for all popup hint localizations
    /// Popups are visual Image sprites with text baked into the image
    /// </summary>
    [CreateAssetMenu(fileName = "PopupLocalizationDB", menuName = "Localization/Popup Database", order = 1)]
    public class LocalizationDatabase : ScriptableObject
    {
        [Header("Popup Image Data")]
        [Tooltip("List of all localized popup images (hints/prompts)")]
        public List<PopupLocalizationData> popups = new List<PopupLocalizationData>();

        private Dictionary<string, PopupLocalizationData> popupDictionary;

        /// <summary>
        /// Initialize the database (build lookup dictionary)
        /// </summary>
        public void Initialize()
        {
            popupDictionary = new Dictionary<string, PopupLocalizationData>();

            foreach (var popup in popups)
            {
                if (!string.IsNullOrEmpty(popup.popupID))
                {
                    if (popupDictionary.ContainsKey(popup.popupID))
                    {
                        Debug.LogWarning($"[LocalizationDatabase] Duplicate popup ID found: {popup.popupID}");
                    }
                    else
                    {
                        popupDictionary[popup.popupID] = popup;
                    }
                }
            }

            Debug.Log($"[LocalizationDatabase] Initialized with {popupDictionary.Count} popups");
        }

        /// <summary>
        /// Get popup data by ID
        /// </summary>
        public PopupLocalizationData GetPopupData(string popupID)
        {
            if (popupDictionary == null || popupDictionary.Count == 0)
            {
                Initialize();
            }

            if (popupDictionary.TryGetValue(popupID, out PopupLocalizationData data))
            {
                return data;
            }

            Debug.LogWarning($"[LocalizationDatabase] Popup ID not found: {popupID}");
            return null;
        }

        /// <summary>
        /// Get normal sprite by ID and language
        /// </summary>
        public Sprite GetNormalSprite(string popupID, Language language)
        {
            var data = GetPopupData(popupID);
            return data?.GetNormalSprite(language);
        }

        /// <summary>
        /// Get pressed sprite by ID and language
        /// </summary>
        public Sprite GetPressedSprite(string popupID, Language language)
        {
            var data = GetPopupData(popupID);
            return data?.GetPressedSprite(language);
        }

        /// <summary>
        /// Check if popup ID exists in database
        /// </summary>
        public bool HasPopup(string popupID)
        {
            if (popupDictionary == null || popupDictionary.Count == 0)
            {
                Initialize();
            }

            return popupDictionary.ContainsKey(popupID);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Validate the database (check for duplicates, missing data)
        /// </summary>
        [ContextMenu("Validate Database")]
        public void ValidateDatabase()
        {
            var duplicates = popups.GroupBy(p => p.popupID)
                                  .Where(g => g.Count() > 1)
                                  .Select(g => g.Key);

            foreach (var duplicate in duplicates)
            {
                Debug.LogError($"[LocalizationDatabase] Duplicate ID: {duplicate}");
            }

            var missingSprites = popups.Where(p => !p.IsValid())
                                       .Select(p => p.popupID);

            foreach (var missing in missingSprites)
            {
                Debug.LogWarning($"[LocalizationDatabase] Missing normal English sprite for ID: {missing}");
            }

            Debug.Log($"[LocalizationDatabase] Validation complete. Total popups: {popups.Count}");
        }
#endif
    }
}
