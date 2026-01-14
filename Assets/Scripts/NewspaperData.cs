using UnityEngine;

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

        [Tooltip("Display name of the newspaper")]
        public string newspaperName;

        [Tooltip("Full newspaper image to display when reading")]
        public Sprite newspaperImage;

        [Tooltip("Small icon for inventory display")]
        public Sprite inventoryIcon;

        [Header("Optional")]
        [Tooltip("Additional description or content")]
        [TextArea(3, 5)]
        public string description;
    }
}
