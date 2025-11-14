using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CollectItem : MonoBehaviour, IInteractable
{
    [Header("CollectItem Settings")]
    [Tooltip("Item name displayed in popup")]
    public string displayName = "Item";

    [Tooltip("Item icon shown in popup")]
    public Sprite itemIcon;

    [Tooltip("Destroy object after collection")]
    public bool destroyOnCollect = true;

    [Tooltip("Optional: play pickup sound")]
    public AudioClip pickupSound;

    [Range(0f, 1f)]
    public float pickupVolume = 1f;

    private bool isCollected = false;
    private AudioSource audioSource;
    private Collider cachedCollider;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();
        if (cachedCollider == null)
        {
            Debug.LogWarning($"CollectItem ({name}): Collider not found. Add a Collider to the object.");
        }

        if (pickupSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = pickupSound;
            audioSource.volume = pickupVolume;
        }
    }

    public string GetInteractText()
    {
        return $"Press button to pick up: {displayName}";
    }

    public void OnInteract(GameObject actor)
    {
        if (isCollected)
        {
            Debug.Log($"CollectItem: {displayName} already collected - ignoring repeat call.");
            return;
        }

        isCollected = true;

        if (cachedCollider != null)
        {
            cachedCollider.enabled = false;
        }

        var inv = actor != null ? actor.GetComponent<SimpleInventory>() : null;
        if (inv != null)
        {
            inv.AddItem(displayName);
            Debug.Log($"CollectItem: {displayName} added to player inventory.");
            
            if (QuestTracker.Instance != null && QuestTracker.Instance.HasQuest("collect_keys"))
            {
                int currentKeys = inv.GetCollectedKeysCount();
                QuestTracker.Instance.UpdateQuest("collect_keys", currentKeys);
                Debug.Log($"CollectItem: updated 'collect_keys' quest progress to {currentKeys}");
            }
        }
        else
        {
            Debug.Log($"CollectItem: {displayName} picked up (inventory not found).");
        }

        if (pickupSound != null)
        {
            GameObject soundHolder = new GameObject("PickupSoundHolder");
            soundHolder.transform.position = transform.position;
            AudioSource a = soundHolder.AddComponent<AudioSource>();
            a.clip = pickupSound;
            a.volume = pickupVolume;
            a.Play();
            Destroy(soundHolder, pickupSound.length + 0.1f);
        }

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
        else
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
            
            foreach (Transform t in transform)
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    public void Interact(GameObject actor)
    {
        OnInteract(actor);
    }

    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null)
        {
            c.isTrigger = false;
        }
    }
}
