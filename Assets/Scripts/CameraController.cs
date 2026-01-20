using UnityEngine;
using System.Collections; 

namespace Assets.Scripts
{
    public class CameraController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float mouseSensitivity = 200f; 
        [SerializeField] private Transform playerBody; 

        private float xRotation = 0f;
        
        // [НОВЕ] Блокування мишки для катсцен смерті
        private bool isInputLocked = false;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerBody == null)
            {
                playerBody = transform.parent;
            }
        }

        void Update()
        {
            // [НОВЕ] Якщо заблоковано - мишка не працює (камера не крутиться від руки)
            if (isInputLocked) return;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -80f, 80f);

            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            if (playerBody != null)
            {
                playerBody.Rotate(Vector3.up * mouseX);
            }
        }

        public void SetInputLock(bool locked)
        {
            isInputLocked = locked;
            // Якщо розблокували, переконаємось що курсор знову схований
            if (!locked) 
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                // При смерті можна показати курсор, якщо це фінал
                // Cursor.lockState = CursorLockMode.None;
                // Cursor.visible = true;
            }
        }

        public void SetRotation(float pitch, float yaw)
        {
            xRotation = pitch;
            if (xRotation > 180) xRotation -= 360;
            xRotation = Mathf.Clamp(xRotation, -80f, 80f);
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            
            if (playerBody != null)
            {
                playerBody.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        // [НОВЕ] Плавний поворот камери на обличчя монстра (КРОК 6)
        public IEnumerator ForceLookAtRoutine(Transform target, float duration)
        {
            isInputLocked = true; // Блокуємо керування мишею

            Quaternion startBodyRot = playerBody.rotation;
            Quaternion startCamRot = transform.localRotation;
            float time = 0f;

            while (time < duration)
            {
                if(target == null) break;

                // Вектор до монстра
                Vector3 direction = (target.position - transform.position).normalized;
                
                // 1. Горизонтальний поворот (Тіло гравця крутиться вліво-вправо)
                Vector3 flatDirection = new Vector3(direction.x, 0, direction.z);
                Quaternion targetBodyRot = Quaternion.LookRotation(flatDirection);

                // 2. Вертикальний поворот (Камера киває вгору-вниз)
                Quaternion fullLookRot = Quaternion.LookRotation(direction);
                float targetPitch = fullLookRot.eulerAngles.x;
                
                // Нормалізація кута (щоб не було стрибків 0 -> 360)
                if (targetPitch > 180) targetPitch -= 360;
                targetPitch = Mathf.Clamp(targetPitch, -80f, 80f);
                
                Quaternion targetCamRot = Quaternion.Euler(targetPitch, 0, 0);

                // Інтерполяція (SmoothStep)
                float t = time / duration;
                t = t * t * (3f - 2f * t); 

                playerBody.rotation = Quaternion.Slerp(startBodyRot, targetBodyRot, t);
                transform.localRotation = Quaternion.Slerp(startCamRot, targetCamRot, t);

                // Оновлюємо внутрішню змінну, щоб після розблокування камера не стрибнула
                xRotation = targetPitch;

                time += Time.deltaTime;
                yield return null;
            }
            
            // Фінальна доводка (щоб точно дивитись в очі)
             Vector3 finalDir = (target.position - transform.position).normalized;
             playerBody.rotation = Quaternion.LookRotation(new Vector3(finalDir.x, 0, finalDir.z));
        }
    }
}