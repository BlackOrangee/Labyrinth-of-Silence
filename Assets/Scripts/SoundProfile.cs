using UnityEngine;

[System.Serializable]
public class SoundProfile
{
    public string soundName;
    public AudioClip[] clips;
    
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    
    [Header("Randomization (Ефект ВАУ)")]
    [Range(0f, 0.5f)] public float volumeRandom = 0.1f;
    [Range(0f, 0.5f)] public float pitchRandom = 0.1f;
    public void Play(AudioSource source)
    {
        if (clips.Length == 0 || source == null) return;

        AudioClip randomClip = clips[Random.Range(0, clips.Length)];

        float finalVolume = volume + Random.Range(-volumeRandom, volumeRandom);
        float finalPitch = pitch + Random.Range(-pitchRandom, pitchRandom);

        source.pitch = finalPitch;

        source.PlayOneShot(randomClip, finalVolume);
    }
}