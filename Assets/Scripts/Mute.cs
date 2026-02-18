using UnityEngine;

public class Mute : MonoBehaviour
{
    private bool isMuted = false;
    private float previousVolume = 1f;

    void Start()
    {
        previousVolume = AudioListener.volume;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMute();
        }
    }

    void ToggleMute()
    {
        isMuted = !isMuted;

        if (isMuted)
        {
            previousVolume = AudioListener.volume;
            AudioListener.volume = 0f;
            Debug.Log("Звук ВИМКНЕНО (Muted)");
        }
        else
        {
            AudioListener.volume = 1f; 
            Debug.Log("Звук УВІМКНЕНО (Unmuted)");
        }
    }
}