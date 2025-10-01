using UnityEngine;

namespace Assets.Scripts
{
    public class CollectItem : MonoBehaviour, IInteractable
    {
        public void Interact()
        {
            Debug.Log("collected");
            Destroy(gameObject);
        }
    }
}
