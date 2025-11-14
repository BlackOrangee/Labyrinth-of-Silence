using UnityEngine;
using TMPro;
namespace Assets.Scripts
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerHideController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Player camera")]
        public Camera playerCamera;
        [Tooltip("Canvas for black screen")]
        public Canvas hideCanvas;

        [Tooltip("UI Image for black screen (must be on hideCanvas)")]
        public UnityEngine.UI.Image blackScreen;

        [Tooltip("Message text on black screen")]
        public TextMeshProUGUI hideText;

        [Header("Settings")]
        [Tooltip("Is player currently hiding?")]
        [SerializeField] private bool isHiding = false;

        private HidingSpot currentHidingSpot;
        private PlayerMovement playerMovement;
        private CharacterController characterController;
        private PlayerInteractor playerInteractor;
        private RaycastInteractor raycastInteractor;

        private bool wasMovementEnabled;
        private bool wasInteractorEnabled;
        private bool wasRaycastEnabled;

        public bool IsHiding => isHiding;

        void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();
            characterController = GetComponent<CharacterController>();
            playerInteractor = GetComponent<PlayerInteractor>();
            raycastInteractor = GetComponent<RaycastInteractor>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null)
                {
                    Debug.LogError("PlayerHideController: Camera not found!");
                }
            }
        }

        void Start()
        {
            if (hideCanvas == null || blackScreen == null || hideText == null)
            {
                CreateHideCanvas();
            }

            if (hideCanvas != null)
            {
                hideCanvas.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (isHiding && Input.GetKeyDown(KeyCode.Escape))
            {
                ExitHiding();
            }
        }

        public void EnterHiding(HidingSpot hidingSpot)
        {
            if (isHiding)
            {
                Debug.LogWarning("PlayerHideController: Player already hidden!");
                return;
            }

            currentHidingSpot = hidingSpot;
            isHiding = true;

            wasMovementEnabled = playerMovement != null && playerMovement.enabled;
            wasInteractorEnabled = playerInteractor != null && playerInteractor.enabled;
            wasRaycastEnabled = raycastInteractor != null && raycastInteractor.enabled;

            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }

            if (playerInteractor != null)
            {
                playerInteractor.enabled = false;
            }

            if (raycastInteractor != null)
            {
                raycastInteractor.enabled = false;
            }

            ShowBlackScreen(true, $"Player in hiding spot: {hidingSpot.GetSpotName()}\n\nPress ESC to exit");
        }

        public void ExitHiding()
        {
            if (!isHiding || currentHidingSpot == null)
            {
                Debug.LogWarning("PlayerHideController: Player not in hiding!");
                return;
            }

            currentHidingSpot.ExitHiding();

            if (playerMovement != null)
            {
                playerMovement.enabled = wasMovementEnabled;
            }

            if (playerInteractor != null)
            {
                playerInteractor.enabled = wasInteractorEnabled;
            }

            if (raycastInteractor != null)
            {
                raycastInteractor.enabled = wasRaycastEnabled;
            }

            ShowBlackScreen(false, "");

            isHiding = false;
            currentHidingSpot = null;
        }

        private void ShowBlackScreen(bool show, string text = "")
        {
            if (hideCanvas != null)
            {
                hideCanvas.gameObject.SetActive(show);

                if (hideText != null)
                {
                    hideText.text = text;
                }
            }
        }

        private void CreateHideCanvas()
        {
            GameObject canvasObj = new GameObject("HideCanvas");
            canvasObj.transform.SetParent(transform);

            hideCanvas = canvasObj.AddComponent<Canvas>();
            hideCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hideCanvas.sortingOrder = 9999;

            var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            GameObject imageObj = new GameObject("BlackScreen");
            imageObj.transform.SetParent(canvasObj.transform, false);

            blackScreen = imageObj.AddComponent<UnityEngine.UI.Image>();
            blackScreen.color = Color.black;

            RectTransform rectImage = imageObj.GetComponent<RectTransform>();
            rectImage.anchorMin = Vector2.zero;
            rectImage.anchorMax = Vector2.one;
            rectImage.sizeDelta = Vector2.zero;
            rectImage.anchoredPosition = Vector2.zero;

            GameObject textObj = new GameObject("HideText");
            textObj.transform.SetParent(canvasObj.transform, false);

            hideText = textObj.AddComponent<TextMeshProUGUI>();
            hideText.text = "";
            hideText.fontSize = 36;
            hideText.color = Color.white;
            hideText.alignment = TextAlignmentOptions.Center;
            hideText.fontStyle = FontStyles.Bold;

            RectTransform rectText = textObj.GetComponent<RectTransform>();
            rectText.anchorMin = Vector2.zero;
            rectText.anchorMax = Vector2.one;
            rectText.sizeDelta = Vector2.zero;
            rectText.anchoredPosition = Vector2.zero;
        }
    }
}