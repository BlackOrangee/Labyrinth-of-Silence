using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Detects light from the player's lamp for enemy AI.
    /// [MODIFIED] Using Linecast for better wall detection and adapted to LampController
    /// </summary>
    public class LightDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("Minimum light level to trigger detection (0.0 to 1.0)")]
        public float detectionThreshold = 0.3f;
        [Tooltip("How often to check for light (seconds)")]
        public float checkInterval = 0.2f;
        [Tooltip("Obstacle layers that block light (Walls, Tables, etc.)")]
        public LayerMask obstacleLayer;
        
        [Tooltip("Soft edge multiplier for light range")]
        public float softEdgeMultiplier = 1.1f;

        [Header("Light Requirements")]
        [Tooltip("Minimum lantern intensity to trigger chase")]
        public float minIntensityForChase = 1.0f;
        [Tooltip("Minimum lantern range to trigger chase")]
        public float minRangeForChase = 5f;

        [Header("Debug")]
        public bool showDebug = true;
        public bool showDebugGizmos = true;

        private LampController lampController;
        private Light lanternLight; 
        
        private float checkTimer;
        private bool isLightDetected;
        private float currentLightLevel;
        private Vector3 lastLightPosition;

        public bool IsLightDetected => isLightDetected;
        public float CurrentLightLevel => currentLightLevel;
        public Vector3 LastLightPosition => lastLightPosition;
        
        public LampController Lamp => lampController;

        void Start()
        {
            FindLantern();
            checkTimer = checkInterval;
        }

        void Update()
        {
            checkTimer -= Time.deltaTime;

            if (checkTimer <= 0f)
            {
                checkTimer = checkInterval;
                CheckForLight();
            }
        }

        void FindLantern()
        {
            lampController = FindObjectOfType<LampController>();

            if (lampController != null)
            {
                lanternLight = lampController.lampLight;

                if (showDebug)
                {
                    Debug.Log($"[LightDetector] Found LampController on: {lampController.gameObject.name}");
                }
            }
            else if (showDebug)
            {
                Debug.LogWarning("[LightDetector] No LampController found in scene!");
            }
        }

        void CheckForLight()
        {
            // Перевірка: чи є лампа і чи вона увімкнена
            if (lampController == null || lanternLight == null || !lampController.IsLightOn())
            {
                isLightDetected = false;
                currentLightLevel = 0f;
                return;
            }

            Vector3 lightPosition = lanternLight.transform.position;
            // [НОВЕ] Піднімаємо точку очей монстра (наприклад, 1.6м), щоб перевірка йшла не від п'ят
            Vector3 enemyEyePosition = transform.position + Vector3.up * 1.6f; 
            
            float distance = Vector3.Distance(lightPosition, enemyEyePosition);
            float effectiveRange = lanternLight.range * softEdgeMultiplier;

            // 1. Перевірка Дистанції
            if (distance > effectiveRange)
            {
                isLightDetected = false;
                currentLightLevel = 0f;
                return;
            }

            // 2. Перевірка Перешкод (Linecast)
            // Ми проводимо лінію від очей монстра до лампи.
            // Якщо лінія вдаряється у щось (стіна, стіл) - світло перекрито.
            if (Physics.Linecast(enemyEyePosition, lightPosition, out RaycastHit hit, obstacleLayer))
            {
                // Переконуємось, що ми не влучили в самого себе або в гравця (якщо вони випадково на шарі перешкод)
                // Якщо влучили в об'єкт, який не є лампою і не є частиною гравця -> це перешкода
                if (hit.transform.root != lampController.transform.root)
                {
                    if (showDebugGizmos)
                    {
                        // Червона лінія - світло заблоковане
                        Debug.DrawLine(enemyEyePosition, hit.point, Color.red, checkInterval);
                    }

                    isLightDetected = false;
                    currentLightLevel = 0f;
                    return;
                }
            }

            if (showDebugGizmos)
            {
                // Зелена лінія - світло проходить
                Debug.DrawLine(enemyEyePosition, lightPosition, Color.green, checkInterval);
            }

            // 3. Розрахунок Яскравості
            currentLightLevel = CalculateLightLevel(distance, lanternLight.intensity, effectiveRange);

            bool wasDetected = isLightDetected;
            isLightDetected = currentLightLevel >= detectionThreshold;

            if (isLightDetected)
            {
                lastLightPosition = lightPosition;

                if (!wasDetected && showDebug)
                {
                    Debug.Log($"[LightDetector] Light detected! Level: {currentLightLevel:F2}");
                }
            }
            else if (wasDetected && showDebug)
            {
                Debug.Log($"[LightDetector] Light lost!");
            }
        }

        float CalculateLightLevel(float distance, float intensity, float effectiveRange)
        {
            float normalizedDistance = Mathf.Clamp01(distance / effectiveRange);
            float attenuation = 1f - normalizedDistance;
            attenuation = attenuation * attenuation * (3f - 2f * attenuation); 
            float proximityBoost = Mathf.Lerp(1.5f, 1.0f, normalizedDistance);
            float lightLevel = intensity * attenuation * proximityBoost;
            return lightLevel;
        }

        public bool CanTriggerChase()
        {
            if (!isLightDetected || lampController == null || lanternLight == null)
            {
                return false;
            }
            bool intensityEnough = lanternLight.intensity >= minIntensityForChase;
            bool rangeEnough = lanternLight.range >= minRangeForChase;
            return intensityEnough && rangeEnough;
        }

        public float GetDistanceToLight()
        {
            if (lanternLight == null) return float.MaxValue;
            return Vector3.Distance(transform.position, lanternLight.transform.position);
        }

        void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            if (isLightDetected && lanternLight != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 startPos = transform.position + Vector3.up * 1.6f;
                Gizmos.DrawLine(startPos, lanternLight.transform.position);
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawSphere(startPos, 0.2f);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (lampController != null && lampController.lampLight != null)
            {
                float effectiveRange = lampController.lampLight.range * softEdgeMultiplier;
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, effectiveRange);
            }
        }
    }
}