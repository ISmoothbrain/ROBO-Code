using TMPro;
using UnityEngine;

// Shared bits for the NPC scripts: the floating "[E] Talk" prompt, distance checks,
// and filling in the player's real key bindings in lines like "press {MoveLeft} & {MoveRight}".
public static class NpcUtil
{
    public static TextMeshPro MakePrompt(Transform owner, float height)
    {
        GameObject go = new GameObject("Prompt");
        go.transform.SetParent(owner, false);
        go.transform.localPosition = new Vector3(0f, height, 0f);
        TextMeshPro t = go.AddComponent<TextMeshPro>();
        UIFont.Apply(t);
        t.fontSize = 4f;
        t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.sizeDelta = new Vector2(8f, 1f);
        t.sortingOrder = 200;
        go.SetActive(false);
        return t;
    }

    public static Player FindPlayer() => Object.FindAnyObjectByType<Player>();

    public static bool Near(Player p, Transform t, float range) =>
        p != null && !p.IsDead && Vector2.Distance(p.transform.position, t.position) <= range;

    // {MoveLeft}, {MoveRight}, {Jump}, {Attack}, {Inventory}, {UseItem} -> the keys the player actually uses.
    public static string Keys(string line)
    {
        if (string.IsNullOrEmpty(line)) return line;
        foreach (string action in Keybinds.Actions)
        {
            string token = "{" + action + "}";
            if (!line.Contains(token)) continue;
            string label = Keybinds.Label(action);
            if (label == "Space") label = "SPACE BAR";
            line = line.Replace(token, label.ToUpperInvariant());
        }
        return line;
    }

    public static string[] Keys(string[] lines)
    {
        if (lines == null) return new string[0];
        string[] o = new string[lines.Length];
        for (int i = 0; i < lines.Length; i++) o[i] = Keys(lines[i]);
        return o;
    }
}
