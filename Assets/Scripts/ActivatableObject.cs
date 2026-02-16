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

        // [Header("Interaction Settings")]
        // [Tooltip("Interaction key")]
        // [SerializeField] private KeyCode interactionKey = KeyCode.E;

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

            if (targetRenderer != null)
            {
                originalMaterial = targetRenderer.material;
            }

            SetVisualState(false);
        }

        public void Interact(GameObject actor)
        {
            if (isActivated && oneTimeActivation)
            {
                PlaySound(alreadyActivatedSound);
                ShowMessage(alreadyActivatedMessage);
                return;
            }

            if (playerInventory == null)
            {
                playerInventory = actor.GetComponent<SimpleInventory>();
            }

            if (requireKey)
            {
                if (playerInventory == null || !playerInventory.HasKey(requiredKey))
                {
                    PlaySound(lockedSound);
                    ShowMessage(lockedMessage);
                    return;
                }
            }

            Activate();
        }

        public void OnInteract(GameObject actor)
        {
            Interact(actor);
        }

        public string GetPopupID()
        {
            return (isActivated && oneTimeActivation) ? "" : "turnOn";
        }

        /// <summary>
        /// Activate the object
        /// </summary>
        private void Activate()
        {
            isActivated = true;

            if (playerInventory != null && keyToGive != KeyColorType.None)
            {
                playerInventory.AddKey(keyToGive);
                Debug.Log($"[ActivatableObject] {objectName} activated! Gave key: {keyToGive}");
            }

            SetVisualState(true);

            PlaySound(activationSound);

            if (animator != null && !string.IsNullOrEmpty(activationTrigger))
            {
                animator.SetTrigger(activationTrigger);
            }

            ShowMessage(activationMessage);

            TriggerThought();
        }

        /// <summary>
        /// Set visual state (lights, materials, etc.)
        /// </summary>
        private void SetVisualState(bool active)
        {
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

            if (active && targetRenderer != null)
            {
                if (activatedMaterial != null)
                {
                    targetRenderer.material = activatedMaterial;
                }

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

        /// <summary>
        /// Trigger thought if ThoughtTrigger component is attached
        /// </summary>
        private void TriggerThought()
        {
            ThoughtTrigger thoughtTrigger = GetComponent<ThoughtTrigger>();
            if (thoughtTrigger != null)
            {
                thoughtTrigger.TriggerFromInteraction();
            }
        }

        private void OnDrawGizmos()
        {
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
