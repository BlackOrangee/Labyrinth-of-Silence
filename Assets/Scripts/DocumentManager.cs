using UnityEngine;
using Assets.Scripts;

public class DocumentManager : MonoBehaviour
{
    public static DocumentManager Instance;

    public int totalDocumentsToFind = 1; 
    private int currentDocumentsFound = 0;
    
    [Tooltip("Якщо це ГОЛОВНА таска (статичний текст) - перетягни її сюди")]
    public GameObject mainObjectiveTextObject;

    [Tooltip("Якщо це ДРУГОРЯДНА таска (префаб) - напиши її ID тут (напр. find_docs)")]
    public string taskIDToRemove;

    private void Awake() { if (Instance == null) Instance = this; }

    public void DocumentFound()
    {
        currentDocumentsFound++;
        if (currentDocumentsFound >= totalDocumentsToFind)
        {
            if (mainObjectiveTextObject != null) mainObjectiveTextObject.SetActive(false);
            
            if (!string.IsNullOrEmpty(taskIDToRemove) && TaskManager.Instance != null)
            {
                TaskManager.Instance.RemoveTask(taskIDToRemove);
            }
        }
    }
}