using UnityEngine;

namespace Assets.Scripts
{
    public class PlayerRaycaster : MonoBehaviour
    {
        public enum AimMode { CenterOfScreen, MouseCursor }

        [Header("Налаштування взаємодії")]
        [Tooltip("Як гравець цілиться: хрестиком по центру екрану чи вільним курсором миші?")]
        public AimMode aimMode = AimMode.CenterOfScreen;
        
        [Tooltip("Довжина руки (в метрах), з якої гравець може натиснути кнопку")]
        public float reachDistance = 2.5f;
        
        [Tooltip("ОБОВ'ЯЗКОВО обери тут шар Interactable!")]
        public LayerMask interactableLayer;
        private Camera _mainCamera;
        private void Start()
        {
            _mainCamera = Camera.main;
            
            if (_mainCamera == null)
            {
                Debug.LogError("[PlayerRaycaster] У сцені немає камери з тегом MainCamera!");
            }
        }
        private void Update()
        {
            if (aimMode == AimMode.MouseCursor)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else if (aimMode == AimMode.CenterOfScreen)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (Input.GetMouseButtonDown(0)) 
            {
                PerformClick();
            }
        }
        private void PerformClick()
        {
            if (_mainCamera == null) return;

            Ray ray;

            if (aimMode == AimMode.CenterOfScreen)
            {
                ray = _mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            }
            else
            {
                ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            }

            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, reachDistance, interactableLayer))
            {
                KeypadButton button = hit.collider.GetComponent<KeypadButton>();
                
                if (button != null)
                {
                    button.PressButton();
                }
            }
        }
    }
}