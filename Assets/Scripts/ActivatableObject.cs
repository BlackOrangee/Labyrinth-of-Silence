using UnityEngine;
using System.Collections;

namespace Assets.Scripts
{
    /// <summary>
    /// Activatable object (generator, lever, button, etc.)
    /// Gives player a virtual key and shows visual feedback
    /// </summary>
    public class ActivatableObject : MonoBehaviour, IInteractable
    {
        [Header("Identity")]
        [Tooltip("Object name for UI (Generator, Lever, etc.)")]
        [SerializeField] private string objectName = "Generator";

        [Header("Key Settings")]
        [Tooltip("Virtual key to give on activation (should be >= 100)")]
        [SerializeField] private KeyColorType keyToGive = KeyColorType.GeneratorPower;

        [Header("Interaction Settings")]
        [Tooltip("Interaction key")]
        [SerializeField] private KeyCode interactionKey = KeyCode.E;

        [Tooltip("Can be activated only once?")]
        [SerializeField] private bool oneTimeActivation = true;

        [Tooltip("Require specific key to activate?")]
        [SerializeField] private bool requireKey = false;

        [Tooltip("Required key to activate")]
        [SerializeField] private KeyColorType requiredKey = KeyColorType.None;

        [Header("Visual Feedback")]
        [Tooltip("Lights to enable on activation")]
        [SerializeField] private Light[] lightsToEnable;

        [Tooltip("GameObjects to enable on activation")]
        [SerializeField] private GameObject[] objectsToEnable;

        [Tooltip("GameObjects to disable on activation")]
        [SerializeField] private GameObject[] objectsToDisable;

        [Tooltip("Renderer to change material (optional)")]
        [SerializeField] private Renderer targetRenderer;

        [Tooltip("Material when activated (optional)")]
        [SerializeField] private Material activatedMaterial;

        [Tooltip("Emission color when activated")]
        [SerializeField] private Color emissionColor = Color.green;

        [Tooltip("Enable emission on activation?")]
        [SerializeField] private bool enableEmission = true;

        [Header("Audio")]
        [Tooltip("Sound when activated")]
        [SerializeField] private AudioClip activationSound;

        [Tooltip("Sound when already activated")]
        [SerializeField] private AudioClip alreadyActivatedSound;

        [Tooltip("Sound when locked")]
        [SerializeField] private AudioClip lockedSound;

        [Header("Animation")]
        [Tooltip("Animator component (optional)")]
        [SerializeField] private Animator animator;

        [Tooltip("Trigger parameter name for activation")]
        [SerializeField] private string activationTrigger = "Activate";

        [Header("Messages")]
        [Tooltip("Message when activated")]
        [SerializeField] private string activationMessage = "Generator activated!";

        [Tooltip("Message when already activated")]
        [SerializeField] private string alreadyActivatedMessage = "Already activated";

        [Tooltip("Message when locked")]
        [SerializeField] private string lockedMessage = "Requires key to activate";

        private bool isActivated = false;
        private SimpleInventory playerInventory;
        private AudioSource audioSource;
        private Material originalMaterial;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Save original material
            if (targetRenderer != null)
            {
                originalMaterial = targetRenderer.material;
            }

            // Initial state - everything disabled
            SetVisualState(false);
        }

        public string GetInteractText()
        {
            if (isActivated && oneTimeActivation)
            {
                return "";
            }

            if (requireKey && playerInventory != null && !playerInventory.HasKey(requiredKey))
            {
                return $"{objectName}: {lockedMessage}";
            }

            return $"{objectName}: Press [{interactionKey}] to activate";
        }

        public void Interact(GameObject actor)
        {
            // Already activated check
            if (isActivated && oneTimeActivation)
            {
                PlaySound(alreadyActivatedSound);
                ShowMessage(alreadyActivatedMessage);
                return;
            }

            // Get inventory
            if (playerInventory == null)
            {
                playerInventory = actor.GetComponent<SimpleInventory>();
            }

            // Check requirements
            if (requireKey)
            {
                if (playerInventory == null || !playerInventory.HasKey(requiredKey))
                {
                    PlaySound(lockedSound);
                    ShowMessage(lockedMessage);
                    return;
                }
            }

            // Activate
            Activate();
        }

        public void OnInteract(GameObject actor)
        {
            Interact(actor);
        }

        /// <summary>
        /// Activate the object
        /// </summary>
        private void Activate()
        {
            isActivated = true;

            // Give virtual key to player
            if (playerInventory != null && keyToGive != KeyColorType.None)
            {
                playerInventory.AddKey(keyToGive);
                Debug.Log($"[ActivatableObject] {objectName} activated! Gave key: {keyToGive}");
            }

            // Visual feedback
            SetVisualState(true);

            // Audio feedback
            PlaySound(activationSound);

            // Animation
            if (animator != null && !string.IsNullOrEmpty(activationTrigger))
            {
                animator.SetTrigger(activationTrigger);
            }

            // Message
            ShowMessage(activationMessage);
        }

        /// <summary>
        /// Set visual state (lights, materials, etc.)
        /// </summary>
        private void SetVisualState(bool active)
        {
            // Enable/disable lights
            if (lightsToEnable != null)
            {
                foreach (Light light in lightsToEnable)
                {
                    if (light != null)
                    {
                        light.enabled = active;
                    }
                }
            }

            // Enable objects
            if (objectsToEnable != null)
            {
                foreach (GameObject obj in objectsToEnable)
                {
                    if (obj != null)
                    {
                        obj.SetActive(active);
                    }
                }
            }

            // Disable objects
            if (objectsToDisable != null)
            {
                foreach (GameObject obj in objectsToDisable)
                {
                    if (obj != null)
                    {
                        obj.SetActive(!active);
                    }
                }
            }

            // Change material
            if (active && targetRenderer != null)
            {
                if (activatedMaterial != null)
                {
                    targetRenderer.material = activatedMaterial;
                }

                // Enable emission
                if (enableEmission)
                {
                    Material mat = targetRenderer.material;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emissionColor);
                }
            }
        }

        /// <summary>
        /// Play sound
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Show message to player (can be extended with UI system)
        /// </summary>
        private void ShowMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Debug.Log($"[ActivatableObject] {message}");
                // TODO: Integrate with your notification/message system if available
            }
        }

        /// <summary>
        /// Manual activation (for testing or triggers)
        /// </summary>
        public void ActivateManually()
        {
            if (!isActivated || !oneTimeActivation)
            {
                Activate();
            }
        }

        /// <summary>
        /// Check if object is activated
        /// </summary>
        public bool IsActivated()
        {
            return isActivated;
        }

        private void OnDrawGizmos()
        {
            // Visual indicator in editor
            if (isActivated)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.yellow;
            }

            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
