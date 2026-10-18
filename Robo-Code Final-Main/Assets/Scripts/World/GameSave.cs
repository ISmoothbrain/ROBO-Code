using UnityEngine;

// Everything that's saved between play sessions, for Continue / New Game.
//   Saved:   which level you were in, completed levels, opened chests, coins, shop upgrades,
//            the Magitek's +20 max HP, the Hack Fragment and whether Hack is crafted.
//   Not saved: the backpack's items (potions etc.) - they last until you quit.
// New Game starts a fresh save ("generation"), so every old key is simply ignored.
public static class GameSave
{
    // A new game starts with this much health (3 hearts at 10 HP per heart).
    public const int BaseMaxHealth = 30;

    private static int Generation => PlayerPrefs.GetInt("SaveGeneration", 0);
    public static string Prefix => "Save" + Generation + "_";

    private static int GetInt(string k, int d = 0) => PlayerPrefs.GetInt(Prefix + k, d);
    private static void SetInt(string k, int v) { PlayerPrefs.SetInt(Prefix + k, v); PlayerPrefs.Save(); }

    public static bool HasSave => PlayerPrefs.HasKey(Prefix + "LastLevel");
    public static int LastLevelIndex { get => GetInt("LastLevel", -1); set => SetInt("LastLevel", value); }
    public static int BossHealthBonus { get => GetInt("BossHealthBonus"); set => SetInt("BossHealthBonus", value); }
    public static bool HasHackFragment { get => GetInt("HackFragment") == 1; set => SetInt("HackFragment", value ? 1 : 0); }
    public static bool HackUnlocked { get => GetInt("HackUnlocked") == 1; set => SetInt("HackUnlocked", value ? 1 : 0); }

    // Coins (Coin.cs) and shop upgrades (PlayerUpgrades.cs) save themselves for the current run
    // (LevelProgress.RunId), so there's nothing extra to load or save for them here.
    // Called on every level load, level complete and quit, to make sure everything is on disk.
    public static void SaveSession()
    {
        PlayerPrefs.Save();
    }

    public static void StartNewGame()
    {
        PlayerPrefs.SetInt("SaveGeneration", Generation + 1);  // a clean save
        PlayerPrefs.Save();
        CoinCounter.Reset();
        PlayerUpgrades.Reset();
        EquippedItem.Clear();
        StoneOfRecall.ForgetReached();
        InventoryManager inv = InventoryManager.Instance;
        if (inv != null)
            foreach (InventorySlot s in inv.slots.ToArray())
                if (s != null && s.item != null) inv.RemoveItem(s.item, s.count);
        SaveSession();
    }

    // The level to load for Continue (the tutorial if there's no save yet).
    public static int ContinueIndex()
    {
        int i = LastLevelIndex;
        return i > 0 && i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings ? i : 1;
    }
}
