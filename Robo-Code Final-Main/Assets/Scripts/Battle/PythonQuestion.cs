using UnityEngine;

// One "fix the code" question for turn-based battles.
// Write the code with ____ (four underscores) where the blank goes.
// Create via: Create > Coding > Python Question
[CreateAssetMenu(fileName = "New Python Question", menuName = "Coding/Python Question")]
public class PythonQuestion : ScriptableObject
{
    public const string Blank = "____";

    [Tooltip("Short goal shown above the code, e.g. 'Make the loop run 3 times'.")]
    public string goal;

    [TextArea(1, 2)]
    [Tooltip("One entry per line of Python. Put ____ where the missing piece goes. Use spaces for indentation.")]
    public string[] codeLines;

    public string[] options;
    [Tooltip("Index of the correct option (first option = 0).")]
    public int correctIndex;

    [TextArea] public string explanation; // shown after answering, right or wrong

    [Tooltip("Optional: scales damage for harder questions.")]
    public float damageMultiplier = 1f;
}
