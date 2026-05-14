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
    private object currentlySelectedItem;
    [Header("UI Elements")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [Header("Animation Settings")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float animationDuration = 0.3f;
    [Header("Category Tabs")]
    [SerializeField] private Button powerUpsTabButton;
    [SerializeField] private Button skinsTabButton;
    [SerializeField] private GameObject powerUpsContent;
    [SerializeField] private GameObject skinsContent;
    [Header("Shop Sounds")]
    [SerializeField] private AudioClip purchaseSuccessSound;
    [SerializeField] private AudioClip purchaseFailSound;
    private AudioSource uiAudioSource;
    [Header("Skin Shop Specifics")]
    [SerializeField] private Button selectCatButton;
    [SerializeField] private Button selectManButton;
    [SerializeField] private List<SkinShopEntry> skinShopItems;
    [Header("Milk Category")]
    [SerializeField] private Button milkTabButton;
    [SerializeField] private GameObject milkContent;
    [SerializeField] private Button buyMilkButton;
    [SerializeField] private int milkPrice = 10000;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private TextMeshProUGUI buyMilkPriceText;
    private enum ShopCategory { PowerUps, Skins, Milk }
    private ShopCategory currentCategory;
    private CharacterType currentCharacterView;
    [Header("Shop Items Configuration")]
    [SerializeField] private List<ShopItemEntry> shopItems; // The list you wanted!
    [SerializeField] private TextMeshProUGUI selectedItemName; // For the item's name
    private List<PowerUpData> purchasedPowerUps = new List<PowerUpData>();

    private void Awake()
    {
        powerUpsTabButton.onClick.AddListener(ShowPowerUpsCategory);
        skinsTabButton.onClick.AddListener(ShowSkinsCategory);
        selectCatButton.onClick.AddListener(ViewCatSkins);
        selectManButton.onClick.AddListener(ViewManSkins);
        milkTabButton.onClick.AddListener(ShowMilkCategory);
        buyMilkButton.onClick.AddListener(OnBuyMilkClicked);
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
        uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;
        uiAudioSource.loop = false;
        uiAudioSource.bypassEffects = true; // This group of settings helps ignore mixers
        uiAudioSource.bypassListenerEffects = true;
        uiAudioSource.bypassReverbZones = true;
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
        // This part handles your existing power-up items
        foreach (ShopItemEntry entry in shopItems)
        {
            entry.purchaseButton.onClick.RemoveAllListeners();
            entry.purchaseButton.onClick.AddListener(() => OnItemSelected(entry.powerUpData));
        }

        // This new part handles the skin items
        foreach (SkinShopEntry entry in skinShopItems)
        {
            entry.purchaseButton.onClick.RemoveAllListeners();
            entry.purchaseButton.onClick.AddListener(() => OnItemSelected(entry.skinData));
        }
    }
    // --- ADD ALL OF THESE NEW FUNCTIONS ---

    public void ShowPowerUpsCategory()
    {
        currentCategory = ShopCategory.PowerUps;
        powerUpsContent.SetActive(true);
        skinsContent.SetActive(false);
        milkContent.SetActive(false);
        descriptionPanel.SetActive(false);
    }

    public void ShowSkinsCategory()
    {
        currentCategory = ShopCategory.Skins;
        powerUpsContent.SetActive(false);
        skinsContent.SetActive(true);
        milkContent.SetActive(false); // <-- ADD THIS
        descriptionPanel.SetActive(false);
        ViewCatSkins();
    }

    private void ViewCatSkins()
    {
        currentCharacterView = CharacterType.Cat;
        RefreshSkinButtonVisibility();
    }

    private void ViewManSkins()
    {
        currentCharacterView = CharacterType.Man;
        RefreshSkinButtonVisibility();
    }
    public void ShowCatSkins()
    {
        // Loop through all skin items and only show the ones for the Cat
        foreach (var entry in skinShopItems)
        {
            if (entry.skinData.character == CharacterType.Cat)
            {
                entry.purchaseButton.gameObject.SetActive(true);
            }
            else
            {
                entry.purchaseButton.gameObject.SetActive(false);
            }
        }
        // You can add animation/color change for the Cat/Man buttons here if you want
    }
    public void ShowMilkCategory()
    {
        currentCategory = ShopCategory.Milk;
        powerUpsContent.SetActive(false);
        skinsContent.SetActive(false);
        milkContent.SetActive(true);

        // Make sure the description panel is hidden for this simple category
        descriptionPanel.SetActive(false);
        if (PlayerPrefs.GetInt("MilkBought", 0) == 0)
        {
            buyMilkPriceText.text = milkPrice.ToString();
        }
        else
        {
            buyMilkPriceText.text = "YAY!";
        }
        // You could also update the price text on the button here if you want
        // For example: buyMilkButton.GetComponentInChildren<TextMeshProUGUI>().text = milkPrice.ToString();
    }

    public void OnBuyMilkClicked()
    {
        // First, check if we've already bought it
        if (PlayerPrefs.GetInt("MilkBought", 0) == 1)
        {
            Debug.Log("You already bought the milk!");
            return;
        }

        if (WalletManager.Instance.SpendCoins(milkPrice))
        {
            // SUCCESS!
            if (purchaseSuccessSound != null) uiAudioSource.PlayOneShot(purchaseSuccessSound);
            Debug.Log("YOU BOUGHT THE MILK! YOU WIN!");

            // --- NEW: Save that the milk has been bought ---
            PlayerPrefs.SetInt("MilkBought", 1);
            PlayerPrefs.Save();
            // ---------------------------------------------

            // Update the button text immediately
            buyMilkButton.interactable = false;
            buyMilkPriceText.text = "YAY!";

            // Hide the shop and start the credits
            if (shopPanel != null) shopPanel.SetActive(false);
            if (creditsPanel != null)
            {
                creditsPanel.SetActive(true);
                CreditsScroller scroller = creditsPanel.GetComponent<CreditsScroller>();
                if (scroller != null) scroller.StartCredits();
            }
        }
        else
        {
            // FAIL!
            if (purchaseFailSound != null) uiAudioSource.PlayOneShot(purchaseFailSound);
            Debug.Log("Not enough coins to buy the milk!");
        }
    }
    public void ShowManSkins()
    {
        // Loop through all skin items and only show the ones for the Man
        foreach (var entry in skinShopItems)
        {
            if (entry.skinData.character == CharacterType.Man)
            {
                entry.purchaseButton.gameObject.SetActive(true);
            }
            else
            {
                entry.purchaseButton.gameObject.SetActive(false);
            }
        }
        // You can add animation/color change for the Cat/Man buttons here if you want
    }
    private void RefreshSkinButtonVisibility()
    {
        Debug.Log("--- Refreshing Skin Visibility ---");
        Debug.Log("Current character view is: " + currentCharacterView.ToString());

        if (skinShopItems.Count == 0)
        {
            Debug.LogWarning("The 'Skin Shop Items' list is empty in the Inspector! Nothing to show.");
            return;
        }

        foreach (var entry in skinShopItems)
        {
            // Safety checks
            if (entry.purchaseButton == null)
            {
                Debug.LogError("Found an entry in the list with a MISSING BUTTON.");
                continue;
            }
            if (entry.skinData == null)
            {
                Debug.LogError("Found an entry for button '" + entry.purchaseButton.name + "' with MISSING SKIN DATA.");
                continue;
            }

            // The actual logic
            bool isCorrectCharacter = (entry.skinData.character == currentCharacterView);

            Debug.Log("Checking skin: '" + entry.skinData.skinName + "'. It belongs to: " + entry.skinData.character + ". Should it be visible? " + isCorrectCharacter);

            entry.purchaseButton.gameObject.SetActive(isCorrectCharacter);
        }
        Debug.Log("--- Finished Refreshing ---");
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
            if (purchaseSuccessSound != null) uiAudioSource.PlayOneShot(purchaseSuccessSound);
            Debug.Log("Purchase successful: " + item.powerUpName);
            InventoryManager.Instance.AddOwnedPowerUp(item);
            UpdateCoinCount();
            PlayerPowerLevelManager.Instance.CalculateAndSavePPL();

            // --- NEW: After buying, update the description panel's state ---
            // Disable the buy button and change its text to "Owned"
            selectedItemBuyButton.interactable = false;
            buyButtonText.text = "Owned";
        }
        else
        {
            if (purchaseFailSound != null) uiAudioSource.PlayOneShot(purchaseFailSound);
            Debug.Log("Purchase failed. Not enough coins.");
            // You could add a visual/sound effect here for failure
        }
    }
    private void OnItemSelected(SkinData item)
    {
        if (item == null) return;

        currentlySelectedItem = item;
        descriptionPanel.SetActive(true);

        selectedItemIcon.sprite = item.icon;
        selectedItemName.text = item.skinName;
        selectedItemDescription.text = item.description;
        buyButtonText.text = item.price.ToString();

        bool isOwned = InventoryManager.Instance.IsSkinOwned(item.GetUniqueID());
        selectedItemBuyButton.interactable = !isOwned;
        buyButtonText.text = isOwned ? "Owned" : item.price.ToString();
    }

    // --- ADD THIS NEW AttemptPurchase FUNCTION ---
    private void AttemptPurchase(SkinData item)
    {
        if (WalletManager.Instance.SpendCoins(item.price))
        {
            if (purchaseSuccessSound != null) uiAudioSource.PlayOneShot(purchaseSuccessSound);
            // We must use the UNIQUE ID here, not the label.
            InventoryManager.Instance.AddOwnedSkin(item.GetUniqueID()); // <--- THIS IS THE FIX
            OnItemSelected(item); // Refresh the UI
            UpdateCoinCount();
            PlayerPowerLevelManager.Instance.CalculateAndSavePPL();

        }
        else
        {
            if (purchaseFailSound != null) uiAudioSource.PlayOneShot(purchaseFailSound);
            Debug.Log("Purchase failed. Not enough coins.");
        }
    }

    // --- NOW, REPLACE your existing OnBuyButtonPressed function with this one ---

    private void OnBuyButtonPressed()
    {
        if (currentlySelectedItem is PowerUpData powerUp)
        {
            AttemptPurchase(powerUp);
        }
        else if (currentlySelectedItem is SkinData skin)
        {
            AttemptPurchase(skin);
        }
    }
    public void OpenShop()
    {
        if (shopPanel != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimatePanel(true));
            UpdateCoinCount();
            descriptionPanel.SetActive(false);
            ShowPowerUpsCategory(); // This is correct
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

            // --- THIS IS THE FIX ---
            coinCountText.text = currentCoins.ToString();
        }
    }
}
