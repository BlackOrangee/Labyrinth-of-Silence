using UnityEngine;
using System.Collections;

public class LevelStarter : MonoBehaviour
{
    [Header("Завдання, які з'являться на старті сцени")]
    public string[] taskIDs;
    public string[] englishTexts;
    public string[] ukrainianTexts;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);

        if (TaskManager.Instance != null)
        {
            for (int i = 0; i < taskIDs.Length; i++)
            {
                if (!string.IsNullOrEmpty(taskIDs[i]) && !string.IsNullOrEmpty(englishTexts[i]))
                {
                    string ukr = (ukrainianTexts.Length > i) ? ukrainianTexts[i] : "";
                    TaskManager.Instance.AddTask(taskIDs[i], englishTexts[i], ukr);
                }
            }
        }
    }
}