using UnityEngine;

// Add this to any GameObject in a scene to choose THAT scene's background.
// Scenes without one use the default in Assets/Resources/BackgroundSettings.
// You can change these while playing; parallax speeds and drift still come from BackgroundSettings.
[AddComponentMenu("Background/Scene Background")]
public class SceneBackground : MonoBehaviour
{
    [Tooltip("This scene's skyline. None = no background in this scene.")]
    public BackgroundTheme theme = BackgroundTheme.Night;

    [Tooltip("Moves this scene's skyline up (positive) or down (negative), in world units.")]
    public float verticalOffset = 0f;

    [Tooltip("Makes this scene's background art bigger or smaller.")]
    public float scale = 1f;
}
