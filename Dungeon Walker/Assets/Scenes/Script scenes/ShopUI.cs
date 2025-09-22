using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [Header("Animation Settings")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float animationDuration = 0.3f;
    // --- THE NEW SYSTEM ---
    [Header("Shop Items Configuration")]
    [SerializeField] private List<ShopItemEntry> shopItems; // The list you wanted!

    private List<PowerUpData> purchasedPowerUps = new List<PowerUpData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = shopPanel.GetComponent<CanvasGroup>();
        }
        // --- Automatically set up all buttons ---
        SetupButtons();
    }

    /// <summary>
    /// This function loops through our list and configures each button's click event automatically.
    /// </summary>
    private void SetupButtons()
    {
        foreach (ShopItemEntry entry in shopItems)
        {
            PowerUpData currentItem = entry.powerUpData;
            Button currentButton = entry.purchaseButton;

            // --- NEW PART: SET THE PRICE TEXT ---
            if (entry.priceText != null)
            {
                entry.priceText.text = currentItem.price.ToString();
            }
            // ------------------------------------

            currentButton.onClick.RemoveAllListeners();
            currentButton.onClick.AddListener(() => AttemptPurchase(currentItem, currentButton));
        }
    }

    private void AttemptPurchase(PowerUpData item, Button button)
    {
        if (item == null) return;

        // CHANGE THIS: Check the InventoryManager's list, not the local one.
        if (InventoryManager.Instance.ownedPowerUps.Contains(item))
        {
            Debug.Log("Item already purchased: " + item.powerUpName);
            return;
        }

        bool purchaseSuccessful = WalletManager.Instance.SpendCoins(item.price);

        if (purchaseSuccessful)
        {
            Debug.Log("Purchase successful: " + item.powerUpName);

            // CHANGE THIS: Add the item to the InventoryManager.
            InventoryManager.Instance.AddOwnedPowerUp(item);

            UpdateCoinCount();

            if (button != null)
            {
                button.interactable = false;
            }
        }
        else
        {
            Debug.Log("Purchase failed. Not enough coins.");
        }
    }

    public void OpenShop()
    {
        if (shopPanel != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimatePanel(true));
            UpdateCoinCount();
        }
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimatePanel(false));
        }
    }
    private IEnumerator AnimatePanel(bool opening)
    {
        float startAlpha = opening ? 0f : 1f;
        float endAlpha = opening ? 1f : 0f;
        Vector3 startScale = opening ? new Vector3(0.8f, 0.8f, 1f) : Vector3.one;
        Vector3 endScale = opening ? Vector3.one : new Vector3(0.8f, 0.8f, 1f);

        if (opening)
        {
            shopPanel.SetActive(true);
            panelCanvasGroup.interactable = false;
        }

        float timer = 0f;
        while (timer < animationDuration)
        {
            float progress = timer / animationDuration;
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            shopPanel.transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        panelCanvasGroup.alpha = endAlpha;
        shopPanel.transform.localScale = endScale;

        if (!opening)
        {
            shopPanel.SetActive(false);
        }

        panelCanvasGroup.interactable = opening;
    }

    public void UpdateCoinCount()
    {
        if (coinCountText != null && WalletManager.Instance != null)
        {
            int currentCoins = WalletManager.Instance.CurrentCoins;
            coinCountText.text = "Coins: " + currentCoins.ToString();
        }
    }
}
