using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// The levels you can travel to with a Stone of Recall, and which ones are unlocked.
// Progress is saved with PlayerPrefs, so it survives closing the game.
// Scenes are matched by name from File > Build Settings, ignoring spaces, underscores and capitals,
// so "Level 2", "Level_2" and "level2" all match.
public static class LevelProgress
{
    public class Destination
    {
        public string id;            // used for saving
        public string displayName;   // what the Stone of Recall shows
        public string[] sceneNames;  // any of these scene names will do
        public string unlockedBy;    // id of the level that must be completed first (null = always open)
        public int tier = 1;         // difficulty: enemies get tougher and questions harder each tier
        public int rewardMaxHealth;  // extra max HP for completing it (beating its boss)
        public bool rewardHackFragment; // drops the Hack Fragment used to craft Hack
        public bool endsGame;        // beating this level's boss plays the ending
    }

    public static readonly Destination[] Destinations =
    {
        new Destination { id = "tutorial", displayName = "The Safe Haven",
                          sceneNames = new[] { "Tuturial Level", "Tutorial Level", "Lobby&Tutorial", "Tutorial" }, unlockedBy = null },
        new Destination { id = "level2", displayName = "Magister's Island",
                          sceneNames = new[] { "Level 2", "Level2" }, unlockedBy = "tutorial",
                          tier = 2, rewardMaxHealth = 20, rewardHackFragment = true },   // the Magitek's rewards
        new Destination { id = "level3", displayName = "Gemini's Blade",
                          sceneNames = new[] { "Level 3", "Level3" }, unlockedBy = "level2", tier = 3,
                          endsGame = true },   // defeating NightBorne is the ending
    };

    private static string Prefix => GameSave.Prefix + "LevelComplete_";  // part of the save (New Game clears it)

    // Difficulty tier of the level that's open (1 if it isn't one of the levels above).
    public static int CurrentTier() { Destination d = Current(); return d != null ? d.tier : 1; }

    // ---- used by your teammate's scripts (Coin.cs, PlayerUpgrades.cs, GamePreferences.cs,
    //      MainMenuRuntimeUI.cs, GameFlowOverlay.cs) ----

    // Which playthrough this is. New Game moves to the next number, so coins and upgrades
    // saved per run (Coins_Run<id>) start fresh. Same number as GameSave's save generation.
    public static int RunId => PlayerPrefs.GetInt("SaveGeneration", 0);

    // Position of the open level in Destinations: 0 = The Safe Haven, 1 = Magister's Island,
    // 2 = Gemini's Blade, -1 = not a level (e.g. the main menu).
    public static int CurrentOrdinal() => System.Array.IndexOf(Destinations, Current());

    // Is there a game to continue?
    public static bool HasProgress()
    {
        if (GameSave.HasSave) return true;
        foreach (Destination d in Destinations) if (IsComplete(d.id)) return true;
        return false;
    }

    // Respawn rule after dying: at the Stone of Recall, except in the tutorial until its stone is reached.
    public static bool ShouldRespawnAtStone()
    {
        Destination level = Current();
        bool tutorial = level != null && level.id == "tutorial";
        return !tutorial || StoneOfRecall.ReachedInThisScene();
    }

    // Build index of the "Main Menu" scene (0 if it isn't found by name).
    public static int MainMenuBuildIndex()
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            if (Normalize(System.IO.Path.GetFileNameWithoutExtension(path)) == "mainmenu") return i;
        }
        return 0;
    }

    public static bool AllComplete()
    {
        foreach (Destination d in Destinations) if (!IsComplete(d.id)) return false;
        return true;
    }

    public static bool IsComplete(string id) => PlayerPrefs.GetInt(Prefix + id, 0) == 1;

    public static bool IsUnlocked(Destination d) => d.unlockedBy == null || IsComplete(d.unlockedBy);

    public static void MarkComplete(string id)
    {
        PlayerPrefs.SetInt(Prefix + id, 1);
        PlayerPrefs.Save();
    }

    // For testing: right-click any Stone of Recall's component header and choose "Reset Level Progress".
    public static void ResetAll()
    {
        foreach (Destination d in Destinations) PlayerPrefs.DeleteKey(Prefix + d.id);
        PlayerPrefs.Save();
    }

    // Which destination is the scene that's open right now (or null).
    public static Destination Current()
    {
        string here = Normalize(SceneManager.GetActiveScene().name);
        foreach (Destination d in Destinations)
            foreach (string n in d.sceneNames)
                if (Normalize(n) == here) return d;
        return null;
    }

    // Finds the destination's scene in Build Settings, trying its names in order
    // (so "Tuturial Level" wins over "Lobby&Tutorial" if both are added). Returns -1 if none is added.
    public static int BuildIndexOf(Destination d)
    {
        foreach (string wanted in d.sceneNames)
        {
            string w = Normalize(wanted);
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                if (Normalize(System.IO.Path.GetFileNameWithoutExtension(path)) == w) return i;
            }
        }
        return -1;
    }

    // The display name ("The Safe Haven"...) of the scene at this build index, or null.
    public static string TitleForBuildIndex(int buildIndex)
    {
        foreach (Destination d in Destinations) if (BuildIndexOf(d) == buildIndex) return d.displayName;
        return null;
    }

    public static Destination Next(Destination d)
    {
        foreach (Destination x in Destinations) if (x.unlockedBy == d.id) return x;
        return null;
    }

    public static string Normalize(string s) =>
        string.IsNullOrEmpty(s) ? "" : s.ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
}
