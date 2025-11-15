using UnityEngine;
using System;

namespace Assets.Scripts
{
    /// <summary>
    /// Global sound system for handling sound events in the game
    /// Enemies subscribe to events and react to sounds
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        /// <summary>
        /// Sound emission event
        /// Vector3 - sound position
        /// float - sound intensity (0-1, affects hearing range)
        /// GameObject - sound source
        /// </summary>
        public static event Action<Vector3, float, GameObject> OnSoundEmitted;

        [Header("Debug")]
        [Tooltip("Show debug visualization for sounds")]
        public bool showDebugSounds = true;

        private static readonly Vector3 DebugRayDirection = Vector3.up * 2f;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Emit sound in the world
        /// </summary>
        /// <param name="position">Sound source position</param>
        /// <param name="intensity">Sound intensity (0-1). 1 = maximum range</param>
        /// <param name="source">Game object sound source</param>
        public static void EmitSound(Vector3 position, float intensity, GameObject source)
        {
            if (intensity > 1f)
            {
                intensity = 1f;
            }
            else if (intensity < 0f)
            {
                intensity = 0f;
            }

            OnSoundEmitted?.Invoke(position, intensity, source);

#if UNITY_EDITOR
            if (Instance != null && Instance.showDebugSounds)
            {
                Debug.DrawRay(position, DebugRayDirection, Color.yellow, 0.5f);
                Debug.Log($"Sound emitted at {position} with intensity {intensity} from {source.name}");
            }
#endif
        }
    }
}