using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public class SettingsMenuController : MonoBehaviour
    {
        public Toggle fullscreenToggle;

        public Dropdown resolutionDropdown;

        public Slider brightnessSlider;

        public Text brightnessValueText;

        public Slider mouseSensitivitySlider;

        public Text sensitivityValueText;

        public Slider masterVolumeSlider;

        public Text masterVolumeValueText;

        public Slider musicVolumeSlider;

        public Text musicVolumeValueText;

        public Slider sfxVolumeSlider;

        public Text sfxVolumeValueText;

        public Toggle subtitlesToggle;

        public Dropdown subtitleLanguageDropdown;

        public Dropdown gameLanguageDropdown;

        [Header("Slider Sound")]
        public AudioClip sliderSound;
        [Range(0f, 1f)] public float sliderSoundVolume = 0.5f;

        private SettingsManager settingsManager;
        private float _lastSliderSoundTime = -1f;
        private const float SliderSoundCooldown = 0.1f;
        private AudioSource _uiAudioSource;

        void Awake()
        {
            settingsManager = SettingsManager.Instance;

            _uiAudioSource = gameObject.AddComponent<AudioSource>();
            _uiAudioSource.playOnAwake = false;
            _uiAudioSource.ignoreListenerPause = true;

            InitializeUI();
            SetupEventListeners();
        }

        void OnEnable()
        {
            if (settingsManager != null)
            {
                LoadCurrentSettings();
            }
        }

        void InitializeUI()
        {
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(settingsManager.GetResolutionStrings());
            }

            if (subtitleLanguageDropdown != null)
            {
                subtitleLanguageDropdown.ClearOptions();
                subtitleLanguageDropdown.AddOptions(new List<string>(settingsManager.AvailableLanguages));
            }

            if (gameLanguageDropdown != null)
            {
                gameLanguageDropdown.ClearOptions();
                gameLanguageDropdown.AddOptions(new List<string>(settingsManager.AvailableLanguages));
            }
        }

        void SetupEventListeners()
        {
            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }

            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }
            
            if (brightnessSlider != null)
            {
                brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
            }
            
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            if (subtitlesToggle != null)
            {
                subtitlesToggle.onValueChanged.AddListener(OnSubtitlesChanged);
            }
            
            if (subtitleLanguageDropdown != null)
            {
                subtitleLanguageDropdown.onValueChanged.AddListener(OnSubtitleLanguageChanged);
            }
            
            if (gameLanguageDropdown != null)
            {
                gameLanguageDropdown.onValueChanged.AddListener(OnGameLanguageChanged);
            }
        }

        void LoadCurrentSettings()
        {
            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = settingsManager.currentSettings.fullscreen;
            }

            if (resolutionDropdown != null)
            {
                resolutionDropdown.value = settingsManager.GetCurrentResolutionIndex();
            }

            if (brightnessSlider != null)
            {
                brightnessSlider.value = settingsManager.currentSettings.brightness;
                UpdateBrightnessText(settingsManager.currentSettings.brightness);
            }

            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.value = settingsManager.currentSettings.mouseSensitivity;
                UpdateSensitivityText(settingsManager.currentSettings.mouseSensitivity);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = settingsManager.currentSettings.masterVolume;
                UpdateMasterVolumeText(settingsManager.currentSettings.masterVolume);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = settingsManager.currentSettings.musicVolume;
                UpdateMusicVolumeText(settingsManager.currentSettings.musicVolume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = settingsManager.currentSettings.sfxVolume;
                UpdateSfxVolumeText(settingsManager.currentSettings.sfxVolume);
            }

            if (subtitlesToggle != null)
            {
                subtitlesToggle.isOn = settingsManager.currentSettings.subtitlesEnabled;
            }

            if (subtitleLanguageDropdown != null)
            {
                int index = System.Array.IndexOf(settingsManager.AvailableLanguages, settingsManager.currentSettings.subtitleLanguage);
                subtitleLanguageDropdown.value = index >= 0 ? index : 0;
            }

            if (gameLanguageDropdown != null)
            {
                int index = System.Array.IndexOf(settingsManager.AvailableLanguages, settingsManager.currentSettings.gameLanguage);
                gameLanguageDropdown.value = index >= 0 ? index : 0;
            }
        }

        void PlaySliderSound()
        {
            if (sliderSound == null || _uiAudioSource == null) return;
            if (Time.unscaledTime - _lastSliderSoundTime < SliderSoundCooldown) return;
            _lastSliderSoundTime = Time.unscaledTime;
            _uiAudioSource.PlayOneShot(sliderSound, sliderSoundVolume);
        }

        #region Event Handlers

        void OnFullscreenChanged(bool value)
        {
            settingsManager.SetFullscreen(value);
            settingsManager.SaveSettings();
        }

        void OnResolutionChanged(int index)
        {
            if (index >= 0 && index < settingsManager.AvailableResolutions.Length)
            {
                Resolution res = settingsManager.AvailableResolutions[index];
                settingsManager.SetResolution(res);
                settingsManager.SaveSettings();
                PlaySliderSound();
            }
        }

        void OnBrightnessChanged(float value)
        {
            settingsManager.SetBrightness(value);
            UpdateBrightnessText(value);
            settingsManager.SaveSettings();
            PlaySliderSound();
        }

        void OnMouseSensitivityChanged(float value)
        {
            settingsManager.SetMouseSensitivity(value);
            UpdateSensitivityText(value);
            settingsManager.SaveSettings();
            PlaySliderSound();
        }

        void OnMasterVolumeChanged(float value)
        {
            settingsManager.SetMasterVolume(value);
            UpdateMasterVolumeText(value);
            settingsManager.SaveSettings();
            PlaySliderSound();
        }

        void OnMusicVolumeChanged(float value)
        {
            settingsManager.SetMusicVolume(value);
            UpdateMusicVolumeText(value);
            settingsManager.SaveSettings();
            PlaySliderSound();
        }

        void OnSfxVolumeChanged(float value)
        {
            settingsManager.SetSfxVolume(value);
            UpdateSfxVolumeText(value);
            settingsManager.SaveSettings();
            PlaySliderSound();
        }

        void OnSubtitlesChanged(bool value)
        {
            settingsManager.SetSubtitlesEnabled(value);
            settingsManager.SaveSettings();
        }

        void OnSubtitleLanguageChanged(int index)
        {
            if (index >= 0 && index < settingsManager.AvailableLanguages.Length)
            {
                settingsManager.SetSubtitleLanguage(settingsManager.AvailableLanguages[index]);
                settingsManager.SaveSettings();
                PlaySliderSound();
            }
        }

        void OnGameLanguageChanged(int index)
        {
            if (index >= 0 && index < settingsManager.AvailableLanguages.Length)
            {
                settingsManager.SetGameLanguage(settingsManager.AvailableLanguages[index]);
                settingsManager.SaveSettings();
                PlaySliderSound();
            }
        }

        #endregion

        #region UI Text Updates

        void UpdateBrightnessText(float value)
        {
            if (brightnessValueText != null)
            {
                brightnessValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        void UpdateSensitivityText(float value)
        {
            if (sensitivityValueText != null)
            {
                sensitivityValueText.text = value.ToString("F1");
            }
        }

        void UpdateMasterVolumeText(float value)
        {
            if (masterVolumeValueText != null)
            {
                masterVolumeValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        void UpdateMusicVolumeText(float value)
        {
            if (musicVolumeValueText != null)
            {
                musicVolumeValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        void UpdateSfxVolumeText(float value)
        {
            if (sfxVolumeValueText != null)
            {
                sfxVolumeValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        #endregion

        void OnDestroy()
        {
            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            }

            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            }

            if (brightnessSlider != null)
            {
                brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
            }

            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.onValueChanged.RemoveListener(OnMouseSensitivityChanged);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            }

            if (subtitlesToggle != null)
            {
                subtitlesToggle.onValueChanged.RemoveListener(OnSubtitlesChanged);
            }

            if (subtitleLanguageDropdown != null)
            {
                subtitleLanguageDropdown.onValueChanged.RemoveListener(OnSubtitleLanguageChanged);
            }

            if (gameLanguageDropdown != null)
            {
                gameLanguageDropdown.onValueChanged.RemoveListener(OnGameLanguageChanged);
            }
        }
    }
}