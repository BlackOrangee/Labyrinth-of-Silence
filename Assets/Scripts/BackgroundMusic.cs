using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    [Header("Settings")]
    public float targetVolume = 0.5f;
    public float fadeDuration = 3.0f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.volume = 0f;

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void Update()
    {
        if (audioSource.volume < targetVolume)
        {
            audioSource.volume += Time.deltaTime / fadeDuration * targetVolume;
        }
    }
}