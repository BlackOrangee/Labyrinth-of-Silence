using UnityEngine;

namespace Assets.Scripts
{
    public class PlayerRaycaster : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [Tooltip("Max distance (meters) from which the player can press a button")]
        public float reachDistance = 2.5f;

        [Tooltip("REQUIRED: assign the Interactable layer here")]
        public LayerMask interactableLayer;

        [Header("Crosshair")]
        [Tooltip("Dot size in pixels")]
        public int dotSize = 10;
        [Tooltip("Dot color")]
        public Color dotColor = Color.white;

        private Camera _mainCamera;
        private bool _isLookingAtKeypad;
        private Texture2D _dotTexture;

        private void Start()
        {
            _mainCamera = Camera.main;

            if (_mainCamera == null)
                Debug.LogError("[PlayerRaycaster] No camera tagged MainCamera found in the scene!");

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _dotTexture = CreateCircleTexture(dotSize, dotColor);
        }

        private void OnDestroy()
        {
            if (_dotTexture != null) Destroy(_dotTexture);
        }

        private void Update()
        {
            if (Time.timeScale == 0f) return;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            if (_mainCamera == null) return;

            Ray centerRay = _mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            RaycastHit centerHit;

            if (Physics.Raycast(centerRay, out centerHit, reachDistance))
            {
                _isLookingAtKeypad = centerHit.collider.GetComponent<KeypadButton>() != null
                    || centerHit.collider.GetComponentInParent<KeypadController>() != null;
            }
            else
            {
                _isLookingAtKeypad = false;
            }

            if (Input.GetMouseButtonDown(0))
                PerformClick();
        }

        private void PerformClick()
        {
            Ray ray = _mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, reachDistance, interactableLayer))
            {
                KeypadButton button = hit.collider.GetComponent<KeypadButton>();
                if (button != null)
                    button.PressButton();
            }
        }

        private void OnGUI()
        {
            if (!_isLookingAtKeypad || _dotTexture == null) return;

            float cx = Screen.width / 2f - dotSize / 2f;
            float cy = Screen.height / 2f - dotSize / 2f;
            GUI.DrawTexture(new Rect(cx, cy, dotSize, dotSize), _dotTexture, ScaleMode.StretchToFill, true);
        }

        private Texture2D CreateCircleTexture(int size, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r + 0.5f, dy = y - r + 0.5f;
                    tex.SetPixel(x, y, dx * dx + dy * dy <= r * r ? color : Color.clear);
                }
            tex.Apply();
            return tex;
        }
    }
}