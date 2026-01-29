using UnityEngine;

[System.Serializable] // Це робить налаштування видимими в Інспекторі
public class SoundProfile
{
    public string soundName;           // Назва для зручності (напр. "Step")
    public AudioClip[] clips;          // Масив звуків (щоб вибирати випадковий)
    
    [Range(0f, 1f)] public float volume = 1f;       // Гучність
    [Range(0.1f, 3f)] public float pitch = 1f;      // Швидкість/Тон
    
    [Header("Randomization (Ефект ВАУ)")]
    [Range(0f, 0.5f)] public float volumeRandom = 0.1f; // Випадкове відхилення гучності
    [Range(0f, 0.5f)] public float pitchRandom = 0.1f;  // Випадкове відхилення тону

    // Метод, який програє звук на вказаному джерелі
    public void Play(AudioSource source)
    {
        if (clips.Length == 0 || source == null) return;

        // 1. Вибираємо випадковий кліп з варіантів
        AudioClip randomClip = clips[Random.Range(0, clips.Length)];

        // 2. Розраховуємо унікальну гучність і пітч для ЦЬОГО конкретного разу
        float finalVolume = volume + Random.Range(-volumeRandom, volumeRandom);
        float finalPitch = pitch + Random.Range(-pitchRandom, pitchRandom);

        source.pitch = finalPitch;
        // PlayOneShot дозволяє накладати звуки (щоб луна не обривалася)
        source.PlayOneShot(randomClip, finalVolume);
    }
}