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
    }

    public static readonly Destination[] Destinations =
    {
        new Destination { id = "tutorial", displayName = "The Safe Haven",
                          sceneNames = new[] { "Tuturial Level", "Tutorial Level", "Lobby&Tutorial", "Tutorial" }, unlockedBy = null },
        new Destination { id = "level2", displayName = "Magister's Island",
                          sceneNames = new[] { "Level 2", "Level2" }, unlockedBy = "tutorial" },
        new Destination { id = "level3", displayName = "Gemini's Blade",
                          sceneNames = new[] { "Level 3", "Level3" }, unlockedBy = "level2" },
    };

    private const string Prefix = "LevelComplete_";

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
