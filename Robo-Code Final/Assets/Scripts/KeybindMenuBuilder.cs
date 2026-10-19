using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Builds the keybinds screen the first time the panel opens, so the rows don't
// have to be laid out by hand. Everything is placed straight onto the panel at a
// share of the screen, so there is no window or background behind it.
// Put this on KeybindsPanel.
public class KeybindMenuBuilder : MonoBehaviour
{
    [Header("Images")]
    public Sprite titleSprite;

    [Tooltip("One per action, in the same order as the rows: Jump, Move Left, Move Right, Attack, Inventory, Use Equipped Item.")]
    public Sprite[] nameSprites;

    public Sprite keyPlateSprite;
    public Sprite resetSprite;

    [Header("Font for the key names only")]
    [Tooltip("The bound key changes while playing, so it has to stay as text.")]
    public TMP_FontAsset font;

    // all of these are a share of the screen, 0 at the bottom and 1 at the top,
    // so the menu lines up the same way at any resolution
    [Header("Title")]
    [Tooltip("Off by default. The panel is already reached from a Keybinds button, so the heading just repeats it.")]
    public bool showTitle = false;

    public float titleLeft = 0.34f;
    public float titleRight = 0.66f;
    public float titleBottom = 0.88f;
    public float titleTop = 0.98f;

    [Header("Rows")]
    public float firstRowTop = 0.855f;
    public float rowHeight = 0.062f;
    public float rowGap = 0.014f;

    [Header("Row columns, left to right")]
    public float nameLeft = 0.22f;
    public float nameRight = 0.50f;
    public float keyLeft = 0.54f;
    public float keyRight = 0.78f;

    [Header("Reset button")]
    [Tooltip("Drag KeybindBackButton in and Reset copies its size and sits right above it.")]
    public RectTransform backButton;
    public float resetGap = 6f;

    // only used when no Back button is dragged in above
    public float resetBottom = 0.30f;
    public float resetTop = 0.365f;
    public float resetLeft = 0.38f;
    public float resetRight = 0.62f;

    // defaults are the Back button's own colours: 778899 and E6E6E6
    [Header("Tint, to match the Back button")]
    public Color normalTint = new Color32(0x77, 0x88, 0x99, 0xFF);
    public Color highlightTint = new Color32(0xE6, 0xE6, 0xE6, 0xFF);
    public Color pressedTint = new Color32(0x77, 0x88, 0x99, 0xFF);

    [Header("Text")]
    public float nameFontSize = 24f;
    public float keyFontSize = 22f;
    // sampled off the lettering baked into the plates, so the key names match
    public Color textColor = new Color32(0xC4, 0xD0, 0xD7, 0xFF);
    public Color messageColor = new Color(1f, 0.82f, 0.4f);
    public Color plainBoxColor = new Color(0.12f, 0.16f, 0.24f, 1f);

    private bool built;

    // the panel starts switched off, so Start never runs until the player opens it
    void OnEnable()
    {
        if (built)
        {
            return;
        }

        built = true;
        Build();
    }

    private void Build()
    {
        if (showTitle)
        {
            GameObject titleGo = Place("Title", titleLeft, titleRight, titleBottom, titleTop);

            if (titleSprite != null)
            {
                // no Button on the title, so it gets the tint directly
                AddImage(titleGo, titleSprite).color = normalTint;
            }
            else
            {
                AddText(titleGo, nameFontSize * 1.5f, textColor, TextAlignmentOptions.Center).text = "KEYBINDS";
            }
        }

        // Fit every row between the top and the Reset button. With more actions than the
        // layout was made for, the rows get a little shorter instead of overlapping Reset.
        int count = Keybinds.Actions.Length;
        float lowest = ResetTopFraction() + 0.06f;           // leave room for the message line
        float needed = count * rowHeight + (count - 1) * rowGap;
        float available = firstRowTop - lowest;
        float fit = (available > 0f && needed > available) ? available / needed : 1f;
        float rh = rowHeight * fit, rg = rowGap * fit;

        float top = firstRowTop;

        for (int i = 0; i < count; i++)
        {
            BuildRow(Keybinds.Actions[i], top, i, rh);
            top -= rh + rg; // step down to where the next row goes
        }

        // the warning line sits just under the last row
        GameObject messageGo = Place("Message", 0.2f, 0.8f, top - 0.055f, top);
        TextMeshProUGUI message = AddText(messageGo, keyFontSize * 0.9f, messageColor, TextAlignmentOptions.Center);

        BuildResetButton();

        // every row shares the one message line, so warnings appear in a single place
        KeybindRow[] rows = GetComponentsInChildren<KeybindRow>(true);
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].messageLabel = message;
        }
    }

    // Where the top of the Reset button sits, as a share of the panel's height (0 = bottom, 1 = top).
    private float ResetTopFraction()
    {
        if (backButton == null) return resetTop;
        Canvas.ForceUpdateCanvases();
        RectTransform panel = (RectTransform)transform;
        Vector3[] corners = new Vector3[4];
        backButton.GetWorldCorners(corners);
        float backTop = panel.InverseTransformPoint(corners[1]).y;
        float resetTopLocal = backTop + backButton.rect.height + resetGap;
        return Mathf.Clamp01((resetTopLocal - panel.rect.yMin) / Mathf.Max(1f, panel.rect.height));
    }

    private void BuildRow(string action, float top, int index, float height)
    {
        float bottom = top - height;

        GameObject nameGo = Place("Name_" + action, nameLeft, nameRight, bottom, top);

        // use the picture of the word if one was dragged in, otherwise fall back
        // to plain text so the menu still works
        if (nameSprites != null && index < nameSprites.Length && nameSprites[index] != null)
        {
            // the name plates have no Button, so they take the tint directly and
            // just hold at it rather than lighting up on hover
            AddImage(nameGo, nameSprites[index]).color = normalTint;
        }
        else
        {
            // No picture for this action (e.g. Use Item): use the blank plate with the name
            // written on it in capitals, so it matches the other rows.
            AddBox(nameGo, keyPlateSprite).color = normalTint;
            GameObject labelGo = Place("Label", 0f, 1f, 0f, 1f, nameGo.transform);
            AddText(labelGo, nameFontSize, textColor, TextAlignmentOptions.Center).text = Spaced(action).ToUpperInvariant();
        }

        GameObject keyGo = Place("Key_" + action, keyLeft, keyRight, bottom, top);

        Button button = keyGo.AddComponent<Button>();
        button.targetGraphic = AddBox(keyGo, keyPlateSprite);
        Tint(button);

        // the key name can't be a picture because it changes whenever the player
        // rebinds it, so this one stays as text sitting on the blank plate
        GameObject keyTextGo = Place("KeyText", 0f, 1f, 0f, 1f, keyGo.transform);
        TextMeshProUGUI keyText = AddText(keyTextGo, keyFontSize, textColor, TextAlignmentOptions.Center);

        KeybindRow bind = keyGo.AddComponent<KeybindRow>();
        bind.action = action;
        bind.keyLabel = keyText;
    }

    private void BuildResetButton()
    {
        GameObject go;

        if (backButton != null)
        {
            go = new GameObject("ResetButton", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();

            // copy the Back button's anchors and size so the two line up exactly,
            // then shift this one up by its own height to sit on top of it
            RectTransform r = (RectTransform)go.transform;
            r.anchorMin = backButton.anchorMin;
            r.anchorMax = backButton.anchorMax;
            r.pivot = backButton.pivot;
            r.sizeDelta = backButton.sizeDelta;
            r.anchoredPosition = backButton.anchoredPosition
                                 + new Vector2(0f, backButton.rect.height + resetGap);
        }
        else
        {
            go = Place("ResetButton", resetLeft, resetRight, resetBottom, resetTop);
        }

        Button button = go.AddComponent<Button>();

        if (resetSprite != null)
        {
            button.targetGraphic = AddImage(go, resetSprite);
        }
        else
        {
            button.targetGraphic = AddBox(go, null);

            GameObject textGo = Place("Text", 0f, 1f, 0f, 1f, go.transform);
            AddText(textGo, keyFontSize, textColor, TextAlignmentOptions.Center).text = "Reset to defaults";
        }

        Tint(button);
        go.AddComponent<KeybindResetButton>();
    }

    // darkens the button at rest and brightens it on hover, the way the Back
    // button does. Unity multiplies these colours over the sprite
    private void Tint(Button button)
    {
        ColorBlock colours = button.colors;
        colours.normalColor = normalTint;
        colours.highlightedColor = highlightTint;
        colours.pressedColor = pressedTint;
        colours.selectedColor = normalTint;
        button.colors = colours;
    }

    // makes a UI object and stretches it between the given fractions of its parent.
    // left and right go across, bottom and top go up
    private GameObject Place(string name, float left, float right, float bottom, float top, Transform parent = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent == null ? transform : parent, false);

        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = new Vector2(left, bottom);
        r.anchorMax = new Vector2(right, top);
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;

        // sits behind the Back button, which was already on the panel, so it
        // can never end up covering it and blocking the click
        go.transform.SetAsFirstSibling();
        return go;
    }

    private Image AddImage(GameObject go, Sprite sprite)
    {
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true; // keeps the pixels square instead of squashing them

        // left white on purpose. anything with a Button on it gets its colour from
        // the Tint method below, and Unity multiplies that over this one, so setting
        // it here as well would apply the tint twice and come out too dark
        return img;
    }

    // the plate behind a key. uses the image if one is given, otherwise just a
    // plain coloured rectangle so nothing breaks when the slot is empty
    private Image AddBox(GameObject go, Sprite sprite)
    {
        if (sprite != null)
        {
            return AddImage(go, sprite);
        }

        Image box = go.AddComponent<Image>();
        box.color = plainBoxColor;
        return box;
    }

    private TextMeshProUGUI AddText(GameObject go, float size, Color colour, TextAlignmentOptions align)
    {
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();

        if (font != null)
        {
            t.font = font;
        }
        else
        {
            UIFont.Apply(t); // the game's pixel font (GNF), so the key names match the lettering
        }

        // shrinks the text if its box gets small, so the size above is a
        // maximum rather than a fixed value
        t.enableAutoSizing = true;
        t.fontSizeMax = size;
        t.fontSizeMin = 8f;

        t.color = colour;
        t.alignment = align;
        t.raycastTarget = false; // so it doesn't swallow clicks meant for the button
        t.text = "";
        return t;
    }

    // "MoveLeft" reads better as "Move Left"
    private static string Spaced(string action)
    {
        string result = "";

        for (int i = 0; i < action.Length; i++)
        {
            if (i > 0 && char.IsUpper(action[i]))
            {
                result += " ";
            }

            result += action[i];
        }

        return result;
    }
}