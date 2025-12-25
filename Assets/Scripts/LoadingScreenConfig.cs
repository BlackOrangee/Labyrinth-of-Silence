using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Configuration for a specific loading screen
    /// </summary>
    [CreateAssetMenu(fileName = "LoadingScreenConfig", menuName = "Game/Loading Screen Config", order = 1)]
    public class LoadingScreenConfig : ScriptableObject
    {
        [Header("Visual Settings")]
        [Tooltip("Background sprite for this loading screen")]
        public Sprite backgroundSprite;

        [Tooltip("Background color (used if no sprite)")]
        public Color backgroundColor = Color.black;

        [Header("Loading Tip")]
        [Tooltip("Tip text to display during loading")]
        [TextArea(2, 4)]
        public string tipText = "123";

        [Header("Animation")]
        [Tooltip("Animation frames for loading icon (will cycle through)")]
        public Sprite[] animationFrames;

        [Tooltip("Time between animation frames (in seconds)")]
        public float frameRate = 0.2f;
    }
}
