using UnityEngine;

public enum ChallengeType
{
    MultipleChoice, // pick the right answer from Options
    FindTheBug      // click the broken line in Code Lines
}

// One lesson + puzzle. Create via right-click in the Project window:
// Create > Coding > Code Challenge
[CreateAssetMenu(fileName = "New Challenge", menuName = "Coding/Code Challenge")]
public class CodeChallenge : ScriptableObject
{
    [Tooltip("Unique ID used for saving. Leave empty to use the asset name.")]
    public string id;
    public string title;

    [Header("Lesson (teaches the concept before the puzzle)")]
    [TextArea(3, 8)] public string lesson;

    [Header("Puzzle")]
    public ChallengeType type;
    [TextArea(2, 4)] public string question;
    [TextArea(1, 2)] public string[] codeLines;
    public string[] options;      // MultipleChoice only
    [Tooltip("MultipleChoice: index of the correct option. FindTheBug: index of the buggy line (first line = 0).")]
    public int correctIndex;
    [TextArea] public string hint;
    [TextArea] public string explanation; // shown once solved

    [Header("Progression")]
    public CodeChallenge[] prerequisites; // must be solved before this one unlocks
    public SkillData rewardSkill;         // optional: skill unlocked when solved

    public string Id => string.IsNullOrEmpty(id) ? name : id;
}
