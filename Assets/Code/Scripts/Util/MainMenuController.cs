using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    // Change it to main scene when finished
    [SerializeField] private string gameSceneName = "MainLevel";


    public void StartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
        PlayerWallet playerWallet = FindObjectOfType<PlayerWallet>(); // or however you access it once player exists
        playerWallet?.ResetWallet();
        
    }

    public void QuitGame()
    {
        Debug.Log("Quit pressed");
        Application.Quit();
    }
}
