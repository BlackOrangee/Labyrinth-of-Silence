using UnityEngine;

namespace Assets.Scripts
{
    public class MonsterAudio : MonoBehaviour
    {
        [Header("Audio Source")]
        public AudioSource mainSource;   // Для кроків і ударів
        public AudioSource breathSource; // Окреме джерело для дихання (Loop)
        public AudioSource chainSource;  // Окреме джерело для дзвону ланцюгів

        [Header("Sound Profiles (Налаштуй це!)")]
        public SoundProfile stepSounds;   // Кроки
        public SoundProfile chainSounds;  // Ланцюги (дзвін при ходьбі)
        public SoundProfile screamSound;  // Атака
        public SoundProfile breathSound;  // Дихання

        void Start()
        {
            // Запускаємо дихання відразу
            if (breathSource != null && breathSound.clips.Length > 0)
            {
                breathSource.clip = breathSound.clips[0];
                breathSource.loop = true;
                breathSource.volume = breathSound.volume;
                breathSource.Play();
            }
        }

        // Викликається з Анімації (Events: PlayStep)
        public void PlayStep()
        {
            // [DEBUG] Цей рядок покаже в консолі, чи працює анімація
            Debug.Log("👣 ГУП! (Крок спрацював)"); 

            stepSounds.Play(mainSource);

            if (chainSource != null)
            {
                chainSounds.Play(chainSource);
            }
        }

        // Викликається з EnemyAI
        public void PlayScream()
        {
            screamSound.Play(mainSource);
        }
    }
}