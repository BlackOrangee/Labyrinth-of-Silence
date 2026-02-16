using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Assets.Scripts
{
    [RequireComponent(typeof(AudioSource))]
    public class DoorController : MonoBehaviour, IInteractable
    {
        [Header("Door Identity")]
        [Tooltip("Назва для UI (DOOR1, DOOR2...)")]
        [SerializeField] private string doorName = "Door"; 

        [Header("Requirements")]
        [Tooltip("Які ключі потрібні")]
        [SerializeField] private List<KeyColorType> requiredKeys; 

        [Header("Settings")]
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float openSpeed = 2f;
        [SerializeField] private Transform doorPivot;

        [Header("Audio")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip lockedSound;

        [Header("Dependencies")]
        [SerializeField] private GameObject levelCompletePanel; 

        private bool isOpen = false;
        private SimpleInventory playerInventory;
        private AudioSource audioSource;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();

            var player = GameObject.FindWithTag("Player");
            if (player != null) playerInventory = player.GetComponent<SimpleInventory>();
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
                Debug.Log($"[DoorController] {doorName} is locked! You need specific keys to open it.");
            }
        }

        public void OnInteract(GameObject actor)
        {
            Interact(actor);
        }

        public string GetPopupID()
        {
            return isOpen ? "" : "open";
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
    }
}