using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
    public GameObject settingsPanel;

    void Start()
    {
        settingsPanel.SetActive(false);
    }

    public void PlayGame()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("Quit pressed");
        Application.Quit();
    }
}