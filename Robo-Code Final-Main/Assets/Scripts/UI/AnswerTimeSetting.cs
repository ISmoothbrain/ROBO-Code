using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Settings option: how many seconds you get to answer (debug) a battle question.
// Put this on your Answer Time slider (or on its parent). It sets the slider's range itself.
// Optional: drag a TextMeshPro text into Value Text to show "25 s".
public class AnswerTimeSetting : MonoBehaviour
{
    public Slider slider;
    public TMP_Text valueText;
    public float minSeconds = 10f;
    public float maxSeconds = 60f;

    private void Awake()
    {
        if (slider == null) slider = GetComponentInChildren<Slider>(true);
        if (slider == null) { Debug.LogWarning("AnswerTimeSetting: no Slider found.", this); return; }

        float current = GameSettings.AnswerTime;
        if (current <= 0f)
        {
            BattleSettings bs = BattleSettings.Load();
            current = bs != null ? bs.answerTimeLimit : 20f;
        }
        slider.minValue = minSeconds;
        slider.maxValue = maxSeconds;
        slider.wholeNumbers = true;
        slider.SetValueWithoutNotify(Mathf.Clamp(current, minSeconds, maxSeconds));
        slider.onValueChanged.AddListener(Set);
        Show(slider.value);
    }

    public void Set(float seconds)
    {
        GameSettings.AnswerTime = seconds;
        Show(seconds);
    }

    private void Show(float seconds)
    {
        if (valueText != null) valueText.text = Mathf.RoundToInt(seconds) + " s";
    }
}
