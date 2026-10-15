using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Builds the dialogue box and the shop window automatically when a level with a Player
// loads, and gives the Player the ability to drink potions (H). Nothing to set up by hand.
public static class GameUIBootstrap
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
        Player player = Object.FindFirstObjectByType<Player>();
        if (player == null) return;
        if (Object.FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include) != null) return;

        try
        {
            if (player.GetComponent<PotionUser>() == null) player.gameObject.AddComponent<PotionUser>();

            GameObject system = new GameObject("[Game UI]");
            Canvas canvas = UIBuild.Canvas(system.transform, "GameUICanvas", 90);

            DialogueRefs d = BuildDialogue(canvas.transform);
            ShopRefs s = BuildShop(canvas.transform);

            // Controllers start switched off so their Awake() runs after they're wired up.
            GameObject controllers = new GameObject("Controllers");
            controllers.SetActive(false);
            controllers.transform.SetParent(system.transform, false);

            DialogueUI dialogue = controllers.AddComponent<DialogueUI>();
            dialogue.root = d.root;
            dialogue.nameText = d.name;
            dialogue.bodyText = d.body;
            dialogue.continueHint = d.hint;
            dialogue.choiceContainer = d.choices;
            dialogue.choiceTemplate = d.choiceTemplate;

            ShopUI shop = controllers.AddComponent<ShopUI>();
            shop.root = s.root;
            shop.titleText = s.title;
            shop.coinsText = s.coins;
            shop.feedbackText = s.feedback;
            shop.itemContainer = s.items;
            shop.rowTemplate = s.rowTemplate;
            shop.closeButton = s.close;

            controllers.SetActive(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Game UI] Couldn't build the dialogue/shop UI automatically. " +
                           "Please send this whole message:\n" + e);
        }
    }

    // ================================================================ dialogue

    private struct DialogueRefs
    {
        public GameObject root;
        public TextMeshProUGUI name, body, hint;
        public Transform choices;
        public Button choiceTemplate;
    }

    private static DialogueRefs BuildDialogue(Transform canvas)
    {
        DialogueRefs r = new DialogueRefs();

        GameObject root = UIBuild.Node("Dialogue", canvas);
        UIBuild.Stretch(root, 0, 0, 1, 1);
        r.root = root;

        // Box along the bottom, centred, fixed width
        GameObject box = UIBuild.Node("Box", root.transform);
        RectTransform br = (RectTransform)box.transform;
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 0f);
        br.pivot = new Vector2(0.5f, 0f);
        br.anchoredPosition = new Vector2(0f, 40f);
        br.sizeDelta = new Vector2(1400f, 230f);
        Image bg = box.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.07f, 0.1f, 0.95f);
        UIBuild.Border(box, new Color(0.9f, 0.75f, 0.35f));

        // Name tag sitting on the top edge of the box
        GameObject tag = UIBuild.Node("NameTag", box.transform);
        RectTransform tr = (RectTransform)tag.transform;
        tr.anchorMin = tr.anchorMax = new Vector2(0f, 1f);
        tr.pivot = new Vector2(0f, 0f);
        tr.anchoredPosition = new Vector2(30f, -10f);
        tr.sizeDelta = new Vector2(320f, 52f);
        tag.AddComponent<Image>().color = new Color(0.9f, 0.75f, 0.35f);
        GameObject tagText = UIBuild.Node("Text", tag.transform);
        UIBuild.Stretch(tagText, 0, 0, 1, 1);
        r.name = UIBuild.Label(tagText, 30, FontStyles.Bold, new Color(0.1f, 0.08f, 0.05f), TextAlignmentOptions.Center);

        GameObject body = UIBuild.Node("Body", box.transform);
        UIBuild.Stretch(body, 0, 0, 1, 1);
        RectTransform bodyRect = (RectTransform)body.transform;
        bodyRect.offsetMin = new Vector2(36f, 50f);
        bodyRect.offsetMax = new Vector2(-36f, -40f);
        r.body = UIBuild.Label(body, 30, FontStyles.Normal, Color.white, TextAlignmentOptions.TopLeft);

        GameObject hint = UIBuild.Node("Hint", box.transform);
        RectTransform hr = (RectTransform)hint.transform;
        hr.anchorMin = hr.anchorMax = new Vector2(1f, 0f);
        hr.pivot = new Vector2(1f, 0f);
        hr.anchoredPosition = new Vector2(-24f, 12f);
        hr.sizeDelta = new Vector2(300f, 36f);
        r.hint = UIBuild.Label(hint, 22, FontStyles.Normal, new Color(0.9f, 0.75f, 0.35f), TextAlignmentOptions.BottomRight);

        // Choices appear above the box, on the right
        GameObject choices = UIBuild.Node("Choices", root.transform);
        RectTransform cr = (RectTransform)choices.transform;
        cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0f);
        cr.pivot = new Vector2(1f, 0f);
        cr.anchoredPosition = new Vector2(700f, 290f);
        cr.sizeDelta = new Vector2(360f, 0f);
        VerticalLayoutGroup v = choices.AddComponent<VerticalLayoutGroup>();
        v.spacing = 8;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        choices.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        r.choices = choices.transform;

        r.choiceTemplate = UIBuild.Button(root.transform, "ChoiceTemplate", "choice", 28, 56);
        r.choiceTemplate.gameObject.SetActive(false);

        root.SetActive(false);
        return r;
    }

    // ================================================================ shop

    private struct ShopRefs
    {
        public GameObject root, rowTemplate;
        public TextMeshProUGUI title, coins, feedback;
        public Transform items;
        public Button close;
    }

    private static ShopRefs BuildShop(Transform canvas)
    {
        ShopRefs r = new ShopRefs();

        GameObject root = UIBuild.Node("Shop", canvas);
        UIBuild.Stretch(root, 0, 0, 1, 1);
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f); // dim the game behind
        r.root = root;

        GameObject window = UIBuild.Node("Window", root.transform);
        RectTransform wr = (RectTransform)window.transform;
        wr.anchorMin = wr.anchorMax = new Vector2(0.5f, 0.5f);
        wr.sizeDelta = new Vector2(1100f, 0f);
        window.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.13f, 0.98f);
        UIBuild.Border(window, new Color(0.9f, 0.75f, 0.35f));
        VerticalLayoutGroup v = window.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(32, 32, 26, 26);
        v.spacing = 12;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        window.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Header: title on the left, coins on the right
        GameObject header = UIBuild.Node("Header", window.transform);
        HorizontalLayoutGroup hh = header.AddComponent<HorizontalLayoutGroup>();
        hh.childControlWidth = hh.childControlHeight = true;
        hh.childForceExpandWidth = true;
        r.title = UIBuild.Label(UIBuild.Node("Title", header.transform), 40, FontStyles.Bold, new Color(0.9f, 0.75f, 0.35f), TextAlignmentOptions.Left);
        r.coins = UIBuild.Label(UIBuild.Node("Coins", header.transform), 30, FontStyles.Bold, Color.white, TextAlignmentOptions.Right);

        GameObject items = UIBuild.Node("Items", window.transform);
        VerticalLayoutGroup iv = items.AddComponent<VerticalLayoutGroup>();
        iv.spacing = 8;
        iv.childControlWidth = iv.childControlHeight = true;
        iv.childForceExpandWidth = true;
        iv.childForceExpandHeight = false;
        r.items = items.transform;

        r.feedback = UIBuild.Label(UIBuild.Node("Feedback", window.transform), 24, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);

        r.close = UIBuild.Button(window.transform, "Close", "Close  [Esc]", 26, 54);

        r.rowTemplate = BuildRowTemplate(root.transform);
        r.rowTemplate.SetActive(false);

        root.SetActive(false);
        return r;
    }

    private static GameObject BuildRowTemplate(Transform parent)
    {
        GameObject row = UIBuild.Node("RowTemplate", parent);
        row.AddComponent<Image>().color = new Color(0.14f, 0.16f, 0.22f);
        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(14, 14, 10, 10);
        h.spacing = 18;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        GameObject icon = UIBuild.Node("Icon", row.transform);
        icon.AddComponent<Image>();
        LayoutElement il = icon.AddComponent<LayoutElement>();
        il.preferredWidth = il.preferredHeight = 64;

        GameObject texts = UIBuild.Node("Texts", row.transform);
        VerticalLayoutGroup tv = texts.AddComponent<VerticalLayoutGroup>();
        tv.childControlWidth = tv.childControlHeight = true;
        tv.childForceExpandWidth = true;
        tv.childForceExpandHeight = false;
        texts.AddComponent<LayoutElement>().flexibleWidth = 1;
        UIBuild.Label(UIBuild.Node("Name", texts.transform), 30, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
        UIBuild.Label(UIBuild.Node("Description", texts.transform), 22, FontStyles.Normal, new Color(0.8f, 0.82f, 0.86f), TextAlignmentOptions.Left);

        GameObject price = UIBuild.Node("Price", row.transform);
        price.AddComponent<LayoutElement>().preferredWidth = 160;
        UIBuild.Label(price, 28, FontStyles.Bold, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Right);

        Button buy = UIBuild.Button(row.transform, "Buy", "Buy", 28, 56);
        buy.GetComponent<LayoutElement>().preferredWidth = 150;
        buy.GetComponent<Image>().color = new Color(0.22f, 0.5f, 0.3f);

        return row;
    }
}

// Small helpers for building UI from code, shared by the bootstraps.
public static class UIBuild
{
    public static Canvas Canvas(Transform parent, string name, int sortingOrder)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        UnityEngine.Canvas canvas = go.AddComponent<UnityEngine.Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static GameObject Node(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static void Stretch(GameObject go, float x0, float y0, float x1, float y1)
    {
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = new Vector2(x0, y0);
        r.anchorMax = new Vector2(x1, y1);
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    public static TextMeshProUGUI Label(GameObject go, float size, FontStyles style, Color c, TextAlignmentOptions align)
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

    public static Button Button(Transform parent, string name, string label, float fontSize, float height)
    {
        GameObject go = Node(name, parent);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.16f, 0.19f, 0.27f);
        Button b = go.AddComponent<UnityEngine.UI.Button>();
        b.targetGraphic = img;
        go.AddComponent<LayoutElement>().minHeight = height;

        GameObject text = Node("Text", go.transform);
        Stretch(text, 0, 0, 1, 1);
        Label(text, fontSize, FontStyles.Bold, Color.white, TextAlignmentOptions.Center).text = label;
        return b;
    }

    // A thin outline frame, drawn with UI Outline (no sprite needed).
    public static void Border(GameObject go, Color c)
    {
        Outline o = go.AddComponent<Outline>();
        o.effectColor = c;
        o.effectDistance = new Vector2(3f, -3f);
    }
}
