using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Interactable newspaper object that can be picked up and read
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class NewspaperPickup : MonoBehaviour, IInteractable
    {
        [Header("Newspaper Settings")]
        [Tooltip("Newspaper data containing image and information")]
        public NewspaperData newspaperData;

        [Header("Pickup Settings")]
        [Tooltip("Destroy object after pickup")]
        public bool destroyOnPickup = true;

        [Tooltip("Automatically add to inventory when read")]
        public bool addToInventoryOnRead = true;

        [Header("Audio")]
        [Tooltip("Sound to play when picking up newspaper")]
        public AudioClip pickupSound;

        [Range(0f, 1f)]
        [Tooltip("Volume for pickup sound")]
        public float pickupVolume = 0.7f;

        private bool isPickedUp = false;
        private Collider cachedCollider;

        private void Awake()
        {
            cachedCollider = GetComponent<Collider>();

            if (newspaperData == null)
            {
                Debug.LogWarning($"NewspaperPickup: '{gameObject.name}' has no NewspaperData assigned!");
            }
        }

        public void Interact(GameObject actor)
        {
            if (isPickedUp)
            {
                Debug.Log("NewspaperPickup: Already picked up, ignoring interaction.");
                return;
            }

            if (newspaperData == null)
            {
                Debug.LogError("NewspaperPickup: Cannot interact - newspaperData is null!");
                return;
            }

            isPickedUp = true;

            if (cachedCollider != null)
            {
                cachedCollider.enabled = false;
            }

            ShowNewspaper(actor);

            PlayPickupSound();

            Debug.Log($"NewspaperPickup: Picked up newspaper '{newspaperData.newspaperName}' (ID: {newspaperData.newspaperId})");

            TriggerThought();
        }

        public void OnInteract(GameObject actor)
        {
            Interact(actor);
        }

        public string GetPopupID()
        {
            return isPickedUp ? "" : "upDocument";
        }

        private void ShowNewspaper(GameObject actor)
        {
            if (NewspaperUI.Instance != null)
            {
                NewspaperUI.Instance.ShowNewspaper(newspaperData);

                if (addToInventoryOnRead)
                {
                    AddToInventory(actor);
                }

                if (destroyOnPickup)
                {
                    StartCoroutine(DestroyAfterUIClose());
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogError("NewspaperPickup: NewspaperUI instance not found in scene!");
            }
        }

        private void AddToInventory(GameObject actor)
        {
            var inventory = actor.GetComponent<SimpleInventory>();
            if (inventory != null && newspaperData != null)
            {
                inventory.AddNewspaper(newspaperData);
                Debug.Log($"NewspaperPickup: Added '{newspaperData.newspaperName}' to inventory.");
            }
            else if (inventory == null)
            {
                Debug.LogWarning("NewspaperPickup: Actor does not have SimpleInventory component.");
            }
        }

        private void PlayPickupSound()
        {
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
            }
        }

        private System.Collections.IEnumerator DestroyAfterUIClose()
        {
            while (NewspaperUI.Instance != null && NewspaperUI.Instance.IsShowing())
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);

            Destroy(gameObject);
        }

        /// <summary>
        /// Trigger thought if ThoughtTrigger component is attached
        /// </summary>
        private void TriggerThought()
        {
            ThoughtTrigger thoughtTrigger = GetComponent<ThoughtTrigger>();
            if (thoughtTrigger != null)
            {
                thoughtTrigger.TriggerFromInteraction();
            }
        }

        private void OnValidate()
        {
            if (newspaperData == null)
            {
                Debug.LogWarning($"NewspaperPickup: '{gameObject.name}' should have newspaperData assigned in inspector.");
            }

            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogWarning($"NewspaperPickup: '{gameObject.name}' requires a Collider component for interaction.");
            }
        }
    }
}
