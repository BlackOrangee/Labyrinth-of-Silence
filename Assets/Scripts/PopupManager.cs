using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;
namespace Assets.Scripts
{
    public class PopupManager : MonoBehaviour
    {
        [Header("UI elements (assign in inspector)")]
        public GameObject popupRoot;
        public TextMeshProUGUI messageText;
        public Image itemIconImage;
        public Button executeButton;
        public TextMeshProUGUI executeButtonText;
        [Header("Default")]
        public string defaultMessage = "Press the E button to pick up the item.";
        public string defaultButtonText = "";

        private const string keysMessageFormat = "Keys collected: {0} / {1}";

        private Action onExecuteCallback;
        private object ownerRef = null;

        void Awake()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }

            if (executeButtonText != null)
            {
                executeButtonText.text = defaultButtonText;
            }

            if (itemIconImage != null)
            {
                itemIconImage.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (popupRoot != null && popupRoot.activeSelf && InteractionLocker.IsOwner(ownerRef))
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
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
            if (popupRoot == null || messageText == null || executeButton == null)
            {
                Debug.LogWarning("PopupManager: UI fields not configured or null; check Inspector.");
                return;
            }

            if (ownerRef != null && !InteractionLocker.IsOwner(owner))
            {
                return;
            }

            if (!InteractionLocker.IsLocked)
            {
                bool ok = InteractionLocker.Claim(owner);

                if (!ok)
                {
                    return;
                }
            }

            ownerRef = owner;
            onExecuteCallback = onExecute;

            messageText.text = string.IsNullOrEmpty(message) ? defaultMessage : message;
            SetButtonText(buttonText);
            SetIcon(icon);

            SetupButton();

            EnsureVisibleAndInteractive();
        }

        public void HidePopup(object requester = null)
        {
            if (ownerRef != null && requester != null && !InteractionLocker.IsOwner(requester))
            {
                return;
            }

            DoHide();

            if (ownerRef != null)
            {
                InteractionLocker.Release(ownerRef);
                ownerRef = null;
            }
        }

        public void HidePopup()
        {
            HidePopup(null);
        }

        private void DoHide()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }

            onExecuteCallback = null;

            if (executeButton != null)
            {
                executeButton.interactable = false;
            }

            if (itemIconImage != null)
            {
                itemIconImage.gameObject.SetActive(false);
            }
        }

        private void SetupButton()
        {
            if (executeButton == null)
            {
                Debug.LogError("PopupManager: executeButton is NULL!");
                return;
            }

            executeButton.onClick.RemoveAllListeners();

            executeButton.onClick.AddListener(OnExecuteButtonClicked);

            executeButton.interactable = (onExecuteCallback != null);
        }

        private void OnExecuteButtonClicked()
        {
            if (!InteractionLocker.IsOwner(ownerRef))
            {
                return;
            }

            if (onExecuteCallback != null)
            {
                onExecuteCallback.Invoke();
            }
            else
            {
                Debug.LogWarning("PopupManager: onExecuteCallback is NULL!");
            }

            HidePopup(ownerRef);
        }

        private void SetButtonText(string text)
        {
            if (executeButtonText != null)
            {
                executeButtonText.text = string.IsNullOrEmpty(text) ? defaultButtonText : text;
            }
        }

        private void SetIcon(Sprite icon)
        {
            if (itemIconImage == null)
            {
                return;
            }

            if (icon == null)
            {
                itemIconImage.gameObject.SetActive(false);
            }
            else
            {
                itemIconImage.sprite = icon;
                itemIconImage.gameObject.SetActive(true);
            }
        }

        private void EnsureVisibleAndInteractive()
        {
            popupRoot.SetActive(true);

            popupRoot.transform.SetAsLastSibling();

            var cg = popupRoot.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = popupRoot.AddComponent<CanvasGroup>();
            }

            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            if (executeButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(executeButton.gameObject);
            }
        }

        public bool IsVisible()
        {
            return popupRoot != null && popupRoot.activeSelf;
        }

        public void ShowKeysCollectedPopup(int collected, int required, Action onOpen, object owner)
        {
            string msg = string.Format(keysMessageFormat, collected, required);

            if (collected >= required)
            {
                ShowPopup(msg, onOpen, owner, "Open", null);
            }
            else
            {
                ShowPopup(msg, null, owner, "OK", null);
            }
        }
    }
}