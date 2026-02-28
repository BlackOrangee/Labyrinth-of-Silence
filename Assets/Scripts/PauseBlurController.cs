using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

namespace Assets.Scripts
{
    /// <summary>
    /// Applies Gaussian Depth of Field blur on pause via URP Volume.
    /// Attach to the same GameObject as PauseMenu.
    /// </summary>
    public class PauseBlurController : MonoBehaviour
    {
        [Header("Blur Settings")]
        [Tooltip("Maximum blur radius (0.5 – 1.5)")]
        [Range(0.5f, 1.5f)]
        [SerializeField] private float maxBlurRadius = 1.5f;

        [Tooltip("Blur start distance (meters). 0 = everything in frame is blurred")]
        [SerializeField] private float blurStart = 0f;

        [Tooltip("Volume priority (must be higher than other Volumes in the scene)")]
        [SerializeField] private int volumePriority = 900;

        [Header("Animation")]
        [Tooltip("Blur fade in/out duration (seconds)")]
        [SerializeField] private float fadeDuration = 0.25f;

        private Volume blurVolume;
        private DepthOfField dof;
        private Coroutine fadeCoroutine;

        void Awake()
        {
            CreateVolume();
        }

        private void CreateVolume()
        {
            GameObject go = new GameObject("PauseBlurVolume");
            go.transform.SetParent(transform);

            blurVolume = go.AddComponent<Volume>();
            blurVolume.isGlobal = true;
            blurVolume.priority = volumePriority;
            blurVolume.weight = 0f;
            blurVolume.enabled = false;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            blurVolume.profile = profile;

            dof = profile.Add<DepthOfField>(overrides: true);
            dof.active = true;

            dof.mode.overrideState = true;
            dof.mode.value = DepthOfFieldMode.Gaussian;

            dof.gaussianStart.overrideState = true;
            dof.gaussianStart.value = blurStart;

            dof.gaussianEnd.overrideState = true;
            dof.gaussianEnd.value = Mathf.Max(blurStart, 0.01f);

            dof.gaussianMaxRadius.overrideState = true;
            dof.gaussianMaxRadius.value = maxBlurRadius;

            dof.highQualitySampling.overrideState = true;
            dof.highQualitySampling.value = false; // false = faster
        }

        /// <summary>
        /// Enables/disables blur with a smooth transition.
        /// </summary>
        public void SetBlur(bool blur)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeBlur(blur ? 1f : 0f));
        }

        private IEnumerator FadeBlur(float target)
        {
            blurVolume.enabled = true;
            float start = blurVolume.weight;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                blurVolume.weight = Mathf.Lerp(start, target, elapsed / fadeDuration);
                yield return null;
            }

            blurVolume.weight = target;

            if (target == 0f)
                blurVolume.enabled = false;
        }
    }
}