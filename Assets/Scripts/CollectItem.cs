using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(Collider))]
    public class CollectItem : MonoBehaviour, IInteractable
    {
        [Header("Key Settings (New)")]
        [Tooltip("Вибери колір ключа тут (Green, Yellow, Blue, Pink)")]
        public KeyColorType keyColor = KeyColorType.None;

        [Header("Visuals (Restored)")]
        public string displayName = "Key";
        [Tooltip("Іконка, яка відображається в Popup")]
        public Sprite itemIcon;
        public bool destroyOnCollect = true;

        [Header("Audio")]
        public AudioClip pickupSound;
        [Range(0f, 1f)] public float pickupVolume = 1f;

        private bool isCollected = false;
        private Collider cachedCollider;

        private void Awake()
        {
            cachedCollider = GetComponent<Collider>();
        }

        public string GetInteractText()
        {
            string nameToShow = keyColor != KeyColorType.None ? $"{keyColor} Key" : displayName;
            return $"Press to take {nameToShow}";
        }

        public void Interact(GameObject actor)
        {
            if (isCollected) return;

            isCollected = true;
            if (cachedCollider != null) cachedCollider.enabled = false;

            var inv = actor.GetComponent<SimpleInventory>();
            if (inv != null)
            {
                if (keyColor != KeyColorType.None)
                {
                    inv.AddKey(keyColor);
                }
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