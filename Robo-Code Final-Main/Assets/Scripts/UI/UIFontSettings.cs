using TMPro;
using UnityEngine;

// Lives at Assets/Resources/UIFontSettings.asset and picks the font for every dialogue, menu,
// battle screen and "[E]" prompt. Drag any TextMesh Pro font asset into Font to change it.
[CreateAssetMenu(fileName = "UIFontSettings", menuName = "UI/UI Font Settings")]
public class UIFontSettings : ScriptableObject
{
    public TMP_FontAsset font;
}
