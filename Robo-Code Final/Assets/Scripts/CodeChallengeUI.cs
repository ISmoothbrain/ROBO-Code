using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The lesson + puzzle window. Build it automatically with
// Tools > Code Challenges > Create Challenge UI, or wire the fields by hand.
// Keep THIS object active in the scene; only the Panel gets shown and hidden.
public class CodeChallengeUI : MonoBehaviour
{
    public static CodeChallengeUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [Header("References")]
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI lessonText;
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI feedbackText;
    public Transform codeLineContainer;
    public Transform optionContainer;
    [Tooltip("An inactive Button with a TextMeshProUGUI child. Cloned for every code line and option.")]
    public Button buttonTemplate;
    public Button hintButton;
    public Button continueButton;
    public Button closeButton;

    [Header("Colours")]
    public Color normalColor = new Color(0.18f, 0.2f, 0.26f);
    public Color wrongColor = new Color(0.7f, 0.2f, 0.2f);
    public Color rightColor = new Color(0.2f, 0.6f, 0.3f);

    private CodeChallenge current;
    private bool solved;
    private float previousTimeScale = 1f;
    private readonly List<Button> answerButtons = new List<Button>();

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
        if (buttonTemplate != null) buttonTemplate.gameObject.SetActive(false);

        if (hintButton != null) hintButton.onClick.AddListener(ShowHint);
        if (continueButton != null) continueButton.onClick.AddListener(Close);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
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

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    public void Open(CodeChallenge challenge)
    {
        if (challenge == null || panel == null) return;

        current = challenge;
        solved = false;

        panel.SetActive(true);
        IsOpen = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f; // freeze the world while the player reads and thinks

        Build();
    }

    public void Close()
    {
        if (!IsOpen) return;
        panel.SetActive(false);
        IsOpen = false;
        Time.timeScale = previousTimeScale;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void Build()
    {
        SetText(titleText, current.title);
        SetText(lessonText, current.lesson);
        SetText(questionText, current.question);
        SetText(feedbackText, "");

        if (hintButton != null) hintButton.gameObject.SetActive(!string.IsNullOrEmpty(current.hint));
        if (continueButton != null) continueButton.gameObject.SetActive(false);

        Clear(codeLineContainer);
        Clear(optionContainer);
        answerButtons.Clear();

        bool linesAreAnswers = current.type == ChallengeType.FindTheBug;
        string[] lines = current.codeLines ?? new string[0];

        for (int i = 0; i < lines.Length; i++)
        {
            string label = $"<color=#7f8a99>{i + 1,2}</color>  {lines[i]}";
            Button b = MakeButton(codeLineContainer, label, TextAlignmentOptions.Left);
            if (linesAreAnswers)
            {
                int index = i;
                b.onClick.AddListener(() => Check(index, b));
                answerButtons.Add(b);
            }
            else
            {
                b.interactable = false; // just displaying the code
                b.transition = Selectable.Transition.None; // so it isn't drawn faded
            }
        }

        if (codeLineContainer != null) codeLineContainer.gameObject.SetActive(lines.Length > 0);

        bool hasOptions = current.type == ChallengeType.MultipleChoice && current.options != null;
        if (optionContainer != null) optionContainer.gameObject.SetActive(hasOptions);
        if (hasOptions)
        {
            for (int i = 0; i < current.options.Length; i++)
            {
                int index = i;
                Button b = MakeButton(optionContainer, current.options[i], TextAlignmentOptions.Center);
                b.onClick.AddListener(() => Check(index, b));
                answerButtons.Add(b);
            }
        }
    }

    private void Check(int index, Button clicked)
    {
        if (solved) return;

        if (index == current.correctIndex)
        {
            solved = true;
            Tint(clicked, rightColor);
            foreach (Button b in answerButtons) b.interactable = false;

            string msg = "<b>Correct!</b>";
            if (!string.IsNullOrEmpty(current.explanation)) msg += "\n" + current.explanation;
            if (current.rewardSkill != null)
            {
                msg += $"\n\n<color=#7fd4ff>New skill crafted: <b>{current.rewardSkill.skillName}</b></color>" +
                       "\nPress Q to use it, Tab to switch skills.";
            }
            SetText(feedbackText, msg);

            if (ProgressManager.Instance != null) ProgressManager.Instance.CompleteChallenge(current);
            else Debug.LogWarning("No ProgressManager in the scene — this solve won't be saved.");

            if (continueButton != null) continueButton.gameObject.SetActive(true);
        }
        else
        {
            Tint(clicked, wrongColor);
            clicked.interactable = false;
            SetText(feedbackText, current.type == ChallengeType.FindTheBug
                ? "That line is fine. Read each line carefully and try again."
                : "Not quite. Try again!");
        }
    }

    private void ShowHint()
    {
        if (current != null) SetText(feedbackText, "<i>Hint:</i> " + current.hint);
    }

    // ---------------------------------------------------------------- helpers

    private Button MakeButton(Transform parent, string label, TextAlignmentOptions align)
    {
        Button b = Instantiate(buttonTemplate, parent);
        b.gameObject.SetActive(true);
        b.onClick.RemoveAllListeners();
        Tint(b, normalColor);

        TextMeshProUGUI t = b.GetComponentInChildren<TextMeshProUGUI>(true);
        if (t != null)
        {
            t.text = label;
            t.alignment = align;
        }
        return b;
    }

    private static void Tint(Button b, Color c)
    {
        if (b.targetGraphic != null) b.targetGraphic.color = c;
    }

    private static void SetText(TextMeshProUGUI t, string s)
    {
        if (t == null) return;
        t.text = s ?? "";
        t.gameObject.SetActive(!string.IsNullOrEmpty(s));
    }

    private static void Clear(Transform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--) Destroy(container.GetChild(i).gameObject);
    }
}
