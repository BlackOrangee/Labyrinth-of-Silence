using UnityEngine;

namespace Assets.Scripts
{
    public class BackgroundMusic : MonoBehaviour
    {
        public static BackgroundMusic Instance;

        [Header("Music Settings")]
        [Tooltip("Максимальна гучність під час погоні/атаки")]
        [Range(0f, 1f)] public float chaseVolume = 0.6f; 
        
        [Tooltip("Скільки секунд музика плавно вмикається")]
        public float fadeIncDuration = 1.5f; 
        
        [Tooltip("Скільки секунд музика плавно затухає, коли гравець втік")]
        public float fadeDecDuration = 4.0f;

        private AudioSource audioSource;
        private float currentVolume = 0f;
        private float targetVolume = 0f;
        private bool isRegistered = false;

        void Awake()
        {
            if (Instance == null) 
            { 
                Instance = this; 
            }
            else 
            { 
                Destroy(gameObject); 
                return;
            }
        }

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.volume = 0f;
            currentVolume = 0f;
            targetVolume = 0f;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.RegisterAudioSource(audioSource, AudioType.Music);
                isRegistered = true;
            }
        }

        void Update()
        {
            if (!Mathf.Approximately(currentVolume, targetVolume))
            {
                float duration = (targetVolume > currentVolume) ? fadeIncDuration : fadeDecDuration;
                float speed = chaseVolume / duration;

                currentVolume = Mathf.MoveTowards(currentVolume, targetVolume, speed * Time.deltaTime);
                ApplyVolume();

                if (currentVolume <= 0.01f && audioSource.isPlaying)
                {
                    audioSource.Pause();
                }
            }
        }

        private void ApplyVolume()
        {
            if (audioSource == null) return;

            float settingsMultiplier = 1f;
            if (SettingsManager.Instance != null)
            {
                settingsMultiplier = SettingsManager.Instance.currentSettings.musicVolume *
                                     SettingsManager.Instance.currentSettings.masterVolume;
            }

            audioSource.volume = currentVolume * settingsMultiplier;
        }

        public void PlayChaseMusic()
        {
            targetVolume = chaseVolume;
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        public void StopChaseMusic()
        {
            targetVolume = 0f;
        }

        public float GetTargetVolume()
        {
            return targetVolume;
        }

        void OnDestroy()
        {
            if (isRegistered && AudioManager.Instance != null && audioSource != null)
            {
                AudioManager.Instance.UnregisterAudioSource(audioSource);
            }
        }
    }
}