using UnityEngine;

// A platform that travels through its Points in a loop (2 points = back and forth).
//
// It moves by a shared clock instead of step-by-step, so platforms can never drift out of sync.
//   Cycle Time:   seconds for one full round trip. Give two platforms the SAME Cycle Time and they stay
//                 in step forever, even if their paths are different lengths. 0 = work it out from Speed.
//   Phase Offset: where in the round trip it starts (0 = at the first point, 0.5 = halfway round).
//                 Use it to make two platforms meet: e.g. one at 0 and one at 0.5.
public class MovingPlatform : MonoBehaviour
{
    public float speed = 2f;
    public Transform[] points;

    [Tooltip("Seconds for one full round trip. Same value on two platforms = they stay in sync. 0 = use Speed.")]
    public float cycleTime = 0f;
    [Tooltip("Where in the round trip this platform starts. 0 = first point, 0.5 = halfway round.")]
    [Range(0f, 1f)] public float phaseOffset = 0f;

    private Vector3[] path;       // the points' positions, read once at Start
    private float[] segmentStart; // distance along the loop where each segment begins
    private float loopLength;

    void Start()
    {
        if (points == null || points.Length == 0) { enabled = false; return; }

        // Read the positions once, so points that are children of the platform don't move with it.
        path = new Vector3[points.Length];
        for (int k = 0; k < points.Length; k++) path[k] = points[k] != null ? points[k].position : transform.position;

        // The loop goes 0 -> 1 -> ... -> last -> back to 0.
        segmentStart = new float[path.Length];
        loopLength = 0f;
        for (int k = 0; k < path.Length; k++)
        {
            segmentStart[k] = loopLength;
            loopLength += Vector2.Distance(path[k], path[(k + 1) % path.Length]);
        }

        transform.position = PositionAt(0f);
    }

    void Update()
    {
        if (loopLength <= 0.0001f) return;
        float duration = cycleTime > 0f ? cycleTime : loopLength / Mathf.Max(0.01f, speed);
        // Everyone uses the same clock (time since the level loaded), so nothing can drift.
        float u = Mathf.Repeat(Time.timeSinceLevelLoad / duration, 1f);
        transform.position = PositionAt(u);
    }

    private Vector3 PositionAt(float u)
    {
        if (path.Length == 1) return path[0];
        float d = Mathf.Repeat(u + phaseOffset, 1f) * loopLength;
        for (int k = path.Length - 1; k >= 0; k--)
        {
            if (d >= segmentStart[k])
            {
                Vector3 a = path[k], b = path[(k + 1) % path.Length];
                float len = Vector2.Distance(a, b);
                return len < 0.0001f ? a : Vector3.Lerp(a, b, (d - segmentStart[k]) / len);
            }
        }
        return path[0];
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (points == null || points.Length < 2) return;
        Gizmos.color = Color.cyan;
        for (int k = 0; k < points.Length; k++)
            if (points[k] != null && points[(k + 1) % points.Length] != null)
                Gizmos.DrawLine(points[k].position, points[(k + 1) % points.Length].position);
    }
}
