using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Assets.Scripts.Localization;

namespace Assets.Scripts
{
    /// <summary>
    /// Plays the ending cutscene based on collected documents and current language.
    /// Good ending = all documents collected. Bad ending = not all.
    /// 4 possible videos: good/bad × English/Ukrainian.
    /// After the video ends (or is skipped), loads MainMenu.
    /// </summary>
    public class EndingVideoManager : MonoBehaviour
    {
        [Header("Ending Videos")]
        [Tooltip("Good ending video — English")]
        public VideoClip goodEndingEng;

        [Tooltip("Good ending video — Ukrainian")]
        public VideoClip goodEndingUkr;

        [Tooltip("Bad ending video — English")]
        public VideoClip badEndingEng;

        [Tooltip("Bad ending video — Ukrainian")]
        public VideoClip badEndingUkr;

        [Header("UI")]
        [Tooltip("RawImage that will show the video (stretch it to cover the screen)")]
        public RawImage videoDisplay;

        [Tooltip("GameObject shown after video ends — 'Press any key' prompt. Optional.")]
        public GameObject pressAnyKeyPrompt;

        [Header("Documents")]
        [Tooltip("Newspaper database — used to count total documents in the game")]
        public NewspaperDatabase newspaperDatabase;

        [Header("Skip")]
        [Tooltip("How many seconds must pass before the player can skip")]
        public float skipCooldown = 2f;

        [Tooltip("Primary skip key")]
        public KeyCode skipKey = KeyCode.Space;

        [Tooltip("Alternative skip key")]
        public KeyCode skipKeyAlt = KeyCode.Return;

        [Header("After Ending")]
        public string mainMenuSceneName = "MainMenu";

        [Serializable]
        private class InventoryTransitData
        {
            public List<string> items = new List<string>();
            public List<string> collectedKeyNames = new List<string>();
            public List<string> collectedNewspaperIds = new List<string>();
        }

        private static string InventoryTransitPath =>
            Path.Combine(Application.persistentDataPath, "transit_inventory.json");

        private VideoPlayer videoPlayer;
        private RenderTexture renderTexture;
        private bool videoFinished;

        private void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            bool goodEnding = CheckGoodEnding();

            Language lang = SettingsManager.Instance != null
                ? SettingsManager.Instance.GetCurrentLanguage()
                : Language.English;

            VideoClip clip = SelectClip(goodEnding, lang);

            Debug.Log($"[EndingVideoManager] Ending={( goodEnding ? "GOOD" : "BAD" )}, Lang={lang}, Clip={(clip != null ? clip.name : "NULL")}");

            if (clip == null)
            {
                Debug.LogError("[EndingVideoManager] No video clip assigned for this ending/language combo — going to main menu.");
                LoadMainMenu();
                return;
            }

            StartCoroutine(PlayEnding(clip));
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnVideoFinished;
                videoPlayer.Stop();
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }

            if (videoDisplay != null)
                videoDisplay.texture = null;
        }

        private bool CheckGoodEnding()
        {
            if (newspaperDatabase == null)
            {
                Debug.LogWarning("[EndingVideoManager] NewspaperDatabase not assigned — defaulting to bad ending.");
                return false;
            }

            int total = newspaperDatabase.allNewspapers.Count;
            if (total <= 0)
            {
                Debug.LogWarning("[EndingVideoManager] NewspaperDatabase is empty — defaulting to bad ending.");
                return false;
            }

            int collected = ReadCollectedNewspapersFromTransit();
            Debug.Log($"[EndingVideoManager] Documents collected: {collected}/{total}");
            return collected >= total;
        }

        private int ReadCollectedNewspapersFromTransit()
        {
            try
            {
                if (!File.Exists(InventoryTransitPath))
                {
                    Debug.LogWarning("[EndingVideoManager] Transit file not found — 0 newspapers.");
                    return 0;
                }

                string json = File.ReadAllText(InventoryTransitPath);
                InventoryTransitData data = JsonUtility.FromJson<InventoryTransitData>(json);
                if (data == null)
                {
                    Debug.LogWarning("[EndingVideoManager] Transit file parse failed — 0 newspapers.");
                    return 0;
                }

                return data.collectedNewspaperIds != null ? data.collectedNewspaperIds.Count : 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"[EndingVideoManager] Transit read error: {e.Message}");
                return 0;
            }
        }

        private VideoClip SelectClip(bool goodEnding, Language lang)
        {
            if (goodEnding)
                return lang == Language.Ukrainian ? goodEndingUkr : goodEndingEng;
            else
                return lang == Language.Ukrainian ? badEndingUkr : badEndingEng;
        }

        private IEnumerator PlayEnding(VideoClip clip)
        {
            GameObject vpObj = new GameObject("EndingVideoPlayer");
            vpObj.transform.SetParent(transform);

            videoPlayer = vpObj.AddComponent<VideoPlayer>();
            videoPlayer.clip = clip;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.isLooping = false;
            videoPlayer.playOnAwake = false;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

            AudioSource audioSource = vpObj.AddComponent<AudioSource>();
            videoPlayer.SetTargetAudioSource(0, audioSource);

            renderTexture = new RenderTexture((int)clip.width, (int)clip.height, 0);
            videoPlayer.targetTexture = renderTexture;

            if (videoDisplay != null)
                videoDisplay.texture = renderTexture;

            videoFinished = false;
            videoPlayer.loopPointReached += OnVideoFinished;

            videoPlayer.Prepare();
            yield return new WaitUntil(() => videoPlayer.isPrepared);

            videoPlayer.Play();

            float elapsed = 0f;

            while (!videoFinished)
            {
                elapsed += Time.deltaTime;

                if (elapsed >= skipCooldown)
                {
                    if (Input.GetKeyDown(skipKey) || Input.GetKeyDown(skipKeyAlt))
                    {
                        Debug.Log("[EndingVideoManager] Video skipped by player.");
                        videoFinished = true;
                    }
                }

                yield return null;
            }

            videoPlayer.Stop();

            if (pressAnyKeyPrompt != null)
                pressAnyKeyPrompt.SetActive(true);

            yield return new WaitUntil(() => Input.anyKeyDown);

            LoadMainMenu();
        }

        private void OnVideoFinished(VideoPlayer vp)
        {
            videoFinished = true;
        }

        private void LoadMainMenu()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene(mainMenuSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
            }
        }
    }
}