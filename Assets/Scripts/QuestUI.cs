using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// QuestUI - менеджер UI для відображення завдань.
/// Відображає завдання у лівому верхньому куті екрану з анімацією.
/// </summary>
public class QuestUI : MonoBehaviour
{
    [Header("UI Елементи (прив'язати в Inspector)")]
    [Tooltip("Корінний GameObject для панелі завдань")]
    public GameObject questPanelRoot;

    [Tooltip("Контейнер для списку завдань (з Vertical Layout Group)")]
    public RectTransform questListContainer; // ВИПРАВЛЕНО: Transform → RectTransform

    [Tooltip("Префаб для одного завдання (QuestItem)")]
    public GameObject questItemPrefab;

    [Header("Звуки")]
    [Tooltip("Звук при оновленні завдання (bell.mp3)")]
    public AudioClip updateSound;

    [Tooltip("Звук при завершенні завдання (triumph.mp3)")]
    public AudioClip completeSound;

    [Range(0f, 1f)]
    [Tooltip("Гучність звуків")]
    public float soundVolume = 0.7f;

    [Header("Анімація")]
    [Tooltip("Тривалість анімації появи (fade in)")]
    public float fadeInDuration = 0.5f;

    [Tooltip("Тривалість анімації викреслювання")]
    public float strikethroughDuration = 0.5f;

    [Tooltip("Тривалість анімації зникнення (fade out)")]
    public float fadeOutDuration = 1f;

    [Header("Кольори (Horror стиль)")]
    [Tooltip("Колір для активних завдань")]
    public Color activeQuestColor = new Color(0.9f, 0.9f, 0.85f, 1f); // Світло-жовтуватий

    [Tooltip("Колір для виконаних завдань")]
    public Color completedQuestColor = new Color(0.4f, 0.4f, 0.4f, 1f); // Темно-сірий

    [Tooltip("Колір лінії викреслювання")]
    public Color strikethroughColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Кривавий червоний

    // Внутрішні дані
    private Dictionary<string, GameObject> questItems = new Dictionary<string, GameObject>();
    private AudioSource audioSource;

    #region Unity Lifecycle

    private void Awake()
    {
        // Налаштування AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

        // Перевірка налаштувань
        if (questPanelRoot == null)
        {
            Debug.LogError("❌ QuestUI: questPanelRoot не прив'язано! Прив'яжіть у Inspector.");
        }

        if (questListContainer == null)
        {
            Debug.LogError("❌ QuestUI: questListContainer не прив'язано! Прив'яжіть у Inspector.");
        }

        if (questItemPrefab == null)
        {
            Debug.LogError("❌ QuestUI: questItemPrefab не прив'язано! Прив'яжіть у Inspector.");
        }

        // Початково ховаємо панель якщо немає завдань
        if (questPanelRoot != null && questItems.Count == 0)
        {
            questPanelRoot.SetActive(false);
        }
    }

    private void Start()
    {
        // Підписуємось на події QuestTracker
        if (QuestTracker.Instance != null)
        {
            QuestTracker.Instance.OnQuestAdded += OnQuestAdded;
            QuestTracker.Instance.OnQuestUpdated += OnQuestUpdated;
            QuestTracker.Instance.OnQuestCompleted += OnQuestCompleted;
            Debug.Log("✅ QuestUI: підписано на події QuestTracker.");
        }
        else
        {
            Debug.LogWarning("⚠️ QuestUI: QuestTracker не знайдено!");
        }
    }

    // ВИПРАВЛЕНО: OnDestroy тепер безпечний
    private void OnDisable()
    {
        // Відписуємось від подій коли компонент вимикається
        if (QuestTracker.Instance != null)
        {
            QuestTracker.Instance.OnQuestAdded -= OnQuestAdded;
            QuestTracker.Instance.OnQuestUpdated -= OnQuestUpdated;
            QuestTracker.Instance.OnQuestCompleted -= OnQuestCompleted;
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Обробник події: додано нове завдання
    /// </summary>
    private void OnQuestAdded(Quest quest)
    {
        Debug.Log($"📥 QuestUI: додається завдання '{quest.questId}'");

        // Показуємо панель якщо вона прихована
        if (questPanelRoot != null && !questPanelRoot.activeSelf)
        {
            questPanelRoot.SetActive(true);
            Debug.Log("✅ QuestUI: questPanelRoot активовано");
        }

        // Створюємо новий UI елемент для завдання
        CreateQuestItem(quest);

        // Відтворюємо звук оновлення (при додаванні)
        PlaySound(updateSound);
    }

    /// <summary>
    /// Обробник події: оновлено прогрес завдання
    /// </summary>
    private void OnQuestUpdated(Quest quest)
    {
        Debug.Log($"🔄 QuestUI: оновлюється завдання '{quest.questId}' до {quest.currentProgress}/{quest.targetProgress}");

        // Оновлюємо текст завдання
        UpdateQuestItemText(quest);

        // Відтворюємо звук оновлення
        PlaySound(updateSound);
    }

    /// <summary>
    /// Обробник події: завдання виконано
    /// </summary>
    private void OnQuestCompleted(Quest quest)
    {
        Debug.Log($"🎉 QuestUI: завершено завдання '{quest.questId}'");

        // ВИПРАВЛЕНО: Запускаємо анімацію викреслювання і зникнення
        StartCoroutine(CompleteQuestAnimation(quest.questId));

        // ВИПРАВЛЕНО: Відтворюємо тріумфальний звук ОДРАЗУ
        PlaySound(completeSound);
    }

    #endregion

    #region Quest Item Management

    /// <summary>
    /// Створити UI елемент для завдання
    /// </summary>
    private void CreateQuestItem(Quest quest)
    {
        // ДОДАНО: Детальні логи для діагностики
        Debug.Log($"🔨 CreateQuestItem: створюю елемент для '{quest.questId}'");

        if (questItemPrefab == null)
        {
            Debug.LogError("❌ QuestUI: questItemPrefab = NULL! Прив'яжіть префаб у Inspector.");
            return;
        }

        if (questListContainer == null)
        {
            Debug.LogError("❌ QuestUI: questListContainer = NULL! Прив'яжіть контейнер у Inspector.");
            return;
        }

        Debug.Log($"✅ QuestUI: Префаб і контейнер існують, створюю елемент...");

        // Перевіряємо чи вже існує
        if (questItems.ContainsKey(quest.questId))
        {
            Debug.LogWarning($"⚠️ QuestUI: QuestItem '{quest.questId}' вже існує.");
            return;
        }

        // Створюємо екземпляр префабу
        GameObject itemGO = Instantiate(questItemPrefab, questListContainer);
        itemGO.name = $"QuestItem_{quest.questId}";
        
        Debug.Log($"✅ QuestUI: Створено GameObject '{itemGO.name}'");

        // Знаходимо компоненти
        TextMeshProUGUI textComponent = itemGO.GetComponentInChildren<TextMeshProUGUI>();
        Transform strikethroughTransform = itemGO.transform.Find("StrikeThrough");
        Image strikethroughImage = strikethroughTransform?.GetComponent<Image>();

        if (textComponent != null)
        {
            textComponent.text = quest.GetFormattedText();
            textComponent.color = activeQuestColor;
            Debug.Log($"✅ QuestUI: Текст встановлено: '{textComponent.text}'");
        }
        else
        {
            Debug.LogError($"❌ QuestUI: не знайдено TextMeshProUGUI в префабі QuestItem!");
        }

        if (strikethroughImage != null)
        {
            strikethroughImage.color = strikethroughColor;
            strikethroughImage.fillAmount = 0f; // Початково прихована
            Debug.Log($"✅ QuestUI: StrikeThrough налаштовано");
        }
        else
        {
            Debug.LogWarning($"⚠️ QuestUI: StrikeThrough не знайдено в префабі");
        }

        // Зберігаємо посилання
        questItems.Add(quest.questId, itemGO);
        Debug.Log($"✅ QuestUI: QuestItem '{quest.questId}' доданий до словника. Всього: {questItems.Count}");

        // Запускаємо анімацію появи
        StartCoroutine(FadeInAnimation(itemGO));
    }

    /// <summary>
    /// Оновити текст завдання
    /// </summary>
    private void UpdateQuestItemText(Quest quest)
    {
        if (!questItems.ContainsKey(quest.questId))
        {
            Debug.LogWarning($"⚠️ QuestUI: не можу оновити текст - QuestItem '{quest.questId}' не знайдено.");
            return;
        }

        GameObject itemGO = questItems[quest.questId];
        TextMeshProUGUI textComponent = itemGO.GetComponentInChildren<TextMeshProUGUI>();

        if (textComponent != null)
        {
            textComponent.text = quest.GetFormattedText();
            Debug.Log($"✅ QuestUI: Текст оновлено на '{textComponent.text}'");
        }
    }

    /// <summary>
    /// Видалити UI елемент завдання
    /// </summary>
    private void RemoveQuestItem(string questId)
    {
        if (questItems.ContainsKey(questId))
        {
            GameObject itemGO = questItems[questId];
            questItems.Remove(questId);
            Destroy(itemGO);
            Debug.Log($"🗑️ QuestUI: видалено QuestItem '{questId}'");
        }

        // Якщо більше немає завдань - ховаємо панель
        if (questItems.Count == 0 && questPanelRoot != null)
        {
            questPanelRoot.SetActive(false);
            Debug.Log($"👻 QuestUI: questPanelRoot приховано (немає завдань)");
        }
    }

    #endregion

    #region Animations

    /// <summary>
    /// Анімація появи (fade in + slide)
    /// </summary>
    private IEnumerator FadeInAnimation(GameObject itemGO)
    {
        CanvasGroup canvasGroup = itemGO.GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = itemGO.AddComponent<CanvasGroup>();
        }

        RectTransform rectTransform = itemGO.GetComponent<RectTransform>();

        // Початкові значення
        canvasGroup.alpha = 0f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 targetPos = startPos;
        startPos.x -= 100f; // Зсув ліворуч для ефекту slide
        rectTransform.anchoredPosition = startPos;

        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;

            // Плавне збільшення прозорості
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            // Плавний зсув
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

            yield return null;
        }

        // Фінальні значення
        canvasGroup.alpha = 1f;
        rectTransform.anchoredPosition = targetPos;
        
        Debug.Log($"✨ QuestUI: FadeIn анімація завершена для {itemGO.name}");
    }

    /// <summary>
    /// Анімація викреслювання та зникнення
    /// </summary>
    private IEnumerator CompleteQuestAnimation(string questId)
    {
        Debug.Log($"🎬 QuestUI: Запуск анімації завершення для '{questId}'");

        if (!questItems.ContainsKey(questId))
        {
            Debug.LogWarning($"⚠️ QuestUI: QuestItem '{questId}' не знайдено для анімації");
            yield break;
        }

        GameObject itemGO = questItems[questId];
        TextMeshProUGUI textComponent = itemGO.GetComponentInChildren<TextMeshProUGUI>();
        Transform strikethroughTransform = itemGO.transform.Find("StrikeThrough");
        Image strikethroughImage = strikethroughTransform?.GetComponent<Image>();

        // Змінюємо колір тексту на "виконаний"
        if (textComponent != null)
        {
            textComponent.color = completedQuestColor;
            Debug.Log($"🎨 QuestUI: Колір тексту змінено на completedQuestColor");
        }

        // Анімація викреслювання
        if (strikethroughImage != null)
        {
            Debug.Log($"✏️ QuestUI: Запуск анімації викреслювання...");
            float elapsed = 0f;

            while (elapsed < strikethroughDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / strikethroughDuration;

                strikethroughImage.fillAmount = Mathf.Lerp(0f, 1f, t);

                yield return null;
            }

            strikethroughImage.fillAmount = 1f;
            Debug.Log($"✅ QuestUI: Викреслювання завершено");
        }
        else
        {
            Debug.LogWarning($"⚠️ QuestUI: StrikeThrough не знайдено, пропускаю анімацію");
        }

        // Пауза перед зникненням
        Debug.Log($"⏸️ QuestUI: Пауза 0.3 сек...");
        yield return new WaitForSeconds(0.3f);

        // Анімація fade out
        Debug.Log($"💨 QuestUI: Запуск fade out...");
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

        Debug.Log($"✅ QuestUI: Fade out завершено, видаляю елемент");

        // Видаляємо елемент
        RemoveQuestItem(questId);
    }

    #endregion

    #region Audio

    /// <summary>
    /// Відтворити звук
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning($"⚠️ QuestUI: AudioClip = NULL, не можу відтворити звук");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError($"❌ QuestUI: AudioSource = NULL!");
            return;
        }

        audioSource.PlayOneShot(clip, soundVolume);
        Debug.Log($"🔊 QuestUI: Відтворюю звук '{clip.name}' з гучністю {soundVolume}");
    }

    #endregion

    #region Debug

    /// <summary>
    /// Тестове додавання завдання (для дебагу)
    /// </summary>
    [ContextMenu("Test: Add Quest")]
    public void TestAddQuest()
    {
        QuestTracker.Instance?.AddQuest(
            "test_quest_" + Random.Range(0, 999),
            "Тестове завдання",
            0,
            5
        );
    }

    /// <summary>
    /// Тестове оновлення завдання
    /// </summary>
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

    /// <summary>
    /// Тестове завершення першого завдання
    /// </summary>
    [ContextMenu("Test: Complete First Quest")]
    public void TestCompleteQuest()
    {
        var quests = QuestTracker.Instance?.GetAllQuests();
        if (quests != null && quests.Count > 0)
        {
            var quest = quests[0];
            // Оновлюємо до максимуму
            QuestTracker.Instance?.UpdateQuest(quest.questId, quest.targetProgress);
        }
    }

    #endregion
}
