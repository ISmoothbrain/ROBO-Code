using UnityEngine;
using UnityEngine.Rendering;

// A layered city skyline behind the level. Each layer repeats left and right forever and moves
// at its own speed as the camera moves (parallax), so it feels deep. Clouds and fog also drift.
// Created automatically when a level with a Player loads (see BackgroundSettings to change it).
public class ParallaxBackground : MonoBehaviour
{
    private const float PixelsPerUnit = 32f;

    private class Layer
    {
        public string name;
        public SpriteRenderer[] tiles;
        public float width;
        public float drift;
    }

    public BackgroundSettings settings;
    [Tooltip("Set automatically when the scene has a Scene Background component.")]
    public SceneBackground sceneOverride;
    private bool usingCopy;

    private Layer[] layers;
    private BackgroundTheme builtTheme;
    private float builtScale;

    private static readonly string[] LayerNames = { "sky", "clouds", "far", "fog", "mid", "near" };

    private void OnEnable() => RenderPipelineManager.beginCameraRendering += OnBeginCamera;
    private void OnDisable() => RenderPipelineManager.beginCameraRendering -= OnBeginCamera;

    private void Start() => Build();

    private void Build()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        layers = null;
        if (settings == null) return;
        builtTheme = settings.theme;
        builtScale = settings.scale;
        if (settings.theme == BackgroundTheme.None) return;

        // Draw behind everything: the bottom-most sorting layer, very low order.
        int sortingLayer = SortingLayer.layers.Length > 0 ? SortingLayer.layers[0].id : 0;
        Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        Material unlitMat = unlit != null ? new Material(unlit) : null; // so scene lights don't darken the sky

        layers = new Layer[LayerNames.Length];
        for (int i = 0; i < LayerNames.Length; i++)
        {
            Sprite sprite = Resources.Load<Sprite>($"Backgrounds/{settings.theme}_{LayerNames[i]}");
            if (sprite == null) continue;

            Layer layer = new Layer { name = LayerNames[i], tiles = new SpriteRenderer[3], width = sprite.bounds.size.x * settings.scale };
            GameObject holder = new GameObject(LayerNames[i]);
            holder.transform.SetParent(transform, false);

            for (int t = 0; t < 3; t++)
            {
                GameObject go = new GameObject($"{LayerNames[i]}_{t}");
                go.transform.SetParent(holder.transform, false);
                go.transform.localScale = Vector3.one * settings.scale;
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerID = sortingLayer;
                sr.sortingOrder = -1000 + i;
                if (unlitMat != null) sr.sharedMaterial = unlitMat;
                layer.tiles[t] = sr;
            }
            layers[i] = layer;
        }
    }

    private void OnBeginCamera(ScriptableRenderContext context, Camera cam)
    {
        if (cam == Camera.main) Position(cam);
    }

    // Clouds and fog drift on their own, once per frame.
    private void Update()
    {
        if (layers == null || settings == null) return;
        foreach (Layer layer in layers)
        {
            if (layer == null) continue;
            if (layer.name == "clouds") layer.drift = (layer.drift + settings.cloudDrift * Time.deltaTime) % layer.width;
            if (layer.name == "fog") layer.drift = (layer.drift + settings.fogDrift * Time.deltaTime) % layer.width;
        }
    }

    private void LateUpdate()
    {
        // This scene's own choice wins over the project-wide default (and can be changed live).
        if (sceneOverride != null && settings != null)
        {
            settings.theme = sceneOverride.theme;
            settings.verticalOffset = sceneOverride.verticalOffset;
            settings.scale = sceneOverride.scale;
        }
        // The background picked in the Settings menu wins over everything.
        int choice = GameSettings.BackgroundChoice;
        if (choice >= 0 && settings != null)
        {
            if (!usingCopy) { settings = Instantiate(settings); usingCopy = true; } // never change the shared file
            settings.theme = (BackgroundTheme)choice;
        }
        if (settings != null && (settings.theme != builtTheme || !Mathf.Approximately(settings.scale, builtScale))) Build();
        if (Camera.main != null) Position(Camera.main);
    }

    private void Position(Camera cam)
    {
        if (layers == null || settings == null) return;

        Vector3 c = cam.transform.position;

        for (int i = 0; i < layers.Length; i++)
        {
            Layer layer = layers[i];
            if (layer == null) continue;

            float parallax = Factor(i);

            // Where this layer's pattern starts, then the 3 copies around the camera.
            float origin = c.x * parallax + layer.drift;
            float k = Mathf.Round((c.x - origin) / layer.width);
            float y = Snap(c.y + settings.verticalOffset);

            for (int t = 0; t < 3; t++)
            {
                float x = Snap(origin + (k + t - 1) * layer.width);
                layer.tiles[t].transform.position = new Vector3(x, y, 50f);
            }
        }
    }

    private float Factor(int i)
    {
        switch (i)
        {
            case 0: return settings.sky;
            case 1: return settings.clouds;
            case 2: return settings.farBuildings;
            case 3: return settings.fog;
            case 4: return settings.midBuildings;
            default: return settings.nearBuildings;
        }
    }

    // Line layers up with the pixel grid so the pixel art doesn't shimmer as it scrolls.
    private float Snap(float v)
    {
        float ppu = PixelsPerUnit / Mathf.Max(0.01f, settings.scale);
        return Mathf.Round(v * ppu) / ppu;
    }
}
