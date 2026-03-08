using UnityEngine;
using System.Collections;

namespace Assets.Scripts
{
    [RequireComponent(typeof(BoxCollider))] 
    public class KeypadButton : MonoBehaviour
    {
        [Header("Налаштування кнопки")]
        [Tooltip("Значення цієї кнопки (0, 1... 9). Для Enter напиши 'E'")]
        public string buttonValue = "1";
        
        [Header("Візуальний ефект")]
        [Tooltip("Об'єкт (Quad), який буде спалахувати при кліку")]
        public GameObject highlightEffect;
        [Tooltip("Скільки секунд кнопка світиться")]
        public float flashDuration = 0.18f;

        [Header("Аудіо")]
        public AudioSource buttonAudioSource;
        public AudioClip clickSound;
        [Range(0f, 1f)] public float clickVolume = 0.5f;
        private KeypadController mainController;
        private void Start()
        {
            mainController = GetComponentInParent<KeypadController>();

            if (highlightEffect != null)
            {
                highlightEffect.SetActive(false);
            }
        }
        public void PressButton()
        {
            if (mainController == null || mainController.IsLocked) return;

            if (!mainController.IsAccessible())
            {
                mainController.PlayDeniedSound();
                StartCoroutine(FlashDenied());
                return;
            }

            if (buttonAudioSource != null && clickSound != null)
            {
                buttonAudioSource.PlayOneShot(clickSound, clickVolume);
            }

            StartCoroutine(FlashHighlight());

            mainController.ReceiveInput(buttonValue);
        }

        private IEnumerator FlashHighlight()
        {
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(true);
                yield return new WaitForSeconds(flashDuration);
                highlightEffect.SetActive(false);
            }
        }

        private IEnumerator FlashDenied()
        {
            if (highlightEffect == null) yield break;
            Renderer r = highlightEffect.GetComponent<Renderer>();
            if (r == null) yield break;

            Color original = r.material.color;
            highlightEffect.SetActive(true);
            r.material.color = Color.red;
            yield return new WaitForSeconds(flashDuration);
            r.material.color = original;
            highlightEffect.SetActive(false);
        }
    }
}