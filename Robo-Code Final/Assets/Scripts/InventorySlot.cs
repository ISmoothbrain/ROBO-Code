[System.Serializable]
public class InventorySlot
{
    public InventoryItem item;
    public int count;

    public InventorySlot(InventoryItem item, int count)
    {
        this.item = item;
        this.count = count;
    }
}
