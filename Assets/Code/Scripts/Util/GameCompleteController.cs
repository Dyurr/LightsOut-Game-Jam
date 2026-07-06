using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
public class GameCompleteController : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private TextMeshProUGUI cashText;




    private void Start()
    {
        int finalScore = PlayerWallet.Instance != null ? PlayerWallet.Instance.totalCash : 0;
        cashText.text = $"You escaped with ${finalScore}";
    }

    public void OnReturnToMenuPressed()
    {
        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.ResetWallet();
        }
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
