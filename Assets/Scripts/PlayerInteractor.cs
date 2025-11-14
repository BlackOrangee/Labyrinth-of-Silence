using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Ray Settings")]
    [Tooltip("Maximum interaction distance")]
    public float interactDistance = 3f;

    [Tooltip("Camera - from which ray is cast (leave Main Camera if not specified)")]
    public Camera cam;

    [Header("UI")]
    [Tooltip("Reference to PopupManager in scene")]
    public PopupManager popupManager;

    private IInteractable currentInteractable = null;
    private GameObject currentGO = null;

    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main;
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
                Debug.LogWarning("PopupManager not found in scene. Add it and link in inspector.");
            }
        }
    }

    void Update()
    {
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
                if (interactable != currentInteractable)
                {
                    currentInteractable = interactable;
                    currentGO = hitGO;
                    string msg = interactable.GetInteractText();
                    
                    string buttonText = "Pick Up";
                    if (hitGO.GetComponent<HidingSpot>() != null)
                    {
                        buttonText = "Enter";
                    }
                    
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

        if ((Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && InteractionLocker.IsOwner(this) && currentInteractable != null)
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