using UnityEngine;

namespace Assets.Scripts
{
    public class LampCollisionAvoidance : MonoBehaviour
    {
        [Header("Settings")]
        public LayerMask obstacleLayer;
        public float checkDistance = 0.6f;
        public float moveSpeed = 15f;
        
        [Header("Position Limits")]
        public float normalZ = 0.15f;
        public float retractedZ = -0.5f;

        void Start()
        {
            if (obstacleLayer == 0) obstacleLayer = LayerMask.GetMask("Default");
        }

        void Update()
        {
            Transform parent = transform.parent;
            if (parent == null) return;

            float targetZ = normalZ;

            Ray ray = new Ray(parent.position, parent.forward);
            RaycastHit hit;

            Debug.DrawRay(parent.position, parent.forward * checkDistance, Color.red);

            if (Physics.Raycast(ray, out hit, checkDistance, obstacleLayer))
            {
                float ratio = 1f - (hit.distance / checkDistance);

                targetZ = Mathf.Lerp(normalZ, retractedZ, ratio);
            }

            Vector3 currentPos = transform.localPosition;

            float newZ = Mathf.Lerp(currentPos.z, targetZ, Time.deltaTime * moveSpeed);
            
            transform.localPosition = new Vector3(currentPos.x, currentPos.y, newZ);
        }
    }
}