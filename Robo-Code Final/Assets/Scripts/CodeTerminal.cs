using TMPro;
using UnityEngine;

// A spot in the world (crafting station, shrine, computer...) where the player
// walks up and presses E to take the next coding challenge.
// Needs a Collider2D with Is Trigger ticked.
public class CodeTerminal : MonoBehaviour
{
    [Tooltip("Challenges offered here, in order. The first unsolved, unlocked one is opened.")]
    public CodeChallenge[] challenges;
    public KeyCode interactKey = KeyCode.E;

    [Header("Prompt (optional)")]
    public GameObject prompt;          // e.g. a world-space "Press E" label
    public TextMeshPro promptText;     // optional, to change the wording
    public string readyMessage = "Press E to code";
    public string lockedMessage = "Solve other challenges first";
    public string doneMessage = "All challenges solved!";

    private bool playerInRange;

    private void Reset() => EnsureTrigger();

    private void Awake() => EnsureTrigger();

    private void EnsureTrigger()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) c = gameObject.AddComponent<BoxCollider2D>();
        c.isTrigger = true;
    }

    private void Start()
    {
        if (prompt != null) prompt.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange || CodeChallengeUI.IsOpen) return;

        RefreshPrompt();

        if (Input.GetKeyDown(interactKey))
        {
            CodeChallenge next = NextChallenge(out _);
            if (next == null) return;

            if (CodeChallengeUI.Instance == null)
            {
                Debug.LogWarning("No CodeChallengeUI in the scene. Use Tools > Code Challenges > Create Challenge UI.");
                return;
            }
            CodeChallengeUI.Instance.Open(next);
        }
    }

    private CodeChallenge NextChallenge(out bool anyLocked)
    {
        anyLocked = false;
        ProgressManager pm = ProgressManager.Instance;
        if (challenges == null) return null;

        foreach (CodeChallenge c in challenges)
        {
            if (c == null) continue;
            if (pm != null && pm.IsCompleted(c)) continue;
            if (pm != null && !pm.IsAvailable(c)) { anyLocked = true; continue; }
            return c;
        }
        return null;
    }

    private void RefreshPrompt()
    {
        if (promptText == null) return;
        CodeChallenge next = NextChallenge(out bool anyLocked);
        promptText.text = next != null ? readyMessage : (anyLocked ? lockedMessage : doneMessage);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (prompt != null) prompt.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (prompt != null) prompt.SetActive(false);
    }
}
