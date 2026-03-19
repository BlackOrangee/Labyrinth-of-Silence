using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(Collider))]
    public class CollectItem : MonoBehaviour, IInteractable
    {
        [Header("Key Settings")]
        [Tooltip("Колір ключа (Green, Yellow, Blue, Pink)")]
        [SerializeField] private KeyColorType keyColor = KeyColorType.None;

        [Header("Interaction")]
        [Tooltip("Popup ID що показується при наведенні")]
        [SerializeField] private string popupID = "open";

        [Header("Visuals")]
        [SerializeField] private string displayName = "Key";
        [Tooltip("Іконка для UI")]
        public Sprite itemIcon; 
        [SerializeField] private bool destroyOnCollect = true;

        [Header("Audio")]
        [SerializeField] private AudioClip pickupSound;
        [Range(0f, 1f)] [SerializeField] private float pickupVolume = 1f;

        [Header("Task System (Зміна завдань при піднятті)")]
        [Tooltip("Яку таску ВИДАЛИТИ з екрана (напр. find_key_1)")]
        [SerializeField] private string taskIDToRemove;
        
        [Tooltip("Яку таску ДОДАТИ на екран (напр. find_exit_A)")]
        [SerializeField] private string taskIDToAdd;
        [SerializeField] private string taskTextEng;
        [SerializeField] private string taskTextUkr;


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

            if (TaskManager.Instance != null)
            {
                if (!string.IsNullOrEmpty(taskIDToRemove)) 
                    TaskManager.Instance.RemoveTask(taskIDToRemove);
                
                if (!string.IsNullOrEmpty(taskIDToAdd) && !string.IsNullOrEmpty(taskTextEng))
                    TaskManager.Instance.AddTask(taskIDToAdd, taskTextEng, taskTextUkr);
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

        public string GetPopupID()
        {
            return isCollected ? "" : popupID;
        }
    }
}