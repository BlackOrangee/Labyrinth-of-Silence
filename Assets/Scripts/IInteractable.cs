using UnityEngine;

namespace Assets.Scripts

{
    public interface IInteractable
    {
        void OnInteract(GameObject actor);

        void Interact(GameObject actor);

        /// <summary>
        /// Get popup hint ID for this interactable object
        /// IDs: "open", "turnOn", "upLamp", "upDocument", "upKerosene", "hide", "out", "lampON", "lampOFF"
        /// Return null or empty string to disable popup hint
        /// </summary>
        string GetPopupID();
    }
}