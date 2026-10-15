#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Must live in a folder named "Editor". Adds: Tools > Hooded Hero
// Swaps ONLY the Player's sprite and animations, so anything else you've
// changed on the Player prefab (attack point, scripts, settings) is kept.
public static class HoodedHeroTools
{
    private const string HeroController = "Assets/Resources/PlayerCharacter.overrideController";
    private const string HeroSheet = "Assets/Arts & Sprites/HoodedHero/HoodedHero_Sheet.png";
    private const string OldController = "Assets/Arts & Sprites/Adventurer-1.5/Animation/Hero.controller";
    private const string OldSheet = "Assets/Arts & Sprites/Adventurer-1.5/adventurer-sheet-4x.png";

    [MenuItem("Tools/Hooded Hero/Use Hooded Hero For Player")]
    private static void UseHoodedHero() => Apply(HeroController, HeroSheet, "idle_0");

    [MenuItem("Tools/Hooded Hero/Switch Back To Adventurer")]
    private static void UseAdventurer() => Apply(OldController, OldSheet, "adventurer-sheet-4x_0");

    private static void Apply(string controllerPath, string sheetPath, string spriteName)
    {
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().FirstOrDefault(s => s.name == spriteName);
        if (controller == null || sprite == null)
        {
            Debug.LogError($"Couldn't find {(controller == null ? controllerPath : sheetPath)}. Was it imported?");
            return;
        }

        int prefabs = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.GetComponent<Player>() == null) continue;

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            SetLook(contents, controller, sprite);
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
            prefabs++;
        }

        // Also fix the Player in the open scene, in case it overrides the prefab's look.
        foreach (Player p in Object.FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Animator a = p.GetComponent<Animator>();
            SpriteRenderer r = p.GetComponent<SpriteRenderer>();
            if (a != null) Undo.RecordObject(a, "Change Player look");
            if (r != null) Undo.RecordObject(r, "Change Player look");
            SetLook(p.gameObject, controller, sprite);
            EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Player now uses {controller.name} ({prefabs} prefab(s) updated). Save the scene to keep it.");
    }

    private static void SetLook(GameObject go, RuntimeAnimatorController controller, Sprite sprite)
    {
        Animator a = go.GetComponent<Animator>();
        SpriteRenderer r = go.GetComponent<SpriteRenderer>();
        if (a != null) a.runtimeAnimatorController = controller;
        if (r != null) r.sprite = sprite;
    }
}
#endif
