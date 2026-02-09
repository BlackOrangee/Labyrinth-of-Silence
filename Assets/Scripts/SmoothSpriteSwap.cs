using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using Assets.Scripts.Localization;

namespace Assets.Scripts
{
    [RequireComponent(typeof(Image))]
    public class SmoothButtonAnimation : LocalizedComponentBase, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Button Sprites - English")]
        [Tooltip("Normal state sprite")]
        public Sprite normalSprite;
        [Tooltip("Highlighted state sprite")]
        public Sprite highlightedSprite;
        [Tooltip("Pressed state sprite")]
        public Sprite pressedSprite;

        [Header("Button Sprites - Ukrainian")]
        [Tooltip("Normal state sprite (Ukrainian)")]
        public Sprite normalSpriteUkr;
        [Tooltip("Highlighted state sprite (Ukrainian)")]
        public Sprite highlightedSpriteUkr;
        [Tooltip("Pressed state sprite (Ukrainian)")]
        public Sprite pressedSpriteUkr;

        [Header("Animation Settings")]
        [Tooltip("Transition duration (seconds)")]
        [Range(0.1f, 2f)]
        public float transitionDuration = 0.2f;

        [Tooltip("Minimum pressed duration (seconds)")]
        [Range(0f, 1f)]
        public float minPressedDuration = 0.5f;

        private Image buttonImage;
        private Image overlayImage;
        private Button button;

        private bool isPointerOver = false;
        private bool isPressed = false;
        private Coroutine currentTransition;
        private Coroutine pressedDelayCoroutine;

        private Sprite currentNormalSprite;
        private Sprite currentHighlightedSprite;
        private Sprite currentPressedSprite;

        protected override void OnEnable()
        {
            base.OnEnable();

            Debug.Log("[SmoothButtonAnimation] OnEnable called - localization subscribed");
        }

        void Start()
        {
            buttonImage = GetComponent<Image>();
            button = GetComponent<Button>();

            if (buttonImage == null)
            {
                Debug.LogError("[SmoothButtonAnimation] Image component not found!");
                enabled = false;
                return;
            }

            buttonImage.preserveAspect = true;

            CreateOverlayImage();

            currentNormalSprite = normalSprite;
            currentHighlightedSprite = highlightedSprite;
            currentPressedSprite = pressedSprite;

            if (currentNormalSprite != null)
            {
                buttonImage.sprite = currentNormalSprite;
            }

            if (button != null)
            {
                button.transition = Selectable.Transition.None;
            }

            Debug.Log("[SmoothButtonAnimation] Initialization completed");
        }

        private void CreateOverlayImage()
        {
            GameObject overlayObject = new GameObject("Overlay");
            overlayObject.transform.SetParent(transform, false);

            overlayImage = overlayObject.AddComponent<Image>();
            overlayImage.preserveAspect = true;

            RectTransform overlayRect = overlayImage.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            overlayRect.anchoredPosition = Vector2.zero;

            Color overlayColor = Color.white;
            overlayColor.a = 0f;
            overlayImage.color = overlayColor;

            overlayImage.raycastTarget = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            
            isPointerOver = true;
            
            if (!isPressed)
            {
                UpdateButtonState();
            }
            
            Debug.Log("[SmoothButtonAnimation] Pointer entered");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            isPressed = false;
            
            if (pressedDelayCoroutine != null)
            {
                StopCoroutine(pressedDelayCoroutine);
                pressedDelayCoroutine = null;
            }
            
            UpdateButtonState();
            Debug.Log("[SmoothButtonAnimation] Pointer exited");
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            
            isPressed = true;
            
            if (pressedDelayCoroutine != null)
            {
                StopCoroutine(pressedDelayCoroutine);
                pressedDelayCoroutine = null;
            }
            
            UpdateButtonState();
            Debug.Log("[SmoothButtonAnimation] Button pressed");
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (pressedDelayCoroutine != null)
            {
                StopCoroutine(pressedDelayCoroutine);
            }
            pressedDelayCoroutine = StartCoroutine(DelayedStateChange());
            
            Debug.Log("[SmoothButtonAnimation] Button released");
        }

        private IEnumerator DelayedStateChange()
        {
            float waitTime = Mathf.Max(minPressedDuration, transitionDuration);
            yield return new WaitForSecondsRealtime(waitTime);
            
            isPressed = false;
            UpdateButtonState();
            
            pressedDelayCoroutine = null;
        }

        private void UpdateButtonState()
        {
            Sprite targetSprite;

            if (isPressed)
            {
                targetSprite = currentPressedSprite ?? currentNormalSprite;
            }
            else if (isPointerOver)
            {
                targetSprite = currentHighlightedSprite ?? currentNormalSprite;
            }
            else
            {
                targetSprite = currentNormalSprite;
            }

            if (currentTransition != null)
            {
                StopCoroutine(currentTransition);
            }
            currentTransition = StartCoroutine(CrossfadeTransition(targetSprite));
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

                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                overlayColor.a = smoothT;
                overlayImage.color = overlayColor;

                yield return null;
            }

            overlayColor.a = 1f;
            overlayImage.color = overlayColor;

            buttonImage.sprite = targetSprite;
            overlayColor.a = 0f;
            overlayImage.color = overlayColor;

            currentTransition = null;
        }

        public void SetNormalState()
        {
            isPressed = false;
            isPointerOver = false;
            UpdateButtonState();
        }

        public void SetHighlightedState()
        {
            isPressed = false;
            isPointerOver = true;
            UpdateButtonState();
        }

        public void SetPressedState()
        {
            isPressed = true;
            UpdateButtonState();
        }

        /// <summary>
        /// Apply localization - update all button sprites based on selected language
        /// Called automatically when language changes
        /// </summary>
        /// <param name="newLanguage">The new language to apply</param>
        protected override void ApplyLocalization(Language newLanguage)
        {
            Debug.Log($"[SmoothButtonAnimation] Applying localization for language: {newLanguage}");

            switch (newLanguage)
            {
                case Language.Ukrainian:
                    currentNormalSprite = normalSpriteUkr != null ? normalSpriteUkr : normalSprite;
                    currentHighlightedSprite = highlightedSpriteUkr != null ? highlightedSpriteUkr : highlightedSprite;
                    currentPressedSprite = pressedSpriteUkr != null ? pressedSpriteUkr : pressedSprite;
                    break;

                case Language.English:
                default:
                    currentNormalSprite = normalSprite;
                    currentHighlightedSprite = highlightedSprite;
                    currentPressedSprite = pressedSprite;
                    break;
            }

            if (buttonImage != null)
            {
                UpdateButtonState();
                Debug.Log("[SmoothButtonAnimation] Button state updated with localized sprites");
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            isPointerOver = false;
            isPressed = false;

            if (currentTransition != null)
            {
                StopCoroutine(currentTransition);
                currentTransition = null;
            }
            if (pressedDelayCoroutine != null)
            {
                StopCoroutine(pressedDelayCoroutine);
                pressedDelayCoroutine = null;
            }

            if (buttonImage != null && currentNormalSprite != null)
            {
                buttonImage.sprite = currentNormalSprite;
            }
            if (overlayImage != null)
            {
                Color overlayColor = overlayImage.color;
                overlayColor.a = 0f;
                overlayImage.color = overlayColor;
            }
        }

        void OnDestroy()
        {
            if (currentTransition != null)
            {
                StopCoroutine(currentTransition);
            }
            if (pressedDelayCoroutine != null)
            {
                StopCoroutine(pressedDelayCoroutine);
            }
        }
    }
}