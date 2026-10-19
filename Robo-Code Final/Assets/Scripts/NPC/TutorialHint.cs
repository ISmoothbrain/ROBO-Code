using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A hint box at the top of the screen (e.g. how to start a battle). Disappears by itself.
public static class TutorialHint
{
    private static GameObject root;
    private static TextMeshProUGUI text;
    private static TutorialHintTimer timer;

    public static void Show(string message, float seconds = 10f)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (root == null) Build();
        text.text = NpcUtil.Keys(message);
        root.SetActive(true);
        timer.hideAt = Time.unscaledTime + seconds;
    }

    public static void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    private static void Build()
    {
        Canvas canvas = UIBuild.Canvas(null, "TutorialHint", 85);
        timer = canvas.gameObject.AddComponent<TutorialHintTimer>();
        root = UIBuild.Node("Hint", canvas.transform);
        RectTransform r = (RectTransform)root.transform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -150f);
        r.sizeDelta = new Vector2(1100f, 0f);
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.07f, 0.12f, 0.88f);
        bg.raycastTarget = false;
        VerticalLayoutGroup v = root.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(28, 28, 16, 18);
        v.spacing = 6;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        UIBuild.Label(UIBuild.Node("Title", root.transform), 26, FontStyles.Bold, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Center).text = "HINT";
        text = UIBuild.Label(UIBuild.Node("Text", root.transform), 26, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
        root.SetActive(false);
    }

    // true while a hint is on screen (used by the timer below)
    public static bool Visible => root != null && root.activeSelf;
}

// Hides the hint after its time, or when a battle starts.
public class TutorialHintTimer : MonoBehaviour
{
    public float hideAt;
    private void Update()
    {
        if (TutorialHint.Visible && (Time.unscaledTime > hideAt || BattleManager.InBattle)) TutorialHint.Hide();
    }
}
