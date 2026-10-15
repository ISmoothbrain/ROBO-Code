using UnityEngine;
using UnityEngine.UI;

// Put this on a "Reset to defaults" button in the keybinds menu.
public class KeybindResetButton : MonoBehaviour
{
    void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(ResetAll);
        }
    }

    public void ResetAll()
    {
        Keybinds.ResetAll();

        // finds the rows itself rather than needing each one dragged into a list,
        // and includes inactive ones in case the panel is hidden
        KeybindRow[] rows = FindObjectsByType<KeybindRow>(FindObjectsInactive.Include);

        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].RefreshLabel();
        }
    }
}
