using UnityEngine;

namespace Assets.Scripts
{
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
        private Rigidbody cachedRigidbody;

        private void Awake()
        {
            cachedCollider = GetComponent<Collider>();
            cachedRigidbody = GetComponent<Rigidbody>();

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

            if (cachedCollider != null) cachedCollider.enabled = false;

            if (cachedRigidbody != null) 
            {
                cachedRigidbody.isKinematic = true;
                cachedRigidbody.detectCollisions = false;
            }

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
                    inv.AddItem(displayName);
                    Debug.Log($"Picked up item: {displayName}");
                }
            }
            else
            {
                Debug.LogWarning("CollectItem: Player has no SimpleInventory component!");
            }

            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
            }

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