using UnityEngine;
using System.Linq;

/// <summary>
/// ProximityInteractor:
/// - Перевіряє поруч предмети (OverlapSphere або distance).
/// - Якщо гравець знаходиться поруч певний час (hoverTimeRequired), показує popup.
/// - Popup показуєся з іконкою предмета та кнопкою; предмет буде знятий тільки після натискання.
/// - Використовує InteractionLocker щоб уникнути дублювань.
/// </summary>
public class ProximityInteractor : MonoBehaviour
{
    [Header("Proximity")]
    [Tooltip("Радіус виявлення предметів (popupDistance)")]
    public float popupDistance = 2.0f;

    [Tooltip("Час (сек) перебування в зоні до показу pop-up")]
    public float hoverTimeRequired = 0.5f;

    [Tooltip("Часто використовувана маска шарів для interactable (за замовчуванням All)")]
    public LayerMask interactableLayerMask = ~0;

    [Header("References")]
    [Tooltip("Посилання на PopupManager (перетягніть UI_Manager)")]
    public PopupManager popupManager;

    [Tooltip("Actor - зазвичай Player (перетягніть Player або автоматичний пошук)")]
    public GameObject actor;

    // Внутрішній стан
    private IInteractable currentInteractable = null;
    private CollectItem currentCollectItem = null;
    private GameObject currentGO = null;
    private float hoverTimer = 0f;

    void Start()
    {
        if (actor == null)
        {
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
            if (actor == null) actor = this.gameObject;
        }

        if (popupManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            popupManager = UnityEngine.Object.FindFirstObjectByType<PopupManager>();
#else
            popupManager = FindObjectOfType<PopupManager>();
#endif
            if (popupManager == null)
                Debug.LogWarning("ProximityInteractor: PopupManager не знайдено. Прив'яжіть його вручну.");
        }
    }

    void Update()
    {
        if (actor == null) return;

        // Знаходимо всі колайдери у радіусі popupDistance
        Collider[] hits = Physics.OverlapSphere(actor.transform.position, popupDistance, interactableLayerMask);

        // Вибираємо найближчий інтерактивний об'єкт (з IInteractable)
        GameObject nearest = null;
        float bestDist = float.MaxValue;
        IInteractable nearestInt = null;
        CollectItem nearestCollect = null;

        foreach (var c in hits)
        {
            GameObject g = c.gameObject;
            IInteractable inter = g.GetComponent<IInteractable>();
            if (inter != null)
            {
                float d = Vector3.Distance(actor.transform.position, g.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    nearest = g;
                    nearestInt = inter;
                    nearestCollect = g.GetComponent<CollectItem>();
                }
            }
        }

        // Якщо знайшли найближчий інтерактивний об'єкт в зоні
        if (nearest != null)
        {
            // Якщо це інший об'єкт — скидаємо timer і починаємо новий
            if (nearest != currentGO)
            {
                currentGO = nearest;
                currentInteractable = nearestInt;
                currentCollectItem = nearestCollect;
                hoverTimer = 0f;
                // (не показуємо popup одразу — чекаємо hoverTimeRequired)
            }
            else
            {
                // Ми все ще на тому самому об'єкті — нарощуємо час
                hoverTimer += Time.deltaTime;
                if (hoverTimer >= hoverTimeRequired)
                {
                    if (currentInteractable != null && popupManager != null)
                    {
                        // Якщо popup уже видимий і ми є власником — нічого робимо
                        if (popupManager.IsVisible() && InteractionLocker.IsOwner(this))
                        {
                            // вже показано нами — нічого
                        }
                        else
                        {
                            string msg = currentInteractable.GetInteractText();
                            string btnText = "Підібрати";
                            Sprite icon = currentCollectItem != null ? currentCollectItem.itemIcon : null;

                            popupManager.ShowPopup(msg, OnPopupExecute, this, btnText, icon);
                        }
                    }
                }
            }
        }
        else
        {
            // Якщо нічого поруч — ховаємо popup якщо ми були власником
            hoverTimer = 0f;
            currentGO = null;
            currentInteractable = null;
            currentCollectItem = null;

            // Передаємо this як requester — щоб не ховати popup іншого власника
            popupManager?.HidePopup(this);
        }
    }

    private void OnPopupExecute()
    {
        if (currentInteractable != null && currentGO != null)
        {
            currentInteractable.Interact(actor);
            popupManager?.HidePopup(this);
            // Скинемо стан — предмет, можливо, знищено
            currentInteractable = null;
            currentCollectItem = null;
            currentGO = null;
            hoverTimer = 0f;
        }
    }

    // Візуалізація зони
    private void OnDrawGizmosSelected()
    {
        if (actor != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(actor.transform.position, popupDistance);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, popupDistance);
        }
    }
}
