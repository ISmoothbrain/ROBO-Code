using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI countText;

    public void Setup(InventoryItem item, int count)
    {
        if (item.icon != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
        }

        countText.text = item.isStackable && count > 1 ? count.ToString() : "";
    }
}
