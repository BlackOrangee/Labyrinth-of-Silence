using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;

namespace Assets.Scripts
{
    public class PopupManager : MonoBehaviour
    {
        [Header("UI elements")]
        public GameObject popupRoot;
        public TextMeshProUGUI messageText;
        public Image itemIconImage;
        public Button executeButton;
        public TextMeshProUGUI executeButtonText;

        [Header("Animation Settings")]
        public float fadeInDuration = 0.2f;
        public float fadeOutDuration = 2f;

        [Header("Default")]
        public string defaultMessage = "Press E to interact.";
        public string defaultButtonText = "E";

        private const string keysMessageFormat = "Keys collected: {0} / {1}";

        private Action onExecuteCallback;
        private object ownerRef = null;

        private CanvasGroup canvasGroup;
        private Coroutine currentFadeRoutine;

        void Awake()
        {
            if (popupRoot != null)
            {
                canvasGroup = popupRoot.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = popupRoot.AddComponent<CanvasGroup>();
                }
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (executeButtonText != null) executeButtonText.text = defaultButtonText;
            if (itemIconImage != null) itemIconImage.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (ownerRef != null && InteractionLocker.IsOwner(ownerRef))
            {
                InteractionLocker.Release(ownerRef);
                ownerRef = null;
            }
        }

        void Update()
        {
            if (canvasGroup != null && canvasGroup.alpha > 0.01f && InteractionLocker.IsOwner(ownerRef))
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (onExecuteCallback != null && executeButton != null && executeButton.interactable)
                    {
                        OnExecuteButtonClicked();
                    }
                }
            }
        }

        public void ShowPopup(string message, Action onExecute, object owner, string buttonText = null, Sprite icon = null)
        {
            if (popupRoot == null) return;

            if (string.IsNullOrEmpty(message))
            {
                HidePopup(owner);
                return;
            }

            if (ownerRef != null && !InteractionLocker.IsOwner(owner)) return;

            if (!InteractionLocker.IsLocked)
            {
                if (!InteractionLocker.Claim(owner)) return;
            }

            ownerRef = owner;
            onExecuteCallback = onExecute;

            if (messageText != null) messageText.text = message;
            SetButtonText(buttonText);
            SetIcon(icon);
            SetupButton();

            StartFade(1f, fadeInDuration);
        }

        public void HidePopup(object requester = null)
        {
            if (ownerRef != null && requester != null && !InteractionLocker.IsOwner(requester))
            {
                return;
            }

            if (ownerRef != null)
            {
                InteractionLocker.Release(ownerRef);
                ownerRef = null;
            }

            onExecuteCallback = null;
            if (executeButton != null) executeButton.interactable = false;

            StartFade(0f, fadeOutDuration);
        }

        private void StartFade(float targetAlpha, float duration)
        {
            if (canvasGroup == null) return;

            if (currentFadeRoutine != null) StopCoroutine(currentFadeRoutine);

            currentFadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
        }

        private IEnumerator FadeRoutine(float targetAlpha, float duration)
        {
            float startAlpha = canvasGroup.alpha;
            float time = 0f;

            if (targetAlpha > 0.5f)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            else
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            while (time < duration)
            {
                time += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        private void SetupButton()
        {
            if (executeButton == null) return;
            executeButton.onClick.RemoveAllListeners();
            executeButton.onClick.AddListener(OnExecuteButtonClicked);
            executeButton.interactable = (onExecuteCallback != null);
        }

        private void OnExecuteButtonClicked()
        {
            if (onExecuteCallback != null)
            {
                onExecuteCallback.Invoke();
            }
            
            HidePopup(ownerRef);
        }

        private void SetButtonText(string text)
        {
            if (executeButtonText != null)
                executeButtonText.text = string.IsNullOrEmpty(text) ? defaultButtonText : text;
        }

        private void SetIcon(Sprite icon)
        {
            if (itemIconImage == null) return;
            itemIconImage.gameObject.SetActive(icon != null);
            if (icon != null) itemIconImage.sprite = icon;
        }

        public void ShowKeysCollectedPopup(int collected, int required, Action onOpen, object owner)
        {
            string msg = string.Format(keysMessageFormat, collected, required);
            if (collected >= required) ShowPopup(msg, onOpen, owner, "Open (E)", null);
            else ShowPopup(msg, null, owner, "OK", null);
        }
    }
}