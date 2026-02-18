using UnityEngine;

namespace Assets.Scripts
{
    public class GeneratorTask : MonoBehaviour, IInteractable 
    {
        [Header("State")]
        private bool isRepaired = false;

        private bool hasFuel = false; 

        [Header("Debug")]
        [Tooltip("Постав галочку, щоб тестувати без пошуку каністри")]
        [SerializeField] private bool debugHasFuel = false;

        [Header("References")]
        [SerializeField] private SkillCheckSystem skillCheckSystem;
        [SerializeField] private MonsterB_AI monsterAI; 

        [Header("Items Needed")]
        [Tooltip("ID предмета в SimpleInventory")]
        [SerializeField] private string fuelItemName = "FuelCanister";

        [Header("Effects")]
        [SerializeField] private AudioSource generatorAudio;
        [SerializeField] private AudioClip workingSound;
        [SerializeField] private AudioClip failAlertSound; 

        private void Start()
        {
            if (skillCheckSystem == null) skillCheckSystem = FindFirstObjectByType<SkillCheckSystem>();
            if (monsterAI == null) monsterAI = FindFirstObjectByType<MonsterB_AI>();
        }

        public string GetInteractText()
        {
            if (isRepaired) return "Generator is ON";

            if (!hasFuel && !debugHasFuel) return "Need Fuel (Find Canister)";
            
            return "Press [E] to Start Generator";
        }

        public void Interact(GameObject actor)
        {
            if (isRepaired) return;

            if (!hasFuel && !debugHasFuel)
            {
                SimpleInventory inventory = actor.GetComponentInChildren<SimpleInventory>();
                
                if (inventory != null)
                {
                    if (inventory.GetItems().Contains(fuelItemName))
                    {
                        hasFuel = true; 
                        Debug.Log("Generator: Fuel poured in!");
                    }
                    else
                    {
                        Debug.Log("Generator: Player has no fuel canister!");
                    }
                }
                return;
            }

            if (hasFuel || debugHasFuel)
            {
                Debug.Log("Generator: Starting Skill Check...");
                
                skillCheckSystem.OnSeriesComplete = OnSuccess;
                skillCheckSystem.OnFail = OnFail;
                skillCheckSystem.StartRepair(); 
            }
        }

        public string GetPopupID()
        {
            return "turnOn";
        }

        private void OnSuccess()
        {
            isRepaired = true;
            Debug.Log("Generator: REPAIRED! Power Restored.");
            
            skillCheckSystem.OnSeriesComplete = null;
            skillCheckSystem.OnFail = null;

            if (generatorAudio && workingSound)
            {
                generatorAudio.clip = workingSound;
                generatorAudio.loop = true;
                generatorAudio.Play();
            }
        }

        private void OnFail()
        {
            Debug.Log("Generator Failed! Monster Alerted!");
            
            if (generatorAudio && failAlertSound) 
            {
                generatorAudio.PlayOneShot(failAlertSound);
            }

            if (monsterAI != null)
            {
                monsterAI.TriggerChaseToLocation(transform.position);
            }
        }

        public void OnInteract(GameObject actor) => Interact(actor);
    }
}