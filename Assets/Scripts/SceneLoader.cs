using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts
{
    /// <summary>
    /// Scene to LoadingScreenConfig mapping
    /// </summary>
    [System.Serializable]
    public class SceneLoadingConfig
    {
        public string sceneName;
        public LoadingScreenConfig config;
    }

    /// <summary>
    /// Manages scene transitions with a loading screen
    /// [MODIFIED] Updated API calls to remove Obsolete warnings
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        #region Singleton

        private static SceneLoader instance;

        public static SceneLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("SceneLoader");
                    instance = go.AddComponent<SceneLoader>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        #endregion

        [Header("Settings")]
        [Tooltip("Name of the loading screen scene")]
        public string loadingSceneName = "Loader";

        [Tooltip("Minimum time to show loading screen (in seconds)")]
        public float minimumLoadTime = 3.0f;

        [Tooltip("Delay before starting to load the target scene (in seconds)")]
        public float loadStartDelay = 0.2f;

        [Header("Loading Screen Configurations")]
        [Tooltip("Default configuration if no specific config is found")]
        public LoadingScreenConfig defaultConfig;

        [Tooltip("All available loading screen configs (will be auto-matched by scene name/index)")]
        public LoadingScreenConfig[] availableConfigs;

        [Tooltip("Scene-specific loading configurations (manual override)")]
        public List<SceneLoadingConfig> sceneConfigs = new List<SceneLoadingConfig>();

        private string targetSceneName;
        private AsyncOperation loadingOperation;
        private LoadingScreenConfig currentConfig;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Load a scene by name with loading screen
        /// </summary>
        /// <param name="sceneName">Name of the scene to load</param>
        /// <param name="customConfig">Optional custom configuration for this load</param>
        public void LoadScene(string sceneName, LoadingScreenConfig customConfig = null)
        {
            targetSceneName = sceneName;

            if (customConfig != null)
            {
                currentConfig = customConfig;
            }
            else
            {
                currentConfig = GetConfigForScene(sceneName);
            }

            StartCoroutine(LoadSceneCoroutine());
        }

        /// <summary>
        /// Load a scene by build index with loading screen
        /// </summary>
        /// <param name="sceneIndex">Build index of the scene to load</param>
        /// <param name="customConfig">Optional custom configuration for this load</param>
        public void LoadScene(int sceneIndex, LoadingScreenConfig customConfig = null)
        {
            if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"[SceneLoader] Invalid scene index: {sceneIndex}");
                return;
            }

            targetSceneName = GetSceneNameFromBuildIndex(sceneIndex);

            if (customConfig != null)
            {
                currentConfig = customConfig;
            }
            else
            {
                currentConfig = GetConfigForScene(targetSceneName);
            }

            StartCoroutine(LoadSceneCoroutine());
        }

        /// <summary>
        /// Get loading progress (0 to 1)
        /// </summary>
        public float GetLoadingProgress()
        {
            if (loadingOperation != null)
            {
                return Mathf.Clamp01(loadingOperation.progress / 0.9f);
            }

            return 0f;
        }

        private IEnumerator LoadSceneCoroutine()
        {
            float startTime = Time.time;

            AsyncOperation loadingScreenOperation = SceneManager.LoadSceneAsync(loadingSceneName);

            if (loadingScreenOperation == null)
            {
                yield break;
            }

            yield return loadingScreenOperation;

            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(loadStartDelay);

            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            int sceneObjectCount = 0;

            foreach (GameObject obj in allObjects)
            {
                if (obj.scene.name == loadingSceneName)
                {
                    sceneObjectCount++;
                }
            }
            
            Assets.Scripts.LoadingScreen loadingScreen = null;

            Assets.Scripts.LoadingScreen[] allLoadingScreens = Resources.FindObjectsOfTypeAll<Assets.Scripts.LoadingScreen>();
            Debug.Log($"[SceneLoader] Found {allLoadingScreens.Length} LoadingScreen components total");

            foreach (var screen in allLoadingScreens)
            {
                if (screen.gameObject.scene.name == loadingSceneName)
                {
                    loadingScreen = screen;
                    break;
                }
            }

            if (loadingScreen == null)
            {
                foreach (GameObject obj in allObjects)
                {
                    if (obj.scene.name == loadingSceneName)
                    {
                        var comp = obj.GetComponent<Assets.Scripts.LoadingScreen>();
                        if (comp != null)
                        {
                            loadingScreen = comp;
                            break;
                        }
                    }
                }
            }

            if (loadingScreen == null)
            {
                Debug.LogWarning("[SceneLoader] LoadingScreen not found in scene! Creating dynamically...");

                // [ВИПРАВЛЕНО] Замінено застарілий FindObjectOfType на FindFirstObjectByType
                Canvas canvas = Object.FindFirstObjectByType<Canvas>();
                
                if (canvas != null)
                {
                    Image background = null;
                    Text tipText = null;
                    Image loadingIcon = null;

                    foreach (GameObject obj in allObjects)
                    {
                        if (obj.scene.name != loadingSceneName) continue;

                        if (obj.name.Contains("LoaderImage") || obj.name.Contains("Background"))
                        {
                            loadingIcon = obj.GetComponent<Image>();
                        }
                        else if (obj.name.Contains("Text") || obj.name.Contains("Tip"))
                        {
                            tipText = obj.GetComponent<Text>();
                        }
                        else if (obj.name.Contains("Image") && !obj.name.Contains("Loader"))
                        {
                            if (background == null)
                                background = obj.GetComponent<Image>();
                        }
                    }

                    GameObject manager = new GameObject("LoadingScreenManager_Dynamic");
                    loadingScreen = manager.AddComponent<Assets.Scripts.LoadingScreen>();

                    if (background != null)
                    {
                        typeof(Assets.Scripts.LoadingScreen).GetField("backgroundImage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.SetValue(loadingScreen, background);
                    }
                    if (tipText != null)
                    {
                        typeof(Assets.Scripts.LoadingScreen).GetField("tipText", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.SetValue(loadingScreen, tipText);
                    }
                    if (loadingIcon != null)
                    {
                        typeof(Assets.Scripts.LoadingScreen).GetField("loadingIcon", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.SetValue(loadingScreen, loadingIcon);
                    }

                    Debug.Log("[SceneLoader] ✓ LoadingScreen created dynamically!");

                    yield return null;
                }
            }

            if (loadingScreen != null && currentConfig != null)
            {
                loadingScreen.ApplyConfiguration(currentConfig);
            }

            loadingOperation = SceneManager.LoadSceneAsync(targetSceneName);

            if (loadingOperation == null)
            {
                yield break;
            }

            loadingOperation.allowSceneActivation = false;

            while (loadingOperation.progress < 0.9f)
            {
                yield return null;
            }

            float elapsedTime = Time.time - startTime;
            if (elapsedTime < minimumLoadTime)
            {
                yield return new WaitForSeconds(minimumLoadTime - elapsedTime);
            }

            loadingOperation.allowSceneActivation = true;

            yield return loadingOperation;

            loadingOperation = null;
            currentConfig = null;

        }

        public string GetSceneNameFromBuildIndex(int buildIndex)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            return sceneName;
        }

        /// <summary>
        /// Get configuration for a specific scene (PUBLIC for manual use)
        /// </summary>
        public LoadingScreenConfig GetConfigForScene(string sceneName)
        {
            foreach (SceneLoadingConfig sceneConfig in sceneConfigs)
            {
                if (sceneConfig.sceneName == sceneName && sceneConfig.config != null)
                {
                    return sceneConfig.config;
                }
            }

            int sceneIndex = GetSceneIndex(sceneName);
            if (availableConfigs != null && availableConfigs.Length > 0)
            {
                LoadingScreenConfig matchedConfig = FindConfigByPattern(sceneName, sceneIndex);
                if (matchedConfig != null)
                {
                    return matchedConfig;
                }
            }

            if (defaultConfig != null)
            {
                return defaultConfig;
            }

            return null;
        }

        /// <summary>
        /// Find config by parsing name pattern: {index}_{name}_LoadingScreenConfig
        /// </summary>
        private LoadingScreenConfig FindConfigByPattern(string sceneName, int sceneIndex)
        {
            if (availableConfigs == null || availableConfigs.Length == 0)
            {
                return null;
            }

            foreach (LoadingScreenConfig config in availableConfigs)
            {
                if (config == null)
                {
                    continue;
                }

                string configName = config.name;
                Debug.Log($"[SceneLoader] Checking config: '{configName}'");

                string[] parts = configName.Split('_');

                if (parts.Length >= 2)
                {
                    if (int.TryParse(parts[0], out int configIndex))
                    {
                        if (configIndex == sceneIndex)
                        {
                            return config;
                        }
                    }

                    if (parts[1].Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return config;
                    }
                }

                if (configName.Contains(sceneName))
                {
                    return config;
                }
            }

            return null;
        }

        /// <summary>
        /// Get scene build index by name
        /// </summary>
        private int GetSceneIndex(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name == sceneName)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}