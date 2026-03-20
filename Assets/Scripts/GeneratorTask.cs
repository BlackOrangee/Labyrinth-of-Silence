using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
        private MonsterAI monsterAI;
        
        [Tooltip("Система індикаторів на генераторі (Red/Orange/Green)")]
        [SerializeField] private GeneratorIndicators indicators;

        [Header("Items Needed")]
        [Tooltip("ID предмета в SimpleInventory")]
        [SerializeField] private string fuelItemName = "FuelCanister";

        [Header("Skill Check Cancel")]
        [SerializeField] private float cancelDistance = 2.5f;

        private bool isSkillCheckActive = false;

        [Header("Effects")]
        [SerializeField] private AudioSource generatorAudio;
        [SerializeField] private AudioClip workingSound;
        [SerializeField] private AudioClip failAlertSound; 

        [SerializeField] private AudioClip refuelSound; 

        private void Start()
        {
            if (skillCheckSystem == null) skillCheckSystem = FindFirstObjectByType<SkillCheckSystem>();
            if (monsterAI == null) monsterAI = FindFirstObjectByType<MonsterAI>();
            
            if (indicators != null)
            {
                indicators.SetState(GeneratorIndicators.IndicatorState.Idle);
            }
        }

        public string GetInteractText()
        {
            if (isRepaired) return "Generator is ON";

            if (hasFuel || debugHasFuel) return "Press [E] to Start Generator";

            if (IsPlayerHoldingFuel()) 
            {
                return "Press [E] to Refuel";
            }
            return "Need Fuel (Find Canister)";
        }

        private bool IsPlayerHoldingFuel()
        {
            SimpleInventory inventory = FindFirstObjectByType<SimpleInventory>();
            if (inventory != null)
            {
                return inventory.GetItems().Contains(fuelItemName);
            }
            return false;
        }

        public void Interact(GameObject actor)
        {
            if (isRepaired) return;

            if (!hasFuel && !debugHasFuel)
            {
                SimpleInventory inventory = actor.GetComponentInChildren<SimpleInventory>();
                
                if (inventory != null && inventory.GetItems().Contains(fuelItemName))
                {
                    hasFuel = true;
                    inventory.RemoveItem(fuelItemName);
                    Debug.Log("Generator: Refueling complete!");

                    if (TaskManager.Instance != null)
                    {
                        TaskManager.Instance.RemoveTask("find_canister");
                        TaskManager.Instance.AddTask("start_gen", "Start the generator", "Завести генератор");
                    }

                    if (generatorAudio && refuelSound) generatorAudio.PlayOneShot(refuelSound);

                    if (indicators != null)
                    {
                        indicators.SetState(GeneratorIndicators.IndicatorState.Waiting);
                    }
                }
                else
                {
                    Debug.Log("Generator: You need fuel!");
                    
                    if (TaskManager.Instance != null)
                    {
                        TaskManager.Instance.AddTask("find_canister", "Find a generator canister", "Знайти каністру до генератора");
                    }
                }
                return;
            }

            if (hasFuel || debugHasFuel)
            {
                Debug.Log("Generator: Starting Skill Check...");
                isSkillCheckActive = true;
                skillCheckSystem.OnSeriesComplete = OnSuccess;
                skillCheckSystem.OnFail = OnFail;
                skillCheckSystem.StartRepair();
                StartCoroutine(MonitorPlayerDistance(actor));
            }
        }

        private IEnumerator MonitorPlayerDistance(GameObject player)
        {
            while (isSkillCheckActive)
            {
                if (player == null || Vector3.Distance(transform.position, player.transform.position) > cancelDistance)
                {
                    isSkillCheckActive = false;
                    skillCheckSystem.ForceStop();
                    skillCheckSystem.OnSeriesComplete = null;
                    skillCheckSystem.OnFail = null;
                    Debug.Log("Generator: Skill check cancelled — player moved away.");
                    yield break;
                }
                yield return null;
            }
        }

        private void OnSuccess()
        {
            isRepaired = true;
            isSkillCheckActive = false;
            Debug.Log("Generator: REPAIRED! Power Restored.");
            
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.RemoveTask("start_gen");
                TaskManager.Instance.RemoveTask("find_canister");
            }
            
            if (CodePuzzleManager.Instance != null)
            {
                CodePuzzleManager.Instance.StartCodeTask(); 
            }

            skillCheckSystem.OnSeriesComplete = null;
            skillCheckSystem.OnFail = null;

            GetComponent<ActivatableObject>()?.ActivateManually();

            if (indicators != null)
            {
                indicators.SetState(GeneratorIndicators.IndicatorState.Success);
            }

            if (generatorAudio && workingSound)
            {
                generatorAudio.clip = workingSound;
                generatorAudio.loop = true;
                generatorAudio.Play();
            }
        }

        private void OnFail()
        {
            isSkillCheckActive = false;
            Debug.Log("Generator Failed! Monster Alerted!");
            
            if (indicators != null)
            {
                indicators.SetState(GeneratorIndicators.IndicatorState.Failed);
            }
            
            if (generatorAudio && failAlertSound) 
            {
                generatorAudio.PlayOneShot(failAlertSound);
            }

            if (monsterAI != null)
            {
                monsterAI.TriggerChaseToLocation(transform.position);
            }
        }

        public void RestoreState(bool repaired, bool fuel)
        {
            isRepaired = repaired;
            hasFuel = fuel;

            if (isRepaired)
            {
                if (indicators != null)
                    indicators.SetState(GeneratorIndicators.IndicatorState.Success);

                if (generatorAudio && workingSound)
                {
                    generatorAudio.clip = workingSound;
                    generatorAudio.loop = true;
                    generatorAudio.Play();
                }
            }
            else if (hasFuel)
            {
                if (indicators != null)
                    indicators.SetState(GeneratorIndicators.IndicatorState.Waiting);
            }
        }

        public void OnInteract(GameObject actor) => Interact(actor);

        public string GetPopupID()
        {
            if (isRepaired) return "generatorIsOn";
            if (hasFuel || debugHasFuel || IsPlayerHoldingFuel()) return "refuel";
            return "needFuel";
        }
    }
}