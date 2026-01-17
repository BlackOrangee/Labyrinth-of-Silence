using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Ray Settings")]
        public float interactDistance = 3f;
        public float rayRadius = 0.3f; 
        public LayerMask interactLayers = ~0;
        public Camera cam;

        [Header("UI")]
        public PopupManager popupManager;

        private PlayerHideController hideController;
        private IInteractable currentInteractable = null;

        private GameObject lastHitGO = null; 

        void Start()
        {
            if (cam == null) cam = Camera.main;
            if (popupManager == null) popupManager = FindFirstObjectByType<PopupManager>();
            hideController = GetComponent<PlayerHideController>();
        }

        public void ForceReset()
        {
            currentInteractable = null;
            lastHitGO = null;
            if (popupManager != null) popupManager.HidePopup(this);
        }

        void Update()
        {
            if (hideController != null && hideController.IsHiding) return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                ClearCurrent();
                return;
            }

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.SphereCast(ray, rayRadius, out hit, interactDistance, interactLayers))
            {
                GameObject hitGO = hit.collider.gameObject;
                IInteractable interactable = hitGO.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    if (hitGO == lastHitGO && currentInteractable != null) 
                    {
                        return; 
                    }

                    string msg = interactable.GetInteractText();

                    if (string.IsNullOrEmpty(msg))
                    {
                        ClearCurrent();
                    }
                    else 
                    {
                        currentInteractable = interactable;
                        lastHitGO = hitGO;
                        
                        string buttonText = "Press E"; 
                        if (hitGO.GetComponent<HidingSpot>() != null) buttonText = "Hide (E)";

                        popupManager?.ShowPopup(msg, OnPopupExecute, this, buttonText, hitGO.GetComponent<CollectItem>()?.itemIcon);
                    }
                }
                else
                {
                    ClearCurrent();
                }
            }
            else
            {
                ClearCurrent();
            }

            if (Input.GetKeyDown(KeyCode.E) && InteractionLocker.IsOwner(this) && currentInteractable != null)
            {
                ExecuteCurrent();
            }
        }

        private void ClearCurrent()
        {
            if (currentInteractable != null || lastHitGO != null)
            {
                currentInteractable = null;
                lastHitGO = null;
                popupManager?.HidePopup(this);
            }
        }

        private void OnPopupExecute() => ExecuteCurrent();

        private void ExecuteCurrent()
        {
            if (currentInteractable != null && lastHitGO != null)
            {
                var target = currentInteractable;
                target.Interact(this.gameObject);

                ClearCurrent();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (cam != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(cam.transform.position + cam.transform.forward * interactDistance, rayRadius);
            }
        }
    }
}