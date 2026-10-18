using UnityEngine;

// What happens when an item is used from the backpack (click) or with the H key.
// Matched by item name, so your own InventoryItem assets work too if they use these names.
public static class Consumables
{
    public static bool IsHealing(InventoryItem item) => HealAmount(item) > 0;

    public static int HealAmount(InventoryItem item)
    {
        if (item == null) return 0;
        switch (item.itemName)
        {
            case "Health Potion": return 30;
            case "Healing Potion": return 50; // matches the description on your HealingPotion asset
            case "Big Health Potion": return 80;
            case "Apple": return 15;
            default: return 0;
        }
    }

    public static int ManaAmount(InventoryItem item)
    {
        if (item == null) return 0;
        return item.itemName == "Mana Potion" ? 50 : 0;
    }

    public static bool CanUse(InventoryItem item) =>
        HealAmount(item) > 0 || ManaAmount(item) > 0 || (item != null && item.itemName == HackCrafting.FragmentName);

    // Uses one of the item. Returns a message for the UI, or null if nothing happened.
    public static string TryUse(InventoryItem item, Player player)
    {
        if (item == null || player == null || player.IsDead || InventoryManager.Instance == null) return null;

        // The Hack Fragment opens the crafting window instead of being consumed.
        if (item.itemName == HackCrafting.FragmentName)
        {
            if (BackpackUI.Instance != null && BackpackUI.IsOpen) BackpackUI.Instance.Close();
            HackCrafting.Open();
            return "Fix the hack script to craft HACK.";
        }

        int heal = HealAmount(item);
        int mana = ManaAmount(item);
        if (heal == 0 && mana == 0) return $"{item.itemName} can't be used.";

        if (heal > 0)
        {
            if (player.health >= player.maxHealth) return "You're already at full health.";
            InventoryManager.Instance.RemoveItem(item, 1);
            player.Heal(heal);
            return $"Restored {heal} HP.";
        }

        PlayerSkills skills = player.GetComponent<PlayerSkills>();
        if (skills == null) return "You don't use mana yet.";
        if (skills.Mana >= skills.maxMana) return "Your mana is already full.";
        InventoryManager.Instance.RemoveItem(item, 1);
        skills.RestoreMana(mana);
        return $"Restored {mana} mana.";
    }
}
