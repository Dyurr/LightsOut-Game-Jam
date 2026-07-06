using UnityEngine;
using System;
public class PlayerWallet : MonoBehaviour
{
    public static int totalCash { get; private set; }

    public event Action<int> onCashChanged;

    public event Action<int> onCashAdded;

    public static PlayerWallet Instance;



    public void AddCash(int amount)
    {
        totalCash += amount;
        onCashAdded?.Invoke(amount);
        onCashChanged?.Invoke(totalCash);
        Debug.Log($"Cash added: {amount}, Total: {totalCash}");
    }

    public void ResetWallet()
    {
        totalCash = 0;
        onCashChanged?.Invoke(totalCash);
        Debug.Log("Wallet reset");
    }
}

    
    
    
