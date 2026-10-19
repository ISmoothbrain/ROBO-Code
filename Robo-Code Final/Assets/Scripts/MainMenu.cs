using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
    public GameObject settingsPanel;
    public GameObject keybindPanel;

    void Start()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (keybindPanel != null)
        {
            keybindPanel.SetActive(false);
        }

        // CONTINUE / NEW GAME buttons and the ANSWER TIME / BACKGROUND settings, in your button art.
        MenuExtras.SetupMainMenu(this);
        MenuExtras.SetupSettingsPanel(settingsPanel);
    }

    // START / CONTINUE button: back to the level you were last in (the tutorial if there's no save).
    public void PlayGame()
    {
        int index = GameSave.ContinueIndex();
        SceneTransition.LoadScene(index, false, LevelProgress.TitleForBuildIndex(index));
    }

    // NEW GAME button: wipes the save (levels, chests, coins, upgrades, Hack) and starts the tutorial.
    public void NewGame()
    {
        GameSave.StartNewGame();
        SceneTransition.LoadScene(1, false, LevelProgress.TitleForBuildIndex(1));
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void OpenKeybinds()
    {
        settingsPanel.SetActive(false);
        keybindPanel.SetActive(true);
    }

    public void CloseKeybinds()
    {
        keybindPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Quit pressed");
        Application.Quit();
    }
}