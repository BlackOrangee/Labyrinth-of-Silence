using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts
{
    /// <summary>
    /// Manager for handling newspaper reading from inventory
    /// Future integration point for full inventory UI
    /// </summary>
    public class NewspaperManager : MonoBehaviour
    {
        #region Singleton
        private static NewspaperManager _instance;

        public static NewspaperManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<NewspaperManager>();

                    if (_instance == null)
                    {
                        GameObject go = new GameObject("NewspaperManager");
                        _instance = go.AddComponent<NewspaperManager>();
                        Debug.Log("NewspaperManager: Auto-created singleton instance.");
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("NewspaperManager: Duplicate instance detected, destroying.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            Debug.Log("NewspaperManager: Initialized.");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Opens a newspaper from the player's inventory by ID
        /// </summary>
        /// <param name="newspaperId">The unique ID of the newspaper to open</param>
        /// <param name="playerInventory">The player's inventory component</param>
        /// <returns>True if newspaper was found and opened, false otherwise</returns>
        public bool OpenNewspaperFromInventory(string newspaperId, SimpleInventory playerInventory)
        {
            if (string.IsNullOrEmpty(newspaperId))
            {
                Debug.LogWarning("NewspaperManager: Cannot open newspaper - ID is null or empty.");
                return false;
            }

            if (playerInventory == null)
            {
                Debug.LogWarning("NewspaperManager: Cannot open newspaper - inventory is null.");
                return false;
            }

            if (!playerInventory.HasNewspaper(newspaperId))
            {
                Debug.LogWarning($"NewspaperManager: Newspaper '{newspaperId}' not found in inventory.");
                return false;
            }

            NewspaperData newspaperData = playerInventory.GetNewspaperById(newspaperId);

            if (newspaperData == null)
            {
                Debug.LogError($"NewspaperManager: Failed to retrieve newspaper data for ID '{newspaperId}'.");
                return false;
            }

            if (NewspaperUI.Instance != null)
            {
                NewspaperUI.Instance.ShowNewspaper(newspaperData);
                Debug.Log($"NewspaperManager: Opened newspaper '{newspaperData.newspaperName}' from inventory.");
                return true;
            }
            else
            {
                Debug.LogError("NewspaperManager: NewspaperUI instance not found in scene.");
                return false;
            }
        }

        /// <summary>
        /// Opens a newspaper directly by NewspaperData reference
        /// </summary>
        /// <param name="newspaperData">The newspaper data to display</param>
        /// <returns>True if newspaper was opened successfully</returns>
        public bool OpenNewspaper(NewspaperData newspaperData)
        {
            if (newspaperData == null)
            {
                Debug.LogWarning("NewspaperManager: Cannot open newspaper - data is null.");
                return false;
            }

            if (NewspaperUI.Instance != null)
            {
                NewspaperUI.Instance.ShowNewspaper(newspaperData);
                Debug.Log($"NewspaperManager: Opened newspaper '{newspaperData.newspaperName}'.");
                return true;
            }
            else
            {
                Debug.LogError("NewspaperManager: NewspaperUI instance not found in scene.");
                return false;
            }
        }

        /// <summary>
        /// Gets all collected newspapers from player inventory
        /// </summary>
        /// <param name="playerInventory">The player's inventory component</param>
        /// <returns>List of collected newspapers, or empty list if none</returns>
        public List<NewspaperData> GetCollectedNewspapers(SimpleInventory playerInventory)
        {
            if (playerInventory == null)
            {
                Debug.LogWarning("NewspaperManager: Cannot get newspapers - inventory is null.");
                return new List<NewspaperData>();
            }

            return playerInventory.GetCollectedNewspapers();
        }

        /// <summary>
        /// Checks if a newspaper is in the player's inventory
        /// </summary>
        public bool HasNewspaper(string newspaperId, SimpleInventory playerInventory)
        {
            if (playerInventory == null)
                return false;

            return playerInventory.HasNewspaper(newspaperId);
        }
        #endregion

        #region Debug Methods
        [ContextMenu("Debug: Log Newspaper Manager Status")]
        private void DebugLogStatus()
        {
            Debug.Log("=== NewspaperManager Status ===");
            Debug.Log($"Instance exists: {_instance != null}");
            Debug.Log($"NewspaperUI exists: {NewspaperUI.Instance != null}");

            if (NewspaperUI.Instance != null)
            {
                Debug.Log($"NewspaperUI is showing: {NewspaperUI.Instance.IsShowing()}");
            }
        }
        #endregion
    }
}
