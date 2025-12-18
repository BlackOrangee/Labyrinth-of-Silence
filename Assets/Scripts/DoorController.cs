using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Assets.Scripts
{
    [RequireComponent(typeof(AudioSource))]
    public class DoorController : MonoBehaviour, IInteractable
    {
        [Header("Door Identity")]
        [Tooltip("Назва дверей (DOOR1, DOOR2). Відображається в підказці.")]
        public string doorName = "Door"; 

        [Header("Door Requirements")]
        [Tooltip("Список кольорів ключів, необхідних для цих дверей")]
        public List<KeyColorType> requiredKeys; 

        [Header("Door Settings")]
        public float openAngle = 90f;
        public float openSpeed = 2f;
        public Transform doorPivot;

        [Header("Audio Settings")]
        public AudioClip openSound;
        public AudioClip lockedSound;

        [Header("Dependencies")]
        public PopupManager popupManager;
        public GameObject levelCompletePanel; 

        private bool isOpen = false;
        private SimpleInventory playerInventory;
        private AudioSource audioSource;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();

            if (popupManager == null)
                popupManager = FindFirstObjectByType<PopupManager>();
            
            var player = GameObject.FindWithTag("Player");
            if (player != null) playerInventory = player.GetComponent<SimpleInventory>();
        }

        public string GetInteractText()
        {
            if (isOpen) return "";

            if (playerInventory != null && playerInventory.HasAllKeys(requiredKeys))
            {
                return $"{doorName}: Keys collected - Press E to Open";
            }
            else
            {
                return $"{doorName} Locked. Check Tasks.";
            }
        }

        public void Interact(GameObject actor)
        {
            if (isOpen) return;
            
            if (playerInventory == null) 
                playerInventory = actor.GetComponent<SimpleInventory>();

            if (playerInventory != null && playerInventory.HasAllKeys(requiredKeys))
            {
                OpenDoor();
            }
            else
            {
                PlaySound(lockedSound);

                if (popupManager != null)
                {
                    popupManager.ShowPopup($"You need specific keys to open {doorName}!", null, this, "OK", null);
                    StartCoroutine(AutoClosePopup());
                }
            }
        }

        public void OnInteract(GameObject actor)
        {
            Interact(actor);
        }

        private void OpenDoor()
        {
            isOpen = true;

            PlaySound(openSound);

            StartCoroutine(RotateDoorRoutine());
            
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(true);
                StartCoroutine(HideLevelCompleteAfterTime());
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private IEnumerator RotateDoorRoutine()
        {
            Quaternion startRot = doorPivot.localRotation;
            Quaternion targetRot = Quaternion.Euler(0, openAngle, 0);
            float time = 0;
            while (time < 1)
            {
                time += Time.deltaTime * openSpeed;
                doorPivot.localRotation = Quaternion.Slerp(startRot, targetRot, time);
                yield return null;
            }
        }

        private IEnumerator HideLevelCompleteAfterTime()
        {
            yield return new WaitForSeconds(5f);
            if(levelCompletePanel != null) levelCompletePanel.SetActive(false);
        }

        private IEnumerator AutoClosePopup()
        {
            yield return new WaitForSeconds(2f);
            if (popupManager != null) popupManager.HidePopup(this);
        }
    }
}