using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorController : MonoBehaviour
{
    [Header("Door settings")]
    public int keysRequired = 4;
    public Animator doorAnimator;
    public bool destroyOnOpen = false;

    [Header("References")]
    public PopupManager popupManager;

    private GameObject player;
    private SimpleInventory playerInventory;
    private bool isPlayerInTrigger = false;

    void Start()
    {
#if UNITY_2023_1_OR_NEWER
        var pi = UnityEngine.Object.FindFirstObjectByType<PlayerInteractor>();
#else
        var pi = FindObjectOfType<PlayerInteractor>();
#endif
        if (pi != null)
        {
            player = pi.gameObject;
            playerInventory = player.GetComponent<SimpleInventory>();
            Debug.Log($"DoorController: PlayerInteractor found -> {player.name}");
        }
        else
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                player = p;
                playerInventory = player.GetComponent<SimpleInventory>();
                Debug.Log($"DoorController: Found GameObject by tag Player -> {player.name}");
            }
            else
            {
                Debug.LogWarning("DoorController: Player not found by PlayerInteractor or tag 'Player'.");
            }
        }

        if (popupManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            popupManager = UnityEngine.Object.FindFirstObjectByType<PopupManager>();
#else
            popupManager = FindObjectOfType<PopupManager>();
#endif
            if (popupManager != null)
            {
                Debug.Log($"DoorController: PopupManager found -> {popupManager.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("DoorController: PopupManager NOT found; please assign it in Inspector.");
            }
        }
        else
        {
            Debug.Log($"DoorController: popupManager assigned in Inspector -> {popupManager.gameObject.name}");
        }
    }

    public void TryOpenDoor(GameObject requester)
    {
        Debug.Log($"DoorController: TryOpenDoor called by {(requester != null ? requester.name : "null")}");
        
        SimpleInventory inv = null;
        if (requester != null)
        {
            inv = requester.GetComponent<SimpleInventory>();
        }
        
        if (inv == null)
        {
            inv = playerInventory;
        }

        if (inv == null)
        {
            Debug.LogWarning("DoorController: SimpleInventory not found on requester or player.");
            if (popupManager != null)
            {
                popupManager.ShowPopup("Inventory not found.", null, requester ?? player, "OK", null);
            }
            return;
        }

        int collected = inv.GetCollectedKeysCount();
        Debug.Log($"DoorController: Collected keys = {collected}, required = {keysRequired}");

        if (popupManager == null)
        {
            Debug.LogWarning("DoorController: popupManager missing — cannot show keys popup.");
            return;
        }

        object owner = requester != null ? (object)requester : (object)player;

        System.Action onConfirmed = null;
        if (collected >= keysRequired)
        {
            onConfirmed = () =>
            {
                Debug.Log($"DoorController: OpenDoor callback invoked. (collected={collected})");
                OpenDoor();
            };
        }

        Debug.Log("DoorController: Calling popupManager.ShowKeysCollectedPopup...");
        popupManager.ShowKeysCollectedPopup(collected, keysRequired, onConfirmed, owner);
    }

    public void OpenDoor()
    {
        Debug.Log($"DoorController: Opening door {gameObject.name}");
        
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("Open");
        }
        else if (destroyOnOpen)
        {
            gameObject.SetActive(false);
        }
        else
        {
            Collider c = GetComponent<Collider>();
            if (c != null)
            {
                c.enabled = false;
            }
            transform.Translate(Vector3.up * 2f);
        }
    }

    public void DebugTryOpenNow()
    {
        TryOpenDoor(player);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"DoorController: OnTriggerEnter by {other.name}");
            isPlayerInTrigger = true;
            
            SimpleInventory inv = other.GetComponent<SimpleInventory>();
            if (inv == null)
            {
                inv = playerInventory;
            }
            
            if (inv != null)
            {
                int collected = inv.GetCollectedKeysCount();
                
                if (collected < keysRequired && QuestTracker.Instance != null)
                {
                    if (!QuestTracker.Instance.HasQuest("collect_keys"))
                    {
                        QuestTracker.Instance.AddQuest(
                            "collect_keys",
                            "Need to find all keys",
                            collected,
                            keysRequired
                        );
                        Debug.Log($"DoorController: added quest 'collect_keys' ({collected}/{keysRequired})");
                    }
                }
            }
            
            TryOpenDoor(other.gameObject);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"DoorController: OnTriggerExit by {other.name}");
            isPlayerInTrigger = false;
            
            SimpleInventory inv = other.GetComponent<SimpleInventory>();
            if (inv == null)
            {
                inv = playerInventory;
            }
            
            if (inv != null)
            {
                int collected = inv.GetCollectedKeysCount();
                
                if (collected < keysRequired && popupManager != null)
                {
                    popupManager.HidePopup(other.gameObject);
                    Debug.Log("DoorController: popup hidden via OnTriggerExit (not enough keys)");
                }
                else
                {
                    Debug.Log("DoorController: popup NOT hidden - enough keys, waiting for button press");
                }
            }
        }
    }
}