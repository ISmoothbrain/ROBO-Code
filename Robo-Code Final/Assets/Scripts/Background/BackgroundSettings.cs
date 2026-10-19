using UnityEngine;

public enum BackgroundTheme { None, Dusk, Night, Industrial, Cyberpunk }

// Lives at Assets/Resources/BackgroundSettings.asset. This is the DEFAULT for every scene:
// to give one scene its own background, add a Scene Background component to any object in it.
// Edits made during Play mode show up straight away and are kept when you stop.
[CreateAssetMenu(fileName = "BackgroundSettings", menuName = "Background/Background Settings")]
public class BackgroundSettings : ScriptableObject
{
    [Tooltip("Which city skyline to show. None turns the background off.")]
    public BackgroundTheme theme = BackgroundTheme.Night;

    [Tooltip("Moves the whole skyline up (positive) or down (negative) on screen, in world units.")]
    public float verticalOffset = 0f;
    [Tooltip("Makes the art bigger or smaller. 1 = same pixel size as your level tiles.")]
    public float scale = 1f;

    [Header("Parallax: 1 = stays put on screen (very far away), 0 = moves with the level")]
    [Range(0f, 1f)] public float sky = 1f;
    [Range(0f, 1f)] public float clouds = 0.95f;
    [Range(0f, 1f)] public float farBuildings = 0.9f;
    [Range(0f, 1f)] public float fog = 0.85f;
    [Range(0f, 1f)] public float midBuildings = 0.75f;
    [Range(0f, 1f)] public float nearBuildings = 0.6f;

    [Header("Drift (world units per second, on their own)")]
    public float cloudDrift = 0.25f;
    public float fogDrift = 0.08f;

    public static BackgroundSettings Load() => Resources.Load<BackgroundSettings>("BackgroundSettings");
}
