using UnityEngine;
using Assets.Scripts;

public class TaskGiverInteractable : MonoBehaviour, IInteractable
{
    [Header("Що додати на екран")]
    public string taskIDToAdd;       
    public string englishTextToAdd;  
    public string ukrainianTextToAdd;

    [Header("Що видалити з екрану")]
    public string taskIDToRemove;    

    [Header("Налаштування підказки UI")]
    public string popupHintID = "open"; 

    [Header("Налаштування спрацьовування")]
    public bool triggerOnlyOnce = true; 

    private bool hasBeenTriggered = false;

    public void OnInteract(GameObject actor) { ProcessTask(); }
    public void Interact(GameObject actor) { ProcessTask(); }
    
    public string GetPopupID() 
    {
        if (triggerOnlyOnce && hasBeenTriggered) 
        {
            return "";
        }
        return popupHintID; 
    }

    private void ProcessTask()
    {
        if (triggerOnlyOnce && hasBeenTriggered) 
        {
            return; 
        }

        if (!string.IsNullOrEmpty(taskIDToRemove)) TaskManager.Instance.RemoveTask(taskIDToRemove);
        
        if (!string.IsNullOrEmpty(taskIDToAdd) && !string.IsNullOrEmpty(englishTextToAdd))
        {
            TaskManager.Instance.AddTask(taskIDToAdd, englishTextToAdd, ukrainianTextToAdd);
        }

        hasBeenTriggered = true;
    }
}