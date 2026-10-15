using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A random key appears inside a fixed ring while a second ring closes in on it.
// Press that key while the closing ring overlaps the fixed one to dodge.
// Pressing early, pressing the wrong key, or letting it pass all count as a miss.
public class QuickTimeEvent : MonoBehaviour
{
    [Header("References")]
    public GameObject root;
    public Image targetRing;   // stays still
    public Image closingRing;  // shrinks toward targetRing
    public TextMeshProUGUI keyText;
    public TextMeshProUGUI resultText;

    [Header("Tuning")]
    [Tooltip("Seconds for the closing ring to reach the target ring.")]
    public float closeTime = 1.3f;
    [Tooltip("How big the closing ring starts, relative to the target ring.")]
    public float startScale = 3f;
    [Tooltip("How close (in scale) counts as a hit. 0.15 = within 15% of the target ring.")]
    public float hitWindow = 0.15f;
    [Tooltip("Keys that can be picked. Leaves out I (inventory), E (talk), Q (skill) and H (potion).")]
    public string keyPool = "WRTYUOPASDFGJKLZXCVBNM";

    [Header("Colours")]
    public Color waitingColor = Color.white;
    public Color inWindowColor = new Color(0.45f, 1f, 0.55f);
    public Color hitColor = new Color(0.45f, 1f, 0.55f);
    public Color missColor = new Color(1f, 0.4f, 0.4f);

    private static Sprite ringSprite;

    private void Awake()
    {
        EnsureSprites();
        if (root != null) root.SetActive(false);
    }

    // No art needed: generate a ring sprite if the Images don't have one.
    private void EnsureSprites()
    {
        if (ringSprite == null) ringSprite = CreateRingSprite(256, 0.1f);
        if (targetRing != null && targetRing.sprite == null) targetRing.sprite = ringSprite;
        if (closingRing != null && closingRing.sprite == null) closingRing.sprite = ringSprite;
    }

    public IEnumerator Run(Action<bool> onDone)
    {
        char letter = keyPool[UnityEngine.Random.Range(0, keyPool.Length)];
        KeyCode key = (KeyCode)Enum.Parse(typeof(KeyCode), letter.ToString().ToUpperInvariant());

        EnsureSprites();
        root.SetActive(true);
        keyText.text = letter.ToString().ToUpperInvariant();
        if (resultText != null) resultText.gameObject.SetActive(false);
        closingRing.color = waitingColor;
        closingRing.transform.localScale = Vector3.one * startScale;

        yield return null; // don't count a key that was already held down

        bool? result = null;
        float t = 0f;
        float shrinkPerSecond = (startScale - 1f) / Mathf.Max(0.05f, closeTime);

        while (result == null)
        {
            t += Time.deltaTime;
            float scale = startScale - shrinkPerSecond * t;
            closingRing.transform.localScale = Vector3.one * Mathf.Max(0f, scale);

            bool inWindow = Mathf.Abs(scale - 1f) <= hitWindow;
            closingRing.color = inWindow ? inWindowColor : waitingColor;

            if (Input.GetKeyDown(key))
            {
                result = inWindow;                // right key: only counts at the right moment
            }
            else if (Input.anyKeyDown && !AnyMouseButtonDown())
            {
                result = false;                   // wrong key
            }
            else if (scale < 1f - hitWindow)
            {
                result = false;                   // too late
            }

            yield return null;
        }

        bool hit = result.Value;
        closingRing.color = hit ? hitColor : missColor;
        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = hit ? "PERFECT!" : "MISS!";
            resultText.color = hit ? hitColor : missColor;
        }

        yield return new WaitForSeconds(0.5f);
        root.SetActive(false);
        onDone(hit);
    }

    private static bool AnyMouseButtonDown() =>
        Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);

    private static Sprite CreateRingSprite(int size, float thickness)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float r = size * 0.5f;
        float outer = r - 1f;
        float inner = outer - size * thickness;

        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                // 1px soft edge on both sides so the ring isn't jagged
                float a = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
