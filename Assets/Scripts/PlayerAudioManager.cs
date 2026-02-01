using UnityEngine;

namespace Assets.Scripts
{
    public class PlayerAudioManager : MonoBehaviour
    {
        [Header("Sources")]
        public AudioSource footstepSource;
        public AudioSource voiceSource;

        [Header("Profiles")]
        public SoundProfile walkSteps;
        public SoundProfile runSteps;
        public SoundProfile snowSteps; // Якщо будеш на снігу
        public SoundProfile jumpSound;
        public SoundProfile damageSound;

        [Header("Settings")]
        public float walkInterval = 0.5f;
        public float runInterval = 0.3f;

        private CharacterController controller;
        private float stepTimer;
        private bool isSnow = false; // Перемикач поверхні

        void Start()
        {
            controller = GetComponent<CharacterController>();
        }

        void Update()
        {
            HandleFootsteps();
        }

        void HandleFootsteps()
        {
            if (controller.isGrounded && controller.velocity.sqrMagnitude > 0.2f)
            {
                bool isRunning = Input.GetKey(KeyCode.LeftShift);
                float interval = isRunning ? runInterval : walkInterval;

                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0)
                {
                    PlayStep(isRunning);
                    stepTimer = interval;
                }
            }
            else
            {
                stepTimer = 0; // Скидаємо, щоб перший крок був миттєвим
            }
        }

        void PlayStep(bool isRunning)
        {
            // Вибираємо профіль звуку
            SoundProfile profileToPlay;

            if (isSnow)
            {
                profileToPlay = snowSteps; // Якщо на снігу
            }
            else
            {
                profileToPlay = isRunning ? runSteps : walkSteps;
            }

            // Граємо через нашу систему
            profileToPlay.Play(footstepSource);
        }

        // Публічний метод для отримання дамагу
        public void PlayDamage()
        {
            damageSound.Play(voiceSource);
        }
        
        // Перемикач поверхонь (можна викликати з тригерів)
        public void SetSnowSurface(bool state)
        {
            isSnow = state;
        }
    }
}