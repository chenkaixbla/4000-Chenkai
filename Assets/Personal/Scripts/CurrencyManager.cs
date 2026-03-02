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
    }

    [Button]
    public void AddCurrency(ulong amount)
    {
        currentCurrency += amount;
        if (currentCurrency <= 0)
            currentCurrency = 0;

        UpdateCurrencyText(currentCurrency);
        Debug.Log($"Added {amount} currency. Current: {currentCurrency}");
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
