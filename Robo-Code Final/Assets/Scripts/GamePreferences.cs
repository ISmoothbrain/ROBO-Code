using UnityEngine;

// Player-facing settings that are not tied to a scene object. Keeping them in
// PlayerPrefs means the main-menu controls work in a build without extra wiring.
public static class GamePreferences
{
    private const string DebugSecondsKey = "DebugAnswerSeconds";
    private const string BackgroundKey = "BackgroundThemeChoice";

    public const float MinDebugSeconds = 10f;
    public const float MaxDebugSeconds = 60f;

    public static float DebugAnswerSeconds
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(DebugSecondsKey, 30f), MinDebugSeconds, MaxDebugSeconds);
        set
        {
            PlayerPrefs.SetFloat(DebugSecondsKey, Mathf.Clamp(value, MinDebugSeconds, MaxDebugSeconds));
            PlayerPrefs.Save();
        }
    }

    // -1 means use BackgroundSettings.asset (or the scene's explicit override).
    public static int BackgroundChoice
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(BackgroundKey, -1), -1, (int)BackgroundTheme.Cyberpunk);
        set
        {
            PlayerPrefs.SetInt(BackgroundKey, Mathf.Clamp(value, -1, (int)BackgroundTheme.Cyberpunk));
            PlayerPrefs.Save();
        }
    }

    public static string BackgroundChoiceName
    {
        get
        {
            int value = BackgroundChoice;
            return value < 0 ? "SCENE DEFAULT" : ((BackgroundTheme)value).ToString().ToUpperInvariant();
        }
    }
}

// One source of truth for the automatic difficulty curve.
public static class DifficultyScaling
{
    public static int Stage => Mathf.Max(0, LevelProgress.CurrentOrdinal());
    public static float EnemyHealthMultiplier => 1f + Stage * 0.35f;
    public static float EnemyDamageMultiplier => 1f + Stage * 0.20f;
    public static float QteSpeedMultiplier => Mathf.Max(0.7f, 1f - Stage * 0.10f);

    // The player's setting is the baseline; later levels trim it slightly while
    // still respecting a useful minimum thinking time.
    public static float AnswerSeconds => Mathf.Max(8f, GamePreferences.DebugAnswerSeconds - Stage * 3f);

    public static bool AllowsQuestion(int difficulty, bool boss)
    {
        if (boss) return difficulty >= Mathf.Min(2, Stage + 1);
        if (Stage == 0) return difficulty <= 1;
        if (Stage == 1) return difficulty >= 1 && difficulty <= 2;
        return difficulty >= 2;
    }
}
