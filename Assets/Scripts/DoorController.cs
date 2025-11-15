using UnityEngine;
namespace Assets.Scripts
{
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

        void Start()
        {
            var pi = FindFirstObjectByType<PlayerInteractor>();
            if (pi != null)
            {
                player = pi.gameObject;
                playerInventory = player.GetComponent<SimpleInventory>();
            }
            else
            {
                GameObject p = GameObject.FindWithTag("Player");
                if (p != null)
                {
                    player = p;
                    playerInventory = player.GetComponent<SimpleInventory>();
                }
            }

            if (popupManager == null)
            {
                popupManager = FindFirstObjectByType<PopupManager>();
            }
        }

        public void TryOpenDoor(GameObject requester)
        {
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
                if (popupManager != null)
                {
                    popupManager.ShowPopup("Inventory not found.", null, requester ?? player, "OK", null);
                }
                return;
            }

            int collected = inv.GetCollectedKeysCount();

            if (popupManager == null)
            {
                return;
            }

            object owner = requester != null ? (object)requester : (object)player;

            System.Action onConfirmed = null;
            if (collected >= keysRequired)
            {
                onConfirmed = () => OpenDoor();
            }

            popupManager.ShowKeysCollectedPopup(collected, keysRequired, onConfirmed, owner);
        }

        public void OpenDoor()
        {
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
                    }
                }
            }
        }
    }
}
