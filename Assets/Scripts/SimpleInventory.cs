using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

namespace Assets.Scripts
{
    public class SimpleInventory : MonoBehaviour
    {
        [SerializeField]
        private List<KeyColorType> collectedKeys = new List<KeyColorType>();
        private List<string> items = new List<string>();
        private List<NewspaperData> collectedNewspapers = new List<NewspaperData>();
        private List<string> pendingNewspaperIds = new List<string>();

        public static event Action OnInventoryChanged;

        private static string TransitFilePath =>
            Path.Combine(Application.persistentDataPath, "transit_inventory.json");

        [Serializable]
        private class TransitData
        {
            public List<string> items = new List<string>();
            public List<string> collectedKeyNames = new List<string>();
            public List<string> collectedNewspaperIds = new List<string>();
        }

        // Очищаем transit при каждом старте новой игровой сессии (в редакторе — при каждом Play)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ClearTransitOnSessionStart()
        {
            ClearTransit();
        }

        private void Awake()
        {
            LoadTransit();
        }

        private void Start()
        {
            ResolveNewspaperIds();
        }

        private void SaveTransit()
        {
            try
            {
                var data = new TransitData();
                data.items = new List<string>(items);
                foreach (var key in collectedKeys)
                    data.collectedKeyNames.Add(key.ToString());
                foreach (var n in collectedNewspapers)
                    if (n != null) data.collectedNewspaperIds.Add(n.newspaperId);
                // Газеты которые ещё не разрешены — тоже сохраняем
                foreach (var id in pendingNewspaperIds)
                    if (!data.collectedNewspaperIds.Contains(id))
                        data.collectedNewspaperIds.Add(id);

                File.WriteAllText(TransitFilePath, JsonUtility.ToJson(data));
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleInventory] Transit save failed: {e.Message}");
            }
        }

        private void LoadTransit()
        {
            if (!File.Exists(TransitFilePath)) return;
            try
            {
                var data = JsonUtility.FromJson<TransitData>(File.ReadAllText(TransitFilePath));
                if (data == null) return;

                items = data.items ?? new List<string>();

                collectedKeys.Clear();
                foreach (string keyName in data.collectedKeyNames ?? new List<string>())
                    if (Enum.TryParse(keyName, out KeyColorType keyType))
                        collectedKeys.Add(keyType);

                // Газеты разрешаем в Start() когда SaveManager уже готов
                pendingNewspaperIds = data.collectedNewspaperIds ?? new List<string>();

                Debug.Log($"[SimpleInventory] Transit loaded: {items.Count} items, {collectedKeys.Count} keys, {pendingNewspaperIds.Count} newspaper IDs pending.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleInventory] Transit load failed: {e.Message}");
            }
        }

        private NewspaperDatabase FindNewspaperDatabase()
        {
            if (SaveManager.Instance?.newspaperDatabase != null)
                return SaveManager.Instance.newspaperDatabase;
            if (InventoryUI.Instance?.newspaperDatabase != null)
                return InventoryUI.Instance.newspaperDatabase;
            return null;
        }

        private void ResolveNewspaperIds()
        {
            if (pendingNewspaperIds.Count == 0) return;

            NewspaperDatabase db = FindNewspaperDatabase();
            if (db == null) return;

            foreach (string id in pendingNewspaperIds)
            {
                NewspaperData newspaper = db.GetNewspaperById(id);
                if (newspaper != null && !collectedNewspapers.Contains(newspaper))
                    collectedNewspapers.Add(newspaper);
            }
            pendingNewspaperIds.Clear();

            Debug.Log($"[SimpleInventory] Resolved {collectedNewspapers.Count} newspapers.");
            OnInventoryChanged?.Invoke();
        }

        public static void ClearTransit()
        {
            try
            {
                if (File.Exists(TransitFilePath))
                    File.Delete(TransitFilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleInventory] Transit clear failed: {e.Message}");
            }
        }

        public void AddKey(KeyColorType keyType)
        {
            if (keyType == KeyColorType.None) return;
            if (!collectedKeys.Contains(keyType))
            {
                collectedKeys.Add(keyType);
                Debug.Log($"SimpleInventory: Picked up {keyType} key.");
                SaveTransit();
                OnInventoryChanged?.Invoke();
            }
        }

        public bool HasKey(KeyColorType keyType) => collectedKeys.Contains(keyType);

        public bool HasAllKeys(List<KeyColorType> requiredKeys)
        {
            foreach (var key in requiredKeys)
                if (!collectedKeys.Contains(key)) return false;
            return true;
        }

        public int GetCollectedKeysCount() => collectedKeys.Count;

        public List<string> GetItems() => new List<string>(items);

        public List<KeyColorType> GetCollectedKeys() => new List<KeyColorType>(collectedKeys);

        public void SetCollectedKeys(List<KeyColorType> keys)
        {
            collectedKeys.Clear();
            if (keys != null) collectedKeys.AddRange(keys);
            SaveTransit();
            OnInventoryChanged?.Invoke();
        }

        public void AddItem(string itemName)
        {
            if (!items.Contains(itemName))
            {
                items.Add(itemName);
                Debug.Log($"SimpleInventory: Added item '{itemName}'");
                SaveTransit();
                OnInventoryChanged?.Invoke();
            }
        }

        public void RemoveItem(string itemName)
        {
            if (items.Contains(itemName))
            {
                items.Remove(itemName);
                SaveTransit();
                OnInventoryChanged?.Invoke();
            }
        }

        public void AddNewspaper(NewspaperData newspaperData)
        {
            if (newspaperData == null)
            {
                Debug.LogWarning("SimpleInventory: Cannot add null newspaper.");
                return;
            }
            if (HasNewspaper(newspaperData.newspaperId))
            {
                Debug.Log($"SimpleInventory: Newspaper '{newspaperData.newspaperName}' already collected.");
                return;
            }
            collectedNewspapers.Add(newspaperData);
            Debug.Log($"SimpleInventory: Collected newspaper '{newspaperData.newspaperName}' (ID: {newspaperData.newspaperId})");
            SaveTransit();
            OnInventoryChanged?.Invoke();
        }

        public bool HasNewspaper(string newspaperId)
        {
            if (string.IsNullOrEmpty(newspaperId)) return false;
            return collectedNewspapers.Exists(n => n != null && n.newspaperId == newspaperId)
                || pendingNewspaperIds.Contains(newspaperId);
        }

        public List<NewspaperData> GetCollectedNewspapers()
        {
            if (pendingNewspaperIds.Count > 0) ResolveNewspaperIds();
            return new List<NewspaperData>(collectedNewspapers);
        }

        public NewspaperData GetNewspaperById(string newspaperId)
        {
            if (string.IsNullOrEmpty(newspaperId)) return null;
            if (pendingNewspaperIds.Count > 0) ResolveNewspaperIds();
            return collectedNewspapers.Find(n => n != null && n.newspaperId == newspaperId);
        }

        public int GetCollectedNewspapersCount() => collectedNewspapers.Count + pendingNewspaperIds.Count;

        public void ClearInventory()
        {
            collectedKeys.Clear();
            items.Clear();
            collectedNewspapers.Clear();
            pendingNewspaperIds.Clear();
            ClearTransit();
            OnInventoryChanged?.Invoke();
        }
    }
}