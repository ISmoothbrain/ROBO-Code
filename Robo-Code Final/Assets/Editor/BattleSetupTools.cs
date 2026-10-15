#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Must live in a folder named "Editor". Adds: Tools > Battle System
public static class BattleSetupTools
{
    private const string QuestionFolder = "Assets/BattleQuestions";

    [MenuItem("Tools/Battle System/Set Up Battle In Open Scene")]
    private static void SetUp()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found. Open your level scene first.");
            return;
        }
        if (Object.FindFirstObjectByType<BattleManager>() != null)
        {
            Debug.LogWarning("This scene already has a BattleManager. Delete it (and BattleUI / BattleCamera / BattleFocus) first to rebuild.");
            return;
        }

        PythonQuestion[] questions = CreateQuestions();

        // Manager + camera focus point
        GameObject managerGo = new GameObject("BattleManager");
        Undo.RegisterCreatedObjectUndo(managerGo, "Set Up Battle");
        BattleManager manager = managerGo.AddComponent<BattleManager>();
        manager.defaultQuestions = questions;

        GameObject focus = new GameObject("BattleFocus");
        Undo.RegisterCreatedObjectUndo(focus, "Set Up Battle");
        manager.battleFocus = focus.transform;

        manager.battleCamera = CreateBattleCamera(focus.transform);

        // UI
        manager.ui = BuildBattleUI(canvas.transform);
        manager.qte = BuildQTE(canvas.transform);

        EditorUtility.SetDirty(manager);
        Selection.activeGameObject = managerGo;
        Debug.Log($"Battle system set up with {questions.Length} Python questions. " +
                  "Save the scene, press Play and hit a bandit to start a fight.");
    }

    // ================================================================ camera

    // Duplicates your existing CinemachineCamera, tightens the zoom and raises its priority.
    // Uses SerializedObject so this file compiles whatever Cinemachine version you have.
    private static GameObject CreateBattleCamera(Transform focus)
    {
        MonoBehaviour source = FindByTypeName("CinemachineCamera");
        if (source == null)
        {
            Debug.LogWarning("No CinemachineCamera found, so there's no battle zoom. Add one and assign it to BattleManager > Battle Camera.");
            return null;
        }

        GameObject cam = Object.Instantiate(source.gameObject);
        cam.name = "BattleCamera";
        Undo.RegisterCreatedObjectUndo(cam, "Set Up Battle");

        MonoBehaviour vcam = null;
        foreach (MonoBehaviour m in cam.GetComponents<MonoBehaviour>())
            if (m != null && m.GetType().Name == "CinemachineCamera") vcam = m;

        SerializedObject so = new SerializedObject(vcam);
        SerializedProperty size = so.FindProperty("Lens.OrthographicSize");
        if (size != null) size.floatValue = Mathf.Max(1.5f, size.floatValue * 0.55f);
        SetBool(so, "Priority.Enabled", true);
        SetInt(so, "Priority.m_Value", 20);
        SerializedProperty target = so.FindProperty("Target.TrackingTarget");
        if (target != null) target.objectReferenceValue = focus;
        so.ApplyModifiedPropertiesWithoutUndo();

        cam.SetActive(false);

        // Make the zoom a quick ease rather than a slow 2 second drift.
        MonoBehaviour brain = FindByTypeName("CinemachineBrain");
        if (brain != null)
        {
            SerializedObject bso = new SerializedObject(brain);
            SerializedProperty time = bso.FindProperty("DefaultBlend.Time");
            if (time != null) { time.floatValue = 0.6f; bso.ApplyModifiedProperties(); }
        }

        return cam;
    }

    private static MonoBehaviour FindByTypeName(string typeName)
    {
        foreach (MonoBehaviour m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (m != null && m.GetType().Name == typeName && m.gameObject.name != "BattleCamera") return m;
        return null;
    }

    private static void SetBool(SerializedObject so, string path, bool v) { var p = so.FindProperty(path); if (p != null) p.boolValue = v; }
    private static void SetInt(SerializedObject so, string path, int v) { var p = so.FindProperty(path); if (p != null) p.intValue = v; }

    // ================================================================ battle UI

    private static BattleUI BuildBattleUI(Transform canvas)
    {
        GameObject go = Node("BattleUI", canvas);
        Stretch(go, 0, 0, 1, 1);
        Undo.RegisterCreatedObjectUndo(go, "Set Up Battle");
        BattleUI ui = go.AddComponent<BattleUI>();

        GameObject root = Node("Root", go.transform);
        Stretch(root, 0, 0, 1, 1);
        ui.root = root;

        // Cinematic letterbox bars sell the "entering a battle" moment
        Panel("LetterboxTop", root.transform, 0, 0.92f, 1, 1, Color.black);
        Panel("LetterboxBottom", root.transform, 0, 0, 1, 0.03f, Color.black);

        // Health bars
        HealthBar(root.transform, "PlayerHealth", "YOU", 0.02f, 0.38f, new Color(0.35f, 0.85f, 0.45f),
                  out ui.playerHealthFill, out ui.playerHealthText, out _);
        HealthBar(root.transform, "EnemyHealth", "ENEMY", 0.62f, 0.98f, new Color(0.9f, 0.3f, 0.3f),
                  out ui.enemyHealthFill, out ui.enemyHealthText, out ui.enemyNameText);

        // Banner
        GameObject banner = Node("Banner", root.transform);
        Stretch(banner, 0.1f, 0.62f, 0.9f, 0.82f);
        ui.bannerText = Label(banner, 54, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        ui.bannerText.outlineWidth = 0.25f;
        ui.bannerText.outlineColor = Color.black;

        // Question panel (bottom of the screen)
        GameObject panel = Panel("QuestionPanel", root.transform, 0.08f, 0.04f, 0.92f, 0.46f, new Color(0.06f, 0.07f, 0.1f, 0.94f));
        ui.questionPanel = panel;
        VerticalLayoutGroup v = panel.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(28, 28, 18, 18);
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
        cv.childForceExpandWidth = true; cv.childForceExpandHeight = false;
        ui.codeText = Label(Node("Code", codeBox.transform), 28, FontStyles.Normal, new Color(0.9f, 0.92f, 0.95f), TextAlignmentOptions.TopLeft);

        GameObject options = Node("Options", panel.transform);
        HorizontalLayoutGroup h = options.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 12;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = true; h.childForceExpandHeight = false;
        ui.optionContainer = options.transform;

        GameObject timerBg = Node("Timer", panel.transform);
        timerBg.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
        timerBg.AddComponent<LayoutElement>().minHeight = 8;
        GameObject timerFill = Panel("Fill", timerBg.transform, 0, 0, 1, 1, new Color(1f, 0.83f, 0.3f));
        ui.timerFill = (RectTransform)timerFill.transform;

        ui.explanationText = Label(Node("Explanation", panel.transform), 22, FontStyles.Normal, new Color(0.88f, 0.88f, 0.88f), TextAlignmentOptions.Left);

        // Option button template (hidden, cloned per answer)
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
        Undo.RegisterCreatedObjectUndo(go, "Set Up Battle");
        QuickTimeEvent qte = go.AddComponent<QuickTimeEvent>();

        GameObject root = Node("Root", go.transform);
        Stretch(root, 0, 0, 1, 1);
        qte.root = root;

        qte.targetRing = Ring(root.transform, "TargetRing", new Color(1f, 1f, 1f, 0.9f));
        qte.closingRing = Ring(root.transform, "ClosingRing", Color.white);

        GameObject key = Node("Key", root.transform);
        Centre(key, 120);
        qte.keyText = Label(key, 72, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        qte.keyText.text = "E";
        qte.keyText.outlineWidth = 0.2f; qte.keyText.outlineColor = Color.black;

        GameObject result = Node("Result", root.transform);
        RectTransform rr = (RectTransform)result.transform;
        rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f);
        rr.sizeDelta = new Vector2(500, 80);
        rr.anchoredPosition = new Vector2(0, -150);
        qte.resultText = Label(result, 48, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        qte.resultText.outlineWidth = 0.25f; qte.resultText.outlineColor = Color.black;

        root.SetActive(false);
        return qte;
    }

    // ================================================================ UI helpers

    private static GameObject Node(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(GameObject go, float x0, float y0, float x1, float y1)
    {
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = new Vector2(x0, y0); r.anchorMax = new Vector2(x1, y1);
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
        t.fontSize = size; t.fontStyle = style; t.color = c; t.alignment = align;
        t.text = "";
        return t;
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
        GameObject f = Panel("Fill", bg.transform, 0, 0, 1, 1, fillColor);
        fill = (RectTransform)f.transform;

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
        return img; // QuickTimeEvent generates the ring sprite at runtime
    }

    // ================================================================ Python questions

    private static PythonQuestion[] CreateQuestions()
    {
        if (!AssetDatabase.IsValidFolder(QuestionFolder)) AssetDatabase.CreateFolder("Assets", "BattleQuestions");
        List<PythonQuestion> list = new List<PythonQuestion>();

        list.Add(Q("Loop_Range", "Make the attack hit 3 times.",
            new[] { "for i in range(____):", "    attack()" },
            new[] { "3", "2", "4", "\"3\"" }, 0,
            "range(3) gives 0, 1, 2, which is three runs. range(n) always runs n times."));

        list.Add(Q("If_Compare", "The hero should faint when HP runs out.",
            new[] { "if hp ____ 0:", "    faint()" },
            new[] { "<=", "=", "=>", "=!" }, 0,
            "'<=' means 'less than or equal to'. '=' assigns a value, and '=>' isn't valid Python."));

        list.Add(Q("Def_Return", "Send the doubled damage back to whoever called the function.",
            new[] { "def double(dmg):", "    ____ dmg * 2" },
            new[] { "return", "print", "give", "send" }, 0,
            "'return' hands a value back to the caller. 'print' only shows it on screen."));

        list.Add(Q("Print_Var", "Print the hero's name, not the word 'name'.",
            new[] { "name = \"Robo\"", "print(____)" },
            new[] { "name", "\"name\"", "Name", "print" }, 0,
            "Without quotes, name means the variable. With quotes it's just the text \"name\". Python is case-sensitive, so Name is a different variable."));

        list.Add(Q("List_Index", "Equip the FIRST item in the bag.",
            new[] { "bag = [\"sword\", \"shield\", \"potion\"]", "equip(bag[____])" },
            new[] { "0", "1", "3", "first" }, 0,
            "Python lists start counting at 0, so bag[0] is \"sword\"."));

        list.Add(Q("While_Alive", "Keep fighting while the enemy still has HP.",
            new[] { "while enemy_hp ____ 0:", "    enemy_hp -= 10" },
            new[] { ">", "<", "==", "=" }, 0,
            "'> 0' stays true while HP is positive. '< 0' would never start, and '== 0' would only run once HP is exactly zero."));

        list.Add(Q("Plus_Equals", "Add 5 to the score so it prints 15.",
            new[] { "score = 10", "score ____ 5", "print(score)  # 15" },
            new[] { "+=", "=", "-=", "==" }, 0,
            "'score += 5' is short for 'score = score + 5'."));

        list.Add(Q("Len_List", "Count how many coins are in the list.",
            new[] { "coins = [1, 1, 5]", "print(____(coins))  # 3" },
            new[] { "len", "size", "count", "length" }, 0,
            "len() returns how many items something has. Python has no built-in size() or length()."));

        list.Add(Q("Bool_True", "Mark the hero as alive.",
            new[] { "is_alive = ____", "if is_alive:", "    keep_playing()" },
            new[] { "True", "true", "TRUE", "yes" }, 0,
            "Python's booleans are True and False with a capital first letter. 'true' would be an undefined variable."));

        list.Add(Q("Heal_Plus", "Healing should ADD 20 HP.",
            new[] { "def heal(hp):", "    return hp ____ 20" },
            new[] { "+", "-", "*", "/" }, 0,
            "Healing increases HP, so add with '+'."));

        list.Add(Q("Elif_Branch", "Only check the second condition if the first one was false.",
            new[] { "if hp > 50:", "    print(\"Healthy\")", "____ hp > 0:", "    print(\"Hurt\")" },
            new[] { "elif", "else if", "else", "if else" }, 0,
            "Python spells it 'elif'. 'else if' works in C#, but not in Python."));

        list.Add(Q("Fstring", "Show the damage number inside the message.",
            new[] { "dmg = 25", "print(____\"Hit for {dmg}!\")" },
            new[] { "f", "s", "$", "%" }, 0,
            "An f-string (f\"...\") fills in {dmg} with the variable's value. C# uses $ for the same idea.", 1.5f));

        AssetDatabase.SaveAssets();
        return list.ToArray();
    }

    private static PythonQuestion Q(string file, string goal, string[] code, string[] options, int correct,
                                    string explanation, float damageMultiplier = 1f)
    {
        string path = $"{QuestionFolder}/{file}.asset";
        PythonQuestion q = AssetDatabase.LoadAssetAtPath<PythonQuestion>(path);
        if (q == null)
        {
            q = ScriptableObject.CreateInstance<PythonQuestion>();
            AssetDatabase.CreateAsset(q, path);
        }
        q.goal = goal; q.codeLines = code; q.options = options; q.correctIndex = correct;
        q.explanation = explanation; q.damageMultiplier = damageMultiplier;
        EditorUtility.SetDirty(q);
        return q;
    }
}
#endif
