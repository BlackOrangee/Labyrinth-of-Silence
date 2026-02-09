using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public enum AudioType
    {
        Music,
        SFX
    }

    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<AudioManager>();
                }

                if (instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                }

                return instance;
            }
        }

        [Header("Audio Mixer (Optional)")]
        [Tooltip("If assigned, will use AudioMixer for volume control. Otherwise uses direct AudioSource volume control.")]
        public AudioMixer audioMixer;

        [Header("Mixer Group Names (if using AudioMixer)")]
        public string musicGroupName = "Music";
        public string sfxGroupName = "SFX";

        private AudioMixerGroup musicGroup;
        private AudioMixerGroup sfxGroup;

        private List<AudioSource> musicSources = new List<AudioSource>();
        private List<AudioSource> sfxSources = new List<AudioSource>();

        private float currentMasterVolume = 1f;
        private float currentMusicVolume = 1f;
        private float currentSfxVolume = 1f;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioMixer();
        }

        void Start()
        {
            if (SettingsManager.Instance != null)
            {
                ApplyVolumeSettings();
            }
        }

        private void InitializeAudioMixer()
        {
            if (audioMixer != null)
            {
                var groups = audioMixer.FindMatchingGroups(musicGroupName);
                if (groups != null && groups.Length > 0)
                {
                    musicGroup = groups[0];
                    Debug.Log($"[AudioManager] Found Music group: {musicGroup.name}");
                }
                else
                {
                    Debug.LogWarning($"[AudioManager] Music group '{musicGroupName}' not found in AudioMixer");
                }

                groups = audioMixer.FindMatchingGroups(sfxGroupName);
                if (groups != null && groups.Length > 0)
                {
                    sfxGroup = groups[0];
                    Debug.Log($"[AudioManager] Found SFX group: {sfxGroup.name}");
                }
                else
                {
                    Debug.LogWarning($"[AudioManager] SFX group '{sfxGroupName}' not found in AudioMixer");
                }
            }
        }

        /// <summary>
        /// Register an AudioSource to be managed by AudioManager
        /// </summary>
        public void RegisterAudioSource(AudioSource source, AudioType type)
        {
            if (source == null) return;

            // Assignmixer group if available
            if (audioMixer != null)
            {
                switch (type)
                {
                    case AudioType.Music:
                        if (musicGroup != null)
                        {
                            source.outputAudioMixerGroup = musicGroup;
                        }
                        if (!musicSources.Contains(source))
                        {
                            musicSources.Add(source);
                        }
                        break;

                    case AudioType.SFX:
                        if (sfxGroup != null)
                        {
                            source.outputAudioMixerGroup = sfxGroup;
                        }
                        if (!sfxSources.Contains(source))
                        {
                            sfxSources.Add(source);
                        }
                        break;
                }
            }
            else
            {
                switch (type)
                {
                    case AudioType.Music:
                        if (!musicSources.Contains(source))
                        {
                            musicSources.Add(source);
                        }
                        break;

                    case AudioType.SFX:
                        if (!sfxSources.Contains(source))
                        {
                            sfxSources.Add(source);
                        }
                        break;
                }

                ApplyVolumeToSource(source, type);
            }

            Debug.Log($"[AudioManager] Registered {type} AudioSource: {source.gameObject.name}");
        }

        /// <summary>
        /// Unregister an AudioSource
        /// </summary>
        public void UnregisterAudioSource(AudioSource source)
        {
            musicSources.Remove(source);
            sfxSources.Remove(source);
        }

        /// <summary>
        /// Apply volume settings from SettingsManager
        /// </summary>
        public void ApplyVolumeSettings()
        {
            if (SettingsManager.Instance == null) return;

            currentMasterVolume = SettingsManager.Instance.currentSettings.masterVolume;
            currentMusicVolume = SettingsManager.Instance.currentSettings.musicVolume;
            currentSfxVolume = SettingsManager.Instance.currentSettings.sfxVolume;

            if (audioMixer == null)
            {
                ApplyVolumesToAllSources();
            }
        }

        private void ApplyVolumesToAllSources()
        {
            musicSources.RemoveAll(s => s == null);
            sfxSources.RemoveAll(s => s == null);

            foreach (var source in musicSources)
            {
                ApplyVolumeToSource(source, AudioType.Music);
            }

            foreach (var source in sfxSources)
            {
                ApplyVolumeToSource(source, AudioType.SFX);
            }
        }

        private void ApplyVolumeToSource(AudioSource source, AudioType type)
        {
            if (source == null) return;

            float baseVolume = 1f;

            var backgroundMusic = source.GetComponent<BackgroundMusic>();
            if (backgroundMusic != null)
            {
                baseVolume = backgroundMusic.GetTargetVolume();
            }

            switch (type)
            {
                case AudioType.Music:
                    source.volume = baseVolume * currentMusicVolume * currentMasterVolume;
                    break;

                case AudioType.SFX:
                    source.volume = baseVolume * currentSfxVolume * currentMasterVolume;
                    break;
            }
        }

        /// <summary>
        /// Play a one-shot sound effect
        /// </summary>
        public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;

            float finalVolume = volume * currentSfxVolume * currentMasterVolume;
            AudioSource.PlayClipAtPoint(clip, position, finalVolume);
        }

        /// <summary>
        /// Play a one-shot sound effect at AudioSource position
        /// </summary>
        public void PlaySFX(AudioSource source, AudioClip clip, float volume = 1f)
        {
            if (source == null || clip == null) return;

            float finalVolume = volume * currentSfxVolume * currentMasterVolume;
            source.PlayOneShot(clip, finalVolume);
        }

        /// <summary>
        /// Get the current effective volume for a given audio type
        /// </summary>
        public float GetEffectiveVolume(AudioType type)
        {
            switch (type)
            {
                case AudioType.Music:
                    return currentMusicVolume * currentMasterVolume;
                case AudioType.SFX:
                    return currentSfxVolume * currentMasterVolume;
                default:
                    return currentMasterVolume;
            }
        }
    }
}
