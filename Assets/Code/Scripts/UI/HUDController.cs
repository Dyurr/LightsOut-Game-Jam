using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class HUDController : MonoBehaviour
{
   [SerializeField] private TextMeshProUGUI cashAddedText;
   [SerializeField] private TextMeshProUGUI totalCashText;

    [SerializeField] private Slider interactProgressBar;

    public static HUDController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public Slider InteractProgressBar => interactProgressBar;

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
        cashAddedText.gameObject.SetActive(false); 
        interactProgressBar.gameObject.SetActive(false);
        UpdateTotalCashText(PlayerWallet.Instance.totalCash);
    }


}
