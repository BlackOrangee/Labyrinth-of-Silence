using UnityEngine;

namespace Assets.Scripts
{
    [DisallowMultipleComponent]
    public class StableClothBone : MonoBehaviour
    {
        [Header("Параметри тканини")]
        [Range(0f, 2f)]
        public float gravityScale = 0.4f;

        [Range(0.01f, 0.3f)]
        public float damping = 0.04f;

        [Range(0f, 15f)]
        public float windStrength = 4f;

        [Range(0f, 5f)]
        public float windSpeed = 1.2f;

        [Range(0f, 1f)]
        public float inertiaStrength = 0.6f;

        [Tooltip("Зсув хвилі. Для верхньої кістки 0, для наступної 0.5, потім 1.0 і т.д.")]
        [Range(0f, 5f)]
        public float wavePhase = 0f;

        [Header("Обмеження руху")]
        [Tooltip("Наскільки сильно обмежуємо бічне скручування (1 = повністю, 0 = вільно)")]
        [Range(0f, 1f)]
        public float twistConstraint = 0.92f;

        private Vector3 _simTip;
        private Vector3 _prevTip;
        private Vector3 _prevAnchorPos;
        private float   _linkLength;
        private Vector3 _localChainAxis;
        private bool    _initialized;
        void OnEnable()  => Initialize();
        void OnDisable() => _initialized = false;
        private void Initialize()
        {
            if (transform.parent == null) return;

            _linkLength = transform.childCount > 0
                ? Vector3.Distance(transform.position, transform.GetChild(0).position)
                : Vector3.Distance(transform.position, transform.parent.position) * 0.8f;

            if (_linkLength < 0.001f) _linkLength = 0.05f;

            Vector3 worldDir = transform.childCount > 0
                ? transform.GetChild(0).position - transform.position
                : transform.position - transform.parent.position;

            _localChainAxis = transform.InverseTransformDirection(worldDir.normalized);

            Vector3 startTip = transform.position +
                               transform.TransformDirection(_localChainAxis) * _linkLength;
            _simTip        = startTip;
            _prevTip       = startTip;
            _prevAnchorPos = transform.position;
            _initialized   = true;
        }
        void LateUpdate()
        {
            if (!_initialized || transform.parent == null) return;

            float dt = Time.deltaTime;
            Vector3 anchor = transform.position;

            Vector3 velocity = (_simTip - _prevTip) * (1f - damping);
            Vector3 gravity = Physics.gravity * gravityScale * dt * dt;

            Vector3 anchorDelta = anchor - _prevAnchorPos;
            Vector3 inertia = -anchorDelta * inertiaStrength;

            _prevAnchorPos = anchor;
            _prevTip = _simTip;
            _simTip += velocity + gravity + inertia;

            float t = (Time.time * windSpeed) - wavePhase;

            float windAmt = Mathf.Sin(t) * windStrength * _linkLength * 0.017f;
            Vector3 windDir = transform.parent.forward; 
            _simTip += windDir * windAmt * dt * 60f * dt;

            Vector3 toTip = _simTip - anchor;
            _simTip = toTip.magnitude > 0.0001f
                ? anchor + toTip * (_linkLength / toTip.magnitude)
                : anchor + transform.TransformDirection(_localChainAxis) * _linkLength;

            Vector3 parentRight = transform.parent.right;
            Vector3 relToAnchor = _simTip - anchor;

            float twistAmount = Vector3.Dot(relToAnchor, parentRight);

            relToAnchor -= parentRight * (twistAmount * twistConstraint);

            if (relToAnchor.magnitude > 0.0001f)
                _simTip = anchor + relToAnchor.normalized * _linkLength;

            Vector3 bindDir = transform.TransformDirection(_localChainAxis);
            Vector3 simDir = (_simTip - anchor).normalized;

            if (bindDir.sqrMagnitude > 0.0001f && simDir.sqrMagnitude > 0.0001f)
            {
                Quaternion rot = Quaternion.FromToRotation(bindDir, simDir);
                transform.rotation = rot * transform.rotation;
            }
        }
        
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_simTip, 0.012f);
            Gizmos.DrawLine(transform.position, _simTip);

            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position,
                transform.TransformDirection(_localChainAxis) * _linkLength);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.parent.right * 0.1f);
        }
#endif
    }
}