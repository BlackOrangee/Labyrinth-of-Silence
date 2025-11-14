using UnityEngine;

[System.Serializable]
public class Quest
{
    [Header("Basic Information")]
    [Tooltip("Unique quest identifier (e.g.: 'collect_keys')")]
    public string questId;

    [Tooltip("Quest description (e.g.: 'Collect all keys')")]
    public string description;

    [Header("Progress")]
    [Tooltip("Current progress (how much is done)")]
    public int currentProgress;

    [Tooltip("Target progress (how much is needed total)")]
    public int targetProgress;

    [Header("State")]
    [Tooltip("Is quest completed")]
    public bool isCompleted;

    [Tooltip("Quest creation time (for sorting)")]
    public float createdTime;

    public Quest(string id, string desc, int current, int target)
    {
        questId = id;
        description = desc;
        currentProgress = current;
        targetProgress = target;
        isCompleted = false;
        createdTime = Time.time;
    }

    public string GetFormattedText()
    {
        return $"{description} ({currentProgress}/{targetProgress})";
    }

    public bool IsTargetReached()
    {
        return currentProgress >= targetProgress;
    }

    public void UpdateProgress(int newProgress)
    {
        currentProgress = newProgress;
    }
}