using UnityEngine;

// All the battle numbers in one place. Lives at Assets/Resources/BattleSettings.asset.
// Edit it any time, even during Play mode: changes apply from the next fight and are KEPT
// when you stop playing (unlike changes to the auto-built [Battle System] object).
[CreateAssetMenu(fileName = "BattleSettings", menuName = "Battle/Battle Settings")]
public class BattleSettings : ScriptableObject
{
    [Header("Battle Camera")]
    [Tooltip("Battle zoom compared to the normal camera. Smaller = closer. 1 = no zoom.")]
    [Range(0.3f, 1f)] public float zoomFactor = 0.67f;
    [Tooltip("Where the camera aims, relative to the point between the fighters.\n" +
             "Raise Y (e.g. -0.5 to 0) to move the camera UP, which puts the fighters LOWER on screen.")]
    public Vector3 focusOffset = new Vector3(0f, -0.5f, 0f);
    [Tooltip("Seconds the zoom in/out takes.")]
    public float zoomTime = 0.6f;

    [Header("Staging")]
    [Tooltip("How far apart the fighters stand during the battle.")]
    public float battleSpacing = 3f;
    public float hopTime = 0.35f;
    public float hopHeight = 0.6f;

    [Header("Damage & Time")]
    public int correctAnswerDamage = 15;
    public int wrongAnswerDamage = 10;
    [Tooltip("Old setting, no longer used: each enemy now hits for its own Attack Damage (set on the enemy prefab).")]
    public int enemyAttackDamage = 10;
    [Tooltip("Multiplies every enemy's Attack Damage in battles. 1 = as set on each enemy, 1.5 = 50% harder, 0.5 = half.")]
    [Range(0f, 3f)] public float enemyDamageMultiplier = 1f;
    public float answerTimeLimit = 20f;

    [Header("Quick-Time Event")]
    [Tooltip("Seconds for the ring to close. Smaller = harder.")]
    public float qteCloseTime = 1.5f;
    [Tooltip("How forgiving the timing is. Smaller = harder.")]
    [Range(0.05f, 0.4f)] public float qteHitWindow = 0.25f;

    public static BattleSettings Load() => Resources.Load<BattleSettings>("BattleSettings");
}
