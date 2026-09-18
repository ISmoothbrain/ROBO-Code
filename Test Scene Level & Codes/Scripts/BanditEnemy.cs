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

    [Header("Combat")]
    public int health = 30;
    public float attackCooldown = 1.5f;

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
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);

        FaceTarget(targetPos);
        animator.SetInteger("AnimState", 2); // Run
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
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange + 0.5f)
        {
            Player playerScript = player.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(10);
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

    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position + Vector3.up * 0.75f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, chaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, attackRange);
    }
}