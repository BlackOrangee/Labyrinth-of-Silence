using UnityEngine;

namespace Assets.Scripts
{
    public class SafeZone : MonoBehaviour
    {
        private EnemyAI enemy;

        void Start()
        {
            // [ВИПРАВЛЕНО] Використовуємо нову команду FindFirstObjectByType
            enemy = Object.FindFirstObjectByType<EnemyAI>();
        }

        // Коли гравець заходить у зону під столом
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) 
            {
                if (enemy != null)
                {
                    enemy.isPlayerHidden = true; // ВМИКАЄМО захист
                    Debug.Log("Гравець у безпеці (Safe Zone)!");
                }
            }
        }

        // Коли гравець виходить з-під стола
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (enemy != null)
                {
                    enemy.isPlayerHidden = false; // ВИМИКАЄМО захист
                    Debug.Log("Гравець вийшов зі схованки.");
                }
            }
        }
    }
}