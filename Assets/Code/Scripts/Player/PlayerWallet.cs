using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public int totalCash { get; private set; }

    public event Action<int> onCashChanged;

    public event Action<int> onCashAdded;

    public static PlayerWallet Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void AddCash(int amount)
    {
        totalCash += amount;
        onCashAdded?.Invoke(amount);
        onCashChanged?.Invoke(totalCash);
    }
}

    
    
    
