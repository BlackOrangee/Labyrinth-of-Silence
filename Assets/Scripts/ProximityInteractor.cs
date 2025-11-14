using UnityEngine;
using System.Linq;

public class ProximityInteractor : MonoBehaviour
{
    [Header("Proximity")]
    [Tooltip("Item detection radius (popupDistance)")]
    public float popupDistance = 2.0f;

    [Tooltip("Time (sec) required in zone before showing pop-up")]
    public float hoverTimeRequired = 0.5f;

    [Tooltip("Layer mask for interactable objects (default All)")]
    public LayerMask interactableLayerMask = ~0;

    [Header("References")]
    [Tooltip("Reference to PopupManager (drag UI_Manager)")]
    public PopupManager popupManager;

    [Tooltip("Actor - usually Player (drag Player or auto-find)")]
    public GameObject actor;

    private IInteractable currentInteractable = null;
    private CollectItem currentCollectItem = null;
    private GameObject currentGO = null;
    private float hoverTimer = 0f;
    private bool isPopupShowing = false; // ← НОВИЙ ФЛАГ!

    void Start()
    {
        if (actor == null)
        {
            GameObject found = GameObject.FindWithTag("Player");
            if (found != null)
            {
                actor = found;
            }
            else
            {
#if UNITY_2023_1_OR_NEWER
                var pi = UnityEngine.Object.FindFirstObjectByType<PlayerInteractor>();
#else
                var pi = FindObjectOfType<PlayerInteractor>();
#endif
                if (pi != null)
                {
                    actor = pi.gameObject;
                }
                else
                {
                    GameObject byName = GameObject.Find("Player");
                    if (byName != null)
                    {
                        actor = byName;
                    }
                }
            }
            
            if (actor == null)
            {
                actor = this.gameObject;
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
            {
                Debug.LogWarning("ProximityInteractor: PopupManager not found. Link it manually.");
            }
        }
    }

    void Update()
    {
        if (actor == null || popupManager == null)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(actor.transform.position, popupDistance, interactableLayerMask);

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

        // ✅ ВИПРАВЛЕНА ЛОГІКА
        if (nearest != null)
        {
            // Новий об'єкт
            if (nearest != currentGO)
            {
                // Ховаємо старий popup якщо був
                if (isPopupShowing)
                {
                    HidePopup();
                }

                currentGO = nearest;
                currentInteractable = nearestInt;
                currentCollectItem = nearestCollect;
                hoverTimer = 0f;
            }
            else
            {
                // Той самий об'єкт - збільшуємо таймер
                hoverTimer += Time.deltaTime;
                
                // Показуємо popup ОДИН РАЗ після затримки
                if (hoverTimer >= hoverTimeRequired && !isPopupShowing)
                {
                    ShowPopup();
                }
            }
        }
        else
        {
            // Немає об'єктів поблизу
            if (isPopupShowing)
            {
                HidePopup();
            }

            ResetState();
        }
    }

    private void ShowPopup()
    {
        if (currentInteractable == null || popupManager == null)
        {
            return;
        }

        // Перевіряємо чи можемо заволодіти popup
        if (InteractionLocker.IsLocked && !InteractionLocker.IsOwner(this))
        {
            return;
        }

        string msg = currentInteractable.GetInteractText();
        string btnText = "Pick Up";
        Sprite icon = currentCollectItem != null ? currentCollectItem.itemIcon : null;

        popupManager.ShowPopup(msg, OnPopupExecute, this, btnText, icon);
        isPopupShowing = true;
    }

    private void HidePopup()
    {
        if (popupManager != null && isPopupShowing)
        {
            popupManager.HidePopup(this);
            isPopupShowing = false;
        }
    }

    private void ResetState()
    {
        hoverTimer = 0f;
        currentGO = null;
        currentInteractable = null;
        currentCollectItem = null;
    }

    private void OnPopupExecute()
    {
        if (currentInteractable != null && currentGO != null)
        {
            currentInteractable.Interact(actor);
            HidePopup();
            ResetState();
        }
    }

    private void OnDisable()
    {
        // Ховаємо popup при вимкненні компонента
        HidePopup();
    }

    private void OnDestroy()
    {
        // Ховаємо popup при знищенні
        HidePopup();
    }

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