using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    // other scripts reach the audio through this instead of needing a reference
    public static AudioManager instance;

    public AudioSource musicSource;
    public AudioSource sfxSource;

    public AudioClip menuMusic;
    public AudioClip gameMusic;
    public AudioClip battleMusic;

    public AudioClip buttonClick;
    public AudioClip buttonHover;
    public AudioClip buttonBack;
    public AudioClip startGame;

    public AudioClip attack;
    public AudioClip heavyAttack;
    public AudioClip enemyAttack;
    public AudioClip defend;
    public AudioClip hurt;
    public AudioClip heal;
    public AudioClip coin;
    public AudioClip enemyDefeated;
    public AudioClip battleStart;
    public AudioClip victory;
    public AudioClip defeat;
    public AudioClip levelUp;

    // several takes of the same step. hearing one clip over and over while
    // walking sounds fake, so these get shuffled through instead
    public AudioClip[] footsteps;

    private int lastFootstep = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance == null) new GameObject("[Audio Manager]").AddComponent<AudioManager>();
    }

    void Awake()
    {
        // there is one in each scene so the game works from either starting point,
        // so any extra copy destroys itself and the first one carries on
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureSources();
        LoadBundledClips();
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // start at whatever volume the player last saved in the settings menu
        musicSource.loop = true;
        musicSource.volume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    private void EnsureSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        if (musicSource == null)
        {
            musicSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        }
        if (sfxSource == null || sfxSource == musicSource)
        {
            sfxSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        }
        musicSource.playOnAwake = false;
        sfxSource.playOnAwake = false;
    }

    private void LoadBundledClips()
    {
        if (menuMusic == null) menuMusic = Resources.Load<AudioClip>("Audio/MenuMusic");
        if (gameMusic == null) gameMusic = Resources.Load<AudioClip>("Audio/GameMusic");
        if (battleMusic == null) battleMusic = Resources.Load<AudioClip>("Audio/BattleMusic");
        if (buttonClick == null) buttonClick = Resources.Load<AudioClip>("Audio/ButtonClick");
        if (buttonHover == null) buttonHover = Resources.Load<AudioClip>("Audio/ButtonHover");
        if (buttonBack == null) buttonBack = Resources.Load<AudioClip>("Audio/ButtonBack");
        if (startGame == null) startGame = Resources.Load<AudioClip>("Audio/StartGame");
        if (attack == null) attack = Resources.Load<AudioClip>("Audio/Attack");
        if (heavyAttack == null) heavyAttack = Resources.Load<AudioClip>("Audio/HeavyAttack");
        if (enemyAttack == null) enemyAttack = Resources.Load<AudioClip>("Audio/EnemyAttack");
        if (defend == null) defend = Resources.Load<AudioClip>("Audio/Defend");
        if (hurt == null) hurt = Resources.Load<AudioClip>("Audio/Hurt");
        if (heal == null) heal = Resources.Load<AudioClip>("Audio/Heal");
        if (coin == null) coin = Resources.Load<AudioClip>("Audio/Coin");
        if (enemyDefeated == null) enemyDefeated = Resources.Load<AudioClip>("Audio/EnemyDefeated");
        if (battleStart == null) battleStart = Resources.Load<AudioClip>("Audio/BattleStart");
        if (victory == null) victory = Resources.Load<AudioClip>("Audio/Victory");
        if (defeat == null) defeat = Resources.Load<AudioClip>("Audio/Defeat");
        if (levelUp == null) levelUp = Resources.Load<AudioClip>("Audio/LevelUp");
        if (footsteps == null || footsteps.Length == 0)
        {
            footsteps = new AudioClip[5];
            for (int i = 0; i < footsteps.Length; i++)
                footsteps[i] = Resources.Load<AudioClip>($"Audio/Footstep-{i + 1}");
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void Start()
    {
        // sceneLoaded does not fire for the scene we started in, so handle that here
        SetUpScene(SceneManager.GetActiveScene().name);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetUpScene(scene.name);
    }

    void SetUpScene(string sceneName)
    {
        if (sceneName == "Main Menu")
        {
            PlayMusic(menuMusic);
        }
        else
        {
            PlayMusic(gameMusic);
        }

        AddClickSounds();
    }

    // gives every button a click sound without having to wire each one by hand.
    // inactive ones are included so the settings and keybind panels are covered
    void AddClickSounds()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].onClick.RemoveListener(PlayClick);
            buttons[i].onClick.AddListener(PlayClick);
        }
    }

    void PlayClick()
    {
        PlaySFX(buttonClick);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null)
        {
            return;
        }

        // without this the track restarts from the beginning every scene change
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }

        // PlayOneShot lets sounds overlap, so a hit and a coin can play together
        sfxSource.PlayOneShot(clip);
    }

    // picks a different step from the last one, at a slightly random volume,
    // so a run of footsteps never sounds like the same sample repeating
    public void PlayFootstep()
    {
        if (footsteps == null || footsteps.Length == 0)
        {
            return;
        }

        int next = Random.Range(0, footsteps.Length);
        if (footsteps.Length > 1 && next == lastFootstep)
        {
            next = (next + 1) % footsteps.Length;
        }
        lastFootstep = next;

        if (footsteps[next] != null)
        {
            sfxSource.PlayOneShot(footsteps[next], Random.Range(0.75f, 1f));
        }
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null) musicSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null) sfxSource.volume = volume;
    }
}
