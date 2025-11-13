using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Дуже простий інвентар, щоб показати приклад додавання предмета.
/// Тепер містить метод GetCollectedKeysCount() для підрахунку зібраних ключів.
/// Прикріпіть до Player (GameObject з PlayerInteractor).
/// </summary>
public class SimpleInventory : MonoBehaviour
{
    private List<string> items = new List<string>();

    // Додатково зберігаємо простий лічильник ключів для швидкого доступу.
    private int collectedKeys = 0;

    public void AddItem(string name)
    {
        items.Add(name);

        // Проста логіка виявлення "ключа".
        // Якщо назва містить "ключ" (українською) або "key" (англ.) — вважаємо, що це ключ.
        if (!string.IsNullOrEmpty(name))
        {
            string lower = name.ToLowerInvariant();
            if (lower.Contains("ключ") || lower.Contains("key"))
            {
                collectedKeys++;
                
                // При підборі першого ключа - додаємо квест
                if (collectedKeys == 1 && QuestTracker.Instance != null)
                {
                    if (!QuestTracker.Instance.HasQuest("collect_keys"))
                    {
                        QuestTracker.Instance.AddQuest(
                            "collect_keys",
                            "Зібрати всі ключі",
                            1,
                            4  // Загальна кількість ключів
                        );
                        Debug.Log("SimpleInventory: додано квест 'collect_keys' після підбору першого ключа.");
                    }
                }
            }
        }

        Debug.Log($"SimpleInventory: додано {name}. Всього предметів: {items.Count}. Ключів: {collectedKeys}");
    }

    /// <summary>
    /// Повертає кількість зібраних ключів, яка враховується в AddItem.
    /// </summary>
    public int GetCollectedKeysCount()
    {
        return collectedKeys;
    }

    public string[] GetItems()
    {
        return items.ToArray();
    }

    // При потребі: метод для вручнуї установки кількості ключів (наприклад, при завантаженні).
    public void SetCollectedKeysCount(int count)
    {
        collectedKeys = Mathf.Max(0, count);
    }
}
