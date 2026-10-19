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

    // True while the spawn (reverse death) is playing; the player can't move until it finishes.
    public static bool Spawning { get; private set; }

    // Spawning: the death animation played backwards (the code comes together into the hero).
    // delay: seconds to stay invisible first (e.g. while the screen is still fading in).
    public static float PlaySpawnOn(GameObject target, float delay = 0f)
    {
        PlayerDeathAnimation d = target.GetComponent<PlayerDeathAnimation>();
        if (d == null) d = target.AddComponent<PlayerDeathAnimation>();
        return d.PlayReverse(delay) ? d.Duration + delay : 0f;
    }

    private void LoadFrames()
    {
        if (frames != null && frames.Length > 0) return;
        frames = Resources.LoadAll<Sprite>("PlayerDeath");
        System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name)); // death_00, death_01, ...
    }

    public bool PlayReverse(float delay = 0f)
    {
        LoadFrames();
        if (frames.Length == 0) return false;
        Animator animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        StartCoroutine(RunReverse(animator, delay));
        return true;
    }

    private IEnumerator RunReverse(Animator animator, float delay)
    {
        Spawning = true;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (delay > 0f)
        {
            if (sr != null) sr.sprite = frames[frames.Length - 1]; // the empty last frame: not there yet
            yield return new WaitForSecondsRealtime(delay);
        }
        for (int i = frames.Length - 1; i >= 0; i--)
        {
            if (sr != null) { sr.sprite = frames[i]; sr.color = Color.white; }
            yield return new WaitForSeconds(1f / Mathf.Max(1f, framesPerSecond));
        }
        if (animator != null) animator.enabled = true; // back to normal animations
        Spawning = false;
    }

    private void OnDestroy()
    {
        Spawning = false; // never leave the player frozen if the scene changes mid-spawn
    }

    public bool Play()
    {
        LoadFrames();
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
