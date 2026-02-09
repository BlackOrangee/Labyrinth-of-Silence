using UnityEngine;

namespace Assets.Scripts
{
    public class BackgroundMusic : MonoBehaviour
    {
        [Header("Settings")]
        public float targetVolume = 0.5f;
        public float fadeDuration = 3.0f;

        private AudioSource audioSource;
        private float currentFadeVolume = 0f;
        private bool isRegistered = false;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.RegisterAudioSource(audioSource, AudioType.Music);
                isRegistered = true;
            }

            currentFadeVolume = 0f;

            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        void Update()
        {
            if (currentFadeVolume < targetVolume)
            {
                currentFadeVolume += Time.deltaTime / fadeDuration * targetVolume;
                currentFadeVolume = Mathf.Min(currentFadeVolume, targetVolume);
            }

            ApplyVolume();
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

            audioSource.volume = currentFadeVolume * settingsMultiplier;
        }

        /// <summary>
        /// Get the target volume (used by AudioManager)
        /// </summary>
        public float GetTargetVolume()
        {
            return targetVolume;
        }

        /// <summary>
        /// Set the target volume
        /// </summary>
        public void SetTargetVolume(float volume)
        {
            targetVolume = Mathf.Clamp01(volume);
        }

        void OnDestroy()
        {
            if (isRegistered && AudioManager.Instance != null)
            {
                AudioManager.Instance.UnregisterAudioSource(audioSource);
            }
        }
    }
}