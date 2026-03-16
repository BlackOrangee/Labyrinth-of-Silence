using UnityEngine;

/// <summary>
/// Marker component for a CameraFlythrough waypoint.
/// Added automatically — no need to add it manually.
/// Shows an "Add Next Waypoint" button in the Inspector on the last waypoint.
/// </summary>
[AddComponentMenu("")] // hidden from Add Component menu
[DisallowMultipleComponent]
public class FlythroughWaypoint : MonoBehaviour
{
    [HideInInspector]
    public CameraFlythrough owner;

    [Tooltip("Двері що відкриються коли камера досягне цієї точки (опціонально)")]
    public GameObject doorToOpen;
}