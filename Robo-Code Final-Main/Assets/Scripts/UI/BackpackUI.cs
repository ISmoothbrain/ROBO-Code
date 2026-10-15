using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The backpack: a grid of slots showing your items and stack counts, plus your coins.
// Press I to open/close (Esc also closes). Click an item to see what it is,
// click it again to use it (potions, apples).
// Built automatically by GameUIBootstrap.
public class BackpackUI : MonoBehaviour
{
    public static BackpackUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    public GameObject root;
    public Transform grid;
    public GameObject slotTemplate; // children: "Icon" (Image), "Count" (TextMeshProUGUI)
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI infoTitle;
    public TextMeshProUGUI infoText;
    public Sprite slotSprite;
    public Sprite slotSelectedSprite;

    public KeyCode toggleKey = KeyCode.I;
    [Tooltip("How many slots the backpack has. Also raises the InventoryManager's Max Slots to match.")]
    public int slotCount = 40;

    private readonly List<GameObject> cells = new List<GameObject>();
    private int selected = -1;
    private InventoryManager subscribed;

    private void Awake()
    {
        Instance = this;
        if (root != null) root.SetActive(false);
        if (slotTemplate != null) slotTemplate.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; IsOpen = false; }
        if (subscribed != null) subscribed.OnInventoryChanged -= Refresh;
        CoinCounter.OnCoinsChanged -= OnCoins;
    }

    private void Update()
    {
        if (IsOpen)
        {
            if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(KeyCode.Escape)) Close();
            return;
        }

        if (!UIBusy.AnyOpen && !UIBusy.ClosedThisFrame && Input.GetKeyDown(toggleKey)) Open();
    }

    public void Open()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv != null)
        {
            inv.maxSlots = Mathf.Max(inv.maxSlots, slotCount);
            if (subscribed != inv)
            {
                if (subscribed != null) subscribed.OnInventoryChanged -= Refresh;
                inv.OnInventoryChanged += Refresh;
                subscribed = inv;
            }
        }
        CoinCounter.OnCoinsChanged -= OnCoins;
        CoinCounter.OnCoinsChanged += OnCoins;

        BuildCells();
        selected = -1;
        Refresh();
        OnCoins(CoinCounter.Total);

        root.SetActive(true);
        IsOpen = true;
    }

    public void Close()
    {
        if (!IsOpen) return;
        root.SetActive(false);
        IsOpen = false;
        UIBusy.MarkClosed();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void BuildCells()
    {
        while (cells.Count < slotCount)
        {
            int index = cells.Count;
            GameObject cell = Instantiate(slotTemplate, grid);
            cell.SetActive(true);
            cell.GetComponent<Button>().onClick.AddListener(() => OnCellClicked(index));
            cells.Add(cell);
        }
    }

    private void Refresh()
    {
        if (cells.Count == 0) return; // not opened yet

        List<InventorySlot> slots = InventoryManager.Instance != null ? InventoryManager.Instance.slots : null;
        for (int i = 0; i < cells.Count; i++)
        {
            InventorySlot slot = slots != null && i < slots.Count ? slots[i] : null;
            Image icon = cells[i].transform.Find("Icon").GetComponent<Image>();
            TextMeshProUGUI count = cells[i].transform.Find("Count").GetComponent<TextMeshProUGUI>();

            bool has = slot != null && slot.item != null;
            Sprite picture = has ? ItemIcons.For(slot.item) : null;
            icon.enabled = picture != null;
            if (icon.enabled) icon.sprite = picture;
            count.text = has && slot.count > 1 ? slot.count.ToString() : "";

            cells[i].GetComponent<Image>().sprite = i == selected ? slotSelectedSprite : slotSprite;
        }

        ShowInfo();
    }

    private void OnCellClicked(int index)
    {
        List<InventorySlot> slots = InventoryManager.Instance != null ? InventoryManager.Instance.slots : null;
        InventorySlot slot = slots != null && index < slots.Count ? slots[index] : null;

        if (slot != null && index == selected && Consumables.CanUse(slot.item))
        {
            string msg = Consumables.TryUse(slot.item, FindFirstObjectByType<Player>());
            Refresh();
            if (!string.IsNullOrEmpty(msg)) infoText.text = msg;
            return;
        }

        selected = slot != null ? index : -1;
        Refresh();
    }

    private void ShowInfo()
    {
        List<InventorySlot> slots = InventoryManager.Instance != null ? InventoryManager.Instance.slots : null;
        InventorySlot slot = slots != null && selected >= 0 && selected < slots.Count ? slots[selected] : null;

        if (slot == null || slot.item == null)
        {
            selected = -1;
            infoTitle.text = "";
            infoText.text = "Click an item to see what it is.";
            return;
        }

        infoTitle.text = slot.count > 1 ? $"{slot.item.itemName}  x{slot.count}" : slot.item.itemName;
        string hint = Consumables.CanUse(slot.item) ? "\n<color=#7FE3FF>Click again to use.</color>" : "";
        infoText.text = (slot.item.description ?? "") + hint;
    }

    private void OnCoins(int total)
    {
        if (coinsText != null) coinsText.text = total.ToString();
    }
}
