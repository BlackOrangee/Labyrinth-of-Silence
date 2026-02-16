using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace Assets.Scripts.Localization
{
    /// <summary>
    /// Localized popup component for visual popups (Image-based hints/prompts)
    /// Shows/hides popup images with localized sprites and state management
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LocalizedPopup : LocalizedComponentBase
    {
        [Header("Localization")]
        [Tooltip("Reference to the localization database")]
        public LocalizationDatabase localizationDatabase;

        [Tooltip("Popup ID to use from the database")]
        public string popupID;

        [Header("Animation")]
        [Tooltip("Fade in/out duration")]
        [Range(0.1f, 2f)]
        public float fadeDuration = 0.3f;

        [Tooltip("Auto-hide after showing (0 = manual hide only)")]
        [Range(0f, 10f)]
        public float autoHideDelay = 0f;

        [Header("State")]
        [SerializeField]
        private bool isPressed = false;

        private Image popupImage;
        private CanvasGroup canvasGroup;
        private Coroutine fadeCoroutine;
        private Coroutine autoHideCoroutine;

        private Sprite currentNormalSprite;
        private Sprite currentPressedSprite;

        void Awake()
        {
            popupImage = GetComponent<Image>();

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);

            if (localizationDatabase != null)
            {
                localizationDatabase.Initialize();
            }
        }

        void Start()
        {
            if (SettingsManager.Instance != null)
            {
                UpdateSpritesForLanguage(SettingsManager.Instance.GetCurrentLanguage());
            }
        }

        /// <summary>
        /// Apply localization - update sprites based on selected language
        /// </summary>
        protected override void ApplyLocalization(Language newLanguage)
        {
            UpdateSpritesForLanguage(newLanguage);

            if (gameObject.activeSelf && popupImage != null)
            {
                popupImage.sprite = isPressed ? currentPressedSprite : currentNormalSprite;
            }

            Debug.Log($"[LocalizedPopup] Localization applied for {newLanguage}, ID: {popupID}");
        }

        private void UpdateSpritesForLanguage(Language language)
        {
            if (localizationDatabase == null || string.IsNullOrEmpty(popupID))
            {
                return;
            }

            var data = localizationDatabase.GetPopupData(popupID);
            if (data != null)
            {
                currentNormalSprite = data.GetNormalSprite(language);
                currentPressedSprite = data.GetPressedSprite(language);
            }
            else
            {
                Debug.LogWarning($"[LocalizedPopup] Popup data not found for ID: {popupID}");
            }
        }

        /// <summary>
        /// Show the popup in normal state
        /// </summary>
        public void ShowPopup()
        {
            ShowPopup(false);
        }

        /// <summary>
        /// Show the popup in specified state
        /// </summary>
        public void ShowPopup(bool pressedState)
        {
            if (localizationDatabase == null || string.IsNullOrEmpty(popupID))
            {
                Debug.LogError("[LocalizedPopup] Cannot show popup - missing database or ID!");
                return;
            }

            isPressed = pressedState;

            Sprite targetSprite = isPressed ? currentPressedSprite : currentNormalSprite;
            if (targetSprite != null && popupImage != null)
            {
                popupImage.sprite = targetSprite;
            }

            gameObject.SetActive(true);
            FadeIn();

            if (autoHideDelay > 0f)
            {
                if (autoHideCoroutine != null)
                {
                    StopCoroutine(autoHideCoroutine);
                }
                autoHideCoroutine = StartCoroutine(AutoHideRoutine());
            }

            Debug.Log($"[LocalizedPopup] Showing popup: {popupID} (state: {(isPressed ? "pressed" : "normal")})");
        }

        /// <summary>
        /// Hide the popup
        /// </summary>
        public void HidePopup()
        {
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
                autoHideCoroutine = null;
            }

            FadeOut();
            Debug.Log($"[LocalizedPopup] Hiding popup: {popupID}");
        }

        /// <summary>
        /// Set popup to pressed state (if already visible)
        /// </summary>
        public void SetPressedState()
        {
            if (gameObject.activeSelf && !isPressed)
            {
                isPressed = true;
                if (currentPressedSprite != null && popupImage != null)
                {
                    popupImage.sprite = currentPressedSprite;
                }
                Debug.Log($"[LocalizedPopup] State changed to pressed: {popupID}");
            }
        }

        /// <summary>
        /// Set popup to normal state (if already visible)
        /// </summary>
        public void SetNormalState()
        {
            if (gameObject.activeSelf && isPressed)
            {
                isPressed = false;
                if (currentNormalSprite != null && popupImage != null)
                {
                    popupImage.sprite = currentNormalSprite;
                }
                Debug.Log($"[LocalizedPopup] State changed to normal: {popupID}");
            }
        }

        /// <summary>
        /// Set the popup ID dynamically
        /// </summary>
        public void SetPopupID(string newPopupID)
        {
            popupID = newPopupID;

            if (SettingsManager.Instance != null)
            {
                UpdateSpritesForLanguage(SettingsManager.Instance.GetCurrentLanguage());
            }

            Debug.Log($"[LocalizedPopup] Popup ID changed to: {popupID}");
        }

        /// <summary>
        /// Check if popup is currently visible
        /// </summary>
        public bool IsVisible()
        {
            return gameObject.activeSelf && canvasGroup != null && canvasGroup.alpha > 0.01f;
        }

        private void FadeIn()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeRoutine(1f));
        }

        private void FadeOut()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeRoutine(0f));
        }

        private IEnumerator FadeRoutine(float targetAlpha)
        {
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            canvasGroup.interactable = targetAlpha > 0.5f;
            canvasGroup.blocksRaycasts = targetAlpha > 0.5f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;

            if (targetAlpha < 0.01f)
            {
                gameObject.SetActive(false);
            }

            fadeCoroutine = null;
        }

        private IEnumerator AutoHideRoutine()
        {
            yield return new WaitForSeconds(autoHideDelay);
            HidePopup();
            autoHideCoroutine = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
                autoHideCoroutine = null;
            }
        }
    }
}
