using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public class SettingsMenuController : MonoBehaviour
    {
        public Toggle fullscreenToggle;

        public Dropdown resolutionDropdown;

        [Tooltip("Slider для яркости")] public Slider brightnessSlider;

        public Text brightnessValueText;

        public Slider mouseSensitivitySlider;

        public Text sensitivityValueText;

        public Slider masterVolumeSlider;

        public Text masterVolumeValueText;

        public Slider musicVolumeSlider;

        public Text musicVolumeValueText;

        public Toggle subtitlesToggle;

        public Dropdown subtitleLanguageDropdown;

        public Dropdown gameLanguageDropdown;

        private SettingsManager settingsManager;

        void Start()
        {
            settingsManager = SettingsManager.Instance;

            InitializeUI();

            LoadCurrentSettings();

            SetupEventListeners();
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
            }
        }

        void OnBrightnessChanged(float value)
        {
            settingsManager.SetBrightness(value);
            UpdateBrightnessText(value);
            settingsManager.SaveSettings();
        }

        void OnMouseSensitivityChanged(float value)
        {
            settingsManager.SetMouseSensitivity(value);
            UpdateSensitivityText(value);
            settingsManager.SaveSettings();
        }

        void OnMasterVolumeChanged(float value)
        {
            settingsManager.SetMasterVolume(value);
            UpdateMasterVolumeText(value);
            settingsManager.SaveSettings();
        }

        void OnMusicVolumeChanged(float value)
        {
            settingsManager.SetMusicVolume(value);
            UpdateMusicVolumeText(value);
            settingsManager.SaveSettings();
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
            }
        }

        void OnGameLanguageChanged(int index)
        {
            if (index >= 0 && index < settingsManager.AvailableLanguages.Length)
            {
                settingsManager.SetGameLanguage(settingsManager.AvailableLanguages[index]);
                settingsManager.SaveSettings();
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