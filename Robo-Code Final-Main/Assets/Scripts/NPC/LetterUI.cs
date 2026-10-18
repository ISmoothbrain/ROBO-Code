using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A letter on parchment (the General's message found on NightBorne).
// Queue() waits until any battle or other window is finished, then shows it. E, Esc or Close dismisses it.
public static class LetterUI
{
    public static bool IsOpen { get; private set; }
    public static bool Busy => IsOpen || pending != null;     // shown, or waiting to be shown

    private static string[] pending;
    private static bool pendingEnds, endsAfterClose;
    private static GameObject root;
    private static TextMeshProUGUI titleText, bodyText, signText;

    // endsGame: closing the letter plays the ending (THE END...) and returns to the main menu.
    public static void Queue(string title, string body, string signature, bool endsGame = false)
    {
        pending = new[] { title, body, signature };
        pendingEnds = endsGame;
        EnsureRunner();
    }

    // called by the runner every frame
    public static void Tick()
    {
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)) Close();
            return;
        }
        if (pending == null) return;
        if (BattleManager.InBattle || DialogueUI.IsOpen || SceneTransition.Busy) return;   // wait for the battle to finish
        Open(pending[0], pending[1], pending[2]);
        endsAfterClose = pendingEnds;
        pending = null;
    }

    private static void Open(string title, string body, string signature)
    {
        if (root == null) Build();
        titleText.text = title;
        bodyText.text = body;
        signText.text = signature;
        root.SetActive(true);
        IsOpen = true;
    }

    private static void Close()
    {
        if (root != null) root.SetActive(false);
        IsOpen = false;
        UIBusy.MarkClosed();
        if (endsAfterClose)
        {
            endsAfterClose = false;
            SceneTransition.ShowEnding();
        }
    }

    private static void EnsureRunner()
    {
        if (Object.FindAnyObjectByType<LetterRunner>() != null) return;
        GameObject go = new GameObject("[Letter]");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<LetterRunner>();
    }

    private static void Build()
    {
        Canvas canvas = UIBuild.Canvas(null, "LetterWindow", 120);
        Object.DontDestroyOnLoad(canvas.gameObject);
        root = UIBuild.Node("Root", canvas.transform);
        UIBuild.Stretch(root, 0, 0, 1, 1);
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        // the parchment: a dark brown edge around a cream sheet
        GameObject edge = UIBuild.Node("Edge", root.transform);
        RectTransform er = (RectTransform)edge.transform;
        er.anchorMin = er.anchorMax = new Vector2(0.5f, 0.5f);
        er.sizeDelta = new Vector2(900f, 620f);
        edge.AddComponent<Image>().color = new Color(0.36f, 0.24f, 0.12f);

        GameObject paper = UIBuild.Node("Paper", edge.transform);
        UIBuild.Stretch(paper, 0, 0, 1, 1);
        RectTransform pr = (RectTransform)paper.transform;
        pr.offsetMin = new Vector2(10f, 10f); pr.offsetMax = new Vector2(-10f, -10f);
        paper.AddComponent<Image>().color = new Color(0.93f, 0.86f, 0.68f);

        Color ink = new Color(0.25f, 0.15f, 0.07f);
        titleText = Text(paper.transform, "Title", 40, FontStyles.Bold, ink, TextAlignmentOptions.Center, new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.94f));
        bodyText = Text(paper.transform, "Body", 28, FontStyles.Normal, ink, TextAlignmentOptions.TopLeft, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.78f));
        signText = Text(paper.transform, "Signature", 28, FontStyles.Italic, ink, TextAlignmentOptions.TopRight, new Vector2(0.45f, 0.10f), new Vector2(0.92f, 0.28f));
        Text(paper.transform, "Hint", 20, FontStyles.Normal, new Color(0.45f, 0.33f, 0.2f), TextAlignmentOptions.BottomLeft, new Vector2(0.08f, 0.03f), new Vector2(0.5f, 0.10f)).text = "[E] Close";

        // a red wax seal
        GameObject seal = UIBuild.Node("Seal", paper.transform);
        RectTransform sr = (RectTransform)seal.transform;
        sr.anchorMin = sr.anchorMax = new Vector2(0.12f, 0.17f);
        sr.sizeDelta = new Vector2(64f, 64f);
        seal.AddComponent<Image>().color = new Color(0.62f, 0.08f, 0.08f);
        sr.localEulerAngles = new Vector3(0f, 0f, 45f);

        root.SetActive(false);
    }

    private static TextMeshProUGUI Text(Transform parent, string name, float size, FontStyles style, Color c,
                                        TextAlignmentOptions align, Vector2 min, Vector2 max)
    {
        GameObject go = UIBuild.Node(name, parent);
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
        TextMeshProUGUI t = UIBuild.Label(go, size, style, c, align);
        return t;
    }
}

// Drives LetterUI (waiting for the battle to end, closing on E).
public class LetterRunner : MonoBehaviour
{
    private void Update() => LetterUI.Tick();
}

// Put on a boss: when it dies, the General's letter is shown (and, optionally, a save flag is set).
public class LetterOnDefeat : MonoBehaviour
{
    public string title = "Message To NightBorne";
    [TextArea] public string body = "NightBorne, you and Magitek are assigned to take control of the world of these Fleshbags. " +
                                    "Your main task is to rid it of them and take all their water supplies for cooling down our data centres.";
    [TextArea] public string signature = "Regards,\nGeneral ChudEBT";
    public string saveKey = "";
    [Tooltip("Closing the letter plays the ending (used for the hidden NightBorne in the tutorial).")]
    public bool endsGame = false;

    private BanditEnemy enemy;
    private bool done;

    private void Start() => enemy = GetComponent<BanditEnemy>();

    private void Update()
    {
        if (done || enemy == null || !enemy.IsDead) return;
        done = true;
        if (!string.IsNullOrEmpty(saveKey)) { PlayerPrefs.SetInt(saveKey, 1); PlayerPrefs.Save(); }
        LetterUI.Queue(title, body, signature, endsGame);
    }
}
