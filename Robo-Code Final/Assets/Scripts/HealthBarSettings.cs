using UnityEngine;

// Shared, game-wide health bar colour. The player's health image and every
// enemy's floating bar read HealthBarSettings.CurrentColor on Start() and
// subscribe to OnHealthBarColorChanged, so dragging the sliders in Settings
// recolours every health bar on screen immediately - no direct references
// between Player/BanditEnemy and the settings UI needed.
public static class HealthBarSettings
{
    private const string PrefR = "HealthBarColor_R";
    private const string PrefG = "HealthBarColor_G";
    private const string PrefB = "HealthBarColor_B";

    private static Color? cachedColor;

    public static Color CurrentColor
    {
        get
        {
            if (cachedColor == null)
            {
                // Default: a standard health-bar red.
                cachedColor = new Color(
                    PlayerPrefs.GetFloat(PrefR, 0.85f),
                    PlayerPrefs.GetFloat(PrefG, 0.15f),
                    PlayerPrefs.GetFloat(PrefB, 0.15f));
            }

            return cachedColor.Value;
        }
    }

    public static event System.Action<Color> OnHealthBarColorChanged;

    public static void SetColor(Color color)
    {
        cachedColor = color;
        PlayerPrefs.SetFloat(PrefR, color.r);
        PlayerPrefs.SetFloat(PrefG, color.g);
        PlayerPrefs.SetFloat(PrefB, color.b);
        OnHealthBarColorChanged?.Invoke(color);
    }
}
