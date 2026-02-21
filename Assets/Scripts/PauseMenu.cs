using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cursor = UnityEngine.Cursor;


namespace Assets.Scripts
{
    /// <summary>
    /// Pause menu controller
    /// Press ESC to pause and show menu
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Pause Key")]
        public KeyCode pauseKey = KeyCode.Escape;
        [Tooltip("Main menu scene name")]
        public string mainMenuSceneName = "MainMenu";
        [Tooltip("Pause panel name")]
        public string pausePanelName = "PausePanel";
        [Tooltip("Load save panel name")]
        public string loadSavePanelName = "LoadSavePanel";
        [Tooltip("Options panel name")]
        public string optionsPanelName = "OptionsPanel";
        [Tooltip("Developers panel name")]
        public string developersPanelName = "DevelopersPanel";
        [Tooltip("Quit panel name")]
        public string quitPanelName = "QuitPanel";

        private bool isPaused = false;
        private GameObject pausePanel;
        private GameObject loadSavePanel;
        private GameObject optionsPanel;
        private GameObject developersPanel;
        private GameObject quitPanel;

        void Start()
        {
            pausePanel = transform.GetComponentsInChildren<Transform>(includeInactive: true)
                .FirstOrDefault(t => t.name == pausePanelName)?.gameObject;

            if (!pausePanel)
            {
                Debug.LogError("PausePanel not found");
                return;
            }

            pausePanel.SetActive(false);
            
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

            quitPanel = transform.GetComponentsInChildren<Transform>(includeInactive: true)
                .FirstOrDefault(t => t.name == quitPanelName)?.gameObject;

            if (!quitPanel)
            {
                Debug.LogError("QuitPanel not found");
                return;
            }

            quitPanel.SetActive(false);

            Time.timeScale = 1f;

            SetCursorState(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(pauseKey))
            {
                if (isPaused)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }

        public void PauseGame()
        {
            pausePanel.SetActive(true);

            Time.timeScale = 0f;
            isPaused = true;

            SetCursorState(true);

            SetUIAnimatorsUnscaledTime(true);

            Debug.Log("Game Paused");
        }

        public void ResumeGame()
        {
            pausePanel.SetActive(false);
            loadSavePanel.SetActive(false);
            optionsPanel.SetActive(false);
            developersPanel.SetActive(false);
            quitPanel.SetActive(false);

            Time.timeScale = 1f;
            isPaused = false;

            SetCursorState(false);

            SetUIAnimatorsUnscaledTime(false);

            Debug.Log("Game Resumed");
        }

        private void SetCursorState(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void SetUIAnimatorsUnscaledTime(bool unscaled)
        {
            Animator[] animators = GetComponentsInChildren<Animator>(includeInactive: true);

            foreach (Animator animator in animators)
            {
                if (animator != null)
                {
                    animator.updateMode = unscaled ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
                }
            }
        }

        public bool IsPaused()
        {
            return isPaused;
        }

        /// <summary>
        /// Quick save - Save to slot 0
        /// </summary>
        public void Save()
        {
            if (SaveManager.Instance != null)
            {
                Canvas canvasToHide = GetComponent<Canvas>();

                SaveManager.Instance.SaveGame(0, "Quick Save", null, canvasToHide);
                Debug.Log("Quick save created!");
            }
            else
            {
                Debug.LogError("SaveManager not found!");
            }
        }

        public void SaveAs()
        {
            pausePanel.SetActive(false);
            loadSavePanel.SetActive(true);
            loadSavePanel.GetComponent<LoadSaveMenuController>().ShowSaveMenu();
        }
        
        public void Load()
        {
            pausePanel.SetActive(false);
            loadSavePanel.SetActive(true);
            loadSavePanel.GetComponent<LoadSaveMenuController>().ShowLoadMenu();
        }

        public void Options()
        {
            pausePanel.SetActive(false);
            optionsPanel.SetActive(true);
        }

        public void Developers()
        {
            pausePanel.SetActive(false);
            developersPanel.SetActive(true);
        }

        public void ShowQuitPanel()
        {
            pausePanel.SetActive(false);
            quitPanel.SetActive(true);
        }

        public void SaveAndQuit()
        {
            if (SaveManager.Instance != null)
            {
                Canvas canvasToHide = GetComponent<Canvas>();
                SaveManager.Instance.SaveGame(0, "Quick Save", null, canvasToHide);
                Debug.Log("Quick save created before quitting!");
            }
            else
            {
                Debug.LogError("SaveManager not found!");
            }

            LoadMainMenu();
        }

        public void QuitWithoutSave()
        {
            LoadMainMenu();
        }

        public void CancelQuit()
        {
            quitPanel.SetActive(false);
            pausePanel.SetActive(true);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            string currentScene = SceneManager.GetActiveScene().name;
            LoadingScreenConfig config = SceneLoader.Instance.GetConfigForScene(currentScene);
            SceneLoader.Instance.LoadScene(currentScene, config);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            LoadingScreenConfig config = SceneLoader.Instance.GetConfigForScene(mainMenuSceneName);
            SceneLoader.Instance.LoadScene(mainMenuSceneName, config);
        }

        public void QuitGame()
        {
            Debug.Log("Quitting game...");
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void BackToPauseMenu()
        {
            pausePanel.SetActive(true);
            loadSavePanel.SetActive(false);
            optionsPanel.SetActive(false);
            developersPanel.SetActive(false);
            quitPanel.SetActive(false);
        }
    }
}
