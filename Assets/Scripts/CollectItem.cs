using UnityEngine;

/// <summary>
/// CollectItem — компонент для предметів, які можна збирати.
/// Тепер містить захист від подвійного підбору (isCollected).
/// Реалізує IInteractable.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CollectItem : MonoBehaviour, IInteractable
{
    [Header("Налаштування CollectItem")]
    [Tooltip("Назва предмета, яка показується в попапі")]
    public string displayName = "Предмет";

    [Tooltip("Іконка предмета, яку показуємо в попапі")]
    public Sprite itemIcon;

    [Tooltip("Чи видаляти об'єкт після підбору")]
    public bool destroyOnCollect = true;

    [Tooltip("Опціонально: відтворити звук підбору")]
    public AudioClip pickupSound;

    [Range(0f, 1f)]
    public float pickupVolume = 1f;

    // Захист від подвійного підбору
    private bool isCollected = false;

    private AudioSource audioSource;
    private Collider cachedCollider;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();
        if (cachedCollider == null)
        {
            Debug.LogWarning($"CollectItem ({name}): не знайдено Collider. Додайте Collider на об'єкт.");
        }

        if (pickupSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = pickupSound;
            audioSource.volume = pickupVolume;
        }
    }

    public string GetInteractText()
    {
        return $"Натисніть кнопку \"Підібрати\", щоб забрати: {displayName}";
    }

    /// <summary>
    /// Основна логіка збирання.
    /// Тут додаємо захист: якщо вже зібрано — ігноруємо повторні виклики.
    /// </summary>
    public void OnInteract(GameObject actor)
    {
        // Якщо вже зібрано — ігноруємо (запобігаємо дублюванню)
        if (isCollected)
        {
            Debug.Log($"CollectItem: {displayName} вже зібрано — ігнорую повторний виклик.");
            return;
        }

        isCollected = true; // блок на наступні виклики

        // Відключаємо колайдер, щоб інші виклики не могли пройти фізично
        if (cachedCollider != null)
            cachedCollider.enabled = false;

        // Спроба додати до інвентаря гравця
        var inv = actor != null ? actor.GetComponent<SimpleInventory>() : null;
        if (inv != null)
        {
            inv.AddItem(displayName);
            Debug.Log($"CollectItem: {displayName} додано в інвентар гравця.");
            
            // Оновлюємо прогрес квесту збору ключів
            if (QuestTracker.Instance != null && QuestTracker.Instance.HasQuest("collect_keys"))
            {
                int currentKeys = inv.GetCollectedKeysCount();
                QuestTracker.Instance.UpdateQuest("collect_keys", currentKeys);
                Debug.Log($"CollectItem: оновлено прогрес квесту 'collect_keys' до {currentKeys}");
            }
        }
        else
        {
            Debug.Log($"CollectItem: {displayName} підібрано (інвентар не знайдено).");
        }

        // Програти звук (через тимчасовий обʼєкт, щоб звук звучав навіть після Destroy)
        if (pickupSound != null)
        {
            GameObject soundHolder = new GameObject("PickupSoundHolder");
            soundHolder.transform.position = transform.position;
            AudioSource a = soundHolder.AddComponent<AudioSource>();
            a.clip = pickupSound;
            a.volume = pickupVolume;
            a.Play();
            Destroy(soundHolder, pickupSound.length + 0.1f);
        }

        // Якщо потрібно — знищити або вимкнути об'єкт
        if (destroyOnCollect)
        {
            // Додаємо невелику затримку перед Destroy, щоб уникнути раптового зникнення, яке може збити інші події.
            Destroy(gameObject);
        }
        else
        {
            // Якщо не знищуємо — ховаємо візуально
            var renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
            // Можемо також вимкнути усі дочірні об'єкти
            foreach (Transform t in transform)
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    // Новий метод Interact — делегує в OnInteract (для сумісності)
    public void Interact(GameObject actor)
    {
        OnInteract(actor);
    }

    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null)
        {
            c.isTrigger = false;
        }
    }
}
