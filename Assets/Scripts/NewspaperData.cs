using UnityEngine;
using UnityEngine.Serialization;

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

        [Tooltip("Title of the newspaper")]
        public string title;

        [FormerlySerializedAs("description")]
        [Tooltip("Description of the newspaper")]
        [TextArea(30, 5)]
        public string content;
    }
}
