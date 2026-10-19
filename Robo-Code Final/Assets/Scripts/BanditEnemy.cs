using UnityEngine;
using System.Collections;

public class BanditEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public Transform pointA;
    public Transform pointB;

    [Header("Detection")]
    public float chaseRange = 5f;   // how close player must be to start chasing
    public float attackRange = 0.5f;  // how close player must be to attack
    public float maxAttackHeightDiff = 1f; // how much vertical difference is still "reachable" to attack
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    private BoxCollider2D boxCollider;

    [Header("Combat")]
    public int health = 30;
    public float attackCooldown = 1.5f;
    [Range(0, 100)] public int attackDamage = 10;

    [Header("Patrol")]
    public float waitTimeAtPoint = 1.5f;
    public float fallbackPatrolDistance = 3f; // used only if Point A / Point B are left empty

    [Header("Health Bar")]
    public GameObject healthBarPrefab; // prefab with an EnemyHealthBar component on its root
    public Vector3 healthBarOffset = new Vector3(0f, 1.2f, 0f);
    private EnemyHealthBar healthBar;
    private int maxHealth;

    [Header("Damage Feedback")]
    public Color damageFlashColor = Color.red;
    public float damageFlashDuration = 0.1f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor = Color.white;
    private Coroutine flashRoutine;

    [Header("Battle")]
    [Tooltip("Python questions this enemy asks. Leave empty to use the BattleManager's defaults.")]
    public PythonQuestion[] battleQuestions;

    [Header("Boss")]
    [Tooltip("Bosses ask harder questions, and after the ring quick-time event the player must also type an arrow-key combo.")]
    public bool isBoss = false;
    [Tooltip("How many arrows are in each boss combo.")]
    [Range(3, 10)] public int arrowComboLength = 5;
    [Tooltip("Seconds the player gets to type the whole combo.")]
    [Range(1f, 10f)] public float arrowComboTime = 4f;

    // Other names for the same three settings, used by StageBossInstaller.cs.
    // They read and write the fields above, so both systems always agree.
    public bool isStageBoss { get => isBoss; set => isBoss = value; }
    public int bossArrowCount { get => arrowComboLength; set => arrowComboLength = value; }
    public float bossArrowSeconds { get => arrowComboTime; set => arrowComboTime = value; }

    [Tooltip("While ticked, this enemy stands still and never attacks (e.g. a boss you've fooled in conversation). Hitting it still starts a battle.")]
    public bool passive = false;
    private bool inBattle;

    [Header("Loot")]
    public GameObject coinPrefab;          // drag your Coin prefab here
    public int minCoins = 2;
    public int maxCoins = 5;
    public Vector3 coinSpawnOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Death")]
    // Safety net only. Add OnDeathAnimationComplete() as an Animation Event on the LAST
    // frame of BOTH LightBandit_Death and HeavyBandit_Death and the enemy despawns the
    // instant that clip finishes instead of waiting out this flat guess.
    public float deathDespawnFallback = 3f;

    private Transform player;
    private Player playerComponent;
    private Transform currentTarget;
    private Rigidbody2D rb;
    private Animator animator;
    private bool isGrounded;
    private bool isAttacking;
    private bool isDead;
    private bool isWaitingAtPoint;
    private float lastAttackTime;
    private float waitTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();

        // Sprite might live on this object or on a child, depending on how the prefab is set up.
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        // If the patrol points were never wired up in the Inspector, make a pair on the
        // fly around the spawn position instead of standing still and spamming warnings.
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning($"{name}: Point A or Point B is not assigned — creating fallback patrol points.");
            pointA = CreateFallbackPoint("_PatrolA", -fallbackPatrolDistance);
            pointB = CreateFallbackPoint("_PatrolB", fallbackPatrolDistance);
        }

        currentTarget = pointB; // start heading toward point B

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerComponent = playerObj.GetComponent<Player>();
        }

        maxHealth = health;
        SpawnHealthBar();
    }

    private void SpawnHealthBar()
    {
        if (healthBarPrefab == null) return;

        GameObject barObj = Instantiate(healthBarPrefab);
        healthBar = barObj.GetComponent<EnemyHealthBar>();

        if (healthBar == null)
        {
            Debug.LogWarning($"{name}: healthBarPrefab is missing an EnemyHealthBar component.");
            return;
        }

        healthBar.target = transform;
        healthBar.offset = healthBarOffset;
        healthBar.SetHealth(health, maxHealth);
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        // The whole world holds still during a battle; the fighting bandit waits in combat stance.
        if (BattleManager.InBattle)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetInteger("AnimState", inBattle ? 1 : 0);
            return;
        }

        // Fooled or waiting to talk: stand still and don't chase or attack.
        if (passive)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetInteger("AnimState", 0); // idle
            return;
        }

        isGrounded = groundCheck != null &&
            Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("Grounded", isGrounded);

        float distanceToPlayer = player != null ? Mathf.Abs(transform.position.x - player.position.x) : Mathf.Infinity;

        if (isAttacking)
        {
            // Stand still while mid-attack
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetInteger("AnimState", 1); // Combat Idle
            return;
        }

        if (distanceToPlayer <= attackRange && GetHeightDiffToPlayer() <= maxAttackHeightDiff)
        {
            // Close enough to attack
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            FaceTarget(player.position);
            animator.SetInteger("AnimState", 1); // Combat Idle

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
            }
        }
        else if (distanceToPlayer <= chaseRange)
        {
            // Chase the player
            MoveToward(player.position);
        }
        else
        {
            // Patrol between point A and point B
            Patrol();
        }
    }

    private Transform CreateFallbackPoint(string suffix, float xOffset)
    {
        GameObject point = new GameObject(name + suffix);
        point.transform.position = transform.position + new Vector3(xOffset, 0f, 0f);
        return point.transform;
    }

    private void Patrol()
    {
        if (currentTarget == null) return;

        if (isWaitingAtPoint)
        {
            // Stand still and idle while waiting
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetInteger("AnimState", 0); // Idle

            waitTimer -= Time.fixedDeltaTime;
            if (waitTimer <= 0f)
            {
                isWaitingAtPoint = false;
                currentTarget = currentTarget == pointA ? pointB : pointA;
            }
            return;
        }

        MoveToward(currentTarget.position);

        float dist = Mathf.Abs(transform.position.x - currentTarget.position.x);

        if (dist < 0.3f)
        {
            isWaitingAtPoint = true;
            waitTimer = waitTimeAtPoint;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private float GetHeightDiffToPlayer()
    {
        float enemyFootY = groundCheck != null ? groundCheck.position.y : transform.position.y;
        float playerFootY = (playerComponent != null && playerComponent.groundCheck != null)
            ? playerComponent.groundCheck.position.y
            : player.position.y;

        return Mathf.Abs(enemyFootY - playerFootY);
    }

    private void MoveToward(Vector2 targetPos)
    {
        Vector2 direction = (targetPos - (Vector2)transform.position).normalized;

        // Edge guard: don't walk off a ledge (or the map) while chasing or patrolling.
        if (isGrounded && !GroundAhead(Mathf.Sign(direction.x)))
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            FaceTarget(targetPos);
            animator.SetInteger("AnimState", 1); // combat idle at the edge
            if (isWaitingAtPoint == false && currentTarget != null && targetPos == (Vector2)currentTarget.position)
            {
                // patrol point is past an edge: turn around instead of getting stuck
                isWaitingAtPoint = true;
                waitTimer = waitTimeAtPoint;
            }
            return;
        }

        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);

        FaceTarget(targetPos);
        animator.SetInteger("AnimState", 2); // Run
    }

    // Is there ground just in front of our feet?
    private bool GroundAhead(float dir)
    {
        if (groundLayer.value == 0 || dir == 0f) return true; // can't check, so don't block
        Vector2 feet = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        Vector2 ahead = feet + new Vector2(dir * 0.6f, 0.3f);
        return Physics2D.Raycast(ahead, Vector2.down, 1.2f, groundLayer).collider != null;
    }

    private void FaceTarget(Vector2 targetPos)
    {
        if (targetPos.x > transform.position.x)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else if (targetPos.x < transform.position.x)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
    }

    private void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        animator.Play("Attack", 0, 0f); // force play from the start, bypassing any early auto-exit transition

        // Damage is dealt via an Animation Event on LightBandit_Attack.anim calling DealDamage().
        Invoke(nameof(EndAttack), 0.6f);
    }

    public void DealDamage()
    {
        if (BattleManager.InBattle) return; // battle damage is handled by BattleManager

        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange + 0.5f)
        {
            Player playerScript = player.GetComponent<Player>();
            if (playerScript != null)
            {
                // Landing the first hit on the player starts an ambush battle (enemy goes first).
                if (BattleManager.Instance != null && BattleManager.Instance.TryStartBattle(playerScript, this, false)) return;

                playerScript.TakeDamage(attackDamage);
            }
        }
    }

    private void EndAttack()
    {
        isAttacking = false;
        animator.SetTrigger("Recover");
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        health -= amount;
        animator.SetTrigger("Hurt");
        healthBar?.SetHealth(health, maxHealth);

        // Same red flash the player gets when hit.
        if (spriteRenderer != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(DamageFlash());
        }

        if (health <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlash()
    {
        spriteRenderer.color = damageFlashColor;
        yield return new WaitForSeconds(damageFlashDuration);
        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        // Attack() schedules Invoke(EndAttack, 0.6f) on every swing. If the killing blow
        // lands mid-attack, that invoke is still pending and fires AFTER Death is
        // triggered below — calling SetTrigger("Recover") and knocking the Animator
        // straight back out of the death animation into idle. That's the "does a
        // Recover, stands there, then disappears" bug. Cancelling it, and clearing any
        // Hurt trigger still sitting in the queue for the same reason, makes Death win.
        CancelInvoke(nameof(EndAttack));
        isAttacking = false;
        animator.ResetTrigger("Hurt");
        animator.ResetTrigger("Recover");
        animator.SetTrigger("Death");

        // Turn collision off straight away so the player can walk through the body.
        DisableCollisionAfterDeath();

        // Make sure the corpse can't keep flashing or blocking the player.
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        DropCoins();

        if (healthBar != null)
        {
            Destroy(healthBar.gameObject); // bar isn't a child, so it won't go with the enemy on its own
        }

        // Fallback only, in case the Animation Event below isn't wired up yet.
        Invoke(nameof(OnDeathAnimationComplete), deathDespawnFallback);
    }

    // Add this as an Animation Event on the LAST frame of BOTH LightBandit_Death and
    // HeavyBandit_Death. Whichever fires first — this or the fallback above — wins;
    // the other becomes a harmless no-op since the object is already gone.
    public void OnDeathAnimationComplete()
    {
        CancelInvoke(nameof(OnDeathAnimationComplete));
        Destroy(gameObject);
    }

    private void DropCoins()
    {
        if (coinPrefab == null) return;

        int count = Random.Range(minCoins, maxCoins + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = transform.position + coinSpawnOffset;
            // Coin.cs handles its own landing hop and scatter from here — no force needed.
            Instantiate(coinPrefab, spawnPos, Quaternion.identity);
        }
    }

    // ---------------------------------------------------------------- battle hooks

    public bool IsDead => isDead;
    public int MaxHealth => maxHealth;

    public void EnterBattle(Vector3 playerPosition)
    {
        inBattle = true;
        CancelInvoke(nameof(EndAttack));
        isAttacking = false;
        isWaitingAtPoint = false;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        FaceTarget(playerPosition);
        animator.SetInteger("AnimState", 1); // combat idle
    }

    public void ExitBattle()
    {
        inBattle = false;
        lastAttackTime = Time.time; // don't swing (and re-trigger a battle) the instant it ends
    }

    public void PlayBattleAttack()
    {
        if (isDead) return;
        CancelInvoke(nameof(EndAttack));
        isAttacking = true;
        animator.Play("Attack", 0, 0f);
        Invoke(nameof(EndAttack), 0.6f);
    }
    
    // Still safe to call from an Animation Event on the death clip; Die() already does it.
    public void DeathCollision()
    {
        if (!isDead) return; // only once the bandit is actually dead (this check was reversed before)
        DisableCollisionAfterDeath();
    }

    // Fell off the map: counts as defeated. Its coins go straight to the player (they'd only fall
    // into the void), and a battle against it ends as a win. It's removed once the battle is over,
    // because deleting it mid-battle is what used to freeze the fight.
    public void FellOffMap()
    {
        if (isDead) return;
        isDead = true;
        health = 0;
        CancelInvoke(nameof(EndAttack));
        isAttacking = false;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; }
        DisableCollisionAfterDeath();
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (healthBar != null) Destroy(healthBar.gameObject);

        int coins = Random.Range(minCoins, maxCoins + 1);
        if (coins > 0) CoinCounter.Add(coins);

        StartCoroutine(RemoveAfterBattle());
    }

    private IEnumerator RemoveAfterBattle()
    {
        while (BattleManager.InBattle) yield return null;
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    private void DisableCollisionAfterDeath()
    {
        // Stop gravity FIRST. A dynamic Rigidbody with no colliders falls straight
        // through the floor, so the body would drop out of the level mid-animation.
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Every collider, including any on children, not just the BoxCollider2D.
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
        {
            c.enabled = false;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position + Vector3.up * 0.75f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, chaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, attackRange);
    }
}