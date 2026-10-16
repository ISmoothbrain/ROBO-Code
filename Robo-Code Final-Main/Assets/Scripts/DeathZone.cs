using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Shows a message and restarts the level if the player falls off the bottom.
// Put this on an empty GameObject in the scene.
public class DeathZone : MonoBehaviour
{
    public Transform player;
    public GameObject deathMessage;   // the "YOU DIED" text, left switched off to start

    [Tooltip("Falling below this height counts as dying. Put it a few units under your lowest ground.")]
    public float deathHeight = -15f;
    public float waitBeforeRestart = 2f;

    private bool dead;

    void Start()
    {
        // hide the message at the start in case it was left on in the scene
        if (deathMessage != null)
        {
            deathMessage.SetActive(false);
        }
    }

    void Update()
    {
        // the bool stops this firing over and over while the player keeps falling
        if (dead || player == null)
        {
            return;
        }

        if (player.position.y < deathHeight)
        {
            dead = true;
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die()
    {
        if (deathMessage != null)
        {
            deathMessage.SetActive(true);
        }

        // real time, so the wait still counts down even if something paused the game
        yield return new WaitForSecondsRealtime(waitBeforeRestart);

        // the pause menu sets this to 0, so put it back or the level loads frozen
        Time.timeScale = 1f;

        // loading the scene that is already open just starts it over, and asking
        // for the active scene means this works in any level without changing it
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}