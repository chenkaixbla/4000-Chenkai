using EditorAttributes;
using TMPro;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }
    public ulong currentCurrency;

    public TMP_Text currencyText;

    void Awake()
    {
        Instance = this;
        UpdateCurrencyText(currentCurrency);
    }

    [Button]
    public void AddCurrency(ulong amount)
    {
        currentCurrency += amount;
        UpdateCurrencyText(currentCurrency);
        Debug.Log($"Added {amount} currency. Current: {currentCurrency}");
    }

    public bool CanAfford(ulong amount)
    {
        return currentCurrency >= amount;
    }

    public bool TrySpendCurrency(ulong amount)
    {
        if (!CanAfford(amount))
        {
            return false;
        }

        currentCurrency -= amount;
        UpdateCurrencyText(currentCurrency);
        Debug.Log($"Spent {amount} currency. Current: {currentCurrency}");
        return true;
    }

    void UpdateCurrencyText(ulong newAmount)
    {
        if (currencyText != null)
        {
            // Make sure its formmatted with commas for thousands
            currencyText.text = $"{newAmount:N0}";
        }   
    }
}
