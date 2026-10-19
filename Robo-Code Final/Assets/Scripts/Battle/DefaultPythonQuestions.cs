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
              "An f-string (f\"...\") fills in {dmg} with the variable's value. C# uses $ for the same idea.", 1.5f, 2),

            // ---- medium (2) and hard (3): bosses ask these ----
            Q("Get the LAST item in the bag.",
              new[] { "bag = [\"sword\", \"shield\", \"potion\"]", "print(bag[____])  # potion" },
              new[] { "-1", "3", "last", "len(bag)" }, 0,
              "Negative indexes count from the end, so -1 is the last item. bag[3] would crash: the indexes are 0, 1 and 2.", 1.0f, 2),

            Q("Check if the number is EVEN.",
              new[] { "n = 14", "if n ____ 2 == 0:", "    print(\"even\")" },
              new[] { "%", "/", "//", "**" }, 0,
              "% gives the remainder of a division. An even number has remainder 0 when divided by 2.", 1.0f, 2),

            Q("Repeat the laugh 3 times.",
              new[] { "print(\"ha\" ____ 3)  # hahaha" },
              new[] { "*", "+", "^", "&" }, 0,
              "Multiplying a string by a number repeats it. \"ha\" + 3 would crash, because you can't add text and a number.", 1.0f, 2),

            Q("Loop through every item.",
              new[] { "for item ____ inventory:", "    use(item)" },
              new[] { "in", "of", "from", "at" }, 0,
              "Python's for-loop is always 'for x in something'.", 1.0f, 2),

            Q("Count DOWN from 3 to 1.",
              new[] { "for i in range(3, 0, ____):", "    print(i)  # 3, 2, 1" },
              new[] { "-1", "1", "0", "-3" }, 0,
              "The third number in range() is the step. -1 counts down; 0 isn't allowed and crashes.", 1.0f, 2),

            Q("Look up the potion's price.",
              new[] { "prices = {\"potion\": 15, \"key\": 40}", "print(prices[____])  # 15" },
              new[] { "\"potion\"", "potion", "0", "prices.potion" }, 0,
              "Dictionary keys here are strings, so you need the quotes. Without them, potion is a variable that doesn't exist.", 1.0f, 2),

            Q("Add a coin to the end of the list.",
              new[] { "coins = [1, 5]", "coins.____(10)" },
              new[] { "append", "add", "push", "insert" }, 0,
              "append() adds to the end of a list. Python lists have no add() or push(), and insert() needs a position too.", 1.0f, 2),

            Q("Open the door only if BOTH are true.",
              new[] { "if hp > 0 ____ has_key:", "    open_door()" },
              new[] { "and", "&&", "or", "+" }, 0,
              "Python uses the word 'and'. && is from C# and JavaScript, and 'or' only needs one of them to be true.", 1.0f, 2),

            Q("Shout the name in capitals.",
              new[] { "name = \"robo\"", "print(name.____())  # ROBO" },
              new[] { "upper", "capitalize", "toUpper", "big" }, 0,
              "upper() makes every letter a capital. capitalize() only changes the first letter (Robo).", 1.0f, 2),

            Q("Use 10 damage when no number is given.",
              new[] { "def attack(dmg ____ 10):", "    return dmg" },
              new[] { "=", "==", ":", "->" }, 0,
              "A default value uses a single = inside the brackets. attack() returns 10, and attack(25) returns 25.", 1.0f, 2),

            Q("Stop searching once the boss is found.",
              new[] { "for e in enemies:", "    if e == \"boss\":", "        ____" },
              new[] { "break", "continue", "stop", "exit" }, 0,
              "break leaves the loop straight away. continue would just skip to the next enemy.", 1.0f, 2),

            Q("Skip allies, attack everyone else.",
              new[] { "for unit in units:", "    if unit.is_ally:", "        ____", "    attack(unit)" },
              new[] { "continue", "break", "pass", "return" }, 0,
              "continue jumps to the next loop turn. pass does nothing (so the ally gets attacked), and break would stop the whole loop.", 1.0f, 2),

            Q("How long is this code?",
              new[] { "print(len(\"0101\"))  # prints ____" },
              new[] { "4", "2", "5", "1" }, 0,
              "len() counts every character in the string, and there are four.", 1.0f, 2),

            Q("Leave early if there's no target.",
              new[] { "def strike(target):", "    if target is None:", "        ____", "    target.hp -= 10" },
              new[] { "return", "break", "continue", "stop" }, 0,
              "return exits the function. break and continue only work inside loops.", 1.0f, 2),

            Q("Turn the HP number into text.",
              new[] { "hp = 40", "msg = \"HP: \" + ____(hp)" },
              new[] { "str", "int", "text", "print" }, 0,
              "You can only add text to text, so str(hp) turns 40 into \"40\" first.", 1.0f, 2),

            Q("Read the shield value without crashing.",
              new[] { "stats = {\"hp\": 50}", "print(stats.____(\"shield\", 0))  # 0" },
              new[] { "get", "find", "read", "fetch" }, 0,
              "dict.get(key, default) returns the default if the key is missing. stats[\"shield\"] would crash.", 1.0f, 2),

            Q("Take the first TWO items.",
              new[] { "items = [\"a\", \"b\", \"c\", \"d\"]", "print(items[____])  # ['a', 'b']" },
              new[] { "0:2", "0:1", "1:2", "2" }, 0,
              "A slice start:end includes start but stops BEFORE end, so 0:2 gives the items at 0 and 1.", 1.3f, 3),

            Q("Reverse the word.",
              new[] { "word = \"code\"", "print(word[____])  # edoc" },
              new[] { "::-1", "-1", "::1", ":-1" }, 0,
              "A slice with step -1 walks backwards through the whole string. -1 alone is just the last letter.", 1.3f, 3),

            Q("Double every number in one line.",
              new[] { "nums = [1, 2, 3]", "doubled = [n * 2 ____ n in nums]  # [2, 4, 6]" },
              new[] { "for", "in", "while", "if" }, 0,
              "A list comprehension reads like a for-loop inside brackets: [expression for item in list].", 1.3f, 3),

            Q("Keep only enemies that still have HP.",
              new[] { "alive = [e for e in enemies ____ e.hp > 0]" },
              new[] { "if", "and", "when", "where" }, 0,
              "A filter in a list comprehension goes at the end with 'if'.", 1.3f, 3),

            Q("What does this print?",
              new[] { "print(7 // 2)  # prints ____" },
              new[] { "3", "3.5", "4", "1" }, 0,
              "// is whole-number division: it throws away the remainder. 7 / 2 would give 3.5.", 1.3f, 3),

            Q("What does this print?",
              new[] { "print(10 % 3)  # prints ____" },
              new[] { "1", "3", "0", "3.33" }, 0,
              "10 divided by 3 is 3 remainder 1, and % gives the remainder.", 1.3f, 3),

            Q("How many times does the loop run?",
              new[] { "count = 0", "for i in range(2, 10, 3):", "    count += 1", "print(count)  # prints ____" },
              new[] { "3", "8", "4", "2" }, 0,
              "range(2, 10, 3) gives 2, 5 and 8 (11 would be past 10), so the loop runs 3 times.", 1.3f, 3),

            Q("Fix the never-ending loop.",
              new[] { "hp = 3", "while hp > 0:", "    print(hp)", "    hp ____ 1" },
              new[] { "-=", "+=", "=", "==" }, 0,
              "hp -= 1 lowers hp each turn until it hits 0. += makes it grow forever, and == only compares.", 1.3f, 3),

            Q("What does this print?",
              new[] { "a = [1, 2]", "b = a", "b.append(3)", "print(len(a))  # prints ____" },
              new[] { "3", "2", "1", "0" }, 0,
              "b = a doesn't copy the list. Both names point at the SAME list, so adding to b also changes a.", 1.4f, 3),

            Q("Unpack the position so x is 4.",
              new[] { "x, y = ____", "print(x)  # 4" },
              new[] { "(4, 9)", "[9, 4]", "(4, 9, 0)", "4" }, 0,
              "Unpacking needs exactly two values, in order: x gets 4 and y gets 9. Three values (or one) would crash.", 1.3f, 3),

            Q("What does this print?",
              new[] { "def f(n):", "    if n <= 1:", "        return 1", "    return n * f(n - 1)", "print(f(4))  # prints ____" },
              new[] { "24", "10", "16", "4" }, 0,
              "f calls itself (recursion): 4 * 3 * 2 * 1 = 24.", 1.5f, 3),

            Q("Weak enemies take DOUBLE damage.",
              new[] { "dmg = 10 ____ 2 if weak else 10" },
              new[] { "*", "+", "**", "//" }, 0,
              "10 * 2 = 20 is double. 10 ** 2 is 100 (a power), and 10 + 2 is only 12.", 1.3f, 3),

            Q("Loop with BOTH the index and the item.",
              new[] { "for i, item in ____(bag):", "    print(i, item)" },
              new[] { "enumerate", "range", "zip", "len" }, 0,
              "enumerate() gives (index, item) pairs. range() only gives numbers.", 1.3f, 3),

            Q("Sort scores from highest to lowest.",
              new[] { "scores = [3, 9, 5]", "scores.sort(____=True)  # [9, 5, 3]" },
              new[] { "reverse", "desc", "backwards", "invert" }, 0,
              "sort(reverse=True) sorts biggest first. The other names aren't real options and crash.", 1.3f, 3),

            Q("What does this print?",
              new[] { "x = 5", "x += 3", "x *= 2", "print(x)  # prints ____" },
              new[] { "16", "13", "11", "10" }, 0,
              "5 + 3 = 8, then 8 * 2 = 16. The order of the lines matters.", 1.3f, 3),

            Q("What type is 3.0?",
              new[] { "print(type(3.0).__name__)  # prints ____" },
              new[] { "float", "int", "double", "number" }, 0,
              "Any number with a decimal point is a float in Python, even when it's .0.", 1.3f, 3),

        };
        return list.ToArray();
    }

    private static PythonQuestion Q(string goal, string[] code, string[] options, int correct,
                                    string explanation, float damageMultiplier = 1f, int difficulty = 1)
    {
        PythonQuestion q = ScriptableObject.CreateInstance<PythonQuestion>();
        q.name = goal;
        q.goal = goal;
        q.codeLines = code;
        q.options = options;
        q.correctIndex = correct;
        q.explanation = explanation;
        q.damageMultiplier = damageMultiplier;
        q.difficulty = difficulty;
        return q;
    }
}
