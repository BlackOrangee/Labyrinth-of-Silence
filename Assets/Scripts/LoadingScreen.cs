using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Localization;

namespace Assets.Scripts
{
    /// <summary>
    /// Controls the loading screen UI and displays loading progress
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Background image")]
        public Image backgroundImage;

        [Tooltip("Loading tip text")]
        public Text tipText;

        [Tooltip("Loading icon for frame animation")]
        public Image loadingIcon;

        [Header("Animation")]
        [Tooltip("Fade in duration")]
        public float fadeInDuration = 0.5f;

        [Tooltip("Fade out duration")]
        public float fadeOutDuration = 0.5f;

        private CanvasGroup canvasGroup;
        private bool isFadingOut = false;
        private Sprite[] currentAnimationFrames;
        private float currentFrameRate = 0.2f;
        private int currentFrame = 0;
        private float frameTimer = 0f;
        private LoadingScreenConfig currentConfig;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            StartCoroutine(FadeIn());
        }

        private void OnEnable()
        {
            SettingsManager.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            SettingsManager.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(Language newLanguage)
        {
            UpdateLocalizedText();
        }

        private void UpdateLocalizedText()
        {
            if (currentConfig != null && tipText != null)
            {
                tipText.text = currentConfig.GetLocalizedTipText();
            }
        }


        private void Update()
        {
            if (loadingIcon != null && currentAnimationFrames != null && currentAnimationFrames.Length > 0)
            {
                frameTimer += Time.unscaledDeltaTime;
                if (frameTimer >= currentFrameRate)
                {
                    frameTimer = 0f;
                    currentFrame = (currentFrame + 1) % currentAnimationFrames.Length;
                    loadingIcon.sprite = currentAnimationFrames[currentFrame];

                }
            }

            if (SceneLoader.Instance != null)
            {
                float progress = SceneLoader.Instance.GetLoadingProgress();

                if (progress >= 1f && !isFadingOut)
                {
                    isFadingOut = true;
                    StartCoroutine(FadeOut());
                }
            }
        }

        /// <summary>
        /// Apply configuration to the loading screen
        /// </summary>
        public void ApplyConfiguration(LoadingScreenConfig config)
        {
            if (config == null)
            {
                return;
            }

            currentConfig = config;

            if (backgroundImage != null)
            {
                if (config.backgroundSprite != null)
                {
                    backgroundImage.sprite = config.backgroundSprite;
                    backgroundImage.color = Color.white;
                }
                else
                {
                    backgroundImage.sprite = null;
                    backgroundImage.color = config.backgroundColor;
                }
            }

            if (tipText != null)
            {
                tipText.text = config.GetLocalizedTipText();
            }

            if (config.animationFrames != null && config.animationFrames.Length > 0)
            {
                currentAnimationFrames = config.animationFrames;
                currentFrameRate = config.frameRate;
                currentFrame = 0;
                frameTimer = 0f;

                if (loadingIcon != null)
                {
                    loadingIcon.sprite = currentAnimationFrames[0];
                }
            }
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            canvasGroup.alpha = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }
    }
}
