using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pausePanel;

    // these live inside pausePanel, so hiding the parent leaves them switched on.
    // the pause menu then reopens on whichever one was last used instead of the main list
    public GameObject settingsPanel;
    public GameObject keybindsPanel;

    private bool isPaused = false;

    void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    void Update()
    {
        // Escape closes dialogue / the shop first, so don't also pause on that same press.
        if (UIBusy.AnyOpen || UIBusy.ClosedThisFrame) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        CloseSubPanels(); // always open on the main list
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        CloseSubPanels();
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    // null checks so the script still runs if a slot is left empty in the inspector
    private void CloseSubPanels()
    {
        if (keybindsPanel != null)
        {
            keybindsPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }
}