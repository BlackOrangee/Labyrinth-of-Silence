using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Events;

namespace Assets.Scripts
{
    [RequireComponent(typeof(AudioSource))]
    public class KeypadController : MonoBehaviour, IInteractable
    {
        [Header("Налаштування коду")]
        [Tooltip("Правильна комбінація (можна змінювати пароль на будь-який)")]
        [SerializeField] private string correctCode = "1234";
        [Tooltip("Максимальна кількість цифр на екрані")]
        [SerializeField] private int maxDigits = 4;

        [Header("UI Дисплей")]
        [SerializeField] private TextMeshPro displayText;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color errorColor = new Color(1f, 0.4f, 0.7f);
        [SerializeField] private Color successColor = Color.green;

        [Header("Аудіо")]
        [SerializeField] private AudioClip errorSound;
        [SerializeField] private AudioClip successSound;

        [Header("Події")]
        [Tooltip("Що має статися, коли код введено правильно? (Відкрити двері, запустити катсцену)")]
        public UnityEvent OnCodeCorrect;
        private string currentInput = "";
        private AudioSource audioSource;
        
        public bool IsLocked { get; private set; } = false; 
        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            UpdateDisplay();
        }
public void Interact(GameObject actor) { }
        public void OnInteract(GameObject actor)
        {
            Interact(actor);
        }
        public string GetPopupID()
        {
            return "Use Keypad"; 
        }
        public void ReceiveInput(string input)
        {
            if (IsLocked) return;

            if (input == "E")
            {
                CheckCode();
            }
            else
            {
                if (currentInput.Length < maxDigits)
                {
                    currentInput += input;
                    UpdateDisplay();
                }
            }
        }
        private void CheckCode()
        {
            if (currentInput.Length == 0) return;

            if (currentInput == correctCode)
            {
                StartCoroutine(SuccessRoutine());
            }
            else
            {
                StartCoroutine(ErrorRoutine());
            }
        }
        private IEnumerator ErrorRoutine()
        {
            IsLocked = true; 
            if (errorSound) audioSource.PlayOneShot(errorSound);

            for (int i = 0; i < 3; i++)
            {
                displayText.color = errorColor;
                yield return new WaitForSeconds(0.15f);
                displayText.color = defaultColor;
                yield return new WaitForSeconds(0.15f);
            }

            currentInput = "";
            UpdateDisplay();
            IsLocked = false; 
        }
        private IEnumerator SuccessRoutine()
        {
            IsLocked = true;
            if (successSound) audioSource.PlayOneShot(successSound);
            
            displayText.color = successColor;
            displayText.text = "OPEN";

            yield return new WaitForSeconds(1f);

            OnCodeCorrect?.Invoke();
        }
        private void UpdateDisplay()
        {
            displayText.text = currentInput.Length > 0 ? currentInput : "----";
            displayText.color = defaultColor;
        }
    }
}