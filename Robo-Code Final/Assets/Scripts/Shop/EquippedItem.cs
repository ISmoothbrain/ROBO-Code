// The item in the HUD's quick slot. Right-click an item in the backpack to equip it,
// then press the Use Item key (F by default, change it in Settings > Controls) to use one.
public static class EquippedItem
{
    public static InventoryItem Item { get; private set; }
    public static event System.Action OnChanged;

    public static void Equip(InventoryItem item)
    {
        Item = item;
        OnChanged?.Invoke();
    }

    public static void Clear()
    {
        if (Item == null) return;
        Item = null;
        OnChanged?.Invoke();
    }

    // How many of the equipped item are in the backpack right now.
    public static int Count()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null || Item == null) return 0;
        int n = 0;
        foreach (InventorySlot s in inv.slots) if (s.item == Item) n += s.count;
        return n;
    }
}
