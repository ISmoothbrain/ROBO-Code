using UnityEngine;

// Press H to drink a Health Potion from the inventory. Added to the Player automatically.
public class PotionUser : MonoBehaviour
{
    public KeyCode drinkKey = KeyCode.H;
    public int healAmount = 30;
    [Tooltip("Inventory items with any of these names count as health potions.")]
    public string[] potionNames = { "Health Potion", "Healing Potion" };

    private Player player;

    private void Start()
    {
        player = GetComponent<Player>();
    }

    private void Update()
    {
        if (player == null || player.IsDead) return;
        if (BattleManager.InBattle || DialogueUI.IsOpen || ShopUI.IsOpen || CodeChallengeUI.IsOpen) return;
        if (!Input.GetKeyDown(drinkKey)) return;

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        InventorySlot slot = inv.slots.Find(s => s.item != null && System.Array.IndexOf(potionNames, s.item.itemName) >= 0);
        if (slot == null)
        {
            Debug.Log("No health potions. Buy some at the shop!");
            return;
        }
        if (player.health >= player.maxHealth) return; // don't waste one at full HP

        inv.RemoveItem(slot.item, 1);
        player.Heal(healAmount);
    }
}
