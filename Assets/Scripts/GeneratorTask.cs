using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public class GeneratorTask : MonoBehaviour, IInteractable 
    {
        [Header("State")]
        private bool isRepaired = false;
        private bool hasFuel = false;
        private bool hasTriedToStart = false;

        [Header("Debug")]
        [Tooltip("Постав галочку, щоб тестувати без пошуку каністри")]
        [SerializeField] private bool debugHasFuel = false;

        [Header("References")]
        [SerializeField] private SkillCheckSystem skillCheckSystem;
        [SerializeField] private MonsterB_AI monsterAI; 
        
        [Tooltip("Система індикаторів на генераторі (Red/Orange/Green)")]
        [SerializeField] private GeneratorIndicators indicators;

        [Header("Items Needed")]
        [Tooltip("ID предмета в SimpleInventory")]
        [SerializeField] private string fuelItemName = "FuelCanister";

        [Header("Effects")]
        [SerializeField] private AudioSource generatorAudio;
        [SerializeField] private AudioClip workingSound;
        [SerializeField] private AudioClip failAlertSound; 

        [SerializeField] private AudioClip refuelSound; 

        private void Start()
        {
            if (skillCheckSystem == null) skillCheckSystem = FindFirstObjectByType<SkillCheckSystem>();
            if (monsterAI == null) monsterAI = FindFirstObjectByType<MonsterB_AI>();
            
            if (indicators != null)
            {
                indicators.SetState(GeneratorIndicators.IndicatorState.Idle);
            }
        }
        public string GetPopupID()
        {
            if (isRepaired)
            {
                return "generatorIsOn";
            }

            if (hasFuel || debugHasFuel)
            {
                return "turnOn";
            }

            if (IsPlayerHoldingFuel())
            {
                return "refuel";
            }

            if (hasTriedToStart)
            {
                return "needFuel";
            }
            return "turnOn";
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
            hasTriedToStart = true;

            if (!hasFuel && !debugHasFuel)
            {
                SimpleInventory inventory = actor.GetComponentInChildren<SimpleInventory>();
                
                if (inventory != null && inventory.GetItems().Contains(fuelItemName))
                {
                    hasFuel = true; 
                    Debug.Log("Generator: Refueling complete!");

                    if (generatorAudio && refuelSound) generatorAudio.PlayOneShot(refuelSound);

                    if (indicators != null)
                    {
                        indicators.SetState(GeneratorIndicators.IndicatorState.Waiting);
                    }
                }
                else
                {
                    Debug.Log("Generator: You need fuel!");
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
        private void OnSuccess()
        {
            isRepaired = true;
            Debug.Log("Generator: REPAIRED! Power Restored.");
            skillCheckSystem.OnSeriesComplete = null;
            skillCheckSystem.OnFail = null;

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
        public void OnInteract(GameObject actor) => Interact(actor);
    }
}