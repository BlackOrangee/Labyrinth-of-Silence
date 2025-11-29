using UnityEngine;

namespace Assets.Scripts
{
    public enum HidingSpotType
    {
        Closet, // Шафа з дверима
        Table   // Стіл (без дверей)
    }

    public class HidingSpot : MonoBehaviour, IInteractable
    {
        [Header("General Settings")]
        [Tooltip("Type of hiding spot")]
        public HidingSpotType spotType = HidingSpotType.Closet;

        [Tooltip("Name to display in UI")]
        public string spotName = "Closet";

        [Header("Positions")]
        [Tooltip("Where player stands inside")]
        public Transform hidePoint; // Точка ВНУТРЕДИНІ
        [Tooltip("Where player appears after exit")]
        public Transform exitPoint; // Точка ЗЗОВНІ

        [Header("Wardrobe Specifics (Optional for Table)")]
        public Transform leftDoorPivot;
        public Transform rightDoorPivot;
        [Tooltip("Angle to open doors when hiding (e.g. 20)")]
        public float openAngle = 20f;
        [Tooltip("Speed of door opening")]
        public float doorSmoothness = 5f;

        [Header("Audio")]
        public AudioClip enterSound;
        public AudioClip exitSound;
        [Range(0f, 1f)]
        public float soundVolume = 0.5f;

        // Internal state
        private AudioSource audioSource;
        private bool isOccupied = false;
        
        // References
        private PopupManager popupManager;

        void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.volume = soundVolume;
        }

        void Start()
        {
            popupManager = FindFirstObjectByType<PopupManager>();

            // Автоматичне створення точок, якщо забули (захист від помилок)
            if (hidePoint == null)
            {
                GameObject hideObj = new GameObject("HidePoint_Ref");
                hideObj.transform.SetParent(transform);
                hideObj.transform.localPosition = Vector3.zero; // Центр об'єкта
                hidePoint = hideObj.transform;
            }

            if (exitPoint == null)
            {
                GameObject exitObj = new GameObject("ExitPoint_Ref");
                exitObj.transform.SetParent(transform);
                exitObj.transform.localPosition = Vector3.forward * 1.5f; // 1.5 метра вперед
                exitPoint = exitObj.transform;
            }
        }

        void Update()
        {
            // Логіка анімації дверей тільки для Шафи
            if (spotType == HidingSpotType.Closet && leftDoorPivot != null && rightDoorPivot != null)
            {
                float targetAngle = isOccupied ? openAngle : 0f;
                
                // Ліва двері (відкриваються в мінус або плюс залежно від орієнтації, зазвичай +)
                Quaternion targetRotL = Quaternion.Euler(0, targetAngle, 0);
                // Права двері (дзеркально, зазвичай -)
                Quaternion targetRotR = Quaternion.Euler(0, -targetAngle, 0);

                leftDoorPivot.localRotation = Quaternion.Slerp(leftDoorPivot.localRotation, targetRotL, Time.deltaTime * doorSmoothness);
                rightDoorPivot.localRotation = Quaternion.Slerp(rightDoorPivot.localRotation, targetRotR, Time.deltaTime * doorSmoothness);
            }
        }

        #region IInteractable
        public string GetInteractText()
        {
            return $"Press Enter to hide: {spotName}";
        }

        public void Interact(GameObject actor)
        {
            PlayerHideController controller = actor.GetComponent<PlayerHideController>();
            if (controller != null && !controller.IsHiding)
            {
                EnterHiding(actor, controller);
            }
        }

        public void OnInteract(GameObject actor) => Interact(actor);
        #endregion

        public void EnterHiding(GameObject player, PlayerHideController controller)
        {
            if (isOccupied) return;

            isOccupied = true;
            PlaySound(enterSound);
            
            // Передаємо керування контролеру гравця
            controller.StartHiding(this);
        }

        public void ExitHiding(GameObject player)
        {
            if (!isOccupied) return;

            isOccupied = false;
            PlaySound(exitSound);

            // Телепортуємо гравця на точку виходу
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false; // Вимикаємо фізику для телепортації
            
            player.transform.position = exitPoint.position;
            player.transform.rotation = exitPoint.rotation;

            if (cc != null) cc.enabled = true;

            if (popupManager != null)
            {
                popupManager.HidePopup(this); 
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip, soundVolume);
        }

        void OnDrawGizmos()
        {
            // Малювання точок для зручності налаштування
            if (hidePoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(hidePoint.position, 0.3f);
                Gizmos.DrawIcon(hidePoint.position, "BuildSettings.SelectedIcon", true);
            }

            if (exitPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(exitPoint.position, 0.3f);
                Gizmos.DrawLine(transform.position, exitPoint.position);
                Gizmos.DrawRay(exitPoint.position, exitPoint.forward * 0.5f); // Напрямок погляду
            }
        }
    }
}