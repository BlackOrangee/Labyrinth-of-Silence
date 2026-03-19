using UnityEngine;
using System.Collections;
using TMPro;

public class PsychosisWarningUI : MonoBehaviour
{
    [Header("UI Компоненти")]
    [Tooltip("Той самий Canvas Group, який ми додали на текст")]
    public CanvasGroup canvasGroup;
    
    [Header("Таймінги")]
    [Tooltip("Через скільки секунд ПІСЛЯ СТАРТУ РІВНЯ з'явиться напис? (Наприклад, 10)")]
    public float delayBeforeShow = 10f;
    
    [Tooltip("Скільки секунд напис буде мерехтіти на екрані? (Наприклад, 6)")]
    public float activeDuration = 6f;
    
    [Tooltip("Швидкість мерехтіння (більше = швидше)")]
    public float pulseSpeed = 3f;

    private void Start()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        StartCoroutine(WaitAndPulseRoutine());
    }

    private IEnumerator WaitAndPulseRoutine()
    {
        yield return new WaitForSeconds(delayBeforeShow);

        float time = 0;
        while (time < activeDuration)
        {
            time += Time.deltaTime;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.2f + Mathf.PingPong(Time.time * pulseSpeed, 0.8f);
            }
            yield return null;
        }

        if (canvasGroup != null)
        {
            while (canvasGroup.alpha > 0)
            {
                canvasGroup.alpha -= Time.deltaTime;
                yield return null;
            }
        }
    }
}