using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Assets.Scripts.Localization
{
    /// <summary>
    /// Localizable interactive image with two states: Normal and Pressed/Activated
    /// Similar to SmoothButtonAnimation but triggered by external events (not mouse hover)
    /// Supports 4 sprites total: 2 languages × 2 states
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LocalizedInteractableImage : LocalizedComponentBase
    {
        [Header("State Sprites - English")]
        [Tooltip("Normal/Idle state sprite - English")]
        public Sprite normalSpriteEnglish;

        [Tooltip("Pressed/Activated state sprite - English")]
        public Sprite pressedSpriteEnglish;

        [Header("State Sprites - Ukrainian")]
        [Tooltip("Normal/Idle state sprite - Ukrainian")]
        public Sprite normalSpriteUkrainian;

        [Tooltip("Pressed/Activated state sprite - Ukrainian")]
        public Sprite pressedSpriteUkrainian;

        [Header("Animation Settings")]
        [Tooltip("Transition duration between states (seconds)")]
        [Range(0.1f, 2f)]
        public float transitionDuration = 0.3f;

        [Tooltip("Easing curve for transitions")]
        public AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("State")]
        [Tooltip("Current state of the image")]
        [SerializeField]
        private bool isPressed = false;

        private Image mainImage;
        private Image overlayImage;
        private Coroutine currentTransition;

        private Sprite currentNormalSprite;
        private Sprite currentPressedSprite;

        void Awake()
        {
            mainImage = GetComponent<Image>();
            mainImage.preserveAspect = true;

            CreateOverlayImage();
        }

        void Start()
        {
            if (SettingsManager.Instance != null)
            {
                UpdateSpritesForLanguage(SettingsManager.Instance.GetCurrentLanguage());
            }

            UpdateImageState(immediate: true);
        }

        private void CreateOverlayImage()
        {
            GameObject overlayObject = new GameObject("StateOverlay");
            overlayObject.transform.SetParent(transform, false);

            overlayImage = overlayObject.AddComponent<Image>();
            overlayImage.preserveAspect = true;
            overlayImage.raycastTarget = false;

            RectTransform overlayRect = overlayImage.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            overlayRect.anchoredPosition = Vector2.zero;

            Color overlayColor = Color.white;
            overlayColor.a = 0f;
            overlayImage.color = overlayColor;
        }

        /// <summary>
        /// Apply localization - update sprites based on selected language
        /// </summary>
        protected override void ApplyLocalization(Language newLanguage)
        {
            UpdateSpritesForLanguage(newLanguage);
            UpdateImageState(immediate: false);

            Debug.Log($"[LocalizedInteractableImage] Localization applied for {newLanguage}");
        }

        private void UpdateSpritesForLanguage(Language language)
        {
            switch (language)
            {
                case Language.Ukrainian:
                    currentNormalSprite = normalSpriteUkrainian != null ? normalSpriteUkrainian : normalSpriteEnglish;
                    currentPressedSprite = pressedSpriteUkrainian != null ? pressedSpriteUkrainian : pressedSpriteEnglish;
                    break;

                case Language.English:
                default:
                    currentNormalSprite = normalSpriteEnglish;
                    currentPressedSprite = pressedSpriteEnglish;
                    break;
            }
        }

        /// <summary>
        /// Set the image to normal state
        /// </summary>
        public void SetNormalState()
        {
            if (isPressed)
            {
                isPressed = false;
                UpdateImageState(immediate: false);
                Debug.Log("[LocalizedInteractableImage] State changed to Normal");
            }
        }

        /// <summary>
        /// Set the image to pressed/activated state
        /// </summary>
        public void SetPressedState()
        {
            if (!isPressed)
            {
                isPressed = true;
                UpdateImageState(immediate: false);
                Debug.Log("[LocalizedInteractableImage] State changed to Pressed");
            }
        }

        /// <summary>
        /// Toggle between normal and pressed states
        /// </summary>
        public void ToggleState()
        {
            isPressed = !isPressed;
            UpdateImageState(immediate: false);
            Debug.Log($"[LocalizedInteractableImage] State toggled to {(isPressed ? "Pressed" : "Normal")}");
        }

        /// <summary>
        /// Get current state
        /// </summary>
        public bool IsPressed()
        {
            return isPressed;
        }

        private void UpdateImageState(bool immediate)
        {
            if (mainImage == null) return;

            Sprite targetSprite = isPressed ? currentPressedSprite : currentNormalSprite;

            if (targetSprite == null)
            {
                Debug.LogWarning($"[LocalizedInteractableImage] Target sprite is null for state: {(isPressed ? "Pressed" : "Normal")}");
                return;
            }

            if (immediate)
            {
                mainImage.sprite = targetSprite;
                if (overlayImage != null)
                {
                    Color color = overlayImage.color;
                    color.a = 0f;
                    overlayImage.color = color;
                }
            }
            else
            {
                if (currentTransition != null)
                {
                    StopCoroutine(currentTransition);
                }
                currentTransition = StartCoroutine(CrossfadeTransition(targetSprite));
            }
        }

        private IEnumerator CrossfadeTransition(Sprite targetSprite)
        {
            if (targetSprite == null || overlayImage == null)
            {
                yield break;
            }

            overlayImage.sprite = targetSprite;

            float elapsed = 0f;
            Color overlayColor = overlayImage.color;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float easedT = easingCurve.Evaluate(t);

                overlayColor.a = easedT;
                overlayImage.color = overlayColor;

                yield return null;
            }

            overlayColor.a = 1f;
            overlayImage.color = overlayColor;

            mainImage.sprite = targetSprite;

            overlayColor.a = 0f;
            overlayImage.color = overlayColor;

            currentTransition = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (currentTransition != null)
            {
                StopCoroutine(currentTransition);
                currentTransition = null;
            }

            if (mainImage != null && currentNormalSprite != null)
            {
                mainImage.sprite = currentNormalSprite;
            }

            if (overlayImage != null)
            {
                Color color = overlayImage.color;
                color.a = 0f;
                overlayImage.color = color;
            }
        }

        void OnDestroy()
        {
            if (currentTransition != null)
            {
                StopCoroutine(currentTransition);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && mainImage != null)
            {
                UpdateImageState(immediate: true);
            }
        }
#endif
    }
}
