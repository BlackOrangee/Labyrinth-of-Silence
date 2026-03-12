using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Assets.Scripts.Localization;

namespace Assets.Scripts
{
    /// <summary>
    /// Inventory filter category
    /// </summary>
    public enum InventoryFilter
    {
        All,
        Keys,
        Documents,
        Items
    }

    /// <summary>
    /// Main inventory UI manager
    /// Handles inventory display, filtering, and user interaction
    /// Opens with Tab key
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        #region Singleton
        private static InventoryUI instance;
        public static InventoryUI Instance
        {
            get { return instance; }
        }
        #endregion

        [Header("UI References")]
        [Tooltip("Main inventory panel (child of this Canvas)")]
        public GameObject inventoryPanel;

        [Tooltip("Grid container for inventory slots")]
        public Transform slotsContainer;

        [Tooltip("Inventory slot prefab")]
        public GameObject slotPrefab;

        [Tooltip("Close button")]
        public Button closeButton;

        [Header("Filter Buttons")]
        [Tooltip("Show all items button")]
        public Button filterAllButton;

        [Tooltip("Show keys only button")]
        public Button filterKeysButton;

        [Tooltip("Show documents only button")]
        public Button filterDocumentsButton;

        [Tooltip("Show items only button")]
        public Button filterItemsButton;

        [Header("Title")]
        [Tooltip("Inventory title text")]
        public Text titleText;

        [Header("Collection Counter")]
        [Tooltip("Text to display collection progress")]
        public Text collectionCounterText;

        [Tooltip("Text to display keys collection progress")]
        public Text keysCounterText;

        [Tooltip("Newspaper database for getting total count")]
        public NewspaperDatabase newspaperDatabase;

        [Tooltip("Total number of collectible keys (non-virtual)")]
        public int totalCollectibleKeys = 4;

        [Header("Preview Panel")]
        [Tooltip("Image to display preview of selected item")]
        public Image previewImage;

        [Tooltip("Button to open full details view")]
        public Button detailsButton;

        [Header("Description Panel")]
        [Tooltip("Description panel container to show/hide")]
        public GameObject descriptionPanel;

        [Tooltip("Text for item name in description panel")]
        public Text descriptionNameText;

        [Tooltip("Text for item title in description panel")]
        public Text descriptionTitleText;

        [Tooltip("Text for item content in description panel")]
        public Text descriptionContentText;

        [Tooltip("Maximum length for description preview text")]
        public int maxDescriptionLength = 150;

        [Header("Item Database")]
        [Tooltip("List of ItemData assets for generic inventory items (canister, etc.)")]
        public List<ItemData> itemDatabase = new List<ItemData>();

        [Header("Key Icons")]
        [Tooltip("Green key icon")]
        public Sprite greenKeyIcon;

        [Tooltip("Yellow key icon")]
        public Sprite yellowKeyIcon;

        [Tooltip("Blue key icon")]
        public Sprite blueKeyIcon;

        [Tooltip("Pink key icon")]
        public Sprite pinkKeyIcon;

        [Header("Settings")]
        [Tooltip("Canvas group for fading (should be on inventoryPanel)")]
        public CanvasGroup canvasGroup;

        [Tooltip("Fade in duration")]
        public float fadeInDuration = 0.2f;

        [Tooltip("Fade out duration")]
        public float fadeOutDuration = 0.15f;

        private SimpleInventory inventory;
        private InventoryFilter currentFilter = InventoryFilter.All;
        private List<InventorySlot> slots = new List<InventorySlot>();
        private bool isOpen = false;
        private float fadeTimer = 0f;
        private bool isFading = false;
        private NewspaperData selectedNewspaper = null;
        private ItemData selectedItem = null;
        private Sprite selectedKeyIcon = null;
        private string selectedKeyName = null;
        private string selectedKeyDescription = null;
        private InventorySlot selectedSlot = null;

        private LocalizedTextDynamic localizedCollectionCounter;
        private LocalizedTextDynamic localizedKeysCounter;
        private CameraController cameraController;
        private PauseBlurController blurController;
        private Canvas _ownCanvas;
        private readonly List<Canvas> _hiddenCanvases = new List<Canvas>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[InventoryUI] Duplicate InventoryUI instance detected. Removing duplicate component.");
                Destroy(this);
                return;
            }

            instance = this;

            if (inventoryPanel != null && inventoryPanel == gameObject)
            {
                Debug.LogError("[InventoryUI] Script should be attached to the Canvas, not to the inventoryPanel itself! Please move this script to the parent Canvas.");
            }

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseInventory);

            if (filterAllButton != null)
                filterAllButton.onClick.AddListener(() => SetFilter(InventoryFilter.All));

            if (filterKeysButton != null)
                filterKeysButton.onClick.AddListener(() => SetFilter(InventoryFilter.Keys));

            if (filterDocumentsButton != null)
                filterDocumentsButton.onClick.AddListener(() => SetFilter(InventoryFilter.Documents));

            if (filterItemsButton != null)
                filterItemsButton.onClick.AddListener(() => SetFilter(InventoryFilter.Items));

            if (detailsButton != null)
                detailsButton.onClick.AddListener(OnDetailsButtonClicked);

            if (inventoryPanel != null)
                inventoryPanel.SetActive(false);

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private void Start()
        {
            if (InteractionLocker.IsLocked)
            {
                InteractionLocker.ForceRelease();
            }

            inventory = FindFirstObjectByType<SimpleInventory>();

            if (inventory != null)
            {
                SimpleInventory.OnInventoryChanged += RefreshInventory;
            }

            if (newspaperDatabase == null)
            {
                newspaperDatabase = FindFirstObjectByType<NewspaperDatabase>();
            }

            if (collectionCounterText != null)
            {
                localizedCollectionCounter = collectionCounterText.GetComponent<LocalizedTextDynamic>();
            }

            if (keysCounterText != null)
            {
                localizedKeysCounter = keysCounterText.GetComponent<LocalizedTextDynamic>();
            }

            SettingsManager.OnLanguageChanged += OnLanguageChanged;

            cameraController = FindObjectOfType<CameraController>();
            blurController = FindObjectOfType<PauseBlurController>();
            _ownCanvas = GetComponentInParent<Canvas>(includeInactive: true) ?? GetComponent<Canvas>();
        }

        /// <summary>
        /// Handle language change - refresh inventory to update key names
        /// </summary>
        private void OnLanguageChanged(Language newLanguage)
        {
            if (isOpen)
            {
                RefreshInventory();
            }
        }

        private void OnDestroy()
        {
            if (inventory != null)
            {
                SimpleInventory.OnInventoryChanged -= RefreshInventory;
            }

            SettingsManager.OnLanguageChanged -= OnLanguageChanged;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleInventory();
            }

            if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseInventory();
            }

            if (isFading)
            {
                fadeTimer += Time.unscaledDeltaTime;
                float duration = isOpen ? fadeInDuration : fadeOutDuration;
                float targetAlpha = isOpen ? 1f : 0f;

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(isOpen ? 0f : 1f, targetAlpha, fadeTimer / duration);
                }

                if (fadeTimer >= duration)
                {
                    isFading = false;
                    if (!isOpen && inventoryPanel != null)
                    {
                        inventoryPanel.SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// Toggle inventory open/close
        /// </summary>
        public void ToggleInventory()
        {
            if (isOpen)
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }

        /// <summary>
        /// Open inventory
        /// </summary>
        public void OpenInventory()
        {
            if (isOpen)
                return;

            if (inventoryPanel == null)
            {
                Debug.LogError("[InventoryUI] Cannot open inventory - inventoryPanel is null! Please assign it in the Inspector.");
                return;
            }

            if (!InteractionLocker.Claim(this))
            {
                Debug.Log("[InventoryUI] Cannot open inventory - interaction is locked");
                return;
            }

            Debug.Log("[InventoryUI] Opening inventory...");
            isOpen = true;
            isFading = true;
            fadeTimer = 0f;

            inventoryPanel.SetActive(true);

            HideOtherCanvases();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Time.timeScale = 0f;

            if (cameraController != null) cameraController.SetInputLock(true);
            if (blurController != null) blurController.SetBlur(true);

            RefreshInventory();
        }

        /// <summary>
        /// Close inventory
        /// </summary>
        public void CloseInventory()
        {
            if (!isOpen)
                return;

            InteractionLocker.Release(this);

            isOpen = false;
            isFading = true;
            fadeTimer = 0f;

            RestoreHiddenCanvases();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Time.timeScale = 1f;

            if (cameraController != null) cameraController.SetInputLock(false);
            if (blurController != null) blurController.SetBlur(false);
        }

        private void HideOtherCanvases()
        {
            _hiddenCanvases.Clear();
            foreach (Canvas canvas in FindObjectsOfType<Canvas>())
            {
                if (canvas == _ownCanvas || !canvas.isActiveAndEnabled) continue;
                if (canvas.transform.parent != null && canvas.transform.parent.GetComponentInParent<Canvas>() != null) continue;
                canvas.enabled = false;
                _hiddenCanvases.Add(canvas);
            }
        }

        private void RestoreHiddenCanvases()
        {
            foreach (Canvas canvas in _hiddenCanvases)
            {
                if (canvas != null)
                    canvas.enabled = true;
            }
            _hiddenCanvases.Clear();
        }

        /// <summary>
        /// Set inventory filter
        /// </summary>
        public void SetFilter(InventoryFilter filter)
        {
            currentFilter = filter;
            RefreshInventory();
        }

        /// <summary>
        /// Refresh inventory display
        /// </summary>
        private void RefreshInventory()
        {
            if (inventory == null || slotsContainer == null)
                return;

            ClearSelection();
            ClearSlots();

            List<KeyColorType> keys = inventory.GetCollectedKeys();
            List<NewspaperData> newspapers = inventory.GetCollectedNewspapers();

            if (currentFilter == InventoryFilter.All || currentFilter == InventoryFilter.Keys)
            {
                foreach (KeyColorType key in keys)
                {
                    if (!key.IsVirtual())
                    {
                        CreateKeySlot(key);
                    }
                }
            }

            if (currentFilter == InventoryFilter.All || currentFilter == InventoryFilter.Documents)
            {
                foreach (NewspaperData newspaper in newspapers)
                {
                    CreateNewspaperSlot(newspaper);
                }
            }

            if (currentFilter == InventoryFilter.All || currentFilter == InventoryFilter.Items)
            {
                List<string> genericItems = inventory.GetItems();
                foreach (string itemName in genericItems)
                {
                    ItemData data = itemDatabase.Find(d => d != null && d.itemId == itemName);
                    if (data != null)
                        CreateItemSlot(data);
                }
            }

            // UpdateTitle();
            UpdateCollectionCounter();
            UpdateKeysCounter();
        }

        /// <summary>
        /// Create slot for a key
        /// </summary>
        private void CreateKeySlot(KeyColorType keyType)
        {
            if (slotPrefab == null || slotsContainer == null)
                return;

            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();

            if (slot != null)
            {
                Sprite keyIcon = GetKeyIcon(keyType);
                string keyName = GetKeyName(keyType);
                slot.SetKey(keyType, keyIcon, keyName, GetKeyDescription(keyType));
                slots.Add(slot);
            }
        }

        /// <summary>
        /// Create slot for a generic item
        /// </summary>
        private void CreateItemSlot(ItemData item)
        {
            if (slotPrefab == null || slotsContainer == null || item == null)
                return;

            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();

            if (slot != null)
            {
                slot.SetItem(item);
                slots.Add(slot);
            }
        }

        /// <summary>
        /// Create slot for a newspaper
        /// </summary>
        private void CreateNewspaperSlot(NewspaperData newspaper)
        {
            if (slotPrefab == null || slotsContainer == null || newspaper == null)
                return;

            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();

            if (slot != null)
            {
                slot.SetNewspaper(newspaper);
                slots.Add(slot);
            }
        }

        /// <summary>
        /// Clear all slots
        /// </summary>
        private void ClearSlots()
        {
            foreach (InventorySlot slot in slots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }

            slots.Clear();
        }

        /// <summary>
        /// Update inventory title
        /// </summary>
        // private void UpdateTitle()
        // {
        //     if (titleText == null)
        //         return;
        //
        //     string filterName = "";
        //     switch (currentFilter)
        //     {
        //         case InventoryFilter.All:
        //             filterName = "All Items";
        //             break;
        //         case InventoryFilter.Keys:
        //             filterName = "Keys";
        //             break;
        //         case InventoryFilter.Documents:
        //             filterName = "Documents";
        //             break;
        //         case InventoryFilter.Items:
        //             filterName = "Items";
        //             break;
        //     }
        //
        //     // titleText.text = $"Inventory - {filterName} ({slots.Count})";
        // }

        /// <summary>
        /// Update collection counter display
        /// </summary>
        private void UpdateCollectionCounter()
        {
            if (collectionCounterText == null)
                return;

            int collectedCount = 0;
            int totalCount = 0;

            if (inventory != null)
            {
                collectedCount = inventory.GetCollectedNewspapersCount();

                if (newspaperDatabase != null)
                {
                    totalCount = newspaperDatabase.allNewspapers.Count;
                }
            }

            if (localizedCollectionCounter != null)
            {
                localizedCollectionCounter.SetText(collectedCount, totalCount);
            }
            else
            {
                collectionCounterText.text = $"{collectedCount}/{totalCount}";
            }
        }

        /// <summary>
        /// Update keys counter display
        /// </summary>
        private void UpdateKeysCounter()
        {
            if (keysCounterText == null)
                return;

            int collectedKeysCount = 0;

            if (inventory != null)
            {
                collectedKeysCount = GetNonVirtualKeysCount();
            }

            if (localizedKeysCounter != null)
            {
                localizedKeysCounter.SetText(collectedKeysCount, totalCollectibleKeys);
            }
            else
            {
                keysCounterText.text = $"{collectedKeysCount}/{totalCollectibleKeys}";
            }
        }

        /// <summary>
        /// Get count of non-virtual keys
        /// </summary>
        private int GetNonVirtualKeysCount()
        {
            if (inventory == null)
                return 0;

            List<KeyColorType> keys = inventory.GetCollectedKeys();
            int count = 0;

            foreach (KeyColorType key in keys)
            {
                if (key == KeyColorType.Green || key == KeyColorType.Yellow)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Get key icon by type
        /// </summary>
        private Sprite GetKeyIcon(KeyColorType keyType)
        {
            switch (keyType)
            {
                case KeyColorType.Green:
                    return greenKeyIcon;
                case KeyColorType.Yellow:
                    return yellowKeyIcon;
                case KeyColorType.Blue:
                    return blueKeyIcon;
                case KeyColorType.Pink:
                    return pinkKeyIcon;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Get localized key name by type
        /// </summary>
        private string GetKeyDescription(KeyColorType keyType)
        {
            Language currentLanguage = SettingsManager.Instance != null
                ? SettingsManager.Instance.GetCurrentLanguage()
                : Language.English;

            switch (keyType)
            {
                case KeyColorType.Green:
                    return currentLanguage == Language.Ukrainian
                        ? "На ключі вигравіювано «GBY 844». Судячи з маркування — це службовий ключ."
                        : "The key is engraved with «GBY 844». Judging by the marking — this is a service key.";
                case KeyColorType.Yellow:
                    return currentLanguage == Language.Ukrainian
                        ? "Звичайний ключ. Від одного з кабінетів у цьому коридорі — тільки б знайти, від якого."
                        : "An ordinary key. From one of the offices in this corridor — if only I knew which one.";
                default:
                    return "";
            }
        }

        private string GetKeyName(KeyColorType keyType)
        {
            Language currentLanguage = SettingsManager.Instance != null
                ? SettingsManager.Instance.GetCurrentLanguage()
                : Language.English;

            switch (keyType)
            {
                case KeyColorType.Green:
                    return currentLanguage == Language.Ukrainian ? "Службовий ключ" : "Service Key";
                case KeyColorType.Yellow:
                    return currentLanguage == Language.Ukrainian ? "Ключ від кабінету" : "Cabinet Key";
                // case KeyColorType.Blue:
                //     return currentLanguage == Language.Ukrainian ? "Синій ключ" : "Blue Key";
                // case KeyColorType.Pink:
                //     return currentLanguage == Language.Ukrainian ? "Рожевий ключ" : "Pink Key";
                default:
                    return currentLanguage == Language.Ukrainian ? "Невідомий ключ" : "Unknown Key";
            }
        }

        /// <summary>
        /// Clear current selection
        /// </summary>
        private void ClearSelection()
        {
            if (selectedSlot != null)
            {
                selectedSlot.SetSelected(false);
            }

            selectedNewspaper = null;
            selectedItem = null;
            selectedKeyIcon = null;
            selectedKeyName = null;
            selectedSlot = null;

            UpdatePreview();
            UpdateDescriptionPanel();
        }

        private void ApplySlotSelection(InventorySlot slot)
        {
            if (selectedSlot != null)
                selectedSlot.SetSelected(false);

            selectedSlot = slot;

            if (selectedSlot != null)
                selectedSlot.SetSelected(true);
        }

        /// <summary>
        /// Select a newspaper to preview
        /// </summary>
        public void SelectNewspaper(NewspaperData newspaper, InventorySlot slot = null)
        {
            selectedNewspaper = newspaper;
            selectedItem = null;
            selectedKeyIcon = null;
            selectedKeyName = null;
            ApplySlotSelection(slot);
            UpdatePreview();
            UpdateDescriptionPanel();
        }

        /// <summary>
        /// Select a key to preview
        /// </summary>
        public void SelectKey(Sprite icon, string keyName, string keyDescription = "", InventorySlot slot = null)
        {
            selectedNewspaper = null;
            selectedItem = null;
            selectedKeyIcon = icon;
            selectedKeyName = keyName;
            selectedKeyDescription = keyDescription;
            ApplySlotSelection(slot);
            UpdatePreview();
            UpdateDescriptionPanel();
        }

        /// <summary>
        /// Select a generic item to preview
        /// </summary>
        public void SelectItem(ItemData item, InventorySlot slot = null)
        {
            selectedNewspaper = null;
            selectedItem = item;
            selectedKeyIcon = null;
            selectedKeyName = null;
            ApplySlotSelection(slot);
            UpdatePreview();
            UpdateDescriptionPanel();
        }

        /// <summary>
        /// Update preview image based on selected item
        /// </summary>
        private void UpdatePreview()
        {
            if (previewImage == null)
                return;

            Sprite sprite = null;

            if (selectedNewspaper != null)
                sprite = selectedNewspaper.GetLocalizedImage();
            else if (selectedItem != null)
                sprite = selectedItem.inventoryIcon;
            else if (selectedKeyIcon != null)
                sprite = selectedKeyIcon;

            if (sprite != null)
            {
                previewImage.sprite = sprite;
                previewImage.color = Color.white;
                previewImage.enabled = true;
            }
            else
            {
                previewImage.sprite = null;
                previewImage.enabled = false;
            }
        }

        /// <summary>
        /// Update description panel with selected item info
        /// </summary>
        private void UpdateDescriptionPanel()
        {
            bool hasSelection = selectedNewspaper != null || selectedItem != null || selectedKeyName != null;

            if (descriptionPanel != null)
                descriptionPanel.SetActive(hasSelection);

            // Details button only for newspapers
            if (detailsButton != null)
                detailsButton.gameObject.SetActive(selectedNewspaper != null);

            if (!hasSelection)
            {
                if (descriptionNameText != null) descriptionNameText.text = "";
                if (descriptionTitleText != null) descriptionTitleText.text = "";
                if (descriptionContentText != null) descriptionContentText.text = "";
                return;
            }

            if (selectedNewspaper != null)
            {
                if (descriptionNameText != null)
                    descriptionNameText.text = selectedNewspaper.GetLocalizedName();

                if (descriptionTitleText != null)
                    descriptionTitleText.text = selectedNewspaper.GetLocalizedTitle();

                if (descriptionContentText != null)
                {
                    string content = selectedNewspaper.GetLocalizedContent();
                    if (!string.IsNullOrEmpty(content) && content.Length > maxDescriptionLength)
                        content = content.Substring(0, maxDescriptionLength) + "...";
                    descriptionContentText.text = content;
                }
            }
            else if (selectedItem != null)
            {
                if (descriptionNameText != null)
                    descriptionNameText.text = selectedItem.GetLocalizedName();

                if (descriptionTitleText != null)
                    descriptionTitleText.text = "";

                if (descriptionContentText != null)
                    descriptionContentText.text = selectedItem.GetLocalizedDescription();
            }
            else if (selectedKeyName != null)
            {
                if (descriptionNameText != null)
                    descriptionNameText.text = selectedKeyName;

                if (descriptionTitleText != null)
                    descriptionTitleText.text = "";

                if (descriptionContentText != null)
                    descriptionContentText.text = selectedKeyDescription ?? "";
            }
        }

        /// <summary>
        /// Handle details button click - opens full newspaper view
        /// </summary>
        private void OnDetailsButtonClicked()
        {
            if (selectedNewspaper == null)
            {
                Debug.LogWarning("[InventoryUI] No newspaper selected to view details");
                return;
            }

            if (NewspaperUI.Instance != null)
            {
                NewspaperUI.Instance.ShowNewspaper(selectedNewspaper);
            }
            else
            {
                Debug.LogError("[InventoryUI] NewspaperUI instance not found");
            }
        }

        public bool IsOpen()
        {
            return isOpen;
        }

        /// <summary>
        /// Helper method to get full path of GameObject in hierarchy
        /// </summary>
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
