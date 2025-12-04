using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(Collider))]
    public class FuelCanister : MonoBehaviour, IInteractable
    {
        [Header("Settings")]
        [Tooltip("Загальний запас палива в цій каністрі (наприклад, 200)")]
        public float canisterCapacity = 200f;
        
        [Header("Audio")]
        [Tooltip("Звук заправки")]
        public AudioClip refuelSound;
        
        [Range(0f, 1f)]
        [Tooltip("Гучність звуку заправки")]
        public float soundVolume = 0.7f;

        private float _maxCapacity;

        private void Start()
        {
            _maxCapacity = canisterCapacity;
        }

        public string GetInteractText()
        {
            float percentage = 0f;
            if (_maxCapacity > 0)
            {
                percentage = (canisterCapacity / _maxCapacity) * 100f;
            }

            return $"Press to Refuel (Canister: {Mathf.Round(percentage)}%)";
        }

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
                    Debug.Log("Lamp is already full.");
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

                if (canisterCapacity <= 0.1f)
                {
                    Destroy(gameObject);
                }
            }
        }

        public void OnInteract(GameObject actor) => Interact(actor);
    }
}