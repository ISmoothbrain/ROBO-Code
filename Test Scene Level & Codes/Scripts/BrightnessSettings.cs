using UnityEngine;
using UnityEngine.UI;

public class BrightnessSettings : MonoBehaviour
{
    public Slider brightnessSlider;
    public Image brightnessOverlay;
    public float maxDim = 0.8f;

    private const string PrefsKey = "BrightnessLevel";

    private void Start()
    {
        float saved = PlayerPrefs.GetFloat(PrefsKey, 1f); // default: full brightness

        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = 0f;
            brightnessSlider.maxValue = 1f;
            brightnessSlider.value = saved;
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }

        SetBrightness(saved);
    }

    public void SetBrightness(float value)
    {
        if (brightnessOverlay != null)
        {
            // value = 0 -> overlay alpha = maxDim (darkest)
            // value = 1 -> overlay alpha = 0 (brightest, no dimming)
            float alpha = Mathf.Lerp(maxDim, 0f, Mathf.Clamp01(value));
            Color c = brightnessOverlay.color;
            c.a = alpha;
            brightnessOverlay.color = c;
        }

        PlayerPrefs.SetFloat(PrefsKey, value);
    }

    private void OnDestroy()
    {
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.RemoveListener(SetBrightness);
    }
}