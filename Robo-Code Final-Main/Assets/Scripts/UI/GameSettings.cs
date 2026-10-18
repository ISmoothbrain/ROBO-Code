// Player options. These forward to GamePreferences (your teammate's settings, changed from the
// main menu's DEBUG TIME and BACKGROUND rows), so the menu and the game always agree.
public static class GameSettings
{
    // Seconds to answer a battle question.
    public static float AnswerTime
    {
        get => GamePreferences.DebugAnswerSeconds;
        set => GamePreferences.DebugAnswerSeconds = value;
    }

    // Background picked in Settings. -1 = each level's own background.
    public static int BackgroundChoice
    {
        get => GamePreferences.BackgroundChoice;
        set => GamePreferences.BackgroundChoice = value;
    }
}
