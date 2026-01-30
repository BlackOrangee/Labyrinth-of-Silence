using UnityEngine;

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

        [Tooltip("Display name of the item")]
        public string itemName;

        [Tooltip("Icon for inventory display")]
        public Sprite inventoryIcon;

        [Header("Classification")]
        [Tooltip("Type of the item")]
        public ItemType itemType = ItemType.General;

        [Header("Optional")]
        [Tooltip("Additional description or content")]
        [TextArea(3, 5)]
        public string description;

        [Tooltip("Can this item be used")]
        public bool isUsable = false;

        [Tooltip("Can multiple instances stack")]
        public bool isStackable = false;

        [Tooltip("Maximum stack size")]
        public int maxStackSize = 1;
    }
}
