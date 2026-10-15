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
    public int enemyAttackDamage = 10;    // to the player when the QTE is missed
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
        questionBag.Clear();

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
        ui.ShowBanner(won ? "VICTORY!" : "DEFEATED...");
        yield return new WaitForSeconds(1.6f);

        EndBattle(player, enemy);
    }

    private IEnumerator PlayerTurn(Player player, BanditEnemy enemy)
    {
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
        while (chosen < 0 && t < answerTimeLimit)
        {
            t += Time.deltaTime;
            ui.SetTimer(1f - t / answerTimeLimit);
            yield return null;
        }

        bool correct = chosen == q.correctIndex;
        ui.RevealAnswer(q, chosen, correct);
        yield return new WaitForSeconds(1.8f);
        ui.HideQuestion();

        if (correct)
        {
            int dmg = Mathf.RoundToInt((correctAnswerDamage + PlayerUpgrades.BonusDamage) * Mathf.Max(0.1f, q.damageMultiplier));
            player.PlayBattleAttack();
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
        ui.ShowBanner("ENEMY TURN\nPress the key when the rings meet!");
        yield return new WaitForSeconds(1f);

        bool dodged = false;
        yield return qte.Run(result => dodged = result);

        enemy.PlayBattleAttack();
        yield return new WaitForSeconds(0.3f);

        if (dodged)
        {
            ui.ShowBanner("DODGED!");
        }
        else
        {
            player.TakeDamage(enemyAttackDamage);
            ui.ShowBanner($"HIT!\n-{enemyAttackDamage} HP");
        }

        yield return new WaitForSeconds(0.9f);
    }

    private void EndBattle(Player player, BanditEnemy enemy)
    {
        ui.Hide();
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

        float playerMove = SafeDistance(player.transform, playerDir, half, player.groundLayer);
        float enemyMove = SafeDistance(enemy.transform, -playerDir, half, enemy.groundLayer);

        // If one of them has a wall or a ledge behind it, the other hops further to make up for it.
        float leftover = need - playerMove - enemyMove;
        if (leftover > 0.05f)
        {
            if (playerMove < half - 0.01f)
                enemyMove = SafeDistance(enemy.transform, -playerDir, enemyMove + leftover, enemy.groundLayer);
            else
                playerMove = SafeDistance(player.transform, playerDir, playerMove + leftover, player.groundLayer);
        }

        Animator playerAnim = player.GetComponent<Animator>();
        Animator enemyAnim = enemy.GetComponent<Animator>();
        if (playerAnim != null && !string.IsNullOrEmpty(player.jumpState)) playerAnim.Play(player.jumpState);
        if (enemyAnim != null) enemyAnim.SetBool("Grounded", false);

        yield return Hop(
            new[] { player.transform, enemy.transform },
            new[] { playerDir * playerMove, -playerDir * enemyMove });

        if (enemyAnim != null) enemyAnim.SetBool("Grounded", true);
        player.ReturnToBattleIdle();
    }

    // How far a fighter can safely hop: stops short of walls and never off the edge of a ledge.
    private static float SafeDistance(Transform t, float dir, float wanted, LayerMask ground)
    {
        if (ground.value == 0) return wanted; // no ground layer set, so we can't check

        for (float d = wanted; d > 0.05f; d -= 0.25f)
        {
            Vector2 from = (Vector2)t.position + Vector2.up * 0.5f;
            Vector2 to = from + Vector2.right * dir * d;

            if (Physics2D.Linecast(from, to, ground).collider != null) continue;            // wall in the way
            if (Physics2D.Raycast(to, Vector2.down, 1.5f, ground).collider == null) continue; // no floor there

            return d;
        }
        return 0f;
    }

    private IEnumerator Hop(Transform[] bodies, float[] offsetsX)
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
            for (int i = 0; i < n; i++)
            {
                if (bodies[i] == null) continue;
                Vector3 p = start[i] + Vector3.right * offsetsX[i] * Mathf.SmoothStep(0f, 1f, k);
                p.y += Mathf.Sin(k * Mathf.PI) * hopHeight;
                bodies[i].position = p;
            }
            yield return null;
        }

        for (int i = 0; i < n; i++)
        {
            if (bodies[i] == null) continue;
            bodies[i].position = start[i] + Vector3.right * offsetsX[i];
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

    // Shuffled "bag": no repeats until every question has been used once.
    private PythonQuestion NextQuestion(BanditEnemy enemy)
    {
        if (questionBag.Count == 0)
        {
            PythonQuestion[] pool = enemy.battleQuestions != null && enemy.battleQuestions.Length > 0
                ? enemy.battleQuestions
                : defaultQuestions;
            if (pool == null) return null;

            foreach (PythonQuestion q in pool) if (q != null) questionBag.Add(q);
            for (int i = questionBag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (questionBag[i], questionBag[j]) = (questionBag[j], questionBag[i]);
            }
        }

        if (questionBag.Count == 0) return null;
        PythonQuestion next = questionBag[questionBag.Count - 1];
        questionBag.RemoveAt(questionBag.Count - 1);
        return next;
    }
}
