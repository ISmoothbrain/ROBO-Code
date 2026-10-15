using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Builds the whole battle system automatically when a level with a Player loads.
// Nothing to place in the scene and nothing to wire up.
// If a scene already contains a BattleManager (for example one made with
// Tools > Battle System > Set Up Battle In Open Scene), that one is used instead.
public static class BattleBootstrap
{
    private static bool subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (!subscribed)
        {
            SceneManager.sceneLoaded += (scene, mode) => Build();
            subscribed = true;
        }
        Build();
    }

    private static void Build()
    {
        if (Object.FindFirstObjectByType<BattleManager>() != null) return; // scene has its own
        if (Object.FindFirstObjectByType<Player>() == null) return;        // menus etc. don't need battles

        try
        {
            EnsureEventSystem();

            // The UI is built while ACTIVE: TextMeshPro only loads its font and material
            // when its object is switched on, and setting an outline before that crashes
            // with "ArgumentNullException ... Parameter name: source".
            GameObject system = new GameObject("[Battle System]");

            GameObject focus = new GameObject("BattleFocus");
            focus.transform.SetParent(system.transform, false);

            Canvas canvas = BuildCanvas(system.transform);
            BattleUI ui = BuildBattleUI(canvas.transform);
            QuickTimeEvent qte = BuildQTE(canvas.transform);

            // The manager alone starts switched off, so its Awake() runs after it's wired up.
            GameObject managerGo = new GameObject("BattleManager");
            managerGo.SetActive(false);
            managerGo.transform.SetParent(system.transform, false);

            BattleManager manager = managerGo.AddComponent<BattleManager>();
            manager.ui = ui;
            manager.qte = qte;
            manager.battleFocus = focus.transform;
            manager.cameraDirector = managerGo.AddComponent<BattleCameraDirector>();
            manager.defaultQuestions = DefaultPythonQuestions.Create();
            manager.settings = BattleSettings.Load();

            managerGo.SetActive(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Battle] Couldn't build the battle system automatically. " +
                           "Please send this whole message (click it to see every line):\n" + e);
        }
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(es);
    }

    // ================================================================ canvas

    private static Canvas BuildCanvas(Transform parent)
    {
        GameObject go = new GameObject("BattleCanvas", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // above the HUD

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // ================================================================ battle UI

    private static BattleUI BuildBattleUI(Transform canvas)
    {
        GameObject go = Node("BattleUI", canvas);
        Stretch(go, 0, 0, 1, 1);

        GameObject root = Node("Root", go.transform);
        Stretch(root, 0, 0, 1, 1);

        Panel("LetterboxTop", root.transform, 0, 0.92f, 1, 1, Color.black);
        Panel("LetterboxBottom", root.transform, 0, 0, 1, 0.03f, Color.black);

        BattleUI ui = go.AddComponent<BattleUI>();
        ui.root = root;

        HealthBar(root.transform, "PlayerHealth", "YOU", 0.02f, 0.38f, new Color(0.35f, 0.85f, 0.45f),
                  out ui.playerHealthFill, out ui.playerHealthText, out _);
        HealthBar(root.transform, "EnemyHealth", "ENEMY", 0.62f, 0.98f, new Color(0.9f, 0.3f, 0.3f),
                  out ui.enemyHealthFill, out ui.enemyHealthText, out ui.enemyNameText);

        GameObject banner = Node("Banner", root.transform);
        Stretch(banner, 0.1f, 0.62f, 0.9f, 0.82f);
        ui.bannerText = Label(banner, 54, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Outline(ui.bannerText, 0.25f);

        // Fixed width, centred at the bottom, and only as tall as its content.
        GameObject panel = Node("QuestionPanel", root.transform);
        RectTransform pr = (RectTransform)panel.transform;
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0f);
        pr.pivot = new Vector2(0.5f, 0f);
        pr.anchoredPosition = new Vector2(0f, 60f);
        pr.sizeDelta = new Vector2(1400f, 0f);
        panel.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.1f, 0.94f);
        ContentSizeFitter fit = panel.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ui.questionPanel = panel;
        VerticalLayoutGroup v = panel.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(28, 28, 18, 22);
        v.spacing = 10;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        ui.goalText = Label(Node("Goal", panel.transform), 26, FontStyles.Bold, new Color(0.5f, 0.85f, 1f), TextAlignmentOptions.Left);

        GameObject codeBox = Node("CodeBox", panel.transform);
        codeBox.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 1f);
        VerticalLayoutGroup cv = codeBox.AddComponent<VerticalLayoutGroup>();
        cv.padding = new RectOffset(18, 18, 12, 12);
        cv.childControlWidth = cv.childControlHeight = true;
        cv.childForceExpandWidth = true;
        cv.childForceExpandHeight = false;
        ui.codeText = Label(Node("Code", codeBox.transform), 28, FontStyles.Normal, new Color(0.9f, 0.92f, 0.95f), TextAlignmentOptions.TopLeft);

        GameObject options = Node("Options", panel.transform);
        HorizontalLayoutGroup h = options.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 12;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = false;
        ui.optionContainer = options.transform;

        GameObject timerBg = Node("Timer", panel.transform);
        timerBg.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
        timerBg.AddComponent<LayoutElement>().minHeight = 8;
        ui.timerFill = (RectTransform)Panel("Fill", timerBg.transform, 0, 0, 1, 1, new Color(1f, 0.83f, 0.3f)).transform;

        ui.explanationText = Label(Node("Explanation", panel.transform), 22, FontStyles.Normal, new Color(0.88f, 0.88f, 0.88f), TextAlignmentOptions.Left);

        ui.optionTemplate = MakeButton(root.transform, "OptionTemplate");
        ui.optionTemplate.gameObject.SetActive(false);

        panel.SetActive(false);
        root.SetActive(false);
        return ui;
    }

    private static QuickTimeEvent BuildQTE(Transform canvas)
    {
        GameObject go = Node("QuickTimeEvent", canvas);
        Stretch(go, 0, 0, 1, 1);

        GameObject root = Node("Root", go.transform);
        Stretch(root, 0, 0, 1, 1);

        QuickTimeEvent qte = go.AddComponent<QuickTimeEvent>();
        qte.root = root;
        qte.targetRing = Ring(root.transform, "TargetRing", new Color(1f, 1f, 1f, 0.9f));
        qte.closingRing = Ring(root.transform, "ClosingRing", Color.white);

        GameObject key = Node("Key", root.transform);
        Centre(key, 120);
        qte.keyText = Label(key, 72, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Outline(qte.keyText, 0.2f);

        GameObject result = Node("Result", root.transform);
        RectTransform rr = (RectTransform)result.transform;
        rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f);
        rr.sizeDelta = new Vector2(500, 80);
        rr.anchoredPosition = new Vector2(0, -150);
        qte.resultText = Label(result, 48, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Outline(qte.resultText, 0.25f);

        root.SetActive(false);
        return qte;
    }

    // ================================================================ helpers

    private static GameObject Node(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(GameObject go, float x0, float y0, float x1, float y1)
    {
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = new Vector2(x0, y0);
        r.anchorMax = new Vector2(x1, y1);
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static void Centre(GameObject go, float size)
    {
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(size, size);
        r.anchoredPosition = Vector2.zero;
    }

    private static GameObject Panel(string name, Transform parent, float x0, float y0, float x1, float y1, Color c)
    {
        GameObject go = Node(name, parent);
        Stretch(go, x0, y0, x1, y1);
        go.AddComponent<Image>().color = c;
        return go;
    }

    private static TextMeshProUGUI Label(GameObject go, float size, FontStyles style, Color c, TextAlignmentOptions align)
    {
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        UIFont.Apply(t);
        t.fontSize = size;
        t.fontStyle = style;
        t.color = c;
        t.alignment = align;
        t.raycastTarget = false;
        t.text = "";
        return t;
    }

    // Outlines need the font's material. Skip quietly rather than crash if it isn't ready.
    private static void Outline(TextMeshProUGUI t, float width)
    {
        if (t == null || t.font == null || t.fontSharedMaterial == null) return;
        t.outlineWidth = width;
        t.outlineColor = Color.black;
    }

    private static void HealthBar(Transform parent, string name, string title, float x0, float x1, Color fillColor,
                                  out RectTransform fill, out TextMeshProUGUI valueText, out TextMeshProUGUI nameText)
    {
        GameObject box = Node(name, parent);
        Stretch(box, x0, 0.84f, x1, 0.91f);

        GameObject label = Node("Name", box.transform);
        Stretch(label, 0, 0.55f, 1, 1);
        nameText = Label(label, 24, FontStyles.Bold, Color.white, TextAlignmentOptions.BottomLeft);
        nameText.text = title;

        GameObject bg = Panel("Background", box.transform, 0, 0, 1, 0.5f, new Color(0f, 0f, 0f, 0.6f));
        fill = (RectTransform)Panel("Fill", bg.transform, 0, 0, 1, 1, fillColor).transform;

        GameObject val = Node("Value", bg.transform);
        Stretch(val, 0, 0, 1, 1);
        valueText = Label(val, 18, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
    }

    private static Button MakeButton(Transform parent, string name)
    {
        GameObject go = Node(name, parent);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.16f, 0.19f, 0.27f);
        Button b = go.AddComponent<Button>();
        b.targetGraphic = img;
        go.AddComponent<LayoutElement>().minHeight = 54;

        GameObject text = Node("Text", go.transform);
        Stretch(text, 0, 0, 1, 1);
        Label(text, 28, FontStyles.Bold, Color.white, TextAlignmentOptions.Center).text = "option";
        return b;
    }

    private static Image Ring(Transform parent, string name, Color c)
    {
        GameObject go = Node(name, parent);
        Centre(go, 180);
        Image img = go.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        return img;
    }
}
