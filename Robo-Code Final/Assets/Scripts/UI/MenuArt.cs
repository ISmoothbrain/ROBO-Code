using UnityEngine;

// The button and label images used by the menus' new parts (MenuExtras).
// Lives at Assets/Resources/MenuArt. Drag different images in here to restyle them.
[CreateAssetMenu(fileName = "MenuArt", menuName = "UI/Menu Art")]
public class MenuArt : ScriptableObject
{
    [Header("Main menu")]
    public Sprite continueButton;
    public Sprite newGameButton;

    [Header("Settings")]
    public Sprite answerTimeLabel;
    public Sprite backgroundLabel;
    [Tooltip("Blank plate behind the background's name (click it to change background).")]
    public Sprite valuePlate;

    public static MenuArt Load() => Resources.Load<MenuArt>("MenuArt");
}
