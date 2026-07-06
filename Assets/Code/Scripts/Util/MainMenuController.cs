using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    // Change it to main scene when finished
    [SerializeField] private string gameSceneName = "MainLevel";


      public void StartGame()
    {
        PlayerWallet.ResetWallet();
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quit pressed");
        Application.Quit();
    }

    public void Credits()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Credits");
    }

    public void MainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
