using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts
{
    public class SaveManager : MonoBehaviour
    {
        #region Singleton
        private static SaveManager instance;
        public static SaveManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SaveManager>();
                }

                if (instance == null)
                {
                    GameObject go = new GameObject("SaveManager");
                    instance = go.AddComponent<SaveManager>();
                }

                return instance;
            }
        }
        #endregion

        public const int AutoSaveSlotIndex = 0;
        public const int QuickSaveSlotIndex = 1;
        public const int FirstManualSlotIndex = 2;

        [Header("Settings")]
        [Tooltip("Maximum number of save slots")]
        public int maxSaveSlots = 10;

        [Tooltip("Screenshot width")]
        public int screenshotWidth = 320;

        [Tooltip("Screenshot height")]
        public int screenshotHeight = 180;

        [Header("Databases")]
        [Tooltip("Database containing all newspapers in the game")]
        public NewspaperDatabase newspaperDatabase;

        private string saveFolderPath => Path.Combine(Application.persistentDataPath, "Saves");
        private string screenshotFolderPath => Path.Combine(Application.persistentDataPath, "Screenshots");

        [Header("Autosave")]
        [Tooltip("Minimum seconds between autosaves")]
        public float autoSaveCooldown = 5f;

        private Canvas loadOverlayCanvas;
        private UnityEngine.UI.Image loadOverlayImage;
        private float _lastAutoSaveTime = -999f;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateLoadOverlay();

            if (!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
            }

            if (!Directory.Exists(screenshotFolderPath))
            {
                Directory.CreateDirectory(screenshotFolderPath);
            }

            Debug.Log($"[SaveManager] Initialized. Save folder: {saveFolderPath}");
        }

        private void CreateLoadOverlay()
        {
            GameObject go = new GameObject("SaveManager_LoadOverlay");
            DontDestroyOnLoad(go);

            loadOverlayCanvas = go.AddComponent<Canvas>();
            loadOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            loadOverlayCanvas.sortingOrder = 9999;
            go.AddComponent<UnityEngine.UI.CanvasScaler>();
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(go.transform, false);
            loadOverlayImage = panel.AddComponent<UnityEngine.UI.Image>();
            loadOverlayImage.color = Color.black;
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            go.SetActive(false);
        }

        #region Save/Load Methods

        /// <summary>
        /// Save game to specified slot
        /// </summary>
        public void SaveGame(int slotIndex, string customName = "", System.Action onSaveComplete = null, Canvas uiToHide = null)
        {
            if (slotIndex < 0 || slotIndex >= maxSaveSlots)
            {
                Debug.LogError($"[SaveManager] Invalid slot index: {slotIndex}");
                return;
            }

            StartCoroutine(SaveGameCoroutine(slotIndex, customName, onSaveComplete, uiToHide));
        }

        private IEnumerator SaveGameCoroutine(int slotIndex, string customName, System.Action onSaveComplete, Canvas uiToHide)
        {
            Debug.Log($"[SaveManager] Saving game to slot {slotIndex}...");

            SaveData saveData = new SaveData();

            if (!string.IsNullOrEmpty(customName))
            {
                saveData.saveName = customName;
            }
            else
            {
                saveData.saveName = $"Save {slotIndex + 1}";
            }

            saveData.sceneName = SceneManager.GetActiveScene().name;
            saveData.sceneBuildIndex = SceneManager.GetActiveScene().buildIndex;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                saveData.playerData = new PlayerData(player.transform);
            }

            SimpleInventory inventory = FindFirstObjectByType<SimpleInventory>();
            if (inventory != null)
            {
                saveData.inventoryData.items = new List<string>(inventory.GetItems());
                saveData.inventoryData.collectedKeys = new List<KeyColorType>(inventory.GetCollectedKeys());

                List<NewspaperData> newspapers = inventory.GetCollectedNewspapers();
                saveData.inventoryData.collectedNewspaperIds = new List<string>();
                foreach (NewspaperData newspaper in newspapers)
                {
                    if (newspaper != null)
                    {
                        saveData.inventoryData.collectedNewspaperIds.Add(newspaper.newspaperId);
                    }
                }
            }

            // if (QuestTracker.Instance != null)
            // {
            //     List<Quest> activeQuests = QuestTracker.Instance.GetAllQuests();
            //     foreach (Quest quest in activeQuests)
            //     {
            //         saveData.questsData.Add(new QuestData(quest));
            //     }
            //
            //     if (activeQuests.Count > 0)
            //     {
            //         saveData.currentQuest = activeQuests[0].description;
            //     }
            // }

            SanityController sanity = FindFirstObjectByType<SanityController>();
            if (sanity != null)
            {
                var sanityField = typeof(SanityController).GetField("_currentSanity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (sanityField != null)
                    saveData.playerData.currentSanity = (float)sanityField.GetValue(sanity);
            }

            SaveAllSceneObjects(saveData);

            string screenshotPath = Path.Combine(screenshotFolderPath, $"save_{slotIndex}.png");

            var canvasesToHide = new List<Canvas>();
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.enabled && c.isRootCanvas)
                    canvasesToHide.Add(c);
            if (uiToHide != null && !canvasesToHide.Contains(uiToHide))
                canvasesToHide.Add(uiToHide);

            yield return StartCoroutine(CaptureScreenshot(screenshotPath, canvasesToHide.ToArray()));
            saveData.screenshotPath = screenshotPath;

            string savePath = GetSaveFilePath(slotIndex);
            try
            {
                string json = JsonUtility.ToJson(saveData, false);
                File.WriteAllText(savePath, json);
                Debug.Log($"[SaveManager] Game saved to slot {slotIndex}: {savePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save game: {e.Message}");
            }

            onSaveComplete?.Invoke();
        }

        /// <summary>
        /// Load game from specified slot
        /// </summary>
        public void LoadGame(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= maxSaveSlots)
            {
                Debug.LogError($"[SaveManager] Invalid slot index: {slotIndex}");
                return;
            }

            string savePath = GetSaveFilePath(slotIndex);
            if (!File.Exists(savePath))
            {
                Debug.LogWarning($"[SaveManager] Save file not found: {savePath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(savePath);
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                Debug.Log($"[SaveManager] Loading game from slot {slotIndex}...");

                currentLoadData = saveData;

                Time.timeScale = 1f;
                SimpleInventory.ClearTransit();
                LampController.ClearPlayerTransit();

                LoadingScreenConfig config = SceneLoader.Instance.GetConfigForScene(saveData.sceneName);
                if (saveData.sceneBuildIndex >= 0)
                    SceneLoader.Instance.LoadScene(saveData.sceneBuildIndex, config);
                else
                    SceneLoader.Instance.LoadScene(saveData.sceneName, config);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load game: {e.Message}");
            }
        }

        private SaveData currentLoadData;

        public static bool IsLoadingGame { get; private set; }

        #region Checkpoint

        private static string CheckpointPath =>
            Path.Combine(Application.persistentDataPath, "checkpoint.json");

        private void SaveCheckpoint()
        {
            try
            {
                SaveData saveData = new SaveData();
                saveData.sceneName = SceneManager.GetActiveScene().name;
                saveData.sceneBuildIndex = SceneManager.GetActiveScene().buildIndex;

                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    saveData.playerData = new PlayerData(player.transform);

                SanityController sanity = FindFirstObjectByType<SanityController>();
                if (sanity != null)
                {
                    var f = typeof(SanityController).GetField("_currentSanity",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (f != null)
                        saveData.playerData.currentSanity = (float)f.GetValue(sanity);
                }

                SimpleInventory inventory = FindFirstObjectByType<SimpleInventory>(FindObjectsInactive.Include);
                if (inventory != null)
                {
                    saveData.inventoryData.items = new List<string>(inventory.GetItems());
                    saveData.inventoryData.collectedKeys = new List<KeyColorType>(inventory.GetCollectedKeys());
                    saveData.inventoryData.collectedNewspaperIds = new List<string>();
                    foreach (var n in inventory.GetCollectedNewspapers())
                        if (n != null) saveData.inventoryData.collectedNewspaperIds.Add(n.newspaperId);
                }

                SaveAllSceneObjects(saveData);

                File.WriteAllText(CheckpointPath, JsonUtility.ToJson(saveData, false));
                Debug.Log($"[SaveManager] Checkpoint saved for scene: {saveData.sceneName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Checkpoint save failed: {e.Message}");
            }
        }

        private IEnumerator SaveCheckpointDelayed(string sceneName)
        {
            yield return null;
            yield return null;
            while (IsLoadingGame) yield return null;
            yield return null;
            if (SceneManager.GetActiveScene().name == sceneName)
            {
                SaveCheckpoint();
                _lastAutoSaveTime = -999f;
                TriggerAutoSave();
            }
        }

        public void LoadCheckpoint()
        {
            if (!File.Exists(CheckpointPath))
            {
                Debug.LogWarning("[SaveManager] No checkpoint found — fresh restart.");
                SimpleInventory.ClearTransit();
                LampController.ClearPlayerTransit();
                Time.timeScale = 1f;
                string scene = SceneManager.GetActiveScene().name;
                LoadingScreenConfig cfg = SceneLoader.Instance.GetConfigForScene(scene);
                SceneLoader.Instance.LoadScene(scene, cfg);
                return;
            }

            try
            {
                SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(CheckpointPath));
                currentLoadData = saveData;
                Time.timeScale = 1f;
                SimpleInventory.ClearTransit();
                LampController.ClearPlayerTransit();
                LoadingScreenConfig config = SceneLoader.Instance.GetConfigForScene(saveData.sceneName);
                if (saveData.sceneBuildIndex >= 0)
                    SceneLoader.Instance.LoadScene(saveData.sceneBuildIndex, config);
                else
                    SceneLoader.Instance.LoadScene(saveData.sceneName, config);
                Debug.Log("[SaveManager] Loading from checkpoint...");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Checkpoint load failed: {e.Message}");
            }
        }

        #endregion

        #region Autosave

        public void TriggerAutoSave()
        {
            if (IsLoadingGame) return;
            if (Time.time - _lastAutoSaveTime < autoSaveCooldown) return;
            _lastAutoSaveTime = Time.time;
            StartCoroutine(AutoSaveCoroutine());
        }

        private IEnumerator AutoSaveCoroutine()
        {
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var activeRootCanvases = new List<Canvas>();
            foreach (var c in allCanvases)
            {
                if (c.enabled && c.isRootCanvas)
                    activeRootCanvases.Add(c);
            }

            string screenshotPath = Path.Combine(screenshotFolderPath, $"save_{AutoSaveSlotIndex}.png");
            yield return StartCoroutine(CaptureScreenshot(screenshotPath, activeRootCanvases.ToArray()));

            try
            {
                SaveData saveData = new SaveData();
                saveData.saveName = "Autosave";
                saveData.saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                saveData.sceneName = SceneManager.GetActiveScene().name;
                saveData.sceneBuildIndex = SceneManager.GetActiveScene().buildIndex;
                saveData.screenshotPath = screenshotPath;

                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    saveData.playerData = new PlayerData(player.transform);

                SanityController sanity = FindFirstObjectByType<SanityController>();
                if (sanity != null)
                {
                    var f = typeof(SanityController).GetField("_currentSanity",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (f != null)
                        saveData.playerData.currentSanity = (float)f.GetValue(sanity);
                }

                SimpleInventory inventory = FindFirstObjectByType<SimpleInventory>(FindObjectsInactive.Include);
                if (inventory != null)
                {
                    saveData.inventoryData.items = new List<string>(inventory.GetItems());
                    saveData.inventoryData.collectedKeys = new List<KeyColorType>(inventory.GetCollectedKeys());
                    saveData.inventoryData.collectedNewspaperIds = new List<string>();
                    foreach (var n in inventory.GetCollectedNewspapers())
                        if (n != null) saveData.inventoryData.collectedNewspaperIds.Add(n.newspaperId);
                }

                SaveAllSceneObjects(saveData);

                string savePath = GetSaveFilePath(AutoSaveSlotIndex);
                File.WriteAllText(savePath, JsonUtility.ToJson(saveData, false));
                Debug.Log($"[SaveManager] Autosave → slot {AutoSaveSlotIndex}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Autosave failed: {e.Message}");
            }
        }

        #endregion

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SimpleInventory.OnInventoryChanged += TriggerAutoSave;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SimpleInventory.OnInventoryChanged -= TriggerAutoSave;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(SaveCheckpointDelayed(scene.name));

            if (currentLoadData == null) return;

            bool isTarget = currentLoadData.sceneBuildIndex >= 0
                ? scene.buildIndex == currentLoadData.sceneBuildIndex
                : scene.name == currentLoadData.sceneName;

            if (isTarget)
            {
                IsLoadingGame = true;

                if (loadOverlayCanvas != null)
                {
                    loadOverlayCanvas.gameObject.SetActive(true);
                    loadOverlayImage.color = Color.black;
                }

                StartCoroutine(ApplyLoadDataDelayed(currentLoadData));
                currentLoadData = null;
            }
        }

        private IEnumerator ApplyLoadDataDelayed(SaveData saveData)
        {
            yield return null;
            yield return null;

            Debug.Log($"[SaveManager] Applying save data...");

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                CharacterController controller = player.GetComponent<CharacterController>();
                if (controller != null)
                {
                    controller.enabled = false;
                }

                player.transform.position = saveData.playerData.GetPosition();
                player.transform.rotation = saveData.playerData.GetRotation();

                if (controller != null)
                {
                    controller.enabled = true;
                }
            }

            SimpleInventory inventory = FindFirstObjectByType<SimpleInventory>(FindObjectsInactive.Include);
            Debug.Log($"[SaveManager] Save data contents — items: [{string.Join(", ", saveData.inventoryData.items)}], keys: [{string.Join(", ", saveData.inventoryData.collectedKeys)}], newspapers: [{string.Join(", ", saveData.inventoryData.collectedNewspaperIds)}]");
            if (inventory != null)
            {
                inventory.ClearInventory();

                foreach (string item in saveData.inventoryData.items)
                {
                    inventory.AddItem(item);
                }

                if (saveData.inventoryData.collectedKeys != null)
                {
                    inventory.SetCollectedKeys(saveData.inventoryData.collectedKeys);
                }

                if (saveData.inventoryData.collectedNewspaperIds != null && saveData.inventoryData.collectedNewspaperIds.Count > 0)
                {
                    if (newspaperDatabase != null)
                    {
                        List<NewspaperData> newspapers = newspaperDatabase.GetNewspapersByIds(saveData.inventoryData.collectedNewspaperIds);
                        foreach (NewspaperData newspaper in newspapers)
                            inventory.AddNewspaper(newspaper);
                    }
                    else
                    {
                        Debug.LogWarning("[SaveManager] newspaperDatabase не назначена — газеты восстановлены как pending IDs");
                        inventory.RestoreNewspaperIds(saveData.inventoryData.collectedNewspaperIds);
                    }
                }
            }
            else
            {
                Debug.LogWarning("[SaveManager] SimpleInventory not found — inventory not restored!");
            }

            // if (QuestTracker.Instance != null)
            // {
            //     QuestTracker.Instance.ClearAllQuests();
            //
            //     foreach (QuestData questData in saveData.questsData)
            //     {
            //         QuestTracker.Instance.AddQuest(
            //             questData.questId,
            //             questData.description,
            //             questData.currentProgress,
            //             questData.targetProgress
            //         );
            //
            //         if (questData.isCompleted)
            //         {
            //             QuestTracker.Instance.CompleteQuest(questData.questId);
            //         }
            //     }
            // }

            if (saveData.playerData.currentSanity >= 0f)
            {
                SanityController sanity = FindFirstObjectByType<SanityController>();
                if (sanity != null)
                {
                    typeof(SanityController).GetField("_currentSanity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(sanity, saveData.playerData.currentSanity);
                }
            }

            RestoreAllSceneObjects(saveData);

            if (inventory != null)
                HideAlreadyCollectedItems(inventory);

            IsLoadingGame = false;

            Debug.Log($"[SaveManager] Save data applied successfully!");

            yield return StartCoroutine(FadeOutLoadOverlay());
        }

        private IEnumerator FadeOutLoadOverlay()
        {
            if (loadOverlayCanvas == null) yield break;

            float duration = 0.4f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                Color c = loadOverlayImage.color;
                c.a = 1f - Mathf.Clamp01(t / duration);
                loadOverlayImage.color = c;
                yield return null;
            }

            loadOverlayCanvas.gameObject.SetActive(false);
        }

        /// <summary>
        /// Delete save from specified slot
        /// </summary>
        public void DeleteSave(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= maxSaveSlots)
            {
                Debug.LogError($"[SaveManager] Invalid slot index: {slotIndex}");
                return;
            }

            string savePath = GetSaveFilePath(slotIndex);
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
                Debug.Log($"[SaveManager] Deleted save from slot {slotIndex}");
            }

            string screenshotPath = Path.Combine(screenshotFolderPath, $"save_{slotIndex}.png");
            if (File.Exists(screenshotPath))
            {
                File.Delete(screenshotPath);
            }
        }

        #endregion

        #region Screenshot Methods

        private IEnumerator CaptureScreenshot(string path, Canvas uiToHide)
        {
            yield return StartCoroutine(CaptureScreenshot(path, uiToHide != null ? new Canvas[] { uiToHide } : null));
        }

        private IEnumerator CaptureScreenshot(string path, Canvas[] uiToHide)
        {
            var hiddenCanvases = new List<Canvas>();
            if (uiToHide != null)
            {
                foreach (var canvas in uiToHide)
                {
                    if (canvas != null && canvas.enabled)
                    {
                        canvas.enabled = false;
                        hiddenCanvases.Add(canvas);
                    }
                }
            }

            yield return null;
            yield return new WaitForEndOfFrame();

            Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenshot.Apply();

            foreach (var canvas in hiddenCanvases)
                if (canvas != null) canvas.enabled = true;

            Texture2D resized = ResizeTexture(screenshot, screenshotWidth, screenshotHeight);

            byte[] bytes = resized.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            Destroy(screenshot);
            Destroy(resized);

            Debug.Log($"[SaveManager] Screenshot saved: {path}");
        }

        private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            RenderTexture.active = rt;

            Graphics.Blit(source, rt);

            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }

        #endregion

        #region Scene Objects Save/Load

        /// <summary>
        /// Saves all SaveableObject objects in the scene
        /// </summary>
        private void SaveAllSceneObjects(SaveData saveData)
        {
            SaveableObject[] saveableObjects = FindObjectsByType<SaveableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Debug.Log($"[SaveManager] Found {saveableObjects.Length} saveable objects in scene");

            foreach (SaveableObject saveableObj in saveableObjects)
            {
                if (saveableObj == null || !saveableObj.autoSave)
                {
                    continue;
                }

                GameObjectState state = new GameObjectState
                {
                    uniqueId = saveableObj.UniqueId,
                    objectName = saveableObj.gameObject.name,
                    isActive = saveableObj.gameObject.activeSelf,
                    transformData = new TransformData(saveableObj.transform)
                };

                SaveComponentStates(saveableObj.gameObject, state);

                saveData.gameObjectStates.Add(state);
            }

            Debug.Log($"[SaveManager] Saved {saveData.gameObjectStates.Count} object states");
        }

        /// <summary>
        /// Saves the states of all components of an object
        /// </summary>
        private void SaveComponentStates(GameObject obj, GameObjectState state)
        {
            DoorController door = obj.GetComponent<DoorController>();
            if (door != null)
            {
                var isOpenField = typeof(DoorController).GetField("isOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var pivotField = typeof(DoorController).GetField("doorPivot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Transform pivot = pivotField?.GetValue(door) as Transform;
                Quaternion rot = pivot != null ? pivot.localRotation : Quaternion.identity;
                state.doorState = new DoorState
                {
                    isOpen = (bool)(isOpenField?.GetValue(door) ?? false),
                    pivotRotX = rot.x, pivotRotY = rot.y, pivotRotZ = rot.z, pivotRotW = rot.w
                };
            }

            if (state.doorState == null)
            {
                ConditionalDoor cDoor = obj.GetComponent<ConditionalDoor>();
                if (cDoor != null)
                {
                    var isOpenField = typeof(ConditionalDoor).GetField("isOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var isLockedField = typeof(ConditionalDoor).GetField("isLocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var pivotField = typeof(ConditionalDoor).GetField("doorPivot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Transform pivot = pivotField?.GetValue(cDoor) as Transform;
                    Quaternion rot = pivot != null ? pivot.localRotation : Quaternion.identity;
                    state.doorState = new DoorState
                    {
                        isOpen = (bool)(isOpenField?.GetValue(cDoor) ?? false),
                        isLocked = (bool)(isLockedField?.GetValue(cDoor) ?? true),
                        pivotRotX = rot.x, pivotRotY = rot.y, pivotRotZ = rot.z, pivotRotW = rot.w
                    };
                }
            }

            if (state.doorState == null)
            {
                DoorOpen doorOpen = obj.GetComponent<DoorOpen>();
                if (doorOpen != null)
                {
                    var isOpenField = typeof(DoorOpen).GetField("isOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Quaternion rot = doorOpen.doorPivot != null ? doorOpen.doorPivot.localRotation : Quaternion.identity;
                    state.doorState = new DoorState
                    {
                        isOpen = (bool)(isOpenField?.GetValue(doorOpen) ?? false),
                        pivotRotX = rot.x, pivotRotY = rot.y, pivotRotZ = rot.z, pivotRotW = rot.w
                    };
                }
            }

            if (state.doorState == null)
            {
                DoorOpen2 doorOpen2 = obj.GetComponent<DoorOpen2>();
                if (doorOpen2 != null)
                {
                    var isOpenField = typeof(DoorOpen2).GetField("isOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Quaternion rotA = doorOpen2.doorPivotA != null ? doorOpen2.doorPivotA.localRotation : Quaternion.identity;
                    Quaternion rotB = doorOpen2.doorPivotB != null ? doorOpen2.doorPivotB.localRotation : Quaternion.identity;
                    state.doorState = new DoorState
                    {
                        isOpen = (bool)(isOpenField?.GetValue(doorOpen2) ?? false),
                        pivotRotX = rotA.x, pivotRotY = rotA.y, pivotRotZ = rotA.z, pivotRotW = rotA.w,
                        hasPivotB = true,
                        pivotBRotX = rotB.x, pivotBRotY = rotB.y, pivotBRotZ = rotB.z, pivotBRotW = rotB.w
                    };
                }
            }

            EnemyAI enemy = obj.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                var stateField = typeof(EnemyAI).GetField("currentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var patrolIndexField = typeof(EnemyAI).GetField("currentPatrolIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var waitTimerField = typeof(EnemyAI).GetField("waitTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var lastKnownPosField = typeof(EnemyAI).GetField("lastKnownPlayerPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                state.enemyState = new EnemyState
                {
                    currentState = stateField?.GetValue(enemy).ToString(),
                    currentPatrolIndex = (int)(patrolIndexField?.GetValue(enemy) ?? 0),
                    waitTimer = (float)(waitTimerField?.GetValue(enemy) ?? 0f),
                    lastKnownPlayerPosition = new Vector3Data((Vector3)(lastKnownPosField?.GetValue(enemy) ?? Vector3.zero))
                };
            }

            CollectItem collectItem = obj.GetComponent<CollectItem>();
            if (collectItem != null)
            {
                var isCollectedField = typeof(CollectItem).GetField("isCollected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                state.collectItemState = new CollectItemState
                {
                    isCollected = (bool)(isCollectedField?.GetValue(collectItem) ?? false)
                };
            }

            LanternController lantern = obj.GetComponent<LanternController>();
            if (lantern != null)
            {
                var targetRangeField = typeof(LanternController).GetField("targetRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var modeIndexField = typeof(LanternController).GetField("currentModeIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                state.lanternState = new LanternState
                {
                    currentRange = (float)(targetRangeField?.GetValue(lantern) ?? 7f),
                    currentIntensity = lantern.CurrentIntensity,
                    currentModeIndex = (int)(modeIndexField?.GetValue(lantern) ?? 1)
                };
            }

            LampController lamp = obj.GetComponent<LampController>();
            if (lamp != null)
            {
                var fuelField = typeof(LampController).GetField("currentFuel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var modeField = typeof(LampController).GetField("lightMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                state.lampState = new LampState
                {
                    currentFuel = (float)(fuelField?.GetValue(lamp) ?? lamp.maxFuel),
                    lightMode = (int)(modeField?.GetValue(lamp) ?? 0)
                };
            }

            ActivatableObject activatable = obj.GetComponent<ActivatableObject>();
            if (activatable != null)
            {
                state.activatableState = new ActivatableState
                {
                    isActivated = activatable.IsActivated()
                };
            }

            GeneratorTask generator = obj.GetComponent<GeneratorTask>();
            if (generator != null)
            {
                var isRepairedField = typeof(GeneratorTask).GetField("isRepaired", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var hasFuelField = typeof(GeneratorTask).GetField("hasFuel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                state.generatorTaskState = new GeneratorTaskState
                {
                    isRepaired = (bool)(isRepairedField?.GetValue(generator) ?? false),
                    hasFuel = (bool)(hasFuelField?.GetValue(generator) ?? false)
                };
            }

            KeypadController keypad = obj.GetComponent<KeypadController>();
            if (keypad != null)
            {
                state.keypadState = new KeypadState { isUnlocked = keypad.IsLocked };
            }

            FuelCanister fuelCanister = obj.GetComponent<FuelCanister>();
            if (fuelCanister != null)
            {
                var capacityField = typeof(FuelCanister).GetField("canisterCapacity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                state.fuelCanisterState = new FuelCanisterState
                {
                    canisterCapacity = (float)(capacityField?.GetValue(fuelCanister) ?? 0f)
                };
            }

            NewspaperPickup newspaperPickup = obj.GetComponent<NewspaperPickup>();
            if (newspaperPickup != null)
            {
                var isPickedUpField = typeof(NewspaperPickup).GetField("isPickedUp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                state.newspaperPickupState = new NewspaperPickupState
                {
                    isPickedUp = (bool)(isPickedUpField?.GetValue(newspaperPickup) ?? false)
                };
            }

            ThoughtTrigger thoughtTrigger = obj.GetComponent<ThoughtTrigger>();
            if (thoughtTrigger != null)
            {
                var triggeredField = typeof(ThoughtTrigger).GetField("hasBeenTriggered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                state.thoughtTriggerState = new ThoughtTriggerState
                {
                    hasBeenTriggered = (bool)(triggeredField?.GetValue(thoughtTrigger) ?? false)
                };
            }
        }

        /// <summary>
        /// Restores all SaveableObject objects in the scene
        /// </summary>
        private void RestoreAllSceneObjects(SaveData saveData)
        {
            if (saveData.gameObjectStates == null || saveData.gameObjectStates.Count == 0)
            {
                Debug.Log("[SaveManager] No object states to restore");
                return;
            }

            SaveableObject[] saveableObjects = FindObjectsByType<SaveableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var objectsById = new System.Collections.Generic.Dictionary<string, SaveableObject>();

            foreach (SaveableObject obj in saveableObjects)
            {
                if (obj != null)
                {
                    objectsById[obj.UniqueId] = obj;
                }
            }

            Debug.Log($"[SaveManager] Found {objectsById.Count} saveable objects in scene for restoration");

            int restoredCount = 0;
            foreach (GameObjectState state in saveData.gameObjectStates)
            {
                if (objectsById.TryGetValue(state.uniqueId, out SaveableObject saveableObj))
                {
                    RestoreGameObjectState(saveableObj.gameObject, state);
                    restoredCount++;
                }
                else
                {
                    Debug.LogWarning($"[SaveManager] Could not find object with ID: {state.uniqueId} ({state.objectName})");
                }
            }

            Debug.Log($"[SaveManager] Restored {restoredCount} object states");
        }

        /// <summary>
        /// Restores the state of a single GameObject from a GameObjectState object
        /// </summary>
        private void RestoreGameObjectState(GameObject obj, GameObjectState state)
        {
            obj.SetActive(state.isActive);

            if (state.transformData != null)
            {
                obj.transform.position = state.transformData.GetPosition();
                obj.transform.rotation = state.transformData.GetRotation();
                obj.transform.localScale = state.transformData.GetScale();
            }

            RestoreComponentStates(obj, state);
        }

        /// <summary>
        /// Restores the state of all components of a single GameObject from a GameObjectState object
        /// </summary>
        private void RestoreComponentStates(GameObject obj, GameObjectState state)
        {
            if (state.doorState != null)
            {
                Quaternion pivotRot = new Quaternion(
                    state.doorState.pivotRotX, state.doorState.pivotRotY,
                    state.doorState.pivotRotZ, state.doorState.pivotRotW);

                DoorController door = obj.GetComponent<DoorController>();
                if (door != null)
                {
                    typeof(DoorController).GetField("isOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(door, state.doorState.isOpen);
                    var pivotField = typeof(DoorController).GetField("doorPivot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Transform pivot = pivotField?.GetValue(door) as Transform;
                    if (pivot != null) pivot.localRotation = pivotRot;
                }

                ConditionalDoor cDoor = obj.GetComponent<ConditionalDoor>();
                if (cDoor != null)
                {
                    typeof(ConditionalDoor).GetField("isOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(cDoor, state.doorState.isOpen);
                    typeof(ConditionalDoor).GetField("isLocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(cDoor, state.doorState.isLocked);
                    var pivotField = typeof(ConditionalDoor).GetField("doorPivot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Transform pivot = pivotField?.GetValue(cDoor) as Transform;
                    if (pivot != null) pivot.localRotation = pivotRot;
                    typeof(ConditionalDoor).GetMethod("UpdateIndicator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.Invoke(cDoor, null);
                }

                DoorOpen doorOpen = obj.GetComponent<DoorOpen>();
                if (doorOpen != null)
                {
                    typeof(DoorOpen).GetField("isOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(doorOpen, state.doorState.isOpen);
                    if (doorOpen.doorPivot != null) doorOpen.doorPivot.localRotation = pivotRot;
                }

                DoorOpen2 doorOpen2 = obj.GetComponent<DoorOpen2>();
                if (doorOpen2 != null)
                {
                    typeof(DoorOpen2).GetField("isOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(doorOpen2, state.doorState.isOpen);
                    if (doorOpen2.doorPivotA != null) doorOpen2.doorPivotA.localRotation = pivotRot;
                    if (state.doorState.hasPivotB && doorOpen2.doorPivotB != null)
                    {
                        doorOpen2.doorPivotB.localRotation = new Quaternion(
                            state.doorState.pivotBRotX, state.doorState.pivotBRotY,
                            state.doorState.pivotBRotZ, state.doorState.pivotBRotW);
                    }
                }
            }

            if (state.enemyState != null)
            {
                EnemyAI enemy = obj.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    if (System.Enum.TryParse(state.enemyState.currentState, out EnemyAI.EnemyState enemyState))
                    {
                        typeof(EnemyAI).GetField("currentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemy, enemyState);
                    }

                    typeof(EnemyAI).GetField("currentPatrolIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemy, state.enemyState.currentPatrolIndex);
                    typeof(EnemyAI).GetField("waitTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemy, state.enemyState.waitTimer);

                    if (state.enemyState.lastKnownPlayerPosition != null)
                    {
                        typeof(EnemyAI).GetField("lastKnownPlayerPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemy, state.enemyState.lastKnownPlayerPosition.ToVector3());
                    }
                }
            }

            if (state.collectItemState != null)
            {
                CollectItem collectItem = obj.GetComponent<CollectItem>();
                if (collectItem != null && state.collectItemState.isCollected)
                {
                    var colliderComp = obj.GetComponent<Collider>();
                    if (colliderComp != null)
                    {
                        colliderComp.enabled = false;
                    }

                    typeof(CollectItem).GetField("isCollected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(collectItem, true);

                    // if (collectItem.destroyOnCollect)
                    // {
                    //     Destroy(obj);
                    // }
                    // else
                    // {
                        var renderer = obj.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.enabled = false;
                        }

                        foreach (Transform t in obj.transform)
                        {
                            t.gameObject.SetActive(false);
                        }
                    // }
                }
            }

            if (state.lanternState != null)
            {
                LanternController lantern = obj.GetComponent<LanternController>();
                if (lantern != null)
                {
                    typeof(LanternController).GetField("targetRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(lantern, state.lanternState.currentRange);
                    typeof(LanternController).GetField("currentModeIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(lantern, state.lanternState.currentModeIndex);

                    lantern.SetRange(state.lanternState.currentRange);
                    lantern.SetIntensity(state.lanternState.currentIntensity);
                }
            }

            if (state.lampState != null)
            {
                LampController lamp = obj.GetComponent<LampController>();
                if (lamp != null)
                {
                    typeof(LampController).GetField("currentFuel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(lamp, state.lampState.currentFuel);
                    typeof(LampController).GetField("lightMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(lamp, state.lampState.lightMode);
                    typeof(LampController).GetMethod("UpdateLightTargets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.Invoke(lamp, null);
                    typeof(LampController).GetMethod("UpdateUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.Invoke(lamp, null);
                }
            }

            if (state.activatableState != null && state.activatableState.isActivated)
            {
                ActivatableObject activatable = obj.GetComponent<ActivatableObject>();
                activatable?.RestoreActivatedState();
            }

            if (state.generatorTaskState != null)
            {
                GeneratorTask generator = obj.GetComponent<GeneratorTask>();
                generator?.RestoreState(state.generatorTaskState.isRepaired, state.generatorTaskState.hasFuel);
            }

            if (state.keypadState != null && state.keypadState.isUnlocked)
            {
                KeypadController keypad = obj.GetComponent<KeypadController>();
                keypad?.RestoreUnlockedState();
            }

            if (state.fuelCanisterState != null)
            {
                FuelCanister fuelCanister = obj.GetComponent<FuelCanister>();
                if (fuelCanister != null)
                {
                    var capacityField = typeof(FuelCanister).GetField("canisterCapacity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    capacityField?.SetValue(fuelCanister, state.fuelCanisterState.canisterCapacity);
                    // Если канистра пуста — скрываем (она была уничтожена в игре)
                    if (state.fuelCanisterState.canisterCapacity <= 0.1f)
                        obj.SetActive(false);
                }
            }

            if (state.newspaperPickupState != null && state.newspaperPickupState.isPickedUp)
            {
                NewspaperPickup np = obj.GetComponent<NewspaperPickup>();
                if (np != null)
                {
                    var isPickedUpField = typeof(NewspaperPickup).GetField("isPickedUp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    isPickedUpField?.SetValue(np, true);
                    var col = obj.GetComponent<Collider>();
                    if (col != null) col.enabled = false;
                    obj.SetActive(false);
                }
            }

            if (state.thoughtTriggerState != null)
            {
                ThoughtTrigger thoughtTrigger = obj.GetComponent<ThoughtTrigger>();
                if (thoughtTrigger != null)
                {
                    var triggeredField = typeof(ThoughtTrigger).GetField("hasBeenTriggered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    triggeredField?.SetValue(thoughtTrigger, state.thoughtTriggerState.hasBeenTriggered);
                }
            }
        }

        /// <summary>
        /// Hides CollectItem objects whose content already exists in the restored inventory.
        /// Covers items that were destroyed on pickup (destroyOnCollect=true) and therefore
        /// have no SaveableObject entry in the save file — they respawn fresh on scene load.
        /// </summary>
        private void HideAlreadyCollectedItems(SimpleInventory inventory)
        {
            List<string> inventoryItems = inventory.GetItems();
            List<KeyColorType> inventoryKeys = inventory.GetCollectedKeys();
            List<NewspaperData> inventoryNewspapers = inventory.GetCollectedNewspapers();
            HashSet<string> newspaperIds = new HashSet<string>();
            foreach (var n in inventoryNewspapers)
                if (n != null) newspaperIds.Add(n.newspaperId);

            NewspaperPickup[] allNewspapers = FindObjectsByType<NewspaperPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (NewspaperPickup np in allNewspapers)
            {
                if (!np.gameObject.activeSelf) continue;
                if (np.newspaperData == null) continue;
                if (!newspaperIds.Contains(np.newspaperData.newspaperId)) continue;

                var isPickedUpField = typeof(NewspaperPickup).GetField("isPickedUp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if ((bool)(isPickedUpField?.GetValue(np) ?? false)) continue;

                isPickedUpField?.SetValue(np, true);
                var col = np.GetComponent<Collider>();
                if (col != null) col.enabled = false;
                np.gameObject.SetActive(false);
                Debug.Log($"[SaveManager] HideCollected: {np.gameObject.name} (newspaper already in inventory)");
            }

            CollectItem[] allCollectItems = FindObjectsByType<CollectItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            var keyColorField = typeof(CollectItem).GetField("keyColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var displayNameField = typeof(CollectItem).GetField("displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isCollectedField = typeof(CollectItem).GetField("isCollected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Debug.Log($"[SaveManager] HideCollected: {allCollectItems.Length} CollectItems in scene, inventory has {inventoryItems.Count} items / {inventoryKeys.Count} keys");

            foreach (CollectItem ci in allCollectItems)
            {
                if (keyColorField == null || displayNameField == null || isCollectedField == null) break;

                if ((bool)isCollectedField.GetValue(ci)) continue;
                if (!ci.gameObject.activeSelf) continue;

                KeyColorType keyColor = (KeyColorType)keyColorField.GetValue(ci);
                string displayName = (string)displayNameField.GetValue(ci);

                bool inInventory = keyColor != KeyColorType.None
                    ? inventoryKeys.Contains(keyColor)
                    : !string.IsNullOrEmpty(displayName) && inventoryItems.Contains(displayName);

                if (!inInventory) continue;

                isCollectedField.SetValue(ci, true);
                var collider = ci.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
                var renderer = ci.GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = false;
                foreach (Transform child in ci.transform)
                    child.gameObject.SetActive(false);
                ci.gameObject.SetActive(false);

                Debug.Log($"[SaveManager] HideCollected: {ci.gameObject.name} (already in inventory)");
            }
        }

        #endregion

        #region Utility Methods

        private string GetSaveFilePath(int slotIndex)
        {
            return Path.Combine(saveFolderPath, $"save_{slotIndex}.json");
        }

        /// <summary>
        /// Checks if a save exists in the specified slot
        /// </summary>
        public bool SaveExists(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= maxSaveSlots)
            {
                return false;
            }

            return File.Exists(GetSaveFilePath(slotIndex));
        }

        /// <summary>
        /// Gets the save data for the specified slot
        /// </summary>
        public SaveData GetSaveData(int slotIndex)
        {
            if (!SaveExists(slotIndex))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(GetSaveFilePath(slotIndex));
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to read save data: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the screenshot path for the specified slot
        /// </summary>
        public string GetScreenshotPath(int slotIndex)
        {
            return Path.Combine(screenshotFolderPath, $"save_{slotIndex}.png");
        }

        /// <summary>
        /// Gets the list of used save slots
        /// </summary>
        public List<int> GetUsedSlots()
        {
            List<int> usedSlots = new List<int>();
            for (int i = 0; i < maxSaveSlots; i++)
            {
                if (SaveExists(i))
                {
                    usedSlots.Add(i);
                }
            }
            return usedSlots;
        }

        #endregion
    }
}
