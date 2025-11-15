using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Tooltip("Pause panel name")]
    public string mainPanelName = "MainPanel";
    [Tooltip("Load save panel name")]
    public string newGamePanelName = "NewGamePanel";
    [Tooltip("Load save panel name")]
    public string loadSavePanelName = "LoadSavePanel";
    [Tooltip("Options panel name")]
    public string optionsPanelName = "OptionsPanel";
    [Tooltip("Developers panel name")]
    public string developersPanelName = "DevelopersPanel";
    
    private GameObject mainPanel;
    private GameObject newGamePanel;
    private GameObject loadSavePanel;
    private GameObject optionsPanel;
    private GameObject developersPanel;
    
    
    void Start()
    {
        mainPanel = transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == mainPanelName)?.gameObject;

        if (!mainPanel)
        {
            Debug.LogError("MainPanel not found");
            return;
        }
        
        mainPanel.SetActive(true);
        
        newGamePanel = transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == newGamePanelName)?.gameObject;

        if (!newGamePanel)
        {
            Debug.LogError("NewGamePanel not found");
            return;
        }

        newGamePanel.SetActive(false);
        
        loadSavePanel = transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == loadSavePanelName)?.gameObject;

        if (!loadSavePanel)
        {
            Debug.LogError("LoadSavePanel not found");
            return;
        }
        
        loadSavePanel.SetActive(false);
        
        optionsPanel = transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == optionsPanelName)?.gameObject;

        if (!optionsPanel)
        {
            Debug.LogError("OptionsPanel not found");
            return;
        }
        
        optionsPanel.SetActive(false);
        
        developersPanel = transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == developersPanelName)?.gameObject;

        if (!developersPanel)
        {
            Debug.LogError("DevelopersPanel not found");
            return;
        }
        
        developersPanel.SetActive(false);

        Time.timeScale = 1f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void NewGame()
    {
        mainPanel.SetActive(false);
        newGamePanel.SetActive(true);
    }

    public void ConfirmNewGame()
    {
        SceneManager.LoadScene(1);
    }
    
    public void Load()
    {
        mainPanel.SetActive(false);
        loadSavePanel.SetActive(true);
        loadSavePanel.GetComponent<LoadSaveMenuController>().ShowLoadMenu();
    }

    public void Options()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }
    
    public void Developers()
    {
        mainPanel.SetActive(false);
        developersPanel.SetActive(true);
    }
    
    public void BackToMainMenu()
    {
        mainPanel.SetActive(true);
        newGamePanel.SetActive(false);
        loadSavePanel.SetActive(false);
        optionsPanel.SetActive(false);
        developersPanel.SetActive(false);
    }
    
    public void Quit()
    {
        Debug.Log("Quitting game...");
            
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}
