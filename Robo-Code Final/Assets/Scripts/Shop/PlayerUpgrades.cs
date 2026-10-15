// Permanent bonuses bought in the shop. Static, so they survive the scene
// reloading when the player dies (they reset when you stop Play mode).
public static class PlayerUpgrades
{
    public static int BonusDamage;     // added to platformer hits AND correct battle answers
    public static int BonusMaxHealth;  // added to the Player's max HP when the level loads

    public static void Reset()
    {
        BonusDamage = 0;
        BonusMaxHealth = 0;
    }
}
