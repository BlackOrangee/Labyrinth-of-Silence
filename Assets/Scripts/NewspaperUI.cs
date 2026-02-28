using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Assets.Scripts
{
    /// <summary>
    /// UI manager for displaying newspapers in fullscreen view
    /// </summary>
    public class NewspaperUI : MonoBehaviour
    {
        #region Singleton
        private static NewspaperUI _instance;

        public static NewspaperUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<NewspaperUI>();

                    if (_instance == null)
                    {
                        Debug.LogWarning("NewspaperUI: No instance found in scene.");
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("NewspaperUI: Duplicate instance detected, destroying.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            InitializeUI();
        }
        #endregion

        #region UI References
        [Header("UI References")]
        [Tooltip("Root GameObject for the newspaper panel")]
        public GameObject newspaperPanel;

        [Tooltip("Image component to display the newspaper")]
        public Image newspaperImage;

        [Tooltip("Text component for newspaper title")]
        public Text titleText;

        [Tooltip("Text component for newspaper content")]
        public Text contentText;

        [Tooltip("Close button to hide the newspaper")]
        public Button closeButton;

        [Header("Animation Settings")]
        [Tooltip("Use fade animation when showing/hiding")]
        public bool useFadeAnimation = true;

        [Tooltip("Duration of fade in animation (seconds)")]
        public float fadeInDuration = 0.3f;

        [Tooltip("Duration of fade out animation (seconds)")]
        public float fadeOutDuration = 0.3f;

        [Header("Audio")]
        [Tooltip("Sound to play when opening newspaper")]
        public AudioClip openSound;

        [Tooltip("Sound to play when closing newspaper")]
        public AudioClip closeSound;

        [Range(0f, 1f)]
        [Tooltip("Volume for newspaper sounds")]
        public float soundVolume = 0.5f;
        #endregion

        #region Private Fields
        private CanvasGroup canvasGroup;
        private Coroutine fadeCoroutine;
        private bool isShowing = false;
        private NewspaperData currentNewspaper;
        private AudioSource audioSource;
        private bool wasInventoryOpenBefore = false;
        private float showTime = 0f;
        private const float INPUT_DELAY = 0.3f;
        private CameraController cameraController;
        private PauseBlurController blurController;
        #endregion

        #region Initialization
        private void InitializeUI()
        {
            if (newspaperPanel != null)
            {
                canvasGroup = newspaperPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null && useFadeAnimation)
                {
                    canvasGroup = newspaperPanel.AddComponent<CanvasGroup>();
                }
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && (openSound != null || closeSound != null))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            HideImmediate();

            cameraController = FindObjectOfType<CameraController>();
            blurController = FindObjectOfType<PauseBlurController>();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Shows the newspaper with the given data
        /// </summary>
        /// <param name="newspaperData">Data containing newspaper information and image</param>
        public void ShowNewspaper(NewspaperData newspaperData)
        {
            if (newspaperData == null)
            {
                Debug.LogWarning("NewspaperUI: Cannot show newspaper - data is null.");
                return;
            }

            if (newspaperImage == null || newspaperPanel == null)
            {
                Debug.LogError("NewspaperUI: UI components not assigned in inspector.");
                return;
            }

            if (isShowing)
            {
                Debug.Log("NewspaperUI: Already showing a newspaper, hiding first.");
                HideNewspaper();
            }

            wasInventoryOpenBefore = InventoryUI.Instance != null && InventoryUI.Instance.IsOpen();

            if (wasInventoryOpenBefore && InventoryUI.Instance != null)
            {
                InventoryUI.Instance.CloseInventory();
            }

            currentNewspaper = newspaperData;
            newspaperImage.sprite = newspaperData.GetLocalizedImage();

            // Set title and content
            if (titleText != null)
            {
                titleText.text = newspaperData.GetLocalizedTitle();
            }

            if (contentText != null)
            {
                contentText.text = newspaperData.GetLocalizedContent();
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = StartCoroutine(ShowNewspaperCoroutine());

            PlaySound(openSound);

            Time.timeScale = 0f;
            showTime = Time.unscaledTime;

            if (cameraController != null) cameraController.SetInputLock(true);
            if (blurController != null) blurController.SetBlur(true);

            Debug.Log($"NewspaperUI: Showing newspaper '{newspaperData.GetLocalizedName()}' (ID: {newspaperData.newspaperId})");
        }

        /// <summary>
        /// Hides the currently displayed newspaper
        /// </summary>
        public void HideNewspaper()
        {
            if (!isShowing)
            {
                return;
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = StartCoroutine(HideNewspaperCoroutine());

            PlaySound(closeSound);

            Time.timeScale = 1f;

            if (cameraController != null) cameraController.SetInputLock(false);
            if (blurController != null) blurController.SetBlur(false);

            if (wasInventoryOpenBefore && InventoryUI.Instance != null)
            {
                InventoryUI.Instance.OpenInventory();
            }

            Debug.Log("NewspaperUI: Hiding newspaper.");
        }

        /// <summary>
        /// Checks if a newspaper is currently being displayed
        /// </summary>
        public bool IsShowing()
        {
            return isShowing;
        }
        #endregion

        #region Private Methods
        private IEnumerator ShowNewspaperCoroutine()
        {
            isShowing = true;

            if (newspaperPanel != null)
            {
                newspaperPanel.SetActive(true);
            }

            if (useFadeAnimation && canvasGroup != null)
            {
                float elapsedTime = 0f;
                while (elapsedTime < fadeInDuration)
                {
                    elapsedTime += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
                    yield return null;
                }
                canvasGroup.alpha = 1f;
            }

            fadeCoroutine = null;
        }

        private IEnumerator HideNewspaperCoroutine()
        {
            if (useFadeAnimation && canvasGroup != null)
            {
                float elapsedTime = 0f;
                float startAlpha = canvasGroup.alpha;

                while (elapsedTime < fadeOutDuration)
                {
                    elapsedTime += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutDuration);
                    yield return null;
                }
                canvasGroup.alpha = 0f;
            }

            if (newspaperPanel != null)
            {
                newspaperPanel.SetActive(false);
            }

            isShowing = false;
            currentNewspaper = null;
            fadeCoroutine = null;
        }

        private void HideImmediate()
        {
            if (newspaperPanel != null)
            {
                newspaperPanel.SetActive(false);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            isShowing = false;
            currentNewspaper = null;
        }

        private void OnCloseButtonClicked()
        {
            HideNewspaper();
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, soundVolume);
            }
        }
        #endregion

        #region Input Handling
        private void Update()
        {
            if (isShowing)
            {
                float timeSinceShow = Time.unscaledTime - showTime;

                if (timeSinceShow < INPUT_DELAY)
                {
                    return;
                }

                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
                {
                    HideNewspaper();
                }
            }
        }
        #endregion

        #region Validation
        private void OnValidate()
        {
            if (newspaperPanel == null)
            {
                Debug.LogWarning("NewspaperUI: newspaperPanel is not assigned! Assign the UI GameObject in the inspector.");
            }

            if (newspaperImage == null)
            {
                Debug.LogWarning("NewspaperUI: newspaperImage is not assigned! Assign the Image component in the inspector.");
            }

            if (titleText == null)
            {
                Debug.LogWarning("NewspaperUI: titleText is not assigned! Assign the TextMeshProUGUI component for title in the inspector.");
            }

            if (contentText == null)
            {
                Debug.LogWarning("NewspaperUI: contentText is not assigned! Assign the TextMeshProUGUI component for content in the inspector.");
            }
        }
        #endregion
    }
}
