// Permanent bonuses bought in the shop. Static, so they survive the scene
// reloading when the player dies (they reset when you stop Play mode).
public static class PlayerUpgrades
{
    private static int bonusDamage;
    private static int bonusMaxHealth;
    private static int loadedRun = -1;

    public static int BonusDamage
    {
        get { EnsureLoaded(); return bonusDamage; }
        set { EnsureLoaded(); bonusDamage = value; Save(); }
    }

    public static int BonusMaxHealth
    {
        get { EnsureLoaded(); return bonusMaxHealth; }
        set { EnsureLoaded(); bonusMaxHealth = value; Save(); }
    }

    private static void EnsureLoaded()
    {
        if (loadedRun == LevelProgress.RunId) return;
        loadedRun = LevelProgress.RunId;
        bonusDamage = UnityEngine.PlayerPrefs.GetInt("UpgradeDamage_Run" + loadedRun, 0);
        bonusMaxHealth = UnityEngine.PlayerPrefs.GetInt("UpgradeHealth_Run" + loadedRun, 0);
    }

    private static void Save()
    {
        UnityEngine.PlayerPrefs.SetInt("UpgradeDamage_Run" + loadedRun, bonusDamage);
        UnityEngine.PlayerPrefs.SetInt("UpgradeHealth_Run" + loadedRun, bonusMaxHealth);
        UnityEngine.PlayerPrefs.Save();
    }

    public static void Reset()
    {
        loadedRun = LevelProgress.RunId;
        bonusDamage = 0;
        bonusMaxHealth = 0;
        Save();
    }
}
