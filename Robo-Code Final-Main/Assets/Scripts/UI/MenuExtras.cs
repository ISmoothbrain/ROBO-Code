using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Adds the new menu items in the same style as your existing buttons, using the images in
// Assets/Resources/MenuArt:
//   Main menu: the Play plate becomes CONTINUE, NEW GAME goes under it, Settings and Quit move down.
//   Settings (main menu and pause menu): a second column with ANSWER TIME (a copy of your Music
//   slider) and BACKGROUND (a plate that cycles the background), beside the existing column.
// Everything is copied from your own objects, so sizes and looks match.
public static class MenuExtras
{
    private const float ColumnShift = 100f;   // the two settings columns sit this far either side of centre
    private const float ButtonGap = 6f;       // space between main-menu buttons

    // ---------------------------------------------------------------- main menu

    public static void SetupMainMenu(MainMenu menu)
    {
        MenuArt art = MenuArt.Load();
        GameObject playGo = GameObject.Find("Play");
        if (menu == null || art == null || playGo == null || playGo.transform.parent.Find("NewGame") != null) return;

        RectTransform play = (RectTransform)playGo.transform;
        Transform parent = play.parent;

        // CONTINUE (the existing Play button, so its On Click still runs MainMenu.PlayGame)
        Button continueButton = playGo.GetComponent<Button>();
        Image continueImage = playGo.GetComponent<Image>();
        if (continueImage != null && art.continueButton != null) continueImage.sprite = art.continueButton;
        bool canContinue = LevelProgress.HasProgress();
        if (continueButton != null) continueButton.interactable = canContinue;
        if (continueImage != null) continueImage.color = canContinue ? Color.white : new Color(1f, 1f, 1f, 0.45f);

        // NEW GAME: a copy of the Play button, just below it
        GameObject newGameGo = Object.Instantiate(playGo, parent);
        newGameGo.name = "NewGame";
        newGameGo.transform.SetSiblingIndex(play.GetSiblingIndex() + 1);
        RectTransform ng = (RectTransform)newGameGo.transform;
        float step = play.rect.height + ButtonGap;
        ng.anchoredPosition = play.anchoredPosition - new Vector2(0f, step);
        Image ngImage = newGameGo.GetComponent<Image>();
        if (ngImage != null) { ngImage.sprite = art.newGameButton; ngImage.color = Color.white; }
        Button ngButton = newGameGo.GetComponent<Button>();
        ngButton.onClick = new Button.ButtonClickedEvent();   // drop the copied PlayGame call
        ngButton.interactable = true;
        ngButton.onClick.AddListener(menu.NewGame);
        AddClickSound(ngButton);

        // make room: everything below the Play button moves down by one button
        foreach (string name in new[] { "Settings", "Quit" })
        {
            RectTransform r = parent.Find(name) as RectTransform;
            if (r != null) r.anchoredPosition -= new Vector2(0f, step);
        }
    }

    // ---------------------------------------------------------------- settings panel

    public static void SetupSettingsPanel(GameObject panel)
    {
        MenuArt art = MenuArt.Load();
        if (panel == null || art == null || panel.transform.Find("AnswerTimeSlider") != null) return;

        RectTransform musicLabel = panel.transform.Find("MusicLabel") as RectTransform;
        RectTransform musicSlider = panel.transform.Find("MusicSlider") as RectTransform;
        RectTransform sfxLabel = panel.transform.Find("SFXLabel") as RectTransform;
        RectTransform sfxSlider = panel.transform.Find("SFXSlider") as RectTransform;
        RectTransform keybinds = panel.transform.Find("KeybindsButton") as RectTransform;
        if (musicLabel == null || musicSlider == null || sfxLabel == null || sfxSlider == null) return;

        // the existing labels and sliders move left; buttons (Keybinds, Back) stay centred
        foreach (Transform child in panel.transform)
        {
            if (child.GetComponent<Button>() != null) continue;
            if (child is RectTransform r) r.anchoredPosition -= new Vector2(ColumnShift, 0f);
        }
        float rightX = musicLabel.anchoredPosition.x + 2f * ColumnShift;

        // ANSWER TIME: label + a copy of the Music slider
        RectTransform answerLabel = CloneAt(musicLabel, "AnswerTimeLabel", rightX, musicLabel.anchoredPosition.y);
        SetSprite(answerLabel, art.answerTimeLabel);

        RectTransform answerSlider = CloneAt(musicSlider, "AnswerTimeSlider", rightX, musicSlider.anchoredPosition.y);
        foreach (VolumeSettings v in answerSlider.GetComponentsInChildren<VolumeSettings>(true)) Object.Destroy(v.gameObject == answerSlider.gameObject ? (Object)v : v.gameObject);
        foreach (BrightnessSettings b in answerSlider.GetComponentsInChildren<BrightnessSettings>(true)) Object.Destroy(b);
        Slider slider = answerSlider.GetComponent<Slider>();
        if (slider != null) slider.onValueChanged = new Slider.SliderEvent();   // drop the copied music volume call

        TextMeshProUGUI seconds = AddText(panel.transform, "AnswerTimeValue", 14,
            new Vector2(rightX + answerSlider.rect.width * 0.5f + 26f, answerSlider.anchoredPosition.y), new Vector2(48f, 20f));
        seconds.alignment = TextAlignmentOptions.Left;
        AnswerTimeSetting ats = answerSlider.gameObject.AddComponent<AnswerTimeSetting>();
        ats.slider = slider;
        ats.valueText = seconds;
        if (answerSlider.gameObject.activeInHierarchy && slider != null) ats.Set(slider.value);

        // BACKGROUND: label + a plate showing the current background; click to change it
        RectTransform bgLabel = CloneAt(sfxLabel, "BackgroundLabel", rightX, sfxLabel.anchoredPosition.y);
        SetSprite(bgLabel, art.backgroundLabel);

        GameObject plate;
        if (keybinds != null)
        {
            plate = Object.Instantiate(keybinds.gameObject, panel.transform);
            foreach (Transform c in plate.transform) Object.Destroy(c.gameObject);
            plate.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();   // drop the copied Keybinds call
        }
        else
        {
            plate = UIBuild.Button(panel.transform, "BackgroundValue", "", 12, 28).gameObject;
        }
        plate.name = "BackgroundValue";
        RectTransform pr = (RectTransform)plate.transform;
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(sfxSlider.rect.width, 28f);
        pr.anchoredPosition = new Vector2(rightX, sfxSlider.anchoredPosition.y - 2f);
        if (art.valuePlate != null) plate.GetComponent<Image>().sprite = art.valuePlate;
        TextMeshProUGUI bgText = AddText(plate.transform, "Text", 12, Vector2.zero, Vector2.zero);
        UIBuild.Stretch(bgText.gameObject, 0, 0, 1, 1);
        bgText.alignment = TextAlignmentOptions.Center;
        plate.AddComponent<BackgroundSetting>().valueText = bgText;
        AddClickSound(plate.GetComponent<Button>());
    }

    // ---------------------------------------------------------------- helpers

    private static RectTransform CloneAt(RectTransform source, string name, float x, float y)
    {
        GameObject go = Object.Instantiate(source.gameObject, source.parent);
        go.name = name;
        RectTransform r = (RectTransform)go.transform;
        r.anchoredPosition = new Vector2(x, y);
        return r;
    }

    private static void SetSprite(RectTransform r, Sprite s)
    {
        Image img = r.GetComponent<Image>();
        if (img != null && s != null) img.sprite = s;
    }

    private static TextMeshProUGUI AddText(Transform parent, string name, float size, Vector2 pos, Vector2 box)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = box;
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        UIFont.Apply(t);
        t.fontSize = size;
        t.color = new Color(0.86f, 0.92f, 0.97f);
        t.raycastTarget = false;
        return t;
    }

    private static void AddClickSound(Button b)
    {
        if (b == null) return;
        b.onClick.AddListener(() =>
        {
            if (AudioManager.instance != null) AudioManager.instance.PlaySFX(AudioManager.instance.buttonClick);
        });
    }
}
