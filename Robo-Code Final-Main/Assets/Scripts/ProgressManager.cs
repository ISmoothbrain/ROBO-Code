using System;
using System.Collections.Generic;
using UnityEngine;

// Tracks solved challenges and the skills they unlock, and saves them between sessions.
// Put on a ROOT GameObject (not a child) in your first gameplay scene, like InventoryManager.
public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance { get; private set; }

    [Tooltip("Every CodeChallenge in the game, so saved progress can be mapped back to assets.")]
    public CodeChallenge[] allChallenges;

    public event Action OnProgressChanged;

    private const string SaveKey = "CodeChallengeProgress";
    private readonly HashSet<string> completed = new HashSet<string>();
    private readonly List<SkillData> unlockedSkills = new List<SkillData>();

    public IReadOnlyList<SkillData> UnlockedSkills => unlockedSkills;

    [Serializable]
    private class SaveData { public List<string> completed = new List<string>(); }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public bool IsCompleted(CodeChallenge c) => c != null && completed.Contains(c.Id);

    public bool IsAvailable(CodeChallenge c)
    {
        if (c == null) return false;
        if (c.prerequisites == null) return true;
        foreach (CodeChallenge pre in c.prerequisites)
        {
            if (pre != null && !IsCompleted(pre)) return false;
        }
        return true;
    }

    public void CompleteChallenge(CodeChallenge c)
    {
        if (c == null || !completed.Add(c.Id)) return;
        RebuildSkills();
        Save();
        OnProgressChanged?.Invoke();
    }

    private void RebuildSkills()
    {
        unlockedSkills.Clear();
        if (allChallenges == null) return;
        foreach (CodeChallenge c in allChallenges)
        {
            if (c != null && c.rewardSkill != null && IsCompleted(c) && !unlockedSkills.Contains(c.rewardSkill))
            {
                unlockedSkills.Add(c.rewardSkill);
            }
        }
    }

    private void Save()
    {
        SaveData data = new SaveData { completed = new List<string>(completed) };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        completed.Clear();
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data != null) foreach (string id in data.completed) completed.Add(id);
        }
        RebuildSkills();
    }

    // Right-click the component header in the Inspector > Reset Progress (handy for testing).
    [ContextMenu("Reset Progress")]
    public void ResetProgress()
    {
        completed.Clear();
        unlockedSkills.Clear();
        PlayerPrefs.DeleteKey(SaveKey);
        OnProgressChanged?.Invoke();
        Debug.Log("Code challenge progress reset.");
    }
}
