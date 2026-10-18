using UnityEngine;
using UnityEngine.SceneManagement;

// Makes sure there's always an AudioManager, so music plays in every scene, including the
// Main Menu (which has no AudioManager object of its own). The prefab to use is set in
// Assets/Resources/AudioManagerLoader. If a scene already has one, nothing changes.
[CreateAssetMenu(fileName = "AudioManagerLoader", menuName = "Audio/Audio Manager Loader")]
public class AudioBootstrap : ScriptableObject
{
    public GameObject audioManagerPrefab;

    private static bool subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (!subscribed)
        {
            SceneManager.sceneLoaded += (s, m) => Ensure();
            subscribed = true;
        }
        Ensure();
    }

    private static void Ensure()
    {
        if (AudioManager.instance != null || Object.FindAnyObjectByType<AudioManager>() != null) return;
        AudioBootstrap loader = Resources.Load<AudioBootstrap>("AudioManagerLoader");
        if (loader != null && loader.audioManagerPrefab != null) Object.Instantiate(loader.audioManagerPrefab);
    }
}
