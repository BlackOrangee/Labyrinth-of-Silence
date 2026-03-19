using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Assets.Scripts; 

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

    private void Awake() { if (Instance == null) Instance = this; }
    private void Start() { UpdatePanelVisibility(); }

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

        bool isUkr = false;
        if (Assets.Scripts.SettingsManager.Instance != null)
        {
            string currentLang = Assets.Scripts.SettingsManager.Instance.GetCurrentLanguage().ToString();
            isUkr = (currentLang == "Ukrainian");
        }

        task.textComponent.text = isUkr && !string.IsNullOrEmpty(task.textUkr) ? task.textUkr : task.textEng;
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
        }
        UpdatePanelVisibility();
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