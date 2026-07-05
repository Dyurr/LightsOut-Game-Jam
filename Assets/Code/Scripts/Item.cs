using UnityEngine;

public class Item : MonoBehaviour
{
    
    [SerializeField] private int cashValue = 50;
    [SerializeField] private string itemName = "Default Item";



    public void Interact()
    {
        PlayerWallet.Instance.AddCash(cashValue);
        NoiseSystem.MakeNoise(transform.position, 2f); // Make noise when picking up item
        Destroy(gameObject);
    }
}
