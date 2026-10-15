using UnityEngine;

public enum ShopEffect
{
    AddToInventory, // goes in the bag; potions are drunk with the Potion User's key (H)
    HealNow,        // instant heal of Amount HP
    DamageBoost,    // +Amount damage, permanently
    MaxHealthUp     // +Amount max HP, permanently (and heals that much)
}

// Something the shop sells. Create via: Create > Shop > Shop Item
[CreateAssetMenu(fileName = "New Shop Item", menuName = "Shop/Shop Item")]
public class ShopItem : ScriptableObject
{
    public string itemName = "Health Potion";
    [TextArea] public string description;
    public Sprite icon;
    public Color iconTint = Color.white; // used as the icon colour when there's no sprite
    public int price = 15;

    public ShopEffect effect = ShopEffect.AddToInventory;
    public int amount = 30;

    [Tooltip("Only for Add To Inventory: the inventory item to add.")]
    public InventoryItem inventoryItem;

    // Returns false (and a reason) if the purchase couldn't be applied, so the coins get refunded.
    public bool Apply(Player player, out string message)
    {
        switch (effect)
        {
            case ShopEffect.AddToInventory:
                if (inventoryItem == null || InventoryManager.Instance == null)
                {
                    message = "There's nowhere to put that.";
                    return false;
                }
                if (!InventoryManager.Instance.AddItem(inventoryItem, 1))
                {
                    message = "Your bag is full!";
                    return false;
                }
                message = $"{itemName} added to your backpack (I).";
                return true;

            case ShopEffect.HealNow:
                if (player == null) { message = "No one to heal."; return false; }
                player.Heal(amount);
                message = $"Restored {amount} HP.";
                return true;

            case ShopEffect.DamageBoost:
                PlayerUpgrades.BonusDamage += amount;
                message = $"Damage +{amount}! (Total bonus: +{PlayerUpgrades.BonusDamage})";
                return true;

            case ShopEffect.MaxHealthUp:
                PlayerUpgrades.BonusMaxHealth += amount;
                if (player != null)
                {
                    player.maxHealth += amount;
                    player.Heal(amount);
                }
                message = $"Max HP +{amount}!";
                return true;
        }

        message = "";
        return false;
    }
}
