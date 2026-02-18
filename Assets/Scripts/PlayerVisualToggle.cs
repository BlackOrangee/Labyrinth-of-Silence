using UnityEngine;

namespace Assets.Scripts
{
    public class PlayerVisualToggle : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Клавіша для перемикання видимості")]
        [SerializeField] private KeyCode toggleKey = KeyCode.L;

        [Header("References")]
        [Tooltip("Сюди перетягни об'єкт GG (модель гравця)")]
        [SerializeField] private GameObject visualModel;

        [Tooltip("Сюди перетягни скрипт лампи (якщо хочеш ховати і світло)")]
        [SerializeField] private LampController lampController;

        private bool isVisible = true;

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleVisibility();
            }
        }

        private void ToggleVisibility()
        {
            isVisible = !isVisible;

            if (visualModel != null)
            {
                visualModel.SetActive(isVisible);
            }

            if (lampController != null && lampController.lightGroupObject != null)
            {
                if (!isVisible)
                {
                    lampController.lightGroupObject.SetActive(false);
                }
                else
                {
                    if (lampController.IsLightOn())
                    {
                        lampController.lightGroupObject.SetActive(true);
                    }
                }
            }

            Debug.Log($"Player Visibility: {isVisible}");
        }
    }
}