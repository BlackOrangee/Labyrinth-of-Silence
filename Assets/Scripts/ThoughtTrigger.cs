using UnityEngine;

/// <summary>
/// Trigger for showing character thoughts when player enters the zone
/// </summary>
[RequireComponent(typeof(Collider))]
public class ThoughtTrigger : MonoBehaviour
{
    [Header("Thought Settings")]
    [Tooltip("The text of the thought that will be displayed")]
    [TextArea(3, 10)]
    public string thoughtText = "test raz raz raz";

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
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
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
        ThoughtUI thoughtUI = FindObjectOfType<ThoughtUI>();
        if (thoughtUI != null)
        {
            thoughtUI.ShowThought(thoughtText, displayDuration);
        }
        else
        {
            Debug.LogWarning($"ThoughtUI not found in the scene! Unable to display the thought: {thoughtText}");
        }
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
    /// Resets the trigger so that the thought can be shown again
    /// </summary>
    public void ResetTrigger()
    {
        hasBeenTriggered = false;
    }
}
