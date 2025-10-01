using UnityEngine;

namespace Assets.Scripts
{
    public class CameraController : MonoBehaviour
    {
        public Transform player;
        public float mouseSensitivity = 100f;
        public float yOffset = 0f;
        public float xOffset = 0f;

        private float xRotation = 0f;
        private float yRotation = 0f;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        void LateUpdate()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -60f, 80f);

            player.rotation = Quaternion.Euler(0f, yRotation, 0f);

            Vector3 cameraPos = player.position + new Vector3(xOffset, yOffset, 0);
            transform.position = cameraPos;

            transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
    }
}
