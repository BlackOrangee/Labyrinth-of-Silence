using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlythroughWaypoint))]
public class FlythroughWaypointEditor : Editor
{
    private Vector3    _cachedPos;
    private Quaternion _cachedRot;

    void OnEnable()
    {
        Tools.hidden = true;   // we draw our own handles in world space

        var wp  = (FlythroughWaypoint)target;
        var fly = wp?.owner;
        if (fly == null || fly.waypoints == null) return;

        _cachedPos = wp.transform.position;
        _cachedRot = wp.transform.rotation;

        int idx = System.Array.IndexOf(fly.waypoints, wp.transform);
        if (idx >= 0)
            CameraFlythroughEditor.SnapCameraToWaypoint(fly, idx);
    }

    void OnDisable()
    {
        Tools.hidden = false;
    }

    void OnSceneGUI()
    {
        var wp  = (FlythroughWaypoint)target;
        var fly = wp?.owner;
        if (fly == null) return;

        // ── World-space position + local rotation handles ────────
        EditorGUI.BeginChangeCheck();

        // Position always along world XYZ — rotation of the waypoint is ignored
        Vector3    newPos = Handles.PositionHandle(wp.transform.position, Quaternion.identity);
        // Rotation stays local (that's intentional — you set where the camera looks)
        Quaternion newRot = Handles.RotationHandle(wp.transform.rotation, wp.transform.position);

        if (EditorGUI.EndChangeCheck())
        {
            // Z (roll) is always zero — camera should never bank sideways
            Vector3 euler = newRot.eulerAngles;
            euler.z = 0f;
            newRot = Quaternion.Euler(euler);

            Undo.RecordObject(wp.transform, "Move/Rotate Waypoint");
            wp.transform.position = newPos;
            wp.transform.rotation = newRot;
        }

        // ── Auto-preview when transform changes ──────────────────
        if (fly.waypoints == null) return;
        if (wp.transform.position == _cachedPos && wp.transform.rotation == _cachedRot) return;

        _cachedPos = wp.transform.position;
        _cachedRot = wp.transform.rotation;

        int idx = System.Array.IndexOf(fly.waypoints, wp.transform);
        if (idx >= 0)
            CameraFlythroughEditor.SnapCameraToWaypoint(fly, idx);
    }

    public override void OnInspectorGUI()
    {
        FlythroughWaypoint wp  = (FlythroughWaypoint)target;
        CameraFlythrough   fly = wp.owner;

        if (fly == null || fly.waypoints == null)
        {
            EditorGUILayout.HelpBox("No route owner assigned.", MessageType.Warning);
            return;
        }

        int idx   = System.Array.IndexOf(fly.waypoints, wp.transform);
        int total = fly.waypoints.Length;

        if (idx < 0)
        {
            EditorGUILayout.HelpBox("Waypoint not found in route.", MessageType.Warning);
            return;
        }

        DrawDefaultInspector();
        EditorGUILayout.Space(4);

        // ── info ────────────────────────────────────────────────
        EditorGUILayout.LabelField($"Route:  {fly.gameObject.name}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Waypoint {idx + 1} of {total}", EditorStyles.boldLabel);

        if (!fly.loop && idx < total - 1)
        {
            Transform next = fly.waypoints[idx + 1];
            EditorGUILayout.HelpBox(
                $"Next → {(next != null ? next.name : "null")}",
                MessageType.None);
        }

        EditorGUILayout.Space(6);

        // ── Preview button ───────────────────────────────────────
        GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
        if (GUILayout.Button("▶  Preview from here  (snaps Game view)", GUILayout.Height(28)))
            CameraFlythroughEditor.SnapCameraToWaypoint(fly, idx);
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(4);

        // ── Insert after ─────────────────────────────────────────
        bool isLast = idx == total - 1;

        if (!isLast || fly.loop)
        {
            GUI.backgroundColor = new Color(0.5f, 1f, 0.55f);
            if (GUILayout.Button("+ Insert waypoint after this", GUILayout.Height(26)))
            {
                int   nextIdx  = (idx + 1) % total;
                Vector3 midPos = (fly.waypoints[idx].position + fly.waypoints[nextIdx].position) * 0.5f;
                Quaternion rot = Quaternion.Slerp(fly.waypoints[idx].rotation,
                                                  fly.waypoints[nextIdx].rotation, 0.5f);
                CameraFlythroughEditor.InsertWaypointAfter(fly, idx, midPos, rot);
            }
            GUI.backgroundColor = Color.white;
        }

        if (isLast && !fly.loop)
        {
            EditorGUILayout.Space(2);
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("+ Add Next Waypoint", GUILayout.Height(30)))
                AddNextWaypoint(fly);
            GUI.backgroundColor = Color.white;
        }
    }

    private static void AddNextWaypoint(CameraFlythrough fly)
    {
        Undo.RecordObject(fly, "Add Flythrough Waypoint");

        int       oldLen = fly.waypoints.Length;
        Transform last   = fly.waypoints[oldLen - 1];

        GameObject wpGo = new GameObject($"Waypoint_{oldLen}");
        Undo.RegisterCreatedObjectUndo(wpGo, "Add Flythrough Waypoint");

        if (last != null)
        {
            wpGo.transform.position = last.position + CameraFlythroughEditor.FlatForward(last) * 5f;
            wpGo.transform.rotation = last.rotation;
        }

        wpGo.transform.SetParent(last != null ? last.parent : null);

        var marker = wpGo.AddComponent<FlythroughWaypoint>();
        marker.owner = fly;

        var newArr = new Transform[oldLen + 1];
        fly.waypoints.CopyTo(newArr, 0);
        newArr[oldLen] = wpGo.transform;
        fly.waypoints  = newArr;

        EditorUtility.SetDirty(fly);
        Selection.activeGameObject = wpGo;
    }
}