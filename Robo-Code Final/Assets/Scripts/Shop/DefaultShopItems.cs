using UnityEngine;

// Stock used when a shop has no Shop Items of its own assigned.
public static class DefaultShopItems
{
    private static ShopItem[] items;
    private static InventoryItem potion;

    // The inventory item the default Health Potion puts in the bag (PotionUser drinks these).
    public static InventoryItem HealthPotion
    {
        get
        {
            if (potion == null)
            {
                potion = ScriptableObject.CreateInstance<InventoryItem>();
                potion.name = "Health Potion";
                potion.itemName = "Health Potion";
                potion.description = "Restores 30 HP. Press H to drink.";
                potion.isStackable = true;
            }
            return potion;
        }
    }

    public static ShopItem[] Get()
    {
        if (items != null) return items;

        items = new[]
        {
            Make("Health Potion", "Restores 30 HP when you drink it (press H).", 15,
                 ShopEffect.AddToInventory, 30, new Color(0.95f, 0.3f, 0.35f), HealthPotion),
            Make("Damage Boost", "Permanently +5 damage to your attacks and correct answers.", 40,
                 ShopEffect.DamageBoost, 5, new Color(1f, 0.65f, 0.2f)),
            Make("Heart Container", "Permanently +20 max HP.", 60,
                 ShopEffect.MaxHealthUp, 20, new Color(1f, 0.4f, 0.75f)),
            Make("Quick Snack", "Heals 15 HP right now.", 5,
                 ShopEffect.HealNow, 15, new Color(0.5f, 0.9f, 0.45f)),
        };
        return items;
    }

    private static ShopItem Make(string name, string desc, int price, ShopEffect effect, int amount,
                                 Color tint, InventoryItem inv = null)
    {
        ShopItem s = ScriptableObject.CreateInstance<ShopItem>();
        s.name = name;
        s.itemName = name;
        s.description = desc;
        s.price = price;
        s.effect = effect;
        s.amount = amount;
        s.iconTint = tint;
        s.inventoryItem = inv;
        return s;
    }
}
