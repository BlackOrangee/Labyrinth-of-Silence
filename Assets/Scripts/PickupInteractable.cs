using UnityEngine;

/// <summary>
/// Компонент для предмета, який можна підібрати/забрати.
/// Прикріплюється до Cube, Cube (1) тощо.
/// Реалізує IInteractable (GetInteractText, OnInteract, Interact).
/// </summary>
[RequireComponent(typeof(Collider))]
public class PickupInteractable : MonoBehaviour, IInteractable
{
    [Header("Налаштування предмета")]
    [Tooltip("Назва предмета, показується в попапі")]
    public string itemName = "Предмет";

    [Tooltip("Чи буде показуватися анімація при знищенні (поки просте видалення)")]
    public bool destroyOnPickup = true;

    public string GetInteractText()
    {
        return $"Натисніть кнопку «Виконати», щоб забрати: {itemName}";
    }

    public void OnInteract(GameObject actor)
    {
        // Логіка підбору
        SimpleInventory inv = actor.GetComponent<SimpleInventory>();
        if (inv != null)
        {
            inv.AddItem(itemName);
            Debug.Log($"Додано {itemName} в інвентар гравця.");
        }
        else
        {
            Debug.Log($"{itemName} підібрано (інвентар не знайдено).");
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }

    // Нова реалізація Interact — просто делегує в OnInteract.
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
