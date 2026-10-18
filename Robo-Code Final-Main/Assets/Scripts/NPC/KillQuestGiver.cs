using TMPro;
using UnityEngine;

// A side quest: defeat every (non-boss) enemy in this stage, then come back for coins.
//   1. Talk (E): the offer lines play and the quest starts (and the Hint, if any, pops up).
//   2. Defeat every enemy in the stage. A hint tells you to come back.
//   3. Talk again: you get the coins and the Complete lines, then the After lines.
// Lines can use {MoveLeft}, {Jump}, {Inventory}, {UseItem}... for the player's keys,
// {reward} for the coins and {left} for enemies remaining. Progress is part of the save.
public class KillQuestGiver : MonoBehaviour
{
    public string npcName = "Stranger";
    [Tooltip("Unique per quest; used for saving.")]
    public string questId = "quest";

    [TextArea] public string[] offerLines = { "Please, destroy every Clanker in this area!" };
    [TextArea] public string hint = "";
    [TextArea] public string[] reminderLines = { "There are still {left} of them out there!" };
    [TextArea] public string[] completeLines = { "Thank you! Here are {reward} coins." };
    [TextArea] public string[] afterLines = { };

    public int rewardCoins = 25;
    [Tooltip("Also count bosses (normally only regular enemies count).")]
    public bool includeBosses = false;

    [Header("Animation (names of NpcAnimator clips)")]
    public string questClip = "";
    public string doneClip = "";

    public float talkRange = 1.8f;
    public float promptHeight = 1.8f;

    private enum State { NotStarted = 0, Active = 1, Done = 2 }
    private State state;
    private Player player;
    private NpcAnimator anim;
    private TextMeshPro prompt;
    private bool told;              // "come back" hint already shown
    private float nextCheck;
    private bool allDead;

    private string Key => GameSave.Prefix + "Quest_" + questId + "_" + gameObject.scene.name;

    private void Start()
    {
        state = (State)PlayerPrefs.GetInt(Key, 0);
        anim = GetComponent<NpcAnimator>();
        prompt = NpcUtil.MakePrompt(transform, promptHeight);
        if (anim != null) anim.Play(state == State.Done ? doneClip : questClip);
    }

    private void Update()
    {
        if (player == null) { player = NpcUtil.FindPlayer(); return; }

        if (state == State.Active && Time.time >= nextCheck)
        {
            nextCheck = Time.time + 0.5f;
            allDead = EnemiesLeft() == 0;
            if (allDead && !told)
            {
                told = true;
                TutorialHint.Show($"Quest complete! Go back to {npcName} for your reward.", 6f);
            }
        }

        bool near = NpcUtil.Near(player, transform, talkRange);
        bool busy = UIBusy.AnyOpen;
        prompt.gameObject.SetActive(near && !busy);
        prompt.text = state == State.Active && allDead ? "[E] Reward" : state == State.NotStarted ? "[E] Talk  !" : "[E] Talk";
        if (near && !busy && !UIBusy.ClosedThisFrame && Input.GetKeyDown(KeyCode.E)) Talk();
    }

    private int EnemiesLeft()
    {
        int n = 0;
        foreach (BanditEnemy e in FindObjectsByType<BanditEnemy>(FindObjectsSortMode.None))
            if (e != null && !e.IsDead && (includeBosses || !e.isBoss)) n++;
        return n;
    }

    private string[] Fill(string[] lines)
    {
        string[] o = NpcUtil.Keys(lines);
        for (int i = 0; i < o.Length; i++)
            o[i] = o[i].Replace("{reward}", rewardCoins.ToString()).Replace("{left}", EnemiesLeft().ToString());
        return o;
    }

    private void Talk()
    {
        if (DialogueUI.Instance == null) return;
        switch (state)
        {
            case State.NotStarted:
                DialogueUI.Instance.Show(npcName, Fill(offerLines), null, null, () =>
                {
                    SetState(State.Active);
                    if (!string.IsNullOrEmpty(hint)) TutorialHint.Show(hint, 12f);
                });
                break;

            case State.Active:
                if (EnemiesLeft() == 0) Claim();
                else DialogueUI.Instance.Show(npcName, Fill(reminderLines));
                break;

            default:
                DialogueUI.Instance.Show(npcName, Fill(afterLines.Length > 0 ? afterLines : completeLines));
                break;
        }
    }

    private void Claim()
    {
        CoinCounter.Add(rewardCoins);
        SetState(State.Done);
        if (anim != null) anim.Play(doneClip);
        var all = new System.Collections.Generic.List<string>(Fill(completeLines));
        all.AddRange(Fill(afterLines));
        DialogueUI.Instance.Show(npcName, all.ToArray());
    }

    private void SetState(State s)
    {
        state = s;
        PlayerPrefs.SetInt(Key, (int)s);
        PlayerPrefs.Save();
    }

    [ContextMenu("Reset this quest")]
    private void ResetQuest() { SetState(State.NotStarted); told = false; }
}
