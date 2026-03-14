using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Component for saving objects
    /// Add this component to any saveable object
    /// </summary>
    public class SaveableObject : MonoBehaviour
    {
        [Header("Identification")]
        [Tooltip("Unique ID for this object (generated automatically if empty)")]
        [SerializeField]
        private string uniqueId = "";

        [Tooltip("Automatically save this object")]
        public bool autoSave = true;

        [Tooltip("Save transform (position/rotation/scale). Enable for moving objects like enemies. Disable for static doors/items to reduce save file size.")]
        public bool saveTransform = true;

        public string UniqueId => uniqueId;

        private void Awake()
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                GenerateUniqueId();
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                GenerateUniqueId();
            }
        }

        private void GenerateUniqueId()
        {
            string hierarchyPath = GetHierarchyPath();
            uniqueId = $"{hierarchyPath}_{GetInstanceID()}";

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private string GetHierarchyPath()
        {
            string path = gameObject.name;
            Transform parent = transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        /// <summary>
        /// Set the unique ID for this object
        /// </summary>
        public void SetUniqueId(string id)
        {
            uniqueId = id;
        }
    }
}
