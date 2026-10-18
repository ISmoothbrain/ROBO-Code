using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Fade to black, show the destination's name, load the scene, fade back in.
// Used by Start on the main menu and by every Stone of Recall teleport.
public class SceneTransition : MonoBehaviour
{
    public float fadeOutTime = 0.6f;
    public float holdTime = 0.9f;     // how long the title stays on the black screen
    public float fadeInTime = 0.5f;

    public static bool Busy => instance != null && instance.running;

    private static SceneTransition instance;
    private static bool arriveAtStone;
    private Image black;
    private Image band;
    private TextMeshProUGUI title;
    private TextMeshProUGUI subtitle;
    private bool running;

    // Loads a scene with the fade. atStone = arrive next to the Stone of Recall (teleporting).
    public static void LoadScene(int buildIndex, bool atStone = false, string titleText = null)
    {
        SceneTransition t = Get();
        if (t.running) return;
        t.StartCoroutine(t.Run(buildIndex, atStone, titleText));
    }

    // Lines shown under YOU DIED (one at random).
    private static readonly string[] DeathLines =
    {
        "Segmentation fault (core dumped)",
        "Uncaught exception: player.health < 0",
        "Stack overflow. Try again.",
        "Process terminated with exit code 1",
        "NullReferenceException: hero was null",
        "Fatal error. Rebooting...",
    };

    // Called when the player dies: YOU DIED, then back to the Stone of Recall (or the level start).
    public static void ShowDeathAndRespawn()
    {
        SceneTransition t = Get();
        if (t.running) return;
        t.StartCoroutine(t.RunDeath());
    }

    // Respawn rule: at the Stone of Recall, except in the tutorial until the player has reached its stone.
    private static bool RespawnAtStone() => LevelProgress.ShouldRespawnAtStone();

    private IEnumerator RunDeath()
    {
        running = true;
        bool atStone = RespawnAtStone();
        string where = atStone && Object.FindAnyObjectByType<StoneOfRecall>() != null
            ? "Returning to the Stone of Recall..." : "Restarting...";

        // dark band across the middle with the big red letters, Dark Souls style
        black.raycastTarget = true;
        band.gameObject.SetActive(true);
        title.text = "YOU DIED";
        title.color = new Color(0.72f, 0.08f, 0.08f);
        title.fontSize = 120;
        subtitle.text = DeathLines[Random.Range(0, DeathLines.Length)] + "\n" + where;
        float t = 0f;
        while (t < 1.4f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 1.4f);
            band.color = new Color(0f, 0f, 0f, 0.75f * k);
            title.alpha = k;
            subtitle.alpha = Mathf.Clamp01((t - 0.6f) / 0.8f);
            title.transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.04f, k);  // slow, heavy grow
            yield return null;
        }
        yield return new WaitForSecondsRealtime(1.6f);

        yield return Fade(0f, 1f, 0.7f, false);              // to black
        title.alpha = 0f; subtitle.alpha = 0f; band.gameObject.SetActive(false);
        title.transform.localScale = Vector3.one;
        // keep it invisible: setting a colour also sets its transparency, and a solid colour
        // here used to leave "YOU DIED" on screen after respawning
        title.color = new Color(0.85f, 0.95f, 1f, 0f); title.fontSize = 64;

        arriveAtStone = atStone;
        Time.timeScale = 1f;
        AsyncOperation op = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        while (!op.isDone) yield return null;
        yield return null;

        yield return Fade(1f, 0f, fadeInTime, false);
        black.raycastTarget = false;
        running = false;
    }

    // Every boss is defeated: THE END, then back to the main menu (build index 0).
    public static void ShowEnding()
    {
        SceneTransition t = Get();
        if (t.running) return;
        t.StartCoroutine(t.RunEnding());
    }

    private IEnumerator RunEnding()
    {
        running = true;
        black.raycastTarget = true;
        yield return Fade(0f, 1f, 1.5f, false);           // slow fade to black

        title.text = "THE END";
        title.fontSize = 110;
        title.color = new Color(0.85f, 0.95f, 1f);
        subtitle.text = "Every boss has fallen. Thanks for playing!\nChoose New Game on the main menu to play again.";
        float t = 0f;
        while (t < 2f)
        {
            t += Time.unscaledDeltaTime;
            title.alpha = Mathf.Clamp01(t / 1.2f);
            subtitle.alpha = Mathf.Clamp01((t - 0.8f) / 1.2f);
            yield return null;
        }
        yield return new WaitForSecondsRealtime(4f);
        yield return Fade(1f, 0f, 0.8f, true);
        subtitle.alpha = 0f;
        title.fontSize = 64;

        Time.timeScale = 1f;
        AsyncOperation op = SceneManager.LoadSceneAsync(0);   // the main menu
        while (!op.isDone) yield return null;
        yield return Fade(1f, 0f, fadeInTime, false);
        black.raycastTarget = false;
        running = false;
    }

    // WorldBootstrap asks this once per scene: did we arrive by teleport?
    public static bool ConsumeArriveAtStone()
    {
        bool v = arriveAtStone;
        arriveAtStone = false;
        return v;
    }

    private static SceneTransition Get()
    {
        if (instance != null) return instance;
        GameObject go = new GameObject("[Scene Transition]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneTransition>();
        instance.Build();
        return instance;
    }

    private IEnumerator Run(int buildIndex, bool atStone, string titleText)
    {
        running = true;
        title.text = titleText ?? "";
        title.alpha = 0f;
        black.raycastTarget = true; // block clicks while fading

        yield return Fade(0f, 1f, fadeOutTime, false);
        yield return Fade(0f, 1f, 0.25f, true);   // title fades in on the black screen
        yield return new WaitForSecondsRealtime(holdTime);

        arriveAtStone = atStone;
        Time.timeScale = 1f;                      // in case we left from a paused game
        AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex);
        while (!op.isDone) yield return null;
        yield return null;                        // let the new level set itself up

        yield return Fade(1f, 0f, 0.25f, true);
        yield return Fade(1f, 0f, fadeInTime, false);
        black.raycastTarget = false;
        running = false;
    }

    private IEnumerator Fade(float from, float to, float time, bool titleOnly)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / Mathf.Max(0.01f, time)));
            if (titleOnly) title.alpha = a; else black.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        if (titleOnly) title.alpha = to; else black.color = new Color(0f, 0f, 0f, to);
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // above everything
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject b = new GameObject("Black", typeof(RectTransform));
        b.transform.SetParent(transform, false);
        RectTransform br = (RectTransform)b.transform;
        br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
        black = b.AddComponent<Image>();
        black.color = new Color(0f, 0f, 0f, 0f);
        black.raycastTarget = false;

        GameObject t = new GameObject("Title", typeof(RectTransform));
        t.transform.SetParent(transform, false);
        RectTransform tr = (RectTransform)t.transform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
        title = t.AddComponent<TextMeshProUGUI>();
        UIFont.Apply(title);
        title.fontSize = 64;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.85f, 0.95f, 1f);
        title.raycastTarget = false;
        title.alpha = 0f;

        // death screen pieces: a dark band behind the text, and a smaller line underneath
        GameObject bd = new GameObject("DeathBand", typeof(RectTransform));
        bd.transform.SetParent(transform, false);
        bd.transform.SetSiblingIndex(1); // above the black, below the text
        RectTransform bdr = (RectTransform)bd.transform;
        bdr.anchorMin = new Vector2(0f, 0.38f); bdr.anchorMax = new Vector2(1f, 0.62f); bdr.offsetMin = bdr.offsetMax = Vector2.zero;
        band = bd.AddComponent<Image>();
        band.color = new Color(0f, 0f, 0f, 0f);
        band.raycastTarget = false;
        bd.SetActive(false);

        GameObject st = new GameObject("Subtitle", typeof(RectTransform));
        st.transform.SetParent(transform, false);
        RectTransform str = (RectTransform)st.transform;
        str.anchorMin = new Vector2(0f, 0.30f); str.anchorMax = new Vector2(1f, 0.40f); str.offsetMin = str.offsetMax = Vector2.zero;
        subtitle = st.AddComponent<TextMeshProUGUI>();
        UIFont.Apply(subtitle);
        subtitle.fontSize = 30;
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.color = new Color(0.8f, 0.78f, 0.75f);
        subtitle.raycastTarget = false;
        subtitle.alpha = 0f;
    }
}
