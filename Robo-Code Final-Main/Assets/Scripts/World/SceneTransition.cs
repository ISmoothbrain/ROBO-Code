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
    private TextMeshProUGUI title;
    private bool running;

    // Loads a scene with the fade. atStone = arrive next to the Stone of Recall (teleporting).
    public static void LoadScene(int buildIndex, bool atStone = false, string titleText = null)
    {
        SceneTransition t = Get();
        if (t.running) return;
        t.StartCoroutine(t.Run(buildIndex, atStone, titleText));
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
    }
}
