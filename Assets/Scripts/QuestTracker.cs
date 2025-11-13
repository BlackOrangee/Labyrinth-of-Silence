using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// QuestTracker - Singleton система управління завданнями.
/// Відповідає за додавання, оновлення та завершення завдань.
/// Генерує події для оновлення UI.
/// </summary>
public class QuestTracker : MonoBehaviour
{
    #region Singleton
    private static QuestTracker _instance;
    public static QuestTracker Instance
    {
        get
        {
            if (_instance == null)
            {
                // ВИПРАВЛЕНО: FindObjectOfType → FindFirstObjectByType
                _instance = FindFirstObjectByType<QuestTracker>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("QuestTracker");
                    _instance = go.AddComponent<QuestTracker>();
                    DontDestroyOnLoad(go);
                    Debug.Log("QuestTracker: автоматично створено Singleton екземпляр.");
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("QuestTracker: виявлено дублікат, знищую.");
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("QuestTracker: ініціалізовано.");
    }
    #endregion

    #region Events
    /// <summary>
    /// Подія: додано нове завдання
    /// </summary>
    public System.Action<Quest> OnQuestAdded;

    /// <summary>
    /// Подія: оновлено прогрес завдання
    /// </summary>
    public System.Action<Quest> OnQuestUpdated;

    /// <summary>
    /// Подія: завдання виконано
    /// </summary>
    public System.Action<Quest> OnQuestCompleted;
    #endregion

    #region Data
    [Header("Активні завдання")]
    [SerializeField]
    [Tooltip("Список всіх активних завдань")]
    private List<Quest> activeQuests = new List<Quest>();

    [Header("Налаштування")]
    [Tooltip("Максимальна кількість одночасних завдань")]
    public int maxActiveQuests = 5;

    [Tooltip("Час затримки перед видаленням виконаного завдання (секунди)")]
    public float completedQuestRemoveDelay = 3f;
    #endregion

    #region Public Methods

    /// <summary>
    /// Додати нове завдання до списку
    /// </summary>
    /// <param name="questId">Унікальний ID завдання</param>
    /// <param name="description">Опис завдання</param>
    /// <param name="currentProgress">Поточний прогрес</param>
    /// <param name="targetProgress">Цільовий прогрес</param>
    public void AddQuest(string questId, string description, int currentProgress, int targetProgress)
    {
        // Перевірка чи вже існує таке завдання
        if (HasQuest(questId))
        {
            Debug.LogWarning($"QuestTracker: завдання '{questId}' вже існує. Оновлюю прогрес.");
            UpdateQuest(questId, currentProgress);
            return;
        }

        // Перевірка ліміту
        if (activeQuests.Count >= maxActiveQuests)
        {
            Debug.LogWarning($"QuestTracker: досягнуто максимум активних завдань ({maxActiveQuests}).");
            return;
        }

        // Створюємо нове завдання
        Quest newQuest = new Quest(questId, description, currentProgress, targetProgress);
        activeQuests.Add(newQuest);

        Debug.Log($"QuestTracker: додано завдання '{questId}' - {description} ({currentProgress}/{targetProgress})");

        // Викликаємо подію
        OnQuestAdded?.Invoke(newQuest);

        // Якщо вже виконано на момент додавання
        if (newQuest.IsTargetReached())
        {
            CompleteQuest(questId);
        }
    }

    /// <summary>
    /// Оновити прогрес існуючого завдання
    /// </summary>
    /// <param name="questId">ID завдання</param>
    /// <param name="newProgress">Новий прогрес</param>
    public void UpdateQuest(string questId, int newProgress)
    {
        Quest quest = GetQuest(questId);
        
        if (quest == null)
        {
            Debug.LogWarning($"QuestTracker: завдання '{questId}' не знайдено для оновлення.");
            return;
        }

        // Якщо вже виконано - не оновлюємо
        if (quest.isCompleted)
        {
            Debug.Log($"QuestTracker: завдання '{questId}' вже виконано, ігнорую оновлення.");
            return;
        }

        int oldProgress = quest.currentProgress;
        quest.UpdateProgress(newProgress);

        Debug.Log($"QuestTracker: оновлено '{questId}': {oldProgress} → {newProgress}/{quest.targetProgress}");

        // Викликаємо подію оновлення
        OnQuestUpdated?.Invoke(quest);

        // ВИПРАВЛЕНО: Якщо досягли цілі - ОДРАЗУ викликаємо OnQuestCompleted
        if (quest.IsTargetReached() && !quest.isCompleted)
        {
            quest.isCompleted = true;
            
            Debug.Log($"🎉 QuestTracker: завдання '{questId}' ВИКОНАНО!");

            // ВАЖЛИВО: Викликаємо подію завершення ОДРАЗУ (без затримки)
            OnQuestCompleted?.Invoke(quest);

            // Видаляємо завдання зі списку через затримку (щоб дати час на анімацію)
            StartCoroutine(RemoveQuestAfterDelay(questId, completedQuestRemoveDelay));
        }
    }

    /// <summary>
    /// Позначити завдання як виконане (використовується рідко, для форсованого завершення)
    /// </summary>
    /// <param name="questId">ID завдання</param>
    public void CompleteQuest(string questId)
    {
        Quest quest = GetQuest(questId);
        
        if (quest == null)
        {
            Debug.LogWarning($"QuestTracker: завдання '{questId}' не знайдено для завершення.");
            return;
        }

        if (quest.isCompleted)
        {
            Debug.Log($"QuestTracker: завдання '{questId}' вже було виконано раніше.");
            return;
        }

        quest.isCompleted = true;

        Debug.Log($"🎉 QuestTracker: завдання '{questId}' форсовано виконано!");

        // Викликаємо подію завершення
        OnQuestCompleted?.Invoke(quest);

        // Видаляємо завдання через затримку
        StartCoroutine(RemoveQuestAfterDelay(questId, completedQuestRemoveDelay));
    }

    /// <summary>
    /// Видалити завдання зі списку
    /// </summary>
    /// <param name="questId">ID завдання</param>
    public void RemoveQuest(string questId)
    {
        Quest quest = GetQuest(questId);
        
        if (quest != null)
        {
            activeQuests.Remove(quest);
            Debug.Log($"QuestTracker: видалено завдання '{questId}'.");
        }
    }

    /// <summary>
    /// Перевірити чи існує завдання
    /// </summary>
    /// <param name="questId">ID завдання</param>
    /// <returns>true якщо завдання існує</returns>
    public bool HasQuest(string questId)
    {
        return activeQuests.Any(q => q.questId == questId);
    }

    /// <summary>
    /// Отримати завдання за ID
    /// </summary>
    /// <param name="questId">ID завдання</param>
    /// <returns>Quest або null</returns>
    public Quest GetQuest(string questId)
    {
        return activeQuests.FirstOrDefault(q => q.questId == questId);
    }

    /// <summary>
    /// Отримати всі активні завдання
    /// </summary>
    /// <returns>Список завдань</returns>
    public List<Quest> GetAllQuests()
    {
        return new List<Quest>(activeQuests);
    }

    /// <summary>
    /// Отримати прогрес завдання
    /// </summary>
    /// <param name="questId">ID завдання</param>
    /// <returns>Кортеж (поточний, цільовий) або (-1, -1) якщо не знайдено</returns>
    public (int current, int target) GetQuestProgress(string questId)
    {
        Quest quest = GetQuest(questId);
        
        if (quest != null)
        {
            return (quest.currentProgress, quest.targetProgress);
        }
        
        return (-1, -1);
    }

    /// <summary>
    /// Очистити всі завдання (для дебагу)
    /// </summary>
    public void ClearAllQuests()
    {
        activeQuests.Clear();
        Debug.Log("QuestTracker: всі завдання очищено.");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Корутина для видалення завдання через затримку
    /// </summary>
    private System.Collections.IEnumerator RemoveQuestAfterDelay(string questId, float delay)
    {
        yield return new WaitForSeconds(delay);
        RemoveQuest(questId);
    }

    #endregion

    #region Debug

    /// <summary>
    /// Дебаг: вивести всі завдання в консоль
    /// </summary>
    [ContextMenu("Debug: Show All Quests")]
    public void DebugShowAllQuests()
    {
        Debug.Log($"=== QuestTracker: активних завдань = {activeQuests.Count} ===");
        
        foreach (Quest quest in activeQuests)
        {
            string status = quest.isCompleted ? "✅ ВИКОНАНО" : "🔄 АКТИВНЕ";
            Debug.Log($"{status} | {quest.questId} | {quest.GetFormattedText()}");
        }
    }

    #endregion
}