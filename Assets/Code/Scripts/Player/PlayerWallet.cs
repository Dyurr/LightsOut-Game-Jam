using UnityEngine;
using System;

public class PlayerWallet : MonoBehaviour
{
    public static int totalCash { get; private set; }

    public static event Action<int> onCashChanged;
    public static event Action<int> onCashAdded;

    public static void AddCash(int amount)
    {
        totalCash += amount;
        onCashAdded?.Invoke(amount);
        onCashChanged?.Invoke(totalCash);
        Debug.Log($"Cash added: {amount}, Total: {totalCash}");
    }

    public static void ResetWallet()
    {
        totalCash = 0;
        onCashChanged?.Invoke(totalCash);
        Debug.Log("Wallet reset");
    }
}