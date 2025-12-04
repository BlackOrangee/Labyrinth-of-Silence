using UnityEngine;

namespace Assets.Scripts
{
    public class SmartLampDimmer : MonoBehaviour
    {
        [Header("Settings")]
        public Light targetLight;
    public LayerMask obstacleLayer;
        public float checkDistance = 0.35f;
        
        private float originalRange;

        void Start()
        {
            if (targetLight == null) targetLight = GetComponent<Light>();
            originalRange = targetLight.range;
        }

        void Update()
        {
            if (targetLight == null) return;

            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, checkDistance, obstacleLayer))
            {
                float ratio = hit.distance / checkDistance;
                targetLight.range = Mathf.Lerp(0.05f, originalRange, ratio);
            }
            else
            {
                targetLight.range = Mathf.Lerp(targetLight.range, originalRange, Time.deltaTime * 5f);
            }
        }
    }
}