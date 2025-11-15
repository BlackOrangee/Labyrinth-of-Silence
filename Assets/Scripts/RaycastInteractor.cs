using UnityEngine;

namespace Assets.Scripts
{
    public class RaycastInteractor : MonoBehaviour
    {
        [Header("Raycast")]
        [Tooltip("Maximum raycast distance")]
        public float raycastDistance = 10f;

        [Tooltip("Camera or transform from which ray is cast (if null, will use Camera.main)")]
        public Camera cam;

        [Header("Actor (player)")]
        [Tooltip("GameObject acting as actor (the one performing interaction). Usually Player. If not specified, script will try to find it automatically.")]
        public GameObject actor;

        [Header("Popup")]
        [Tooltip("Distance from player to item at which popup appears (in scene units)")]
        public float popupDistance = 2.0f;

        [Tooltip("Reference to PopupManager in scene (drag UI_Manager with PopupManager component)")]
        public PopupManager popupManager;

        private IInteractable currentInteractable = null;
        private GameObject currentHitGO = null;
        private bool popupVisible = false;

        private void Start()
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

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
                    Debug.LogWarning("RaycastInteractor: PopupManager not found. Link it manually in inspector.");
                }
            }
        }

        private void Update()
        {
            if (cam == null)
            {
                return;
            }

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, raycastDistance))
            {
                GameObject hitGO = hit.collider.gameObject;
                IInteractable interactable = hitGO.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    float distance = Vector3.Distance(actor.transform.position, hitGO.transform.position);

                    if (distance <= popupDistance)
                    {
                        if (currentInteractable != interactable || !popupVisible)
                        {
                            currentInteractable = interactable;
                            currentHitGO = hitGO;
                            ShowPopupForCurrent();
                        }
                    }
                    else
                    {
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
                    if (popupVisible)
                    {
                        HidePopup();
                    }

                    currentInteractable = null;
                    currentHitGO = null;
                }
            }
            else
            {
                if (popupVisible)
                {
                    HidePopup();
                }

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
            if (ci != null)
            {
                icon = ci.itemIcon;
            }

            popupManager.ShowPopup(currentInteractable.GetInteractText(), OnPopupExecute, this, "Pick Up", icon);
            popupVisible = true;
        }

        private void HidePopup()
        {
            popupManager?.HidePopup(this);
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
                {
                    cam = Camera.main;
                }
                else
                {
                    return;
                }
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
}