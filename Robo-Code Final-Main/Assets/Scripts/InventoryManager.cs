using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public List<InventorySlot> slots = new List<InventorySlot>();
    public int maxSlots = 20;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool AddItem(InventoryItem item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        if (item.isStackable)
        {
            InventorySlot existingSlot = slots.Find(s => s.item == item);
            if (existingSlot != null)
            {
                existingSlot.count += amount;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        if (slots.Count >= maxSlots)
        {
            Debug.Log("Inventory full!");
            return false;
        }

        slots.Add(new InventorySlot(item, amount));
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(InventoryItem item, int amount = 1)
    {
        InventorySlot slot = slots.Find(s => s.item == item);
        if (slot == null) return false;

        slot.count -= amount;
        if (slot.count <= 0)
        {
            slots.Remove(slot);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }
}
