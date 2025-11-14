using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

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
        Debug.Log($"PopupManager: ShowPopup called. owner={(owner!=null?owner.ToString():"null")}, message='{message}', buttonText='{buttonText}', icon={(icon!=null?icon.name:"null")}");
        
        if (popupRoot == null || messageText == null || executeButton == null)
        {
            Debug.LogWarning("PopupManager: UI fields not configured or null; check Inspector.");
            return;
        }

        if (ownerRef != null && !InteractionLocker.IsOwner(owner))
        {
            Debug.Log($"PopupManager: ShowPopup ignored because locked by another owner. requestedOwner={owner} CurrentOwner={ownerRef}");
            return;
        }

        if (!InteractionLocker.IsLocked)
        {
            bool ok = InteractionLocker.Claim(owner);
            Debug.Log($"PopupManager: Claim returned {ok} for owner {owner}");
            
            if (!ok)
            {
                Debug.Log("PopupManager: Claim failed — aborting ShowPopup.");
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

        Debug.Log($"PopupManager: ShowPopup by owner (claimed). owner={owner}");
    }

    public void HidePopup(object requester = null)
    {
        if (ownerRef != null && requester != null && !InteractionLocker.IsOwner(requester))
        {
            Debug.Log($"PopupManager: HidePopup called by non-owner {requester}. CurrentOwner = {ownerRef}");
            return;
        }

        Debug.Log($"PopupManager: HidePopup by owner={(requester!=null?requester.ToString():"(no requester)")}");
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

        Debug.Log($"PopupManager: Button setup complete. Interactable={executeButton.interactable}, HasCallback={onExecuteCallback != null}");
    }

    private void OnExecuteButtonClicked()
    {
        Debug.Log($"PopupManager: ExecuteButton clicked! ownerRef={ownerRef}, HasCallback={onExecuteCallback != null}");
        
        if (!InteractionLocker.IsOwner(ownerRef))
        {
            Debug.Log("PopupManager: Click ignored — not owner.");
            return;
        }

        if (onExecuteCallback != null)
        {
            Debug.Log("PopupManager: Invoking callback NOW!");
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
            Debug.Log("PopupManager: Added CanvasGroup to popupRoot for safety.");
        }
        
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        if (executeButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(executeButton.gameObject);
            Debug.Log("PopupManager: SetSelectedGameObject on executeButton.");
        }

        var img = popupRoot.GetComponentInChildren<Image>();
        if (img != null)
        {
            Debug.Log($"PopupManager: popupRoot child Image found (name={img.name}, enabled={img.enabled}, color.a={img.color.a})");
        }

        Debug.Log($"PopupManager: popupRoot activeSelf={popupRoot.activeSelf} siblingIndex={popupRoot.transform.GetSiblingIndex()}");
    }

    public bool IsVisible()
    {
        return popupRoot != null && popupRoot.activeSelf;
    }

    public void ShowKeysCollectedPopup(int collected, int required, Action onOpen, object owner)
    {
        string msg = string.Format(keysMessageFormat, collected, required);
        
        Debug.Log($"PopupManager: ShowKeysCollectedPopup called. collected={collected}, required={required}, hasCallback={onOpen != null}");

        if (collected >= required)
        {
            ShowPopup(msg, onOpen, owner, "Open", null);
        }
        else
        {
            ShowPopup(msg, null, owner, "OK", null);
        }

        Debug.Log("PopupManager: ShowKeysCollectedPopup complete.");
    }
}