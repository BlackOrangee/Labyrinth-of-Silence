using UnityEngine;
using System.Collections;
using TMPro;
using Assets.Scripts.Localization;

namespace Assets.Scripts
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerHideController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CameraController mainCamController;

        [Header("Hiding Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [Tooltip("Наскільки далеко можна відійти від центру схованки")]
        [SerializeField] private float movementRange = 0.5f; 

        private bool isHiding = false;
        private bool isTransitioning = false;
        
        private HidingSpot currentSpot;
        private CharacterController charController;
        private PlayerMovement playerMovement;
        private PlayerInteractor playerInteractor;
        
        private Transform cameraTransform;

        private float currentYRotation = 0f;
        private float currentXRotation = 0f;
        private Vector3 initialHidePos;

        public bool IsHiding => isHiding;

        void Awake()
        {
            charController = GetComponent<CharacterController>();
            playerMovement = GetComponent<PlayerMovement>();
            playerInteractor = GetComponent<PlayerInteractor>();

            if (mainCamController == null) mainCamController = GetComponentInChildren<CameraController>();

            if (Camera.main != null) cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            if (isTransitioning) return;

            if (isHiding)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
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
            StartCoroutine(EnterHidingRoutine(spot));
        }

        private IEnumerator EnterHidingRoutine(HidingSpot spot)
        {
            isTransitioning = true;
            isHiding = true;
            currentSpot = spot;

            TogglePlayerControl(false);

            if (Localization.PopupManager.Instance != null)
            {
                Localization.PopupManager.Instance.HidePopup();
            }

            yield return new WaitForSeconds(0.1f);

            transform.position = spot.hidePoint.position;
            transform.rotation = spot.hidePoint.rotation;
            initialHidePos = spot.hidePoint.position;

            currentYRotation = 0f;
            currentXRotation = 0f;

            if(cameraTransform) cameraTransform.localRotation = Quaternion.identity;

            if (Localization.PopupManager.Instance != null)
            {
                Localization.PopupManager.Instance.ShowPopup("out", false, 0f);
            }

            yield return new WaitForSeconds(0.4f);
            isTransitioning = false;
        }

        private IEnumerator ExitHidingRoutine()
        {
            if (currentSpot == null) yield break;
            isTransitioning = true;

            if (Localization.PopupManager.Instance != null)
            {
                Localization.PopupManager.Instance.HidePopup();
            }

            Vector3 lookAngles = cameraTransform.eulerAngles;
            float exitPitch = lookAngles.x;
            float exitYaw = lookAngles.y;

            currentSpot.ExitHiding(this.gameObject);

            yield return null; 

            TogglePlayerControl(true);

            if (mainCamController != null)
            {
                mainCamController.SetRotation(exitPitch, exitYaw);
            }

            isHiding = false;
            currentSpot = null;
            isTransitioning = false;
        }

        private void TogglePlayerControl(bool isActive)
        {
            if (charController) charController.enabled = isActive;
            if (playerMovement) playerMovement.enabled = isActive;
            if (mainCamController) mainCamController.enabled = isActive;
            
            if (playerInteractor)
            {
                if (!isActive) playerInteractor.ForceReset();
                playerInteractor.enabled = isActive;
            }
        }

        private void HandleHidingCamera()
        {
            if (cameraTransform == null) return;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            currentYRotation += mouseX;
            
            currentXRotation -= mouseY;
            currentXRotation = Mathf.Clamp(currentXRotation, -85f, 85f);

            cameraTransform.localRotation = Quaternion.Euler(currentXRotation, currentYRotation, 0f);
        }

        private void HandleHidingMovement()
        {
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDir = (right * x + forward * z).normalized;
            Vector3 newPos = transform.position + moveDir * Time.deltaTime * 2.0f;

            if (Vector3.Distance(newPos, initialHidePos) < movementRange)
            {
                transform.position = newPos;
            }
        }
    }
}