using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class Coin : MonoBehaviour
{
    [Header("Value")]
    public int value = 1;

    [Header("Landing")]

    public LayerMask groundLayer;
    public float scatterSideDistance = 1f;   // how far left/right of the drop point it can land
    public float scatterArcHeight = 0.6f;    // how high the little hop arcs before landing
    public float settleTime = 0.35f;         // how long the hop takes
    public float groundRayDistance = 10f;    // how far down to search for ground from the spawn point
    public float restingHeight = 0.15f;      // how far above the ground surface the coin sits

    [Header("Float (once landed)")]
    public bool letAnimatorHandleFloat = false; // tick this if a child Animator already bobs the sprite
    public float bobHeight = 0.15f;
    public float bobSpeed = 2f;

    [Header("Spin (optional, only used if nothing else drives rotation)")]
    public float spinSpeed = 0f; // leave at 0 if a child Animator/sprite-flip handles the spin

    [Header("Pickup")]
    public float magnetRange = 1.5f;
    public float magnetSpeed = 8f;
    public float lifetime = 30f; // 0 = never expires

    private Rigidbody2D rb;
    private Transform player;
    private Vector3 floatAnchor;
    private float bobOffset;
    private bool isFloating;
    private bool collected;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Always kinematic, no gravity: this script drives every position change by
        // hand, so nothing about the fall is left to physics simulation.
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    void Start()
    {
        bobOffset = Random.Range(0f, Mathf.PI * 2f); // so a pile of coins doesn't bob in sync

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (lifetime > 0f) Destroy(gameObject, lifetime);

        if (groundLayer.value == 0)
        {
            Debug.LogWarning($"{name}: Ground Layer is not set — coin can't find a floor to land " +
                              $"on and will just hover at its spawn height.");
        }

        StartCoroutine(HopToLandingSpot());
    }

    private System.Collections.IEnumerator HopToLandingSpot()
    {
        Vector3 start = transform.position;
        float dx = Random.Range(-scatterSideDistance, scatterSideDistance);
        Vector3 probeFrom = start + new Vector3(dx, 0.5f, 0f);

        Vector3 landingPos;
        RaycastHit2D hit = Physics2D.Raycast(probeFrom, Vector2.down, groundRayDistance, groundLayer);
        if (hit.collider != null)
        {
            landingPos = new Vector3(probeFrom.x, hit.point.y + restingHeight, start.z);
        }
        else
        {
            // No ground found under the scatter point — land straight down at spawn
            // height instead of falling forever or ending up underground.
            landingPos = new Vector3(start.x, start.y, start.z);
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(settleTime, 0.01f);
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            float arc = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * scatterArcHeight;
            Vector3 pos = Vector3.Lerp(start, landingPos, eased);
            pos.y += arc;
            transform.position = pos;
            yield return null;
        }

        transform.position = landingPos;
        floatAnchor = landingPos;
        isFloating = true;
    }

    void Update()
    {
        if (collected) return;

        if (isFloating)
        {
            // floatAnchor is the coin's real position, with no bob baked into it. The
            // old version wrote transform.position (anchor + bob) back into floatAnchor
            // every frame, so the NEXT frame added a fresh bob on top of an anchor that
            // already had a bob in it — the offset compounded frame after frame instead
            // of oscillating in place, which is what looked like random teleporting.
            // Keeping the anchor bob-free fixes that regardless of magnet state.
            if (player != null && magnetRange > 0f &&
                Vector2.Distance(floatAnchor, player.position) <= magnetRange)
            {
                floatAnchor = Vector3.MoveTowards(floatAnchor, player.position, magnetSpeed * Time.deltaTime);
            }

            Vector3 displayPos = floatAnchor;
            if (!letAnimatorHandleFloat)
            {
                displayPos.y += Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight;
            }
            transform.position = displayPos;
        }

        if (spinSpeed != 0f)
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        collected = true;
        CoinCounter.Add(value);
        Destroy(gameObject);
    }
}

// Dead simple global tally. Swap this out for your InventoryManager if you already
// have one — just call into that from OnTriggerEnter2D instead.
public static class CoinCounter
{
    public static int Total { get; private set; }
    public static System.Action<int> OnCoinsChanged;

    public static void Add(int amount)
    {
        Total += amount;
        OnCoinsChanged?.Invoke(Total);
    }

    public static void Reset()
    {
        Total = 0;
        OnCoinsChanged?.Invoke(Total);
    }
}