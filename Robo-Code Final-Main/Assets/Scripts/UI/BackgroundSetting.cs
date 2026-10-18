using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Settings option: which background the levels use. Put this on a Button: each click picks the
// next one (Level default, Night, Dusk, Industrial, Cyberpunk). It shows the name on the button's
// text, or on Value Text if you set one. "Level default" = each scene's own background.
public class BackgroundSetting : MonoBehaviour
{
    public TMP_Text valueText;

    private static readonly int[] Choices = { -1, (int)BackgroundTheme.Night, (int)BackgroundTheme.Dusk,
                                              (int)BackgroundTheme.Industrial, (int)BackgroundTheme.Cyberpunk };
    private static readonly string[] Names = { "Level default", "Night", "Dusk", "Industrial", "Cyberpunk" };

    private void Awake()
    {
        if (valueText == null) valueText = GetComponentInChildren<TMP_Text>(true);
        Button b = GetComponent<Button>();
        if (b != null) b.onClick.AddListener(Next);
        Show();
    }

    public void Next() => Step(1);
    public void Previous() => Step(-1);

    private void Step(int dir)
    {
        int i = System.Array.IndexOf(Choices, GameSettings.BackgroundChoice);
        if (i < 0) i = 0;
        i = (i + dir + Choices.Length) % Choices.Length;
        GameSettings.BackgroundChoice = Choices[i];
        Show();
    }

    private void Show()
    {
        int i = Mathf.Max(0, System.Array.IndexOf(Choices, GameSettings.BackgroundChoice));
        if (valueText != null) valueText.text = Names[i];
    }
}
