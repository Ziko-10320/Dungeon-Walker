using UnityEngine;
using UnityEngine.UI;
using TMPro; // Add this for TextMeshPro

[System.Serializable]
public class ShopItemEntry
{
    public PowerUpData powerUpData;
    public Button purchaseButton;
    public TextMeshProUGUI priceText; // <-- ADD THIS LINE
}
