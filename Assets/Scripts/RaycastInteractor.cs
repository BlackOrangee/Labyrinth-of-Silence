using UnityEngine;

/// <summary>
/// RaycastInteractor (оновлений)
/// - кидає промінь від камери (по центру екрану),
/// - якщо попадає в IInteractable і actor знаходиться ближче ніж popupDistance,
///   показує popup через PopupManager з кнопкою "Підібрати",
/// - тільки при натисканні "Підібрати" викликає Interact(actor).
/// </summary>
public class RaycastInteractor : MonoBehaviour
{
    [Header("Raycast")]
    [Tooltip("Максимальна відстань raycast'у")]
    public float raycastDistance = 10f;

    [Tooltip("Камера або трансформ, від якого кидається промінь (якщо null, буде Camera.main)")]
    public Camera cam;

    [Header("Actor (гравець)")]
    [Tooltip("GameObject який виступає actor (той, хто виконує взаємодію). Зазвичай це Player. Якщо не вказано, скрипт спробує знайти його автоматично.")]
    public GameObject actor;

    [Header("Popup")]
    [Tooltip("Відстань від гравця до предмета, при якій з'являється popup (в одиницях сцени)")]
    public float popupDistance = 2.0f;

    [Tooltip("Посилання на PopupManager у сцені (перетягни UI_Manager з компонентом PopupManager)")]
    public PopupManager popupManager;

    // Внутрішній стан
    private IInteractable currentInteractable = null;
    private GameObject currentHitGO = null;
    private bool popupVisible = false;

    private void Start()
    {
        if (cam == null) cam = Camera.main;

        if (actor == null)
        {
            // Спроба знайти actor розумно
            GameObject found = GameObject.FindWithTag("Player");
            if (found != null) actor = found;
            else
            {
#if UNITY_2023_1_OR_NEWER
                var pi = UnityEngine.Object.FindFirstObjectByType<PlayerInteractor>();
#else
                var pi = FindObjectOfType<PlayerInteractor>();
#endif
                if (pi != null) actor = pi.gameObject;
                else
                {
                    GameObject byName = GameObject.Find("Player");
                    if (byName != null) actor = byName;
                }
            }

            if (actor == null)
            {
                actor = this.gameObject;
                Debug.Log($"RaycastInteractor: actor не встановлено — використовую {actor.name} (fallback).");
            }
            else
            {
                Debug.Log($"RaycastInteractor: actor знайдено -> {actor.name}");
            }
        }

        if (popupManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            popupManager = UnityEngine.Object.FindFirstObjectByType<PopupManager>();
#else
            popupManager = FindObjectOfType<PopupManager>();
#endif
            if (popupManager == null)
                Debug.LogWarning("RaycastInteractor: PopupManager не знайдено. Прив'яжіть його вручну в інспекторі.");
        }
    }

    private void Update()
    {
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        // Якщо попадаємо чимось в межах raycastDistance
        if (Physics.Raycast(ray, out hit, raycastDistance))
        {
            GameObject hitGO = hit.collider.gameObject;
            IInteractable interactable = hitGO.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // Перевіряємо відстань між actor та позицією предмета
                float distance = Vector3.Distance(actor.transform.position, hitGO.transform.position);

                // Якщо в межах popupDistance — показуємо popup
                if (distance <= popupDistance)
                {
                    // Якщо це новий об'єкт або popup ще не показаний — покажемо
                    if (currentInteractable != interactable || !popupVisible)
                    {
                        currentInteractable = interactable;
                        currentHitGO = hitGO;
                        ShowPopupForCurrent();
                    }
                }
                else
                {
                    // Якщо уже був popup видаляємо його, бо пішли з діапазону
                    if (popupVisible)
                    {
                        HidePopup();
                    }
                    if (currentInteractable == interactable)
                    {
                        currentInteractable = null;
                        currentHitGO = null;
                    }
                }
            }
            else
            {
                if (popupVisible) HidePopup();
                currentInteractable = null;
                currentHitGO = null;
            }
        }
        else
        {
            if (popupVisible) HidePopup();
            currentInteractable = null;
            currentHitGO = null;
        }
    }

    private void ShowPopupForCurrent()
    {
        if (currentInteractable == null || popupManager == null)
        {
            return;
        }

        Sprite icon = null;
        var ci = currentHitGO.GetComponent<CollectItem>();
        if (ci != null) icon = ci.itemIcon;

        popupManager.ShowPopup(currentInteractable.GetInteractText(), OnPopupExecute, this, "Підібрати", icon);
        popupVisible = true;
    }

    private void HidePopup()
    {
        popupManager?.HidePopup(this); // <- передаємо this
        popupVisible = false;
    }

    private void OnPopupExecute()
    {
        if (currentInteractable != null && currentHitGO != null)
        {
            currentInteractable.Interact(actor);
            HidePopup();
            currentInteractable = null;
            currentHitGO = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (cam == null)
        {
            if (Application.isPlaying)
                cam = Camera.main;
            else
                return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(cam.transform.position, cam.transform.forward * raycastDistance);

        if (actor != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(actor.transform.position, popupDistance);
        }
    }
}
