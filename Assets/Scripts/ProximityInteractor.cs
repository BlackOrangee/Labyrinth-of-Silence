using UnityEngine;
using System.Linq;
namespace Assets.Scripts
{
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
        private bool isPopupShowing = false;

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
                    var pi = FindFirstObjectByType<PlayerInteractor>();
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
                popupManager = FindFirstObjectByType<PopupManager>();
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

            if (nearest != null)
            {
                if (nearest != currentGO)
                {
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
                    hoverTimer += Time.deltaTime;
                    if (hoverTimer >= hoverTimeRequired && !isPopupShowing)
                    {
                        ShowPopup();
                    }
                }
            }
            else
            {
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
            HidePopup();
        }

        private void OnDestroy()
        {
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
}