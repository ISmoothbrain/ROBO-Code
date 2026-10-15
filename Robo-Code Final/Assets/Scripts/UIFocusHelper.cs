using UnityEngine;
using UnityEngine.EventSystems;

// Attach to the EventSystem object. Hook a UI element's event
// (e.g. Slider.OnValueChanged) to ClearSelection() so the UI stops
// "holding" input focus after the player is done using it — otherwise
// any movement code that checks EventSystem.current.currentSelectedGameObject
// stays blocked indefinitely.
public class UIFocusHelper : MonoBehaviour
{
    public void ClearSelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
