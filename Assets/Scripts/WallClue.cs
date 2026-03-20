using UnityEngine;
using Assets.Scripts;

public class WallClue : MonoBehaviour, IInteractable 
{
    [Header("Налаштування")]
    public int positionIndex;
    public string digitValue;

    [Header("Налаштування підказки UI")]
    [Tooltip("ID для системи")]
    public string popupHintID = "upDocument";

    private bool isFound = false;

    public void OnInteract(GameObject actor)
    {
        ProcessInteraction();
    }

    public void Interact(GameObject actor)
    {
        ProcessInteraction();
    }

    public string GetPopupID()
    {
        return popupHintID; 
    }

private void ProcessInteraction()
    {
        if (isFound) return;
        
        // NEW: Перевіряємо, чи генератор вже увімкнений (чи активна таска)
        if (CodePuzzleManager.Instance == null || !CodePuzzleManager.Instance.IsTaskActive())
        {
            // Якщо таски ще нема - гравець не може взаємодіяти з цифрою. Виходимо.
            return; 
        }
        
        // Якщо дійшли сюди - генератор працює! Беремо цифру.
        isFound = true;
        CodePuzzleManager.Instance.DiscoverDigit(positionIndex, digitValue);
        
        if (GetComponent<Collider>() != null)
        {
            GetComponent<Collider>().enabled = false;
        }
    }
}