using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using Assets.Scripts.Localization;

namespace Assets.Scripts
{
    /// <summary>
    /// Manages the intro video sequence with hold-to-skip functionality
    /// Plays background video (looping) + text video (once) for each slide
    /// </summary>
    public class IntroVideoManager : MonoBehaviour
    {
        #region State Machine

        private enum IntroState
        {
            Initializing,
            PlayingText,
            TextFinished,
            Transitioning,
            Completed
        }

        private IntroState currentState = IntroState.Initializing;

        #endregion

        #region Inspector References

        [Header("Database")]
        [Tooltip("Intro video database with all slides and configuration")]
        public IntroVideoDatabase introDatabase;

        [Header("UI References")]
        [Tooltip("RawImage for background video")]
        public RawImage backgroundImage;

        [Tooltip("TextMeshProUGUI for running text (typewriter effect)")]
        public TMPro.TextMeshProUGUI runningText;

        [Tooltip("Second independent TextMeshProUGUI for running text (optional, enabled per slide)")]
        public TMPro.TextMeshProUGUI secondRunningText;

        [Tooltip("Skip button image (normal/pressed state)")]
        public Image skipButtonImage;

        [Tooltip("Skip button overlay for crossfade effect")]
        public Image skipButtonOverlay;

        [Tooltip("Fill circle image for hold progress")]
        public Image fillCircleImage;

        [Tooltip("AudioSource for slide background audio (looping)")]
        public AudioSource slideAudioSource;

        [Header("Input Settings")]
        [Tooltip("Primary skip key")]
        public KeyCode skipKey = KeyCode.Space;

        [Tooltip("Alternative skip key")]
        public KeyCode skipKeyAlt = KeyCode.Return;

        #endregion

        #region Private Variables

        private VideoPlayer backgroundPlayer;
        private VideoPlayer backgroundLoopPlayer;
        private RenderTexture backgroundRenderTexture;
        private VideoClip backgroundLoopClip;

        private int currentSlideIndex = 0;
        private Language currentLanguage;

        private float holdTime = 0f;
        private bool isHolding = false;
        private bool isTransitioningToPressed = false;
        private Coroutine crossfadeCoroutine;

        private bool isTyping = false;
        private bool isSecondTyping = false;
        private Coroutine typewriterCoroutine;
        private Coroutine secondTypewriterCoroutine;
        private string currentFullText = "";
        private string currentSecondFullText = "";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (introDatabase == null)
            {
                Debug.LogError("[IntroVideoManager] IntroDatabase is not assigned!");
                enabled = false;
                return;
            }

            if (backgroundImage == null || runningText == null)
            {
                Debug.LogError("[IntroVideoManager] Background RawImage or Running Text is not assigned!");
                enabled = false;
                return;
            }

            if (SettingsManager.Instance != null)
            {
                currentLanguage = SettingsManager.Instance.GetCurrentLanguage();
            }
            else
            {
                currentLanguage = Language.English;
                Debug.LogWarning("[IntroVideoManager] SettingsManager not found, using English");
            }

            SetupVideoPlayers();

            SetupSkipButton();
        }

        private void Start()
        {
            StartCoroutine(PlayIntroSequence());
        }

        private void Update()
        {
            if (currentState == IntroState.PlayingText || currentState == IntroState.TextFinished)
            {
                UpdateSkipInput();
            }
        }

        private void OnDestroy()
        {
            CleanupResources();
        }

        #endregion

        #region Setup

        private void SetupVideoPlayers()
        {
            GameObject bgPlayerObj = new GameObject("BackgroundVideoPlayer");
            bgPlayerObj.transform.SetParent(transform);
            backgroundPlayer = bgPlayerObj.AddComponent<VideoPlayer>();
            backgroundPlayer.renderMode = VideoRenderMode.RenderTexture;
            backgroundPlayer.isLooping = false;
            backgroundPlayer.playOnAwake = false;
            backgroundPlayer.audioOutputMode = VideoAudioOutputMode.None;
            backgroundPlayer.loopPointReached += OnBackgroundVideoFinished;

            backgroundRenderTexture = new RenderTexture(1920, 1080, 0);
            backgroundPlayer.targetTexture = backgroundRenderTexture;
            backgroundImage.texture = backgroundRenderTexture;

            if (runningText != null)
            {
                runningText.text = "";
                runningText.color = introDatabase != null ? introDatabase.textColor : Color.white;
                runningText.fontSize = introDatabase != null ? introDatabase.fontSize : 36;

                if (introDatabase != null && introDatabase.textFont != null)
                {
                    runningText.font = introDatabase.textFont;
                }
            }

            if (secondRunningText != null)
            {
                secondRunningText.text = "";
                secondRunningText.color = introDatabase != null ? introDatabase.textColor : Color.white;
                secondRunningText.fontSize = introDatabase != null ? introDatabase.fontSize : 36;

                if (introDatabase != null && introDatabase.textFont != null)
                {
                    secondRunningText.font = introDatabase.textFont;
                }

                secondRunningText.gameObject.SetActive(false);
            }

            Debug.Log("[IntroVideoManager] Video player and text components setup complete");
        }

        private void SetupSkipButton()
        {
            if (skipButtonImage == null || introDatabase.skipButtonData == null)
            {
                Debug.LogWarning("[IntroVideoManager] Skip button not configured");
                return;
            }

            skipButtonImage.sprite = introDatabase.skipButtonData.GetNormalSprite(currentLanguage);

            if (skipButtonOverlay != null)
            {
                skipButtonOverlay.sprite = introDatabase.skipButtonData.GetPressedSprite(currentLanguage);
                Color overlayColor = skipButtonOverlay.color;
                overlayColor.a = 0f;
                skipButtonOverlay.color = overlayColor;
                skipButtonOverlay.gameObject.SetActive(false);
            }

            if (fillCircleImage != null && introDatabase.fillCircleSprite != null)
            {
                fillCircleImage.sprite = introDatabase.fillCircleSprite;
                fillCircleImage.type = Image.Type.Filled;
                fillCircleImage.fillMethod = Image.FillMethod.Radial360;
                fillCircleImage.fillAmount = 0f;
            }

            Debug.Log("[IntroVideoManager] Skip button setup complete");
        }

        #endregion

        #region Intro Sequence

        private IEnumerator PlayIntroSequence()
        {
            Debug.Log($"[IntroVideoManager] Starting intro sequence ({introDatabase.GetSlideCount()} slides)");

            for (currentSlideIndex = 0; currentSlideIndex < introDatabase.GetSlideCount(); currentSlideIndex++)
            {
                yield return PlaySlide(currentSlideIndex);
            }

            currentState = IntroState.Completed;
            Debug.Log("[IntroVideoManager] Intro sequence completed");

            LoadNextScene();
        }

        private IEnumerator PlaySlide(int slideIndex)
        {
            Debug.Log($"[IntroVideoManager] Playing slide {slideIndex + 1}/{introDatabase.GetSlideCount()}");

            IntroVideoSlideData slide = introDatabase.GetSlide(slideIndex);
            if (slide == null || !slide.IsValid())
            {
                Debug.LogError($"[IntroVideoManager] Slide {slideIndex} is invalid, skipping");
                yield break;
            }

            currentState = IntroState.Initializing;

            backgroundPlayer.Stop();
            if (backgroundLoopPlayer != null)
            {
                backgroundLoopPlayer.Stop();
                backgroundLoopPlayer.clip = null;
            }

            backgroundLoopClip = slide.backgroundLoopVideo;

            if (slide.backgroundIntroVideo != null)
            {
                backgroundPlayer.isLooping = false;
                backgroundPlayer.clip = slide.backgroundIntroVideo;
                backgroundPlayer.Prepare();

                if (backgroundLoopPlayer == null)
                {
                    GameObject loopObj = new GameObject("BackgroundLoopPlayer");
                    loopObj.transform.SetParent(transform);
                    backgroundLoopPlayer = loopObj.AddComponent<VideoPlayer>();
                    backgroundLoopPlayer.renderMode = VideoRenderMode.RenderTexture;
                    backgroundLoopPlayer.targetTexture = backgroundRenderTexture;
                    backgroundLoopPlayer.isLooping = true;
                    backgroundLoopPlayer.playOnAwake = false;
                    backgroundLoopPlayer.audioOutputMode = VideoAudioOutputMode.None;
                }

                backgroundLoopPlayer.clip = slide.backgroundLoopVideo;
                backgroundLoopPlayer.Prepare();

                while (!backgroundPlayer.isPrepared)
                {
                    yield return null;
                }
                backgroundPlayer.Play();
            }
            else
            {
                backgroundPlayer.isLooping = true;
                backgroundPlayer.clip = slide.backgroundLoopVideo;
                backgroundPlayer.Prepare();
                while (!backgroundPlayer.isPrepared)
                {
                    yield return null;
                }
                backgroundPlayer.Play();
            }

            if (slideAudioSource != null)
            {
                if (slide.slideAudio != null)
                {
                    // Only restart if it's a different clip
                    if (slideAudioSource.clip != slide.slideAudio)
                    {
                        slideAudioSource.Stop();
                        slideAudioSource.clip = slide.slideAudio;
                        slideAudioSource.loop = true;
                        slideAudioSource.Play();
                    }
                }
                else
                {
                    // No audio on this slide — stop whatever was playing
                    slideAudioSource.Stop();
                    slideAudioSource.clip = null;
                }
            }

            string text = slide.GetText(currentLanguage);
            currentFullText = text;

            currentState = IntroState.PlayingText;
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(TypewriterEffect(text, false));

            if (slide.hasSecondText && secondRunningText != null)
            {
                string secondText = slide.GetSecondText(currentLanguage);
                currentSecondFullText = secondText;
                secondRunningText.gameObject.SetActive(true);
                if (secondTypewriterCoroutine != null) StopCoroutine(secondTypewriterCoroutine);
                secondTypewriterCoroutine = StartCoroutine(TypewriterEffect(secondText, true));
            }
            else
            {
                isSecondTyping = false;
                currentSecondFullText = "";
                if (secondRunningText != null) secondRunningText.gameObject.SetActive(false);
            }

            yield return new WaitUntil(() => !isTyping && !isSecondTyping);

            currentState = IntroState.TextFinished;

            while (currentState == IntroState.TextFinished)
            {
                yield return null;
            }

            Debug.Log($"[IntroVideoManager] Slide {slideIndex} completed");
        }

        #endregion

        #region Input Handling

        private void UpdateSkipInput()
        {
            bool skipPressed = Input.GetKey(skipKey) || Input.GetKey(skipKeyAlt);

            if (skipPressed)
            {
                if (!isHolding)
                {
                    isHolding = true;
                    holdTime = 0f;
                }
                else
                {
                    holdTime += Time.deltaTime;

                    if (fillCircleImage != null)
                    {
                        fillCircleImage.fillAmount = Mathf.Clamp01(holdTime / introDatabase.holdDuration);
                    }

                    if (holdTime >= introDatabase.holdDuration && !isTransitioningToPressed)
                    {
                        OnSkipHoldCompleted();
                    }
                }
            }
            else
            {
                if (isHolding)
                {
                    isHolding = false;
                    holdTime = 0f;

                    if (fillCircleImage != null)
                    {
                        fillCircleImage.fillAmount = 0f;
                    }
                }
            }
        }

        private void OnSkipHoldCompleted()
        {
            isTransitioningToPressed = true;

            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
            }
            crossfadeCoroutine = StartCoroutine(TransitionToPressed());
        }

        private IEnumerator TransitionToPressed()
        {
            float crossfade = introDatabase != null ? introDatabase.crossfadeDuration : 0.2f;
            float pressedHold = introDatabase != null ? introDatabase.pressedHoldDuration : 0.4f;

            if (skipButtonOverlay != null)
            {
                skipButtonOverlay.gameObject.SetActive(true);
                SetImageAlpha(skipButtonOverlay, 0f);
            }
            if (skipButtonImage != null)
            {
                SetImageAlpha(skipButtonImage, 1f);
            }

            float elapsed = 0f;
            while (elapsed < crossfade)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / crossfade);
                if (skipButtonImage != null)   SetImageAlpha(skipButtonImage, 1f - t);
                if (skipButtonOverlay != null) SetImageAlpha(skipButtonOverlay, t);
                yield return null;
            }

            SetImageAlpha(skipButtonImage, 0f);
            SetImageAlpha(skipButtonOverlay, 1f);

            yield return new WaitForSeconds(pressedHold);

            ExecuteSkipAction();

            elapsed = 0f;
            while (elapsed < crossfade)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / crossfade);
                if (skipButtonImage != null)   SetImageAlpha(skipButtonImage, t);
                if (skipButtonOverlay != null) SetImageAlpha(skipButtonOverlay, 1f - t);
                yield return null;
            }

            SetImageAlpha(skipButtonImage, 1f);

            if (skipButtonOverlay != null)
            {
                skipButtonOverlay.gameObject.SetActive(false);
            }

            crossfadeCoroutine = null;
        }

        private void SetImageAlpha(Image image, float alpha)
        {
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }

        private void ExecuteSkipAction()
        {
            if (currentState == IntroState.PlayingText)
            {
                SkipToTextEnd();
            }
            else if (currentState == IntroState.TextFinished)
            {
                AdvanceToNextSlide();
            }

            ResetSkipButton();
        }

        private void SkipToTextEnd()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }

            if (runningText != null && !string.IsNullOrEmpty(currentFullText))
            {
                runningText.text = currentFullText;
                runningText.maxVisibleCharacters = currentFullText.Length;
            }

            isTyping = false;

            if (secondTypewriterCoroutine != null)
            {
                StopCoroutine(secondTypewriterCoroutine);
                secondTypewriterCoroutine = null;
            }

            if (secondRunningText != null && !string.IsNullOrEmpty(currentSecondFullText))
            {
                secondRunningText.text = currentSecondFullText;
                secondRunningText.maxVisibleCharacters = currentSecondFullText.Length;
            }

            isSecondTyping = false;

            currentState = IntroState.TextFinished;
        }

        private void AdvanceToNextSlide()
        {
            currentState = IntroState.Transitioning;
        }

        private void ResetSkipButton()
        {
            isHolding = false;
            holdTime = 0f;
            isTransitioningToPressed = false;

            if (fillCircleImage != null)
            {
                fillCircleImage.fillAmount = 0f;
            }
        }


        #endregion

        #region Background Video Events

        private void OnBackgroundVideoFinished(VideoPlayer vp)
        {
            if (vp != backgroundPlayer)
            {
                return;
            }

            if (backgroundLoopClip == null || vp.clip == backgroundLoopClip)
            {
                return;
            }

            backgroundPlayer.Stop();

            if (backgroundLoopPlayer != null)
            {
                if (backgroundLoopPlayer.isPrepared)
                {
                    backgroundLoopPlayer.Play();
                }
                else
                {
                    StartCoroutine(WaitAndPlayLoopPlayer());
                }
            }
        }

        private IEnumerator WaitAndPlayLoopPlayer()
        {
            while (backgroundLoopPlayer != null && !backgroundLoopPlayer.isPrepared)
            {
                yield return null;
            }
            backgroundLoopPlayer?.Play();
        }

        #endregion

        #region Typewriter Effect

        private IEnumerator TypewriterEffect(string fullText, bool isSecond)
        {
            TextMeshProUGUI target = isSecond ? secondRunningText : runningText;

            if (target == null || string.IsNullOrEmpty(fullText))
            {
                if (isSecond) isSecondTyping = false;
                else isTyping = false;
                yield break;
            }

            if (isSecond) isSecondTyping = true;
            else isTyping = true;

            target.text = fullText;
            target.maxVisibleCharacters = 0;

            float charsPerSecond = introDatabase != null ? introDatabase.charsPerSecond : 30f;
            float delay = 1f / charsPerSecond;

            int charIndex = 0;
            while (charIndex < fullText.Length)
            {
                charIndex++;
                target.maxVisibleCharacters = charIndex;
                yield return new WaitForSeconds(delay);
            }

            if (isSecond)
            {
                isSecondTyping = false;
                secondTypewriterCoroutine = null;
            }
            else
            {
                isTyping = false;
                typewriterCoroutine = null;
            }

            Debug.Log($"[IntroVideoManager] {(isSecond ? "Second t" : "T")}ypewriter completed");
        }

        #endregion

        #region Scene Transition

        private void LoadNextScene()
        {
            Debug.Log("[IntroVideoManager] Loading next scene...");

            if (SceneLoader.Instance == null)
            {
                Debug.LogError("[IntroVideoManager] SceneLoader not found!");
                return;
            }

            if (introDatabase.useSceneName)
            {
                SceneLoader.Instance.LoadScene(introDatabase.nextSceneName);
            }
            else
            {
                SceneLoader.Instance.LoadScene(introDatabase.nextSceneIndex);
            }
        }

        #endregion

        #region Memory Cleanup

        private void CleanupResources()
        {
            Debug.Log("[IntroVideoManager] Cleaning up resources...");

            StopAllCoroutines();

            if (backgroundPlayer != null)
            {
                backgroundPlayer.loopPointReached -= OnBackgroundVideoFinished;
                backgroundPlayer.Stop();
                Destroy(backgroundPlayer.gameObject);
                backgroundPlayer = null;
            }

            if (backgroundLoopPlayer != null)
            {
                backgroundLoopPlayer.Stop();
                Destroy(backgroundLoopPlayer.gameObject);
                backgroundLoopPlayer = null;
            }

            if (backgroundRenderTexture != null)
            {
                backgroundRenderTexture.Release();
                Destroy(backgroundRenderTexture);
                backgroundRenderTexture = null;
            }

            if (backgroundImage != null)
            {
                backgroundImage.texture = null;
            }

            if (runningText != null)
            {
                runningText.text = "";
            }

            if (slideAudioSource != null)
            {
                slideAudioSource.Stop();
            }

            Resources.UnloadUnusedAssets();

            Debug.Log("[IntroVideoManager] Cleanup complete");
        }

        #endregion
    }
}
