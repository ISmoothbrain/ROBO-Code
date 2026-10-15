using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Lets the player use skills crafted by solving code challenges.
// Add to the same GameObject as Player.
[RequireComponent(typeof(Player))]
public class PlayerSkills : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode castKey = KeyCode.Q;
    public KeyCode switchKey = KeyCode.Tab;

    [Header("Mana")]
    public int maxMana = 100;
    public float manaRegenPerSecond = 8f;
    public Image manaImage; // your existing ManaBar fill Image (Image Type: Filled)

    [Header("UI (optional)")]
    public TextMeshProUGUI equippedSkillText;

    private Player player;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private float mana;
    private int equippedIndex;
    private float nextCastTime;

    public float Mana => mana;

    private void Start()
    {
        player = GetComponent<Player>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mana = maxMana;
        RefreshUI();
    }

    private void Update()
    {
        if (CodeChallengeUI.IsOpen) return;

        mana = Mathf.Min(maxMana, mana + manaRegenPerSecond * Time.deltaTime);

        IReadOnlyList<SkillData> skills = ProgressManager.Instance != null
            ? ProgressManager.Instance.UnlockedSkills
            : null;
        int count = skills != null ? skills.Count : 0;
        if (count > 0) equippedIndex = Mathf.Clamp(equippedIndex, 0, count - 1);

        if (count > 1 && Input.GetKeyDown(switchKey))
        {
            equippedIndex = (equippedIndex + 1) % count;
        }

        if (count > 0 && Input.GetKeyDown(castKey))
        {
            TryCast(skills[equippedIndex]);
        }

        RefreshUI(count > 0 ? skills[equippedIndex] : null);
    }

    private void TryCast(SkillData skill)
    {
        if (skill == null) return;
        if (Time.time < nextCastTime) return;
        if (mana < skill.manaCost) return;
        if (player.attackPoint == null)
        {
            Debug.LogWarning("PlayerSkills: the Player's Attack Point isn't assigned.");
            return;
        }

        mana -= skill.manaCost;
        nextCastTime = Time.time + skill.cooldown;

        float lockTime = skill.hitCount * skill.timeBetweenHits + 0.3f;
        player.LockAnimation(lockTime);

        int hash = Animator.StringToHash(skill.animationState);
        if (animator != null && animator.HasState(0, hash)) animator.Play(hash, 0, 0f);

        StartCoroutine(CastRoutine(skill));
    }

    private IEnumerator CastRoutine(SkillData skill)
    {
        if (spriteRenderer != null) spriteRenderer.color = skill.flashColor;

        float radius = player.attackRadius * skill.radiusMultiplier;
        for (int i = 0; i < skill.hitCount; i++)
        {
            Collider2D[] hits = player.enemyLayer.value == 0
                ? Physics2D.OverlapCircleAll(player.attackPoint.position, radius)
                : Physics2D.OverlapCircleAll(player.attackPoint.position, radius, player.enemyLayer);

            // An enemy with several colliders should only be hit once per strike.
            HashSet<BanditEnemy> struck = new HashSet<BanditEnemy>();
            foreach (Collider2D h in hits)
            {
                BanditEnemy enemy = h.GetComponentInParent<BanditEnemy>();
                if (enemy != null && struck.Add(enemy)) enemy.TakeDamage(skill.damagePerHit);
            }

            if (i < skill.hitCount - 1) yield return new WaitForSeconds(skill.timeBetweenHits);
        }

        yield return new WaitForSeconds(0.1f);
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
    }

    private void RefreshUI(SkillData equipped = null)
    {
        if (manaImage != null) manaImage.fillAmount = mana / maxMana;

        if (equippedSkillText != null)
        {
            if (equipped == null)
            {
                equippedSkillText.text = "No skills yet: solve a code challenge!";
            }
            else
            {
                float cd = Mathf.Max(0f, nextCastTime - Time.time);
                equippedSkillText.text = $"[Q] {equipped.skillName}  ({equipped.manaCost} mana)" +
                                         (cd > 0f ? $"  {cd:F1}s" : "");
            }
        }
    }
}
