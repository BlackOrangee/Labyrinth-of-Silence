using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Assets.Scripts.Localization
{
    /// <summary>
    /// Centralized singleton manager for all popup hints
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Reference to the localization database")]
        public LocalizationDatabase localizationDatabase;

        [Header("Animation")]
        [Tooltip("Crossfade transition duration (normal ↔ pressed)")]
        [Range(0.1f, 1f)]
        public float transitionDuration = 0.2f;

        [Tooltip("Fade in/out duration")]
        [Range(0.1f, 2f)]
        public float fadeDuration = 0.3f;

        [Tooltip("Auto-hide delay (0 = manual hide only)")]
        [Range(0f, 10f)]
        public float defaultAutoHideDelay = 3f;

        private Image popupImage;
        private Image overlayImage;
        private Canvas overlayCanvas;
        private CanvasGroup canvasGroup;
        private Coroutine fadeCoroutine;
        private Coroutine autoHideCoroutine;
        private Coroutine crossfadeCoroutine;

        private string currentPopupID;
        private bool isPressed;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            popupImage = GetComponent<Image>();
            if (popupImage == null)
            {
                return;
            }

            popupImage.preserveAspect = true;
            popupImage.raycastTarget = false;
            popupImage.color = Color.white;
            popupImage.type = Image.Type.Simple;
            popupImage.sprite = null;

            CreateOverlayImage();

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
            else
            {
                Debug.LogError("[PopupManager] LocalizationDatabase is not assigned!");
            }
        }

        void OnEnable()
        {
            SettingsManager.OnLanguageChanged += OnLanguageChanged;
        }

        void OnDisable()
        {
            SettingsManager.OnLanguageChanged -= OnLanguageChanged;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void CreateOverlayImage()
        {
            GameObject overlayObject = new GameObject("PopupOverlay");

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                overlayObject.transform.SetParent(parentCanvas.transform, false);
            }
            else
            {
                overlayObject.transform.SetParent(transform.parent, false);
            }

            overlayCanvas = overlayObject.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;

            Canvas popupCanvas = GetComponentInParent<Canvas>();
            if (popupCanvas != null)
            {
                overlayCanvas.sortingOrder = popupCanvas.sortingOrder + 1;
            }
            else
            {
                overlayCanvas.sortingOrder = 1;
            }

            overlayObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            overlayImage = overlayObject.AddComponent<Image>();
            overlayImage.preserveAspect = true;
            overlayImage.raycastTarget = false;

            RectTransform popupRect = GetComponent<RectTransform>();
            RectTransform overlayRect = overlayImage.GetComponent<RectTransform>();

            overlayRect.anchorMin = popupRect.anchorMin;
            overlayRect.anchorMax = popupRect.anchorMax;
            overlayRect.anchoredPosition = popupRect.anchoredPosition;
            overlayRect.sizeDelta = popupRect.sizeDelta;
            overlayRect.pivot = popupRect.pivot;
            overlayRect.localScale = popupRect.localScale;
            overlayRect.localRotation = popupRect.localRotation;

            Color overlayColor = Color.white;
            overlayColor.a = 0f;
            overlayImage.color = overlayColor;

            overlayObject.SetActive(false);
        }

        /// <summary>
        /// Show popup in normal state
        /// </summary>
        public void ShowPopup(string popupID)
        {
            ShowPopup(popupID, false, defaultAutoHideDelay);
        }

        /// <summary>
        /// Show popup with specified state
        /// </summary>
        public void ShowPopup(string popupID, bool pressedState)
        {
            ShowPopup(popupID, pressedState, defaultAutoHideDelay);
        }

        /// <summary>
        /// Show popup with custom auto-hide delay
        /// </summary>
        public void ShowPopup(string popupID, bool pressedState, float autoHideDelay)
        {
            if (localizationDatabase == null)
            {
                Debug.LogError("[PopupManager] Cannot show popup - database is null!");
                return;
            }

            if (string.IsNullOrEmpty(popupID))
            {
                Debug.LogError("[PopupManager] Cannot show popup - ID is empty!");
                return;
            }

            currentPopupID = popupID;
            isPressed = pressedState;

            gameObject.SetActive(true);

            if (overlayImage != null)
            {
                overlayImage.gameObject.SetActive(true);
            }

            UpdateSprite();
            FadeIn();

            if (autoHideDelay > 0f)
            {
                if (autoHideCoroutine != null)
                {
                    StopCoroutine(autoHideCoroutine);
                }
                autoHideCoroutine = StartCoroutine(AutoHideRoutine(autoHideDelay));
            }

            Debug.Log($"[PopupManager] Showing: {popupID} (state: {(isPressed ? "pressed" : "normal")})");
        }

        /// <summary>
        /// Hide the current popup
        /// </summary>
        public void HidePopup()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
                autoHideCoroutine = null;
            }

            FadeOut();
        }

        /// <summary>
        /// Change to pressed state (if popup is visible) with smooth crossfade
        /// </summary>
        public void SetPressedState()
        {
            if (gameObject.activeSelf && !isPressed)
            {
                isPressed = true;

                Language currentLanguage = SettingsManager.Instance != null
                    ? SettingsManager.Instance.GetCurrentLanguage()
                    : Language.English;

                Sprite pressedSprite = localizationDatabase != null
                    ? localizationDatabase.GetPressedSprite(currentPopupID, currentLanguage)
                    : null;

                if (pressedSprite != null)
                {
                    crossfadeCoroutine = StartCoroutine(CrossfadeTransition(pressedSprite));
                    Debug.Log($"[PopupManager] State changed to pressed with crossfade: {currentPopupID}");
                }
                else
                {
                    Debug.LogWarning($"[PopupManager] Pressed sprite not found for: {currentPopupID}");
                }
            }
        }

        /// <summary>
        /// Change to normal state (if popup is visible)
        /// </summary>
        public void SetNormalState()
        {
            if (gameObject.activeSelf && isPressed)
            {
                isPressed = false;
                UpdateSprite();
            }
        }

        /// <summary>
        /// Check if popup is currently visible
        /// </summary>
        public bool IsVisible()
        {
            return gameObject.activeSelf && canvasGroup != null && canvasGroup.alpha > 0.01f;
        }

        /// <summary>
        /// Get current popup ID
        /// </summary>
        public string GetCurrentPopupID()
        {
            return currentPopupID;
        }

        private void UpdateSprite()
        {
            if (localizationDatabase == null || string.IsNullOrEmpty(currentPopupID))
            {
                return;
            }

            Language currentLanguage = SettingsManager.Instance != null
                ? SettingsManager.Instance.GetCurrentLanguage()
                : Language.English;

            Sprite targetSprite = isPressed
                ? localizationDatabase.GetPressedSprite(currentPopupID, currentLanguage)
                : localizationDatabase.GetNormalSprite(currentPopupID, currentLanguage);

            if (targetSprite != null && popupImage != null)
            {
                popupImage.sprite = targetSprite;
                popupImage.enabled = true;
            }
        }

        /// <summary>
        /// Smooth crossfade transition between sprites (like button animation)
        /// </summary>
        private IEnumerator CrossfadeTransition(Sprite targetSprite)
        {
            if (targetSprite == null || overlayImage == null || popupImage == null)
            {
                Debug.LogWarning("[PopupManager] CrossfadeTransition failed - missing sprite or image");
                yield break;
            }

            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
            }

            overlayImage.sprite = targetSprite;
            overlayImage.enabled = true;

            float elapsed = 0f;
            Color overlayColor = overlayImage.color;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);

                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                overlayColor.a = smoothT;
                overlayImage.color = overlayColor;

                yield return null;
            }

            overlayColor.a = 1f;
            overlayImage.color = overlayColor;

            popupImage.sprite = targetSprite;
            popupImage.enabled = true;

            overlayColor.a = 0f;
            overlayImage.color = overlayColor;

            crossfadeCoroutine = null;
        }

        private void OnLanguageChanged(Language newLanguage)
        {
            if (gameObject.activeSelf)
            {
                UpdateSprite();
            }
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

                if (overlayImage != null)
                {
                    overlayImage.gameObject.SetActive(false);
                    Color overlayColor = overlayImage.color;
                    overlayColor.a = 0f;
                    overlayImage.color = overlayColor;
                }
            }

            fadeCoroutine = null;
        }

        private IEnumerator AutoHideRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            HidePopup();
            autoHideCoroutine = null;
        }

#if UNITY_EDITOR
        [ContextMenu("Test Show Normal")]
        private void TestShowNormal()
        {
            if (!string.IsNullOrEmpty(currentPopupID))
            {
                ShowPopup(currentPopupID, false);
            }
            else
            {
                Debug.LogWarning("[PopupManager] Set currentPopupID in inspector first");
            }
        }

        [ContextMenu("Test Show Pressed")]
        private void TestShowPressed()
        {
            if (!string.IsNullOrEmpty(currentPopupID))
            {
                ShowPopup(currentPopupID, true);
            }
            else
            {
                Debug.LogWarning("[PopupManager] Set currentPopupID in inspector first");
            }
        }

        [ContextMenu("Test Hide")]
        private void TestHide()
        {
            HidePopup();
        }
#endif
    }
}