using UnityEngine;
using System.Collections; 

namespace Assets.Scripts
{
    public class CameraController : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Base sensitivity multiplier (will be multiplied by setting from SettingsManager)")]
        [SerializeField] private float baseSensitivity = 100f;

        private float mouseSensitivity = 200f;

        [Header("Smoothing (Плавність)")]
        [Tooltip("Час згладжування. 0 = різко (як було), 0.1 = дуже плавно.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float smoothTime = 0.05f; 

        [SerializeField] private Transform playerBody; 

        private float xRotation = 0f;

        private float currentMouseX;
        private float currentMouseY;
        private float xVelocity; 
        private float yVelocity;

        private bool isInputLocked = false;

        private bool isHiding = false;
        private float hidingYaw = 0f;
        private float maxHidingAngle = 50f;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerBody == null)
            {
                playerBody = transform.parent;
            }
            UpdateSensitivityFromSettings();
        }

        void Update()
        {
            if (isInputLocked) return;

            float targetMouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float targetMouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            if (float.IsNaN(targetMouseX) || float.IsInfinity(targetMouseX)) targetMouseX = 0;
            if (float.IsNaN(targetMouseY) || float.IsInfinity(targetMouseY)) targetMouseY = 0;

            currentMouseX = Mathf.SmoothDamp(currentMouseX, targetMouseX, ref xVelocity, smoothTime);
            currentMouseY = Mathf.SmoothDamp(currentMouseY, targetMouseY, ref yVelocity, smoothTime);

            float rotationX = currentMouseX; 
            float rotationY = currentMouseY;

            if (float.IsNaN(rotationX) || float.IsNaN(rotationY)) return;

            xRotation -= rotationY;
            xRotation = Mathf.Clamp(xRotation, -80f, 80f);

            if (isHiding)
            {
                hidingYaw += rotationX;

                hidingYaw = Mathf.Clamp(hidingYaw, -maxHidingAngle, maxHidingAngle);

                transform.localRotation = Quaternion.Euler(xRotation, hidingYaw, 0f);
            }
            else
            {
                if (!float.IsNaN(xRotation))
                {
                    transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                }

                if (playerBody != null)
                {
                    playerBody.Rotate(Vector3.up * rotationX);
                }
            }
        }

        public void SetHidingMode(bool hiding)
        {
            isHiding = hiding;
            hidingYaw = 0f;
        }

        public void SetInputLock(bool locked)
        {
            isInputLocked = locked;
            
            if (locked)
            {
                currentMouseX = 0;
                currentMouseY = 0;
                xVelocity = 0;
                yVelocity = 0;
            }

            if (!locked && !PauseMenu.IsGameOver)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void SetRotation(float pitch, float yaw)
        {
            xRotation = pitch;
            currentMouseX = 0;
            currentMouseY = 0;
            xVelocity = 0;
            yVelocity = 0;

            if (xRotation > 180) xRotation -= 360;
            xRotation = Mathf.Clamp(xRotation, -80f, 80f);
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            
            if (playerBody != null)
            {
                playerBody.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        public IEnumerator ForceLookAtRoutine(Transform target, float duration)
        {
            isInputLocked = true; 
            currentMouseX = 0;
            currentMouseY = 0;
            xVelocity = 0;
            yVelocity = 0;

            Quaternion startBodyRot = playerBody.rotation;
            Quaternion startCamRot = transform.localRotation;
            float time = 0f;

            while (time < duration)
            {
                if(target == null) break;

                Vector3 direction = (target.position - transform.position).normalized;
                if (direction == Vector3.zero) direction = playerBody.forward;

                Vector3 flatDirection = new Vector3(direction.x, 0, direction.z);
                
                if (flatDirection != Vector3.zero)
                {
                    Quaternion targetBodyRot = Quaternion.LookRotation(flatDirection);
                    
                    Quaternion fullLookRot = Quaternion.LookRotation(direction);
                    float targetPitch = fullLookRot.eulerAngles.x;
                    
                    if (targetPitch > 180) targetPitch -= 360;
                    targetPitch = Mathf.Clamp(targetPitch, -80f, 80f);
                    
                    Quaternion targetCamRot = Quaternion.Euler(targetPitch, 0, 0);

                    float t = time / duration;
                    t = t * t * (3f - 2f * t); 

                    playerBody.rotation = Quaternion.Slerp(startBodyRot, targetBodyRot, t);
                    transform.localRotation = Quaternion.Slerp(startCamRot, targetCamRot, t);

                    xRotation = targetPitch;
                }

                time += Time.deltaTime;
                yield return null;
            }
            
            if (target != null)
            {
                 Vector3 finalDir = (target.position - transform.position).normalized;
                 if (finalDir != Vector3.zero && new Vector3(finalDir.x, 0, finalDir.z) != Vector3.zero)
                 {
                     playerBody.rotation = Quaternion.LookRotation(new Vector3(finalDir.x, 0, finalDir.z));
                 }
            }
        }

        /// <summary>
        /// Update sensitivity from SettingsManager
        /// </summary>
        public void UpdateSensitivityFromSettings()
        {
            if (SettingsManager.Instance != null)
            {
                float settingsSensitivity = SettingsManager.Instance.currentSettings.mouseSensitivity;
                mouseSensitivity = baseSensitivity * settingsSensitivity;

                Debug.Log($"[CameraController] Mouse sensitivity updated: {mouseSensitivity:F1} (base: {baseSensitivity}, multiplier: {settingsSensitivity:F2})");
            }
            else
            {
                mouseSensitivity = baseSensitivity * 2f; // Default to 200
                Debug.LogWarning("[CameraController] SettingsManager not found, using default sensitivity");
            }
        }

        /// <summary>
        /// Set mouse sensitivity directly
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = baseSensitivity * sensitivity;
            Debug.Log($"[CameraController] Mouse sensitivity set to: {mouseSensitivity:F1}");
        }
    }
}