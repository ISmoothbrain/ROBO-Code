using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Crafting the HACK ability. Beating the Magitek drops a Hack Fragment; using it from the backpack
// opens this window. Fix the hack script one line at a time. One wrong fix and it fails to compile,
// so you start again (the fragment isn't used up). Fix every line and Hack is unlocked for battles.
public static class HackCrafting
{
    public static bool IsOpen { get; private set; }
    public const string FragmentName = "Hack Fragment";

    private class Step
    {
        public string line;          // with ____ for the blank
        public string[] options;     // first one is correct (they're shuffled on screen)
        public string why;           // shown when a wrong option is picked
        public Step(string l, string[] o, string w) { line = l; options = o; why = w; }
    }

    private static readonly Step[] Steps =
    {
        new Step("def hack(target____", new[] { "):", ")", "];", "){" },
                 "A function header ends with ): in Python. No colon, no function."),
        new Step("    if target.firewall ____ 0:", new[] { "<=", "=<", "=", "=>" },
                 "\"Less than or equal\" is written <= in Python. A single = assigns, it doesn't compare."),
        new Step("        for port in ____(3):", new[] { "range", "len", "list", "loop" },
                 "range(3) gives 0, 1, 2. len(3) and list(3) crash, and loop() doesn't exist."),
        new Step("            target.ports[port] ____ \"open\"", new[] { "=", "==", ":", "=>" },
                 "One = stores the value. == only compares, so the port would never open."),
        new Step("    ____ target", new[] { "return", "print", "break", "send" },
                 "return hands the hacked target back. The others aren't valid here."),
    };

    private static InventoryItem fragment;
    public static InventoryItem Fragment
    {
        get
        {
            if (fragment == null)
            {
                fragment = ScriptableObject.CreateInstance<InventoryItem>();
                fragment.itemName = FragmentName;
                fragment.icon = Resources.Load<Sprite>("Icons/hack_fragment");
                fragment.description = "A shard of the Magitek's core. Use it to craft the HACK ability by fixing its code.";
                fragment.isStackable = false;
            }
            return fragment;
        }
    }

    // Puts the fragment in the backpack if it's saved but not there (e.g. after quitting the game).
    public static void EnsureFragmentInBackpack()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null || !GameSave.HasHackFragment || GameSave.HackUnlocked) return;
        foreach (InventorySlot s in inv.slots) if (s.item != null && s.item.itemName == FragmentName) return;
        inv.AddItem(Fragment, 1);
    }

    // ------------------------------------------------------------------ the window

    private static GameObject root;
    private static TextMeshProUGUI codeText, infoText;
    private static Button[] optionButtons;
    private static int step;
    private static int[] order = new int[4];

    public static void Open()
    {
        if (GameSave.HackUnlocked) return;
        if (root == null) Build();
        step = 0;
        infoText.text = "Fix each line of the hack script. One mistake and it won't compile.";
        ShowStep();
        root.SetActive(true);
        IsOpen = true;
    }

    private static void Close()
    {
        if (root != null) root.SetActive(false);
        IsOpen = false;
        UIBusy.MarkClosed();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private static void ShowStep()
    {
        // the whole script: fixed lines in green, the current line highlighted, the rest still broken
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < Steps.Length; i++)
        {
            string l = Steps[i].line.Replace("<", "<noparse><</noparse>");
            if (i < step) sb.Append("<color=#7CFF9A>").Append(l.Replace("____", Steps[i].options[0].Replace("<", "<noparse><</noparse>"))).Append("</color>");
            else if (i == step) sb.Append("<color=#FFD966>> ").Append(l.Replace("____", "<u>____</u>")).Append("</color>");
            else sb.Append("<color=#7A8494>").Append(l).Append("</color>");
            sb.Append('\n');
        }
        codeText.text = sb.ToString();

        // shuffle the four options
        for (int i = 0; i < 4; i++) order[i] = i;
        for (int i = 3; i > 0; i--) { int j = Random.Range(0, i + 1); (order[i], order[j]) = (order[j], order[i]); }
        for (int i = 0; i < 4; i++)
        {
            int pick = order[i];
            optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = Steps[step].options[pick];
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => Choose(pick));
        }
    }

    private static void Choose(int option)
    {
        if (option != 0)
        {
            infoText.text = $"<color=#FF7A7A>Compile error!</color> {Steps[step].why}\nStart again from the first line.";
            step = 0;
            ShowStep();
            return;
        }
        step++;
        if (step < Steps.Length) { infoText.text = "Line fixed! Next line."; ShowStep(); return; }

        // all lines fixed: Hack is crafted
        GameSave.HackUnlocked = true;
        GameSave.HasHackFragment = false;
        if (InventoryManager.Instance != null) InventoryManager.Instance.RemoveItem(Fragment, 1);
        Close();
        if (DialogueUI.Instance != null)
            DialogueUI.Instance.Show("Crafting", new[]
            {
                "HACK ability crafted!",
                "In battle, on your turn, choose HACK (1 energy bar):",
                "OVERRIDE takes control of the enemy so it loses its next turn. OVERLOAD makes it take double damage and deal half.",
                "Hacking asks a hard question. Fail it and the hack is traced back to you, for double damage."
            });
    }

    private static void Build()
    {
        Canvas canvas = UIBuild.Canvas(null, "HackCraftingWindow", 110);
        Object.DontDestroyOnLoad(canvas.gameObject);
        root = UIBuild.Node("Root", canvas.transform);
        UIBuild.Stretch(root, 0, 0, 1, 1);
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        GameObject window = UIBuild.Node("Window", root.transform);
        RectTransform wr = (RectTransform)window.transform;
        wr.anchorMin = wr.anchorMax = new Vector2(0.5f, 0.5f);
        wr.sizeDelta = new Vector2(1100f, 0f);
        UIBuild.Sliced(window, Resources.Load<Sprite>("UI/panel"), new Color(0.04f, 0.09f, 0.15f, 1f));
        VerticalLayoutGroup v = window.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(36, 36, 26, 28);
        v.spacing = 14;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        window.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        UIBuild.Label(UIBuild.Node("Title", window.transform), 40, FontStyles.Bold, new Color(0.55f, 0.88f, 1f), TextAlignmentOptions.Center).text = "Craft: HACK";

        GameObject code = UIBuild.Node("Code", window.transform);
        code.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.07f, 1f);
        codeText = UIBuild.Label(UIBuild.Node("Text", code.transform), 28, FontStyles.Normal, Color.white, TextAlignmentOptions.TopLeft);
        RectTransform cr = codeText.rectTransform;
        cr.anchorMin = Vector2.zero; cr.anchorMax = Vector2.one; cr.offsetMin = new Vector2(20f, 14f); cr.offsetMax = new Vector2(-20f, -14f);
        codeText.richText = true;
        code.AddComponent<LayoutElement>().preferredHeight = 260f;

        GameObject row = UIBuild.Node("Options", window.transform);
        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 12;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = true;
        optionButtons = new Button[4];
        for (int i = 0; i < 4; i++) optionButtons[i] = UIBuild.Button(row.transform, "Option" + i, "", 30, 64);

        infoText = UIBuild.Label(UIBuild.Node("Info", window.transform), 24, FontStyles.Normal, new Color(0.8f, 0.86f, 0.92f), TextAlignmentOptions.Center);
        infoText.richText = true;
        Button close = UIBuild.Button(window.transform, "Close", "Close  [Esc]", 24, 52);
        close.onClick.AddListener(Close);

        canvas.gameObject.AddComponent<HackCraftingEscKey>();
        root.SetActive(false);
    }

    public static void CloseFromKey() => Close();
}

// Esc closes the crafting window.
public class HackCraftingEscKey : MonoBehaviour
{
    private void Update()
    {
        if (HackCrafting.IsOpen && Input.GetKeyDown(KeyCode.Escape)) HackCrafting.CloseFromKey();
    }
}
