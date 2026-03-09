using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraFlythrough))]
public class CameraFlythroughEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CameraFlythrough fly = (CameraFlythrough)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "How to set up a route:\n" +
            "• Click 'Add Waypoint' — an empty object will be created near the camera\n" +
            "• Move and rotate it to the desired position (rotation = where the camera looks)\n" +
            "• Want the camera sideways? Rotate the waypoint sideways\n\n" +
            "Play Mode:\n" +
            "  Space  — pause / resume\n" +
            "  R      — reset to start\n" +
            "  W      — accelerate\n" +
            "  S      — decelerate",
            MessageType.Info);

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Add Waypoint to End", GUILayout.Height(28)))
        {
            Undo.RecordObject(fly, "Add Flythrough Waypoint");

            int    oldLen = fly.waypoints?.Length ?? 0;
            string name   = $"Waypoint_{oldLen}";

            GameObject wp = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(wp, "Add Flythrough Waypoint");

            // position: +5m forward from last waypoint, or in front of the camera
            if (fly.waypoints != null && fly.waypoints.Length > 0)
            {
                Transform last = fly.waypoints[fly.waypoints.Length - 1];
                if (last != null)
                {
                    wp.transform.position = last.position + last.forward * 5f;
                    wp.transform.rotation = last.rotation;
                }
            }
            else
            {
                wp.transform.position = fly.transform.position + fly.transform.forward * 5f;
                wp.transform.rotation = fly.transform.rotation;
            }

            // IMPORTANT: waypoints go into a separate container, NOT as children of the camera
            wp.transform.SetParent(GetOrCreateContainer(fly));

            // marker component — provides "Add Next" button in the Inspector
            var marker = wp.AddComponent<FlythroughWaypoint>();
            marker.owner = fly;

            Transform[] newArr = new Transform[oldLen + 1];
            if (fly.waypoints != null)
                fly.waypoints.CopyTo(newArr, 0);
            newArr[oldLen] = wp.transform;
            fly.waypoints  = newArr;

            EditorUtility.SetDirty(fly);
            Selection.activeGameObject = wp;
        }
    }

    private static Transform GetOrCreateContainer(CameraFlythrough fly)
    {
        string     name     = fly.gameObject.name + "_Waypoints";
        GameObject existing = GameObject.Find(name);
        if (existing != null) return existing.transform;

        GameObject container = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(container, "Create Waypoints Container");
        container.transform.SetParent(null);
        return container.transform;
    }
}