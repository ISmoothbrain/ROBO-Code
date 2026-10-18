using UnityEngine;

// Plays an NPC's frame animations by name (idle, scared, thumbsUp, point...).
// The art faces right; with Face Player on, the NPC turns toward the player when they're close.
public class NpcAnimator : MonoBehaviour
{
    [System.Serializable]
    public class Clip
    {
        public string name = "idle";
        public Sprite[] frames;
        public float framesPerSecond = 6f;
    }

    public Clip[] clips;
    public string startClip = "idle";
    public bool facePlayer = true;
    public float faceRange = 6f;

    private SpriteRenderer sr;
    private Clip current;
    private float t;
    private Transform player;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        Play(startClip);
    }

    public bool Has(string clipName)
    {
        if (clips == null || string.IsNullOrEmpty(clipName)) return false;
        foreach (Clip c in clips) if (c != null && c.name == clipName && c.frames != null && c.frames.Length > 0) return true;
        return false;
    }

    public void Play(string clipName)
    {
        if (!Has(clipName)) return;
        foreach (Clip c in clips)
        {
            if (c == null || c.name != clipName) continue;
            if (current == c) return;
            current = c;
            t = 0f;
            if (sr != null) sr.sprite = c.frames[0];
            return;
        }
    }

    private void Update()
    {
        if (current != null && sr != null)
        {
            t += Time.deltaTime;
            int i = (int)(t * Mathf.Max(0.1f, current.framesPerSecond)) % current.frames.Length;
            sr.sprite = current.frames[i];
        }
        if (facePlayer && sr != null)
        {
            if (player == null) { Player p = FindAnyObjectByType<Player>(); if (p != null) player = p.transform; }
            if (player != null && Mathf.Abs(player.position.x - transform.position.x) < faceRange)
                sr.flipX = player.position.x < transform.position.x;
        }
    }
}
