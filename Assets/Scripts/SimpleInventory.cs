using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts

{

    public class SimpleInventory : MonoBehaviour
    {
        private List<string> items = new List<string>();
        private int collectedKeys = 0;

        public void AddItem(string name)
        {
            items.Add(name);

            if (!string.IsNullOrEmpty(name))
            {
                string lower = name.ToLowerInvariant();
                if (lower.Contains("key"))
                {
                    collectedKeys++;

                    if (collectedKeys == 1 && QuestTracker.Instance != null)
                    {
                        if (!QuestTracker.Instance.HasQuest("collect_keys"))
                        {
                            QuestTracker.Instance.AddQuest(
                                "collect_keys",
                                "Collect all keys",
                                1,
                                4
                            );
                            Debug.Log("SimpleInventory: added quest 'collect_keys' after picking up first key.");
                        }
                    }
                }
            }

            Debug.Log($"SimpleInventory: added {name}. Total items: {items.Count}. Keys: {collectedKeys}");
        }

        public int GetCollectedKeysCount()
        {
            return collectedKeys;
        }

        public string[] GetItems()
        {
            return items.ToArray();
        }

        public void SetCollectedKeysCount(int count)
        {
            collectedKeys = Mathf.Max(0, count);
        }
    }
}