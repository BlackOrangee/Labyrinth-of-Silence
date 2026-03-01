using UnityEngine;
using System.Collections;

namespace Assets.Scripts
{
    [RequireComponent(typeof(AudioSource))]
    public class DoorOpen2 : MonoBehaviour
    {
        [Header("Налаштування Подвійних Дверей")]
        [Tooltip("Ліва стулка дверей (door_A_012). Крутиться ЛІВОРУЧ від центру.")]
        public Transform doorPivotA;

        [Tooltip("Права стулка дверей (door_B_012). Крутиться ПРАВОРУЧ від центру.")]
        public Transform doorPivotB;

        [Tooltip("Вісь обертання обох стулок. X,Y,Z (0,0,1) для стандартних дверей.")]
        public Vector3 rotationAxis = new Vector3(0, 0, 1);

        [Tooltip("Кут відчинення лівої стулки (door_A). ПОЗИТИВНИЙ (+90).")]
        public float openAngleA = 90f;

        [Tooltip("Кут відчинення правої стулки (door_B). НЕГАТИВНИЙ (-90).")]
        public float openAngleB = -90f;

        [Tooltip("Швидкість відчинення дверей")]
        public float openSpeed = 2f;

        [Header("Аудіо")]
        public AudioClip openSound;

        private bool isOpen = false;
        private AudioSource audioSource;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();

            if (doorPivotA == null)
            {
                Debug.LogWarning("doorPivotA - door_A_012");
                doorPivotA = transform;
            }

            if (doorPivotB == null)
            {
                Debug.LogWarning("doorPivotB - door_B_012");
                doorPivotB = transform;
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

            StartCoroutine(AnimateDoorsCoroutine());
        }
        private IEnumerator AnimateDoorsCoroutine()
        {
            Quaternion startRotA = doorPivotA.localRotation;
            Quaternion startRotB = doorPivotB.localRotation;

            Quaternion endRotA = startRotA * Quaternion.AngleAxis(openAngleA, rotationAxis);
            Quaternion endRotB = startRotB * Quaternion.AngleAxis(openAngleB, rotationAxis);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * openSpeed;
                float tClamped = Mathf.Clamp01(t);

                doorPivotA.localRotation = Quaternion.Slerp(startRotA, endRotA, tClamped);
                doorPivotB.localRotation = Quaternion.Slerp(startRotB, endRotB, tClamped);

                yield return null;
            }

            doorPivotA.localRotation = endRotA;
            doorPivotB.localRotation = endRotB;

            Debug.Log("Подвійні двері відчинено.");
        }
    }
}