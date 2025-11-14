using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestTracker : MonoBehaviour
{
    #region Singleton
    private static QuestTracker _instance;
    private static bool _isApplicationQuitting = false;

    public static QuestTracker Instance
    {
        get
        {
            // Не створюємо новий екземпляр якщо гра закривається
            if (_isApplicationQuitting)
            {
                Debug.LogWarning("QuestTracker: Application is quitting, returning null.");
                return null;
            }

            if (_instance == null)
            {
                _instance = FindFirstObjectByType<QuestTracker>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("QuestTracker");
                    _instance = go.AddComponent<QuestTracker>();
                    DontDestroyOnLoad(go);
                    Debug.Log("QuestTracker: auto-created Singleton instance.");
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("QuestTracker: duplicate detected, destroying.");
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("QuestTracker: initialized.");
    }

    private void OnDestroy()
    {
        // Очищаємо reference якщо це наш екземпляр
        if (_instance == this)
        {
            _instance = null;
            Debug.Log("QuestTracker: instance destroyed and cleared.");
        }
    }

    private void OnApplicationQuit()
    {
        _isApplicationQuitting = true;
        Debug.Log("QuestTracker: Application quitting flag set.");
    }

    // Метод для Editor - очищення при вході в Play Mode
    #if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        _isApplicationQuitting = false;
        Debug.Log("QuestTracker: Static variables reset for Play Mode.");
    }
    #endif
    #endregion

    #region Events
    public System.Action<Quest> OnQuestAdded;
    public System.Action<Quest> OnQuestUpdated;
    public System.Action<Quest> OnQuestCompleted;
    #endregion

    #region Data
    [Header("Active Quests")]
    [SerializeField]
    [Tooltip("List of all active quests")]
    private List<Quest> activeQuests = new List<Quest>();

    [Header("Settings")]
    [Tooltip("Maximum number of concurrent quests")]
    public int maxActiveQuests = 5;

    [Tooltip("Delay before removing completed quest (seconds)")]
    public float completedQuestRemoveDelay = 3f;
    #endregion

    #region Public Methods

    public void AddQuest(string questId, string description, int currentProgress, int targetProgress)
    {
        if (HasQuest(questId))
        {
            Debug.LogWarning($"QuestTracker: quest '{questId}' already exists. Updating progress.");
            UpdateQuest(questId, currentProgress);
            return;
        }

        if (activeQuests.Count >= maxActiveQuests)
        {
            Debug.LogWarning($"QuestTracker: reached maximum active quests ({maxActiveQuests}).");
            return;
        }

        Quest newQuest = new Quest(questId, description, currentProgress, targetProgress);
        activeQuests.Add(newQuest);

        Debug.Log($"QuestTracker: added quest '{questId}' - {description} ({currentProgress}/{targetProgress})");

        OnQuestAdded?.Invoke(newQuest);

        if (newQuest.IsTargetReached())
        {
            CompleteQuest(questId);
        }
    }

    public void UpdateQuest(string questId, int newProgress)
    {
        Quest quest = GetQuest(questId);
        
        if (quest == null)
        {
            Debug.LogWarning($"QuestTracker: quest '{questId}' not found for update.");
            return;
        }

        if (quest.isCompleted)
        {
            Debug.Log($"QuestTracker: quest '{questId}' already completed, ignoring update.");
            return;
        }

        int oldProgress = quest.currentProgress;
        quest.UpdateProgress(newProgress);

        Debug.Log($"QuestTracker: updated '{questId}': {oldProgress} → {newProgress}/{quest.targetProgress}");

        OnQuestUpdated?.Invoke(quest);

        if (quest.IsTargetReached() && !quest.isCompleted)
        {
            quest.isCompleted = true;
            
            Debug.Log($"🎉 QuestTracker: quest '{questId}' COMPLETED!");

            OnQuestCompleted?.Invoke(quest);

            StartCoroutine(RemoveQuestAfterDelay(questId, completedQuestRemoveDelay));
        }
    }

    public void CompleteQuest(string questId)
    {
        Quest quest = GetQuest(questId);
        
        if (quest == null)
        {
            Debug.LogWarning($"QuestTracker: quest '{questId}' not found for completion.");
            return;
        }

        if (quest.isCompleted)
        {
            Debug.Log($"QuestTracker: quest '{questId}' was already completed earlier.");
            return;
        }

        quest.isCompleted = true;

        Debug.Log($"🎉 QuestTracker: quest '{questId}' force completed!");

        OnQuestCompleted?.Invoke(quest);

        StartCoroutine(RemoveQuestAfterDelay(questId, completedQuestRemoveDelay));
    }

    public void RemoveQuest(string questId)
    {
        Quest quest = GetQuest(questId);
        
        if (quest != null)
        {
            activeQuests.Remove(quest);
            Debug.Log($"QuestTracker: removed quest '{questId}'.");
        }
    }

    public bool HasQuest(string questId)
    {
        return activeQuests.Any(q => q.questId == questId);
    }

    public Quest GetQuest(string questId)
    {
        return activeQuests.FirstOrDefault(q => q.questId == questId);
    }

    public List<Quest> GetAllQuests()
    {
        return new List<Quest>(activeQuests);
    }

    public (int current, int target) GetQuestProgress(string questId)
    {
        Quest quest = GetQuest(questId);
        
        if (quest != null)
        {
            return (quest.currentProgress, quest.targetProgress);
        }
        
        return (-1, -1);
    }

    public void ClearAllQuests()
    {
        activeQuests.Clear();
        
        // Очищаємо події
        OnQuestAdded = null;
        OnQuestUpdated = null;
        OnQuestCompleted = null;
        
        Debug.Log("QuestTracker: all quests cleared.");
    }

    #endregion

    #region Private Methods

    private System.Collections.IEnumerator RemoveQuestAfterDelay(string questId, float delay)
    {
        yield return new WaitForSeconds(delay);
        RemoveQuest(questId);
    }

    #endregion

    #region Debug

    [ContextMenu("Debug: Show All Quests")]
    public void DebugShowAllQuests()
    {
        Debug.Log($"=== QuestTracker: active quests = {activeQuests.Count} ===");
        
        foreach (Quest quest in activeQuests)
        {
            string status = quest.isCompleted ? "✅ COMPLETED" : "🔄 ACTIVE";
            Debug.Log($"{status} | {quest.questId} | {quest.GetFormattedText()}");
        }
    }

    [ContextMenu("Debug: Clear All Quests")]
    public void DebugClearAllQuests()
    {
        ClearAllQuests();
    }

    #endregion
}