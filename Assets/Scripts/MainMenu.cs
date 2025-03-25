using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject PausePanel;
    private bool IsPaused = false;
    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Escape))
        {
            if (IsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }
    public void PauseGame()
    {
        if (PausePanel != null)
        {
            PausePanel.SetActive(true);
            Time.timeScale = 0;
                IsPaused = true ;
        }
    }

    public void ResumeGame()
    {
        if (PausePanel != null)
        {
            PausePanel.SetActive(false);
            Time.timeScale = 1;
            IsPaused = false ;
        }
    }

}
