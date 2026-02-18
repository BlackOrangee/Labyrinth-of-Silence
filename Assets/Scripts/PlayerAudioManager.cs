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
        public SoundProfile snowSteps;
        public SoundProfile jumpSound;
        public SoundProfile damageSound;

        [Header("Settings")]
        public float walkInterval = 0.5f;
        public float runInterval = 0.3f;

        private CharacterController controller;
        private float stepTimer;
        private bool isSnow = false;

        void Start()
        {
            controller = GetComponent<CharacterController>();

            if (AudioManager.Instance != null)
            {
                if (footstepSource != null)
                {
                    AudioManager.Instance.RegisterAudioSource(footstepSource, AudioType.SFX);
                }
                if (voiceSource != null)
                {
                    AudioManager.Instance.RegisterAudioSource(voiceSource, AudioType.SFX);
                }
            }
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
                stepTimer = 0;
            }
        }

        void PlayStep(bool isRunning)
        {
            SoundProfile profileToPlay;

            if (isSnow)
            {
                profileToPlay = snowSteps;
            }
            else
            {
                profileToPlay = isRunning ? runSteps : walkSteps;
            }

            profileToPlay.Play(footstepSource);
        }

        public void PlayDamage()
        {
            damageSound.Play(voiceSource);
        }
        
        public void SetSnowSurface(bool state)
        {
            isSnow = state;
        }

        void OnDestroy()
        {
            if (AudioManager.Instance != null)
            {
                if (footstepSource != null)
                {
                    AudioManager.Instance.UnregisterAudioSource(footstepSource);
                }
                if (voiceSource != null)
                {
                    AudioManager.Instance.UnregisterAudioSource(voiceSource);
                }
            }
        }
    }
}