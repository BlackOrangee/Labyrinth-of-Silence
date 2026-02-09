using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;

namespace Assets.Scripts
{
    public class SanityController : MonoBehaviour
    {
        [Header("Basic Settings")]
        public float maxSanity = 100f;
        public float timeToDeath = 100f;
        public float timeToRecover = 35f;
        public float criticalThreshold = 30f;

        [Header("DEATH AUDIO")]
        public AnimationCurve fallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AudioClip bodyFallClip;
        [Range(0f, 1f)] public float bodyFallVolume = 1.0f;
        public AudioClip lanternDropClip;
        [Range(0f, 1f)] public float lanternVolume = 0.3f;
        public AudioClip lastBreathClip;
        [Range(0f, 1f)] public float breathVolume = 0.8f;

        [Header("LAMP PHYSICS")]
        public float dropForwardForce = 2f;
        public float dropUpwardForce = 1f;
        public float rotationForce = 5f;

        [Header("Heartbeat Rhythm (Ритм)")]

        public AudioClip heartbeatClip; 
        public float startPulseSpeed = 1.0f;
        public float endPulseSpeed = 2.2f;
        public float pulseSharpness = 20f;

        [Header("GLITCH & TUNNEL VISION")]
        public float shakeAmount = 100f;
        public float pixelStep = 20f;
        public float glitchStutter = 0.05f;
        public float scaleAmount = 0.3f;
        public float colorSplitStrength = 3.0f;

        [Tooltip("Наскільки сильно звужується екран перед смертю")]
        [Range(0f, 1f)] public float maxVignetteIntensity = 0.65f;

        [Header("References")]
        public LampController lampController;
        public GameHUDManager hudManager;
        public Volume globalVolume;
        public GameObject deathPanel;
        public AudioSource heartbeatSource;
        public MonoBehaviour playerMovementScript;
        public Collider playerCollider;

        [Header("Visual Effects")]
        public CanvasGroup psychosisCanvasGroup;
        public CanvasGroup darknessCanvasGroup;

        [Header("VIDEO GLITCH")]
        public CanvasGroup videoGlitchCanvasGroup;
        public VideoPlayer glitchVideoPlayer;

        private RectTransform _overlayRect;
        private Vector2 _originalPos;
        private float _currentSanity;
        private bool _isDead = false;
        private float _lastBeatTime = 0f;
        private float _glitchTimer = 0f;
        private Vector2 _currentShakePos;
        private ChromaticAberration _aberration;
        private Vignette _vignette;

        private bool _isHeartbeatMuted = false;

        private void Start()
        {
            _currentSanity = maxSanity;

            if (deathPanel != null) deathPanel.SetActive(false);

            if (psychosisCanvasGroup != null)
            {
                psychosisCanvasGroup.alpha = 0f;
                psychosisCanvasGroup.blocksRaycasts = false;
                _overlayRect = psychosisCanvasGroup.GetComponent<RectTransform>();
                if (_overlayRect != null)
                {
                    _originalPos = _overlayRect.anchoredPosition;
                    _currentShakePos = _originalPos;
                }
            }

            if (darknessCanvasGroup != null) darknessCanvasGroup.alpha = 0f;
            if (videoGlitchCanvasGroup != null) videoGlitchCanvasGroup.alpha = 0f;

            if (glitchVideoPlayer != null)
            {
                glitchVideoPlayer.Prepare();
                glitchVideoPlayer.Play();
                glitchVideoPlayer.Pause();
            }

            if (globalVolume != null && globalVolume.profile != null)
            {
                globalVolume.profile.TryGet(out _aberration);
                globalVolume.profile.TryGet(out _vignette);
            }

            if (heartbeatSource != null)
            {
                heartbeatSource.loop = false;
                heartbeatSource.Stop();
            }

            if (lampController == null) lampController = GetComponentInChildren<LampController>();
            if (playerCollider == null) playerCollider = GetComponent<Collider>();
        }

        private void Update()
        {
            if (_isDead) return;

            bool isLightOn = (lampController != null && lampController.IsLightOn());

            if (isLightOn)
                _currentSanity += (maxSanity / timeToRecover) * Time.deltaTime;
            else
                _currentSanity -= (maxSanity / timeToDeath) * Time.deltaTime;

            _currentSanity = Mathf.Clamp(_currentSanity, 0, maxSanity);

            if (hudManager != null) hudManager.UpdateMindUI(_currentSanity, maxSanity);

            UpdatePsychosisEffects();

            if (_currentSanity <= 0) Die();
        }

        public void SetHeartbeatMute(bool isMuted)
        {
            _isHeartbeatMuted = isMuted;
            
            if (isMuted && heartbeatSource != null)
            {
                heartbeatSource.Stop();
            }
        }

        private void UpdatePsychosisEffects()
        {
            float insanityFactor = 1f - (_currentSanity / maxSanity);

            float currentBPS = Mathf.Lerp(startPulseSpeed, endPulseSpeed, insanityFactor);
            float beatDuration = 1f / currentBPS;
            bool isBeat = false;

            if (Time.time - _lastBeatTime >= beatDuration) { _lastBeatTime = Time.time; isBeat = true; }

            if (isBeat && insanityFactor > 0.05f && heartbeatSource != null && heartbeatClip != null && !_isHeartbeatMuted)
            {
                heartbeatSource.PlayOneShot(heartbeatClip, 1f);
            }

            float pulseWave = Mathf.Sin(((Time.time - _lastBeatTime) / beatDuration) * Mathf.PI);
            if (pulseWave < 0) pulseWave = 0;
            float impact = Mathf.Pow(pulseWave, pulseSharpness) * insanityFactor;

            if (darknessCanvasGroup != null) darknessCanvasGroup.alpha = insanityFactor * 0.95f;

            if (psychosisCanvasGroup != null)
            {
                float bloodAlpha = (impact * 0.8f) + (insanityFactor * 0.3f);
                psychosisCanvasGroup.alpha = Mathf.Clamp01(bloodAlpha);
            }

            if (videoGlitchCanvasGroup != null && glitchVideoPlayer != null)
            {
                videoGlitchCanvasGroup.alpha = impact * 1.5f;
                if (insanityFactor > 0.1f)
                {
                    if (!glitchVideoPlayer.isPlaying) glitchVideoPlayer.Play();
                    glitchVideoPlayer.playbackSpeed = 0.5f + (insanityFactor * 2.0f);
                }
                else
                {
                    if (glitchVideoPlayer.isPlaying) glitchVideoPlayer.Pause();
                    videoGlitchCanvasGroup.alpha = 0f;
                }
            }

            if (_overlayRect != null)
            {
                _overlayRect.localScale = new Vector3(1f + (impact * scaleAmount), 1f + (impact * scaleAmount), 1f);
                _glitchTimer += Time.deltaTime;
                if (_glitchTimer >= glitchStutter)
                {
                    _glitchTimer = 0f;
                    if (impact > 0.01f)
                    {
                        Vector2 r = Random.insideUnitCircle * shakeAmount * impact;
                        float x = Mathf.Round((_originalPos.x + r.x) / pixelStep) * pixelStep;
                        float y = Mathf.Round((_originalPos.y + r.y) / pixelStep) * pixelStep;
                        _currentShakePos = new Vector2(x, y);
                    }
                    else { _currentShakePos = _originalPos; }
                }
                _overlayRect.anchoredPosition = _currentShakePos;
            }

            if (_aberration != null) _aberration.intensity.value = impact * colorSplitStrength;

            if (_vignette != null)
            {
                float baseVignette = insanityFactor * maxVignetteIntensity;
                float pulseVignette = impact * 0.1f;
                _vignette.intensity.value = baseVignette + pulseVignette;
                _vignette.smoothness.value = 1.0f;
            }
        }

private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            if (GlobalSoundManager.Instance != null)
            {
                GlobalSoundManager.Instance.FadeOutAllSounds(2f);
            }

            if (heartbeatSource != null) heartbeatSource.Stop();
            if (glitchVideoPlayer != null) glitchVideoPlayer.Stop();

            if (playerMovementScript != null) playerMovementScript.enabled = false;

            if (lampController != null)
            {
                lampController.transform.SetParent(null);
                Rigidbody lampRb = lampController.GetComponent<Rigidbody>();
                Collider lampCol = lampController.GetComponent<Collider>();

                if (lampRb != null)
                {
                    if (lampCol != null) lampCol.isTrigger = false;
                    lampRb.isKinematic = false;
                    lampRb.useGravity = true;
                    
                    if (playerCollider != null && lampCol != null)
                        Physics.IgnoreCollision(playerCollider, lampCol);

                    Vector3 dropVector = (Camera.main.transform.forward * dropForwardForce) + (Vector3.up * dropUpwardForce);
                    lampRb.AddForce(dropVector, ForceMode.Impulse);
                    lampRb.AddTorque(Random.insideUnitSphere * rotationForce, ForceMode.Impulse);
                }
            }
            
            StartCoroutine(CameraDropEffect());
        }

        public void RestartGame() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

        private IEnumerator CameraDropEffect()
        {
            Transform cam = Camera.main.transform;
            Vector3 startPos = cam.localPosition;
            Quaternion startRot = cam.localRotation;
            Vector3 endPos = startPos + new Vector3(0, -1.4f, 0);
            Quaternion endRot = startRot * Quaternion.Euler(0, 0, 75);
            float duration = 0.7f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / duration;
                float curveValue = fallCurve.Evaluate(percent);
                cam.localPosition = Vector3.Lerp(startPos, endPos, curveValue);
                cam.localRotation = Quaternion.Slerp(startRot, endRot, curveValue);
                yield return null;
            }

            cam.localPosition = endPos;
            cam.localRotation = endRot;

            if (heartbeatSource != null)
            {
                if (bodyFallClip != null) heartbeatSource.PlayOneShot(bodyFallClip, bodyFallVolume);
                if (lanternDropClip != null) heartbeatSource.PlayOneShot(lanternDropClip, lanternVolume);
            }

            Vector3 hitPos = cam.localPosition;
            float bounceTime = 0f;
            while (bounceTime < 0.15f)
            {
                bounceTime += Time.deltaTime;
                cam.localPosition = hitPos + (Random.insideUnitSphere * 0.05f * (1 - (bounceTime / 0.15f)));
                yield return null;
            }
            cam.localPosition = hitPos;

            yield return new WaitForSeconds(0.2f);
            if (heartbeatSource != null && lastBreathClip != null)
                heartbeatSource.PlayOneShot(lastBreathClip, breathVolume);

            float waitTimer = 0f;
            while (waitTimer < 0.2f)
            {
                waitTimer += Time.deltaTime;
                cam.localPosition = hitPos;
                cam.localRotation = endRot;
                yield return null;
            }

            if (deathPanel != null)
            {
                CanvasGroup cg = deathPanel.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0f;

                deathPanel.SetActive(true);

                VideoPlayer vp = deathPanel.GetComponentInChildren<VideoPlayer>();
                if (vp != null)
                {
                    vp.Prepare();
                    while (!vp.isPrepared) yield return null; 
                    vp.Play(); 
                }

                if (cg != null) cg.alpha = 1f;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}