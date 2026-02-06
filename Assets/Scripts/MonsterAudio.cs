using UnityEngine;

namespace Assets.Scripts
{
    public class MonsterAudio : MonoBehaviour
    {
        [Header("--- AUDIO SOURCES (Динаміки) ---")]
        [Tooltip("Для звуку Кроків")]
        public AudioSource stepsSource;   
        
        [Tooltip("Для звуку Ланцюгів")]
        public AudioSource chainSource;   
        
        [Tooltip("Для Дихання (Loop)")]
        public AudioSource breathSource;  
        
        [Tooltip("Для Удару (Атака)")]
        public AudioSource attackSource;  
        
        [Tooltip("Для Рику (Коли помітив)")]
        public AudioSource spotSource;    

        [Header("--- SOUND PROFILES (Файли) ---")]
        public SoundProfile stepSounds;
        public SoundProfile chainSounds;
        public SoundProfile screamSound;
        public SoundProfile spotSound;
        public SoundProfile breathSound;

        private bool hasSpotted = false; 

        void Start()
        {
            if (breathSource != null && breathSound != null && breathSound.clips != null && breathSound.clips.Length > 0)
            {
                breathSource.clip = breathSound.clips[0];
                breathSource.loop = true;
                breathSource.volume = breathSound.volume;
                breathSource.Play();
            }
        }
public void PlayStep()
        {
            Debug.Log($"Monster-A: ТУП! (Гучність: {stepSounds?.volume})");

            if (stepSounds != null && stepsSource != null)
                stepSounds.Play(stepsSource);

            if (chainSounds != null && chainSource != null)
                chainSounds.Play(chainSource);
        }
        public void PlayScream()
        {
            if (screamSound != null && attackSource != null) 
            {
                screamSound.Play(attackSource);
            }
            else
            {
                Debug.LogWarning("MonsterAudio: Немає AttackSource або профілю звуку удару!");
            }
        }
        public void PlaySpotSound()
        {
            if (!hasSpotted && spotSound != null && spotSource != null)
            {
                spotSound.Play(spotSource);
                hasSpotted = true;
                Invoke(nameof(ResetSpot), 10f);
            }
        }

        private void ResetSpot()
        {
            hasSpotted = false;
        }
    }
}