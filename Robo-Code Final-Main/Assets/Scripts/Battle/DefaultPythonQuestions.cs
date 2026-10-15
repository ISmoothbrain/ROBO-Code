using System.Collections.Generic;
using UnityEngine;

// Built-in starter questions, so battles work without creating any assets.
// Your own PythonQuestion assets (Create > Coding > Python Question) are used instead when assigned.
public static class DefaultPythonQuestions
{
    public static PythonQuestion[] Create()
    {
        List<PythonQuestion> list = new List<PythonQuestion>
        {
            Q("Make the attack hit 3 times.",
              new[] { "for i in range(____):", "    attack()" },
              new[] { "3", "2", "4", "\"3\"" }, 0,
              "range(3) gives 0, 1, 2, which is three runs. range(n) always runs n times."),

            Q("The hero should faint when HP runs out.",
              new[] { "if hp ____ 0:", "    faint()" },
              new[] { "<=", "=", "=>", "=!" }, 0,
              "'<=' means 'less than or equal to'. '=' assigns a value, and '=>' isn't valid Python."),

            Q("Send the doubled damage back to whoever called the function.",
              new[] { "def double(dmg):", "    ____ dmg * 2" },
              new[] { "return", "print", "give", "send" }, 0,
              "'return' hands a value back to the caller. 'print' only shows it on screen."),

            Q("Print the hero's name, not the word 'name'.",
              new[] { "name = \"Robo\"", "print(____)" },
              new[] { "name", "\"name\"", "Name", "print" }, 0,
              "Without quotes, name means the variable. With quotes it's just the text \"name\". Python is case-sensitive, so Name is a different variable."),

            Q("Equip the FIRST item in the bag.",
              new[] { "bag = [\"sword\", \"shield\", \"potion\"]", "equip(bag[____])" },
              new[] { "0", "1", "3", "first" }, 0,
              "Python lists start counting at 0, so bag[0] is \"sword\"."),

            Q("Keep fighting while the enemy still has HP.",
              new[] { "while enemy_hp ____ 0:", "    enemy_hp -= 10" },
              new[] { ">", "<", "==", "=" }, 0,
              "'> 0' stays true while HP is positive. '< 0' would never start, and '== 0' would only run once HP is exactly zero."),

            Q("Add 5 to the score so it prints 15.",
              new[] { "score = 10", "score ____ 5", "print(score)  # 15" },
              new[] { "+=", "=", "-=", "==" }, 0,
              "'score += 5' is short for 'score = score + 5'."),

            Q("Count how many coins are in the list.",
              new[] { "coins = [1, 1, 5]", "print(____(coins))  # 3" },
              new[] { "len", "size", "count", "length" }, 0,
              "len() returns how many items something has. Python has no built-in size() or length()."),

            Q("Mark the hero as alive.",
              new[] { "is_alive = ____", "if is_alive:", "    keep_playing()" },
              new[] { "True", "true", "TRUE", "yes" }, 0,
              "Python's booleans are True and False with a capital first letter. 'true' would be an undefined variable."),

            Q("Healing should ADD 20 HP.",
              new[] { "def heal(hp):", "    return hp ____ 20" },
              new[] { "+", "-", "*", "/" }, 0,
              "Healing increases HP, so add with '+'."),

            Q("Only check the second condition if the first one was false.",
              new[] { "if hp > 50:", "    print(\"Healthy\")", "____ hp > 0:", "    print(\"Hurt\")" },
              new[] { "elif", "else if", "else", "if else" }, 0,
              "Python spells it 'elif'. 'else if' works in C#, but not in Python."),

            Q("Show the damage number inside the message.",
              new[] { "dmg = 25", "print(____\"Hit for {dmg}!\")" },
              new[] { "f", "s", "$", "%" }, 0,
              "An f-string (f\"...\") fills in {dmg} with the variable's value. C# uses $ for the same idea.", 1.5f),
        };
        return list.ToArray();
    }

    private static PythonQuestion Q(string goal, string[] code, string[] options, int correct,
                                    string explanation, float damageMultiplier = 1f)
    {
        PythonQuestion q = ScriptableObject.CreateInstance<PythonQuestion>();
        q.name = goal;
        q.goal = goal;
        q.codeLines = code;
        q.options = options;
        q.correctIndex = correct;
        q.explanation = explanation;
        q.damageMultiplier = damageMultiplier;
        return q;
    }
}
