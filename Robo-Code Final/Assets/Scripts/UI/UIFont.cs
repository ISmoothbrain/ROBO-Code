using TMPro;
using UnityEngine;

// One place to choose the font for every UI the game builds itself
// (battles, dialogue, shop, "Press E" prompts).
// To use your pixel font: make a TextMesh Pro Font Asset from it, name it "UIFont",
// and put it in any folder called "Resources" (e.g. Assets/Resources/UIFont.asset).
public static class UIFont
{
    private static TMP_FontAsset cached;
    private static bool looked;

    public static TMP_FontAsset Get()
    {
        if (!looked)
        {
            looked = true;
            cached = Resources.Load<TMP_FontAsset>("UIFont");
        }
        return cached;
    }

    public static void Apply(TMP_Text text)
    {
        TMP_FontAsset font = Get();
        if (text != null && font != null) text.font = font;
    }
}
