using UnityEngine;
using Assets.Scripts.Localization;
using Assets.Scripts;

/// <summary>
/// Mode of ThoughtTrigger operation
/// </summary>
public enum ThoughtTriggerMode
{
    [Tooltip("Activates when player enters trigger zone (default)")]
    TriggerZone = 0,

    [Tooltip("Activates when player interacts with object (pickup/activate)")]
    OnInteraction = 1
}

/// <summary>
/// Trigger for showing character thoughts when player enters the zone
/// [MODIFIED] Updated API calls to remove Obsolete warnings
/// </summary>
[RequireComponent(typeof(Collider))]
public class ThoughtTrigger : MonoBehaviour
{
    [Header("Trigger Mode")]
    [Tooltip("How this thought trigger activates")]
    public ThoughtTriggerMode triggerMode = ThoughtTriggerMode.TriggerZone;

    [Header("Thought Settings - English")]
    [Tooltip("The text of the thought that will be displayed (English)")]
    [TextArea(3, 10)]
    public string thoughtText = "test raz raz raz";

    [Header("Thought Settings - Ukrainian")]
    [Tooltip("The text of the thought that will be displayed (Ukrainian)")]
    [TextArea(3, 10)]
    public string thoughtTextUkr = "";

    [Header("Display Settings")]
    [Tooltip("Duration of thought display (seconds). 0 = infinite")]
    public float displayDuration = 3f;

    [Header("Trigger Settings")]
    [Tooltip("Player tag")]
    public string playerTag = "Player";

    [Tooltip("Show the thought only once")]
    public bool showOnlyOnce = true;

    [Tooltip("Delay before displaying a thought (seconds)")]
    public float displayDelay = 0f;

    [Header("Debug")]
    [Tooltip("Show gizmo in editor")]
    public bool showGizmo = true;

    private bool hasBeenTriggered = false;
    private Collider triggerCollider;

    private void Awake()
    {
        if (triggerMode == ThoughtTriggerMode.TriggerZone)
        {
            triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
            else
            {
                Debug.LogWarning($"[ThoughtTrigger] TriggerZone mode requires Collider component on '{gameObject.name}'!");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerMode != ThoughtTriggerMode.TriggerZone)
        {
            return;
        }

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (SaveManager.IsLoadingGame)
        {
            return;
        }

        if (showOnlyOnce && hasBeenTriggered)
        {
            return;
        }

        hasBeenTriggered = true;

        if (displayDelay > 0)
        {
            Invoke(nameof(ShowThought), displayDelay);
        }
        else
        {
            ShowThought();
        }
    }

    private void ShowThought()
    {
        ThoughtUI thoughtUI = Object.FindFirstObjectByType<ThoughtUI>();
        
        if (thoughtUI != null)
        {
            string localizedText = GetLocalizedThoughtText();
            thoughtUI.ShowThought(localizedText, displayDuration);
        }
        else
        {
            Debug.LogWarning($"ThoughtUI not found in the scene! Unable to display the thought: {GetLocalizedThoughtText()}");
        }
    }

    /// <summary>
    /// Get localized thought text based on current language settings
    /// </summary>
    private string GetLocalizedThoughtText()
    {
        if (Assets.Scripts.SettingsManager.Instance != null)
        {
            Language currentLanguage = Assets.Scripts.SettingsManager.Instance.GetCurrentLanguage();

            switch (currentLanguage)
            {
                case Language.Ukrainian:
                    return string.IsNullOrEmpty(thoughtTextUkr) ? thoughtText : thoughtTextUkr;
                case Language.English:
                default:
                    return thoughtText;
            }
        }

        return thoughtText;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo)
            return;

        Collider col = GetComponent<Collider>();
        if (col == null)
            return;

        Gizmos.color = hasBeenTriggered ? new Color(0.5f, 0.5f, 0.5f, 0.3f) : new Color(0f, 1f, 1f, 0.3f);

        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
        }
    }

    /// <summary>
    /// Trigger thought from external source (NewspaperPickup, ActivatableObject)
    /// </summary>
    public void TriggerFromInteraction()
    {
        if (triggerMode != ThoughtTriggerMode.OnInteraction)
        {
            Debug.LogWarning("[ThoughtTrigger] TriggerFromInteraction called but mode is not OnInteraction!");
            return;
        }

        if (showOnlyOnce && hasBeenTriggered)
        {
            return;
        }

        hasBeenTriggered = true;

        if (displayDelay > 0)
        {
            Invoke(nameof(ShowThought), displayDelay);
        }
        else
        {
            ShowThought();
        }
    }

    /// <summary>
    /// Resets the trigger so that the thought can be shown again
    /// </summary>
    public void ResetTrigger()
    {
        hasBeenTriggered = false;
    }
}