using UnityEngine;

// A conversation that starts when the player reaches the boss's platform.
//   Choice 1 (play along): the boss lowers its guard and won't attack, so you can strike first (your advantage).
//   Choice 2 (defy):       the boss replies, then ambushes you (the boss gets the first turn).
// Added automatically to the Magitek and NightBorne bosses (WorldBootstrap), with their lines.
// You can also add it to any boss by hand and write your own lines.
[RequireComponent(typeof(BanditEnemy))]
public class BossEncounter : MonoBehaviour
{
    public string bossName = "Boss";
    [TextArea] public string greeting = "Who goes there?";
    [TextArea] public string submitChoice = "A friend.";
    [TextArea] public string submitReply = "Then move along.";
    [TextArea] public string defyChoice = "Your end.";
    [TextArea] public string defyReply = "So be it.";

    [Header("Trigger area (centred on the boss)")]
    [Tooltip("Width and height of the area that starts the conversation. Keep the height small so it only triggers on the boss's platform.")]
    public Vector2 triggerSize = new Vector2(14f, 2.5f);
    public Vector2 triggerOffset = new Vector2(0f, 0.8f);

    private BanditEnemy boss;
    private Player player;
    private bool resolved;      // the player has chosen
    private bool waitForExit;   // closed the talk without choosing: ask again after they step away

    private void Start()
    {
        boss = GetComponent<BanditEnemy>();
        boss.passive = true;    // it waits for the conversation instead of attacking on sight
    }

    private void Update()
    {
        if (resolved || boss == null || boss.IsDead) return;
        if (player == null) { player = FindAnyObjectByType<Player>(); return; }

        bool inside = InArea(player.transform.position);
        if (waitForExit) { if (!inside) waitForExit = false; return; }
        if (!inside || player.IsDead || UIBusy.AnyOpen || DialogueUI.Instance == null) return;

        bool chose = false;
        DialogueUI.Instance.Show(bossName, new[] { greeting }, new[] { submitChoice, defyChoice },
            choice =>
            {
                chose = true;
                resolved = true;
                if (choice == 0) PlayAlong();
                else Defy();
            },
            () => { if (!chose) waitForExit = true; });
    }

    private bool InArea(Vector3 p)
    {
        Vector2 c = (Vector2)transform.position + triggerOffset;
        return Mathf.Abs(p.x - c.x) <= triggerSize.x * 0.5f && Mathf.Abs(p.y - c.y) <= triggerSize.y * 0.5f;
    }

    private void PlayAlong()
    {
        // The boss is fooled: it stays passive, so hitting it first starts the battle with YOUR advantage.
        DialogueUI.Instance.Show(bossName, new[] { submitReply });
    }

    private void Defy()
    {
        DialogueUI.Instance.Show(bossName, new[] { defyReply }, null, null, () =>
        {
            boss.passive = false;
            if (BattleManager.Instance != null && player != null)
                BattleManager.Instance.TryStartBattle(player, boss, false); // the boss strikes first
        });
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
        Gizmos.DrawWireCube((Vector2)transform.position + triggerOffset, triggerSize);
    }
}
