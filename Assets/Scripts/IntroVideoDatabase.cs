using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Database of intro video slides and configuration
    /// ScriptableObject asset that stores all intro video data
    /// </summary>
    [CreateAssetMenu(fileName = "IntroVideoDatabase", menuName = "Game/Intro Video Database", order = 2)]
    public class IntroVideoDatabase : ScriptableObject
    {
        [Header("Slides")]
        [Tooltip("List of intro video slides (will be played in order)")]
        public List<IntroVideoSlideData> slides = new List<IntroVideoSlideData>();

        [Header("Typewriter Text Configuration")]
        [Tooltip("Characters per second for typewriter effect")]
        [Range(10f, 100f)]
        public float charsPerSecond = 30f;

        [Tooltip("Font for running text (if null, will use default)")]
        public TMPro.TMP_FontAsset textFont;

        [Tooltip("Font size for running text")]
        [Range(20, 100)]
        public int fontSize = 36;

        [Tooltip("Text color")]
        public Color textColor = Color.white;

        [Header("Skip Button Configuration")]
        [Tooltip("Skip button sprites for different languages and states")]
        public IntroSkipButtonData skipButtonData;

        [Tooltip("Fill circle sprite for radial progress indicator")]
        public Sprite fillCircleSprite;

        [Tooltip("Time required to hold skip key for action (seconds)")]
        [Range(0.5f, 3.0f)]
        public float holdDuration = 1.5f;

        [Tooltip("Duration of the crossfade animation (normal → pressed)")]
        [Range(0.05f, 1.0f)]
        public float crossfadeDuration = 0.2f;

        [Tooltip("How long the pressed state is shown before executing the action")]
        [Range(0f, 2.0f)]
        public float pressedHoldDuration = 0.4f;

        [Header("Scene Transition")]
        [Tooltip("Name of the scene to load after intro completes")]
        public string nextSceneName = "Forest";

        [Tooltip("Use scene name instead of build index")]
        public bool useSceneName = true;

        [Tooltip("Build index of the scene to load (if not using scene name)")]
        public int nextSceneIndex = 1;

        /// <summary>
        /// Get a specific slide by index
        /// </summary>
        public IntroVideoSlideData GetSlide(int index)
        {
            if (index < 0 || index >= slides.Count)
            {
                Debug.LogError($"[IntroVideoDatabase] Slide index {index} out of range (0-{slides.Count - 1})");
                return null;
            }
            return slides[index];
        }

        /// <summary>
        /// Get the total number of slides
        /// </summary>
        public int GetSlideCount()
        {
            return slides.Count;
        }

        /// <summary>
        /// Check if a specific slide index is valid
        /// </summary>
        public bool IsValidSlideIndex(int index)
        {
            return index >= 0 && index < slides.Count;
        }

        #region Validation

        /// <summary>
        /// Validate the database configuration
        /// </summary>
        [ContextMenu("Validate Database")]
        public void ValidateDatabase()
        {
            Debug.Log("[IntroVideoDatabase] Starting validation...");

            int errorCount = 0;
            int warningCount = 0;

            if (slides == null || slides.Count == 0)
            {
                Debug.LogError("[IntroVideoDatabase] No slides defined!");
                errorCount++;
            }
            else
            {
                Debug.Log($"[IntroVideoDatabase] Found {slides.Count} slide(s)");

                for (int i = 0; i < slides.Count; i++)
                {
                    IntroVideoSlideData slide = slides[i];
                    if (slide == null)
                    {
                        Debug.LogError($"[IntroVideoDatabase] Slide [{i}] is null!");
                        errorCount++;
                        continue;
                    }

                    if (!slide.IsValid())
                    {
                        Debug.LogError($"[IntroVideoDatabase] Slide [{i}] validation failed: {slide.GetValidationError()}");
                        errorCount++;
                    }
                    else
                    {
                        Debug.Log($"[IntroVideoDatabase] Slide [{i}] ✓ Valid");

                        if (string.IsNullOrEmpty(slide.textUkrainian))
                        {
                            Debug.LogWarning($"[IntroVideoDatabase] Slide [{i}] missing Ukrainian text (will fallback to English)");
                            warningCount++;
                        }
                    }
                }
            }

            if (skipButtonData == null)
            {
                Debug.LogError("[IntroVideoDatabase] Skip button data is null!");
                errorCount++;
            }
            else if (!skipButtonData.IsValid())
            {
                Debug.LogError($"[IntroVideoDatabase] Skip button validation failed: {skipButtonData.GetValidationError()}");
                errorCount++;
            }
            else
            {
                Debug.Log("[IntroVideoDatabase] Skip button data ✓ Valid");
            }

            if (fillCircleSprite == null)
            {
                Debug.LogWarning("[IntroVideoDatabase] Fill circle sprite is missing (skip progress won't be visible)");
                warningCount++;
            }

            if (holdDuration < 0.5f || holdDuration > 3.0f)
            {
                Debug.LogWarning($"[IntroVideoDatabase] Hold duration ({holdDuration}s) is outside recommended range (0.5-3.0s)");
                warningCount++;
            }

            if (useSceneName)
            {
                if (string.IsNullOrEmpty(nextSceneName))
                {
                    Debug.LogError("[IntroVideoDatabase] Next scene name is empty!");
                    errorCount++;
                }
                else
                {
                    Debug.Log($"[IntroVideoDatabase] Next scene: {nextSceneName}");
                }
            }
            else
            {
                if (nextSceneIndex < 0)
                {
                    Debug.LogError($"[IntroVideoDatabase] Next scene index ({nextSceneIndex}) is invalid!");
                    errorCount++;
                }
                else
                {
                    Debug.Log($"[IntroVideoDatabase] Next scene index: {nextSceneIndex}");
                }
            }
        }

        #endregion
    }
}
