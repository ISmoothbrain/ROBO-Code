using System.Collections;
using UnityEngine;

// Plays the player's death frames. The Player's Animator has no Death state, so this
// swaps the sprite directly, then Player reloads the level once it has finished.
// Frames come from Assets/Resources/PlayerDeath.png unless you assign your own.
public class PlayerDeathAnimation : MonoBehaviour
{
    public Sprite[] frames;
    public float framesPerSecond = 12f;

    public float Duration => frames == null ? 0f : frames.Length / Mathf.Max(1f, framesPerSecond);

    // Gets (or adds) the component and plays it. Returns how long it will take, or 0 if there are no frames.
    public static float PlayOn(GameObject target)
    {
        PlayerDeathAnimation d = target.GetComponent<PlayerDeathAnimation>();
        if (d == null) d = target.AddComponent<PlayerDeathAnimation>();
        return d.Play() ? d.Duration : 0f;
    }

    public bool Play()
    {
        if (frames == null || frames.Length == 0)
        {
            frames = Resources.LoadAll<Sprite>("PlayerDeath");
            System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name)); // death_00, death_01, ...
        }
        if (frames.Length == 0) return false;

        Animator animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = false; // stop it overwriting our frames
        StartCoroutine(Run());
        return true;
    }

    private IEnumerator Run()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        foreach (Sprite f in frames)
        {
            if (sr != null)
            {
                sr.sprite = f;
                sr.color = Color.white; // don't stay red from the last hit flash
            }
            yield return new WaitForSeconds(1f / Mathf.Max(1f, framesPerSecond));
        }
    }
}
