using UnityEngine;
using UnityEngine.EventSystems;

// Lets a backpack slot react to RIGHT-clicks (Unity buttons only handle left-clicks).
// Right-click = equip the item to the HUD's quick slot.
public class BackpackSlotClick : MonoBehaviour, IPointerClickHandler
{
    public int index;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && BackpackUI.Instance != null)
            BackpackUI.Instance.EquipCell(index);
    }
}
