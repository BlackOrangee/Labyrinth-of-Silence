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

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

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

            SaveAllSceneObjects(saveData);

            string screenshotPath = Path.Combine(screenshotFolderPath, $"save_{slotIndex}.png");
            yield return StartCoroutine(CaptureScreenshot(screenshotPath, uiToHide));
            saveData.screenshotPath = screenshotPath;

            string savePath = GetSaveFilePath(slotIndex);
            try
            {
                string json = JsonUtility.ToJson(saveData, true);
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

                LoadingScreenConfig config = SceneLoader.Instance.GetConfigForScene(saveData.sceneName);
                SceneLoader.Instance.LoadScene(saveData.sceneName, config);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load game: {e.Message}");
            }
        }

        private SaveData currentLoadData;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (currentLoadData != null)
            {
                StartCoroutine(ApplyLoadDataDelayed(currentLoadData));
                currentLoadData = null;
            }
        }

        private IEnumerator ApplyLoadDataDelayed(SaveData saveData)
        {
            yield return new WaitForEndOfFrame();

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

            SimpleInventory inventory = FindFirstObjectByType<SimpleInventory>();
            if (inventory != null)
            {
                inventory.ClearInventory();

                foreach (string item in saveData.inventoryData.items)
                {
                    // inventory.AddItem(item);
                }

                if (saveData.inventoryData.collectedKeys != null)
                {
                    inventory.SetCollectedKeys(saveData.inventoryData.collectedKeys);
                }

                if (saveData.inventoryData.collectedNewspaperIds != null && newspaperDatabase != null)
                {
                    List<NewspaperData> newspapers = newspaperDatabase.GetNewspapersByIds(saveData.inventoryData.collectedNewspaperIds);
                    foreach (NewspaperData newspaper in newspapers)
                    {
                        inventory.AddNewspaper(newspaper);
                    }
                }
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

            RestoreAllSceneObjects(saveData);

            Debug.Log($"[SaveManager] Save data applied successfully!");
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
            bool wasUIActive = false;
            if (uiToHide != null)
            {
                wasUIActive = uiToHide.enabled;
                uiToHide.enabled = false;
            }

            yield return null;

            yield return new WaitForEndOfFrame();

            Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenshot.Apply();

            if (uiToHide != null)
            {
                uiToHide.enabled = wasUIActive;
            }

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
            SaveableObject[] saveableObjects = FindObjectsByType<SaveableObject>(FindObjectsSortMode.None);

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
            // if (door != null)
            // {
            //     state.doorState = new DoorState
            //     {
            //         isOpen = (bool)typeof(DoorController).GetField("isOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(door),
            //         currentRotationY = door.doorPivot != null ? door.doorPivot.localRotation.eulerAngles.y : 0f
            //     };
            // }

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

            SaveableObject[] saveableObjects = FindObjectsByType<SaveableObject>(FindObjectsSortMode.None);
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
                // DoorController door = obj.GetComponent<DoorController>();
                // if (door != null)
                // {
                //     typeof(DoorController).GetField("isOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(door, state.doorState.isOpen);
                //
                //     if (door.doorPivot != null && state.doorState.isOpen)
                //     {
                //         door.doorPivot.localRotation = Quaternion.Euler(0, state.doorState.currentRotationY, 0);
                //     }
                // }
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
