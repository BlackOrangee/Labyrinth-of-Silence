using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(Collider))]
    public class FuelCanister : MonoBehaviour, IInteractable
    {
        [Header("Settings")]
        [Tooltip("Поточний запас палива в цій каністрі (може бути більше 100)")]
        [SerializeField] private float canisterCapacity = 200f;
        
        [SerializeField] private bool destroyOnEmpty = true;

        [Header("Audio")]
        [Tooltip("Звук заправки")]
        [SerializeField] private AudioClip refuelSound;
        
        [Range(0f, 1f)]
        [Tooltip("Гучність звуку заправки")]
        [SerializeField] private float soundVolume = 0.7f;


        public void Interact(GameObject actor)
        {
            LampController lamp = actor.GetComponentInChildren<LampController>();
            if (lamp == null) lamp = FindFirstObjectByType<LampController>();

            if (lamp != null)
            {
                float currentLampFuel = lamp.GetCurrentFuel();
                float maxLampFuel = lamp.GetMaxFuel();

                float spaceInLamp = maxLampFuel - currentLampFuel;

                if (spaceInLamp <= 1f) 
                {
                    Debug.Log("FuelCanister: Lamp is already full.");
                    return; 
                }

                float amountToTransfer = Mathf.Min(spaceInLamp, canisterCapacity);

                lamp.Refuel(amountToTransfer);

                canisterCapacity -= amountToTransfer;

                Debug.Log($"Refueled: {amountToTransfer}. Remaining in canister: {canisterCapacity}");

                if (refuelSound != null)
                {
                    AudioSource.PlayClipAtPoint(refuelSound, transform.position, soundVolume);
                }

                if (destroyOnEmpty && canisterCapacity <= 0.1f)
                {
                    if (GetComponent<SaveableObject>() != null)
                        gameObject.SetActive(false);
                    else
                        Destroy(gameObject);
                }

            }
            else
            {
                Debug.LogWarning("FuelCanister: LampController not found!");
            }
        }

        public void OnInteract(GameObject actor) => Interact(actor);

        public string GetPopupID()
        {
            return canisterCapacity > 0.1f ? "upKerosene" : "";
        }
    }
}