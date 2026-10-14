#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Text;

// IMPORTANT: this file must live in a folder called "Editor"
// (e.g. Assets/Editor/AnimatorStateTools.cs). Unity will not compile it anywhere else.
//
// Select the Player (in the Hierarchy OR the prefab in the Project window), then use
// the Tools > Animator menu.
public static class AnimatorStateTools
{
    private static readonly string[] AttackStateNames = { "Attack1", "Attack2", "Attack3" };

    // ---------------------------------------------------------------- diagnose

    [MenuItem("Tools/Animator/Dump States Of Selected")]
    private static void DumpStates()
    {
        AnimatorController ac = GetControllerFromSelection(out string sourceName);
        if (ac == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Animator Controller used by '{sourceName}':");
        sb.AppendLine($"  Asset: {AssetDatabase.GetAssetPath(ac)}");
        sb.AppendLine($"  Layers: {ac.layers.Length}");

        for (int i = 0; i < ac.layers.Length; i++)
        {
            AnimatorControllerLayer layer = ac.layers[i];
            sb.AppendLine($"\n  --- Layer {i}: \"{layer.name}\" ---");
            AppendStates(layer.stateMachine, "", sb);
        }

        sb.AppendLine("\nThe names above are what animator.Play() needs. If a state sits inside a");
        sb.AppendLine("sub-state machine, use the full dotted path shown (e.g. \"Combat.Attack1\").");

        Debug.Log(sb.ToString());
    }

    private static void AppendStates(AnimatorStateMachine sm, string path, StringBuilder sb)
    {
        foreach (ChildAnimatorState child in sm.states)
        {
            AnimationClip clip = child.state.motion as AnimationClip;
            string clipName = clip != null ? clip.name : "<no clip>";
            float length = clip != null ? clip.length : 0f;
            sb.AppendLine($"    state: \"{path}{child.state.name}\"   clip: {clipName} ({length:F2}s)");

            if (clip != null)
            {
                AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
                if (events.Length == 0)
                {
                    sb.AppendLine("        (no animation events on this clip)");
                }
                foreach (AnimationEvent e in events)
                {
                    sb.AppendLine($"        event: {e.functionName}() at {e.time:F3}s");
                }
            }
        }

        foreach (ChildAnimatorStateMachine sub in sm.stateMachines)
        {
            sb.AppendLine($"    [sub-state machine: {sub.stateMachine.name}]");
            AppendStates(sub.stateMachine, path + sub.stateMachine.name + ".", sb);
        }
    }

    // ---------------------------------------------------------------- repair

    [MenuItem("Tools/Animator/Create Missing Attack States")]
    private static void CreateMissingAttackStates()
    {
        AnimatorController ac = GetControllerFromSelection(out string sourceName);
        if (ac == null) return;

        AnimatorStateMachine root = ac.layers[0].stateMachine;
        HashSet<string> existing = new HashSet<string>();
        CollectStateNames(root, existing);

        int created = 0;
        Vector3 placeAt = new Vector3(400f, 0f, 0f);

        foreach (string stateName in AttackStateNames)
        {
            if (existing.Contains(stateName))
            {
                Debug.Log($"State \"{stateName}\" already exists on layer 0 — skipping.");
                continue;
            }

            AnimationClip clip = FindClip(stateName);
            if (clip == null)
            {
                Debug.LogError($"No AnimationClip found for \"{stateName}\". " +
                               $"Make sure a clip with that name exists somewhere in Assets/.");
                continue;
            }

            AnimatorState state = root.AddState(stateName, placeAt);
            state.motion = clip;
            state.writeDefaultValues = false;
            placeAt += new Vector3(0f, 70f, 0f);
            created++;

            Debug.Log($"Created state \"{stateName}\" on layer 0 using clip '{clip.name}'.");
        }

        if (created > 0)
        {
            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
            Debug.Log($"Added {created} state(s) to {AssetDatabase.GetAssetPath(ac)} for '{sourceName}'. " +
                      $"No transitions were added — Player.cs drives these with animator.Play(), " +
                      $"so standalone states are exactly what it wants.");
        }
    }

    [MenuItem("Tools/Animator/Add Attack Animation Events")]
    private static void AddAttackEvents()
    {
        foreach (string stateName in AttackStateNames)
        {
            AnimationClip clip = FindClip(stateName);
            if (clip == null)
            {
                Debug.LogError($"No AnimationClip named \"{stateName}\" found.");
                continue;
            }

            List<AnimationEvent> events = new List<AnimationEvent>();
            foreach (AnimationEvent e in AnimationUtility.GetAnimationEvents(clip))
            {
                // Drop any old copies so running this twice doesn't stack duplicates.
                if (e.functionName == "DealDamage" || e.functionName == "OnAttackAnimationEnd") continue;
                events.Add(e);
            }

            events.Add(new AnimationEvent
            {
                functionName = "DealDamage",
                time = clip.length * 0.5f // roughly mid-swing; nudge it in the Animation window to taste
            });

            events.Add(new AnimationEvent
            {
                functionName = "OnAttackAnimationEnd",
                time = Mathf.Max(clip.length - 0.01f, 0f) // last frame, never frame 0
            });

            AnimationUtility.SetAnimationEvents(clip, events.ToArray());
            EditorUtility.SetDirty(clip);

            Debug.Log($"'{clip.name}': DealDamage at {clip.length * 0.5f:F3}s, " +
                      $"OnAttackAnimationEnd at {Mathf.Max(clip.length - 0.01f, 0f):F3}s.");
        }

        AssetDatabase.SaveAssets();
    }

    // ---------------------------------------------------------------- helpers

    private static void CollectStateNames(AnimatorStateMachine sm, HashSet<string> into)
    {
        foreach (ChildAnimatorState child in sm.states) into.Add(child.state.name);
        foreach (ChildAnimatorStateMachine sub in sm.stateMachines) CollectStateNames(sub.stateMachine, into);
    }

    private static AnimationClip FindClip(string exactName)
    {
        string[] guids = AssetDatabase.FindAssets($"t:AnimationClip {exactName}");
        AnimationClip fuzzy = null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) continue;
            if (clip.name == exactName) return clip;
            if (fuzzy == null) fuzzy = clip;
        }

        return fuzzy;
    }

    private static AnimatorController GetControllerFromSelection(out string sourceName)
    {
        sourceName = "<nothing>";
        Object sel = Selection.activeObject;

        if (sel == null)
        {
            Debug.LogError("Select the Player GameObject (or its prefab, or an Animator Controller) first.");
            return null;
        }

        sourceName = sel.name;

        if (sel is AnimatorController direct) return direct;

        GameObject go = sel as GameObject;
        if (go == null)
        {
            Debug.LogError($"'{sel.name}' is not a GameObject or an Animator Controller.");
            return null;
        }

        Animator animator = go.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError($"'{go.name}' has no Animator component.");
            return null;
        }

        RuntimeAnimatorController rac = animator.runtimeAnimatorController;
        if (rac == null)
        {
            Debug.LogError($"'{go.name}' has an Animator with NO controller assigned. " +
                           $"That alone would explain every missing state.");
            return null;
        }

        // An Override Controller wraps a real one; state names come from the base.
        while (rac is AnimatorOverrideController ovr) rac = ovr.runtimeAnimatorController;

        AnimatorController ac = rac as AnimatorController;
        if (ac == null)
        {
            Debug.LogError($"Could not resolve '{rac.name}' to an editable AnimatorController.");
        }

        return ac;
    }
}
#endif
