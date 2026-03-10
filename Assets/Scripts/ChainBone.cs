using UnityEngine;

namespace Assets.Scripts
{
    public class ChainBone : MonoBehaviour
    {
        [Header("Фізика ланцюга")]
        [Range(0.5f, 5f)]
        public float gravityScale = 1.5f;

        [Range(0.01f, 0.3f)]
        public float damping = 0.08f;

        // [NEW] --- БЛОК КОЛІЗІЇ (ВІДШТОВХУВАННЯ) ---
        [Header("Колізія (щоб не проходило крізь тіло)")]
        [Tooltip("Об'єкт, від якого треба відштовхуватися (наприклад, кістка коліна або стегна)")]
        public Transform collisionTarget; 

        [Tooltip("Розмір сфери відштовхування")]
        [Range(0f, 1f)]
        public float collisionRadius = 0.15f; 

        [Tooltip("Зміщення сфери відносно центру collisionTarget")]
        public Vector3 collisionOffset = Vector3.zero;
        // ------------------------------------------

        private Vector3    _simTip;
        private Vector3    _prevTip;
        private float      _linkLength;
        private Vector3    _localChainAxis;
        private bool       _initialized;

        void OnEnable()  => Initialize();
        void OnDisable() => _initialized = false;
        
        private void Initialize()
        {
            if (transform.parent == null) return;

            if (transform.childCount > 0)
                _linkLength = Vector3.Distance(
                    transform.position,
                    transform.GetChild(0).position
                );
            else
                _linkLength = Vector3.Distance(
                    transform.position,
                    transform.parent.position
                ) * 0.8f;

            if (_linkLength < 0.001f) _linkLength = 0.05f;

            if (transform.childCount > 0)
            {
                Vector3 worldDir = transform.GetChild(0).position - transform.position;
                _localChainAxis = transform.InverseTransformDirection(worldDir.normalized);
            }
            else
            {
                Vector3 worldDir = transform.position - transform.parent.position;
                _localChainAxis = transform.InverseTransformDirection(worldDir.normalized);
            }

            Vector3 startTip = transform.position +
                               transform.TransformDirection(_localChainAxis) * _linkLength;
            _simTip  = startTip;
            _prevTip = startTip;
            _initialized = true;
        }

        void LateUpdate()
        {
            if (!_initialized)            return;
            if (transform.parent == null) return;

            float dt       = Time.deltaTime;
            Vector3 vel    = (_simTip - _prevTip) * (1f - damping);
            Vector3 grav   = Physics.gravity * gravityScale * dt * dt;

            _prevTip = _simTip;
            _simTip += vel + grav;

            // [NEW] --- ЛОГІКА ВІДШТОВХУВАННЯ ---
            // Перевіряємо, чи ми призначили об'єкт для колізії (ногу) і чи радіус більше 0
            if (collisionTarget != null && collisionRadius > 0f)
            {
                // Знаходимо центр нашої захисної сфери
                Vector3 sphereCenter = collisionTarget.position + collisionTarget.TransformDirection(collisionOffset);
                
                // Вектор від центру сфери до кінчика нашого ланцюга
                Vector3 fromCenterToTip = _simTip - sphereCenter;
                
                // Визначаємо відстань
                float distanceToTip = fromCenterToTip.magnitude;

                // Якщо кінчик ланцюга опинився ВСЕРЕДИНІ сфери (ближче ніж радіус)
                if (distanceToTip < collisionRadius)
                {
                    // Виштовхуємо кінчик ланцюга на поверхню сфери!
                    Vector3 pushDirection = fromCenterToTip.normalized;
                    
                    // Якщо кінчик ідеально в центрі (рідко, але буває), штовхаємо просто назовні
                    if (distanceToTip == 0) pushDirection = Vector3.forward; 

                    _simTip = sphereCenter + (pushDirection * collisionRadius);
                }
            }
            // ------------------------------------

            Vector3 anchor = transform.position;
            Vector3 toTip  = _simTip - anchor;
            float   dist   = toTip.magnitude;

            if (dist > 0.0001f)
                _simTip = anchor + toTip * (_linkLength / dist);
            else
                _simTip = anchor + Vector3.down * _linkLength;

            Vector3 bindDir = transform.TransformDirection(_localChainAxis);
            Vector3 simDir  = (_simTip - anchor).normalized;

            if (bindDir.sqrMagnitude > 0.0001f && simDir.sqrMagnitude > 0.0001f)
            {
                Quaternion rot   = Quaternion.FromToRotation(bindDir, simDir);
                transform.rotation = rot * transform.rotation;
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            // Малюємо лінію ланцюга
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_simTip, 0.015f);
            Gizmos.DrawLine(transform.position, _simTip);

            Gizmos.color = Color.green;
            Gizmos.DrawRay(
                transform.position,
                transform.TransformDirection(_localChainAxis) * _linkLength
            );

            // [NEW] --- МАЛЮЄМО СФЕРУ КОЛІЗІЇ, ЩОБ ЇЇ БУЛО ВИДНО В РЕДАКТОРІ ---
            if (collisionTarget != null && collisionRadius > 0f)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Напівпрозорий червоний
                Vector3 sphereCenter = collisionTarget.position + collisionTarget.TransformDirection(collisionOffset);
                Gizmos.DrawSphere(sphereCenter, collisionRadius);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(sphereCenter, collisionRadius);
            }
        }
#endif
    }
}