using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Adds Continue/New Game and the two new settings without requiring the supplied
// Main Menu scene to be rebuilt by hand.
public class MainMenuRuntimeUI : MonoBehaviour
{
    private MainMenu menu;
    private TextMeshProUGUI debugValue;
    private TextMeshProUGUI backgroundValue;

    public static void Build(MainMenu menu, GameObject settingsPanel)
    {
        if (menu == null || menu.GetComponent<MainMenuRuntimeUI>() != null) return;
        MainMenuRuntimeUI ui = menu.gameObject.AddComponent<MainMenuRuntimeUI>();
        ui.menu = menu;
        ui.BuildButtons();
        if (settingsPanel != null) ui.BuildSettings(settingsPanel.transform);
    }

    private void BuildButtons()
    {
        GameObject playObject = GameObject.Find("Play");
        if (playObject == null) return;
        Button continueButton = playObject.GetComponent<Button>();
        RectTransform playRect = playObject.GetComponent<RectTransform>();
        Transform parent = playObject.transform.parent;

        // The original START text is baked into its sprite. Replace it with the
        // same in-game panel texture so Continue can have live text.
        Image image = playObject.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = Resources.Load<Sprite>("UI/panel");
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = new Color(0.09f, 0.16f, 0.23f, 1f);
        }
        TextMeshProUGUI continueLabel = AddButtonLabel(playObject, "CONTINUE");
        continueButton.interactable = LevelProgress.HasProgress();
        continueLabel.color = continueButton.interactable ? Color.white : new Color(0.55f, 0.6f, 0.66f);

        Button newGame = UIBuild.Button(parent, "NewGame", "NEW GAME", 18, playRect.rect.height);
        RectTransform nr = newGame.GetComponent<RectTransform>();
        CopyRect(playRect, nr);
        nr.anchoredPosition = playRect.anchoredPosition + new Vector2(0f, -43f);
        newGame.onClick.AddListener(menu.NewGame);
        Image newImage = newGame.GetComponent<Image>();
        newImage.sprite = Resources.Load<Sprite>("UI/panel");
        newImage.type = newImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        newImage.color = new Color(0.09f, 0.16f, 0.23f, 1f);

        MoveButton("Settings", -43f);
        MoveButton("Quit", -43f);
    }

    private static TextMeshProUGUI AddButtonLabel(GameObject button, string text)
    {
        GameObject label = UIBuild.Node("RuntimeLabel", button.transform);
        UIBuild.Stretch(label, 0, 0, 1, 1);
        TextMeshProUGUI tmp = UIBuild.Label(label, 18, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        tmp.text = text;
        return tmp;
    }

    private static void CopyRect(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.sizeDelta = source.sizeDelta;
        target.anchoredPosition = source.anchoredPosition;
    }

    private static void MoveButton(string name, float deltaY)
    {
        GameObject go = GameObject.Find(name);
        RectTransform r = go != null ? go.GetComponent<RectTransform>() : null;
        if (r != null) r.anchoredPosition += new Vector2(0f, deltaY);
    }

    private void BuildSettings(Transform panel)
    {
        GameObject box = UIBuild.Node("GameplaySettings", panel);
        RectTransform br = (RectTransform)box.transform;
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
        br.anchoredPosition = new Vector2(225f, -5f);
        br.sizeDelta = new Vector2(390f, 225f);
        UIBuild.Sliced(box, Resources.Load<Sprite>("UI/panel"), new Color(0.045f, 0.09f, 0.14f, 0.96f));

        VerticalLayoutGroup vertical = box.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(20, 20, 16, 16);
        vertical.spacing = 10;
        vertical.childControlHeight = true;
        vertical.childControlWidth = true;
        vertical.childForceExpandHeight = false;
        vertical.childForceExpandWidth = true;

        TextMeshProUGUI title = UIBuild.Label(UIBuild.Node("Title", box.transform), 24, FontStyles.Bold,
            new Color(0.55f, 0.88f, 1f), TextAlignmentOptions.Center);
        title.text = "GAMEPLAY";
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;

        debugValue = AddSettingRow(box.transform, "Debug", "", () => AdjustDebug(-5f), () => AdjustDebug(5f));
        backgroundValue = AddSettingRow(box.transform, "Background", "", () => CycleBackground(-1), () => CycleBackground(1));
        RefreshSettings();
    }

    private TextMeshProUGUI AddSettingRow(Transform parent, string name, string initial,
        UnityEngine.Events.UnityAction previous, UnityEngine.Events.UnityAction next)
    {
        GameObject row = UIBuild.Node(name, parent);
        row.AddComponent<LayoutElement>().preferredHeight = 62f;
        HorizontalLayoutGroup horizontal = row.AddComponent<HorizontalLayoutGroup>();
        horizontal.spacing = 8;
        horizontal.childControlHeight = true;
        horizontal.childControlWidth = true;
        horizontal.childForceExpandHeight = true;
        horizontal.childForceExpandWidth = false;

        Button left = UIBuild.Button(row.transform, "Previous", "<", 25, 52);
        left.GetComponent<LayoutElement>().preferredWidth = 52f;
        left.onClick.AddListener(previous);

        GameObject valueObject = UIBuild.Node("Value", row.transform);
        LayoutElement valueLayout = valueObject.AddComponent<LayoutElement>();
        valueLayout.preferredWidth = 238f;
        TextMeshProUGUI value = UIBuild.Label(valueObject, 19, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        value.text = initial;

        Button right = UIBuild.Button(row.transform, "Next", ">", 25, 52);
        right.GetComponent<LayoutElement>().preferredWidth = 52f;
        right.onClick.AddListener(next);
        return value;
    }

    private void AdjustDebug(float delta)
    {
        GamePreferences.DebugAnswerSeconds += delta;
        RefreshSettings();
    }

    private void CycleBackground(int direction)
    {
        int value = GamePreferences.BackgroundChoice + direction;
        if (value > (int)BackgroundTheme.Cyberpunk) value = -1;
        if (value < -1) value = (int)BackgroundTheme.Cyberpunk;
        GamePreferences.BackgroundChoice = value;
        RefreshSettings();
    }

    private void RefreshSettings()
    {
        if (debugValue != null) debugValue.text = $"DEBUG TIME  {GamePreferences.DebugAnswerSeconds:0}s";
        if (backgroundValue != null) backgroundValue.text = "BACKGROUND  " + GamePreferences.BackgroundChoiceName;
    }
}
