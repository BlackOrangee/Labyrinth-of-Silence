using UnityEngine;
using UnityEngine.Video;

namespace Assets.Scripts.Localization
{
    /// <summary>
    /// Компонент для локалізації відео. 
    /// Вішається на той самий об'єкт, де знаходиться VideoPlayer.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class LocalizedVideo : MonoBehaviour
    {
        private VideoPlayer videoPlayer;

        [Header("Localized Video Clips")]
        [Tooltip("Відео для англійської мови")]
        public VideoClip englishClip;

        [Tooltip("Відео для української мови")]
        public VideoClip ukrainianClip;

        void Awake()
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        void OnEnable()
        {
            SettingsManager.OnLanguageChanged += OnLanguageChanged;
            
            UpdateVideoClip(); 
        }

        void OnDisable()
        {
            SettingsManager.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(Language newLanguage)
        {
            UpdateVideoClip(newLanguage);
        }

        private void UpdateVideoClip()
        {
            if (SettingsManager.Instance != null)
            {
                UpdateVideoClip(SettingsManager.Instance.GetCurrentLanguage());
            }
        }

        private void UpdateVideoClip(Language language)
        {
            if (videoPlayer == null) return;

            VideoClip clipToPlay = englishClip;

            switch (language)
            {
                case Language.Ukrainian:
                    if (ukrainianClip != null) clipToPlay = ukrainianClip;
                    break;
                case Language.English:
                default:
                    if (englishClip != null) clipToPlay = englishClip;
                    break;
            }

            if (videoPlayer.clip != clipToPlay)
            {
                bool wasPlaying = videoPlayer.isPlaying;

                videoPlayer.clip = clipToPlay;

                if (wasPlaying)
                {
                    videoPlayer.Play();
                }
            }
        }
    }
}