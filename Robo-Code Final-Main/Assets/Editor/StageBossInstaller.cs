#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Tools > NightBorne: one-click, repeat-safe setup of the two new boss arenas.
// Deliberately opt-in: never silently rewrites someone's scene on script reload.
public static class StageBossInstaller
{
    private const string Base = "Assets/StageBosses";
    private const string AnimationPath = Base + "/Animations";
    private const string PrefabPath = Base + "/Prefabs";
    private const string TilePath = Base + "/Tiles";

    [MenuItem("Tools/NightBorne/Install Stage Bosses Into Levels 2 & 3")]
    public static void Install()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string oldScene = SceneManager.GetActiveScene().path;
        AssetDatabase.Refresh();
        EnsureFolder(AnimationPath);
        EnsureFolder(PrefabPath);
        EnsureFolder(TilePath);

        GameObject nightBorne = MakeBoss("NightBorne", "NightBorne Warden", 130, 17, 4, 0.95f);
        GameObject magitech = MakeBoss("Magitech", "Magitech Overlord", 190, 24, 6, 0.72f);
        Tile floor = MakeTile("LabFloor", true);
        Tile backdrop = MakeTile("LabPanel", false);
        Tile platform = MakeTile("LabSmallPlatform", false);
        Tile monitor = MakeTile("LabMonitor", false);
        Tile hazard = MakeTile("LabHazard", false);
        AssetDatabase.SaveAssets();

        if (nightBorne == null || magitech == null || floor == null)
        {
            Debug.LogError("[NightBorne] Couldn't import sprites or create prefabs. Check the StageBosses folder.");
            return;
        }

        AddArena("Assets/Scenes/Level 2.unity", nightBorne, floor, backdrop, platform, monitor, hazard);
        AddArena("Assets/Scenes/Level 3.unity", magitech, floor, backdrop, platform, monitor, hazard);
        if (!string.IsNullOrEmpty(oldScene) && File.Exists(oldScene))
            EditorSceneManager.OpenScene(oldScene, OpenSceneMode.Single);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[NightBorne] Stage bosses installed. Open Levels 2/3 to inspect the end-of-level arenas.");
    }

    [MenuItem("Tools/NightBorne/Rebuild Boss Prefabs Only")]
    public static void BuildPrefabs()
    {
        AssetDatabase.Refresh();
        EnsureFolder(AnimationPath);
        EnsureFolder(PrefabPath);
        MakeBoss("NightBorne", "NightBorne Warden", 130, 17, 4, 0.95f);
        MakeBoss("Magitech", "Magitech Overlord", 190, 24, 6, 0.72f);
        AssetDatabase.SaveAssets();
        Debug.Log("[NightBorne] Prefabs rebuilt under Assets/StageBosses/Prefabs.");
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static Sprite[] Frames(string character, string animation)
    {
        string directory = Base + "/Sprites/" + character + "/" + animation;
        List<Sprite> sprites = new List<Sprite>();
        foreach (string path in Directory.GetFiles(directory, "*.png"))
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace('\\', '/'));
            if (sprite != null) sprites.Add(sprite);
        }
        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites.ToArray();
    }

    private static AnimationClip Clip(string character, string name, Sprite[] sprites, int fps, bool loop)
    {
        string path = AnimationPath + "/" + character + "_" + name + ".anim";
        if (sprites.Length == 0) return null;
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.frameRate = fps;
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < keys.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = (float)i / fps, value = sprites[i] };
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);

        SerializedObject so = new SerializedObject(clip);
        SerializedProperty loopProperty = so.FindProperty("m_AnimationClipSettings.m_LoopTime");
        if (loopProperty != null) { loopProperty.boolValue = loop; so.ApplyModifiedPropertiesWithoutUndo(); }
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController Controller(string character)
    {
        Sprite[] idleFrames = Frames(character, "idle");
        Sprite[] attackFrames = Frames(character, "attack");
        Sprite[] deathFrames = Frames(character, "death");
        if (idleFrames.Length == 0 || attackFrames.Length == 0 || deathFrames.Length == 0)
        {
            Debug.LogError("[NightBorne] Missing frames for " + character);
            return null;
        }

        AnimationClip idle = Clip(character, "Idle", idleFrames, 8, true);
        AnimationClip attack = Clip(character, "Attack", attackFrames, 10, false);
        AnimationClip death = Clip(character, "Death", deathFrames, 10, false);
        Sprite[] runFrames = Frames(character, "run");
        Sprite[] hurtFrames = Frames(character, "hurt");
        AnimationClip run = runFrames.Length > 0 ? Clip(character, "Run", runFrames, 10, true) : idle;
        AnimationClip hurt = hurtFrames.Length > 0 ? Clip(character, "Hurt", hurtFrames, 10, false) : attack;

        string path = AnimationPath + "/" + character + ".controller";
        // Clean rebuild: don't accumulate duplicate transitions when the menu is clicked twice.
        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("AnimState", AnimatorControllerParameterType.Int);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Recover", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState idleState = sm.AddState("Idle");
        AnimatorState runState = sm.AddState("Run");
        AnimatorState attackState = sm.AddState("Attack");
        AnimatorState hurtState = sm.AddState("Hurt");
        AnimatorState deathState = sm.AddState("Death");
        sm.defaultState = idleState;
        idleState.motion = idle;
        runState.motion = run;
        attackState.motion = attack;
        hurtState.motion = hurt;
        deathState.motion = death;

        AnimatorStateTransition toRun = idleState.AddTransition(runState);
        toRun.hasExitTime = false; toRun.duration = 0f;
        toRun.AddCondition(AnimatorConditionMode.Equals, 2f, "AnimState");

        AnimatorStateTransition toIdle = runState.AddTransition(idleState);
        toIdle.hasExitTime = false; toIdle.duration = 0f;
        toIdle.AddCondition(AnimatorConditionMode.NotEqual, 2f, "AnimState");

        AnimatorStateTransition hurtTransition = sm.AddAnyStateTransition(hurtState);
        hurtTransition.hasExitTime = false; hurtTransition.duration = 0f;
        hurtTransition.AddCondition(AnimatorConditionMode.If, 0f, "Hurt");

        AnimatorStateTransition deathTransition = sm.AddAnyStateTransition(deathState);
        deathTransition.hasExitTime = false; deathTransition.duration = 0f;
        deathTransition.AddCondition(AnimatorConditionMode.If, 0f, "Death");

        AnimatorStateTransition hurtEnd = hurtState.AddTransition(idleState);
        hurtEnd.hasExitTime = true; hurtEnd.exitTime = 1f; hurtEnd.duration = 0f;
        AnimatorStateTransition attackEnd = attackState.AddTransition(idleState);
        attackEnd.hasExitTime = true; attackEnd.exitTime = 1f; attackEnd.duration = 0f;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject MakeBoss(string folder, string title, int hp, int dmg, int arrows, float arrowTime)
    {
        AnimatorController controller = Controller(folder);
        if (controller == null) return null;

        // Clone the project's working enemy prefab so ground layers, collision,
        // patrol logic, enemy hits and health bars behave exactly like ordinary enemies.
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bandito.prefab");
        if (template == null) template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Clanker.prefab");
        if (template == null) { Debug.LogError("[NightBorne] Can't find Bandito.prefab"); return null; }
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
        instance.name = title;
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        BanditEnemy enemy = instance.GetComponent<BanditEnemy>();
        if (enemy == null)
        {
            Debug.LogError("[NightBorne] The base enemy prefab has no BanditEnemy component!");
            Object.DestroyImmediate(instance);
            return null;
        }

        enemy.isStageBoss = true;
        enemy.bossArrowCount = arrows;
        enemy.bossArrowSeconds = arrowTime;
        enemy.health = hp;
        enemy.attackDamage = dmg;
        enemy.chaseRange = 8f;
        enemy.attackCooldown = 1.35f;
        enemy.minCoins = 14;
        enemy.maxCoins = 26;
        enemy.deathDespawnFallback = 3f;
        enemy.battleQuestions = new PythonQuestion[0];
        Animator anim = instance.GetComponent<Animator>();
        if (anim != null) anim.runtimeAnimatorController = controller;
        SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
        Sprite[] idle = Frames(folder, "idle");
        if (sr != null && idle.Length > 0)
        {
            sr.sprite = idle[0];
            sr.sortingOrder = 5;
        }

        string path = PrefabPath + "/" + folder + "Boss.prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        return saved;
    }

    private static Tile MakeTile(string name, bool solid)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Base + "/Sprites/LabTiles/" + name + ".png");
        if (sprite == null) return null;
        string path = TilePath + "/" + name + ".asset";
        Tile asset = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(asset, path);
        }
        asset.sprite = sprite;
        asset.color = Color.white;
        asset.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static void AddArena(string level, GameObject prefab, Tile floor, Tile wall, Tile raised, Tile monitor, Tile hazard)
    {
        if (!File.Exists(level)) { Debug.LogError("[NightBorne] No scene at " + level); return; }
        Scene scene = EditorSceneManager.OpenScene(level, OpenSceneMode.Single);
        // The arena is normally a child of Grid, NOT a scene root.
        // Search the whole scene to avoid installing duplicate arenas on repeated runs.
        GameObject existingArena = FindSceneObject(scene, "[Stage Boss Arena]");
        if (existingArena != null)
        {
            // Previous installs may have created the arena without adding a visible boss.
            // Repair the missing boss in place rather than skipping the entire scene.
            GameObject existingBoss = FindBossInScene(scene, prefab.name);
            if (existingBoss == null)
            {
                GameObject repairedBoss = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                repairedBoss.name = prefab.name;
                repairedBoss.transform.SetParent(existingArena.transform, true);

                Tilemap arenaFloor = null;
                foreach (Tilemap tm in existingArena.GetComponentsInChildren<Tilemap>(true))
                    if (tm.name == "Lab Arena Ground") { arenaFloor = tm; break; }

                if (arenaFloor != null && arenaFloor.GetUsedTilesCount() > 0)
                {
                    // Put the boss near the right end of the existing lab arena.
                    BoundsInt floorBounds = arenaFloor.cellBounds;
                    int highestFloorY = floorBounds.yMin;
                    int rightmostX = floorBounds.xMin;
                    foreach (Vector3Int cell in floorBounds.allPositionsWithin)
                        if (arenaFloor.HasTile(cell))
                        {
                            if (cell.x > rightmostX) rightmostX = cell.x;
                            if (cell.y > highestFloorY) highestFloorY = cell.y;
                        }
                    Vector3 spawn = arenaFloor.GetCellCenterWorld(
                        new Vector3Int(rightmostX - 7, highestFloorY + 1, 0));
                    repairedBoss.transform.position = spawn + Vector3.up;
                }
                else
                {
                    // Older or partially installed arena: position relative to its anchor.
                    repairedBoss.transform.position = existingArena.transform.position + new Vector3(8f, 3f, 0f);
                    Debug.LogWarning("[NightBorne] No populated Lab Arena Ground in " + scene.name +
                        "; placed the boss near the arena anchor for manual adjustment.");
                }
                EnsureBossVisible(repairedBoss, prefab.name);
                Debug.Log("[NightBorne] Repaired missing " + repairedBoss.name + " in " + scene.name);
            }
            else
            {
                EnsureBossVisible(existingBoss, prefab.name);
                Debug.Log("[NightBorne] Found and checked " + existingBoss.name + " in " + scene.name);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return;
        }

        Tilemap ground = null;
        foreach (Tilemap m in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            if (m.name == "Ground") { ground = m; break; }

        if (ground == null)
        {
            Debug.LogError("[NightBorne] Couldn't find the 'Ground' tilemap in " + scene.name);
            return;
        }

        // Find the last actually painted ground tile (bounds may contain empty cells).
        BoundsInt b = ground.cellBounds;
        int right = b.xMin;
        int top = b.yMin;
        foreach (Vector3Int pos in b.allPositionsWithin)
        {
            if (!ground.HasTile(pos)) continue;
            if (pos.x > right || (pos.x == right && pos.y > top))
            {
                right = pos.x; top = pos.y;
            }
        }

        GameObject arena = new GameObject("[Stage Boss Arena]");
        arena.transform.SetParent(ground.transform.parent, false);
        arena.transform.localPosition = Vector3.zero;
        Tilemap floorMap = MakeTilemap(arena.transform, "Lab Arena Ground", 0);
        // Match the existing ground layer so Player and BanditEnemy ground checks work.
        floorMap.gameObject.layer = ground.gameObject.layer;
        floorMap.gameObject.AddComponent<TilemapCollider2D>();

        Tilemap decoMap = MakeTilemap(arena.transform, "Lab Panels & Machinery", -3);
        // Extend the ground directly from the current right-hand edge, so the
        // player walks naturally from the existing stage into the boss arena.
        for (int x = right + 1; x <= right + 31; x++)
        {
            for (int y = top - 3; y <= top; y++)
                floorMap.SetTile(new Vector3Int(x, y, 0), floor);
            if ((x - right) % 4 == 0)
                decoMap.SetTile(new Vector3Int(x, top + 3, 0), wall);
        }
        for (int x = right + 4; x < right + 29; x += 8)
        {
            decoMap.SetTile(new Vector3Int(x, top + 4, 0), monitor);
            decoMap.SetTile(new Vector3Int(x + 3, top + 4, 0), raised);
            decoMap.SetTile(new Vector3Int(x + 5, top + 2, 0), hazard);
        }

        // Prefab pivot and collider remain independent of the changing animation sprites.
        GameObject boss = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        boss.name = prefab.name;
        boss.transform.SetParent(arena.transform, true);
        Vector3 worldPoint = floorMap.GetCellCenterWorld(new Vector3Int(right + 24, top + 1, 0));
        boss.transform.position = new Vector3(worldPoint.x, worldPoint.y + 1f, 0f);
        EnsureBossVisible(boss, prefab.name);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[NightBorne] Added " + boss.name + " and lab arena to " + scene.name);
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == objectName) return t.gameObject;
        return null;
    }

    private static GameObject FindBossInScene(Scene scene, string prefabName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == prefabName) return t.gameObject;
                BanditEnemy enemy = t.GetComponent<BanditEnemy>();
                if (enemy != null && enemy.isStageBoss)
                {
                    // Detect older bosses that were renamed by the player.
                    string expected = prefabName.StartsWith("NightBorne") ? "NightBorne" : "Magitech";
                    if (t.name.IndexOf(expected, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return t.gameObject;
                }
            }
        return null;
    }

    private static void EnsureBossVisible(GameObject boss, string prefabName)
    {
        boss.SetActive(true);
        SpriteRenderer sr = boss.GetComponent<SpriteRenderer>();
        if (sr == null) sr = boss.GetComponentInChildren<SpriteRenderer>(true);
        if (sr == null)
        {
            sr = boss.AddComponent<SpriteRenderer>();
        }
        sr.enabled = true;
        sr.color = Color.white;
        sr.sortingOrder = 5;

        string character = prefabName.StartsWith("NightBorne") ? "NightBorne" : "Magitech";
        Sprite[] idle = Frames(character, "idle");
        if (sr.sprite == null && idle.Length > 0)
            sr.sprite = idle[0];
        if (idle.Length == 0)
            Debug.LogError("[NightBorne] No imported " + character + " idle sprites. " +
                "Check Assets/StageBosses/Sprites/" + character + "/idle");

        Animator anim = boss.GetComponent<Animator>();
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            AnimationPath + "/" + character + ".controller");
        if (anim != null && anim.runtimeAnimatorController == null && controller != null)
            anim.runtimeAnimatorController = controller;

        EditorUtility.SetDirty(boss);
        EditorUtility.SetDirty(sr);
    }

    private static Tilemap MakeTilemap(Transform parent, string name, int sortingOrder)
    {
        GameObject obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        obj.transform.SetParent(parent, false);
        TilemapRenderer renderer = obj.GetComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        return obj.GetComponent<Tilemap>();
    }
}

// Import settings for the artwork shipped with this patch.
// If you add new PNG frames later, Unity picks them up with the same pixel-art rules.
public class StageBossSpriteImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/StageBosses/Sprites/")) return;
        TextureImporter tex = (TextureImporter)assetImporter;
        tex.textureType = TextureImporterType.Sprite;
        tex.spriteImportMode = SpriteImportMode.Single;
        tex.textureCompression = TextureImporterCompression.Uncompressed;
        tex.filterMode = FilterMode.Point;
        tex.mipmapEnabled = false;
        tex.alphaIsTransparency = true;
        // Custom sprite alignment belongs to TextureImporterSettings, not TextureImporter.
        // This preserves the non-central pivots configured below for boss animations.
        TextureImporterSettings settings = new TextureImporterSettings();
        tex.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        tex.SetTextureSettings(settings);
        if (assetPath.Contains("/NightBorne/"))
        {
            tex.spritePixelsPerUnit = 64f;
            tex.spritePivot = new Vector2(0.5f, 0.12f);
        }
        else if (assetPath.Contains("/Magitech/"))
        {
            tex.spritePixelsPerUnit = 145f;
            // Baseline of the Magitech sprites is ~Y490 in a 682px-high image.
            tex.spritePivot = new Vector2(0.5f, 0.28f);
        }
        else
        {
            tex.spritePixelsPerUnit = 256f;
            tex.spritePivot = new Vector2(0.5f, 0.5f);
        }
    }
}
#endif
