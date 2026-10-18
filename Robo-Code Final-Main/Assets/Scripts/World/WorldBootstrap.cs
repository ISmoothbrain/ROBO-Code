using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Sets up the world automatically whenever a level with a Player loads:
//   - Stone of Recall:  travel menu; teleporting puts you next to the stone
//   - Chests:           press E for coins (Golden 20, Silver 10, Wooden 5)
//   - Bosses:           the Magitek / NightBorne conversations
//   - Spawning:         the death animation played backwards
//   - Level complete:   beat the boss (or every enemy if there's no boss) to unlock the next level
public static class WorldBootstrap
{
    private static bool subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (!subscribed)
        {
            SceneManager.sceneLoaded += (scene, mode) => Setup();
            subscribed = true;
        }
        Setup();
    }

    private static void Setup()
    {
        Player player = Object.FindAnyObjectByType<Player>();
        if (player == null) return;                                          // menus
        if (Object.FindAnyObjectByType<WorldRunner>() != null) return;       // already done for this scene

        try
        {
            StoneOfRecall stone = null;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (!t.name.Contains("Stone of Recall")) continue;
                StoneOfRecall s = t.GetComponent<StoneOfRecall>();
                if (s == null) s = t.gameObject.AddComponent<StoneOfRecall>();
                if (stone == null) stone = s;
            }

            foreach (Animator a in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
            {
                if (!a.name.Contains("Chest") || a.GetComponent<LootChest>() != null || !HasParam(a, "IsOpened")) continue;
                LootChest c = a.gameObject.AddComponent<LootChest>();
                c.coins = a.name.Contains("Golden") ? 20 : a.name.Contains("Silver") ? 10 : a.name.Contains("Wooden") ? 5 : 10;
            }

            // Later levels are harder: normal enemies get +30% health and damage per tier.
            // (Bosses keep the exact numbers on their prefabs.)
            int tier = LevelProgress.CurrentTier();
            float scale = 1f + 0.3f * (tier - 1);
            foreach (BanditEnemy e in Object.FindObjectsByType<BanditEnemy>(FindObjectsSortMode.None))
            {
                if (e.isBoss || scale <= 1f) continue;
                e.health = Mathf.RoundToInt(e.health * scale);
                e.attackDamage = Mathf.RoundToInt(e.attackDamage * scale);
            }

            // Save where we are, for Continue on the main menu.
            if (LevelProgress.Current() != null)
                GameSave.LastLevelIndex = SceneManager.GetActiveScene().buildIndex;
            GameSave.SaveSession();
            HackCrafting.EnsureFragmentInBackpack();

            foreach (BanditEnemy e in Object.FindObjectsByType<BanditEnemy>(FindObjectsSortMode.None))
            {
                if (e.GetComponent<BossEncounter>() != null) continue;
                string n = e.name.ToLowerInvariant();
                if (n.Contains("magitek") || n.Contains("magitech")) AddMagitekTalk(e);
                else if (n.Contains("nightborne")) AddNightBorneTalk(e);
            }

            // Teleported here: appear at the Stone of Recall. New game or respawn after dying:
            // stay where the Player object is placed in the scene. Either way, materialise from code.
            if (SceneTransition.ConsumeArriveAtStone() && stone != null)
            {
                player.transform.position = stone.SpawnPosition;
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
            // After a fade, wait for the screen to start clearing so the spawn is visible.
            PlayerDeathAnimation.PlaySpawnOn(player.gameObject, SceneTransition.Busy ? 0.35f : 0f);

            new GameObject("[World]").AddComponent<WorldRunner>();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[World] Couldn't set up the level automatically. Please send this message:\n" + ex);
        }
    }

    private static bool HasParam(Animator a, string name)
    {
        if (a.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter p in a.parameters) if (p.name == name) return true;
        return false;
    }

    private static void AddMagitekTalk(BanditEnemy e)
    {
        BossEncounter t = e.gameObject.AddComponent<BossEncounter>();
        t.bossName = "Magitek";
        t.greeting = "Who do we have here? Are you here to serve me or slay me?";
        t.submitChoice = "Yes Master.";
        t.submitReply = "Good. Then stay out of my way, servant.";
        t.defyChoice = "No, I am here to take your C.P.U and fry your Motherboard!";
        t.defyReply = "I see, so you're just gonna be another of my cosmetic upgrades.";
    }

    private static void AddNightBorneTalk(BanditEnemy e)
    {
        BossEncounter t = e.gameObject.AddComponent<BossEncounter>();
        t.bossName = "NightBorne";
        t.greeting = "Are you one of my soldiers? I have no request of you. Go and do your duty.";
        t.submitChoice = "Yes Sir, I apologise for interrupting!!";
        t.submitReply = "THEN GET THE HELL OUT OF HERE!!";
        t.defyChoice = "No, I am here to help you end your duty.";
        t.defyReply = "I see... another practice dummy.";
        e.gameObject.AddComponent<LetterOnDefeat>();   // the General's letter when he falls
    }
}

// Watches for the level being completed and unlocks the next one.
public class WorldRunner : MonoBehaviour
{
    private LevelProgress.Destination level;
    private readonly List<BanditEnemy> bosses = new List<BanditEnemy>();
    private readonly List<BanditEnemy> enemies = new List<BanditEnemy>();
    private bool announce;
    private float nextCheck;
    private float killY = float.NegativeInfinity; // falling below this = off the map
    private Player player;

    private void Start()
    {
        player = FindAnyObjectByType<Player>();

        // The fall line: a few units below the lowest solid thing in the level.
        float lowest = float.PositiveInfinity;
        foreach (Collider2D c in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (c.isTrigger || c.GetComponentInParent<Player>() != null || c.GetComponentInParent<BanditEnemy>() != null) continue;
            lowest = Mathf.Min(lowest, c.bounds.min.y);
        }
        if (!float.IsInfinity(lowest)) killY = lowest - 6f;

        level = LevelProgress.Current();
        foreach (BanditEnemy e in FindObjectsByType<BanditEnemy>(FindObjectsSortMode.None))
        {
            enemies.Add(e);
            if (e.isBoss) bosses.Add(e);
        }
    }

    private void Update()
    {
        // Fell off the map?
        if (player != null && !player.IsDead && player.transform.position.y < killY) player.FellOffMap();
        foreach (BanditEnemy e in enemies)
            if (e != null && !e.IsDead && e.transform.position.y < killY) Destroy(e.gameObject); // enemies that fall are gone

        if (level == null) return;

        if (announce)
        {
            if (UIBusy.AnyOpen || SceneTransition.Busy || LetterUI.Busy || DialogueUI.Instance == null) return; // wait for the battle and the letter
            announce = false;

            // NightBorne defeated (Level 3), or every level done: roll the ending, then back to the main menu.
            if (level.endsGame || LevelProgress.AllComplete()) { SceneTransition.ShowEnding(); return; }

            var lines = new System.Collections.Generic.List<string> { $"{level.displayName} complete!" };
            if (level.rewardHackFragment) lines.Add("You found a Hack Fragment! Open your backpack and use it to craft the Hack ability.");
            if (level.rewardMaxHealth > 0) lines.Add($"Your max HP went up by {level.rewardMaxHealth}!");
            LevelProgress.Destination next = LevelProgress.Next(level);
            if (next != null) lines.Add($"{next.displayName} is now unlocked. Travel there from any Stone of Recall.");
            DialogueUI.Instance.Show("Stone of Recall", lines.ToArray());
            return;
        }

        if (LevelProgress.IsComplete(level.id) || Time.time < nextCheck) return;
        nextCheck = Time.time + 0.5f;

        List<BanditEnemy> goal = bosses.Count > 0 ? bosses : enemies;
        if (goal.Count == 0) return;                                          // nothing to beat here
        foreach (BanditEnemy e in goal) if (e != null && !e.IsDead) return;   // someone's still standing

        LevelProgress.MarkComplete(level.id);
        GiveRewards();
        announce = true;
    }

    // The level's reward (e.g. the Magitek: +20 max HP and the Hack Fragment). Only ever given once.
    private void GiveRewards()
    {
        if (level.rewardMaxHealth > 0)
        {
            GameSave.BossHealthBonus += level.rewardMaxHealth;
            if (player != null && !player.IsDead)
            {
                player.maxHealth += level.rewardMaxHealth;
                player.Heal(level.rewardMaxHealth);
            }
        }
        if (level.rewardHackFragment && !GameSave.HackUnlocked)
        {
            GameSave.HasHackFragment = true;
            HackCrafting.EnsureFragmentInBackpack();
        }
        GameSave.SaveSession();
    }

    private void OnApplicationQuit() => GameSave.SaveSession();
}
