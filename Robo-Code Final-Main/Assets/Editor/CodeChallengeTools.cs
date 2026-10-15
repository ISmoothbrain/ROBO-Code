#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Must live in a folder named "Editor" (e.g. Assets/Editor/CodeChallengeTools.cs).
public static class CodeChallengeTools
{
    private const string Folder = "Assets/CodeChallenges";

    // ================================================================ sample content

    [MenuItem("Tools/Code Challenges/Create Sample Challenges")]
    private static void CreateSamples()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "CodeChallenges");

        SkillData power = Skill("Skill_PowerStrike", "Power Strike",
            "One heavy blow. Crafted by fixing a broken if statement.", 35, 1, 0f, 1.2f, 20, 1.5f);
        SkillData triple = Skill("Skill_TripleSlash", "Triple Slash",
            "Three quick hits. Crafted by fixing a loop.", 12, 3, 0.15f, 1f, 25, 2f);
        SkillData whirl = Skill("Skill_Whirlwind", "Whirlwind",
            "Spin and hit everything nearby. Crafted by mastering functions.", 8, 5, 0.12f, 2f, 40, 4f);

        CodeChallenge c1 = Challenge("01_Variables", "Lesson 1: Variables",
            "A variable is a named box that stores a value. 'int' means the box holds a whole number.\n" +
            "You can change what's inside later: 'health = health - 25' takes the old value, subtracts 25, and stores the result back.",
            ChallengeType.MultipleChoice,
            "What number does this code print?",
            new[] { "int health = 100;", "health = health - 25;", "Debug.Log(health);" },
            new[] { "125", "75", "25", "100" }, 1,
            "Start at 100, then follow the second line.",
            "health starts at 100. Line 2 subtracts 25 and stores it back, so it prints 75.",
            null);

        CodeChallenge c2 = Challenge("02_IfStatements", "Lesson 2: If Statements",
            "An 'if' runs code only when a condition is true. To COMPARE values use '==', '<', '>', '<=' or '>='.\n" +
            "A single '=' doesn't compare. It ASSIGNS a new value, which is a very common bug.",
            ChallengeType.FindTheBug,
            "The player should die when health runs out, but never does. Click the buggy line.",
            new[] { "void TakeDamage(int amount)", "{", "    health -= amount;", "    if (health = 0)", "        Die();", "}" },
            null, 3,
            "Look closely at the symbols inside the if.",
            "'health = 0' assigns instead of comparing. It should be 'if (health <= 0)', which also catches health going below zero.",
            power, c1);

        CodeChallenge c3 = Challenge("03_Loops", "Lesson 3: Loops",
            "A 'for' loop repeats code. 'for (int i = 0; i < 3; i++)' starts i at 0, runs while i is less than 3, " +
            "and adds 1 each time. That's 3 runs: i = 0, 1 and 2.",
            ChallengeType.FindTheBug,
            "This attack should hit 3 times, but it only hits once. Click the buggy line.",
            new[] { "void TripleSlash()", "{", "    for (int i = 0; i < 1; i++)", "    {", "        Hit();", "    }", "}" },
            null, 2,
            "How many times does the loop run before the condition becomes false?",
            "'i < 1' only allows i = 0, so it runs once. 'i < 3' runs three times.",
            triple, c2);

        CodeChallenge c4 = Challenge("04_Functions", "Lesson 4: Functions",
            "A function is reusable code with a name. Values you pass in are called parameters, and 'return' sends a result back.\n" +
            "Damage(10, 3) runs the function with baseDmg = 10 and combo = 3.",
            ChallengeType.MultipleChoice,
            "What is the value of total?",
            new[] { "int Damage(int baseDmg, int combo)", "{", "    return baseDmg * combo;", "}", "", "int total = Damage(10, 3);" },
            new[] { "13", "30", "103", "10" }, 1,
            "'*' means multiply.",
            "baseDmg is 10 and combo is 3, so the function returns 10 * 3 = 30.",
            whirl, c3);

        AssetDatabase.SaveAssets();

        ProgressManager pm = Object.FindFirstObjectByType<ProgressManager>();
        if (pm != null)
        {
            Undo.RecordObject(pm, "Assign challenges");
            pm.allChallenges = new[] { c1, c2, c3, c4 };
            EditorUtility.SetDirty(pm);
        }

        Debug.Log($"Created 4 lessons and 3 skills in {Folder}." +
                  (pm != null ? " Assigned them to the ProgressManager in this scene."
                              : " Add a ProgressManager to the scene and drag all 4 challenges into All Challenges."));
        Selection.activeObject = c1;
    }

    private static SkillData Skill(string file, string skillName, string desc, int dmg, int hits,
                                   float gap, float radius, int mana, float cooldown)
    {
        SkillData s = LoadOrCreate<SkillData>(file);
        s.skillName = skillName; s.description = desc; s.damagePerHit = dmg; s.hitCount = hits;
        s.timeBetweenHits = gap; s.radiusMultiplier = radius; s.manaCost = mana; s.cooldown = cooldown;
        EditorUtility.SetDirty(s);
        return s;
    }

    private static CodeChallenge Challenge(string file, string title, string lesson, ChallengeType type,
        string question, string[] code, string[] options, int correct, string hint, string explanation,
        SkillData reward, params CodeChallenge[] prereqs)
    {
        CodeChallenge c = LoadOrCreate<CodeChallenge>(file);
        c.id = file; c.title = title; c.lesson = lesson; c.type = type; c.question = question;
        c.codeLines = code; c.options = options; c.correctIndex = correct; c.hint = hint;
        c.explanation = explanation; c.rewardSkill = reward; c.prerequisites = prereqs;
        EditorUtility.SetDirty(c);
        return c;
    }

    private static T LoadOrCreate<T>(string file) where T : ScriptableObject
    {
        string path = $"{Folder}/{file}.asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    // ================================================================ UI builder

    [MenuItem("Tools/Code Challenges/Create Challenge UI")]
    private static void CreateUI()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas in the open scene. Open your level scene first.");
            return;
        }

        GameObject root = new GameObject("CodeChallengeUI", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(root, "Create Code Challenge UI");
        root.transform.SetParent(canvas.transform, false);
        Stretch(root, 0, 0);
        root.transform.SetAsLastSibling(); // draw on top of the HUD
        CodeChallengeUI ui = root.AddComponent<CodeChallengeUI>();

        // Dark full-screen backdrop + centred window
        GameObject panel = Child(root, "Panel");
        Stretch(panel, 0, 0);
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        GameObject window = Child(panel, "Window");
        RectTransform wr = (RectTransform)window.transform;
        wr.anchorMin = new Vector2(0.15f, 0.08f);
        wr.anchorMax = new Vector2(0.85f, 0.92f);
        wr.offsetMin = wr.offsetMax = Vector2.zero;
        window.AddComponent<Image>().color = new Color(0.1f, 0.11f, 0.15f, 0.97f);
        VerticalLayoutGroup v = window.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(32, 32, 28, 28);
        v.spacing = 14;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        ui.panel = panel;
        ui.titleText = Text(window, "Title", 36, FontStyles.Bold, new Color(0.5f, 0.85f, 1f));
        ui.lessonText = Text(window, "Lesson", 22, FontStyles.Normal, new Color(0.85f, 0.87f, 0.9f));
        ui.questionText = Text(window, "Question", 24, FontStyles.Bold, Color.white);

        ui.codeLineContainer = Group(window, "CodeLines", true, 4).transform;
        ui.optionContainer = Group(window, "Options", false, 10).transform;

        ui.feedbackText = Text(window, "Feedback", 22, FontStyles.Normal, new Color(1f, 0.9f, 0.6f));

        GameObject buttons = Group(window, "Buttons", false, 10);
        ui.hintButton = MakeButton(buttons, "Hint", "Hint");
        ui.continueButton = MakeButton(buttons, "Continue", "Continue");
        ui.closeButton = MakeButton(buttons, "Close", "Close (Esc)");

        // Template for code lines and answer options (cloned at runtime, kept hidden)
        ui.buttonTemplate = MakeButton(root, "ButtonTemplate", "line");
        ui.buttonTemplate.gameObject.SetActive(false);

        panel.SetActive(false);
        EditorUtility.SetDirty(ui);
        Selection.activeGameObject = root;
        Debug.Log("Created CodeChallengeUI under the Canvas. Restyle it however you like. Fonts, colours and sizes are all normal UI.");
    }

    private static GameObject Child(GameObject parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static void Stretch(GameObject go, float x, float y)
    {
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(x, y); r.offsetMax = new Vector2(-x, -y);
    }

    private static TextMeshProUGUI Text(GameObject parent, string name, float size, FontStyles style, Color color)
    {
        GameObject go = Child(parent, name);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size; t.fontStyle = style; t.color = color; t.text = name;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    private static GameObject Group(GameObject parent, string name, bool vertical, float spacing)
    {
        GameObject go = Child(parent, name);
        HorizontalOrVerticalLayoutGroup g = vertical
            ? (HorizontalOrVerticalLayoutGroup)go.AddComponent<VerticalLayoutGroup>()
            : go.AddComponent<HorizontalLayoutGroup>();
        g.spacing = spacing;
        g.childControlWidth = g.childControlHeight = true;
        g.childForceExpandWidth = true;
        g.childForceExpandHeight = false;
        return go;
    }

    private static Button MakeButton(GameObject parent, string name, string label)
    {
        GameObject go = Child(parent, name);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.2f, 0.26f);
        Button b = go.AddComponent<Button>();
        b.targetGraphic = img;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = 44;

        GameObject textGo = Child(go, "Text");
        Stretch(textGo, 14, 4);
        TextMeshProUGUI t = textGo.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 22; t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        return b;
    }
}
#endif
