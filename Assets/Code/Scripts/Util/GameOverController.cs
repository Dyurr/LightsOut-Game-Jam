using UnityEngine;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private AudioSource screamAudio;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string restartSceneName = "MainLevel";
    [SerializeField] private GameObject gameOverUI;

    [SerializeField] private float delayBeforeGameOverUI = 1f;

    

    private void Start()
    {
        gameOverUI.SetActive(false);
    
        if (screamAudio != null)
        {
            screamAudio.Play();
        }
        Invoke(nameof(ShowGameOverUI), delayBeforeGameOverUI);
    }

    private void ShowGameOverUI()
    {
        gameOverUI.SetActive(true);
    }

    public void OnReturnToMenuPressed()
    {
        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.ResetWallet();
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnRestartPressed()
    {
        Debug.Log("Restart pressed");
        UnityEngine.SceneManagement.SceneManager.LoadScene(restartSceneName);
    }
}
