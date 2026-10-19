using UnityEngine;

// Save data for the Magitek fragment and the crafted Hack ability.
public static class HackProgress
{
    private const string FragmentKey = "HackFragmentOwned";
    private const string CraftedKey = "HackAbilityCrafted";

    public static bool HasFragment => PlayerPrefs.GetInt(FragmentKey, 0) == 1;
    public static bool IsCrafted => PlayerPrefs.GetInt(CraftedKey, 0) == 1;

    public static bool AwardFragment()
    {
        if (HasFragment) return false;
        PlayerPrefs.SetInt(FragmentKey, 1);
        PlayerPrefs.Save();
        return true;
    }

    public static void Craft()
    {
        if (!HasFragment) return;
        PlayerPrefs.SetInt(CraftedKey, 1);
        PlayerPrefs.Save();
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(FragmentKey);
        PlayerPrefs.DeleteKey(CraftedKey);
        PlayerPrefs.Save();
    }
}
