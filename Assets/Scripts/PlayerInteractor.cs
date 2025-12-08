using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Ray Settings")]
        public float interactDistance = 3f;
        public Camera cam;

        [Header("UI")]
        public PopupManager popupManager;

        private PlayerHideController hideController;
        private IInteractable currentInteractable = null;
        private GameObject currentGO = null;

        void Start()
        {
            if (cam == null) cam = Camera.main;
            if (popupManager == null) popupManager = FindFirstObjectByType<PopupManager>();
            hideController = GetComponent<PlayerHideController>();
        }

        public void ForceReset()
        {
            currentInteractable = null;
            currentGO = null;
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

            if (Physics.Raycast(ray, out hit, interactDistance))
            {
                GameObject hitGO = hit.collider.gameObject;
                IInteractable interactable = hitGO.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    string msg = interactable.GetInteractText();

                    if (string.IsNullOrEmpty(msg))
                    {
                        ClearCurrent();
                    }
                    else
                    {
                        currentInteractable = interactable;
                        currentGO = hitGO;

                        string buttonText = "Press E";
                        if (hitGO.GetComponent<HidingSpot>() != null) buttonText = "Press E to Hide";

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
            if (currentInteractable != null)
            {
                currentInteractable = null;
                currentGO = null;
                popupManager?.HidePopup(this);
            }
        }

        private void OnPopupExecute() => ExecuteCurrent();

        private void ExecuteCurrent()
        {
            if (currentInteractable != null && currentGO != null)
            {
                var target = currentInteractable;
                target.Interact(this.gameObject);

                if (string.IsNullOrEmpty(target.GetInteractText()))
                {
                    ClearCurrent();
                }
            }
        }
    }
}