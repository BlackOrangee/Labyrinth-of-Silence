using UnityEngine;
using TMPro;
namespace Assets.Scripts
{
    // Створюємо список ідентифікаторів для всіх завдань у грі
    public enum GameObjective
    {
        None,               // Пусто (немає завдання)
        ExitLab,            // Вийдіть з лабораторії
        ExploreBuilding,    // Дослідіть будівлю
        FindBehindDoor,     // Дізнайтеся що знаходиться за дверима
        FindPower,          // Знайти джерело живлення
        FindWorkaround,     // Знайдіть обхідний шлях
        GetToCity           // Дістаньтесь до міста
    }
    public class MainObjectiveManager : MonoBehaviour
    {
        public static MainObjectiveManager Instance;

        [Header("UI References")]
        public GameObject panelRoot;       
        public TextMeshProUGUI objectiveText;

        [Header("Debug")]
        [Tooltip("Вибери тут завдання і натисни правою кнопкою на скрипт -> Set Objective From Inspector, щоб протестувати")]
        public GameObjective currentDebugObjective; 
        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        void Start()
        {
            SetObjective(GameObjective.ExitLab);
        }
        public void SetObjective(GameObjective objectiveType)
        {
            string textToShow = GetTextForObjective(objectiveType);
            UpdateUI(textToShow);
        }
        private string GetTextForObjective(GameObjective type)
        {
            switch (type)
            {
                case GameObjective.ExitLab:
                    return "Exit the lab";
                
                case GameObjective.ExploreBuilding:
                    return "Explore the building";
                
                case GameObjective.FindBehindDoor:
                    return "Find out what's behind the door";
                
                case GameObjective.FindPower:
                    return "Find the power supply";
                
                case GameObjective.FindWorkaround:
                    return "Find a workaround";
                
                case GameObjective.GetToCity:
                    return "Get to the city";

                case GameObjective.None:
                default:
                    return "";
            }
        }
        private void UpdateUI(string newText)
        {
            if (panelRoot != null)
            {
                if (string.IsNullOrEmpty(newText))
                {
                    panelRoot.SetActive(false);
                }
                else
                {
                    panelRoot.SetActive(true);
                    if (objectiveText != null)
                    {
                        objectiveText.text = newText;
                    }
                }
            }
        }

        [ContextMenu("Apply Debug Objective")]
        public void ApplyDebugObjective()
        {
            SetObjective(currentDebugObjective);
        }
    }
}