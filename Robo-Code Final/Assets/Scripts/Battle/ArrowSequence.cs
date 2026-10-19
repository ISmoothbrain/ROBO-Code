using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Boss combo, Helldivers "Stratagem Hero" style: a row of arrows to type in order before the
// timer runs out. Arrow keys or WASD. A wrong key resets the combo to the start.
// Builds its own UI; BattleBootstrap places it on the battle screen.
public class ArrowSequence : MonoBehaviour
{
    public float arrowSize = 64f;
    public Color waitingColor = new Color(1f, 1f, 1f, 0.85f);
    public Color doneColor = new Color(1f, 0.85f, 0.3f);
    public Color wrongColor = new Color(1f, 0.35f, 0.35f);

    // 0 = up, 1 = right, 2 = down, 3 = left
    private static readonly KeyCode[][] Keys =
    {
        new[] { KeyCode.UpArrow, KeyCode.W },
        new[] { KeyCode.RightArrow, KeyCode.D },
        new[] { KeyCode.DownArrow, KeyCode.S },
        new[] { KeyCode.LeftArrow, KeyCode.A },
    };
    private static readonly float[] Rotation = { 0f, -90f, 180f, 90f };
    private static Sprite arrowSprite;

    private GameObject root;
    private RectTransform row;
    private RectTransform timerFill;
    private TextMeshProUGUI title;
    private readonly List<Image> arrows = new List<Image>();

    private void Awake() => EnsureBuilt();

    private void EnsureBuilt()
    {
        if (root != null) return;
        if (arrowSprite == null) arrowSprite = MakeArrowSprite();
        BuildUI();
        root.SetActive(false);
    }

    public IEnumerator Run(int length, float timeLimit, Action<bool> onDone)
    {
        // When a battle starts, everything on screen that isn't the battle display gets hidden,
        // and that included this combo. Switch ourselves back on and draw on top of everything.
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        EnsureBuilt();
        transform.SetAsLastSibling();

        length = Mathf.Clamp(length, 1, 12);
        int[] combo = new int[length];
        for (int i = 0; i < length; i++) combo[i] = UnityEngine.Random.Range(0, 4);

        // one arrow image per step
        while (arrows.Count < length)
        {
            GameObject a = new GameObject("Arrow", typeof(RectTransform));
            a.transform.SetParent(row, false);
            ((RectTransform)a.transform).sizeDelta = new Vector2(arrowSize, arrowSize);
            Image img = a.AddComponent<Image>();
            img.sprite = arrowSprite;
            img.raycastTarget = false;
            arrows.Add(img);
        }
        for (int i = 0; i < arrows.Count; i++)
        {
            bool used = i < length;
            arrows[i].gameObject.SetActive(used);
            if (!used) continue;
            arrows[i].rectTransform.anchoredPosition = new Vector2((i - (length - 1) * 0.5f) * (arrowSize + 12f), 0f);
            arrows[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, Rotation[combo[i]]);
            arrows[i].color = waitingColor;
        }

        title.text = "BOSS COMBO! Type the arrows";
        title.color = Color.white;
        SetTimer(1f);
        root.SetActive(true);
        yield return null; // ignore keys held from the ring

        int progress = 0;
        float t = 0f;
        bool? result = null;
        while (result == null)
        {
            t += Time.deltaTime;
            SetTimer(1f - t / Mathf.Max(0.1f, timeLimit));
            if (t >= timeLimit) { result = false; break; }

            int pressed = ReadDirection();
            if (pressed >= 0)
            {
                if (pressed == combo[progress])
                {
                    arrows[progress].color = doneColor;
                    progress++;
                    if (progress >= length) result = true;
                }
                else
                {
                    // wrong key: flash red and start again
                    for (int i = 0; i < length; i++) arrows[i].color = wrongColor;
                    yield return new WaitForSeconds(0.12f);
                    t += 0.12f;
                    for (int i = 0; i < length; i++) arrows[i].color = waitingColor;
                    progress = 0;
                }
            }
            yield return null;
        }

        bool ok = result.Value;
        title.text = ok ? "COMBO COMPLETE!" : "COMBO FAILED!";
        title.color = ok ? doneColor : wrongColor;
        if (!ok) for (int i = progress; i < length; i++) arrows[i].color = wrongColor;
        yield return new WaitForSeconds(0.6f);
        root.SetActive(false);
        onDone(ok);
    }

    private static int ReadDirection()
    {
        for (int d = 0; d < 4; d++)
            foreach (KeyCode k in Keys[d])
                if (Input.GetKeyDown(k)) return d;
        return -1;
    }

    private void SetTimer(float f)
    {
        if (timerFill != null) timerFill.anchorMax = new Vector2(Mathf.Clamp01(f), 1f);
    }

    // ---------------------------------------------------------------- UI

    private void BuildUI()
    {
        root = Node("Root", transform);
        RectTransform rr = (RectTransform)root.transform;
        rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f);
        rr.sizeDelta = new Vector2(900f, 190f);
        rr.anchoredPosition = new Vector2(0f, 60f);
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.06f, 0.1f, 0.85f);
        bg.raycastTarget = false;

        GameObject t = Node("Title", root.transform);
        RectTransform tr = (RectTransform)t.transform;
        tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f); tr.pivot = new Vector2(0.5f, 1f);
        tr.sizeDelta = new Vector2(0f, 50f); tr.anchoredPosition = new Vector2(0f, -8f);
        title = t.AddComponent<TextMeshProUGUI>();
        UIFont.Apply(title);
        title.fontSize = 34; title.fontStyle = FontStyles.Bold; title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;

        GameObject r = Node("Arrows", root.transform);
        row = (RectTransform)r.transform;
        row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
        row.sizeDelta = Vector2.zero; row.anchoredPosition = new Vector2(0f, -5f);

        GameObject timerBg = Node("Timer", root.transform);
        RectTransform tb = (RectTransform)timerBg.transform;
        tb.anchorMin = new Vector2(0.06f, 0f); tb.anchorMax = new Vector2(0.94f, 0f); tb.pivot = new Vector2(0.5f, 0f);
        tb.sizeDelta = new Vector2(0f, 12f); tb.anchoredPosition = new Vector2(0f, 16f);
        timerBg.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
        GameObject fill = Node("Fill", timerBg.transform);
        timerFill = (RectTransform)fill.transform;
        timerFill.anchorMin = Vector2.zero; timerFill.anchorMax = Vector2.one; timerFill.offsetMin = timerFill.offsetMax = Vector2.zero;
        fill.AddComponent<Image>().color = new Color(1f, 0.85f, 0.3f);
    }

    private static GameObject Node(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    // A chunky pixel arrow pointing up, made in code so no art is needed.
    private static Sprite MakeArrowSprite()
    {
        const int S = 16;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        Color32[] px = new Color32[S * S];
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                int fromTop = S - 1 - y;
                bool head = fromTop >= 1 && fromTop <= 7 && Mathf.Abs(x - 7.5f) <= fromTop + 0.5f;
                bool stem = fromTop > 7 && fromTop <= 14 && x >= 5 && x <= 10;
                px[y * S + x] = head || stem ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 16f);
    }
}
