using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts;

public class LoadSaveMenuController : MonoBehaviour
{

    [Tooltip("Title name")]
    public string titleName = "Title";
    [Tooltip("Content name")]
    public string contentName = "Content";
    [Tooltip("Save item name")]
    public string saveItemName = "SaveItem";
    [Tooltip("Save button name")]
    public string saveButtonName = "SaveButton";
    [Tooltip("Load button name")]
    public string loadButtonName = "LoadButton";
    [Tooltip("Delete button name")]
    public string deleteButtonName = "DeleteButton";

    [Header("Save Item UI Elements")]
    [Tooltip("Text element name for save name")]
    public string saveNameTextName = "SaveNameText";
    [Tooltip("Text element name for save time")]
    public string saveTimeTextName = "SaveTimeText";
    [Tooltip("Text element name for quest info")]
    public string questInfoTextName = "QuestInfoText";
    [Tooltip("Image element name for screenshot")]
    public string screenshotImageName = "ScreenshotImage";
    public string screenshotImageName2 = "ScreenshotImage2";
    public string screenshotImageName3 = "ScreenshotImage3";
    public string screenshotImageName4 = "ScreenshotImage4";

    private GameObject title;
    private GameObject content;
    private GameObject saveItem;

    private bool saveMenu = false;
    private List<GameObject> saveItems = new List<GameObject>();
    private bool initialized = false;
    private List<Texture2D> cachedTextures = new List<Texture2D>();
    private List<Sprite> cachedSprites = new List<Sprite>();

    void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized) return;

        title = transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == titleName)?.gameObject;

        if (!title)
        {
            Debug.LogError("title not found");
            return;
        }


        content = transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == contentName)?.gameObject;

        if (!content)
        {
            Debug.LogError("content not found");
            return;
        }

        saveItem = transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == saveItemName)?.gameObject;

        if (!saveItem)
        {
            Debug.LogError("saveItem not found");
            return;
        }
        saveItem.SetActive(false);

        GameObject saveButton = saveItem.transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == saveButtonName)?.gameObject;
        if (saveButton)
        {
            saveButton.SetActive(false);
        }

        GameObject loadButton = saveItem.transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == loadButtonName)?.gameObject;
        if (loadButton)
        {
            loadButton.SetActive(false);
        }

        initialized = true;
    }

    public void ShowSaveMenu()
    {
        Initialize();

        title.GetComponent<Text>().text = "Save Game";
        content.SetActive(true);

        saveMenu = true;
        FillMenu();
    }

    public void ShowLoadMenu()
    {
        Initialize();

        title.GetComponent<Text>().text = "Load Game";
        content.SetActive(true);

        saveMenu = false;
        FillMenu();
    }

    private void ClearMenu()
    {
        foreach (GameObject item in saveItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        saveItems.Clear();

        foreach (var sprite in cachedSprites)
            if (sprite != null) Destroy(sprite);
        cachedSprites.Clear();

        foreach (var tex in cachedTextures)
            if (tex != null) Destroy(tex);
        cachedTextures.Clear();
    }

    private void FillMenu()
    {
        ClearMenu();

        if (SaveManager.Instance == null)
        {
            Debug.LogError("[LoadSaveMenuController] SaveManager instance not found!");
            return;
        }

        RectTransform saveItemRect = saveItem.GetComponent<RectTransform>();
        float itemHeight = saveItemRect.rect.height;
        float spacing = 10f;

        Vector2 saveItemPos = new Vector2(0, -itemHeight / 2);

        int maxSlots = SaveManager.Instance.maxSaveSlots;

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject newSaveItem = Instantiate(this.saveItem);
            RectTransform newSaveItemRect = newSaveItem.GetComponent<RectTransform>();

            newSaveItem.transform.SetParent(content.transform, false);
            newSaveItem.transform.localScale = Vector3.one;

            newSaveItemRect.anchoredPosition = saveItemPos;
            newSaveItem.SetActive(true);

            int slotIndex = i;
            PopulateSaveSlot(newSaveItem, slotIndex);

            saveItems.Add(newSaveItem);

            saveItemPos.y -= (itemHeight + spacing);
        }

        RectTransform contentRect = content.GetComponent<RectTransform>();
        float totalHeight = (itemHeight * maxSlots) + (spacing * (maxSlots - 1)) + spacing;
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);
    }

    private void PopulateSaveSlot(GameObject slotObject, int slotIndex)
    {
        SaveData saveData = SaveManager.Instance.GetSaveData(slotIndex);
        bool saveExists = saveData != null;

        Text saveNameText = slotObject.transform.GetComponentsInChildren<Text>(includeInactive: true)
            .FirstOrDefault(t => t.name == saveNameTextName);

        Text saveTimeText = slotObject.transform.GetComponentsInChildren<Text>(includeInactive: true)
            .FirstOrDefault(t => t.name == saveTimeTextName);

        Text questInfoText = slotObject.transform.GetComponentsInChildren<Text>(includeInactive: true)
            .FirstOrDefault(t => t.name == questInfoTextName);

        Transform screenshotTransform = slotObject.transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == screenshotImageName);
        Transform screenshotTransform2 = slotObject.transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == screenshotImageName2);
        Transform screenshotTransform3 = slotObject.transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == screenshotImageName3);
        Transform screenshotTransform4 = slotObject.transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == screenshotImageName4);

        Image screenshotImage = screenshotTransform?.GetComponent<Image>();
        Image screenshotImage2 = screenshotTransform2?.GetComponent<Image>();
        Image screenshotImage3 = screenshotTransform3?.GetComponent<Image>();
        Image screenshotImage4 = screenshotTransform4?.GetComponent<Image>();
        if (saveExists)
        {
            if (saveNameText != null)
            {
                saveNameText.text = saveData.saveName;
            }

            if (saveTimeText != null)
            {
                saveTimeText.text = saveData.saveDateTime;
            }

            if (questInfoText != null)
            {
                questInfoText.text = string.IsNullOrEmpty(saveData.currentQuest)
                    ? saveData.chapter
                    : $"{saveData.chapter} - {saveData.currentQuest}";
            }

            if (screenshotImage != null && 
                screenshotImage2 != null && 
                screenshotImage3 != null && 
                screenshotImage4 != null && 
                !string.IsNullOrEmpty(saveData.screenshotPath))
            {
                LoadScreenshot(screenshotImage, saveData.screenshotPath);
                LoadScreenshot(screenshotImage2, saveData.screenshotPath);
                LoadScreenshot(screenshotImage3, saveData.screenshotPath);
                LoadScreenshot(screenshotImage4, saveData.screenshotPath);
            }
        }
        else
        {
            if (saveNameText != null)
            {
                saveNameText.text = saveMenu ? $"Empty Slot {slotIndex + 1}" : $"Slot {slotIndex + 1}";
            }

            if (saveTimeText != null)
            {
                saveTimeText.text = saveMenu ? "Click to Save" : "No Save";
            }

            if (questInfoText != null)
            {
                questInfoText.text = "";
            }

            if (screenshotImage != null)
            {
                screenshotImage.color = new Color(0.2f, 0.2f, 0.2f);
            }
            if (screenshotImage2 != null)
            {
                screenshotImage2.color = new Color(0.2f, 0.2f, 0.2f, 0.43137255f);
            }
            if (screenshotImage3 != null)
            {
                screenshotImage3.color = new Color(0.2f, 0.2f, 0.2f, 0.22745098f);
            }
            if (screenshotImage4 != null)
            {
                screenshotImage4.color = new Color(0.2f, 0.2f, 0.2f, 0.2627451f);
            }
        }

        GameObject saveButton = slotObject.transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == saveButtonName)?.gameObject;

        GameObject loadButton = slotObject.transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == loadButtonName)?.gameObject;

        GameObject deleteButton = slotObject.transform.GetComponentsInChildren<Transform>(includeInactive: true)
            .FirstOrDefault(t => t.name == deleteButtonName)?.gameObject;

        if (saveMenu)
        {
            if (saveButton != null)
            {
                saveButton.SetActive(true);
                Button btn = saveButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnSaveClicked(slotIndex));
                }
            }

            if (loadButton != null)
            {
                loadButton.SetActive(false);
            }

            if (deleteButton != null)
            {
                deleteButton.SetActive(saveExists);
                Button btn = deleteButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnDeleteClicked(slotIndex));
                }
            }
        }
        else
        {
            if (loadButton != null)
            {
                loadButton.SetActive(saveExists);
                Button btn = loadButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    int capturedIndex = slotIndex;
                    btn.onClick.AddListener(() => OnLoadClicked(capturedIndex));
                }
            }

            if (saveButton != null)
            {
                saveButton.SetActive(false);
            }

            if (deleteButton != null)
            {
                deleteButton.SetActive(saveExists);
                Button btn = deleteButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnDeleteClicked(slotIndex));
                }
            }
        }
    }

    private void LoadScreenshot(Image image, string path)
    {
        if (File.Exists(path))
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(fileData);
            cachedTextures.Add(texture);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
            cachedSprites.Add(sprite);

            image.sprite = sprite;
            Color originalColor = image.color;
            image.color = new Color(1f, 1f, 1f, originalColor.a);

            image.raycastTarget = false;
        }
    }

    private void OnDestroy()
    {
        foreach (var sprite in cachedSprites)
            if (sprite != null) Destroy(sprite);
        cachedSprites.Clear();

        foreach (var tex in cachedTextures)
            if (tex != null) Destroy(tex);
        cachedTextures.Clear();
    }

    private void OnSaveClicked(int slotIndex)
    {
        Debug.Log($"[LoadSaveMenuController] Saving to slot {slotIndex}");

        Canvas canvasToHide = GetComponentInParent<Canvas>();

        SaveManager.Instance.SaveGame(slotIndex, "", () =>
        {
            FillMenu();
        }, canvasToHide);
    }

    private void OnLoadClicked(int slotIndex)
    {
        Debug.Log($"[LoadSaveMenuController] Loading from slot {slotIndex}");
        SaveManager.Instance.LoadGame(slotIndex);
    }

    private void OnDeleteClicked(int slotIndex)
    {
        Debug.Log($"[LoadSaveMenuController] Deleting slot {slotIndex}");
        SaveManager.Instance.DeleteSave(slotIndex);

        FillMenu();
    }

    private void OnDisable()
    {
        ClearMenu();
    }
}
