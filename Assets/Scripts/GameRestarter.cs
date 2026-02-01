using UnityEngine;
using UnityEngine.SceneManagement; // Потрібно для роботи зі сценами

namespace Assets.Scripts
{
    public class GameRestarter : MonoBehaviour
    {
        // Цей метод ми прив'яжемо до кнопки
        public void RestartGame()
        {
            Debug.Log("Restarting Game...");
            
            // Забезпечуємо, що час йде (якщо гра була на паузі)
            Time.timeScale = 1f;

            // Перезавантажуємо поточну сцену (рівень почнеться заново)
            // Примітка: Щоб почати З ТОГО Ж МІСЦЯ, потрібна система збережень.
            // Поки що ми робимо повний рестарт рівня, як в більшості хоррорів.
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}