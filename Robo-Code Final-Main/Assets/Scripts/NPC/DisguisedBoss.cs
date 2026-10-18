using System.Collections;
using TMPro;
using UnityEngine;

// The hidden boss: NightBorne disguised as DayForborne (tutorial).
//   Talk (E): greeting with two choices.
//     Reveal ("Fleshbags?...")  -> reply, he transforms and AMBUSHES you (he strikes first).
//     Deflect ("They deserved better") -> reply, nothing happens.
//   Hit him with your sword at any time -> same reveal reply, he transforms, but YOU strike first.
// The transformation is the boss's death animation played backwards.
// Once that NightBorne is defeated he never appears again in this save.
public class DisguisedBoss : MonoBehaviour
{
    public string npcName = "DayForborne";
    [TextArea] public string greeting = "Shame these fleshbags could've been great soldiers.";
    public string revealChoice = "Fleshbags? You talk like you're a Clanker.";
    public string deflectChoice = "They deserved better.";
    [TextArea] public string revealReply = "Ah, it seems you've seen under my disguise...";
    [TextArea] public string deflectReply = "Hmmmmm....";

    [Header("Transformation")]
    public GameObject bossPrefab;          // NightBorne
    public Sprite[] transformFrames;       // the boss's death frames (played backwards)
    public float transformFps = 16f;
    public string defeatedId = "tutorial_nightborne";

    public float talkRange = 1.8f;
    public float promptHeight = 1.9f;

    private Player player;
    private TextMeshPro prompt;
    private bool revealing;

    public static string DefeatedKey(string id) => GameSave.Prefix + "HiddenBossDefeated_" + id;

    private void Start()
    {
        if (PlayerPrefs.GetInt(DefeatedKey(defeatedId), 0) == 1) { Destroy(gameObject); return; }
        prompt = NpcUtil.MakePrompt(transform, promptHeight);
    }

    private void Update()
    {
        if (revealing) return;
        if (player == null) { player = NpcUtil.FindPlayer(); return; }
        bool near = NpcUtil.Near(player, transform, talkRange);
        bool busy = UIBusy.AnyOpen;
        prompt.gameObject.SetActive(near && !busy);
        prompt.text = "[E] Talk";
        if (near && !busy && !UIBusy.ClosedThisFrame && Input.GetKeyDown(KeyCode.E) && DialogueUI.Instance != null)
        {
            DialogueUI.Instance.Show(npcName, new[] { greeting }, new[] { revealChoice, deflectChoice }, choice =>
            {
                if (choice == 0) Reveal(true);
                else DialogueUI.Instance.Show(npcName, new[] { deflectReply });
            });
        }
    }

    // Player.DealDamage calls this when your sword hits him: you saw through it, so YOU strike first.
    public void OnPlayerHit(Player p)
    {
        if (revealing) return;
        player = p;
        Reveal(false);
    }

    private void Reveal(bool bossStrikesFirst)
    {
        revealing = true;
        if (prompt != null) prompt.gameObject.SetActive(false);
        if (DialogueUI.Instance != null)
            DialogueUI.Instance.Show(npcName, new[] { revealReply }, null, null, () => StartCoroutine(TransformRoutine(bossStrikesFirst)));
        else
            StartCoroutine(TransformRoutine(bossStrikesFirst));
    }

    private IEnumerator TransformRoutine(bool bossStrikesFirst)
    {
        if (bossPrefab == null) { Debug.LogWarning("DisguisedBoss: no Boss Prefab set.", this); yield break; }

        // the disguise vanishes...
        SpriteRenderer mine = GetComponent<SpriteRenderer>();
        if (mine != null) mine.enabled = false;
        foreach (Collider2D c in GetComponents<Collider2D>()) c.enabled = false;

        // ...and NightBorne forms where he stood, his death played backwards
        GameObject go = Instantiate(bossPrefab, transform.position, Quaternion.identity);
        go.name = "NightBorne";
        BanditEnemy boss = go.GetComponent<BanditEnemy>();
        if (boss != null) boss.passive = true;
        Animator anim = go.GetComponent<Animator>();
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (anim != null) anim.enabled = false;
        if (sr != null && transformFrames != null)
        {
            for (int i = transformFrames.Length - 1; i >= 0; i--)
            {
                sr.sprite = transformFrames[i];
                yield return new WaitForSeconds(1f / Mathf.Max(1f, transformFps));
            }
        }
        if (anim != null) anim.enabled = true;

        if (boss != null)
        {
            boss.passive = false;
            LetterOnDefeat letter = go.AddComponent<LetterOnDefeat>();
            letter.saveKey = DefeatedKey(defeatedId);
            letter.endsGame = true;   // seeing through the disguise and winning is a secret early ending
            if (player == null) player = NpcUtil.FindPlayer();
            if (BattleManager.Instance != null && player != null)
                BattleManager.Instance.TryStartBattle(player, boss, !bossStrikesFirst);
        }
        Destroy(gameObject);
    }
}
