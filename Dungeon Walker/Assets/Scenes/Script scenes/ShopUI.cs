using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }
    [Header("Description Panel")]
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private Image selectedItemIcon;
    [SerializeField] private TextMeshProUGUI selectedItemDescription;
    [SerializeField] private Button selectedItemBuyButton;
    [SerializeField] private TextMeshProUGUI buyButtonText; // The text on the buy button for the price
    private PowerUpData currentlySelectedItem;
    [Header("UI Elements")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [Header("Animation Settings")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float animationDuration = 0.3f;
    // --- THE NEW SYSTEM ---
    [Header("Shop Items Configuration")]
    [SerializeField] private List<ShopItemEntry> shopItems; // The list you wanted!
    [SerializeField] private TextMeshProUGUI selectedItemName; // For the item's name
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
        selectedItemBuyButton.onClick.AddListener(OnBuyButtonPressed);
    }
    private void Start()
    {
        // Hide the description panel at the start
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }
    }
    /// <summary>
    /// This function loops through our list and configures each button's click event automatically.
    /// </summary>
    private void SetupButtons()
    {
        foreach (ShopItemEntry entry in shopItems)
        {
            PowerUpData currentItem = entry.powerUpData;
            Button itemButton = entry.purchaseButton;

            itemButton.onClick.RemoveAllListeners();
            // When a power-up button is clicked, call OnItemSelected
            itemButton.onClick.AddListener(() => OnItemSelected(currentItem));
        }
    }
    private void OnItemSelected(PowerUpData item)
    {
        if (item == null) return;

        // Store the selected item
        currentlySelectedItem = item;

        // Show the description panel
        descriptionPanel.SetActive(true);

        // Populate the panel with the item's data
        selectedItemIcon.sprite = item.icon;
        selectedItemDescription.text = item.description;
        selectedItemName.text = item.powerUpName;
        // Update the buy button's text to show the price
        buyButtonText.text = item.price.ToString();

        // Check if the item is already owned and disable the button if so
        if (InventoryManager.Instance.ownedPowerUps.Contains(item))
        {
            selectedItemBuyButton.interactable = false;
            buyButtonText.text = "Owned";
        }
        else
        {
            selectedItemBuyButton.interactable = true;
        }
    }

    private void AttemptPurchase(PowerUpData item)
    {
        if (item == null) return;

        if (InventoryManager.Instance.ownedPowerUps.Contains(item))
        {
            Debug.Log("Item already purchased: " + item.powerUpName);
            return;
        }

        bool purchaseSuccessful = WalletManager.Instance.SpendCoins(item.price);

        if (purchaseSuccessful)
        {
            Debug.Log("Purchase successful: " + item.powerUpName);
            InventoryManager.Instance.AddOwnedPowerUp(item);
            UpdateCoinCount();

            // --- NEW: After buying, update the description panel's state ---
            // Disable the buy button and change its text to "Owned"
            selectedItemBuyButton.interactable = false;
            buyButtonText.text = "Owned";
        }
        else
        {
            Debug.Log("Purchase failed. Not enough coins.");
            // You could add a visual/sound effect here for failure
        }
    }
    private void OnBuyButtonPressed()
    {
        // Attempt to purchase the item we currently have selected
        AttemptPurchase(currentlySelectedItem);
    }
    public void OpenShop()
    {
        if (shopPanel != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimatePanel(true));
            UpdateCoinCount();
            descriptionPanel.SetActive(false);
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
