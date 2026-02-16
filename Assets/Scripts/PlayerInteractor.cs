using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Scripts.Localization;

namespace Assets.Scripts
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Ray Settings")]
        public float interactDistance = 3f;
        public float rayRadius = 0.3f;
        public LayerMask interactLayers = ~0;
        public Camera cam;

        [Header("Visual Feedback Timing")]
        [Tooltip("1. Crossfade duration (normal → pressed transition)")]
        [Range(0.05f, 1f)]
        public float crossfadeDuration = 0.2f;

        [Tooltip("2. Pressed display duration (how long to show pressed state AFTER crossfade)")]
        [Range(0.1f, 2f)]
        public float pressedDisplayDuration = 0.3f;

        [Tooltip("3. Fade out duration (how long popup takes to disappear)")]
        [Range(0.1f, 2f)]
        public float fadeOutDuration = 0.3f;

        [Tooltip("Wait for fade out to complete before executing interaction")]
        public bool waitForFadeOut = true;

        private PlayerHideController hideController;
        private IInteractable currentInteractable = null;

        private GameObject lastHitGO = null;
        private string currentPopupID = null;
        private Coroutine interactionCoroutine = null; 

        void Start()
        {
            if (cam == null) cam = Camera.main;
            hideController = GetComponent<PlayerHideController>();
        }

        public void ForceReset()
        {
            if (interactionCoroutine != null)
            {
                StopCoroutine(interactionCoroutine);
                interactionCoroutine = null;
            }

            currentInteractable = null;
            lastHitGO = null;
            currentPopupID = null;

            if (Localization.PopupManager.Instance != null)
            {
                Localization.PopupManager.Instance.HidePopup();
            }
        }

        void Update()
        {
            if (hideController != null && hideController.IsHiding)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.LogWarning("[PlayerInteractor] E pressed but player is hiding!");
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log($"[PlayerInteractor] E pressed! currentInteractable: {currentInteractable != null}, lastHitGO: {lastHitGO?.name}");

                if (currentInteractable != null)
                {
                    ExecuteCurrent();
                }
                else
                {
                    Debug.LogWarning("[PlayerInteractor] E pressed but currentInteractable is null!");
                }
            }

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.SphereCast(ray, rayRadius, out hit, interactDistance, interactLayers))
            {
                GameObject hitGO = hit.collider.gameObject;
                IInteractable interactable = hitGO.GetComponent<IInteractable>();

                Debug.Log($"[PlayerInteractor] 🎯 Raycast hit: {hitGO.name}, hasInteractable: {interactable != null}");

                if (interactable != null)
                {
                    string popupID = interactable.GetPopupID();

                    if (hitGO == lastHitGO && currentInteractable != null && currentPopupID == popupID)
                    {
                        return;
                    }

                    if (string.IsNullOrEmpty(popupID))
                    {
                        Debug.LogWarning($"[PlayerInteractor] PopupID is empty for {hitGO.name}! Clearing.");
                        ClearCurrent();
                    }
                    else
                    {
                        currentInteractable = interactable;
                        lastHitGO = hitGO;
                        currentPopupID = popupID;

                        Debug.Log($"[PlayerInteractor] ✅ Set currentInteractable: {hitGO.name}, popupID: {popupID}");

                        if (Localization.PopupManager.Instance != null)
                        {
                            Localization.PopupManager.Instance.ShowPopup(popupID, false, 0f);
                        }
                    }
                }
                else
                {
                    ClearCurrent();
                }
            }
            else
            {
                ClearCurrent();
            }
        }

        private void ClearCurrent()
        {
            if (currentInteractable != null || lastHitGO != null)
            {
                if (interactionCoroutine != null)
                {
                    StopCoroutine(interactionCoroutine);
                    interactionCoroutine = null;
                }

                currentInteractable = null;
                lastHitGO = null;
                currentPopupID = null;

                if (Localization.PopupManager.Instance != null)
                {
                    Localization.PopupManager.Instance.HidePopup();
                }
            }
        }

        private void ExecuteCurrent()
        {
            if (currentInteractable != null && lastHitGO != null)
            {
                var target = currentInteractable;
                var targetGO = lastHitGO;

                if (Localization.PopupManager.Instance != null)
                {
                    Localization.PopupManager.Instance.SetPressedState();
                    Debug.Log($"[PlayerInteractor] 👆 Switched to PRESSED state for {targetGO.name}");
                }

                if (interactionCoroutine != null)
                {
                    StopCoroutine(interactionCoroutine);
                }
                interactionCoroutine = StartCoroutine(DelayedInteractionRoutine(target, targetGO));
            }
            else
            {
                Debug.LogWarning($"[PlayerInteractor] ExecuteCurrent failed - currentInteractable is null: {currentInteractable == null}, lastHitGO is null: {lastHitGO == null}");
            }
        }

        private System.Collections.IEnumerator DelayedInteractionRoutine(IInteractable target, GameObject targetGO)
        {
            if (Localization.PopupManager.Instance != null)
            {
                Localization.PopupManager.Instance.transitionDuration = crossfadeDuration;
                Localization.PopupManager.Instance.fadeDuration = fadeOutDuration;
            }

            yield return new WaitForSeconds(crossfadeDuration);
            yield return new WaitForSeconds(pressedDisplayDuration);
            if (Localization.PopupManager.Instance != null)
            {
                Localization.PopupManager.Instance.HidePopup();
            }

            if (waitForFadeOut)
            {
                yield return new WaitForSeconds(fadeOutDuration);
            }

            target.Interact(this.gameObject);

            currentInteractable = null;
            lastHitGO = null;
            currentPopupID = null;

            interactionCoroutine = null;
        }

        private void OnDrawGizmosSelected()
        {
            if (cam != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(cam.transform.position + cam.transform.forward * interactDistance, rayRadius);
            }
        }
    }
}