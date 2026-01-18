using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts
{
    /// <summary>
    /// ScriptableObject database that holds references to all newspapers in the game
    /// Used for save/load system to restore newspaper references by ID
    /// </summary>
    [CreateAssetMenu(fileName = "NewspaperDatabase", menuName = "Game/Newspaper Database")]
    public class NewspaperDatabase : ScriptableObject
    {
        [Header("Database")]
        [Tooltip("List of all newspapers in the game")]
        public List<NewspaperData> allNewspapers = new List<NewspaperData>();

        /// <summary>
        /// Get newspaper by ID
        /// </summary>
        public NewspaperData GetNewspaperById(string newspaperId)
        {
            if (string.IsNullOrEmpty(newspaperId))
                return null;

            return allNewspapers.Find(n => n != null && n.newspaperId == newspaperId);
        }

        /// <summary>
        /// Get multiple newspapers by IDs
        /// </summary>
        public List<NewspaperData> GetNewspapersByIds(List<string> newspaperIds)
        {
            List<NewspaperData> result = new List<NewspaperData>();

            if (newspaperIds == null || newspaperIds.Count == 0)
                return result;

            foreach (string id in newspaperIds)
            {
                NewspaperData newspaper = GetNewspaperById(id);
                if (newspaper != null)
                {
                    result.Add(newspaper);
                }
            }

            return result;
        }

        /// <summary>
        /// Check if newspaper exists in database
        /// </summary>
        public bool HasNewspaper(string newspaperId)
        {
            return GetNewspaperById(newspaperId) != null;
        }
    }
}
