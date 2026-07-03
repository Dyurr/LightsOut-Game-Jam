using UnityEngine;

public class Item : MonoBehaviour
{
    
    [SerializeField] private int cashValue = 50;
    [SerializeField] private string itemName = "Default Item";



    public void Interact()
    {
        PlayerWallet.Instance.AddCash(cashValue);
        Destroy(gameObject);
    }
}
