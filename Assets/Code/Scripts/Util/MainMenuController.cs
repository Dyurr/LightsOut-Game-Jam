using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    // Change it to main scene when finished
    [SerializeField] private string gameSceneName = "BlankRoom";


    public void StartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quit pressed");
        Application.Quit();
    }
}
