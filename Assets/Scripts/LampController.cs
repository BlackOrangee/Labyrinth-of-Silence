using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;

namespace Assets.Scripts
{
    public class LampController : MonoBehaviour
    {
        private static string PlayerStatsTransitPath =>
            Path.Combine(Application.persistentDataPath, "transit_player_stats.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ClearTransitOnSessionStart()
        {
            ClearPlayerTransit();
        }

        public static void ClearPlayerTransit()
        {
            try
            {
                if (File.Exists(PlayerStatsTransitPath))
                    File.Delete(PlayerStatsTransitPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LampController] Transit clear failed: {e.Message}");
            }
        }

        private void LoadTransit()
        {
            if (!File.Exists(PlayerStatsTransitPath)) return;
            try
            {
                var data = JsonUtility.FromJson<PlayerStatsTransitData>(File.ReadAllText(PlayerStatsTransitPath));
                if (data == null) return;
                if (data.fuel >= 0f) currentFuel = data.fuel;
                if (data.lampMode >= 0) lightMode = data.lampMode;
                Debug.Log($"[LampController] Transit loaded: fuel={currentFuel}, mode={lightMode}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LampController] Transit load failed: {e.Message}");
            }
        }

        private void SaveTransit()
        {
            try
            {
                PlayerStatsTransitData data = null;
                if (File.Exists(PlayerStatsTransitPath))
                    data = JsonUtility.FromJson<PlayerStatsTransitData>(File.ReadAllText(PlayerStatsTransitPath));
                if (data == null) data = new PlayerStatsTransitData();
                data.fuel = currentFuel;
                data.lampMode = lightMode;
                File.WriteAllText(PlayerStatsTransitPath, JsonUtility.ToJson(data));
            }
            catch (Exception e)
            {
                Debug.LogError($"[LampController] Transit save failed: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            SaveTransit();
        }

        [Header("MAIN GROUP")]
        [Tooltip("Перетягни сюди об'єкт Light_Group, в якому лежать світло і вогонь")]
        public GameObject lightGroupObject; 

        [Header("References")]
        public Light lampLight;

        public ParticleSystem fireEffect;

        [Header("Audio")]
        public AudioSource lampAudioSource;

        [Header("Advanced Audio (Sound Profiles)")]
        public SoundProfile turnOnProfile;
        public SoundProfile turnOffProfile;
        public SoundProfile emptyClickProfile;

        public AudioClip burningSoundLoop;

        [Header("Old UI (Optional)")]
        public Slider fuelSlider;
        public TextMeshProUGUI fuelPercentageText;

        [Header("Settings")]
        public float maxFuel = 100f;

        [Header("Fading & UI Settings")]
        [Range(0f, 1f)]
        public float fadeThreshold = 0.2f;

        // public Color normalTextColor = Color.green;
        // public Color warningTextColor = Color.red;

        [Header("Main Light Intensity")]
        public float dimIntensity = 3f; 
        public float brightIntensity = 8f; 
        public float dimRange = 10f; 
        public float brightRange = 20f; 
        
        [Tooltip("Швидкість зміни яскравості. Більше = швидше.")]
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
        private int lightMode = 0;
        
        private float targetMainIntensity;
        private float targetMainRange;

        private void Start()
        {
            currentFuel = maxFuel;
            lightMode = 0;
            LoadTransit();

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

            if (fireEffect != null) { var em = fireEffect.emission; em.enabled = false; }

            UpdateLightTargets();
            
            if (lampLight != null) { lampLight.intensity = targetMainIntensity; lampLight.range = targetMainRange; }
            
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
                if (currentFuel <= 0 && lightMode == 0)
                {
                    Debug.Log("Click! No fuel.");
                    
                    if (emptyClickProfile != null) emptyClickProfile.Play(lampAudioSource);
                    else if (turnOffProfile != null) turnOffProfile.Play(lampAudioSource);

                    return; 
                }

                int oldMode = lightMode;
                lightMode++;
                if (lightMode > 2) lightMode = 0;

                if (lampAudioSource != null)
                {
                    if (lightMode == 0)
                    {
                        lampAudioSource.Stop();

                        if (turnOffProfile != null) turnOffProfile.Play(lampAudioSource);
                    }

                    else if (lightMode == 1)
                    {
                        if (turnOnProfile != null) turnOnProfile.Play(lampAudioSource);

                        if (burningSoundLoop) lampAudioSource.Play();
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

            if (turnOffProfile != null) turnOffProfile.Play(lampAudioSource);
            
            Debug.Log("Fuel empty! Lamp turned off.");
            UpdateLightTargets();
        }
        
        private void UpdateLightTargets()
        {
            if (currentFuel <= 0 || lightMode == 0)
            {
                targetMainIntensity = 0f;
                targetMainRange = 0f; 

                if (lightGroupObject != null) lightGroupObject.SetActive(true); 

                if (fireEffect != null)
                {
                    var emission = fireEffect.emission;
                    emission.enabled = false;
                }
                
                return; 
            }
            else
            {
                if (fireEffect != null)
                {
                    var emission = fireEffect.emission;
                    emission.enabled = true;
                }
            }

            if (lightGroupObject != null) lightGroupObject.SetActive(true);

            float fadeFactor = 1.0f;
            float thresholdValue = maxFuel * fadeThreshold;

            if (currentFuel < thresholdValue)
            {
                fadeFactor = currentFuel / thresholdValue;
            }

            if (lightMode == 1)
            {
                targetMainIntensity = dimIntensity * fadeFactor;
                targetMainRange = dimRange;
            }
            else if (lightMode == 2)
            {
                targetMainIntensity = brightIntensity * fadeFactor;
                targetMainRange = brightRange;
            }

            if (fireEffect != null)
            {
                
                var main = fireEffect.main;
                float targetSize = (lightMode == 2) ? 1.0f : 0.5f;
                main.startSizeMultiplier = targetSize * fadeFactor;
            }
        }

        private void UpdateLightVisuals()
        {
            if (lightGroupObject != null && !lightGroupObject.activeSelf) return;

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

                // if (fraction <= fadeThreshold)
                // {
                //     fuelPercentageText.color = warningTextColor;
                // }
                // else
                // {
                //     fuelPercentageText.color = normalTextColor;
                // }
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

        public void TurnOff()
        {
            if (lightMode == 0) return;
            lightMode = 0;
            if (lampAudioSource != null) lampAudioSource.Stop();
            if (turnOffProfile != null) turnOffProfile.Play(lampAudioSource);
            UpdateLightTargets();
        }
    }
}