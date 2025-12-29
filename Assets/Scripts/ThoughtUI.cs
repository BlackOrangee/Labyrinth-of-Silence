using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// UI manager for displaying character thoughts on canvas
/// </summary>
public class ThoughtUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Parent GameObject for the thought panel")]
    public GameObject thoughtPanel;

    [Tooltip("Text component for displaying thought text")]
    public Text thoughtText;

    [Header("Animation Settings")]
    [Tooltip("Duration of fade in animation (seconds)")]
    public float fadeInDuration = 0.5f;

    [Tooltip("Duration of fade out animation (seconds)")]
    public float fadeOutDuration = 0.5f;

    [Tooltip("Use fade animation")]
    public bool useFadeAnimation = true;

    [Header("Display Settings")]
    [Tooltip("Maximum text length (warning if exceeded)")]
    public int maxTextLength = 200;

    private CanvasGroup canvasGroup;
    private Coroutine currentThoughtCoroutine;
    private bool isShowing = false;

    private void Awake()
    {
        if (thoughtPanel != null)
        {
            canvasGroup = thoughtPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null && useFadeAnimation)
            {
                canvasGroup = thoughtPanel.AddComponent<CanvasGroup>();
            }
        }

        HideImmediate();
    }

    /// <summary>
    /// Displays the thought on the screen
    /// </summary>
    /// <param name="text">Text of the thought</param>
    /// <param name="duration">Display duration (0 = infinite)</param>
    public void ShowThought(string text, float duration = 3f)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("An attempt to show an empty thought");
            return;
        }

        if (text.Length > maxTextLength)
        {
            Debug.LogWarning($"The text of the thought is too long. ({text.Length}). It is recommended not to exceed {maxTextLength}.");
        }

        if (currentThoughtCoroutine != null)
        {
            StopCoroutine(currentThoughtCoroutine);
        }

        currentThoughtCoroutine = StartCoroutine(ShowThoughtCoroutine(text, duration));
    }

    /// <summary>
    /// Hides a thought with animation
    /// </summary>
    public void HideThought()
    {
        if (currentThoughtCoroutine != null)
        {
            StopCoroutine(currentThoughtCoroutine);
        }

        currentThoughtCoroutine = StartCoroutine(HideThoughtCoroutine());
    }

    private IEnumerator ShowThoughtCoroutine(string text, float duration)
    {
        isShowing = true;

        if (thoughtText != null)
        {
            thoughtText.text = text;
        }

        if (thoughtPanel != null)
        {
            thoughtPanel.SetActive(true);
        }

        if (useFadeAnimation && canvasGroup != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        if (duration > 0)
        {
            yield return new WaitForSeconds(duration);

            yield return StartCoroutine(HideThoughtCoroutine());
        }

        currentThoughtCoroutine = null;
    }

    private IEnumerator HideThoughtCoroutine()
    {
        if (useFadeAnimation && canvasGroup != null)
        {
            float elapsedTime = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }

        if (thoughtPanel != null)
        {
            thoughtPanel.SetActive(false);
        }

        isShowing = false;
        currentThoughtCoroutine = null;
    }

    /// <summary>
    /// Instantly hides the panel without animation
    /// </summary>
    private void HideImmediate()
    {
        if (thoughtPanel != null)
        {
            thoughtPanel.SetActive(false);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        isShowing = false;
    }

    /// <summary>
    /// Checks whether the thought is currently displayed
    /// </summary>
    public bool IsShowing()
    {
        return isShowing;
    }

    private void OnValidate()
    {
        if (thoughtPanel == null)
        {
            Debug.LogWarning("ThoughtUI: thoughtPanel is not assigned! Assign the UI GameObject in the inspector.");
        }

        if (thoughtText == null)
        {
            Debug.LogWarning("ThoughtUI: thoughtText is not assigned! Assign the TextMeshProUGUI component in the inspector.");
        }
    }
}
