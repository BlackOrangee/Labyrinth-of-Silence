using UnityEngine;
using System.Collections;
using TMPro;

namespace Assets.Scripts
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerHideController : MonoBehaviour
    {
        [Header("References")]
        public CameraController mainCamController;
        public PopupManager popupManager;

        [Header("Hiding Settings")]
        public float mouseSensitivity = 2f;
        public float maxLookAngle = 80f; 
        public float movementRange = 0.1f; 
        
        private bool isHiding = false;
        private HidingSpot currentSpot;

        private CharacterController charController;
        private PlayerMovement playerMovement;

        private PlayerInteractor playerInteractor;
        private ProximityInteractor proximityInteractor;
        private RaycastInteractor raycastInteractor;
        
        private float currentYRotation = 0f;
        private float currentXRotation = 0f;
        private Vector3 initialHidePos;

        private bool isTransitioning = false; 

        public bool IsHiding => isHiding;

        void Awake()
        {
            charController = GetComponent<CharacterController>();
            playerMovement = GetComponent<PlayerMovement>();

            playerInteractor = GetComponent<PlayerInteractor>();
            proximityInteractor = GetComponent<ProximityInteractor>();
            raycastInteractor = GetComponent<RaycastInteractor>();

            if (mainCamController == null) mainCamController = GetComponent<CameraController>();
            if (popupManager == null) popupManager = FindFirstObjectByType<PopupManager>();
        }

        void Update()
        {
            if (isTransitioning) return;

            if (isHiding)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    StartCoroutine(ExitHidingRoutine());
                }
                HandleHidingCamera();
                HandleHidingMovement();
            }
        }

        public void StartHiding(HidingSpot spot)
        {
            if (isHiding || isTransitioning) return;
            isHiding = true;
            StartCoroutine(EnterHidingRoutine(spot));
        }

        private IEnumerator EnterHidingRoutine(HidingSpot spot)
        {
            isTransitioning = true;
            currentSpot = spot;

            DisableAllInteractors();

            if (popupManager != null) popupManager.HidePopup(null);

            yield return null;

            if (charController) charController.enabled = false;
            if (playerMovement) playerMovement.enabled = false;
            if (mainCamController) mainCamController.enabled = false;

            transform.position = spot.hidePoint.position;
            transform.rotation = spot.hidePoint.rotation;
            initialHidePos = spot.hidePoint.position;

            currentYRotation = 0f;
            currentXRotation = 0f;
            Camera.main.transform.localRotation = Quaternion.identity;

            if (popupManager != null)
            {
                popupManager.ShowPopup(
                    $"Player in hiding spot: {spot.spotName}\nPress ESC to exit", 
                    null, 
                    this, 
                    "", 
                    null
                );
            }

            isTransitioning = false;
        }

        private IEnumerator ExitHidingRoutine()
        {
            if (currentSpot == null) yield break;
            isTransitioning = true;

            if (popupManager != null) popupManager.HidePopup(this);

            yield return null; 

            currentSpot.ExitHiding(this.gameObject);

            if (charController) charController.enabled = true;
            if (playerMovement) playerMovement.enabled = true;
            if (mainCamController) mainCamController.enabled = true;

            Physics.SyncTransforms();
            yield return null;

            isHiding = false;

            EnableAllInteractors();

            currentSpot = null;
            isTransitioning = false;
        }
        
        private void DisableAllInteractors()
        {
            if (playerInteractor) 
            {
                playerInteractor.ForceReset();
                playerInteractor.enabled = false;
            }

            if (proximityInteractor) proximityInteractor.enabled = false;

            if (raycastInteractor) raycastInteractor.enabled = false;
        }

        private void EnableAllInteractors()
        {
            if (playerInteractor) playerInteractor.enabled = true;
            if (proximityInteractor) proximityInteractor.enabled = true;
            if (raycastInteractor) raycastInteractor.enabled = true;
        }

        public void StopHiding() { if (!isHiding || isTransitioning) return; StartCoroutine(ExitHidingRoutine()); }
        
        private void HandleHidingCamera() {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            currentYRotation += mouseX; currentYRotation = Mathf.Clamp(currentYRotation, -maxLookAngle, maxLookAngle);
            currentXRotation -= mouseY; currentXRotation = Mathf.Clamp(currentXRotation, -60f, 60f);
            Camera.main.transform.localRotation = Quaternion.Euler(currentXRotation, currentYRotation, 0f);
        }
        
        private void HandleHidingMovement() {
            float x = Input.GetAxis("Horizontal"); float z = Input.GetAxis("Vertical");
            Vector3 moveDir = (transform.right * x + transform.forward * z).normalized;
            Vector3 newPos = transform.position + moveDir * Time.deltaTime * 1.5f;
            if (Vector3.Distance(newPos, initialHidePos) < movementRange) transform.position = newPos;
            else transform.position = initialHidePos + (newPos - initialHidePos).normalized * movementRange;
        }
    }
}