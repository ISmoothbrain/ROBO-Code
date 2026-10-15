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
            ApplyPlayerLook(player);
            CreateBackground();
            if (player.GetComponent<PotionUser>() == null) player.gameObject.AddComponent<PotionUser>();
            if (player.GetComponent<PlayerSkills>() == null) player.gameObject.AddComponent<PlayerSkills>();
            if (Object.FindFirstObjectByType<InventoryManager>() == null) new GameObject("InventoryManager").AddComponent<InventoryManager>();
            HideOldHud();

            GameObject system = new GameObject("[Game UI]");
            Canvas canvas = UIBuild.Canvas(system.transform, "GameUICanvas", 90);

            HudRefs h = BuildHud(canvas.transform);
            BackpackRefs b = BuildBackpack(canvas.transform);
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

            HudUI hud = controllers.AddComponent<HudUI>();
            hud.heartRow = h.hearts;
            hud.manaRow = h.mana;

            BackpackUI backpack = controllers.AddComponent<BackpackUI>();
            backpack.root = b.root;
            backpack.grid = b.grid;
            backpack.slotTemplate = b.slotTemplate;
            backpack.coinsText = b.coins;
            backpack.infoTitle = b.infoTitle;
            backpack.infoText = b.infoText;
            backpack.slotSprite = b.slotSprite;
            backpack.slotSelectedSprite = b.slotSelectedSprite;

            controllers.SetActive(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Game UI] Couldn't build the dialogue/shop UI automatically. " +
                           "Please send this whole message:\n" + e);
        }
    }

    // ================================================================ player look & old HUD

    // Uses Assets/Resources/PlayerCharacter (an Animator Override Controller) for the Player's
    // sprites and animations. Delete or rename that file to go back to the original look.
    private static void ApplyPlayerLook(Player player)
    {
        RuntimeAnimatorController look = Resources.Load<RuntimeAnimatorController>("PlayerCharacter");
        Animator animator = player.GetComponent<Animator>();
        if (look != null && animator != null) animator.runtimeAnimatorController = look;
    }

    // The parallax city skyline. Pick the theme in Assets/Resources/BackgroundSettings.
    private static void CreateBackground()
    {
        if (Object.FindFirstObjectByType<ParallaxBackground>() != null) return;
        BackgroundSettings settings = BackgroundSettings.Load();
        if (settings == null) return;
        new GameObject("[Background]").AddComponent<ParallaxBackground>().settings = settings;
    }

    // The scene's original HUD pieces are replaced by the new HUD and backpack.
    private static readonly string[] OldHudNames = { "HealthBar", "ManaBar", "CoinCounter", "InventoryPanel" };

    private static void HideOldHud()
    {
        foreach (InventoryUI old in Object.FindObjectsByType<InventoryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            old.enabled = false; // the new backpack handles the I key
            if (old.inventoryPanel != null) old.inventoryPanel.SetActive(false);
        }

        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!c.isRootCanvas) continue;
            foreach (Transform child in c.transform)
            {
                if (System.Array.IndexOf(OldHudNames, child.name) >= 0) child.gameObject.SetActive(false);
            }
        }
    }

    // ================================================================ HUD

    private struct HudRefs { public RectTransform hearts, mana; }

    private static HudRefs BuildHud(Transform canvas)
    {
        HudRefs r = new HudRefs();
        GameObject hud = UIBuild.Node("HUD", canvas);
        RectTransform hr = (RectTransform)hud.transform;
        hr.anchorMin = hr.anchorMax = new Vector2(0f, 1f);   // pinned to the top-left corner
        hr.pivot = new Vector2(0f, 1f);
        hr.anchoredPosition = new Vector2(28f, -24f);
        hr.sizeDelta = new Vector2(900f, 0f);
        VerticalLayoutGroup v = hud.AddComponent<VerticalLayoutGroup>();
        v.spacing = 8;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = v.childForceExpandHeight = false;
        hud.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        r.hearts = Row(hud.transform, "Hearts", 4);
        r.mana = Row(hud.transform, "Mana", 6);
        return r;
    }

    private static RectTransform Row(Transform parent, string name, float spacing)
    {
        GameObject row = UIBuild.Node(name, parent);
        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = spacing;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        return (RectTransform)row.transform;
    }

    // ================================================================ backpack

    private struct BackpackRefs
    {
        public GameObject root, slotTemplate;
        public Transform grid;
        public TextMeshProUGUI coins, infoTitle, infoText;
        public Sprite slotSprite, slotSelectedSprite;
    }

    private static BackpackRefs BuildBackpack(Transform canvas)
    {
        BackpackRefs r = new BackpackRefs();
        Sprite panel = Resources.Load<Sprite>("UI/panel");
        r.slotSprite = Resources.Load<Sprite>("UI/slot");
        r.slotSelectedSprite = Resources.Load<Sprite>("UI/slot_selected");
        Color cyan = new Color(0.55f, 0.88f, 1f);

        GameObject root = UIBuild.Node("Backpack", canvas);
        UIBuild.Stretch(root, 0, 0, 1, 1);
        r.root = root;

        GameObject window = UIBuild.Node("Window", root.transform);
        RectTransform wr = (RectTransform)window.transform;
        wr.anchorMin = wr.anchorMax = new Vector2(0.5f, 0.5f);
        wr.sizeDelta = new Vector2(880f, 0f);
        UIBuild.Sliced(window, panel, new Color(0.04f, 0.09f, 0.15f, 1f));
        VerticalLayoutGroup v = window.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(26, 26, 18, 22);
        v.spacing = 12;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        window.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Header: "Backpack" on the left, coin icon + total on the right
        GameObject header = UIBuild.Node("Header", window.transform);
        HorizontalLayoutGroup hh = header.AddComponent<HorizontalLayoutGroup>();
        hh.childAlignment = TextAnchor.MiddleLeft;
        hh.spacing = 10;
        hh.childControlWidth = hh.childControlHeight = true;
        hh.childForceExpandWidth = false;
        GameObject title = UIBuild.Node("Title", header.transform);
        title.AddComponent<LayoutElement>().flexibleWidth = 1;
        UIBuild.Label(title, 36, FontStyles.Normal, cyan, TextAlignmentOptions.Left).text = "Backpack";
        GameObject coinIcon = UIBuild.Node("CoinIcon", header.transform);
        Image ci = coinIcon.AddComponent<Image>();
        ci.sprite = Resources.Load<Sprite>("Icons/coin");
        ci.preserveAspect = true;
        LayoutElement cl = coinIcon.AddComponent<LayoutElement>();
        cl.preferredWidth = cl.preferredHeight = 32; // 4x the 8px coin
        GameObject coins = UIBuild.Node("Coins", header.transform);
        coins.AddComponent<LayoutElement>().preferredWidth = 110;
        r.coins = UIBuild.Label(coins, 32, FontStyles.Bold, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Left);

        // Slot grid, 10 across
        GameObject grid = UIBuild.Node("Grid", window.transform);
        GridLayoutGroup g = grid.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(70f, 70f);
        g.spacing = new Vector2(8f, 8f);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 10;
        g.childAlignment = TextAnchor.UpperCenter;
        r.grid = grid.transform;

        // Item info
        GameObject info = UIBuild.Node("Info", window.transform);
        UIBuild.Sliced(info, panel, new Color(0.03f, 0.07f, 0.12f, 1f));
        VerticalLayoutGroup iv = info.AddComponent<VerticalLayoutGroup>();
        iv.padding = new RectOffset(18, 18, 10, 12);
        iv.childControlWidth = iv.childControlHeight = true;
        iv.childForceExpandWidth = true;
        iv.childForceExpandHeight = false;
        info.AddComponent<LayoutElement>().minHeight = 96;
        r.infoTitle = UIBuild.Label(UIBuild.Node("Name", info.transform), 28, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
        r.infoText = UIBuild.Label(UIBuild.Node("Description", info.transform), 22, FontStyles.Normal, new Color(0.78f, 0.86f, 0.92f), TextAlignmentOptions.Left);

        GameObject hint = UIBuild.Node("Hint", window.transform);
        UIBuild.Label(hint, 20, FontStyles.Normal, new Color(0.45f, 0.6f, 0.7f), TextAlignmentOptions.Center).text = "[I] or [Esc] to close";

        // Slot template: frame + icon + stack count in the bottom-right corner
        GameObject slot = UIBuild.Node("SlotTemplate", root.transform);
        Image frame = UIBuild.Sliced(slot, r.slotSprite, Color.white);
        Button btn = slot.AddComponent<Button>();
        btn.targetGraphic = frame;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        btn.colors = cb;

        GameObject icon = UIBuild.Node("Icon", slot.transform);
        UIBuild.Stretch(icon, 0, 0, 1, 1);
        RectTransform ir = (RectTransform)icon.transform;
        ir.offsetMin = new Vector2(11f, 11f); // 48px icon area = 3x the 16px icons
        ir.offsetMax = new Vector2(-11f, -11f);
        Image iconImg = icon.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        iconImg.enabled = false;

        GameObject count = UIBuild.Node("Count", slot.transform);
        UIBuild.Stretch(count, 0, 0, 1, 1);
        RectTransform cr = (RectTransform)count.transform;
        cr.offsetMin = new Vector2(4f, 2f);
        cr.offsetMax = new Vector2(-6f, -4f);
        UIBuild.Label(count, 22, FontStyles.Bold, Color.white, TextAlignmentOptions.BottomRight);

        r.slotTemplate = slot;
        slot.SetActive(false);
        root.SetActive(false);
        return r;
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
        UIBuild.Sliced(box, Resources.Load<Sprite>("UI/panel"), new Color(0.04f, 0.09f, 0.15f, 1f));

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
        UIBuild.Sliced(window, Resources.Load<Sprite>("UI/panel"), new Color(0.04f, 0.09f, 0.15f, 1f));
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
        UIBuild.Sliced(row, Resources.Load<Sprite>("UI/slot"), Color.white);
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

    // A 9-sliced pixel-art frame. Each sprite pixel is drawn 3 screen pixels wide, so borders stay chunky and crisp.
    public static Image Sliced(GameObject go, Sprite sprite, Color tint)
    {
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = tint;
        if (sprite != null)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / 3f;
        }
        return img;
    }

    // A thin outline frame, drawn with UI Outline (no sprite needed).
    public static void Border(GameObject go, Color c)
    {
        Outline o = go.AddComponent<Outline>();
        o.effectColor = c;
        o.effectDistance = new Vector2(3f, -3f);
    }
}
