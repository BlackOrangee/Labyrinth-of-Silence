using UnityEngine;
using System.Collections;

public class GlobalSoundManager : MonoBehaviour
{
    public static GlobalSoundManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        AudioListener.volume = 1f;
    }

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

            AudioListener.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        AudioListener.volume = 0f;
    }
}