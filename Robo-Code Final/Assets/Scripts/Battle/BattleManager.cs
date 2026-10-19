using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Turns a platformer hit into a Persona-style turn-based fight.
//   Player hits a bandit first  -> battle starts, player gets the first turn.
//   Bandit hits the player first -> battle starts, the bandit attacks first.
// Player turn: fill in the blank in some Python code. Right = damage the enemy, wrong = take damage.
// Enemy turn:  a quick-time event. Press the shown key as the ring closes, or take damage.
// Build everything with Tools > Battle System > Set Up Battle In Open Scene.
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    public static bool InBattle { get; private set; }

    [Header("References")]
    public BattleUI ui;
    public QuickTimeEvent qte;
    [Tooltip("Boss arrow combo. Created automatically by BattleBootstrap.")]
    public ArrowSequence arrows;
    [Tooltip("Attack/Hack choice. Created automatically by BattleBootstrap.")]
    public HackMenu hackMenu;

    private const float HackCost = 20f;  // one energy bar
    private int enemySkipTurns;          // HACK: OVERRIDE
    private bool enemyWeakened;          // HACK: OVERLOAD

    // Answer time: the Settings menu's choice, or BattleSettings' Answer Time Limit.
    private float AnswerLimit => GameSettings.AnswerTime > 0f ? GameSettings.AnswerTime : answerTimeLimit;
    [Tooltip("A CinemachineCamera with a higher priority than the normal one. Kept inactive until a battle starts.")]
    public GameObject battleCamera;
    [Tooltip("Used instead of Battle Camera when that's empty: zooms your existing camera directly.")]
    public BattleCameraDirector cameraDirector;
    [Tooltip("The battle camera tracks this. It's moved between the two fighters.")]
    public Transform battleFocus;
    [Tooltip("Negative Y aims the camera below the fighters, so they sit above the code panel.")]
    public Vector3 focusOffset = new Vector3(0f, -0.5f, 0f);

    [Tooltip("Optional. When set, its values are copied in at the start of every battle.")]
    public BattleSettings settings;

    [Header("Questions")]
    [Tooltip("Used when the enemy has no Battle Questions of its own.")]
    public PythonQuestion[] defaultQuestions;

    [Header("Rules")]
    public bool startBattleOnHit = true;
    public int correctAnswerDamage = 15;  // to the enemy
    public int wrongAnswerDamage = 10;    // to the player
    public int enemyAttackDamage = 10;    // old: battles now use each enemy's own Attack Damage
    public float enemyDamageMultiplier = 1f; // scales every enemy's Attack Damage in battles
    public float answerTimeLimit = 20f;

    [Header("Staging")]
    [Tooltip("How far apart (in world units) the fighters stand once the battle starts.")]
    public float battleSpacing = 3f;
    public float hopTime = 0.35f;
    public float hopHeight = 0.6f;
    [Tooltip("Hide the normal HUD (health bar, mana, coins) during battles.")]
    public bool hideHudDuringBattle = true;
    [Tooltip("HUD objects with these names stay visible, e.g. the brightness overlay.")]
    public string[] keepVisibleNames = { "BrightnessOverlay" };

    [Header("Timing")]
    public float cameraBlendTime = 0.8f;  // keep close to the CinemachineBrain's Default Blend time
    public float cooldownAfterBattle = 1.5f;

    private float nextBattleAllowed;
    private readonly List<GameObject> hiddenHud = new List<GameObject>();
    private readonly List<PythonQuestion> questionBag = new List<PythonQuestion>();
    private BanditEnemy lastQuestionSource; // only reshuffle when switching to an enemy with its own questions

    private void Awake()
    {
        Instance = this;
        InBattle = false;
        if (battleCamera != null) battleCamera.SetActive(false);

        int count = defaultQuestions != null ? defaultQuestions.Length : 0;
        Debug.Log($"[Battle] Ready. Camera: {(battleCamera != null ? battleCamera.name : cameraDirector != null ? "auto zoom" : "NONE (no zoom)")}, " +
                  $"UI: {(ui != null ? "ok" : "MISSING")}, QTE: {(qte != null ? "ok" : "MISSING")}, questions: {count}.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            InBattle = false; // a scene reload mid-battle must not leave the game frozen
        }
    }

    public bool TryStartBattle(Player player, BanditEnemy enemy, bool playerFirst)
    {
        if (InBattle) return false;
        if (!startBattleOnHit)
        {
            Debug.Log("[Battle] Start Battle On Hit is off on the BattleManager, so no battle.");
            return false;
        }
        if (player == null || enemy == null || player.IsDead || enemy.IsDead) return false;
        if (Time.time < nextBattleAllowed)
        {
            Debug.Log("[Battle] Hit landed during the short cooldown after a battle, so no new battle yet.");
            return false;
        }
        if (ui == null || qte == null)
        {
            Debug.LogWarning("BattleManager: UI or QTE not assigned. Run Tools > Battle System > Set Up Battle In Open Scene.");
            return false;
        }

        ApplySettings();
        Debug.Log($"[Battle] Starting battle with {enemy.name} ({(playerFirst ? "player" : "enemy")} goes first).");
        StartCoroutine(BattleRoutine(player, enemy, playerFirst));
        return true;
    }

    // ---------------------------------------------------------------- main loop

    private IEnumerator BattleRoutine(Player player, BanditEnemy enemy, bool playerFirst)
    {
        InBattle = true;
        enemySkipTurns = 0;
        enemyWeakened = false;
                if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.battleStart);
            AudioManager.instance.PlayMusic(AudioManager.instance.battleMusic);
        }
        // The question bag is NOT reset here, so you see every question before any repeats.
        // Only reshuffle when the question pool changes (an enemy with its own questions vs. the defaults).
        
        BanditEnemy source = enemy.battleQuestions != null && enemy.battleQuestions.Length > 0 ? enemy : null;
        if (source != lastQuestionSource)
        {
            questionBag.Clear();
            lastQuestionSource = source;
        }

        player.EnterBattle(enemy.transform.position);
        enemy.EnterBattle(player.transform.position);
        if (hideHudDuringBattle) HideHud();

        // Both fighters hop back so they aren't standing on top of each other.
        yield return HopApart(player, enemy);

        if (battleFocus != null)
        {
            battleFocus.position = (player.transform.position + enemy.transform.position) * 0.5f + focusOffset;
        }
        if (battleCamera != null) battleCamera.SetActive(true);
        else if (cameraDirector != null) cameraDirector.Enter(battleFocus);

        ui.Show(enemy.name);
        ui.RefreshHealth(player.health, player.maxHealth, enemy.health, enemy.MaxHealth);
        ui.ShowBanner(playerFirst ? "ADVANTAGE!\nYou strike first" : "AMBUSH!\nThe enemy strikes first");
        yield return new WaitForSeconds(cameraBlendTime + 0.6f);

        bool playerTurn = playerFirst;
        while (!player.IsDead && !enemy.IsDead)
        {
            if (playerTurn) yield return PlayerTurn(player, enemy);
            else yield return EnemyTurn(player, enemy);

            ui.RefreshHealth(player.health, player.maxHealth, enemy.health, enemy.MaxHealth);
            playerTurn = !playerTurn;
            yield return new WaitForSeconds(0.4f);
        }

        bool won = enemy == null || enemy.IsDead;
                if (AudioManager.instance != null)
        {
                        if (won) AudioManager.instance.PlaySFX(AudioManager.instance.victory);
        }
        ui.ShowBanner(won ? "VICTORY!" : "DEFEATED...");
        yield return new WaitForSeconds(1.6f);

        EndBattle(player, enemy);
    }

    private IEnumerator PlayerTurn(Player player, BanditEnemy enemy)
    {
        // Once HACK is crafted, the turn starts with a choice: attack, or hack (1 energy bar).
        PlayerSkills skills = player.GetComponent<PlayerSkills>();
        if (hackMenu != null && GameSave.HackUnlocked && skills != null)
        {
            int bars = Mathf.FloorToInt(skills.Mana / HackCost);
            int choice = 0;
            yield return hackMenu.Choose(bars >= 1, bars, c => choice = c);
            if (choice > 0)
            {
                yield return HackTurn(player, enemy, skills, choice == 1);
                yield break;
            }
        }

        PythonQuestion q = NextQuestion(enemy);
        if (q == null)
        {
            Debug.LogWarning("BattleManager: no Python questions assigned. Auto-hitting instead.");
            enemy.TakeDamage(correctAnswerDamage);
            yield return new WaitForSeconds(0.6f);
            yield break;
        }

        ui.ShowBanner("YOUR TURN\nFix the code!");
        yield return new WaitForSeconds(0.7f);

        int chosen = -1;
        ui.ShowQuestion(q, i => chosen = i);

        float t = 0f;
        float limit = AnswerLimit;
        while (chosen < 0 && t < limit)
        {
            t += Time.deltaTime;
            ui.SetTimer(1f - t / limit);
            yield return null;
        }

        bool correct = chosen == q.correctIndex;
        ui.RevealAnswer(q, chosen, correct);
        yield return new WaitForSeconds(1.8f);
        ui.HideQuestion();

        if (correct)
        {
            int dmg = Mathf.RoundToInt((correctAnswerDamage + PlayerUpgrades.BonusDamage) * Mathf.Max(0.1f, q.damageMultiplier));
            if (enemyWeakened) dmg *= 2; // OVERLOAD
            player.PlayBattleAttack();
                        if (AudioManager.instance != null) AudioManager.instance.PlaySFX(AudioManager.instance.attack);
            yield return new WaitForSeconds(0.3f);
            enemy.TakeDamage(dmg);
            ui.ShowBanner($"Code compiled!\n{dmg} damage");
        }
        else
        {
            player.TakeDamage(wrongAnswerDamage);
            ui.ShowBanner(chosen < 0 ? $"Time's up!\n-{wrongAnswerDamage} HP" : $"Bug! Your code crashed\n-{wrongAnswerDamage} HP");
        }

        yield return new WaitForSeconds(0.9f);
        if (!player.IsDead) player.ReturnToBattleIdle();
    }

    private IEnumerator EnemyTurn(Player player, BanditEnemy enemy)
    {
        // HACK: OVERRIDE - the enemy is under your control and loses this turn.
        if (enemySkipTurns > 0)
        {
            enemySkipTurns--;
            ui.ShowBanner("OVERRIDDEN!\nThe enemy can't attack this turn");
            yield return new WaitForSeconds(1.4f);
            yield break;
        }

        ui.ShowBanner("ENEMY TURN\nPress the key when the rings meet!");
        yield return new WaitForSeconds(1f);

        bool dodged = false;
        yield return qte.Run(result => dodged = result);

        // Bosses add a second test: an arrow-key combo, Helldivers "Stratagem Hero" style.
        bool comboOk = true;
        if (enemy.isBoss && arrows != null)
        {
            ui.ShowBanner("BOSS COMBO!\nType the arrows!");
            yield return new WaitForSeconds(0.5f);
            yield return arrows.Run(enemy.arrowComboLength, enemy.arrowComboTime, r => comboOk = r);
        }

        enemy.PlayBattleAttack();
                if (AudioManager.instance != null) AudioManager.instance.PlaySFX(AudioManager.instance.enemyAttack);
        yield return new WaitForSeconds(0.3f);

        // One hit for a missed ring, and (bosses only) another for a failed combo.
        int dmg = Mathf.Max(0, Mathf.RoundToInt(enemy.attackDamage * enemyDamageMultiplier * (enemyWeakened ? 0.5f : 1f)));
        int hits = (dodged ? 0 : 1) + (comboOk ? 0 : 1);
        if (hits == 0)
        {
            ui.ShowBanner("DODGED!");
            if (AudioManager.instance != null) AudioManager.instance.PlaySFX(AudioManager.instance.defend);
        }
        else
        {
            player.TakeDamage(dmg * hits);
            ui.ShowBanner(hits == 2 ? $"DOUBLE HIT!\n-{dmg * 2} HP" : $"HIT!\n-{dmg} HP");
        }

        yield return new WaitForSeconds(0.9f);
    }

    private void EndBattle(Player player, BanditEnemy enemy)
    {
        ui.Hide();
                if (AudioManager.instance != null) AudioManager.instance.PlayMusic(AudioManager.instance.gameMusic);
        RestoreHud();
        if (battleCamera != null) battleCamera.SetActive(false);
        else if (cameraDirector != null) cameraDirector.Exit();

        if (player != null) player.ExitBattle();
        if (enemy != null && !enemy.IsDead) enemy.ExitBattle();

        InBattle = false;
        nextBattleAllowed = Time.time + cooldownAfterBattle;
    }

    // Copies BattleSettings in fresh each fight, so edits made during Play mode take effect.
    private void ApplySettings()
    {
        if (settings == null) return;

        focusOffset = settings.focusOffset;
        battleSpacing = settings.battleSpacing;
        hopTime = settings.hopTime;
        hopHeight = settings.hopHeight;
        correctAnswerDamage = settings.correctAnswerDamage;
        wrongAnswerDamage = settings.wrongAnswerDamage;
        enemyAttackDamage = settings.enemyAttackDamage;
        enemyDamageMultiplier = settings.enemyDamageMultiplier;
        answerTimeLimit = settings.answerTimeLimit;

        if (cameraDirector != null)
        {
            cameraDirector.zoomFactor = settings.zoomFactor;
            cameraDirector.zoomTime = settings.zoomTime;
        }
        if (qte != null)
        {
            qte.closeTime = settings.qteCloseTime;
            qte.hitWindow = settings.qteHitWindow;
        }
    }

    // ---------------------------------------------------------------- staging

    private IEnumerator HopApart(Player player, BanditEnemy enemy)
    {
        float gap = Mathf.Abs(player.transform.position.x - enemy.transform.position.x);
        float need = battleSpacing - gap;
        if (need <= 0.05f) yield break;

        float playerDir = player.transform.position.x >= enemy.transform.position.x ? 1f : -1f;
        float half = need * 0.5f;

        Vector3 playerTarget = FindLanding(player.transform, playerDir, half, player.groundLayer);
        Vector3 enemyTarget = FindLanding(enemy.transform, -playerDir, half, enemy.groundLayer);

        // If one of them is backed against a wall or cliff, the other hops further to make up for it.
        float leftover = need - Mathf.Abs(playerTarget.x - player.transform.position.x)
                              - Mathf.Abs(enemyTarget.x - enemy.transform.position.x);
        if (leftover > 0.05f)
        {
            float playerMoved = Mathf.Abs(playerTarget.x - player.transform.position.x);
            float enemyMoved = Mathf.Abs(enemyTarget.x - enemy.transform.position.x);
            if (playerMoved < half - 0.01f)
                enemyTarget = FindLanding(enemy.transform, -playerDir, enemyMoved + leftover, enemy.groundLayer);
            else
                playerTarget = FindLanding(player.transform, playerDir, playerMoved + leftover, player.groundLayer);
        }

        Animator playerAnim = player.GetComponent<Animator>();
        Animator enemyAnim = enemy.GetComponent<Animator>();
        if (playerAnim != null && !string.IsNullOrEmpty(player.jumpState)) playerAnim.Play(player.jumpState);
        if (enemyAnim != null) enemyAnim.SetBool("Grounded", false);

        yield return Hop(new[] { player.transform, enemy.transform }, new[] { playerTarget, enemyTarget });

        if (enemyAnim != null) enemyAnim.SetBool("Grounded", true);
        player.ReturnToBattleIdle();
    }

    // Where a fighter lands if it hops up to 'wanted' units in 'dir'. It can hop up onto low things
    // (rocks, small steps) or down small drops, but never into a wall or off a cliff.
    // Returns its current position if there's nowhere safe to go.
    private static Vector3 FindLanding(Transform t, float dir, float wanted, LayerMask ground)
    {
        Vector3 here = t.position;
        if (ground.value == 0) return here + Vector3.right * dir * wanted; // no ground layer set, so we can't check

        Collider2D body = t.GetComponent<Collider2D>();
        float feetOffset = body != null ? here.y - body.bounds.min.y : 0f;
        // Both feet must be on solid ground, with a little margin, so nobody lands half over an edge.
        float footHalf = body != null ? body.bounds.extents.x * 0.9f + 0.1f : 0.4f;
        float feetY = here.y - feetOffset;
        const float maxStepUp = 1.2f, maxStepDown = 1.0f, headroom = 1.4f;

        for (float d = wanted; d > 0.05f; d -= 0.2f)
        {
            float x = here.x + dir * d;

            // Find the floor at the landing spot.
            RaycastHit2D floor = Physics2D.Raycast(new Vector2(x, feetY + maxStepUp + 0.3f), Vector2.down,
                                                   maxStepUp + maxStepDown + 0.3f, ground);
            if (floor.collider == null) continue;                    // a cliff or a gap: no floor
            if (floor.distance < 0.01f) continue;                    // started inside a wall
            if (floor.point.y > feetY + maxStepUp) continue;         // too high to hop onto
            if (!FloorUnder(x - footHalf, floor.point.y, ground) || !FloorUnder(x + footHalf, floor.point.y, ground))
                continue;                                            // too close to an edge

            // Make sure nothing blocks the arc, measured above the higher of the two floors.
            float clearY = Mathf.Max(feetY, floor.point.y) + headroom;
            if (Physics2D.Linecast(new Vector2(here.x, clearY), new Vector2(x, clearY), ground).collider != null) continue;

            return new Vector3(x, floor.point.y + feetOffset + 0.02f, here.z);
        }
        return here;
    }

    // Is there ground at this x, at about the same height as the landing floor?
    private static bool FloorUnder(float x, float floorY, LayerMask ground)
    {
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, floorY + 0.5f), Vector2.down, 0.9f, ground);
        return hit.collider != null && hit.distance > 0.01f;
    }

    private IEnumerator Hop(Transform[] bodies, Vector3[] targets)
    {
        int n = bodies.Length;
        Vector3[] start = new Vector3[n];
        Rigidbody2D[] rbs = new Rigidbody2D[n];
        RigidbodyType2D[] types = new RigidbodyType2D[n];

        for (int i = 0; i < n; i++)
        {
            start[i] = bodies[i].position;
            rbs[i] = bodies[i].GetComponent<Rigidbody2D>();
            if (rbs[i] != null)
            {
                types[i] = rbs[i].bodyType;
                rbs[i].linearVelocity = Vector2.zero;
                rbs[i].bodyType = RigidbodyType2D.Kinematic; // so gravity doesn't fight the arc
            }
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.05f, hopTime);
            float k = Mathf.Clamp01(t);
            float e = Mathf.SmoothStep(0f, 1f, k);
            for (int i = 0; i < n; i++)
            {
                if (bodies[i] == null) continue;
                Vector3 p = Vector3.Lerp(start[i], targets[i], e);
                if ((targets[i] - start[i]).sqrMagnitude > 0.0001f) p.y += Mathf.Sin(k * Mathf.PI) * hopHeight;
                bodies[i].position = p;
            }
            yield return null;
        }

        for (int i = 0; i < n; i++)
        {
            if (bodies[i] == null) continue;
            bodies[i].position = targets[i];
            if (rbs[i] != null)
            {
                rbs[i].bodyType = types[i];
                rbs[i].linearVelocity = Vector2.zero;
            }
        }
    }

    // Hides the normal HUD (health, mana, coins...) while the battle screen is up.
    private void HideHud()
    {
        hiddenHud.Clear();
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!c.isRootCanvas) continue;
            foreach (Transform child in c.transform)
            {
                GameObject go = child.gameObject;
                if (!go.activeSelf) continue;
                if (IsBattleObject(child) || ShouldStayVisible(go)) continue;
                go.SetActive(false);
                hiddenHud.Add(go);
            }
        }
    }

    private void RestoreHud()
    {
        foreach (GameObject go in hiddenHud) if (go != null) go.SetActive(true);
        hiddenHud.Clear();
    }

    private bool IsBattleObject(Transform t)
    {
        return (ui != null && (ui.transform == t || ui.transform.IsChildOf(t) || t.IsChildOf(ui.transform))) ||
               (qte != null && (qte.transform == t || qte.transform.IsChildOf(t) || t.IsChildOf(qte.transform)));
    }

    private bool ShouldStayVisible(GameObject go)
    {
        if (keepVisibleNames == null) return false;
        foreach (string n in keepVisibleNames) if (!string.IsNullOrEmpty(n) && go.name == n) return true;
        return false;
    }

    // ---------------------------------------------------------------- questions

    // Questions already asked this play session (kept across level reloads), so you see every
    // question before any repeats. Normal enemies ask easy-medium, bosses ask medium-hard.
    private static readonly HashSet<string> askedNormal = new HashSet<string>();
    private static readonly HashSet<string> askedBoss = new HashSet<string>();
    private static readonly HashSet<string> askedCustom = new HashSet<string>();

    private PythonQuestion NextQuestion(BanditEnemy enemy)
    {
        bool custom = enemy.battleQuestions != null && System.Array.Exists(enemy.battleQuestions, q => q != null);
        PythonQuestion[] source = custom ? enemy.battleQuestions : defaultQuestions;
        if (source == null) return null;

        // Questions get harder level by level (LevelProgress tier):
        //   normal enemies: The Safe Haven easy, Magister's Island easy-medium, Gemini's Blade medium-hard
        //   bosses:         Magister's Island medium-hard, Gemini's Blade hard only
        int tier = LevelProgress.CurrentTier();
        int minDifficulty = enemy.isBoss ? Mathf.Clamp(tier, 2, 3) : Mathf.Max(1, tier - 1);
        int maxDifficulty = enemy.isBoss ? 3 : Mathf.Min(3, tier);

        List<PythonQuestion> pool = new List<PythonQuestion>();
        foreach (PythonQuestion q in source)
        {
            if (q == null) continue;
            if (!custom && (q.difficulty < minDifficulty || q.difficulty > maxDifficulty)) continue;
            pool.Add(q);
        }
        if (pool.Count == 0) return null;

        HashSet<string> asked = custom ? askedCustom : (enemy.isBoss ? askedBoss : askedNormal);
        List<PythonQuestion> fresh = pool.FindAll(q => !asked.Contains(Key(q)));
        if (fresh.Count == 0)
        {
            foreach (PythonQuestion q in pool) asked.Remove(Key(q)); // seen them all: start a new round
            fresh = pool;
        }
        PythonQuestion pick = fresh[Random.Range(0, fresh.Count)];
        asked.Add(Key(pick));
        return pick;
    }

    private static string Key(PythonQuestion q) =>
        q.goal + "|" + (q.codeLines != null && q.codeLines.Length > 0 ? q.codeLines[0] : "");

    // ------------------------------------------------------------------ HACK
    // Costs 1 energy bar and asks a HARD question. Right: the hack lands. Wrong: it's traced back
    // to you and you take double the wrong-answer damage. High risk, high reward.
    private IEnumerator HackTurn(Player player, BanditEnemy enemy, PlayerSkills skills, bool overrideHack)
    {
        skills.TrySpendMana(HackCost);
        ui.ShowBanner(overrideHack ? "HACK: OVERRIDE\nBreak in to take control!" : "HACK: OVERLOAD\nBreak in to overload it!");
        yield return new WaitForSeconds(0.8f);

        bool correct = true;
        PythonQuestion q = HardQuestion();
        if (q != null)
        {
            int chosen = -1;
            ui.ShowQuestion(q, i => chosen = i);
            float t = 0f, limit = AnswerLimit;
            while (chosen < 0 && t < limit)
            {
                t += Time.deltaTime;
                ui.SetTimer(1f - t / limit);
                yield return null;
            }
            correct = chosen == q.correctIndex;
            ui.RevealAnswer(q, chosen, correct);
            yield return new WaitForSeconds(1.8f);
            ui.HideQuestion();
        }

        if (correct)
        {
            if (overrideHack)
            {
                enemySkipTurns = 1;
                ui.ShowBanner("OVERRIDE!\nThe enemy loses its next turn");
            }
            else
            {
                enemyWeakened = true;
                ui.ShowBanner("OVERLOAD!\nDouble damage to it, half from it");
            }
            if (AudioManager.instance != null) AudioManager.instance.PlaySFX(AudioManager.instance.levelUp);
        }
        else
        {
            int dmg = wrongAnswerDamage * 2;
            player.TakeDamage(dmg);
            ui.ShowBanner($"HACK TRACED!\n-{dmg} HP");
        }

        yield return new WaitForSeconds(1.1f);
        if (!player.IsDead) player.ReturnToBattleIdle();
    }

    private PythonQuestion HardQuestion()
    {
        List<PythonQuestion> pool = new List<PythonQuestion>();
        if (defaultQuestions != null)
            foreach (PythonQuestion q in defaultQuestions)
                if (q != null && q.difficulty >= 3) pool.Add(q);
        return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
    }
}
