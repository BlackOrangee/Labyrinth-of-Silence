using UnityEngine;
using Assets.Scripts;

public class CodePuzzleManager : MonoBehaviour
{
    public static CodePuzzleManager Instance;
    private string[] currentCode = new string[4] { "_", "_", "_", "_" };
    private bool isTaskActive = false; 

    private void Awake() { if (Instance == null) Instance = this; }

    public bool IsTaskActive() { return isTaskActive; }

    public void StartCodeTask()
    {
        if (isTaskActive) return;
        isTaskActive = true;

        TaskManager.Instance.AddTask("find_code", "Learn the code: _   _   _   _", "Дізнатися код: _   _   _   _");
    }

    public void DiscoverDigit(int index, string digit)
    {
        if (!isTaskActive) return; 
        if (index >= 0 && index < 4)
        {
            currentCode[index] = digit; 
            string displayCode = string.Join("   ", currentCode);
            string fullEng = "Learn the code: " + displayCode;
            string fullUkr = "Дізнатися код: " + displayCode;
            
            TaskManager.Instance.UpdateTaskText("find_code", fullEng, fullUkr);
        }
    }
}