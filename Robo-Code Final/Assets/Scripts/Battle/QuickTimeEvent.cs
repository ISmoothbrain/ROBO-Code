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
    [Header("Boss Direction Sequence")]
    public TextMeshProUGUI sequenceText;
    public TextMeshProUGUI instructionText;
    [Tooltip("Seconds to show the sequence before input begins.")]
    public float previewTime = 0.85f;
    public Color sequenceDoneColor = new Color(0.35f, 1f, 0.6f);
    public Color sequenceCurrentColor = new Color(1f, 0.84f, 0.35f);

    [Header("Tuning")]
    [Tooltip("Seconds for the closing ring to reach the target ring.")]
    public float closeTime = 1.5f;
    [Tooltip("How big the closing ring starts, relative to the target ring.")]
    public float startScale = 3f;
    [Tooltip("How close (in scale) counts as a hit. 0.15 = within 15% of the target ring.")]
    public float hitWindow = 0.25f;
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

    // Bosses pass arrowCount > 0. Normal enemies keep the original ring-only dodge.
    public IEnumerator Run(Action<bool> onDone, int arrowCount = 0, float secondsPerArrow = 0.9f)
    {
        if (string.IsNullOrEmpty(keyPool))
        {
            Debug.LogWarning("[QTE] Key pool is empty.");
            onDone(false);
            yield break;
        }
        char letter = keyPool[UnityEngine.Random.Range(0, keyPool.Length)];
        KeyCode key = (KeyCode)Enum.Parse(typeof(KeyCode), letter.ToString().ToUpperInvariant());

        EnsureSprites();
        root.SetActive(true);
        keyText.text = letter.ToString().ToUpperInvariant();
        if (resultText != null) resultText.gameObject.SetActive(false);
        if (sequenceText != null) sequenceText.gameObject.SetActive(false);
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(true);
            instructionText.text = arrowCount > 0 ? "BOSS QTE  •  Time the key, then enter the arrows!" : "Time your key as the rings meet!";
        }
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

        yield return new WaitForSeconds(hit && arrowCount > 0 ? 0.25f : 0.5f);
        if (hit && arrowCount > 0)
            yield return DirectionSequence(arrowCount, secondsPerArrow, passed => hit = passed);

        root.SetActive(false);
        onDone(hit);
    }

    private static readonly KeyCode[] Directions =
    {
        KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow
    };
    private static readonly string[] DirectionNames = { "UP", "DOWN", "LEFT", "RIGHT" };
    private static readonly string[] DirectionGlyphs = { "↑", "↓", "←", "→" };

    private IEnumerator DirectionSequence(int count, float secondsPerArrow, Action<bool> completed)
    {
        count = Mathf.Clamp(count, 1, 12);
        secondsPerArrow = Mathf.Max(0.25f, secondsPerArrow);
        int[] order = new int[count];
        for (int i = 0; i < count; i++)
        {
            order[i] = UnityEngine.Random.Range(0, Directions.Length);
            // Avoid streaks that are too similar, without making the next button predictable.
            if (i > 1 && order[i] == order[i - 1] && order[i - 1] == order[i - 2])
                order[i] = (order[i] + UnityEngine.Random.Range(1, 4)) % 4;
        }

        closingRing.gameObject.SetActive(false);
        targetRing.gameObject.SetActive(false);
        keyText.gameObject.SetActive(false);
        if (resultText != null) resultText.gameObject.SetActive(false);

        if (sequenceText != null) sequenceText.gameObject.SetActive(true);
        if (instructionText != null) instructionText.text = "MEMORIZE  •  Arrow keys, left to right";
        DrawSequence(order, -1);
        yield return new WaitForSeconds(previewTime);
        if (instructionText != null) instructionText.text = "EXECUTE  •  One arrow at a time before the timer expires!";

        bool success = true;
        for (int step = 0; step < count; step++)
        {
            DrawSequence(order, step);
            float remaining = secondsPerArrow;
            bool pressed = false;
            // A key held from the previous step must be released before this step.
            yield return null;
            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                if (resultText != null)
                {
                    resultText.gameObject.SetActive(true);
                    resultText.color = remaining < secondsPerArrow * 0.3f ? missColor : sequenceCurrentColor;
                    resultText.text = $"{step + 1} / {count}   •   {Mathf.Max(0, remaining):0.0}s";
                }

                // Explicitly check all four arrow keys: a wrong arrow fails immediately.
                int pressedArrow = -1;
                for (int j = 0; j < Directions.Length; j++)
                    if (Input.GetKeyDown(Directions[j])) { pressedArrow = j; break; }
                if (pressedArrow >= 0)
                {
                    success = pressedArrow == order[step];
                    pressed = true;
                    break;
                }
                if (Input.anyKeyDown && !AnyMouseButtonDown())
                {
                    success = false;
                    pressed = true;
                    break;
                }
                yield return null;
            }
            if (!pressed || !success) { success = false; break; }
        }

        DrawSequence(order, success ? count : -2);
        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = success ? "SEQUENCE CLEAR!" : "SEQUENCE FAILED!";
            resultText.color = success ? hitColor : missColor;
        }
        yield return new WaitForSeconds(0.55f);
        if (sequenceText != null) sequenceText.gameObject.SetActive(false);
        targetRing.gameObject.SetActive(true);
        closingRing.gameObject.SetActive(true);
        keyText.gameObject.SetActive(true);
        completed(success);
    }

    private void DrawSequence(int[] order, int step)
    {
        if (sequenceText == null)
        {
            // Legacy scene QTEs without the new widget still show the current prompt.
            if (step >= 0 && step < order.Length)
                keyText.text = DirectionNames[order[step]];
            else
                keyText.text = step == order.Length ? "CLEAR" : "ARROWS";
            keyText.gameObject.SetActive(true);
            return;
        }

        System.Text.StringBuilder text = new System.Text.StringBuilder();
        for (int i = 0; i < order.Length; i++)
        {
            if (i > 0) text.Append("  ");
            if (step == -2) text.Append("<color=#FF6262>");
            else if (step >= 0 && i < step) text.Append("<color=#65FF99>");
            else if (step >= 0 && i == step) text.Append("<color=#FFD766><u>");
            else text.Append("<color=#C3DEF5>");

            text.Append(DirectionGlyphs[order[i]]);
            if (step >= 0 && i == step && step < order.Length) text.Append("</u>");
            text.Append("</color>");
        }
        sequenceText.text = text.ToString();
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
