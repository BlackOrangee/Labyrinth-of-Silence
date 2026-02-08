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
        
        // [НОВЕ] Змінні для математики згладжування
        private float currentMouseX;
        private float currentMouseY;
        private float xVelocity; 
        private float yVelocity;
        
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
            UpdateSensitivityFromSettings();
        }

        void Update()
        {
            // [НОВЕ] Якщо заблоковано - мишка не працює (камера не крутиться від руки)
            if (isInputLocked) return;

            // 1. Отримуємо "сирі" дані (ціль, куди хочемо повернути)
            // [FIX] Прибрав множення на Time.deltaTime тут. 
            // Чому: Input.GetAxis вже повертає значення зміщення за кадр. 
            // Множення на час ДВІЧІ (тут і нижче) призводило до "ватного" керування і непередбачуваної поведінки при стрибках FPS.
            float targetMouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float targetMouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // [FIX] ЗАХИСТ ВІД NaN (Not a Number)
            // Тачпад іноді може видати некоректні дані (нескінченність), що ламає Quaternion.
            // Якщо прийшло сміття - ми його обнуляємо.
            if (float.IsNaN(targetMouseX) || float.IsInfinity(targetMouseX)) targetMouseX = 0;
            if (float.IsNaN(targetMouseY) || float.IsInfinity(targetMouseY)) targetMouseY = 0;

            // [НОВЕ] 2. Застосовуємо згладжування (SmoothDamp)
            // Це прибирає різкі ривки тачпада, роблячи рух "гумовим"
            currentMouseX = Mathf.SmoothDamp(currentMouseX, targetMouseX, ref xVelocity, smoothTime);
            currentMouseY = Mathf.SmoothDamp(currentMouseY, targetMouseY, ref yVelocity, smoothTime);

            // 3. Застосовуємо вже згладжені значення
            // [FIX] Тут ми використовуємо значення напряму. Оскільки SmoothDamp вже враховує час всередині себе,
            // додаткове множення на Time.deltaTime тут не потрібне, воно тільки псує чутливість.
            float rotationX = currentMouseX; 
            float rotationY = currentMouseY;

            // [FIX] Захист перед використанням в повороті
            if (float.IsNaN(rotationX) || float.IsNaN(rotationY)) return;

            xRotation -= rotationY;
            xRotation = Mathf.Clamp(xRotation, -80f, 80f);

            // [FIX] Захист самого повороту (це вирішує помилку "Input rotation is NaN")
            if (!float.IsNaN(xRotation))
            {
                transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            }

            if (playerBody != null)
            {
                playerBody.Rotate(Vector3.up * rotationX);
            }
        }

        public void SetInputLock(bool locked)
        {
            isInputLocked = locked;
            
            // [НОВЕ] Якщо блокуємо, треба скинути інерцію, щоб камера не "доїжджала" сама
            if (locked)
            {
                currentMouseX = 0;
                currentMouseY = 0;
                xVelocity = 0;
                yVelocity = 0;
            }

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
            // [НОВЕ] Скидаємо інерцію при примусовому повороті
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

        // [НОВЕ] Плавний поворот камери на обличчя монстра (КРОК 6)
        public IEnumerator ForceLookAtRoutine(Transform target, float duration)
        {
            isInputLocked = true; // Блокуємо керування мишею
            
            // [FIX] Скидаємо інерцію, щоб камеру не "вело" вбік під час катсцени
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

                // Вектор до монстра
                Vector3 direction = (target.position - transform.position).normalized;
                
                // [FIX] Захист: якщо монстр в тій самій точці (vector zero), LookRotation видасть помилку.
                if (direction == Vector3.zero) direction = playerBody.forward;

                // 1. Горизонтальний поворот (Тіло гравця крутиться вліво-вправо)
                Vector3 flatDirection = new Vector3(direction.x, 0, direction.z);
                
                // [FIX] Додатковий захист від нульового вектора
                if (flatDirection != Vector3.zero)
                {
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
                }

                time += Time.deltaTime;
                yield return null;
            }
            
            // Фінальна доводка (щоб точно дивитись в очі)
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