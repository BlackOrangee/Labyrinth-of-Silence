using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Location flythrough for designers using a Centripetal Catmull-Rom spline
/// with arc-length parameterization. Speed is uniform across the entire route.
///
/// Play Mode controls:
///   Space  — pause / resume
///   R      — reset to start
///   W      — smooth acceleration
///   S      — smooth deceleration
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFlythrough : MonoBehaviour
{
    [Header("Route")]
    [Tooltip("Empty GameObjects — waypoints. Position + rotation define where the camera flies and looks.")]
    public Transform[] waypoints;

    [Header("Speed")]
    [Tooltip("Base movement speed (m/s)")]
    public float speed = 5f;

    [Tooltip("Maximum speed when holding W (m/s)")]
    public float maxSpeed = 20f;

    [Tooltip("Acceleration (m/s²)")]
    public float acceleration = 8f;

    [Tooltip("Deceleration (m/s²)")]
    public float deceleration = 6f;

    [Header("Route")]
    public bool loop = true;
    public bool autoPlay = true;

    [Header("Controls")]
    public KeyCode pauseKey    = KeyCode.Space;
    public KeyCode resetKey    = KeyCode.R;
    public KeyCode speedUpKey  = KeyCode.W;
    public KeyCode slowDownKey = KeyCode.S;

    // ── arc-length tables ───────────────────────────────────────
    // Per segment: array of cumulative arc lengths at ARC_SAMPLES points.
    // Converts "meters traveled" → spline parameter t.
    private const int ARC_SAMPLES = 100;
    private float[][] _arcTable;

    // ── state ───────────────────────────────────────────────────
    private int   _segIdx;
    private float _arcProgress;   // meters traveled in current segment
    private float _currentSpeed;
    private bool  _playing;
    private bool  _finished;

    // ── init ────────────────────────────────────────────────────
    void Start()
    {
        DisableOtherCameras();
        DisableAllCanvases();

        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogWarning("[CameraFlythrough] At least 2 waypoints are required!");
            return;
        }

        BuildArcTables();
        DoReset();

        if (autoPlay)
        {
            _playing      = true;
            _currentSpeed = speed;
        }
    }

    // ── arc-length table precomputation ─────────────────────────
    private void BuildArcTables()
    {
        int segs  = TotalSegments();
        _arcTable = new float[segs][];

        for (int i = 0; i < segs; i++)
        {
            _arcTable[i] = new float[ARC_SAMPLES + 1];
            _arcTable[i][0] = 0f;
            Vector3 prev = SplinePosition(i, 0f);

            for (int s = 1; s <= ARC_SAMPLES; s++)
            {
                float   t    = s / (float)ARC_SAMPLES;
                Vector3 curr = SplinePosition(i, t);
                _arcTable[i][s] = _arcTable[i][s - 1] + Vector3.Distance(prev, curr);
                prev = curr;
            }
        }
    }

    // Segment length in meters (along the curve, not straight line)
    private float SegArcLen(int segIdx) => _arcTable[segIdx][ARC_SAMPLES];

    // Convert meters along segment → spline parameter t (0..1)
    private float ArcToT(int segIdx, float arcLen)
    {
        float[] table    = _arcTable[segIdx];
        float   totalLen = table[ARC_SAMPLES];
        arcLen = Mathf.Clamp(arcLen, 0f, totalLen);

        for (int s = 1; s <= ARC_SAMPLES; s++)
        {
            if (table[s] >= arcLen)
            {
                float ratio = (arcLen - table[s - 1]) / Mathf.Max(table[s] - table[s - 1], 0.0001f);
                float t0    = (s - 1) / (float)ARC_SAMPLES;
                float t1    = s       / (float)ARC_SAMPLES;
                return Mathf.Lerp(t0, t1, ratio);
            }
        }
        return 1f;
    }

    // ── update ──────────────────────────────────────────────────
    void Update()
    {
        HandleInput();

        if (!_playing || _finished || _arcTable == null || waypoints == null || waypoints.Length < 2)
            return;

        UpdateSpeed();

        // Move along the curve in meters.
        // smoothDeltaTime smooths frame-time spikes → no stuttering
        _arcProgress += _currentSpeed * Time.smoothDeltaTime;

        // Advance to next segment (while handles very high speeds)
        float segLen = SegArcLen(_segIdx);
        while (_arcProgress >= segLen)
        {
            _arcProgress -= segLen;
            if (AdvanceSegment()) return;
            segLen = SegArcLen(_segIdx);
        }

        // Spline parameter t corresponding to meters traveled
        float t = ArcToT(_segIdx, _arcProgress);

        transform.position = SplinePosition(_segIdx, t);

        // Rotation: Centripetal Catmull-Rom applied to forward vectors.
        // Interpolates the look direction (3D vector) using the same spline as position.
        // No quaternion math — no jerks at segment boundaries.
        Vector3 f0 = waypoints[WrapIdx(_segIdx - 1)].forward;
        Vector3 f1 = waypoints[WrapIdx(_segIdx    )].forward;
        Vector3 f2 = waypoints[WrapIdx(_segIdx + 1)].forward;
        Vector3 f3 = waypoints[WrapIdx(_segIdx + 2)].forward;
        Vector3 fwd = CentripetalCatmullRom(f0, f1, f2, f3, t).normalized;
        if (fwd.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }

    // ── speed control ───────────────────────────────────────────
    private void UpdateSpeed()
    {
        float target = speed;
        if      (Input.GetKey(speedUpKey))  target = maxSpeed;
        else if (Input.GetKey(slowDownKey)) target = 0f;

        float rate = target > _currentSpeed ? acceleration : deceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, target, rate * Time.smoothDeltaTime);
    }

    // ── advance to next segment ─────────────────────────────────
    /// <returns>true if route is finished</returns>
    private bool AdvanceSegment()
    {
        int next = _segIdx + 1;

        if (next >= TotalSegments())
        {
            if (loop)
            {
                _segIdx = 0;
                return false;
            }
            else
            {
                int lastIdx = waypoints.Length - 1;
                transform.position = waypoints[lastIdx].position;
                transform.rotation = waypoints[lastIdx].rotation;
                _playing  = false;
                _finished = true;
                Debug.Log("[CameraFlythrough] Route complete.");
                return true;
            }
        }

        _segIdx = next;

        var wp = waypoints[WrapIdx(_segIdx)].GetComponent<FlythroughWaypoint>();
        if (wp != null && wp.doorToOpen != null)
            wp.doorToOpen.SendMessage("ForceOpen", SendMessageOptions.DontRequireReceiver);

        return false;
    }

    private int TotalSegments() => loop ? waypoints.Length : waypoints.Length - 1;

    // ── Centripetal Catmull-Rom spline ──────────────────────────
    // alpha=0.5 — centripetal parameterization.
    // Unlike standard (alpha=0), guarantees no loops or overshoots on sharp turns.
    private Vector3 SplinePosition(int segIdx, float t)
    {
        Vector3 p0 = waypoints[WrapIdx(segIdx - 1)].position;
        Vector3 p1 = waypoints[WrapIdx(segIdx    )].position;
        Vector3 p2 = waypoints[WrapIdx(segIdx + 1)].position;
        Vector3 p3 = waypoints[WrapIdx(segIdx + 2)].position;
        return CentripetalCatmullRom(p0, p1, p2, p3, t);
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

    private int WrapIdx(int i)
    {
        int n = waypoints.Length;
        if (loop) return ((i % n) + n) % n;
        return Mathf.Clamp(i, 0, n - 1);
    }

    // ── input ───────────────────────────────────────────────────
    private void HandleInput()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            _playing  = !_playing;
            _finished = false;
            if (_playing && _currentSpeed < 0.1f) _currentSpeed = speed;
        }

        if (Input.GetKeyDown(resetKey))
        {
            DoReset();
            _playing      = true;
            _currentSpeed = speed;
        }
    }

    // ── utilities ───────────────────────────────────────────────
    private void DoReset()
    {
        _segIdx      = 0;
        _arcProgress = 0f;
        _finished    = false;
        _playing     = false;

        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
        {
            transform.position = waypoints[0].position;
            transform.rotation = waypoints[0].rotation;
        }
    }

    private void DisableOtherCameras()
    {
        Camera mine = GetComponent<Camera>();
        int count = 0;
        foreach (Camera cam in FindObjectsOfType<Camera>(true))
            if (cam != mine) { cam.enabled = false; count++; }
        Debug.Log($"[CameraFlythrough] Disabled cameras: {count}");
    }

    private void DisableAllCanvases()
    {
        Canvas[] all = FindObjectsOfType<Canvas>(true);
        foreach (Canvas c in all) c.gameObject.SetActive(false);
        Debug.Log($"[CameraFlythrough] Disabled canvases: {all.Length}");
    }

    public void Play()  { _playing = true;  _finished = false; }
    public void Pause() { _playing = false; }
    public void Reset() { DoReset(); }

    // ══════════════════════════════════════════════════════════════
    // GIZMOS — real spline curve, always visible
    // ══════════════════════════════════════════════════════════════
#if UNITY_EDITOR
    private const int GIZMO_SAMPLES = 40;

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        int segments = loop ? waypoints.Length : waypoints.Length - 1;

        for (int i = 0; i < segments; i++)
        {
            bool    isClosing = loop && i == segments - 1;
            Vector3 prev      = SplinePositionEditor(i, 0f);

            for (int s = 1; s <= GIZMO_SAMPLES; s++)
            {
                float   t    = s / (float)GIZMO_SAMPLES;
                Vector3 curr = SplinePositionEditor(i, t);
                Gizmos.color = isClosing
                    ? new Color(0.2f, 0.8f, 1f, 0.25f)
                    : new Color(0.2f, 0.8f, 1f, 0.9f);
                Gizmos.DrawLine(prev, curr);
                prev = curr;
            }

            // direction arrow at segment midpoint
            Vector3 mid  = SplinePositionEditor(i, 0.50f);
            Vector3 mid2 = SplinePositionEditor(i, 0.53f);
            Vector3 dir  = (mid2 - mid).normalized;
            if (dir != Vector3.zero)
                DrawArrow(mid, dir, 0.5f, new Color(0.2f, 0.8f, 1f, 0.5f));
        }

        for (int i = 0; i < waypoints.Length; i++)
        {
            Transform wp = waypoints[i];
            if (wp == null) continue;

            bool  isFirst = i == 0;
            bool  isLast  = !loop && i == waypoints.Length - 1;
            Color col     = isFirst ? new Color(0.1f, 1f,  0.2f)
                          : isLast  ? new Color(1f,  0.2f, 0.2f)
                                    : new Color(1f,  0.8f, 0.1f);

            Gizmos.color = col;
            Gizmos.DrawSphere(wp.position, 0.25f);
            DrawArrow(wp.position, wp.forward, 1.5f, Color.white);

            GUIStyle style = new GUIStyle();
            style.normal.textColor = col;
            style.fontStyle = FontStyle.Bold;
            style.fontSize  = 12;
            Handles.Label(wp.position + Vector3.up * 0.45f,
                          isFirst ? "► START" : i.ToString(), style);
        }
    }

    private Vector3 SplinePositionEditor(int segIdx, float t)
    {
        Vector3 p0 = WaypointPosEditor(segIdx - 1);
        Vector3 p1 = WaypointPosEditor(segIdx    );
        Vector3 p2 = WaypointPosEditor(segIdx + 1);
        Vector3 p3 = WaypointPosEditor(segIdx + 2);
        return CentripetalCatmullRom(p0, p1, p2, p3, t);
    }

    private Vector3 WaypointPosEditor(int i)
    {
        int n = waypoints.Length;
        if (loop) i = ((i % n) + n) % n;
        else      i = Mathf.Clamp(i, 0, n - 1);
        return waypoints[i] != null ? waypoints[i].position : Vector3.zero;
    }

    private static void DrawArrow(Vector3 origin, Vector3 dir, float len, Color color)
    {
        if (dir == Vector3.zero) return;
        Gizmos.color = color;
        Vector3 tip   = origin + dir * len;
        Gizmos.DrawLine(origin, tip);
        Vector3 back  = -dir * (len * 0.22f);
        Vector3 right = Vector3.Cross(dir, Vector3.up);
        if (right.sqrMagnitude < 0.001f) right = Vector3.Cross(dir, Vector3.forward);
        right = right.normalized * (len * 0.13f);
        Gizmos.DrawLine(tip, tip + back + right);
        Gizmos.DrawLine(tip, tip + back - right);
    }
#endif
}