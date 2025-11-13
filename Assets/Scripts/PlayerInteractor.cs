using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Скрипт кидає raycast з камери в центр екрану.
/// Якщо попадає в IInteractable — показує popup через PopupManager.
/// Також обробляє натискання клавіші E для швидкої взаємодії.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Налаштування променю")]
    [Tooltip("Максимальна відстань взаємодії")]
    public float interactDistance = 3f;

    [Tooltip("Camera - від якої робиться ray (залиште Main Camera якщо не вказано)")]
    public Camera cam;

    [Header("UI")]
    [Tooltip("Посилання на PopupManager у сцені")]
    public PopupManager popupManager;

    private IInteractable currentInteractable = null;
    private GameObject currentGO = null;

    void Start()
    {
        if (cam == null) cam = Camera.main;

        if (popupManager == null)
        {
            #if UNITY_2023_1_OR_NEWER
            popupManager = UnityEngine.Object.FindFirstObjectByType<PopupManager>();
            #else
            popupManager = FindObjectOfType<PopupManager>();
            #endif

            if (popupManager == null)
                Debug.LogWarning("PopupManager не знайдено в сцені. Додайте його і зв'яжіть у інспекторі.");
        }
    }

    void Update()
    {
        // Якщо UI над активним елементом (наприклад курсор у полі вводу) — не показувати
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // Якщо курсор над UI — приховати попередній попап
            ClearCurrent();
            return;
        }

        // Візуальна діагностика (опціонально): Debug.DrawRay(cam.transform.position, cam.transform.forward * interactDistance, Color.green);
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            GameObject hitGO = hit.collider.gameObject;
            IInteractable interactable = hitGO.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // Якщо новий — оновити
                if (interactable != currentInteractable)
                {
                    currentInteractable = interactable;
                    currentGO = hitGO;
                    // Тепер викликаємо ShowPopup з owner = this
                    string msg = interactable.GetInteractText();
                    popupManager?.ShowPopup(msg, OnPopupExecute, this, "Підібрати", hitGO.GetComponent<CollectItem>()?.itemIcon);
                }
                // Ніяких додаткових дій поки що
            }
            else
            {
                // hit не-інтерактивний
                ClearCurrent();
            }
        }
        else
        {
            ClearCurrent();
        }

        // Нативна клавіша швидкої взаємодії (E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Якщо ми є current owner — E спрацьовує
            if (InteractionLocker.IsOwner(this))
            {
                if (currentInteractable != null)
                {
                    ExecuteCurrent();
                }
            }
            else
            {
                // Якщо не наш owner — нічого
            }
        }
    }

    private void ClearCurrent()
    {
        if (currentInteractable != null)
        {
            currentInteractable = null;
            currentGO = null;
            // Передаємо this як requester — щоб не ховати popup який належить іншому інтеррактору
            popupManager?.HidePopup(this);
        }
    }

    private void OnPopupExecute()
    {
        ExecuteCurrent();
    }

    private void ExecuteCurrent()
    {
        if (currentInteractable != null && currentGO != null)
        {
            currentInteractable.Interact(this.gameObject);
            popupManager?.HidePopup(this);
            currentInteractable = null;
            currentGO = null;
        }
    }
}
