using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class RaycastInteractor : MonoBehaviour
    {
        public float interactDistance = 1.7f;
        public KeyCode interactKey = KeyCode.E;
        public Transform rayOrigin;
        public Text interactTip;

        private IInteractable currentTarget;

        void Start()
        {
            interactTip.text = "";
            if (rayOrigin == null)
            {
                rayOrigin = transform;
            }

            InvokeRepeating(nameof(CheckForInteractable), 0f, 0.1f);
        }

        void Update()
        {
            if (currentTarget != null && Input.GetKeyDown(interactKey))
            {
                currentTarget.Interact();
                Debug.Log("Interacted with " + currentTarget);
            }
        }

        void CheckForInteractable()
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactDistance))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    if (currentTarget != interactable)
                    {
                        currentTarget = interactable;
                        interactTip.text = $"Press {interactKey} to interact with {hit.collider.name}";
                    }
                    return;
                }
            }

            if (currentTarget != null)
            {
                currentTarget = null;
                interactTip.text = "";
            }
        }
    }
}
