using UnityEngine;

namespace Assets.Scripts
{
    public class SanityController : MonoBehaviour
    {
        [Header("Sanity Settings")]
        public float maxSanity = 100f;
        public float sanityDrainRate = 2f; // Скільки втрачаємо в секунду в темряві
        public float sanityRegenRate = 5f; // Скільки відновлюємо на світлі

        private float currentSanity;
        private LampController lampController;

        void Start()
        {
            currentSanity = maxSanity;
            lampController = GetComponent<LampController>();
        }

        void Update()
        {
            bool isSafe = false;

            if (lampController != null)
            {
                if (lampController.GetCurrentFuel() > 0) 
                {
                    isSafe = true; 
                }
            }

            if (isSafe)
            {
                RecoverSanity();
            }
            else
            {
                LoseSanity();
            }

            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.UpdateMindUI(currentSanity, maxSanity);
            }
        }

        void LoseSanity()
        {
            if (currentSanity > 0)
            {
                currentSanity -= sanityDrainRate * Time.deltaTime;
            }
        }

        void RecoverSanity()
        {
            if (currentSanity < maxSanity)
            {
                currentSanity += sanityRegenRate * Time.deltaTime;
            }
        }
    }
}