using UnityEngine;
using System.Collections;

namespace Assets.Scripts
{
    [RequireComponent(typeof(AudioSource))]
    public class DoorOpen : MonoBehaviour
    {
        [Header("Налаштування Дверей")]
        [Tooltip("Об'єкт петлі, навколо якого крутяться двері.")]
        public Transform doorPivot;

        [Tooltip("Вісь обертання.")]
        public Vector3 rotationAxis = new Vector3(0, 1, 0);

        [Tooltip("Кут відчинення.")]
        public float openAngle = 90f;
        
        [Tooltip("Швидкість відчинення дверей")]
        public float openSpeed = 2f;

        [Header("Аудіо")]
        public AudioClip openSound;
        private bool isOpen = false;
        private AudioSource audioSource;
        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            
            if (doorPivot == null) 
            {
                doorPivot = transform;
            }
        }
        public void OpenDoor()
        {
            if (isOpen) return; 
            
            isOpen = true;

            if (openSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(openSound);
            }

            StartCoroutine(AnimateDoorCoroutine());
        }
        private IEnumerator AnimateDoorCoroutine()
        {
            Quaternion startRotation = doorPivot.localRotation;

            Quaternion endRotation = startRotation * Quaternion.AngleAxis(openAngle, rotationAxis);

            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * openSpeed;
                doorPivot.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
                yield return null; 
            }

            doorPivot.localRotation = endRotation;
        }
    }
}