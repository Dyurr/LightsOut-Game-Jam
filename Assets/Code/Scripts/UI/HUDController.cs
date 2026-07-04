using UnityEngine;
using TMPro;
public class HUDController : MonoBehaviour
{
   [SerializeField] private TextMeshProUGUI cashAddedText;
   [SerializeField] private TextMeshProUGUI totalCashText;


   private void OnEnable()
   {
       PlayerWallet.Instance.onCashAdded += UpdateCashAddedText;
       PlayerWallet.Instance.onCashChanged += UpdateTotalCashText;
   }

    private void OnDisable()
    {
         PlayerWallet.Instance.onCashAdded -= UpdateCashAddedText;
         PlayerWallet.Instance.onCashChanged -= UpdateTotalCashText;
    }


    private void UpdateCashAddedText(int amount)
    {
        cashAddedText.text = $"+${amount}";
        cashAddedText.gameObject.SetActive(true);
        Invoke("HideCashAddedText", 2f); // Hide after 2 seconds
    }

    private void HideCashAddedText()
    {
        cashAddedText.gameObject.SetActive(false);
    }


    private void UpdateTotalCashText(int totalCash)
    {
        totalCashText.text = $"Total Cash: ${totalCash}";
    }

    private void Start()
    {
        cashAddedText.gameObject.SetActive(false); // Initially hide the cash added text
        // Initialize the total cash display at the start
        UpdateTotalCashText(PlayerWallet.Instance.totalCash);
    }


}
