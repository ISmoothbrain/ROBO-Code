using UnityEngine;

// A crafted attack unlocked by solving a CodeChallenge.
// Create via: Create > Coding > Skill
[CreateAssetMenu(fileName = "New Skill", menuName = "Coding/Skill")]
public class SkillData : ScriptableObject
{
    public string skillName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Damage")]
    public int damagePerHit = 15;
    public int hitCount = 1;
    public float timeBetweenHits = 0.15f;
    [Tooltip("Multiplies the Player's Attack Radius for this skill.")]
    public float radiusMultiplier = 1f;

    [Header("Cost")]
    public int manaCost = 20;
    public float cooldown = 2f;

    [Header("Visuals")]
    [Tooltip("Animator state to play. Must exist on the Player's Animator Controller.")]
    public string animationState = "Attack3";
    public Color flashColor = new Color(0.4f, 0.8f, 1f);
}
