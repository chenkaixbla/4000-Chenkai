using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }
    public int currentCurrency;

    void Awake()
    {
        Instance = this;
    }

    public void AddCurrency(int amount)
    {
        currentCurrency += amount;
        if (currentCurrency <= 0)
            currentCurrency = 0;
        Debug.Log($"Added {amount} currency. Current: {currentCurrency}");
    }
}
