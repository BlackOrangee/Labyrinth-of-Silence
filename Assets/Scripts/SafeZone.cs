using UnityEngine;

namespace Assets.Scripts
{
    public class SafeZone : MonoBehaviour
    {
        private EnemyAI enemy;

        void Start()
        {
            enemy = Object.FindFirstObjectByType<EnemyAI>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) 
            {
                if (enemy != null)
                {
                    enemy.isPlayerHidden = true;
                    Debug.Log("Гравець у безпеці (Safe Zone)!");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (enemy != null)
                {
                    enemy.isPlayerHidden = false;
                    Debug.Log("Гравець вийшов зі схованки.");
                }
            }
        }
    }
}