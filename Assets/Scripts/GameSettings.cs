using System;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    public class GameSettings
    {
        [Header("Graphics Settings")] public bool fullscreen;
        public int resolutionWidth;
        public int resolutionHeight;
        [Range(0f, 1f)] public float brightness;

        [Header("Controls")] [Range(0.1f, 10f)]
        public float mouseSensitivity;

        [Header("Audio Settings")] [Range(0f, 1f)]
        public float masterVolume;

        [Range(0f, 1f)] public float musicVolume;

        [Range(0f, 1f)] public float sfxVolume;

        [Header("Localization")] public bool subtitlesEnabled;
        public string subtitleLanguage;
        public string gameLanguage;

        public GameSettings()
        {
            fullscreen = true;
            resolutionWidth = 1920;
            resolutionHeight = 1080;
            brightness = 0.5f;
            mouseSensitivity = 1.0f;
            masterVolume = 1.0f;
            musicVolume = 0.8f;
            sfxVolume = 0.8f;
            subtitlesEnabled = true;
            subtitleLanguage = "English";
            gameLanguage = "English";
        }

        public void SetDefaults()
        {
            fullscreen = true;
            resolutionWidth = Screen.currentResolution.width;
            resolutionHeight = Screen.currentResolution.height;
            brightness = 0.5f;
            mouseSensitivity = 1.0f;
            masterVolume = 1.0f;
            musicVolume = 0.8f;
            sfxVolume = 0.8f;
            subtitlesEnabled = true;
            subtitleLanguage = "English";
            gameLanguage = "English";
        }

        public GameSettings Clone()
        {
            return new GameSettings
            {
                fullscreen = this.fullscreen,
                resolutionWidth = this.resolutionWidth,
                resolutionHeight = this.resolutionHeight,
                brightness = this.brightness,
                mouseSensitivity = this.mouseSensitivity,
                masterVolume = this.masterVolume,
                musicVolume = this.musicVolume,
                sfxVolume = this.sfxVolume,
                subtitlesEnabled = this.subtitlesEnabled,
                subtitleLanguage = this.subtitleLanguage,
                gameLanguage = this.gameLanguage
            };
        }

        public void CopyFrom(GameSettings other)
        {
            fullscreen = other.fullscreen;
            resolutionWidth = other.resolutionWidth;
            resolutionHeight = other.resolutionHeight;
            brightness = other.brightness;
            mouseSensitivity = other.mouseSensitivity;
            masterVolume = other.masterVolume;
            musicVolume = other.musicVolume;
            sfxVolume = other.sfxVolume;
            subtitlesEnabled = other.subtitlesEnabled;
            subtitleLanguage = other.subtitleLanguage;
            gameLanguage = other.gameLanguage;
        }
    }
}