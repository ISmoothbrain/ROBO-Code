using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The shop window: a list of items with prices and Buy buttons, paid for with coins.
// Built automatically by GameUIBootstrap; shops call ShopUI.Instance.Open(...).
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    public GameObject root;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI feedbackText;
    public Transform itemContainer;
    public GameObject rowTemplate; // children named Icon, Name, Description, Price, Buy
    public Button closeButton;

    public Color affordableColor = new Color(1f, 0.85f, 0.3f);
    public Color tooExpensiveColor = new Color(0.7f, 0.4f, 0.4f);

    private ShopItem[] stock;
    private readonly List<(ShopItem item, Button buy, TextMeshProUGUI price)> rows =
        new List<(ShopItem, Button, TextMeshProUGUI)>();
    private float openedAt;

    private void Awake()
    {
        Instance = this;
        if (root != null) root.SetActive(false);
        if (rowTemplate != null) rowTemplate.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; IsOpen = false; }
        CoinCounter.OnCoinsChanged -= OnCoinsChanged;
    }

    private void Update()
    {
        if (IsOpen && Time.unscaledTime - openedAt > 0.15f && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    public void Open(string title, ShopItem[] items)
    {
        // Use the shop's own items if it has any real ones; empty Inspector slots don't count.
        stock = items != null && System.Array.Exists(items, i => i != null) ? items : DefaultShopItems.Get();
        titleText.text = title;
        feedbackText.text = "";

        foreach (var r in rows) if (r.buy != null) Destroy(r.buy.transform.parent.gameObject);
        rows.Clear();

        foreach (ShopItem item in stock)
        {
            if (item == null) continue;
            GameObject row = Instantiate(rowTemplate, itemContainer);
            row.SetActive(true);

            Image icon = row.transform.Find("Icon").GetComponent<Image>();
            icon.sprite = ItemIcons.For(item);
            icon.color = icon.sprite != null ? Color.white : item.iconTint;
            icon.preserveAspect = true;

            row.transform.Find("Texts/Name").GetComponent<TextMeshProUGUI>().text = item.itemName;
            row.transform.Find("Texts/Description").GetComponent<TextMeshProUGUI>().text = item.description;
            TextMeshProUGUI price = row.transform.Find("Price").GetComponent<TextMeshProUGUI>();
            price.text = $"{item.price} coins";

            Button buy = row.transform.Find("Buy").GetComponent<Button>();
            ShopItem captured = item;
            buy.onClick.AddListener(() => Buy(captured));
            rows.Add((item, buy, price));
        }

        if (rows.Count == 0) Feedback("Nothing for sale right now.", false);

        CoinCounter.OnCoinsChanged -= OnCoinsChanged;
        CoinCounter.OnCoinsChanged += OnCoinsChanged;
        OnCoinsChanged(CoinCounter.Total);

        root.SetActive(true);
        IsOpen = true;
        openedAt = Time.unscaledTime;
    }

    public void Close()
    {
        if (!IsOpen) return;
        CoinCounter.OnCoinsChanged -= OnCoinsChanged;
        root.SetActive(false);
        IsOpen = false;
        UIBusy.MarkClosed();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void Buy(ShopItem item)
    {
        if (!CoinCounter.TrySpend(item.price))
        {
            Feedback($"You need {item.price - CoinCounter.Total} more coins.", false);
            return;
        }

        Player player = FindFirstObjectByType<Player>();
        if (item.Apply(player, out string message))
        {
            Feedback(message, true);
        }
        else
        {
            CoinCounter.Add(item.price); // refund
            Feedback(message, false);
        }
    }

    private void Feedback(string message, bool good)
    {
        feedbackText.text = message;
        feedbackText.color = good ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.5f, 0.5f);
    }

    private void OnCoinsChanged(int total)
    {
        coinsText.text = $"Coins: {total}";
        foreach (var r in rows)
        {
            bool canAfford = total >= r.item.price;
            r.price.color = canAfford ? affordableColor : tooExpensiveColor;
        }
    }
}
