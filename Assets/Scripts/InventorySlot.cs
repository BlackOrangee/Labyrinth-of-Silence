using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts
{
    /// <summary>
    /// Inventory slot item type
    /// </summary>
    public enum InventorySlotType
    {
        Empty,
        Key,
        Newspaper,
        Item
    }

    /// <summary>
    /// UI component for a single inventory slot
    /// Displays item icon and handles click interaction
    /// </summary>
    public class InventorySlot : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Item icon image")]
        public Image iconImage;

        [Tooltip("Item name text (optional)")]
        public Text nameText;

        [Tooltip("Background image")]
        public Image backgroundImage;

        [Tooltip("Button component")]
        public Button slotButton;

        [Header("Visual Settings")]
        [Tooltip("Color for empty slot")]
        public Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        [Tooltip("Color for filled slot")]
        public Color filledColor = new Color(1f, 1f, 1f, 1f);

        [Tooltip("Color for selected slot")]
        public Color selectedColor = new Color(1f, 1f, 0.5f, 1f);

        [Tooltip("Icon color when slot is empty")]
        public Color emptyIconColor = new Color(1f, 1f, 1f, 0f);

        private InventorySlotType slotType = InventorySlotType.Empty;
        private KeyColorType keyData;
        private NewspaperData newspaperData;
        private ItemData itemData;
        private bool isSelected = false;

        private void Awake()
        {
            if (slotButton == null)
                slotButton = GetComponent<Button>();

            if (slotButton != null)
                slotButton.onClick.AddListener(OnSlotClicked);

            SetEmpty();
        }

        /// <summary>
        /// Set slot to display a key
        /// </summary>
        private string keyDescription;

        public void SetKey(KeyColorType keyType, Sprite keyIcon, string keyName, string description = "")
        {
            slotType = InventorySlotType.Key;
            keyData = keyType;
            keyDescription = description;
            newspaperData = null;
            itemData = null;

            if (iconImage != null)
            {
                iconImage.sprite = keyIcon;
                iconImage.color = filledColor;
            }

            if (nameText != null)
            {
                nameText.text = keyName;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = filledColor;
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Set slot to display a newspaper
        /// </summary>
        public void SetNewspaper(NewspaperData newspaper)
        {
            if (newspaper == null)
            {
                SetEmpty();
                return;
            }

            slotType = InventorySlotType.Newspaper;
            keyData = KeyColorType.None;
            newspaperData = newspaper;
            itemData = null;

            if (iconImage != null)
            {
                iconImage.sprite = newspaper.inventoryIcon;
                iconImage.color = filledColor;
            }

            if (nameText != null)
            {
                nameText.text = newspaper.GetLocalizedName();
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = filledColor;
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Set slot to display a general item
        /// </summary>
        public void SetItem(ItemData item)
        {
            if (item == null)
            {
                SetEmpty();
                return;
            }

            slotType = InventorySlotType.Item;
            keyData = KeyColorType.None;
            newspaperData = null;
            itemData = item;

            if (iconImage != null)
            {
                iconImage.sprite = item.inventoryIcon;
                iconImage.color = filledColor;
            }

            if (nameText != null)
            {
                nameText.text = item.itemName;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = filledColor;
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Set slot to empty state
        /// </summary>
        public void SetEmpty()
        {
            slotType = InventorySlotType.Empty;
            keyData = KeyColorType.None;
            newspaperData = null;
            itemData = null;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = emptyIconColor;
            }

            if (nameText != null)
            {
                nameText.text = "";
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = emptyColor;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Handle slot click
        /// </summary>
        private void OnSlotClicked()
        {
            switch (slotType)
            {
                case InventorySlotType.Key:
                    OnKeyClicked();
                    break;

                case InventorySlotType.Newspaper:
                    OnNewspaperClicked();
                    break;

                case InventorySlotType.Item:
                    OnItemClicked();
                    break;
            }
        }

        private void OnKeyClicked()
        {
            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.SelectKey(iconImage?.sprite, nameText?.text, keyDescription, this);
            }
        }

        private void OnNewspaperClicked()
        {
            if (newspaperData == null)
                return;

            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.SelectNewspaper(newspaperData, this);
            }
            else
            {
                Debug.LogError("[InventorySlot] InventoryUI instance not found");
            }
        }

        private void OnItemClicked()
        {
            if (itemData == null)
                return;

            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.SelectItem(itemData, this);
            }
        }

        /// <summary>
        /// Set slot as selected
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateVisualState();
        }

        /// <summary>
        /// Update visual state based on selection
        /// </summary>
        private void UpdateVisualState()
        {
            if (backgroundImage == null)
                return;

            if (slotType == InventorySlotType.Empty)
            {
                backgroundImage.color = emptyColor;
            }
            else if (isSelected)
            {
                backgroundImage.color = selectedColor;
            }
            else
            {
                backgroundImage.color = filledColor;
            }
        }

        public InventorySlotType GetSlotType()
        {
            return slotType;
        }

        public bool IsEmpty()
        {
            return slotType == InventorySlotType.Empty;
        }
    }
}
