using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Everything on screen during a battle except the quick-time event.
// Keep THIS object active; only Root is shown and hidden.
public class BattleUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("Health")]
    public RectTransform playerHealthFill; // bars shrink by changing anchorMax.x, so no sprite setup is needed
    public RectTransform enemyHealthFill;
    public TextMeshProUGUI playerHealthText;
    public TextMeshProUGUI enemyHealthText;
    public TextMeshProUGUI enemyNameText;

    [Header("Banner")]
    public TextMeshProUGUI bannerText;
    public float bannerDuration = 1.4f;

    [Header("Question")]
    public GameObject questionPanel;
    public TextMeshProUGUI goalText;
    public TextMeshProUGUI codeText;
    public Transform optionContainer;
    public Button optionTemplate;
    public RectTransform timerFill;
    public TextMeshProUGUI explanationText;

    [Header("Colours")]
    public Color optionColor = new Color(0.16f, 0.19f, 0.27f);
    public Color correctColor = new Color(0.2f, 0.62f, 0.32f);
    public Color wrongColor = new Color(0.72f, 0.2f, 0.2f);

    private readonly List<Button> optionButtons = new List<Button>();
    private readonly List<int> buttonToOption = new List<int>(); // options are shuffled on screen
    private Coroutine bannerRoutine;

    private static readonly Regex Keywords = new Regex(
        @"\b(def|return|if|elif|else|for|while|in|not|and|or|True|False|None|import|from|class|break|continue|pass)\b");
    private static readonly Regex Builtins = new Regex(@"\b(print|range|len|int|str|float|list|input)\b");
    private static readonly Regex Numbers = new Regex(@"\b\d+\b");

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        if (optionTemplate != null) optionTemplate.gameObject.SetActive(false);
    }

    public void Show(string enemyName)
    {
        root.SetActive(true);
        if (questionPanel != null) questionPanel.SetActive(false);
        if (bannerText != null) bannerText.gameObject.SetActive(false);
        if (enemyNameText != null) enemyNameText.text = enemyName.Replace("(Clone)", "").Trim();
    }

    public void Hide()
    {
        root.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void RefreshHealth(int playerHp, int playerMax, int enemyHp, int enemyMax)
    {
        SetBar(playerHealthFill, playerHealthText, playerHp, playerMax);
        SetBar(enemyHealthFill, enemyHealthText, enemyHp, enemyMax);
    }

    public void ShowBanner(string message)
    {
        if (bannerText == null) return;
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine(message));
    }

    public void SetTimer(float fraction)
    {
        if (timerFill != null) timerFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    // ---------------------------------------------------------------- question

    public void ShowQuestion(PythonQuestion q, Action<int> onAnswer)
    {
        questionPanel.SetActive(true);
        if (goalText != null) goalText.text = q.goal;
        codeText.text = RenderCode(q, null, false);
        if (explanationText != null) explanationText.gameObject.SetActive(false);
        SetTimer(1f);

        for (int i = optionContainer.childCount - 1; i >= 0; i--) Destroy(optionContainer.GetChild(i).gameObject);
        optionButtons.Clear();
        buttonToOption.Clear();

        if (q.options == null) return;

        // Shuffle display order so the right answer isn't always in the same spot.
        for (int i = 0; i < q.options.Length; i++) buttonToOption.Add(i);
        for (int i = buttonToOption.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (buttonToOption[i], buttonToOption[j]) = (buttonToOption[j], buttonToOption[i]);
        }

        foreach (int optionIndex in buttonToOption)
        {
            int index = optionIndex;
            Button b = Instantiate(optionTemplate, optionContainer);
            b.gameObject.SetActive(true);
            b.onClick.RemoveAllListeners();
            b.targetGraphic.color = optionColor;

            TextMeshProUGUI label = b.GetComponentInChildren<TextMeshProUGUI>(true);
            label.richText = false; // so answers like "<=" show as typed
            label.text = q.options[index];

            b.onClick.AddListener(() =>
            {
                foreach (Button other in optionButtons) other.interactable = false;
                onAnswer(index);
            });
            optionButtons.Add(b);
        }
    }

    public void RevealAnswer(PythonQuestion q, int chosen, bool correct)
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            int option = buttonToOption[i];
            optionButtons[i].interactable = false;
            optionButtons[i].transition = Selectable.Transition.None; // keep reveal colours bright
            if (option == q.correctIndex) optionButtons[i].targetGraphic.color = correctColor;
            else if (option == chosen) optionButtons[i].targetGraphic.color = wrongColor;
        }

        string shown = chosen >= 0 && q.options != null && chosen < q.options.Length ? q.options[chosen] : null;
        codeText.text = RenderCode(q, shown, correct);

        if (explanationText != null)
        {
            explanationText.gameObject.SetActive(true);
            string head = correct ? "<color=#7CFC9A>Correct!</color> " : "<color=#FF7A7A>Not quite.</color> ";
            explanationText.text = head + (q.explanation ?? "");
        }
    }

    public void HideQuestion()
    {
        if (questionPanel != null) questionPanel.SetActive(false);
    }

    // ---------------------------------------------------------------- helpers

    private string RenderCode(PythonQuestion q, string filled, bool correct)
    {
        StringBuilder sb = new StringBuilder("<mspace=0.55em>");
        if (q.codeLines != null)
        {
            for (int i = 0; i < q.codeLines.Length; i++)
            {
                string line = q.codeLines[i] ?? "";
                string[] parts = line.Split(new[] { PythonQuestion.Blank }, StringSplitOptions.None);
                for (int p = 0; p < parts.Length; p++)
                {
                    sb.Append(Highlight(parts[p]));
                    if (p < parts.Length - 1) sb.Append(BlankMarkup(filled, correct));
                }
                if (i < q.codeLines.Length - 1) sb.Append('\n');
            }
        }
        sb.Append("</mspace>");
        return sb.ToString();
    }

    private static string BlankMarkup(string filled, bool correct)
    {
        if (filled == null) return "<mark=#FFD54A55><color=#FFD54A> ??? </color></mark>";
        string colour = correct ? "#7CFC9A" : "#FF7A7A";
        return $"<mark={colour}44><color={colour}> {Escape(filled)} </color></mark>";
    }

    // Simple Python syntax colouring. Comments and strings first, so keywords inside them aren't coloured.
    private static string Highlight(string code)
    {
        int hash = code.IndexOf('#');
        string comment = "";
        if (hash >= 0)
        {
            comment = "<color=#6A7585>" + Escape(code.Substring(hash)) + "</color>";
            code = code.Substring(0, hash);
        }

        StringBuilder sb = new StringBuilder();
        int start = 0;
        while (start < code.Length)
        {
            int q = code.IndexOfAny(new[] { '"', '\'' }, start);
            if (q < 0) { sb.Append(ColourWords(code.Substring(start))); break; }

            sb.Append(ColourWords(code.Substring(start, q - start)));
            int end = code.IndexOf(code[q], q + 1);
            if (end < 0) end = code.Length - 1;
            sb.Append("<color=#A5D6A7>").Append(Escape(code.Substring(q, end - q + 1))).Append("</color>");
            start = end + 1;
        }

        return sb + comment;
    }

    private static string ColourWords(string s)
    {
        s = Escape(s);
        s = Keywords.Replace(s, "<color=#C792EA>$1</color>");
        s = Builtins.Replace(s, "<color=#82AAFF>$1</color>");
        s = Numbers.Replace(s, "<color=#F78C6C>$0</color>");
        return s;
    }

    // TextMeshPro treats '<' as the start of a tag, so code like "hp <= 0" needs escaping.
    private static string Escape(string s) => s.Replace("<", "<noparse><</noparse>");

    private static void SetBar(RectTransform fill, TextMeshProUGUI label, int hp, int max)
    {
        max = Mathf.Max(1, max);
        hp = Mathf.Clamp(hp, 0, max);
        if (fill != null) fill.anchorMax = new Vector2((float)hp / max, 1f);
        if (label != null) label.text = $"{hp} / {max}";
    }

    private IEnumerator BannerRoutine(string message)
    {
        bannerText.text = message;
        bannerText.gameObject.SetActive(true);
        bannerText.alpha = 1f;
        bannerText.transform.localScale = Vector3.one * 1.25f;

        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            bannerText.transform.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, t / 0.15f);
            yield return null;
        }

        yield return new WaitForSeconds(bannerDuration);

        t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            bannerText.alpha = 1f - t / 0.25f;
            yield return null;
        }
        bannerText.gameObject.SetActive(false);
        bannerRoutine = null;
    }
}
