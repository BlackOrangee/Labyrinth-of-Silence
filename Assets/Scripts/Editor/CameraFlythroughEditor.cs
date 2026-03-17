using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraFlythrough))]
public class CameraFlythroughEditor : Editor
{
    // ── Canvas hide state (editor session only) ──────────────────
    private static bool         _canvasesHidden  = false;
    private static List<int>    _hiddenCanvasIds = new List<int>();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CameraFlythrough fly = (CameraFlythrough)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "How to set up a route:\n" +
            "• Click 'Add Waypoint' — an empty object will be created near the camera\n" +
            "• Move and rotate it to the desired position (rotation = where the camera looks)\n" +
            "• Click waypoint sphere in Scene view to select it and preview camera angle\n" +
            "• Click green '+' cube between waypoints to insert one in the middle\n\n" +
            "Play Mode:\n" +
            "  Space  — pause / resume\n" +
            "  R      — reset to start\n" +
            "  W      — accelerate\n" +
            "  S      — decelerate",
            MessageType.Info);

        EditorGUILayout.Space(4);

        // ── Canvas visibility toggle ─────────────────────────────
        GUI.backgroundColor = _canvasesHidden
            ? new Color(1f, 0.75f, 0.35f)
            : new Color(0.75f, 0.75f, 0.75f);
        string canvasLabel = _canvasesHidden
            ? "Show All Canvases (editor)"
            : "Hide All Canvases (editor)";
        if (GUILayout.Button(canvasLabel, GUILayout.Height(26)))
        {
            if (_canvasesHidden) ShowCanvases();
            else                 HideCanvases();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Add Waypoint to End", GUILayout.Height(28)))
        {
            Undo.RecordObject(fly, "Add Flythrough Waypoint");

            int    oldLen = fly.waypoints?.Length ?? 0;
            string name   = $"Waypoint_{oldLen}";

            GameObject wp = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(wp, "Add Flythrough Waypoint");

            if (fly.waypoints != null && fly.waypoints.Length > 0)
            {
                Transform last = fly.waypoints[fly.waypoints.Length - 1];
                if (last != null)
                {
                    wp.transform.position = last.position + FlatForward(last) * 5f;
                    wp.transform.rotation = last.rotation;
                }
            }
            else
            {
                wp.transform.position = fly.transform.position + FlatForward(fly.transform) * 5f;
                wp.transform.rotation = fly.transform.rotation;
            }

            wp.transform.SetParent(GetOrCreateContainer(fly));

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

    void OnSceneGUI()
    {
        CameraFlythrough fly = (CameraFlythrough)target;
        if (fly.waypoints == null || fly.waypoints.Length < 2) return;

        // ── Clickable waypoint spheres ───────────────────────────
        for (int i = 0; i < fly.waypoints.Length; i++)
        {
            Transform wp = fly.waypoints[i];
            if (wp == null) continue;

            bool isFirst = i == 0;
            bool isLast  = !fly.loop && i == fly.waypoints.Length - 1;
            Color col    = isFirst ? new Color(0.1f, 1f,  0.2f)
                         : isLast  ? new Color(1f,  0.2f, 0.2f)
                                   : new Color(1f,  0.8f, 0.1f);

            float size = HandleUtility.GetHandleSize(wp.position) * 0.25f;
            Handles.color = col;

            if (Handles.Button(wp.position, Quaternion.identity, size, size * 1.2f, Handles.SphereHandleCap))
            {
                Selection.activeGameObject = wp.gameObject;
                SnapCameraToWaypoint(fly, i);
            }
        }

        // ── Green '+' insert handles at segment midpoints ────────
        int segments = fly.loop ? fly.waypoints.Length : fly.waypoints.Length - 1;

        for (int i = 0; i < segments; i++)
        {
            int nextIdx = (i + 1) % fly.waypoints.Length;
            if (fly.waypoints[i] == null || fly.waypoints[nextIdx] == null) continue;

            Vector3 mid  = SplinePositionEditor(fly, i, 0.5f);
            float   size = HandleUtility.GetHandleSize(mid) * 0.18f;

            Handles.color = new Color(0.2f, 0.9f, 0.4f, 0.85f);

            if (Handles.Button(mid, Quaternion.identity, size, size * 1.5f, Handles.CubeHandleCap))
            {
                Quaternion rot = Quaternion.Slerp(
                    fly.waypoints[i].rotation,
                    fly.waypoints[nextIdx].rotation,
                    0.5f);
                InsertWaypointAfter(fly, i, mid, rot);
                break; // array changed — stop
            }

            Handles.Label(mid + Vector3.up * size * 2.2f, "+",
                new GUIStyle
                {
                    normal    = { textColor = new Color(0.2f, 0.9f, 0.4f) },
                    fontSize  = 14,
                    fontStyle = FontStyle.Bold
                });
        }
    }

    // ── Canvas hide / show ───────────────────────────────────────
    private static void HideCanvases()
    {
        _hiddenCanvasIds.Clear();
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
        {
            if (c.gameObject.activeSelf)
            {
                _hiddenCanvasIds.Add(c.gameObject.GetInstanceID());
                c.gameObject.SetActive(false);
                EditorUtility.SetDirty(c.gameObject);
            }
        }
        _canvasesHidden = true;
    }

    private static void ShowCanvases()
    {
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
        {
            if (_hiddenCanvasIds.Contains(c.gameObject.GetInstanceID()))
            {
                c.gameObject.SetActive(true);
                EditorUtility.SetDirty(c.gameObject);
            }
        }
        _hiddenCanvasIds.Clear();
        _canvasesHidden = false;
    }

    // ── Snap flythrough camera to waypoint (updates Game view preview) ──
    internal static void SnapCameraToWaypoint(CameraFlythrough fly, int idx)
    {
        if (idx < 0 || idx >= fly.waypoints.Length) return;
        Transform wp = fly.waypoints[idx];
        if (wp == null) return;

        Undo.RecordObject(fly.transform, "Preview Waypoint");
        fly.transform.position = wp.position;
        fly.transform.rotation = wp.rotation;

        EditorApplication.QueuePlayerLoopUpdate();

        // Force realtime lighting recalculation on the next editor frame.
        // Simply moving a light in edit mode doesn't invalidate Unity's light cache —
        // toggling enabled off/on is the reliable way to flush it.
        var lights = fly.GetComponentsInChildren<Light>(true);
        EditorApplication.delayCall += () =>
        {
            foreach (Light l in lights)
            {
                if (l == null) continue;
                l.enabled = false;
                l.enabled = true;
            }
            SceneView.RepaintAll();
        };
    }

    // ── Insert a new waypoint after afterIdx ─────────────────────
    internal static void InsertWaypointAfter(CameraFlythrough fly, int afterIdx, Vector3 position, Quaternion rotation)
    {
        Undo.RecordObject(fly, "Insert Waypoint");

        GameObject wpGo = new GameObject($"Waypoint_{afterIdx + 1}");
        Undo.RegisterCreatedObjectUndo(wpGo, "Insert Waypoint");

        wpGo.transform.position = position;
        wpGo.transform.rotation = rotation;

        Transform parent = fly.waypoints[afterIdx] != null ? fly.waypoints[afterIdx].parent : null;
        wpGo.transform.SetParent(parent);

        var marker = wpGo.AddComponent<FlythroughWaypoint>();
        marker.owner = fly;

        var list = new List<Transform>(fly.waypoints);
        list.Insert(afterIdx + 1, wpGo.transform);
        fly.waypoints = list.ToArray();

        // Renumber for clarity
        for (int i = 0; i < fly.waypoints.Length; i++)
            if (fly.waypoints[i] != null)
                fly.waypoints[i].gameObject.name = $"Waypoint_{i}";

        EditorUtility.SetDirty(fly);
        Selection.activeGameObject = wpGo;
    }

    // ── Spline helpers (mirrors CameraFlythrough logic) ──────────
    private static Vector3 SplinePositionEditor(CameraFlythrough fly, int segIdx, float t)
    {
        Vector3 p0 = WaypointPos(fly, segIdx - 1);
        Vector3 p1 = WaypointPos(fly, segIdx);
        Vector3 p2 = WaypointPos(fly, segIdx + 1);
        Vector3 p3 = WaypointPos(fly, segIdx + 2);
        return CentripetalCatmullRom(p0, p1, p2, p3, t);
    }

    private static Vector3 WaypointPos(CameraFlythrough fly, int i)
    {
        int n = fly.waypoints.Length;
        if (fly.loop) i = ((i % n) + n) % n;
        else          i = Mathf.Clamp(i, 0, n - 1);
        return fly.waypoints[i] != null ? fly.waypoints[i].position : Vector3.zero;
    }

    private static Vector3 CentripetalCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        const float alpha = 0.5f;
        float t0 = 0f;
        float t1 = t0 + Mathf.Pow(Mathf.Max(Vector3.Distance(p0, p1), 1e-4f), alpha);
        float t2 = t1 + Mathf.Pow(Mathf.Max(Vector3.Distance(p1, p2), 1e-4f), alpha);
        float t3 = t2 + Mathf.Pow(Mathf.Max(Vector3.Distance(p2, p3), 1e-4f), alpha);

        float tp = Mathf.Lerp(t1, t2, t);

        Vector3 A1 = SplineRemap(t0, t1, p0, p1, tp);
        Vector3 A2 = SplineRemap(t1, t2, p1, p2, tp);
        Vector3 A3 = SplineRemap(t2, t3, p2, p3, tp);
        Vector3 B1 = SplineRemap(t0, t2, A1, A2, tp);
        Vector3 B2 = SplineRemap(t1, t3, A2, A3, tp);
        return    SplineRemap(t1, t2, B1, B2, tp);
    }

    private static Vector3 SplineRemap(float t0, float t1, Vector3 p0, Vector3 p1, float t)
    {
        float d = t1 - t0;
        if (d < 1e-4f) return p0;
        return ((t1 - t) * p0 + (t - t0) * p1) / d;
    }

    // Returns forward projected onto the XZ plane — ignores pitch so spawned waypoints stay at same height.
    internal static Vector3 FlatForward(Transform t)
    {
        Vector3 f = new Vector3(t.forward.x, 0f, t.forward.z);
        return f.sqrMagnitude > 0.001f ? f.normalized : t.right; // fallback if looking straight up/down
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