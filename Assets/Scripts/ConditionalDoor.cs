using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

namespace Assets.Scripts
{
    /// <summary>
    /// Door with conditions (physical door animation + key requirements)
    /// Can require virtual keys (like GeneratorPower)
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ConditionalDoor : MonoBehaviour, IInteractable
    {
        [Header("Door Identity")]
        [Tooltip("Door name for UI")]
        [SerializeField] private string doorName = "Door";

        [Header("Requirements")]
        [Tooltip("Required keys (can include virtual keys)")]
        [SerializeField] private KeyColorType[] requiredKeys;

        [Tooltip("Allow opening if ANY key is present (OR logic)")]
        [SerializeField] private bool requireAnyKey = false;

        [Header("Door Settings")]
        [Tooltip("Door opening angle")]
        [SerializeField] private float openAngle = 90f;

        [Tooltip("Door opening speed")]
        [SerializeField] private float openSpeed = 2f;

        [Tooltip("Rotation axis (0,1,0 = Y, 1,0,0 = X, 0,0,1 = Z)")]
        [SerializeField] private Vector3 rotationAxis = new Vector3(0, 1, 0);

        [Tooltip("Door pivot transform")]
        [SerializeField] private Transform doorPivot;

        [Tooltip("Auto-close door after opening?")]
        [SerializeField] private bool autoClose = false;

        [Tooltip("Time before auto-closing (seconds)")]
        [SerializeField] private float autoCloseDelay = 3f;

        [Header("Audio")]
        [Tooltip("Sound when door opens")]
        [SerializeField] private AudioClip openSound;

        [Tooltip("Sound when door closes")]
        [SerializeField] private AudioClip closeSound;

        [Tooltip("Sound when door is locked")]
        [SerializeField] private AudioClip lockedSound;

        [Header("Visual Feedback")]
        [Tooltip("Light to enable when unlocked")]
        [SerializeField] private Light indicatorLight;

        [Tooltip("Color when locked")]
        [SerializeField] private Color lockedColor = Color.red;

        [Tooltip("Color when unlocked")]
        [SerializeField] private Color unlockedColor = Color.green;

        [FormerlySerializedAs("lockedMessage")]
        [Header("Messages")]
        [Tooltip("Message when locked")]
        [SerializeField] private string lockedMessageID = "doorLocked";

        [FormerlySerializedAs("unlockedMessage")]
        [Tooltip("Message when unlocked")]
        [SerializeField] private string popupID = "open";

        [Header("Task System (Завдання дверей)")]
        [Tooltip("Що додати, якщо гравець підійшов до ЗАЧИНЕННИХ дверей? (напр: find_key)")]
        [SerializeField] private string taskToAddIfLocked;
        
        [Tooltip("Текст англійською")]
        [SerializeField] private string taskTextEngIfLocked;
        
        [Tooltip("Текст українською")]
        [SerializeField] private string taskTextUkrIfLocked;
        
        [Tooltip("Що видалити, коли двері ВІДЧИНЯЮТЬСЯ? (напр: find_key, next_block) - можна вписати декілька через кому")]
        [SerializeField] private string tasksToRemoveOnOpen;


        private bool isOpen = false;
        private bool isLocked = true;
        private SimpleInventory playerInventory;
        private AudioSource audioSource;
        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Coroutine doorCoroutine;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();

            if (doorPivot == null)
            {
                doorPivot = transform;
            }

            closedRotation = doorPivot.localRotation;
            openRotation = closedRotation * Quaternion.AngleAxis(openAngle, rotationAxis);

            // Check if door should start unlocked
            CheckLockState();

            UpdateIndicator();
        }

        private void Update()
        {
            // Continuously check lock state (in case keys are collected)
            if (isLocked)
            {
                CheckLockState();
            }
        }

        public void Interact(GameObject actor)
        {
            // Get inventory
            if (playerInventory == null)
            {
                playerInventory = actor.GetComponent<SimpleInventory>();
            }

            // Check if door is locked
            if (isLocked)
            {
                // Try to unlock
                if (CheckRequirements())
                {
                    Unlock();
                    OpenDoor();
                }
                else
                {
                    PlaySound(lockedSound);
                    ShowLockedMessage();

                    if (TaskManager.Instance != null && !string.IsNullOrEmpty(taskToAddIfLocked) && !string.IsNullOrEmpty(taskTextEngIfLocked))
                    {
                        TaskManager.Instance.AddTask(taskToAddIfLocked, taskTextEngIfLocked, taskTextUkrIfLocked);
                    }
                }
            }
            else
            {
                // Door is unlocked, toggle open/close
                if (isOpen)
                {
                    CloseDoor();
                }
                else
                {
                    OpenDoor();
                }
            }
        }

        public void OnInteract(GameObject actor)
        {
            Interact(actor);
        }

        public string GetPopupID()
        {
            if (isOpen) return "";
            return isLocked ? lockedMessageID : popupID;
        }

        /// <summary>
        /// Check if player has all required keys
        /// </summary>
        private bool CheckRequirements()
        {
            if (playerInventory == null)
            {
                return false;
            }

            if (requiredKeys == null || requiredKeys.Length == 0)
            {
                return true; // No keys required
            }

            if (requireAnyKey)
            {
                // OR logic - player needs at least one key
                foreach (KeyColorType key in requiredKeys)
                {
                    if (playerInventory.HasKey(key))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                // AND logic - player needs all keys
                foreach (KeyColorType key in requiredKeys)
                {
                    if (!playerInventory.HasKey(key))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Check lock state based on player inventory
        /// </summary>
        private void CheckLockState()
        {
            if (playerInventory == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerInventory = player.GetComponent<SimpleInventory>();
                }
            }

            if (CheckRequirements())
            {
                if (isLocked)
                {
                    Unlock();
                }
            }
        }

        /// <summary>
        /// Unlock the door
        /// </summary>
        private void Unlock()
        {
            isLocked = false;
            UpdateIndicator();
            Debug.Log($"[ConditionalDoor] {doorName} unlocked!");
        }

        /// <summary>
        /// Open the door
        /// </summary>
        private void OpenDoor()
        {
            if (isOpen) return;

            isOpen = true;
            PlaySound(openSound);

            if (TaskManager.Instance != null && !string.IsNullOrEmpty(tasksToRemoveOnOpen))
            {
                string[] tasks = tasksToRemoveOnOpen.Split(',');
                foreach (string task in tasks)
                {
                    TaskManager.Instance.RemoveTask(task.Trim()); 
                }
            }

            if (doorCoroutine != null)
            {
                StopCoroutine(doorCoroutine);
            }
            doorCoroutine = StartCoroutine(AnimateDoor(openRotation));

            if (autoClose)
            {
                StartCoroutine(AutoCloseRoutine());
            }
        }

        /// <summary>
        /// Close the door
        /// </summary>
        private void CloseDoor()
        {
            if (!isOpen) return;

            isOpen = false;
            PlaySound(closeSound);

            if (doorCoroutine != null)
            {
                StopCoroutine(doorCoroutine);
            }
            doorCoroutine = StartCoroutine(AnimateDoor(closedRotation));
        }

        /// <summary>
        /// Animate door rotation
        /// </summary>
        private IEnumerator AnimateDoor(Quaternion targetRotation)
        {
            Quaternion startRotation = doorPivot.localRotation;
            float time = 0f;

            while (time < 1f)
            {
                time += Time.deltaTime * openSpeed;
                doorPivot.localRotation = Quaternion.Slerp(startRotation, targetRotation, time);
                yield return null;
            }

            doorPivot.localRotation = targetRotation;
            doorCoroutine = null;
        }

        /// <summary>
        /// Auto-close routine
        /// </summary>
        private IEnumerator AutoCloseRoutine()
        {
            yield return new WaitForSeconds(autoCloseDelay);
            if (isOpen)
            {
                CloseDoor();
            }
        }

        /// <summary>
        /// Update indicator light
        /// </summary>
        private void UpdateIndicator()
        {
            if (indicatorLight != null)
            {
                indicatorLight.color = isLocked ? lockedColor : unlockedColor;
                indicatorLight.enabled = true;
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
        /// Show locked message
        /// </summary>
        private void ShowLockedMessage()
        {
            string missingKeys = GetMissingKeysMessage();
            Debug.Log($"[ConditionalDoor] {doorName} is locked! {missingKeys}");

            if (Localization.PopupManager.Instance != null)
            {
                Localization.PopupManager.Instance.ShowPopup(lockedMessageID, false, 1f);
            }
        }

        /// <summary>
        /// Get message about missing keys
        /// </summary>
        private string GetMissingKeysMessage()
        {
            if (playerInventory == null || requiredKeys == null || requiredKeys.Length == 0)
            {
                return "";
            }

            string message = "Missing keys: ";
            bool first = true;

            foreach (KeyColorType key in requiredKeys)
            {
                if (!playerInventory.HasKey(key))
                {
                    if (!first) message += ", ";
                    message += key.ToString();
                    first = false;
                }
            }

            return message;
        }

        /// <summary>
        /// Force open (for scripts/cutscenes — bypasses lock/key requirements)
        /// </summary>
        public void ForceOpen()
        {
            isLocked = false;
            OpenDoor();
        }

        /// <summary>
        /// Manual unlock (for scripts/triggers)
        /// </summary>
        public void UnlockManually()
        {
            Unlock();
        }

        /// <summary>
        /// Manual lock (for scripts/triggers)
        /// </summary>
        public void LockManually()
        {
            isLocked = true;
            UpdateIndicator();
        }

        /// <summary>
        /// Check if door is locked
        /// </summary>
        public bool IsLocked()
        {
            return isLocked;
        }

        /// <summary>
        /// Check if door is open
        /// </summary>
        public bool IsOpen()
        {
            return isOpen;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isLocked ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }
}