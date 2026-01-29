using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    [Header("Settings")]
    public float targetVolume = 0.5f; // Яка гучність має бути в кінці (налаштуй під себе)
    public float fadeDuration = 3.0f; // Скільки секунд наростає звук

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        // 1. Ставимо гучність в нуль на старті
        audioSource.volume = 0f;
        
        // 2. Якщо забули поставити галочку Play On Awake - вмикаємо примусово
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void Update()
    {
        // 3. Плавно піднімаємо гучність до цільової
        if (audioSource.volume < targetVolume)
        {
            audioSource.volume += Time.deltaTime / fadeDuration * targetVolume;
        }
    }
}