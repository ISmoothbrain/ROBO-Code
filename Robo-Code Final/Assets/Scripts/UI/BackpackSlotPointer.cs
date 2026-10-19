using UnityEngine;
using UnityEngine.EventSystems;

// Unity UI Buttons normally react to left clicks only. This routes right-clicks
// through the existing EventSystem without requiring a second invisible button.
public class BackpackSlotPointer : MonoBehaviour, IPointerClickHandler
{
    public BackpackUI backpack;
    public int index;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && backpack != null)
            backpack.ToggleEquip(index);
    }
}
