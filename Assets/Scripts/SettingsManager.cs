using UnityEngine;
using UnityEngine.Audio;
using System.IO;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public class SettingsManager : MonoBehaviour
    {
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

        private string[] availableLanguages = { "English", "Ukrainian", "French", "German", "Spanish" };
        public string[] AvailableLanguages => availableLanguages;

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

            ApplyAllSettings();
        }

        #region Save/Load

        public void SaveSettings()
        {
            try
            {
                string json = JsonUtility.ToJson(currentSettings, true);
                File.WriteAllText(settingsFilePath, json);
                Debug.Log($"[SettingsManager] Settings saved {settingsFilePath}");
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
                    Debug.Log("[SettingsManager] Setting loaded");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SettingsManager] Setting loading error: {e.Message}");
                    currentSettings = new GameSettings();
                }
            }
            else
            {
                Debug.Log("[SettingsManager] Used default settings. File not found:");
                currentSettings = new GameSettings();
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
        }

        public void ApplyGraphicsSettings()
        {
            Screen.SetResolution(
                currentSettings.resolutionWidth,
                currentSettings.resolutionHeight,
                currentSettings.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed
            );

            RenderSettings.ambientLight = Color.white * currentSettings.brightness;

            Debug.Log(
                $"[SettingsManager] Graphic settings: {currentSettings.resolutionWidth}x{currentSettings.resolutionHeight}, Fullscreen: {currentSettings.fullscreen}");
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

                audioMixer.SetFloat("MasterVolume", masterVolumeDb);
                audioMixer.SetFloat("MusicVolume", musicVolumeDb);
            }
            else
            {
                AudioListener.volume = currentSettings.masterVolume;
            }

            Debug.Log(
                $"[SettingsManager] Audio settings: Master={currentSettings.masterVolume}, Music={currentSettings.musicVolume}");
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
            currentSettings.mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
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
            currentSettings.gameLanguage = language;
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