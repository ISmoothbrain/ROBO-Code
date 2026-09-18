using UnityEngine;
using UnityEngine.UI;

// Put this on SettingsPanel alongside VolumeSettings. Wire redSlider/greenSlider/
// blueSlider's OnValueChanged (float) events to OnSliderChanged.
public class HealthBarColorSettings : MonoBehaviour
{
    public Slider redSlider;
    public Slider greenSlider;
    public Slider blueSlider;
    public Image previewSwatch; // optional - shows the colour live as you drag

    void Start()
    {
        Color current = HealthBarSettings.CurrentColor;

        if (redSlider != null) redSlider.value = current.r;
        if (greenSlider != null) greenSlider.value = current.g;
        if (blueSlider != null) blueSlider.value = current.b;

        UpdatePreview(current);
    }

    // Hook all three sliders to this same method - it just reads whatever the
    // three of them currently say, so it doesn't matter which one fired.
    public void OnSliderChanged(float _)
    {
        Color color = new Color(
            redSlider != null ? redSlider.value : 1f,
            greenSlider != null ? greenSlider.value : 0f,
            blueSlider != null ? blueSlider.value : 0f);

        HealthBarSettings.SetColor(color);
        UpdatePreview(color);
    }

    private void UpdatePreview(Color color)
    {
        if (previewSwatch != null)
        {
            previewSwatch.color = color;
        }
    }
}
