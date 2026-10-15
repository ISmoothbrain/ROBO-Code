using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One row of the keybinds menu. Click it, press a key, and that action is rebound.
public class KeybindRow : MonoBehaviour
{
    public string action = "Jump";
    public TextMeshProUGUI keyLabel;
    public TextMeshProUGUI messageLabel;

    private bool waitingForKey;
    private int skipFrames;

    void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(BeginRebind);
        }

        RefreshLabel();
        if (messageLabel != null)
        {
            messageLabel.text = "";
        }
    }

    public void BeginRebind()
    {
        if (waitingForKey)
        {
            return;
        }

        waitingForKey = true;

        // the mouse click that got us here is still counted as "down" this frame,
        // so wait one frame or Attack instantly rebinds itself to left click
        skipFrames = 1;

        if (keyLabel != null) keyLabel.text = "Press a key";
        if (messageLabel != null) messageLabel.text = "Esc to cancel";
    }

    void Update()
    {
        if (!waitingForKey)
        {
            return;
        }

        if (skipFrames > 0)
        {
            skipFrames--;
            return;
        }

        if (!Input.anyKeyDown)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Stop("");
            return;
        }

        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(key))
            {
                continue;
            }

            string clash = Keybinds.ActionUsing(key, action);

            if (clash != null)
            {
                Stop("Already used by " + clash);
            }
            else
            {
                Keybinds.Set(action, key);
                Stop("");
            }

            return;
        }
    }

    private void Stop(string message)
    {
        waitingForKey = false;
        RefreshLabel();

        if (messageLabel != null)
        {
            messageLabel.text = message;
        }
    }

    public void RefreshLabel()
    {
        if (keyLabel != null)
        {
            keyLabel.text = Keybinds.Label(action);
        }
    }
}
