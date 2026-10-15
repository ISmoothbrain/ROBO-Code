using UnityEngine;

// Finds a picture for items that don't have an Icon set, so nothing shows up as an empty slot.
// Looks in Assets/Resources/Icons by the item's name. Setting an Icon on the item always wins.
public static class ItemIcons
{
    public static Sprite For(InventoryItem item) => item == null ? null : (item.icon != null ? item.icon : Guess(item.itemName));

    public static Sprite For(ShopItem item) => item == null ? null : (item.icon != null ? item.icon : Guess(item.itemName));

    public static Sprite Guess(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;
        string n = itemName.ToLowerInvariant();

        // An exact file name first, e.g. "Apple" -> Icons/apple, "Key Iron" -> Icons/key_iron
        Sprite exact = Resources.Load<Sprite>("Icons/" + n.Replace(' ', '_'));
        if (exact != null) return exact;

        if (n.Contains("mana")) return Load(n.Contains("big") ? "potion_big_blue" : "potion_blue");
        if (n.Contains("potion") || n.Contains("heal")) return Load(n.Contains("big") ? "potion_big_red" : "potion_red");
        if (n.Contains("heart")) return Load("heart_full");
        if (n.Contains("key")) return Load("key_iron");
        if (n.Contains("book")) return Load("book_red");
        if (n.Contains("coin")) return Load("coin");
        return null;
    }

    private static Sprite Load(string file) => Resources.Load<Sprite>("Icons/" + file);
}
