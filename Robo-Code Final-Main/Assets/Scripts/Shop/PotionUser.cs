using UnityEngine;

// Press H to drink a healing item from the backpack. Added to the Player automatically.
// Uses the weakest one first so big potions are saved for emergencies.
public class PotionUser : MonoBehaviour
{
    public KeyCode drinkKey = KeyCode.H;

    private Player player;

    private void Start()
    {
        player = GetComponent<Player>();
    }

    private void Update()
    {
        if (player == null || player.IsDead || UIBusy.AnyOpen) return;
        if (!Input.GetKeyDown(drinkKey)) return;

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        InventorySlot best = null;
        foreach (InventorySlot s in inv.slots)
        {
            int heal = Consumables.HealAmount(s.item);
            if (heal > 0 && (best == null || heal < Consumables.HealAmount(best.item))) best = s;
        }

        if (best == null)
        {
            Debug.Log("No healing items. Buy some at the shop!");
            return;
        }
        Consumables.TryUse(best.item, player);
    }
}
