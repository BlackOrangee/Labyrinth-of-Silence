using UnityEngine;

/// <summary>
/// Клас Quest - структура даних для одного завдання.
/// Містить інформацію про ID, опис, прогрес та стан виконання.
/// </summary>
[System.Serializable]
public class Quest
{
    [Header("Основна інформація")]
    [Tooltip("Унікальний ідентифікатор завдання (наприклад: 'collect_keys')")]
    public string questId;

    [Tooltip("Опис завдання (наприклад: 'Зібрати всі ключі')")]
    public string description;

    [Header("Прогрес")]
    [Tooltip("Поточний прогрес (скільки вже зроблено)")]
    public int currentProgress;

    [Tooltip("Цільовий прогрес (скільки потрібно всього)")]
    public int targetProgress;

    [Header("Стан")]
    [Tooltip("Чи виконано завдання")]
    public bool isCompleted;

    [Tooltip("Час створення завдання (для сортування)")]
    public float createdTime;

    /// <summary>
    /// Конструктор для створення нового завдання
    /// </summary>
    public Quest(string id, string desc, int current, int target)
    {
        questId = id;
        description = desc;
        currentProgress = current;
        targetProgress = target;
        isCompleted = false;
        createdTime = Time.time;
    }

    /// <summary>
    /// Отримати відформатований текст прогресу "Опис (поточний/цільовий)"
    /// </summary>
    public string GetFormattedText()
    {
        return $"{description} ({currentProgress}/{targetProgress})";
    }

    /// <summary>
    /// Перевірити чи досягнуто цілі
    /// </summary>
    public bool IsTargetReached()
    {
        return currentProgress >= targetProgress;
    }

    /// <summary>
    /// Оновити прогрес (БЕЗ автоматичного встановлення isCompleted)
    /// QuestTracker сам керує станом isCompleted
    /// </summary>
    public void UpdateProgress(int newProgress)
    {
        currentProgress = newProgress;
        
        // ВИДАЛЕНО автоматичне встановлення isCompleted
        // Тепер QuestTracker повністю контролює цей процес
    }
}