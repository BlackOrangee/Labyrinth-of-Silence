using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Detects light from lantern for enemy AI
    /// Calculates light intensity at enemy position
    /// </summary>
    public class LightDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("Minimum light level to trigger detection")]
        public float detectionThreshold = 0.3f;
        [Tooltip("How often to check for light (seconds)")]
        public float checkInterval = 0.2f;
        [Tooltip("Obstacle layers that block light")]
        public LayerMask obstacleLayer;
        [Tooltip("Use lantern's effective range (synced with visual) instead of fixed soft edge")]
        public bool useLanternEffectiveRange = true;
        [Tooltip("Soft edge multiplier - only used if useLanternEffectiveRange is false (1.0 = no extension, 1.3 = 30% extension)")]
        public float softEdgeMultiplier = 1.3f;

        [Header("Light Requirements")]
        [Tooltip("Minimum lantern intensity to trigger chase")]
        public float minIntensityForChase = 1.0f;
        [Tooltip("Minimum lantern range to trigger chase")]
        public float minRangeForChase = 8f;

        [Header("Debug")]
        [Tooltip("Show debug information")]
        public bool showDebug = true;
        [Tooltip("Show debug gizmos")]
        public bool showDebugGizmos = true;

        private LanternController lanternController;
        private Light lanternLight;
        private float checkTimer;
        private bool isLightDetected;
        private float currentLightLevel;
        private Vector3 lastLightPosition;

        public bool IsLightDetected => isLightDetected;
        public float CurrentLightLevel => currentLightLevel;
        public Vector3 LastLightPosition => lastLightPosition;
        public LanternController Lantern => lanternController;

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
            lanternController = FindObjectOfType<LanternController>();

            if (lanternController != null)
            {
                lanternLight = lanternController.GetComponent<Light>();

                if (showDebug)
                {
                    Debug.Log($"[LightDetector] Found lantern: {lanternController.gameObject.name}");
                }
            }
            else if (showDebug)
            {
                Debug.LogWarning("[LightDetector] No LanternController found in scene!");
            }
        }

        void CheckForLight()
        {
            if (lanternLight == null || !lanternLight.enabled)
            {
                isLightDetected = false;
                currentLightLevel = 0f;
                return;
            }

            Vector3 lightPosition = lanternLight.transform.position;
            Vector3 enemyPosition = transform.position;
            Vector3 directionToLight = lightPosition - enemyPosition;
            float distance = directionToLight.magnitude;

            float effectiveRange;
            if (useLanternEffectiveRange && lanternController != null)
            {
                effectiveRange = lanternController.EffectiveRange;
            }
            else
            {
                effectiveRange = lanternLight.range * softEdgeMultiplier;
            }

            if (distance > effectiveRange)
            {
                isLightDetected = false;
                currentLightLevel = 0f;
                return;
            }

            Vector3 directionNormalized = directionToLight / distance;
            RaycastHit hit;
            if (Physics.Raycast(enemyPosition + Vector3.up * 0.5f, directionNormalized, out hit, distance, obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                if (showDebugGizmos)
                {
                    Debug.DrawRay(enemyPosition + Vector3.up * 0.5f, directionNormalized * hit.distance, Color.red, checkInterval);
                }

                isLightDetected = false;
                currentLightLevel = 0f;
                return;
            }

            if (showDebugGizmos)
            {
                Debug.DrawRay(enemyPosition + Vector3.up * 0.5f, directionNormalized * distance, Color.green, checkInterval);
            }

            currentLightLevel = CalculateLightLevel(distance, lanternLight.intensity, effectiveRange);

            bool wasDetected = isLightDetected;
            isLightDetected = currentLightLevel >= detectionThreshold;

            if (isLightDetected)
            {
                lastLightPosition = lightPosition;

                if (!wasDetected && showDebug)
                {
                    Debug.Log($"[LightDetector] Light detected! Level: {currentLightLevel:F2}, Distance: {distance:F2}");
                }
            }
            else if (wasDetected && showDebug)
            {
                Debug.Log($"[LightDetector] Light lost! Last position: {lastLightPosition}");
            }
        }

        float CalculateLightLevel(float distance, float intensity, float effectiveRange)
        {
            float normalizedDistance = Mathf.Clamp01(distance / effectiveRange);

            float attenuation = 1f - normalizedDistance;
            attenuation = attenuation * attenuation * (3f - 2f * attenuation); // Smoothstep

            float proximityBoost = Mathf.Lerp(1.5f, 1.0f, normalizedDistance);
            float lightLevel = intensity * attenuation * proximityBoost;

            return lightLevel;
        }

        public bool CanTriggerChase()
        {
            if (!isLightDetected || lanternController == null)
            {
                return false;
            }

            bool intensityEnough = lanternController.CurrentIntensity >= minIntensityForChase;
            bool rangeEnough = lanternController.BaseRange >= minRangeForChase;

            return intensityEnough && rangeEnough;
        }

        public float GetDistanceToLight()
        {
            if (lanternLight == null) return float.MaxValue;
            return Vector3.Distance(transform.position, lanternLight.transform.position);
        }

        void OnDrawGizmos()
        {
            if (!showDebugGizmos)
            {
                return;
            }

            if (isLightDetected && lanternLight != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, lanternLight.transform.position);

                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.5f);
            }

            if (lastLightPosition != Vector3.zero)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(lastLightPosition, 0.5f);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (lanternLight != null)
            {
                float effectiveRange;
                if (useLanternEffectiveRange && lanternController != null)
                {
                    effectiveRange = lanternController.EffectiveRange;
                }
                else
                {
                    effectiveRange = lanternLight.range * softEdgeMultiplier;
                }

                Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, effectiveRange);
            }
        }
    }
}
