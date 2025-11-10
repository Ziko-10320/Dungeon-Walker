using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class UIWalletDisplay : MonoBehaviour
{
    private TextMeshProUGUI coinText;

    void Awake()
    {
        // Get the TextMeshPro component on this same GameObject.
        coinText = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        // When this UI element becomes active, subscribe to the WalletManager's event.
        if (WalletManager.Instance != null)
        {
            WalletManager.Instance.OnCoinsChanged.AddListener(UpdateCoinText);
            // Also, update the text immediately with the current value.
            UpdateCoinText(WalletManager.Instance.CurrentCoins);
        }
    }

    void OnDisable()
    {
        // When this UI element is disabled, unsubscribe to prevent errors.
        if (WalletManager.Instance != null)
        {
            WalletManager.Instance.OnCoinsChanged.RemoveListener(UpdateCoinText);
        }
    }

    /// <summary>
    /// This method is called automatically by the OnCoinsChanged event.
    /// </summary>
    private void UpdateCoinText(int newAmount)
    {
        if (coinText != null)
        {
            coinText.text = newAmount.ToString();
        }
    }
}
