using TMPro;
using UnityEngine;

// The tutorial guide (Nerdwar). Starts talking when the player walks near:
//   1. welcome + how to move  -> waits until the player has actually moved
//   2. how to jump             -> waits until the player jumps
//   3. "head EAST"            -> done (saved). Talking again repeats the last line.
// {MoveLeft}, {MoveRight}, {Jump} become the player's real keys.
public class TutorialGuide : MonoBehaviour
{
    public string npcName = "Nerdwar";
    public string guideId = "tutorial_guide";
    [TextArea] public string[] welcomeLines = { "So you're Robo! Welcome to the Safe Haven.", "To move, press {MoveLeft} & {MoveRight}." };
    [TextArea] public string[] jumpLines = { "Good! Now you know how to move from place to place. You will encounter some obstacles on your way, so press {Jump} to jump." };
    [TextArea] public string[] doneLines = { "Good! Now head EAST from here." };

    [Tooltip("How close the player must come before he starts talking.")]
    public float startRange = 4f;
    [Tooltip("How far the player must walk to finish the moving lesson.")]
    public float moveDistance = 2f;
    public float talkRange = 1.8f;
    public float promptHeight = 1.9f;

    private int step;           // 0 not started, 1 moving lesson, 2 jumping lesson, 3 done
    private float startX;
    private float jumpReadyAt;
    private Player player;
    private TextMeshPro prompt;

    private string Key => GameSave.Prefix + "Guide_" + guideId;

    private void Start()
    {
        step = PlayerPrefs.GetInt(Key, 0) == 1 ? 3 : 0;
        prompt = NpcUtil.MakePrompt(transform, promptHeight);
    }

    private void Update()
    {
        if (player == null) { player = NpcUtil.FindPlayer(); return; }
        bool busy = UIBusy.AnyOpen || UIBusy.ClosedThisFrame || DialogueUI.Instance == null;

        switch (step)
        {
            case 0:
                if (!busy && NpcUtil.Near(player, transform, startRange))
                {
                    step = -1; // talking
                    DialogueUI.Instance.Show(npcName, NpcUtil.Keys(welcomeLines), null, null, () =>
                    {
                        startX = player.transform.position.x;
                        step = 1;
                        TutorialHint.Show("Move with {MoveLeft} and {MoveRight}.", 8f);
                    });
                }
                break;

            case 1:
                if (!busy && Mathf.Abs(player.transform.position.x - startX) >= moveDistance)
                {
                    step = -1;
                    DialogueUI.Instance.Show(npcName, NpcUtil.Keys(jumpLines), null, null, () =>
                    {
                        step = 2;
                        jumpReadyAt = Time.time + 0.3f;   // don't count the key that closed the dialogue
                        TutorialHint.Show("Jump with {Jump}.", 8f);
                    });
                }
                break;

            case 2:
                if (!busy && Time.time >= jumpReadyAt && Input.GetKeyDown(Keybinds.Get("Jump")))
                {
                    step = -1;
                    Invoke(nameof(FinishLesson), 0.6f);     // let the jump happen first
                }
                break;

            case 3:
                bool near = NpcUtil.Near(player, transform, talkRange);
                prompt.gameObject.SetActive(near && !UIBusy.AnyOpen);
                prompt.text = "[E] Talk";
                if (near && !busy && Input.GetKeyDown(KeyCode.E)) DialogueUI.Instance.Show(npcName, NpcUtil.Keys(doneLines));
                break;
        }
    }

    private void FinishLesson()
    {
        if (DialogueUI.Instance == null) { step = 3; return; }
        DialogueUI.Instance.Show(npcName, NpcUtil.Keys(doneLines), null, null, () =>
        {
            step = 3;
            PlayerPrefs.SetInt(Key, 1);
            PlayerPrefs.Save();
        });
    }

    [ContextMenu("Reset the tutorial")]
    private void ResetGuide() { PlayerPrefs.DeleteKey(Key); step = 0; }
}
