using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace Assets.Scripts
{
    [RequireComponent(typeof(Image))]
    public class SmoothButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Button Sprites")]
        [Tooltip("Normal state sprite")]
        public Sprite normalSprite;
        [Tooltip("Highlighted state sprite")]
        public Sprite highlightedSprite;
        [Tooltip("Pressed state sprite")]
        public Sprite pressedSprite;

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

        void Start()
        {
            buttonImage = GetComponent<Image>();
            button = GetComponent<Button>();

            if (buttonImage == null)
            {
                Debug.LogError("[SmoothButtonAnimation] Image component не найден!");
                enabled = false;
                return;
            }

            CreateOverlayImage();

            if (normalSprite != null)
            {
                buttonImage.sprite = normalSprite;
            }

            if (button != null)
            {
                button.transition = Selectable.Transition.None;
            }

            Debug.Log("[SmoothButtonAnimation] Инициализация завершена");
        }

        private void CreateOverlayImage()
        {
            GameObject overlayObject = new GameObject("Overlay");
            overlayObject.transform.SetParent(transform, false);
            
            overlayImage = overlayObject.AddComponent<Image>();
            
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
            
            Debug.Log("[SmoothButtonAnimation] Курсор наведён");
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
            Debug.Log("[SmoothButtonAnimation] Курсор ушёл");
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
            Debug.Log("[SmoothButtonAnimation] Кнопка нажата");
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (pressedDelayCoroutine != null)
            {
                StopCoroutine(pressedDelayCoroutine);
            }
            pressedDelayCoroutine = StartCoroutine(DelayedStateChange());
            
            Debug.Log("[SmoothButtonAnimation] Кнопка отпущена");
        }

        private IEnumerator DelayedStateChange()
        {
            float waitTime = Mathf.Max(minPressedDuration, transitionDuration);
            yield return new WaitForSeconds(waitTime);
            
            isPressed = false;
            UpdateButtonState();
            
            pressedDelayCoroutine = null;
        }

        private void UpdateButtonState()
        {
            Sprite targetSprite;

            if (isPressed)
            {
                targetSprite = pressedSprite ?? normalSprite;
            }
            else if (isPointerOver)
            {
                targetSprite = highlightedSprite ?? normalSprite;
            }
            else
            {
                targetSprite = normalSprite;
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
                elapsed += Time.deltaTime;
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

        void OnDisable()
        {
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

            if (buttonImage != null && normalSprite != null)
            {
                buttonImage.sprite = normalSprite;
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