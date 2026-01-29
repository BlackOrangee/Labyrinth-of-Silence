using UnityEngine;
using System.Collections;

public class GlobalSoundManager : MonoBehaviour
{
    // Робимо "Сінглтон" (це щоб інші скрипти могли легко знайти цей менеджер)
    public static GlobalSoundManager Instance;

    void Awake()
    {
        // Якщо такого менеджера ще немає - це ми.
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Щоб не було дублікатів
        }
    }

    void Start()
    {
        // ВАЖЛИВО: Коли гра починається (або перезавантажується після смерті),
        // ми маємо переконатися, що звук увімкнено на 100%!
        AudioListener.volume = 1f;
    }

    // Цей метод ми будемо викликати при смерті
    public void FadeOutAllSounds(float duration)
    {
        StartCoroutine(FadeOutRoutine(duration));
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVolume = AudioListener.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            // Плавно зменшуємо загальну гучність гри до 0
            AudioListener.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        AudioListener.volume = 0f; // Гарантуємо повну тишу в кінці
    }
}