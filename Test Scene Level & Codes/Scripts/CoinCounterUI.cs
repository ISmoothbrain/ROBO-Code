using UnityEngine;
using TMPro;

// Put this on the Text (TMP) object you want to use as the coin counter, and
// drag that same TextMeshProUGUI component into the Coin Text field below.
// It updates itself automatically whenever CoinCounter.Add() runs, which
// Coin.cs already calls on pickup — nothing else to wire.
public class CoinCounterUI : MonoBehaviour
{
    public TextMeshProUGUI coinText;

    [Tooltip("{0} is replaced with the coin total, e.g. \"Coins: {0}\" or just \"{0}\".")]
    public string format = "{0}";

    void OnEnable()
    {
        CoinCounter.OnCoinsChanged += Refresh;
        Refresh(CoinCounter.Total);
    }

    void OnDisable()
    {
        CoinCounter.OnCoinsChanged -= Refresh;
    }

    private void Refresh(int total)
    {
        if (coinText != null)
        {
            coinText.text = string.Format(format, total);
        }
    }
}