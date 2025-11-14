using UnityEngine;

namespace Assets.Scripts

{

    public interface IInteractable
    {
        string GetInteractText();

        void OnInteract(GameObject actor);

        void Interact(GameObject actor);
    }
}