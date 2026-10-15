using UnityEngine;

// Loops a list of sprites on a SpriteRenderer, e.g. the shop's glowing sign.
// Simpler than setting up an Animator for a looping effect.
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFrameAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float framesPerSecond = 8f;

    private SpriteRenderer sr;
    private float timer;
    private int index;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || framesPerSecond <= 0f) return;

        timer += Time.deltaTime;
        float frameTime = 1f / framesPerSecond;
        while (timer >= frameTime)
        {
            timer -= frameTime;
            index = (index + 1) % frames.Length;
            sr.sprite = frames[index];
        }
    }
}
