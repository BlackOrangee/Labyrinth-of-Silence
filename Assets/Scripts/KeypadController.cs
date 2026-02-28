using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Events;

namespace Assets.Scripts
{
    [RequireComponent(typeof(AudioSource))]
    public class KeypadController : MonoBehaviour, IInteractable
    {
        private enum TargetAction { Light, Door }

        [Tooltip("Пароль для відкриття дверей")]
        [SerializeField] private string doorCode = "1234";
        
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

        [Header("Події (AAA-архітектура)")]
        [Tooltip("Що має статися загалом, коли введено БУДЬ-ЯКИЙ правильний код?")]
        public UnityEvent OnCodeCorrect;

        [Header("Додатковий функціонал (Двері)")]
        [Tooltip("Перетягни сюди об'єкт дверей зі скриптом DoorOpen.")]
        [SerializeField] private DoorOpen _door;


        private string currentInput = "";
        private AudioSource audioSource;
        
        public bool IsLocked { get; private set; } = false; 

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            UpdateDisplay();
        }

        public void Interact(GameObject actor)
        {
            PlayerRaycaster raycaster = FindObjectOfType<PlayerRaycaster>();
            if (raycaster != null)
            {
                if (raycaster.aimMode == PlayerRaycaster.AimMode.CenterOfScreen)
                {
                    raycaster.aimMode = PlayerRaycaster.AimMode.MouseCursor;
                }
                else
                {
                    raycaster.aimMode = PlayerRaycaster.AimMode.CenterOfScreen;
                }
            }
        }

        public void OnInteract(GameObject actor) => Interact(actor);

        public string GetPopupID() => "Ввести код (E)"; 

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

            else if (currentInput == doorCode)
            {
                StartCoroutine(SuccessRoutine(TargetAction.Door));
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
        private IEnumerator SuccessRoutine(TargetAction actionToPerform)
        {
            IsLocked = true;
            if (successSound) audioSource.PlayOneShot(successSound);
            
            displayText.color = successColor;
            displayText.text = "OPEN";

            yield return new WaitForSeconds(1f); 

            PlayerRaycaster playerCast = FindObjectOfType<PlayerRaycaster>();
            if (playerCast != null)
            {
                playerCast.aimMode = PlayerRaycaster.AimMode.CenterOfScreen;
            }

            currentInput = "";
            UpdateDisplay();
            IsLocked = false; 

            try
            {
                OnCodeCorrect?.Invoke();

                if (actionToPerform == TargetAction.Light)
                {
            
                }
                else if (actionToPerform == TargetAction.Door)
                {
                    if (_door != null)
                    {
                        _door.OpenDoor();
                    }
                }

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[KeypadController] Помилка під час виконання дії після правильного пароля: {ex.Message}");
            }
        }
        private void UpdateDisplay()
        {
            displayText.text = currentInput.Length > 0 ? currentInput : "----";
            displayText.color = defaultColor;
        }
    }
}