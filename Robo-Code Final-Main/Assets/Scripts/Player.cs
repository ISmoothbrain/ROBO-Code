using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Player : MonoBehaviour
{
    [Header("Stats")]
    public int health = 100;
    public int maxHealth = 100;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("UI")]
    public Image healthImage;

    [Header("Attack")]
    // Editable in the Inspector now, so if your states are named something else
    // (or live in a sub-state machine, e.g. "Combat.Attack1") you can fix it there
    // instead of editing this file.
    public string[] attackStateNames = { "Attack1", "Attack2", "Attack3" };
    public float comboResetTime = 0.8f;
    public bool lockMovementDuringAttack = true;
    public float maxAttackDuration = 1.5f;
    public float minAttackDuration = 0.08f;
    public Transform attackPoint;
    public float attackRadius = 0.6f;
    public int attackDamage = 20;
    public LayerMask enemyLayer; // leave empty to hit anything with a BanditEnemy on it

    [Header("Locomotion States")]
    public string idleState = "Hero_Idle";
    public string runState = "Hero_Run";
    public string jumpState = "Hero_Jump";
    public string fallState = "Hero_Fall";

    [Header("Death")]
    public float deathReloadDelay = 1.2f;

    // A resolved attack: which layer it lives on and the hash that actually works.
    private struct ResolvedState
    {
        public int layer;
        public int hash;
        public string name;
        public bool found;
    }

    private ResolvedState[] resolvedAttacks;
    private bool isAttacking;
    private int comboStep;
    private float comboTimer;
    private float attackStartTime;
    private bool isDead;

    private Rigidbody2D rb;
    private bool isGrounded;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private float moveInput;
    private float animationLockUntil; // set by PlayerSkills so a skill's animation isn't overwritten
        private float footstepTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (maxHealth <= 0) maxHealth = 100;

        // Max HP bought in the shop carries over when the level reloads.
        maxHealth += PlayerUpgrades.BonusMaxHealth;
        health = Mathf.Clamp(health + PlayerUpgrades.BonusMaxHealth, 0, maxHealth);

        ResolveAttackStates();
        ApplyHealthBarColor(HealthBarSettings.CurrentColor);
    }

    // ------------------------------------------------------------------ state resolution

    // animator.Play() fails silently when the state name doesn't exist, which is the usual
    // reason an attack "just doesn't animate". Rather than trusting one spelling, try the
    // obvious variants across every layer and remember whatever actually resolves.
    private void ResolveAttackStates()
    {
        resolvedAttacks = new ResolvedState[attackStateNames.Length];

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogError($"{name}: no Animator Controller assigned — nothing will animate.");
            return;
        }

        for (int i = 0; i < attackStateNames.Length; i++)
        {
            resolvedAttacks[i] = Resolve(attackStateNames[i]);

            if (!resolvedAttacks[i].found)
            {
                Debug.LogError(
                    $"{name}: no state matching \"{attackStateNames[i]}\" exists in " +
                    $"'{animator.runtimeAnimatorController.name}' on any layer.\n" +
                    $"An animation CLIP named Attack1 is not the same thing as a STATE named Attack1 — " +
                    $"the clip has to be dragged into the Animator window to become a state.\n" +
                    $"Run Tools > Animator > Dump States Of Selected to see what this controller " +
                    $"actually contains, then either rename the state or set it in the " +
                    $"Attack State Names list on this component.");
            }
            else if (resolvedAttacks[i].name != attackStateNames[i])
            {
                Debug.LogWarning(
                    $"{name}: \"{attackStateNames[i]}\" wasn't found, but \"{resolvedAttacks[i].name}\" " +
                    $"was (layer {resolvedAttacks[i].layer}). Using it. Put that exact name in the " +
                    $"Attack State Names list to silence this.");
            }
        }
    }

    private ResolvedState Resolve(string wanted)
    {
        foreach (string candidate in NameCandidates(wanted))
        {
            int hash = Animator.StringToHash(candidate);
            for (int layer = 0; layer < animator.layerCount; layer++)
            {
                if (animator.HasState(layer, hash))
                {
                    return new ResolvedState { layer = layer, hash = hash, name = candidate, found = true };
                }
            }
        }

        return new ResolvedState { found = false };
    }

    private IEnumerable<string> NameCandidates(string wanted)
    {
        yield return wanted;

        // "Attack1" -> "Attack" + "1", so we can rebuild the common spellings.
        string stem = wanted;
        string digits = "";
        while (stem.Length > 0 && char.IsDigit(stem[stem.Length - 1]))
        {
            digits = stem[stem.Length - 1] + digits;
            stem = stem.Substring(0, stem.Length - 1);
        }

        if (digits.Length > 0)
        {
            yield return stem + " " + digits;   // "Attack 1"
            yield return stem + "_" + digits;   // "Attack_1"
            yield return stem + "-" + digits;   // "Attack-1"
        }

        // Common prefixes people end up with on 2D character controllers.
        string[] prefixes = { "Hero_", "Hero ", "Player_", "Player ", "Char_" };
        foreach (string p in prefixes) yield return p + wanted;

        // Common sub-state machine paths (Play() needs the dotted path for those).
        string[] machines = { "Attack", "Attacks", "Combat", "Combo", "Attack Layer" };
        foreach (string m in machines) yield return m + "." + wanted;

        yield return wanted.ToLowerInvariant();
        yield return wanted.ToUpperInvariant();
    }

    // ------------------------------------------------------------------ loop

    void OnEnable() { HealthBarSettings.OnHealthBarColorChanged += ApplyHealthBarColor; }
    void OnDisable() { HealthBarSettings.OnHealthBarColorChanged -= ApplyHealthBarColor; }

    private void ApplyHealthBarColor(Color color)
    {
        if (healthImage != null) healthImage.color = color;
    }

    void Update()
    {
        if (isDead) return;

        // Freeze platformer input while a battle, conversation, shop, backpack or coding challenge is open.
        if (UIBusy.AnyOpen) { moveInput = 0f; return; }

        moveInput = Input.GetAxis("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        bool clickedOnUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetMouseButtonDown(0) && !clickedOnUI)
        {
            TryAttack();
        }

        if (isAttacking && Time.time - attackStartTime > maxAttackDuration)
        {
            Debug.LogWarning(
                $"{name}: attack timed out after {maxAttackDuration}s — the clip has no " +
                $"OnAttackAnimationEnd event on its last frame. " +
                $"Tools > Animator > Add Attack Animation Events will add it.");
            ForceEndAttack();
        }

        if (!isAttacking && comboStep > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f) comboStep = 0;
        }

        if (!isAttacking && Time.time >= animationLockUntil) SetAnimation(moveInput);
                if (isGrounded && Mathf.Abs(moveInput) > 0.1f && !isAttacking)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                footstepTimer = 0.32f;
                if (AudioManager.instance != null) AudioManager.instance.PlayFootstep();
            }
        }
        else
        {
            footstepTimer = 0f;
        }

        UpdateHealthUI();
    }

    // Called by PlayerSkills: stops idle/run from replacing a skill animation for a moment.
    public void LockAnimation(float seconds)
    {
        animationLockUntil = Mathf.Max(animationLockUntil, Time.time + seconds);
    }

    private void UpdateHealthUI()
    {
        if (healthImage == null) return;
        healthImage.fillAmount = (float)health / maxHealth;
    }

    private void FixedUpdate()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        if (isDead)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float horizontalInput = (isAttacking && lockMovementDuringAttack) ? 0f : moveInput;
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    // ------------------------------------------------------------------ attacking

    private void TryAttack()
    {
        if (isAttacking) return;
        if (resolvedAttacks == null || resolvedAttacks.Length == 0) return;

        int nextStep = (comboStep >= resolvedAttacks.Length) ? 1 : comboStep + 1;

        // Skip over any combo steps whose state is missing, so one broken state
        // doesn't leave the player unable to attack at all.
        int attempts = 0;
        while (!resolvedAttacks[nextStep - 1].found && attempts < resolvedAttacks.Length)
        {
            nextStep = (nextStep >= resolvedAttacks.Length) ? 1 : nextStep + 1;
            attempts++;
        }

        ResolvedState state = resolvedAttacks[nextStep - 1];
        if (!state.found)
        {
            // Nothing is playable. Deal damage anyway so combat still functions
            // while the animator gets sorted out.
            comboStep = 0;
            DealDamage();
            return;
        }

        comboStep = nextStep;
        isAttacking = true;
        attackStartTime = Time.time;
        animator.Play(state.hash, state.layer, 0f);
                if (AudioManager.instance != null) AudioManager.instance.PlaySFX(AudioManager.instance.attack);
    }

    // Animation Event on the LAST frame of each attack clip.
    public void OnAttackAnimationEnd()
    {
        if (!isAttacking) return;

        // If this fires on frame 0 the clip gets cancelled before a single frame draws,
        // which looks exactly like "the animation never plays". Ignore it.
        if (Time.time - attackStartTime < minAttackDuration)
        {
            Debug.LogWarning(
                $"{name}: OnAttackAnimationEnd fired immediately. Move that Animation Event " +
                $"to the last frame of the clip.");
            return;
        }

        ForceEndAttack();
    }

    private void ForceEndAttack()
    {
        isAttacking = false;
        comboTimer = comboResetTime;
    }

    // Animation Event on the HIT frame of each attack clip.
    public void DealDamage()
    {
        if (attackPoint == null)
        {
            Debug.LogWarning($"{name}: attackPoint is not assigned, can't deal attack damage.");
            return;
        }

        // An unset Enemy Layer serializes as 0 ("Nothing"), and OverlapCircleAll with an
        // empty mask silently returns nothing — so attacks appear to do no damage at all.
        Collider2D[] hits = enemyLayer.value == 0
            ? Physics2D.OverlapCircleAll(attackPoint.position, attackRadius)
            : Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);

        // Battles deal damage through BattleManager, so ignore this animation event mid-battle.
        if (BattleManager.InBattle) return;

        foreach (Collider2D hit in hits)
        {
            BanditEnemy enemy = hit.GetComponentInParent<BanditEnemy>();
            if (enemy == null) continue;

            // Hitting a bandit first starts a turn-based battle with the player going first.
            if (BattleManager.Instance != null)
            {
                if (BattleManager.Instance.TryStartBattle(this, enemy, true)) return;
            }
            else if (!warnedNoBattleManager)
            {
                warnedNoBattleManager = true;
                Debug.LogWarning("[Battle] There's no BattleManager in this scene, so hits do normal damage. " +
                                 "Run Tools > Battle System > Set Up Battle In Open Scene, then SAVE the scene.");
            }

            enemy.TakeDamage(attackDamage + PlayerUpgrades.BonusDamage);
        }
    }

    // ------------------------------------------------------------------ battle hooks

    public bool IsDead => isDead;

    // Potions and shop items heal through this. Never goes above max HP.
    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        health = Mathf.Min(maxHealth, health + amount);
                if (AudioManager.instance != null) AudioManager.instance.PlaySFX(AudioManager.instance.heal);
    }
    private static bool warnedNoBattleManager;

    public void EnterBattle(Vector3 enemyPosition)
    {
        moveInput = 0f;
        isAttacking = false;
        comboStep = 0;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        transform.localScale = new Vector3(enemyPosition.x >= transform.position.x ? 1 : -1, 1, 1);
        ReturnToBattleIdle();
    }

    public void ExitBattle()
    {
        isAttacking = false;
        comboTimer = 0f;
        comboStep = 0;
    }

    public void PlayBattleAttack()
    {
        if (resolvedAttacks == null || animator == null) return;
        List<ResolvedState> playable = new List<ResolvedState>();
        foreach (ResolvedState r in resolvedAttacks) if (r.found) playable.Add(r);
        if (playable.Count == 0) return;

        ResolvedState pick = playable[Random.Range(0, playable.Count)];
        animator.Play(pick.hash, pick.layer, 0f);
    }

    public void ReturnToBattleIdle()
    {
        if (animator != null && !isDead) animator.Play(idleState);
    }

    // ------------------------------------------------------------------ locomotion

    private void SetAnimation(float moveInput)
    {
        if (moveInput > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0) transform.localScale = new Vector3(-1, 1, 1);

        string targetState;

        if (isGrounded) targetState = moveInput == 0 ? idleState : runState;
        else targetState = rb.linearVelocity.y > 0 ? jumpState : fallState;

        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(targetState))
        {
            animator.Play(targetState);
        }
    }

    // ------------------------------------------------------------------ damage & death

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Damage")) TakeDamage(10);
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        health -= amount;
                if (AudioManager.instance != null) AudioManager.instance.PlaySFX(AudioManager.instance.hurt);
        if (spriteRenderer != null) StartCoroutine(DamageFlash());

        if (health <= 0)
        {
            health = 0;
            Die();
        }
    }

    private IEnumerator DamageFlash()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        isAttacking = false;

        animator.SetTrigger("Death");
                if (AudioManager.instance != null) AudioManager.instance.PlaySFX(AudioManager.instance.defeat);
        StartCoroutine(ReloadAfterDeath());
    }

    private IEnumerator ReloadAfterDeath()
    {
        yield return new WaitForSeconds(deathReloadDelay);
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
