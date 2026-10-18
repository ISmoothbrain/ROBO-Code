using UnityEngine;

// Stock used when a shop has no Shop Items of its own assigned.
// Icons come from Assets/Resources/Icons.
public static class DefaultShopItems
{
    private static ShopItem[] items;
    private static InventoryItem healthPotion, bigHealthPotion, manaPotion;

    public static Sprite Icon(string name) => Resources.Load<Sprite>("Icons/" + name);

    // Note: Unity objects need an explicit "== null" check here. Runtime-made items are destroyed
    // when Play mode stops, and "??=" would keep handing out those destroyed (empty) items.
    public static InventoryItem HealthPotion
    {
        get { if (healthPotion == null) healthPotion = Item("Health Potion", "potion_red", "Restores 30 HP. Click it here, or press H."); return healthPotion; }
    }
    public static InventoryItem BigHealthPotion
    {
        get { if (bigHealthPotion == null) bigHealthPotion = Item("Big Health Potion", "potion_big_red", "Restores 80 HP. Click it here to drink."); return bigHealthPotion; }
    }
    public static InventoryItem ManaPotion
    {
        get { if (manaPotion == null) manaPotion = Item("Mana Potion", "potion_blue", "Restores 50 mana. Click it here to drink."); return manaPotion; }
    }

    public static ShopItem[] Get()
    {
        if (items != null && System.Array.TrueForAll(items, i => i != null)) return items;

        items = new[]
        {
            Make("Health Potion", "Restores 30 HP. Goes in your backpack (press H to drink).", 15,
                 ShopEffect.AddToInventory, 30, "potion_red", HealthPotion),
            Make("Big Health Potion", "Restores 80 HP. Goes in your backpack.", 25,
                 ShopEffect.AddToInventory, 80, "potion_big_red", BigHealthPotion),
            Make("Mana Potion", "Restores 50 mana for your crafted skills.", 15,
                 ShopEffect.AddToInventory, 50, "potion_blue", ManaPotion),
            Make("Damage Boost", "Permanently +5 damage to attacks and correct answers.", 30,
                 ShopEffect.DamageBoost, 5, "fire"),
            Make("Heart Container", "Permanently +20 max HP (two more hearts).", 50,
                 ShopEffect.MaxHealthUp, 20, "heart_full"),
            Make("Apple", "Heals 15 HP right away.", 5,
                 ShopEffect.HealNow, 15, "apple"),
        };
        return items;
    }

    private static InventoryItem Item(string name, string icon, string desc)
    {
        InventoryItem i = ScriptableObject.CreateInstance<InventoryItem>();
        i.name = name;
        i.itemName = name;
        i.icon = Icon(icon);
        i.description = desc;
        i.isStackable = true;
        return i;
    }

    private static ShopItem Make(string name, string desc, int price, ShopEffect effect, int amount,
                                 string icon, InventoryItem inv = null)
    {
        ShopItem s = ScriptableObject.CreateInstance<ShopItem>();
        s.name = name;
        s.itemName = name;
        s.description = desc;
        s.price = price;
        s.effect = effect;
        s.amount = amount;
        s.icon = Icon(icon);
        s.inventoryItem = inv;
        return s;
    }
}
