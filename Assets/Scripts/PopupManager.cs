using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

/// <summary>
/// PopupManager (ВИПРАВЛЕНИЙ - двері працюють!)
/// - Кнопка підключається В МОМЕНТ показу popup
/// - Додана перевірка на null
/// - ShowKeysCollectedPopup для дверей
/// </summary>
public class PopupManager : MonoBehaviour
{
    [Header("UI елементи (прив'яжіть у інспекторі)")]
    public GameObject popupRoot;               // Panel
    public TextMeshProUGUI messageText;        // головний текст
    public Image itemIconImage;                // іконка предмета (Image компонент)
    public Button executeButton;               // кнопка
    public TextMeshProUGUI executeButtonText;  // текст на кнопці

    [Header("Default")]
    public string defaultMessage = "Натисніть кнопку, щоб забрати предмет.";
    public string defaultButtonText = "Підібрати";

    // Для дверей
    private const string keysMessageFormat = "Зібрано ключів: {0} / {1}";

    private Action onExecuteCallback;
    private object ownerRef = null;

    void Awake()
    {
        // Дефолтні налаштування
        if (popupRoot != null) popupRoot.SetActive(false);

        if (executeButtonText != null)
            executeButtonText.text = defaultButtonText;

        if (itemIconImage != null)
            itemIconImage.gameObject.SetActive(false);
    }

    // ---------- Основний ShowPopup ----------
    public void ShowPopup(string message, Action onExecute, object owner, string buttonText = null, Sprite icon = null)
    {
        Debug.Log($"PopupManager: ShowPopup called. owner={(owner!=null?owner.ToString():"null")}, message='{message}', buttonText='{buttonText}', icon={(icon!=null?icon.name:"null")}");
        if (popupRoot == null || messageText == null || executeButton == null)
        {
            Debug.LogWarning("PopupManager: UI-поля не налаштовані або null; перевірте Inspector.");
            return;
        }

        // Якщо вже є owner і це не наш — ігноруємо
        if (ownerRef != null && !InteractionLocker.IsOwner(owner))
        {
            Debug.Log($"PopupManager: ShowPopup ignored because locked by another owner. requestedOwner={owner} CurrentOwner={ownerRef}");
            return;
        }

        // Примусове claim якщо ще ніхто не власник
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

        // Встановлюємо текст/іконку/текст кнопки
        messageText.text = string.IsNullOrEmpty(message) ? defaultMessage : message;
        SetButtonText(buttonText);
        SetIcon(icon);

        // ВИПРАВЛЕНО: Підключаємо кнопку ПЕРЕД показом popup
        SetupButton();

        // Вмикаємо popupRoot і робимо його видимим / інтерактивним
        EnsureVisibleAndInteractive();

        Debug.Log($"PopupManager: ShowPopup by owner (claimed). owner={owner}");
    }

    // ---------- Hide ----------
    public void HidePopup(object requester = null)
    {
        // Якщо є ownerRef — тільки owner може ховати (або ховаємо без args)
        if (ownerRef != null)
        {
            if (requester != null && !InteractionLocker.IsOwner(requester))
            {
                Debug.Log($"PopupManager: HidePopup called by non-owner {requester}. CurrentOwner = {ownerRef}");
                return;
            }
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
        if (executeButton != null) executeButton.interactable = false;
        if (itemIconImage != null) itemIconImage.gameObject.SetActive(false);
    }

    // ---------- ВИПРАВЛЕНО: Setup Button ----------
    private void SetupButton()
    {
        if (executeButton == null)
        {
            Debug.LogError("PopupManager: executeButton is NULL!");
            return;
        }

        // Видаляємо всі старі listeners
        executeButton.onClick.RemoveAllListeners();

        // Додаємо наш listener
        executeButton.onClick.AddListener(OnExecuteButtonClicked);

        // Робимо кнопку активною ТІЛЬКИ якщо є callback
        executeButton.interactable = (onExecuteCallback != null);

        Debug.Log($"PopupManager: Button setup complete. Interactable={executeButton.interactable}, HasCallback={onExecuteCallback != null}");
    }

    // ---------- Execute ----------
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
            executeButtonText.text = string.IsNullOrEmpty(text) ? defaultButtonText : text;
    }

    private void SetIcon(Sprite icon)
    {
        if (itemIconImage == null) return;
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
        // Активуємо корінь
        popupRoot.SetActive(true);

        // Ставимо як останній sibling щоб бути зверху
        popupRoot.transform.SetAsLastSibling();

        // Якщо є CanvasGroup — встановимо видимість і блокування
        var cg = popupRoot.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            // додамо, щоб на 100% контролювати
            cg = popupRoot.AddComponent<CanvasGroup>();
            Debug.Log("PopupManager: Added CanvasGroup to popupRoot for safety.");
        }
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        // Спробуємо поставити фокус на кнопку
        if (executeButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(executeButton.gameObject);
            Debug.Log("PopupManager: SetSelectedGameObject on executeButton.");
        }

        // Перевіримо видимість
        var img = popupRoot.GetComponentInChildren<Image>();
        if (img != null)
            Debug.Log($"PopupManager: popupRoot child Image found (name={img.name}, enabled={img.enabled}, color.a={img.color.a})");

        Debug.Log($"PopupManager: popupRoot activeSelf={popupRoot.activeSelf} siblingIndex={popupRoot.transform.GetSiblingIndex()}");
    }

    public bool IsVisible()
    {
        return popupRoot != null && popupRoot.activeSelf;
    }

    // ---------- Helpers for Doors ----------
    public void ShowKeysCollectedPopup(int collected, int required, Action onOpen, object owner)
    {
        string msg = string.Format(keysMessageFormat, collected, required);
        
        Debug.Log($"PopupManager: ShowKeysCollectedPopup called. collected={collected}, required={required}, hasCallback={onOpen != null}");

        // Якщо достатньо ключів — покажемо кнопку "Відчинити"
        if (collected >= required)
        {
            ShowPopup(msg, onOpen, owner, "Відчинити", null);
        }
        else
        {
            ShowPopup(msg, null, owner, "OK", null);
        }

        Debug.Log("PopupManager: ShowKeysCollectedPopup complete.");
    }
}