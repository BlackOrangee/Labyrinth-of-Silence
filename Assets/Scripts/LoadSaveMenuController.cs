using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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
    
    private GameObject title;
    private GameObject content;
    private GameObject saveItem;

    private bool saveMenu = false;
    private List<GameObject> saveItems = new List<GameObject>();
    private bool initialized = false;

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
    }

    private void FillMenu()
    {
        ClearMenu();

        RectTransform saveItemRect = saveItem.GetComponent<RectTransform>();
        float itemHeight = saveItemRect.rect.height;
        float spacing = 10f;

        Vector2 saveItemPos = new Vector2(0, -itemHeight/2);

        for (int i = 0; i < 10; i++)
        {
            GameObject newSaveItem = Instantiate(this.saveItem);
            RectTransform newSaveItemRect = newSaveItem.GetComponent<RectTransform>();

            newSaveItem.transform.SetParent(content.transform, false);
            newSaveItem.transform.localScale = Vector3.one;

            newSaveItemRect.anchoredPosition = saveItemPos;
            newSaveItem.SetActive(true);

            GameObject button = newSaveItem.transform.GetComponentsInChildren<Transform>(includeInactive: true)
                .FirstOrDefault(t => t.name == (saveMenu ? saveButtonName : loadButtonName))?.gameObject;
            if (button)
            {
                button.SetActive(true);
            }

            saveItems.Add(newSaveItem);

            saveItemPos.y -= (itemHeight + spacing);
        }
    }

    private void OnDisable()
    {
        ClearMenu();
    }
}
