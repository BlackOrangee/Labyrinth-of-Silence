using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class QuestUI : MonoBehaviour
{
    [Header("UI Elements (link in Inspector)")]
    [Tooltip("Root GameObject for quest panel")]
    public GameObject questPanelRoot;

    [Tooltip("Container for quest list (with Vertical Layout Group)")]
    public RectTransform questListContainer;

    [Tooltip("Prefab for single quest (QuestItem)")]
    public GameObject questItemPrefab;

    [Header("Sounds")]
    [Tooltip("Sound when quest is updated (bell.mp3)")]
    public AudioClip updateSound;

    [Tooltip("Sound when quest is completed (triumph.mp3)")]
    public AudioClip completeSound;

    [Range(0f, 1f)]
    [Tooltip("Sound volume")]
    public float soundVolume = 0.7f;

    [Header("Animation")]
    [Tooltip("Fade in animation duration")]
    public float fadeInDuration = 0.5f;

    [Tooltip("Strikethrough animation duration")]
    public float strikethroughDuration = 0.5f;

    [Tooltip("Fade out animation duration")]
    public float fadeOutDuration = 1f;

    [Header("Colors (Horror style)")]
    [Tooltip("Color for active quests")]
    public Color activeQuestColor = new Color(0.9f, 0.9f, 0.85f, 1f);

    [Tooltip("Color for completed quests")]
    public Color completedQuestColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Tooltip("Strikethrough line color")]
    public Color strikethroughColor = new Color(0.8f, 0.2f, 0.2f, 1f);

    private Dictionary<string, GameObject> questItems = new Dictionary<string, GameObject>();
    private AudioSource audioSource;
    private bool isSubscribed = false;

    #region Unity Lifecycle

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

        if (questPanelRoot == null)
        {
            Debug.LogError("❌ QuestUI: questPanelRoot not linked! Link in Inspector.");
        }

        if (questListContainer == null)
        {
            Debug.LogError("❌ QuestUI: questListContainer not linked! Link in Inspector.");
        }

        if (questItemPrefab == null)
        {
            Debug.LogError("❌ QuestUI: questItemPrefab not linked! Link in Inspector.");
        }

        if (questPanelRoot != null && questItems.Count == 0)
        {
            questPanelRoot.SetActive(false);
        }
    }

    private void Start()
    {
        if (QuestTracker.Instance != null)
        {
            QuestTracker.Instance.OnQuestAdded += OnQuestAdded;
            QuestTracker.Instance.OnQuestUpdated += OnQuestUpdated;
            QuestTracker.Instance.OnQuestCompleted += OnQuestCompleted;
            isSubscribed = true;
            Debug.Log("✅ QuestUI: subscribed to QuestTracker events.");
        }
        else
        {
            Debug.LogWarning("⚠️ QuestUI: QuestTracker not found!");
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromQuestTracker();
    }

    private void OnDisable()
    {
        UnsubscribeFromQuestTracker();
    }

    private void UnsubscribeFromQuestTracker()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (QuestTracker.Instance != null)
        {
            QuestTracker.Instance.OnQuestAdded -= OnQuestAdded;
            QuestTracker.Instance.OnQuestUpdated -= OnQuestUpdated;
            QuestTracker.Instance.OnQuestCompleted -= OnQuestCompleted;
            isSubscribed = false;
            Debug.Log("✅ QuestUI: unsubscribed from QuestTracker events.");
        }
    }

    #endregion

    #region Event Handlers

    private void OnQuestAdded(Quest quest)
    {
        Debug.Log($"📥 QuestUI: adding quest '{quest.questId}'");

        if (questPanelRoot != null && !questPanelRoot.activeSelf)
        {
            questPanelRoot.SetActive(true);
            Debug.Log("✅ QuestUI: questPanelRoot activated");
        }

        CreateQuestItem(quest);

        PlaySound(updateSound);
    }

    private void OnQuestUpdated(Quest quest)
    {
        Debug.Log($"🔄 QuestUI: updating quest '{quest.questId}' to {quest.currentProgress}/{quest.targetProgress}");

        UpdateQuestItemText(quest);

        PlaySound(updateSound);
    }

    private void OnQuestCompleted(Quest quest)
    {
        Debug.Log($"🎉 QuestUI: completed quest '{quest.questId}'");

        StartCoroutine(CompleteQuestAnimation(quest.questId));

        PlaySound(completeSound);
    }

    #endregion

    #region Quest Item Management

    private void CreateQuestItem(Quest quest)
    {
        Debug.Log($"🔨 CreateQuestItem: creating element for '{quest.questId}'");

        if (questItemPrefab == null)
        {
            Debug.LogError("❌ QuestUI: questItemPrefab = NULL! Link prefab in Inspector.");
            return;
        }

        if (questListContainer == null)
        {
            Debug.LogError("❌ QuestUI: questListContainer = NULL! Link container in Inspector.");
            return;
        }

        Debug.Log($"✅ QuestUI: Prefab and container exist, creating element...");

        if (questItems.ContainsKey(quest.questId))
        {
            Debug.LogWarning($"⚠️ QuestUI: QuestItem '{quest.questId}' already exists.");
            return;
        }

        GameObject itemGO = Instantiate(questItemPrefab, questListContainer);
        itemGO.name = $"QuestItem_{quest.questId}";

        RectTransform rectTransform = itemGO.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            float halfWidth = rectTransform.rect.width / 2f;
            rectTransform.localPosition = new Vector3(halfWidth, -10, 0);
            rectTransform.localScale = Vector3.one;
        }
        
        Debug.Log($"✅ QuestUI: Created GameObject '{itemGO.name}'");

        TextMeshProUGUI textComponent = itemGO.GetComponentInChildren<TextMeshProUGUI>();
        Transform strikethroughTransform = itemGO.transform.Find("StrikeThrough");
        Image strikethroughImage = strikethroughTransform?.GetComponent<Image>();

        if (textComponent != null)
        {
            textComponent.text = quest.GetFormattedText();
            textComponent.color = activeQuestColor;
            Debug.Log($"✅ QuestUI: Text set: '{textComponent.text}'");
        }
        else
        {
            Debug.LogError($"❌ QuestUI: TextMeshProUGUI not found in QuestItem prefab!");
        }

        if (strikethroughImage != null)
        {
            strikethroughImage.color = strikethroughColor;
            strikethroughImage.fillAmount = 0f;
            Debug.Log($"✅ QuestUI: StrikeThrough configured");
        }
        else
        {
            Debug.LogWarning($"⚠️ QuestUI: StrikeThrough not found in prefab");
        }

        questItems.Add(quest.questId, itemGO);
        Debug.Log($"✅ QuestUI: QuestItem '{quest.questId}' added to dictionary. Total: {questItems.Count}");

        StartCoroutine(FadeInAnimation(itemGO));
    }

    private void UpdateQuestItemText(Quest quest)
    {
        if (!questItems.ContainsKey(quest.questId))
        {
            Debug.LogWarning($"⚠️ QuestUI: cannot update text - QuestItem '{quest.questId}' not found.");
            return;
        }

        GameObject itemGO = questItems[quest.questId];
        TextMeshProUGUI textComponent = itemGO.GetComponentInChildren<TextMeshProUGUI>();

        if (textComponent != null)
        {
            textComponent.text = quest.GetFormattedText();
            Debug.Log($"✅ QuestUI: Text updated to '{textComponent.text}'");
        }
    }

    private void RemoveQuestItem(string questId)
    {
        if (questItems.ContainsKey(questId))
        {
            GameObject itemGO = questItems[questId];
            questItems.Remove(questId);
            Destroy(itemGO);
            Debug.Log($"🗑️ QuestUI: removed QuestItem '{questId}'");
        }

        if (questItems.Count == 0 && questPanelRoot != null)
        {
            questPanelRoot.SetActive(false);
            Debug.Log($"👻 QuestUI: questPanelRoot hidden (no quests)");
        }
    }

    #endregion

    #region Animations

    private IEnumerator FadeInAnimation(GameObject itemGO)
    {
        CanvasGroup canvasGroup = itemGO.GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = itemGO.AddComponent<CanvasGroup>();
        }

        RectTransform rectTransform = itemGO.GetComponent<RectTransform>();

        canvasGroup.alpha = 0f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 targetPos = startPos;
        startPos.x -= 100f;
        rectTransform.anchoredPosition = startPos;

        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        rectTransform.anchoredPosition = targetPos;
        
        Debug.Log($"✨ QuestUI: FadeIn animation completed for {itemGO.name}");
    }

    private IEnumerator CompleteQuestAnimation(string questId)
    {
        Debug.Log($"🎬 QuestUI: Starting completion animation for '{questId}'");

        if (!questItems.ContainsKey(questId))
        {
            Debug.LogWarning($"⚠️ QuestUI: QuestItem '{questId}' not found for animation");
            yield break;
        }

        GameObject itemGO = questItems[questId];
        TextMeshProUGUI textComponent = itemGO.GetComponentInChildren<TextMeshProUGUI>();
        Transform strikethroughTransform = itemGO.transform.Find("StrikeThrough");
        Image strikethroughImage = strikethroughTransform?.GetComponent<Image>();

        if (textComponent != null)
        {
            textComponent.color = completedQuestColor;
            Debug.Log($"🎨 QuestUI: Text color changed to completedQuestColor");
        }

        if (strikethroughImage != null)
        {
            Debug.Log($"✏️ QuestUI: Starting strikethrough animation...");
            float elapsed = 0f;

            while (elapsed < strikethroughDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / strikethroughDuration;

                strikethroughImage.fillAmount = Mathf.Lerp(0f, 1f, t);

                yield return null;
            }

            strikethroughImage.fillAmount = 1f;
            Debug.Log($"✅ QuestUI: Strikethrough completed");
        }
        else
        {
            Debug.LogWarning($"⚠️ QuestUI: StrikeThrough not found, skipping animation");
        }

        Debug.Log($"⏸️ QuestUI: Pause 0.3 sec...");
        yield return new WaitForSeconds(0.3f);

        Debug.Log($"💨 QuestUI: Starting fade out...");
        CanvasGroup canvasGroup = itemGO.GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = itemGO.AddComponent<CanvasGroup>();
        }

        float elapsedFade = 0f;

        while (elapsedFade < fadeOutDuration)
        {
            elapsedFade += Time.deltaTime;
            float t = elapsedFade / fadeOutDuration;

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        Debug.Log($"✅ QuestUI: Fade out completed, removing element");

        RemoveQuestItem(questId);
    }

    #endregion

    #region Audio

    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning($"⚠️ QuestUI: AudioClip = NULL, cannot play sound");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError($"❌ QuestUI: AudioSource = NULL!");
            return;
        }

        audioSource.PlayOneShot(clip, soundVolume);
        Debug.Log($"🔊 QuestUI: Playing sound '{clip.name}' with volume {soundVolume}");
    }

    #endregion

    #region Debug

    [ContextMenu("Test: Add Quest")]
    public void TestAddQuest()
    {
        QuestTracker.Instance?.AddQuest(
            "test_quest_" + Random.Range(0, 999),
            "Test quest",
            0,
            5
        );
    }

    [ContextMenu("Test: Update First Quest")]
    public void TestUpdateQuest()
    {
        var quests = QuestTracker.Instance?.GetAllQuests();
        if (quests != null && quests.Count > 0)
        {
            var quest = quests[0];
            QuestTracker.Instance?.UpdateQuest(quest.questId, quest.currentProgress + 1);
        }
    }

    [ContextMenu("Test: Complete First Quest")]
    public void TestCompleteQuest()
    {
        var quests = QuestTracker.Instance?.GetAllQuests();
        if (quests != null && quests.Count > 0)
        {
            var quest = quests[0];
            QuestTracker.Instance?.UpdateQuest(quest.questId, quest.targetProgress);
        }
    }

    #endregion
}