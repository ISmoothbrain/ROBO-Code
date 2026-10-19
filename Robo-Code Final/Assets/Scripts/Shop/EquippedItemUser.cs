using UnityEngine;

// Equipped items are deliberately separate from the H-key healing shortcut.
// The quick-use slot works for any inventory item that Consumables knows how to use.
public class EquippedItemUser : MonoBehaviour
{
    private Player player;
    private void Awake() { player = GetComponent<Player>(); }
    private void Update()
    {
        if (player == null || player.IsDead || UIBusy.AnyOpen || BattleManager.InBattle) return;
        if (!Input.GetKeyDown(Keybinds.Get("UseEquippedItem"))) return;
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null || inv.EquippedItem == null) return;
        string message = Consumables.TryUse(inv.EquippedItem, player);
        if (!string.IsNullOrEmpty(message)) Debug.Log("[Quick Item] " + message);
    }
}
