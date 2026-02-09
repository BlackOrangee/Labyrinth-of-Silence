using UnityEngine;

namespace Assets.Scripts
{
    // Гарантуємо, що на об'єкті є Колайдер
    [RequireComponent(typeof(Collider))]
    public class CollectItem : MonoBehaviour, IInteractable
    {
        [Header("Key Settings")]
        [Tooltip("Колір ключа (Green, Yellow, Blue, Pink)")]
        [SerializeField] private KeyColorType keyColor = KeyColorType.None;

        [Header("Visuals")]
        [SerializeField] private string displayName = "Key";
        [Tooltip("Іконка для UI")]
        public Sprite itemIcon; 
        [SerializeField] private bool destroyOnCollect = true;

        [Header("Audio")]
        [SerializeField] private AudioClip pickupSound;
        [Range(0f, 1f)] [SerializeField] private float pickupVolume = 1f;

        private bool isCollected = false;
        private Collider cachedCollider;
        private Rigidbody cachedRigidbody; // Кешуємо посилання на Rigidbody

        private void Awake()
        {
            cachedCollider = GetComponent<Collider>();
            cachedRigidbody = GetComponent<Rigidbody>();

            // --- [FIX: АВТОМАТИЧНА ФІКСАЦІЯ] ---
            // При старті робимо ключ "кінематичним", щоб він не падав крізь стіл 
            // і не відлітав у космос через баги фізики.
            if (cachedRigidbody != null)
            {
                cachedRigidbody.isKinematic = true; 
                cachedRigidbody.useGravity = false; 
            }
        }

        public string GetInteractText()
        {
            string nameToShow = keyColor != KeyColorType.None ? $"{keyColor} Key" : displayName;
            return $"Press [E] to pick: {nameToShow}";
        }

        public void Interact(GameObject actor)
        {
            if (isCollected) return;

            isCollected = true;
            
            // 1. Вимикаємо колайдер, щоб не можна було натиснути двічі
            if (cachedCollider != null) cachedCollider.enabled = false;
            
            // 2. [ВИПРАВЛЕНО] Вимикаємо фізику для 3D
            // Замість .simulated (якого немає в 3D) використовуємо ці налаштування:
            if (cachedRigidbody != null) 
            {
                cachedRigidbody.isKinematic = true;      // Об'єкт завмирає
                cachedRigidbody.detectCollisions = false; // Перестає стикатися з іншими
            }

            // 3. Додаємо в інвентар
            var inv = actor.GetComponent<SimpleInventory>();
            if (inv != null)
            {
                if (keyColor != KeyColorType.None)
                {
                    inv.AddKey(keyColor);
                    Debug.Log($"Picked up key: {keyColor}");
                }
                else
                {
                    Debug.Log($"Picked up item: {displayName}");
                }
            }
            else
            {
                Debug.LogWarning("CollectItem: Player has no SimpleInventory component!");
            }

            // 4. Звук
            if (pickupSound != null)
            {
                // PlayClipAtPoint створює тимчасовий об'єкт у точці, звук дограє до кінця
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
            }

            // 5. Видалення або приховування
            if (destroyOnCollect)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void OnInteract(GameObject actor)
        {
            Interact(actor);
        }
    }
}