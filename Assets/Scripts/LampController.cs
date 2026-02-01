using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts
{
    public class LampController : MonoBehaviour
    {
        [Header("References")]
        public Light lampLight;
        public Light fillLight;
        public ParticleSystem fireEffect;

        [Header("Audio")]
        public AudioSource lampAudioSource;

        // [СТАРЕ] Старі змінні для звуків
        // public AudioClip turnOnSound;
        // public AudioClip turnOffSound;
        // public AudioClip emptyClickSound;
        
        // [НОВЕ] --- ADVANCED AUDIO SYSTEM (Sound Profiles) ---
        [Header("Advanced Audio (Sound Profiles)")]
        public SoundProfile turnOnProfile;   // Профіль для ввімкнення
        public SoundProfile turnOffProfile;  // Профіль для вимкнення
        public SoundProfile emptyClickProfile; // Клік, коли немає пального
        // -----------------------------------------------------

        public AudioClip burningSoundLoop; // Луп залишаємо як є, він простий

        [Header("Old UI (Optional)")]
        public Slider fuelSlider;
        public TextMeshProUGUI fuelPercentageText;

        [Header("Settings")]
        public float maxFuel = 100f;

        [Header("Fading & UI Settings")]
        [Range(0f, 1f)]
        public float fadeThreshold = 0.2f;

        public Color normalTextColor = Color.green;
        public Color warningTextColor = Color.red;

        [Header("Main Light Intensity")]
        public float dimIntensity = 20f; 
        public float brightIntensity = 40f; 
        public float dimRange = 10f; 
        public float brightRange = 20f; 
        public float lightChangeSpeed = 5f; 

        [Header("Fill Light Intensity (Soft Glow)")]
        public float fillDimIntensity = 0.2f; 
        public float fillBrightIntensity = 1.5f; 

        [Header("Flame Flicker Effect")] 
        public bool useFlicker = true;
        public float flickerSpeed = 10f; 
        public float flickerStrength = 0.1f; 

        [Header("Fuel Consumption")]
        public float dimBurnRate = 2f; 
        public float brightBurnRate = 10f;

        private float currentFuel;
        private int lightMode = 0; // 0 = Off, 1 = Dim, 2 = Bright
        
        private float targetMainIntensity;
        private float targetMainRange;
        private float targetFillIntensity;

        private void Start()
        {
            currentFuel = maxFuel;

            if (fuelSlider != null)
            {
                fuelSlider.maxValue = maxFuel;
                fuelSlider.value = currentFuel;
            }

            if (lampAudioSource != null)
            {
                lampAudioSource.loop = true;
                lampAudioSource.playOnAwake = false;
                lampAudioSource.clip = burningSoundLoop;
            }

            UpdateLightTargets();
            
            if (lampLight != null) { lampLight.intensity = targetMainIntensity; lampLight.range = targetMainRange; }
            if (fillLight != null) { fillLight.intensity = targetFillIntensity; }
            
            UpdateUI();
        }

        private void Update()
        {
            HandleInput();
            ConsumeFuel();
            UpdateLightVisuals(); 
            UpdateUI();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                // Перевірка пального ПЕРЕД перемиканням
                if (currentFuel <= 0 && lightMode == 0)
                {
                    Debug.Log("Click! No fuel.");
                    
                    // [СТАРЕ]
                    // if (emptyClickSound) AudioSource.PlayClipAtPoint(emptyClickSound, transform.position);
                    // else if (turnOffSound) AudioSource.PlayClipAtPoint(turnOffSound, transform.position);

                    // [НОВЕ] Граємо звук "пустишки" через просунуту систему
                    if (emptyClickProfile != null) emptyClickProfile.Play(lampAudioSource);
                    else if (turnOffProfile != null) turnOffProfile.Play(lampAudioSource);

                    return; 
                }

                int oldMode = lightMode;
                lightMode++;
                if (lightMode > 2) lightMode = 0;

                // Аудіо
                if (lampAudioSource != null)
                {
                    if (lightMode == 0) // Вимкнення
                    {
                        lampAudioSource.Stop(); 
                        
                        // [СТАРЕ]
                        // if(turnOffSound) AudioSource.PlayClipAtPoint(turnOffSound, transform.position);
                        
                        // [НОВЕ] Звук вимкнення
                        if (turnOffProfile != null) turnOffProfile.Play(lampAudioSource);
                    }
                    else if (oldMode == 0) // Увімкнення
                    {
                        // [СТАРЕ]
                        // if(turnOnSound) AudioSource.PlayClipAtPoint(turnOnSound, transform.position);
                        
                        // [НОВЕ] Звук увімкнення
                        if (turnOnProfile != null) turnOnProfile.Play(lampAudioSource);

                        if(burningSoundLoop) lampAudioSource.Play(); 
                    }
                }

                UpdateLightTargets();
            }
        }

        private void ConsumeFuel()
        {
            if (lightMode == 0) return;

            float burnRate = (lightMode == 2) ? brightBurnRate : dimBurnRate;

            if (currentFuel > 0)
            {
                currentFuel -= burnRate * Time.deltaTime;
                
                if (currentFuel <= 0)
                {
                    currentFuel = 0;
                    TurnOffDueToNoFuel(); 
                }
            }
        }

        private void TurnOffDueToNoFuel()
        {
            if (lightMode == 0) return; 

            lightMode = 0;
            
            if (lampAudioSource != null) lampAudioSource.Stop();
            
            // [СТАРЕ]
            // if (turnOffSound) AudioSource.PlayClipAtPoint(turnOffSound, transform.position);
            
            // [НОВЕ]
            if (turnOffProfile != null) turnOffProfile.Play(lampAudioSource);
            
            Debug.Log("Fuel empty! Lamp turned off.");
            UpdateLightTargets();
        }

        // ... Далі код без змін ...
        private void UpdateLightTargets()
        {
            if (currentFuel <= 0)
            {
                targetMainIntensity = 0f;
                targetMainRange = 0f; 
                targetFillIntensity = 0f;

                if (fireEffect != null)
                {
                    fireEffect.Stop();
                    fireEffect.Clear();
                }
                return; 
            }

            bool isLightOn = (lightMode != 0);
            float fadeFactor = 1.0f;
            float thresholdValue = maxFuel * fadeThreshold;

            if (currentFuel < thresholdValue)
            {
                fadeFactor = currentFuel / thresholdValue;
            }

            if (isLightOn)
            {
                float baseIntensity = (lightMode == 2) ? brightIntensity : dimIntensity;
                targetMainIntensity = baseIntensity * fadeFactor;
                
                targetMainRange = (lightMode == 2) ? brightRange : dimRange;

                float baseFill = (lightMode == 2) ? fillBrightIntensity : fillDimIntensity;
                targetFillIntensity = baseFill * fadeFactor;

                if (fireEffect != null)
                {
                    if (!fireEffect.isPlaying) fireEffect.Play();
                    var main = fireEffect.main;
                    float targetSize = (lightMode == 2) ? 1.0f : 0.6f;
                    main.startSizeMultiplier = targetSize * fadeFactor;
                }
            }
            else
            {
                targetMainIntensity = 0f;
                targetMainRange = 0f; 
                targetFillIntensity = 0f;

                if (fireEffect != null)
                {
                    fireEffect.Stop();
                }
            }
        }

        private void UpdateLightVisuals()
        {
            if (lampLight != null)
            {
                float currentBaseIntensity = Mathf.Lerp(lampLight.intensity, targetMainIntensity, Time.deltaTime * lightChangeSpeed);
                
                if (lightMode != 0 && useFlicker && currentFuel > 0)
                {
                    float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
                    float flickerMultiplier = 1.0f + (noise - 0.5f) * flickerStrength; 
                    lampLight.intensity = currentBaseIntensity * flickerMultiplier;
                }
                else
                {
                    lampLight.intensity = currentBaseIntensity;
                }

                lampLight.range = Mathf.Lerp(lampLight.range, targetMainRange, Time.deltaTime * lightChangeSpeed);
                lampLight.enabled = lampLight.intensity > 0.01f;
            }

            if (fillLight != null)
            {
                fillLight.intensity = Mathf.Lerp(fillLight.intensity, targetFillIntensity, Time.deltaTime * lightChangeSpeed);
                fillLight.enabled = fillLight.intensity > 0.01f;
            }
        }

        private void UpdateUI()
        {
            if (fuelSlider != null)
            {
                fuelSlider.value = currentFuel;
            }

            if (fuelPercentageText != null)
            {
                float fraction = currentFuel / maxFuel;
                int percent = Mathf.Clamp(Mathf.RoundToInt(fraction * 100f), 0, 100);

                fuelPercentageText.text = $"{percent}%";

                if (fraction <= fadeThreshold)
                {
                    fuelPercentageText.color = warningTextColor;
                }
                else
                {
                    fuelPercentageText.color = normalTextColor;
                }
            }

            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.UpdateFuelUI(currentFuel, maxFuel);
            }
        }

        public void Refuel(float amount)
        {
            currentFuel += amount;
            if (currentFuel > maxFuel) currentFuel = maxFuel;

            UpdateLightTargets();
            UpdateUI();
        }

        public float GetCurrentFuel() => currentFuel;
        public float GetMaxFuel() => maxFuel;

        public bool IsLightOn()
        {
            return lightMode != 0 && currentFuel > 0;
        }
    }
}