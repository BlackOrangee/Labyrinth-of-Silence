using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Assets.Scripts
{
    /// <summary>
    /// Fixes ScrollView behavior: disables mouse drag and enables scroll wheel
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollViewFix : MonoBehaviour, IScrollHandler
    {
        [Header("Scroll Settings")] [Tooltip("Scroll sensitivity for mouse wheel")]
        public float scrollSensitivity = 3f;

        [Tooltip("Disable mouse drag")] public bool disableDrag = true;

        [Tooltip("Enable scroll wheel")] public bool enableScrollWheel = true;

        private ScrollRect scrollRect;

        void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();

            if (scrollRect != null)
            {
                if (disableDrag)
                {
                    scrollRect.horizontal = false;
                    scrollRect.vertical = false;
                }

                scrollRect.movementType = ScrollRect.MovementType.Clamped;

                scrollRect.inertia = false;
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!enableScrollWheel || scrollRect == null)
                return;

            float scrollDelta = eventData.scrollDelta.y;

            float newVerticalPosition =
                scrollRect.verticalNormalizedPosition + (scrollDelta * scrollSensitivity * 0.01f);

            newVerticalPosition = Mathf.Clamp01(newVerticalPosition);

            scrollRect.verticalNormalizedPosition = newVerticalPosition;
        }
    }
}