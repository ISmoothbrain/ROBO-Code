using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Bottom-of-screen dialogue box with a name tag, typewriter text and optional choices.
// Built automatically by GameUIBootstrap; NPCs call DialogueUI.Instance.Show(...).
// E / Space / Enter / click: finish the line, then go to the next one.
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    public GameObject root;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI continueHint;
    public Transform choiceContainer;
    public Button choiceTemplate;
    public float charactersPerSecond = 45f;

    private string[] lines;
    private int index;
    private string[] choices;
    private Action<int> onChoice;
    private Action onClosed;
    private Coroutine typing;
    private bool lineFinished;
    private float openedAt;
    private readonly List<Button> choiceButtons = new List<Button>();

    private void Awake()
    {
        Instance = this;
        if (root != null) root.SetActive(false);
        if (choiceTemplate != null) choiceTemplate.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; IsOpen = false; }
    }

    public void Show(string speaker, string[] dialogueLines, string[] choiceLabels = null,
                     Action<int> onChoiceMade = null, Action onDialogueClosed = null)
    {
        lines = dialogueLines != null && dialogueLines.Length > 0 ? dialogueLines : new[] { "..." };
        choices = choiceLabels;
        onChoice = onChoiceMade;
        onClosed = onDialogueClosed;
        index = 0;

        root.SetActive(true);
        IsOpen = true;
        openedAt = Time.unscaledTime;
        nameText.text = speaker;
        ClearChoices();
        StartLine();
    }

    public void Close()
    {
        if (!IsOpen) return;
        if (typing != null) StopCoroutine(typing);
        ClearChoices();
        root.SetActive(false);
        IsOpen = false;
        UIBusy.MarkClosed();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        Action closed = onClosed;
        onClosed = null;
        closed?.Invoke();
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Time.unscaledTime - openedAt < 0.15f) return; // ignore the same E press that opened us

        bool choicesShowing = choiceButtons.Count > 0;
        bool advance = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) ||
                       Input.GetKeyDown(KeyCode.Return) || (!choicesShowing && Input.GetMouseButtonDown(0));

        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (!advance || choicesShowing) return;

        if (!lineFinished)
        {
            FinishLine();
        }
        else if (index < lines.Length - 1)
        {
            index++;
            StartLine();
        }
        else
        {
            Close();
        }
    }

    private void StartLine()
    {
        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(TypeLine(lines[index]));
    }

    private IEnumerator TypeLine(string line)
    {
        lineFinished = false;
        continueHint.gameObject.SetActive(false);
        bodyText.text = line;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();
        int total = bodyText.textInfo.characterCount;

        float shown = 0f;
        while (shown < total)
        {
            shown += Time.unscaledDeltaTime * charactersPerSecond;
            bodyText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
            yield return null;
        }
        FinishLine();
    }

    private void FinishLine()
    {
        if (typing != null) StopCoroutine(typing);
        typing = null;
        bodyText.maxVisibleCharacters = int.MaxValue;
        lineFinished = true;

        bool last = index >= lines.Length - 1;
        if (last && choices != null && choices.Length > 0)
        {
            ShowChoices();
            continueHint.gameObject.SetActive(false);
        }
        else
        {
            continueHint.text = last ? "[E] Close" : "[E] Next";
            continueHint.gameObject.SetActive(true);
        }
    }

    private void ShowChoices()
    {
        ClearChoices();
        for (int i = 0; i < choices.Length; i++)
        {
            int pick = i;
            Button b = Instantiate(choiceTemplate, choiceContainer);
            b.gameObject.SetActive(true);
            TextMeshProUGUI label = b.GetComponentInChildren<TextMeshProUGUI>(true);
            label.text = choices[i];

            // Size the button to its text, so long answers wrap inside it instead of spilling out.
            RectTransform lr = label.rectTransform;
            lr.offsetMin = new Vector2(16f, 6f);
            lr.offsetMax = new Vector2(-16f, -6f);
            float width = ((RectTransform)choiceContainer).rect.width - 32f;
            float textHeight = label.GetPreferredValues(choices[i], Mathf.Max(120f, width), 0f).y;
            LayoutElement le = b.GetComponent<LayoutElement>();
            if (le == null) le = b.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = Mathf.Max(56f, textHeight + 22f);
            b.onClick.AddListener(() =>
            {
                Action<int> callback = onChoice;
                onChoice = null;
                Close();
                callback?.Invoke(pick);
            });
            choiceButtons.Add(b);
        }
    }

    private void ClearChoices()
    {
        foreach (Button b in choiceButtons) if (b != null) Destroy(b.gameObject);
        choiceButtons.Clear();
    }
}
