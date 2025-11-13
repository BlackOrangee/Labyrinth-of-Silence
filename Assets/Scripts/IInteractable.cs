using UnityEngine;

/// <summary>
/// Інтерфейс для об'єктів з якими можна взаємодіяти.
/// Додав метод Interact для сумісності з існуючими скриптами (RaycastInteractor тощо).
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Текст, який показується у попапі (наприклад назва/дія).
    /// </summary>
    string GetInteractText();

    /// <summary>
    /// Стара/внутрішня реалізація - виконує логіку взаємодії.
    /// Залишено для сумісності (якщо є код що викликає OnInteract).
    /// </summary>
    void OnInteract(GameObject actor);

    /// <summary>
    /// Новий/загальний метод, який можуть викликати інші скрипти (наприклад RaycastInteractor.Interact).
    /// Рекомендується, щоб реалізації викликали OnInteract або безпосередньо реалізували логіку тут.
    /// </summary>
    void Interact(GameObject actor);
}
