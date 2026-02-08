using UnityEngine;
using UnityEngine.Audio;
using System.IO;
using System.Collections.Generic;
using Assets.Scripts.Localization;

namespace Assets.Scripts
{
    public class SettingsManager : MonoBehaviour
    {
        public static event System.Action<Language> OnLanguageChanged;

        private static SettingsManager instance;

        public static SettingsManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SettingsManager>();
                }

                if (instance == null)
                {
                    GameObject go = new GameObject("SettingsManager");
                    instance = go.AddComponent<SettingsManager>();
                }

                return instance;
            }
        }

        [Header("References")] [Tooltip("AudioMixer (optional)")]
        public AudioMixer audioMixer;

        [Header("Current Settings")] public GameSettings currentSettings;

        private string settingsFilePath => Path.Combine(Application.persistentDataPath, "settings.json");

        private Resolution[] availableResolutions;
        public Resolution[] AvailableResolutions => availableResolutions;

        private string[] availableLanguages = { "English", "Ukrainian" };
        public string[] AvailableLanguages => availableLanguages;

        private Language currentLanguage = Language.English;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            availableResolutions = Screen.resolutions;

            LoadSettings();

            InitializeLanguage();

            ApplyAllSettings();
        }

        #region Save/Load

        public void SaveSettings()
        {
            try
            {
                string json = JsonUtility.ToJson(currentSettings, true);
                File.WriteAllText(settingsFilePath, json);
                Debug.Log($"[SettingsManager] Saved: Resolution={currentSettings.resolutionWidth}x{currentSettings.resolutionHeight}, Fullscreen={currentSettings.fullscreen}, Language={currentSettings.gameLanguage}, MouseSensitivity={currentSettings.mouseSensitivity}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SettingsManager] Settings save error: {e.Message}");
            }
        }

        public void LoadSettings()
        {
            if (File.Exists(settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(settingsFilePath);
                    currentSettings = JsonUtility.FromJson<GameSettings>(json);
                    Debug.Log($"[SettingsManager] Loaded: Resolution={currentSettings.resolutionWidth}x{currentSettings.resolutionHeight}, Fullscreen={currentSettings.fullscreen}, Language={currentSettings.gameLanguage}");

                    if (currentSettings.sfxVolume == 0f &&
                        (currentSettings.masterVolume > 0f || currentSettings.musicVolume > 0f))
                    {
                        currentSettings.sfxVolume = 0.8f;
                        SaveSettings();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SettingsManager] Setting loading error: {e.Message}");
                    currentSettings = new GameSettings();
                }
            }
            else
            {
                Debug.Log("[SettingsManager] Settings file not found, creating default settings");
                currentSettings = new GameSettings();
                SaveSettings();
            }
        }

        public void ResetToDefaults()
        {
            currentSettings.SetDefaults();
            ApplyAllSettings();
            SaveSettings();
            Debug.Log("[SettingsManager] Reset to defaults");
        }

        #endregion

        #region Apply Settings

        public void ApplyAllSettings()
        {
            ApplyGraphicsSettings();
            ApplyAudioSettings();
            ApplyLanguageSettings();
        }

        private void InitializeLanguage()
        {
            if (System.Enum.TryParse(currentSettings.gameLanguage, out Language language))
            {
                currentLanguage = language;
            }
            else
            {
                currentLanguage = Language.English;
                currentSettings.gameLanguage = Language.English.ToString();
                SaveSettings();
            }
        }

        public void ApplyLanguageSettings()
        {
            SetGameLanguage(currentLanguage);
        }

        public void ApplyGraphicsSettings()
        {
            Debug.Log($"[SettingsManager] Applying graphics settings: {currentSettings.resolutionWidth}x{currentSettings.resolutionHeight}, Fullscreen: {currentSettings.fullscreen}");

            FullScreenMode fullscreenMode = currentSettings.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

            Debug.Log($"[SettingsManager] Current screen: {Screen.width}x{Screen.height}, Fullscreen: {Screen.fullScreen}");

            Screen.SetResolution(
                currentSettings.resolutionWidth,
                currentSettings.resolutionHeight,
                fullscreenMode
            );

            if (BrightnessController.Instance != null)
            {
                BrightnessController.Instance.ApplyBrightness(currentSettings.brightness);
            }
            else
            {
                RenderSettings.ambientLight = Color.white * currentSettings.brightness;
            }
        }

        public void ApplyAudioSettings()
        {
            if (audioMixer != null)
            {
                float masterVolumeDb = currentSettings.masterVolume > 0
                    ? Mathf.Log10(currentSettings.masterVolume) * 20
                    : -80f;

                float musicVolumeDb = currentSettings.musicVolume > 0
                    ? Mathf.Log10(currentSettings.musicVolume) * 20
                    : -80f;

                float sfxVolumeDb = currentSettings.sfxVolume > 0
                    ? Mathf.Log10(currentSettings.sfxVolume) * 20
                    : -80f;

                audioMixer.SetFloat("MasterVolume", masterVolumeDb);
                audioMixer.SetFloat("MusicVolume", musicVolumeDb);
                audioMixer.SetFloat("SFXVolume", sfxVolumeDb);
            }
            else
            {
                AudioListener.volume = currentSettings.masterVolume;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ApplyVolumeSettings();
            }

            Debug.Log(
                $"[SettingsManager] Audio settings: Master={currentSettings.masterVolume}, Music={currentSettings.musicVolume}, SFX={currentSettings.sfxVolume}");
        }

        #endregion

        #region Settings Setters

        public void SetFullscreen(bool fullscreen)
        {
            currentSettings.fullscreen = fullscreen;
            ApplyGraphicsSettings();
        }

        public void SetResolution(int width, int height)
        {
            currentSettings.resolutionWidth = width;
            currentSettings.resolutionHeight = height;
            ApplyGraphicsSettings();
        }

        public void SetResolution(Resolution resolution)
        {
            SetResolution(resolution.width, resolution.height);
        }

        public void SetBrightness(float brightness)
        {
            currentSettings.brightness = Mathf.Clamp01(brightness);
            ApplyGraphicsSettings();
        }

        public void SetMouseSensitivity(float sensitivity)
        {
            currentSettings.mouseSensitivity = Mathf.Clamp(sensitivity, 0.05f, 5f);

            CameraController[] cameras = FindObjectsByType<CameraController>(FindObjectsSortMode.None);
            foreach (var cam in cameras)
            {
                cam.UpdateSensitivityFromSettings();
            }
        }

        public void SetMasterVolume(float volume)
        {
            currentSettings.masterVolume = Mathf.Clamp01(volume);
            ApplyAudioSettings();
        }

        public void SetMusicVolume(float volume)
        {
            currentSettings.musicVolume = Mathf.Clamp01(volume);
            ApplyAudioSettings();
        }

        public void SetSfxVolume(float volume)
        {
            currentSettings.sfxVolume = Mathf.Clamp01(volume);
            ApplyAudioSettings();
        }

        public void SetSubtitlesEnabled(bool enabled)
        {
            currentSettings.subtitlesEnabled = enabled;
        }

        public void SetSubtitleLanguage(string language)
        {
            currentSettings.subtitleLanguage = language;
        }

        public void SetGameLanguage(string language)
        {
            if (System.Enum.TryParse(language, out Language parsedLanguage))
            {
                SetGameLanguage(parsedLanguage);
            }
            else
            {
                Debug.LogWarning($"[SettingsManager] Invalid language string: {language}. Using English as fallback.");
                SetGameLanguage(Language.English);
            }
        }

        public void SetGameLanguage(Language language)
        {
            currentLanguage = language;
            currentSettings.gameLanguage = language.ToString();

            OnLanguageChanged?.Invoke(language);

            Debug.Log($"[SettingsManager] Language changed to: {language}");
        }

        public Language GetCurrentLanguage()
        {
            return currentLanguage;
        }

        #endregion

        #region Utility Methods

        public int GetCurrentResolutionIndex()
        {
            for (int i = 0; i < availableResolutions.Length; i++)
            {
                if (availableResolutions[i].width == currentSettings.resolutionWidth &&
                    availableResolutions[i].height == currentSettings.resolutionHeight)
                {
                    return i;
                }
            }

            return 0;
        }

        public List<string> GetResolutionStrings()
        {
            List<string> resolutionStrings = new List<string>();
            foreach (var res in availableResolutions)
            {
                double refreshRate = (double)res.refreshRateRatio.numerator / res.refreshRateRatio.denominator;
                resolutionStrings.Add($"{res.width} x {res.height} @ {refreshRate:F0}Hz");
            }

            return resolutionStrings;
        }

        #endregion
    }
}