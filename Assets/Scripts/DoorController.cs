using UnityEngine;
using System.Collections;
using TMPro;

namespace Assets.Scripts
{
    public class DoorController : MonoBehaviour, IInteractable
    {
        [Header("Settings")]
        public int keysRequired = 4;
        [Tooltip("Angle to open (e.g. 60 or -60)")]
        public float openAngle = 60f;
        public float openSpeed = 2f;

        [Header("References")]
        [Tooltip("Drag the Door_Pivot object here")]
        public Transform doorPivot; 
        public PopupManager popupManager;
        
        [Header("Level Complete UI")]
        [Tooltip("Assign the LevelCompletePanel here")]
        public GameObject levelCompletePanel;

        private bool isOpen = false;
        private SimpleInventory playerInventory;

        void Start()
        {
            if (popupManager == null)
                popupManager = FindFirstObjectByType<PopupManager>();

            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerInventory = playerObj.GetComponent<SimpleInventory>();
            }
        }

        public string GetInteractText()
        {
            if (isOpen) return "";
            
            int currentKeys = playerInventory != null ? playerInventory.GetCollectedKeysCount() : 0;
            
            if (currentKeys >= keysRequired)
                return "Press Enter to Open Door";
            else
                return $"Locked. Need keys: {currentKeys}/{keysRequired}";
        }

        public void Interact(GameObject actor)
        {
            if (isOpen) return;

            if (playerInventory == null)
            {
                playerInventory = actor.GetComponent<SimpleInventory>();
            }

            int currentKeys = playerInventory != null ? playerInventory.GetCollectedKeysCount() : 0;

            if (currentKeys >= keysRequired)
            {
                OpenDoor();
            }
            else
            {
                if (popupManager != null)
                {
                    popupManager.ShowPopup($"Need {keysRequired} keys! You have {currentKeys}.", null, this, "OK", null);
                    StartCoroutine(AutoClosePopup());
                }
            }
        }

        public void OnInteract(GameObject actor) => Interact(actor);

        private void OpenDoor()
        {
            isOpen = true;

            StartCoroutine(RotateDoorRoutine());

            StartCoroutine(LevelCompleteRoutine());

            if (popupManager != null) popupManager.HidePopup(this);
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

        private IEnumerator LevelCompleteRoutine()
        {
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(true);
            }

            yield return new WaitForSeconds(5f);

            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(false);
            }
        }

        private IEnumerator AutoClosePopup()
        {
            yield return new WaitForSeconds(2f);
            if (popupManager != null) popupManager.HidePopup(this);
        }
    }
}