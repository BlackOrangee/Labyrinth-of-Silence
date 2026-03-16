using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlythroughWaypoint))]
public class FlythroughWaypointEditor : Editor
{
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

        bool isLast = idx == total - 1;

        if (!isLast)
        {
            EditorGUILayout.Space(2);
            Transform next = fly.waypoints[idx + 1];
            EditorGUILayout.HelpBox(
                $"Next → {(next != null ? next.name : "null")}",
                MessageType.None);
        }
        else
        {
            // ── button only on the last waypoint ────────────────
            EditorGUILayout.Space(6);
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
            wpGo.transform.position = last.position + last.forward * 5f;
            wpGo.transform.rotation = last.rotation;
        }

        // place in the same container as the previous waypoint
        wpGo.transform.SetParent(last != null ? last.parent : null);

        // marker
        var marker = wpGo.AddComponent<FlythroughWaypoint>();
        marker.owner = fly;

        // extend array
        var newArr = new Transform[oldLen + 1];
        fly.waypoints.CopyTo(newArr, 0);
        newArr[oldLen] = wpGo.transform;
        fly.waypoints  = newArr;

        EditorUtility.SetDirty(fly);
        Selection.activeGameObject = wpGo;
    }
}