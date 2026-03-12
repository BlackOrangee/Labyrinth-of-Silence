using UnityEngine;
using Assets.Scripts.Localization;

namespace Assets.Scripts
{
    public enum ItemType
    {
        Key,
        Document,
        General,
        Quest
    }

    /// <summary>
    /// ScriptableObject that holds data for general inventory items
    /// </summary>
    [CreateAssetMenu(fileName = "ItemData", menuName = "Game/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Item Information")]
        [Tooltip("Unique identifier for this item")]
        public string itemId;

        [Tooltip("Display name (English)")]
        public string itemName;

        [Tooltip("Display name (Ukrainian)")]
        public string itemNameUA;

        [Tooltip("Icon for inventory display")]
        public Sprite inventoryIcon;

        [Header("Classification")]
        [Tooltip("Type of the item")]
        public ItemType itemType = ItemType.General;

        [Header("Optional")]
        [Tooltip("Description (English)")]
        [TextArea(3, 5)]
        public string description;

        [Tooltip("Description (Ukrainian)")]
        [TextArea(3, 5)]
        public string descriptionUA;

        [Tooltip("Can this item be used")]
        public bool isUsable = false;

        [Tooltip("Can multiple instances stack")]
        public bool isStackable = false;

        [Tooltip("Maximum stack size")]
        public int maxStackSize = 1;

        public string GetLocalizedName()
        {
            bool isUA = SettingsManager.Instance != null &&
                        SettingsManager.Instance.GetCurrentLanguage() == Language.Ukrainian;
            return isUA && !string.IsNullOrEmpty(itemNameUA) ? itemNameUA : itemName;
        }

        public string GetLocalizedDescription()
        {
            bool isUA = SettingsManager.Instance != null &&
                        SettingsManager.Instance.GetCurrentLanguage() == Language.Ukrainian;
            return isUA && !string.IsNullOrEmpty(descriptionUA) ? descriptionUA : description;
        }
    }
}
