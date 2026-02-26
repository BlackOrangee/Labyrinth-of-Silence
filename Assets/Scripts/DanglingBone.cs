using UnityEngine;

namespace Assets.Scripts
{
    public class DanglingBone : MonoBehaviour
    {
        [Header("Налаштування фізики")]
        [Tooltip("Сила ваги (0.1 - м'яко, 1.0 - важко). Спробуй 0.7")]
        [Range(0, 1)] public float gravityInfluence = 0.7f;
        
        [Tooltip("Інерція (0 - желе, 1 - камінь). Спробуй 0.1")]
        [Range(0, 1)] public float stiffness = 0.1f;
        
        [Tooltip("Затухання (гальмування). Спробуй 0.9")]
        [Range(0, 1)] public float damping = 0.9f;
        private Vector3 targetPos;
        private Vector3 dynamicPos;
        private Vector3 velocity;
        private float boneLength;
        void Awake()
        {
            if (transform.childCount > 0)
            {
                boneLength = Vector3.Distance(transform.position, transform.GetChild(0).position);

                dynamicPos = transform.GetChild(0).position;
            }
            else
            {
                boneLength = 0.1f;

                dynamicPos = transform.position + transform.TransformDirection(new Vector3(0, -1, 0)) * boneLength;
            }
        }
        void LateUpdate()
        {
            Vector3 animatedTargetPos;
            if (transform.childCount > 0)
                animatedTargetPos = transform.TransformPoint(transform.GetChild(0).localPosition);
            else
                animatedTargetPos = transform.TransformPoint(new Vector3(0, -1, 0) * boneLength);

            Vector3 gravityTarget = animatedTargetPos + Vector3.down * gravityInfluence * 2f;

            velocity += (gravityTarget - dynamicPos) * stiffness;
            velocity *= damping;
            dynamicPos += velocity;

            Vector3 direction = (dynamicPos - transform.position).normalized;
            dynamicPos = transform.position + direction * boneLength;

            transform.LookAt(dynamicPos, transform.parent.up); 

        }
        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, dynamicPos);
            Gizmos.DrawWireSphere(dynamicPos, 0.02f);
        }
    }
}