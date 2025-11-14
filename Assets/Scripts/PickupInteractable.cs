using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupInteractable : MonoBehaviour, IInteractable
{
    [Header("Item Settings")]
    [Tooltip("Item name displayed in popup")]
    public string itemName = "Item";

    [Tooltip("Destroy object on pickup")]
    public bool destroyOnPickup = true;

    public string GetInteractText()
    {
        return $"Press Execute button to pick up: {itemName}";
    }

    public void OnInteract(GameObject actor)
    {
        SimpleInventory inv = actor.GetComponent<SimpleInventory>();
        if (inv != null)
        {
            inv.AddItem(itemName);
            Debug.Log($"Added {itemName} to player inventory.");
        }
        else
        {
            Debug.Log($"{itemName} picked up (inventory not found).");
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
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