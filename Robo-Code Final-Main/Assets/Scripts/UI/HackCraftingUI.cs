using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A three-step coding recipe for the Hack ability. It is created at runtime so
// every existing level can open it from a Stone of Recall without scene edits.
public class HackCraftingUI : MonoBehaviour
{
    public static HackCraftingUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    private GameObject root;
    private TextMeshProUGUI stepText;
    private TextMeshProUGUI codeText;
    private TextMeshProUGUI feedbackText;
    private Transform options;
    private Button continueButton;
    private float previousTimeScale = 1f;
    private int step;

    private readonly string[] prompts =
    {
        "Choose the condition that spends exactly one mana cell.",
        "Choose the safe target check before the override is sent.",
        "Choose the cleanup line that stops permanent enemy control."
    };

    private readonly string[] code =
    {
        "const int manaCell = 20;\nif (mana >= ???)\n{\n    mana -= manaCell;\n    SendOverride(target);\n}",
        "BanditEnemy target = FindNearestEnemy();\nif (???)\n{\n    target.ApplyHack(6f);\n}",
        "IEnumerator ReleaseControl(BanditEnemy target)\n{\n    yield return new WaitForSeconds(6f);\n    ???\n}"
    };

    private readonly string[][] answers =
    {
        new[] { "manaCell", "0", "maxMana" },
        new[] { "target != null", "target.IsDead", "target == player" },
        new[] { "target.ReleaseHack();", "Destroy(target);", "mana = 0;" }
    };

    private readonly int[] correct = { 0, 0, 0 };

    public static HackCraftingUI Ensure()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("[Hack Crafting]");
        return go.AddComponent<HackCraftingUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Build();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (IsOpen) Time.timeScale = previousTimeScale;
            IsOpen = false;
            Instance = null;
        }
    }

    public void Open()
    {
        if (!HackProgress.HasFragment || HackProgress.IsCrafted || root == null) return;
        step = 0;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        root.SetActive(true);
        IsOpen = true;
        ShowStep();
    }

    public void Close()
    {
        if (!IsOpen) return;
        root.SetActive(false);
        IsOpen = false;
        Time.timeScale = previousTimeScale;
        UIBusy.MarkClosed();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void ShowStep()
    {
        stepText.text = $"ASSEMBLY {step + 1} / {prompts.Length}  •  {prompts[step]}";
        codeText.text = code[step];
        feedbackText.text = "";
        continueButton.gameObject.SetActive(false);

        foreach (Transform child in options) Destroy(child.gameObject);
        for (int i = 0; i < answers[step].Length; i++)
        {
            int choice = i;
            Button b = UIBuild.Button(options, "Option" + i, answers[step][i], 24, 54);
            b.onClick.AddListener(() => Choose(choice));
        }
    }

    private void Choose(int choice)
    {
        if (choice != correct[step])
        {
            feedbackText.text = "<color=#FF7777>That introduces another bug. Trace the values and try again.</color>";
            return;
        }

        feedbackText.text = "<color=#7CFC9A>Compiled successfully.</color>";
        foreach (Button b in options.GetComponentsInChildren<Button>()) b.interactable = false;
        continueButton.gameObject.SetActive(true);
    }

    private void Continue()
    {
        step++;
        if (step < prompts.Length) { ShowStep(); return; }

        HackProgress.Craft();
        stepText.text = "HACK ABILITY CRAFTED";
        codeText.text = "Press Q to spend one blue mana cell.\nIn battle, a successful Hack damages and controls the enemy's next turn.";
        feedbackText.text = "<color=#7FD4FF>High risk, high reward: failed overrides leave you exposed.</color>";
        foreach (Transform child in options) Destroy(child.gameObject);
        continueButton.GetComponentInChildren<TextMeshProUGUI>().text = "EQUIP HACK";
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(Close);
    }

    private void Build()
    {
        Canvas canvas = UIBuild.Canvas(transform, "HackCraftingCanvas", 120);
        root = UIBuild.Node("Root", canvas.transform);
        UIBuild.Stretch(root, 0, 0, 1, 1);
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);

        GameObject window = UIBuild.Node("Window", root.transform);
        RectTransform wr = (RectTransform)window.transform;
        wr.anchorMin = wr.anchorMax = new Vector2(0.5f, 0.5f);
        wr.sizeDelta = new Vector2(820f, 0f);
        UIBuild.Sliced(window, Resources.Load<Sprite>("UI/panel"), new Color(0.035f, 0.085f, 0.13f, 1f));
        VerticalLayoutGroup layout = window.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(38, 38, 30, 30);
        layout.spacing = 15;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        window.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI title = UIBuild.Label(UIBuild.Node("Title", window.transform), 42, FontStyles.Bold,
            new Color(0.45f, 0.9f, 1f), TextAlignmentOptions.Center);
        title.text = "MAGITEK FRAGMENT • CRAFT HACK";
        stepText = UIBuild.Label(UIBuild.Node("Step", window.transform), 23, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);

        GameObject codeBox = UIBuild.Node("Code", window.transform);
        codeBox.AddComponent<LayoutElement>().preferredHeight = 190f;
        UIBuild.Sliced(codeBox, Resources.Load<Sprite>("UI/panel"), new Color(0.025f, 0.035f, 0.055f, 1f));
        GameObject codeLabel = UIBuild.Node("CodeText", codeBox.transform);
        UIBuild.Stretch(codeLabel, 0, 0, 1, 1);
        codeText = UIBuild.Label(codeLabel, 25, FontStyles.Normal, new Color(0.75f, 0.9f, 1f), TextAlignmentOptions.MidlineLeft);
        codeText.margin = new Vector4(28, 18, 28, 18);

        GameObject optionRoot = UIBuild.Node("Options", window.transform);
        VerticalLayoutGroup choices = optionRoot.AddComponent<VerticalLayoutGroup>();
        choices.spacing = 9;
        choices.childControlWidth = choices.childControlHeight = true;
        choices.childForceExpandWidth = true;
        choices.childForceExpandHeight = false;
        options = optionRoot.transform;

        feedbackText = UIBuild.Label(UIBuild.Node("Feedback", window.transform), 22, FontStyles.Normal,
            Color.white, TextAlignmentOptions.Center);
        continueButton = UIBuild.Button(window.transform, "Continue", "CONTINUE", 25, 56);
        continueButton.onClick.AddListener(Continue);

        Button later = UIBuild.Button(window.transform, "Later", "CRAFT LATER  [ESC]", 21, 48);
        later.onClick.AddListener(Close);
        root.SetActive(false);
    }
}
