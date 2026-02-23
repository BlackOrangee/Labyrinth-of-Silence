using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Trigger for loading next scene through loading screen
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SceneTrigger : MonoBehaviour
    {
        [Header("Scene Settings")]
        [Tooltip("Load scene by name or index?")]
        public bool useSceneName = true;

        [Tooltip("Name of the scene to load")]
        public string targetSceneName = "Forest";

        [Tooltip("Build index of the scene to load (if not using scene name)")]
        public int targetSceneIndex = 1;

        [Tooltip("Custom loading screen config (optional)")]
        public LoadingScreenConfig customConfig;

        [Header("Trigger Settings")]
        [Tooltip("Require player to press button to activate?")]
        public bool requireInteraction = true;

        [Tooltip("Interaction key")]
        public KeyCode interactionKey = KeyCode.E;

        [Tooltip("Player tag to detect")]
        public string playerTag = "Player";

        [Header("UI Settings")]
        [Tooltip("Show prompt when player is near?")]
        public bool showPrompt = true;

        [Tooltip("Popup ID to show (e.g. 'open', 'turnOn', 'out'). Leave empty to disable.")]
        public string popupID = "open";

        [Tooltip("Thought to trigger when player tries to activate but requirements are not met (optional)")]
        public ThoughtTrigger lockedThought;

        [Tooltip("Require specific keys?")]
        public bool requireKeys = false;

        [Tooltip("Required key colors")]
        public KeyColorType[] requiredKeys;

        private bool playerInZone = false;
        private bool isLoading = false;

        private void Start()
        {
            // Ensure collider is trigger
            Collider col = GetComponent<Collider>();
            if (!col.isTrigger)
            {
                Debug.LogWarning($"[SceneTrigger] Collider on {gameObject.name} is not a trigger! Setting isTrigger = true.");
                col.isTrigger = true;
            }
        }

        private void Update()
        {
            if (playerInZone && !isLoading)
            {
                if (requireInteraction)
                {
                    if (Input.GetKeyDown(interactionKey))
                    {
                        TryLoadScene();
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                playerInZone = true;

                if (!requireInteraction && !isLoading)
                {
                    TryLoadScene();
                }
                else if (showPrompt)
                {
                    ShowPrompt();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                playerInZone = false;
                HidePrompt();
            }
        }

        private void TryLoadScene()
        {
            // Check requirements
            if (!CheckRequirements())
            {
                Debug.Log("[SceneTrigger] Requirements not met!");
                return;
            }

            isLoading = true;
            HidePrompt();

            // Load scene through SceneLoader
            if (SceneLoader.Instance != null)
            {
                LoadingScreenConfig config = customConfig;

                if (config == null)
                {
                    // Get config automatically
                    string sceneName = useSceneName ? targetSceneName : SceneLoader.Instance.GetSceneNameFromBuildIndex(targetSceneIndex);
                    config = SceneLoader.Instance.GetConfigForScene(sceneName);
                }

                if (useSceneName)
                {
                    Debug.Log($"[SceneTrigger] Loading scene: {targetSceneName}");
                    SceneLoader.Instance.LoadScene(targetSceneName, config);
                }
                else
                {
                    Debug.Log($"[SceneTrigger] Loading scene index: {targetSceneIndex}");
                    SceneLoader.Instance.LoadScene(targetSceneIndex, config);
                }
            }
            else
            {
                Debug.LogError("[SceneTrigger] SceneLoader not found!");
                isLoading = false;
            }
        }

        private bool CheckRequirements()
        {
            if (requireKeys && requiredKeys != null && requiredKeys.Length > 0)
            {
                SimpleInventory inventory = FindFirstObjectByType<SimpleInventory>();
                if (inventory == null)
                {
                    Debug.LogWarning("[SceneTrigger] SimpleInventory not found!");
                    return false;
                }

                foreach (KeyColorType keyColor in requiredKeys)
                {
                    if (!inventory.HasKey(keyColor))
                    {
                        Debug.Log($"[SceneTrigger] Missing required key: {keyColor}");
                        if (lockedThought != null)
                        {
                            lockedThought.TriggerFromInteraction();
                        }
                        return false;
                    }
                }
            }

            return true;
        }

        private void ShowPrompt()
        {
            if (!string.IsNullOrEmpty(popupID) && Localization.PopupManager.Instance != null)
            {
                Localization.PopupManager.Instance.ShowPopup(popupID, false, 0f);
            }
        }

        private void HidePrompt()
        {
            if (Localization.PopupManager.Instance != null)
            {
                Localization.PopupManager.Instance.HidePopup();
            }
        }

        private void OnDrawGizmos()
        {
            // Draw trigger zone in editor
            Gizmos.color = new Color(0, 1, 0, 0.3f);

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                if (col is BoxCollider)
                {
                    BoxCollider box = col as BoxCollider;
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(box.center, box.size);
                }
                else if (col is SphereCollider)
                {
                    SphereCollider sphere = col as SphereCollider;
                    Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
                }
            }
        }
    }
}
