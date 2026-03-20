using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Assets.Scripts;
using Assets.Scripts.Localization;

public class TaskItem
{
    public string taskID; 
    public GameObject uiObject; 
    public TextMeshProUGUI textComponent; 
    public string textEng; 
    public string textUkr; 
}

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance;

    public GameObject panelRoot;       
    public Transform tasksParent;      
    public GameObject taskRowPrefab;   

    [Header("Налаштування видимості")]
    [Tooltip("Увімкніть, якщо на рівні є статична головна таска (щоб панель не зникала, коли немає підзавдань)")]
    public bool keepPanelAlwaysVisible = false;


    private List<TaskItem> activeTasks = new List<TaskItem>();
    private HashSet<string> completedTaskIds = new HashSet<string>();

    private void Awake() { if (Instance == null) Instance = this; }
    private void Start() { UpdatePanelVisibility(); }

    private void OnEnable()
    {
        SettingsManager.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        SettingsManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(Language lang)
    {
        RefreshAllLanguages();
    }

    public void AddTask(string id, string englishText, string ukrText)
    {
        if (activeTasks.Exists(t => t.taskID == id)) return;

        GameObject newUI = Instantiate(taskRowPrefab, tasksParent);
        TextMeshProUGUI textComp = newUI.transform.Find("Txt_Objective").GetComponent<TextMeshProUGUI>();
        
        TaskItem newTask = new TaskItem 
        { 
            taskID = id, 
            uiObject = newUI, 
            textComponent = textComp,
            textEng = englishText, 
            textUkr = ukrText      
        };

        activeTasks.Add(newTask);
        UpdateTextLanguage(newTask); 
        UpdatePanelVisibility(); 
    }

    private void UpdateTextLanguage(TaskItem task)
    {
        if (task.textComponent == null) return;

        Language lang = SettingsManager.Instance != null
            ? SettingsManager.Instance.GetCurrentLanguage()
            : Language.English;

        task.textComponent.text = (lang == Language.Ukrainian && !string.IsNullOrEmpty(task.textUkr))
            ? task.textUkr
            : task.textEng;
    }

    public void RefreshAllLanguages()
    {
        foreach (var task in activeTasks)
        {
            UpdateTextLanguage(task);
        }
    }

    public void UpdateTaskText(string id, string newTextEng, string newTextUkr)
    {
        TaskItem task = activeTasks.Find(t => t.taskID == id);
        if (task != null)
        {
            task.textEng = newTextEng;
            task.textUkr = newTextUkr;
            UpdateTextLanguage(task);
        }
    }

    public void RemoveTask(string id)
    {
        TaskItem task = activeTasks.Find(t => t.taskID == id);
        if (task != null)
        {
            Destroy(task.uiObject);
            activeTasks.Remove(task);
            completedTaskIds.Add(id);
        }
        UpdatePanelVisibility();
    }

    public bool IsCompleted(string id) => completedTaskIds.Contains(id);

    public List<TaskSaveItem> GetActiveTasksForSave()
    {
        var result = new List<TaskSaveItem>();
        foreach (var t in activeTasks)
            result.Add(new TaskSaveItem { taskId = t.taskID, textEng = t.textEng, textUkr = t.textUkr });
        return result;
    }

    public List<string> GetCompletedTaskIds() => new List<string>(completedTaskIds);

    public void RestoreFromSave(List<TaskSaveItem> tasks, List<string> completed)
    {
        foreach (var t in activeTasks)
            if (t.uiObject != null) Destroy(t.uiObject);
        activeTasks.Clear();

        completedTaskIds.Clear();
        if (completed != null)
            foreach (var id in completed)
                completedTaskIds.Add(id);

        if (tasks != null)
            foreach (var t in tasks)
                AddTask(t.taskId, t.textEng, t.textUkr);
    }

    private void UpdatePanelVisibility()
    {
        if (panelRoot != null) 
        {
            if (keepPanelAlwaysVisible)
            {
                panelRoot.SetActive(true);
            }
            else
            {
                panelRoot.SetActive(activeTasks.Count > 0);
            }
        }
    }
}