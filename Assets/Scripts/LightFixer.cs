using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts
{
    [ExecuteAlways] 
    public class LightShadowFixer : MonoBehaviour
    {
        [Header("Force Shadow Settings")]
        public float shadowNearPlane = 0.01f;
        public float shadowBias = 0.002f;
        public float normalBias = 0.0f;

        private Light myLight;

        void Update()
        {
            if (myLight == null) myLight = GetComponent<Light>();

            if (myLight != null)
            {
                myLight.shadowNearPlane = shadowNearPlane;
                myLight.shadowBias = shadowBias;
                myLight.shadowNormalBias = normalBias;
            }
        }
    }
}