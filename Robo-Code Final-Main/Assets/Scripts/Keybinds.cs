using System.Collections.Generic;
using UnityEngine;

// Holds the key for each action and remembers changes between sessions.
// Static so any script can ask for a key without needing a reference.
public static class Keybinds
{
    // the order the settings menu shows them in
    public static readonly string[] Actions = { "Jump", "MoveLeft", "MoveRight", "Attack", "Inventory" };

    private static readonly Dictionary<string, KeyCode> defaults = new Dictionary<string, KeyCode>
    {
        { "Jump", KeyCode.Space },
        { "MoveLeft", KeyCode.A },
        { "MoveRight", KeyCode.D },
        { "Attack", KeyCode.Mouse0 },
        { "Inventory", KeyCode.I }
    };

    private static Dictionary<string, KeyCode> current = new Dictionary<string, KeyCode>(defaults);

    // runs once when the game starts, before any scene loads, so saved keys
    // are in place before anything asks for them
    [RuntimeInitializeOnLoadMethod]
    private static void Load()
    {
        foreach (string action in Actions)
        {
            string saved = PlayerPrefs.GetString(PrefKey(action), "");

            // a saved value could be nonsense if someone edited it, so check before parsing
            if (saved != "" && System.Enum.IsDefined(typeof(KeyCode), saved))
            {
                current[action] = (KeyCode)System.Enum.Parse(typeof(KeyCode), saved);
            }
        }
    }

    // prefixed so these can't clash with MusicVolume, SFXVolume and the rest
    private static string PrefKey(string action)
    {
        return "Keybind_" + action;
    }

    public static KeyCode Get(string action)
    {
        return current.ContainsKey(action) ? current[action] : KeyCode.None;
    }

    // returns the action already using this key, or null if it's free.
    // ignoreAction is the one being rebound, since keeping its own key is fine
    public static string ActionUsing(KeyCode key, string ignoreAction)
    {
        foreach (KeyValuePair<string, KeyCode> pair in current)
        {
            if (pair.Key != ignoreAction && pair.Value == key)
            {
                return pair.Key;
            }
        }

        return null;
    }

    public static void Set(string action, KeyCode key)
    {
        if (!current.ContainsKey(action))
        {
            return;
        }

        current[action] = key;
        PlayerPrefs.SetString(PrefKey(action), key.ToString());
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        foreach (string action in Actions)
        {
            current[action] = defaults[action];
            PlayerPrefs.DeleteKey(PrefKey(action));
        }

        PlayerPrefs.Save();
    }

    // "Mouse0" means nothing to a player, so the menu shows this instead
    public static string Label(string action)
    {
        KeyCode key = Get(action);

        if (key == KeyCode.Mouse0) return "Left Click";
        if (key == KeyCode.Mouse1) return "Right Click";
        if (key == KeyCode.Mouse2) return "Middle Click";

        return key.ToString();
    }
}
