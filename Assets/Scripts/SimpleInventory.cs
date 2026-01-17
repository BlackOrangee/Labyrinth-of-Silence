using UnityEngine;
using System.Collections.Generic;
using System;

namespace Assets.Scripts
{
    public class SimpleInventory : MonoBehaviour
    {
        [SerializeField]
        private List<KeyColorType> collectedKeys = new List<KeyColorType>();
        private List<string> items = new List<string>();

        public static event Action OnInventoryChanged;

        public void AddKey(KeyColorType keyType)
        {
            if (keyType == KeyColorType.None) return;

            if (!collectedKeys.Contains(keyType))
            {
                collectedKeys.Add(keyType);
                Debug.Log($"SimpleInventory: Picked up {keyType} key.");

                OnInventoryChanged?.Invoke();
            }
        }

        public bool HasKey(KeyColorType keyType)
        {
            return collectedKeys.Contains(keyType);
        }

        public bool HasAllKeys(List<KeyColorType> requiredKeys)
        {
            foreach (var key in requiredKeys)
            {
                if (!collectedKeys.Contains(key))
                    return false;
            }
            return true;
        }

        public int GetCollectedKeysCount()
        {
            return collectedKeys.Count;
        }

        public List<string> GetItems()
        {
            return new List<string>(items);
        }

        public List<KeyColorType> GetCollectedKeys()
        {
            return new List<KeyColorType>(collectedKeys);
        }

        public void SetCollectedKeys(List<KeyColorType> keys)
        {
            collectedKeys.Clear();
            if (keys != null)
            {
                collectedKeys.AddRange(keys);
            }
            OnInventoryChanged?.Invoke();
        }

        public void ClearInventory()
        {
            collectedKeys.Clear();
            items.Clear();
            OnInventoryChanged?.Invoke();
        }
    }
}