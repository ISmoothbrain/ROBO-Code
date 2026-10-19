using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Dark-Souls-style death message and the final ending card. The runner survives
// long enough to hand the scene load to SceneTransition.
public class GameFlowOverlay : MonoBehaviour
{
    private static GameFlowOverlay instance;
    public static bool Busy => instance != null;

    private CanvasGroup group;
    private TextMeshProUGUI title;
    private TextMeshProUGUI subtitle;

    public static void ShowDeath(float delay, bool fell)
    {
        if (Busy) return;
        Ensure().StartCoroutine(instance.DeathRoutine(Mathf.Max(1.4f, delay), fell));
    }

    public static void ShowEnding()
    {
        if (Busy) return;
        Ensure().StartCoroutine(instance.EndingRoutine());
    }

    private static GameFlowOverlay Ensure()
    {
        if (instance != null) return instance;
        GameObject go = new GameObject("[Game Flow Overlay]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GameFlowOverlay>();
        instance.Build();
        return instance;
    }

    private IEnumerator DeathRoutine(float delay, bool fell)
    {
        title.text = "YOU DIED";
        subtitle.text = fell ? "SIGNAL LOST BELOW THE WORLD" : "SYSTEM FAILURE";
        yield return Fade(0f, 1f, 0.4f);
        yield return new WaitForSecondsRealtime(delay);

        int scene = SceneManager.GetActiveScene().buildIndex;
        bool atStone = LevelProgress.ShouldRespawnAtStone();
        string sceneTitle = LevelProgress.TitleForBuildIndex(scene);
        group.alpha = 0f;
        SceneTransition.LoadScene(scene, atStone, sceneTitle);
        Destroy(gameObject);
    }

    private IEnumerator EndingRoutine()
    {
        title.text = "THE END";
        subtitle.text = "EVERY BOSS DEFEATED  •  THE CODE LIVES ON";
        yield return Fade(0f, 1f, 1.1f);
        yield return new WaitForSecondsRealtime(4f);
        yield return Fade(1f, 0f, 0.8f);

        SceneTransition.LoadScene(LevelProgress.MainMenuBuildIndex(), false, null);
        Destroy(gameObject);
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
            yield return null;
        }
        group.alpha = to;
    }

    private void Build()
    {
        Canvas canvas = UIBuild.Canvas(transform, "FlowCanvas", 500);
        GameObject root = UIBuild.Node("Root", canvas.transform);
        UIBuild.Stretch(root, 0, 0, 1, 1);
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        GameObject titleObject = UIBuild.Node("Title", root.transform);
        RectTransform tr = (RectTransform)titleObject.transform;
        tr.anchorMin = new Vector2(0.1f, 0.38f);
        tr.anchorMax = new Vector2(0.9f, 0.62f);
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        title = UIBuild.Label(titleObject, 92, FontStyles.Bold, new Color(0.72f, 0.08f, 0.08f), TextAlignmentOptions.Center);
        title.characterSpacing = 8f;

        GameObject subtitleObject = UIBuild.Node("Subtitle", root.transform);
        RectTransform sr = (RectTransform)subtitleObject.transform;
        sr.anchorMin = new Vector2(0.1f, 0.28f);
        sr.anchorMax = new Vector2(0.9f, 0.40f);
        sr.offsetMin = sr.offsetMax = Vector2.zero;
        subtitle = UIBuild.Label(subtitleObject, 24, FontStyles.Normal, new Color(0.72f, 0.78f, 0.84f), TextAlignmentOptions.Center);
    }
}
