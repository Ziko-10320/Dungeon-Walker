using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }
    [Header("Category Tabs")]
    [SerializeField] private Button powerUpsTabButton;
    [SerializeField] private Button skinsTabButton;
    [SerializeField] private GameObject powerUpsContent; // Drag your PowerUps_OwnedContent here
    [SerializeField] private GameObject skinsContent;    // Drag your Skins_OwnedContent here
    private enum InventoryCategory { PowerUps, Skins, Weapons }
    private InventoryCategory currentCategory;
    private CharacterType currentCharacterView;
    [Header("Skin Inventory Specifics")]
    [SerializeField] private Button selectCatButton;
    [SerializeField] private Button selectManButton;
    [SerializeField] private Image equippedSkinIcon;
    [SerializeField] private TextMeshProUGUI equippedSkinName;
    [Header("Weapon Data References")]
    [SerializeField] private List<WeaponData> allWeapons;
    private int currentWeaponIndex = 0;
    [Header("Main Panel")]
    [SerializeField] private GameObject inventoryPanel;
    [Header("Animation Settings")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float animationDuration = 0.3f;
    [Header("Equipped Slots Display")]
    [SerializeField] private Image equippedIcon_1;
    [SerializeField] private TextMeshProUGUI equippedName_1;
    [SerializeField] private Button unequipButton_1;
    [SerializeField] private Image equippedIcon_2;
    [SerializeField] private TextMeshProUGUI equippedName_2;
    [SerializeField] private Button unequipButton_2;
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Transform ownedSkinsContainer;
    [Header("Third Slot Upgrade")]
    [SerializeField] private GameObject slot3_LockedGroup; // A parent object for the "Locked" state UI
    [SerializeField] private GameObject slot3_UnlockedGroup; // A parent object for the "Unlocked" state UI
    [SerializeField] private Button unlockSlotButton;
    [SerializeField] private TextMeshProUGUI unlockPriceText;
    [SerializeField] private int thirdSlotPrice = 5000; // You can change this price in the Inspector!
    [Header("Weapon Upgrade UI")]
    [SerializeField] private Button weaponsTabButton;
    [SerializeField] private GameObject weaponsContent;
    [SerializeField] private Slider levelSlider;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Button selectBatButton_Weapons;
    [SerializeField] private Button selectBowButton_Weapons;
    [SerializeField] private Button selectLauncherButton_Weapons;
    [SerializeField] private Button selectPistolButton_Weapons;
    [SerializeField] private Image weaponIconImage;
    [SerializeField] private Button upgradeWeaponButton;
    [SerializeField] private TextMeshProUGUI upgradeCostText;
    [Header("Second Slot Upgrade")]
    [SerializeField] private GameObject slot2_LockedGroup;
    [SerializeField] private GameObject slot2_UnlockedGroup;
    [SerializeField] private Button unlockSlot2Button;
    [SerializeField] private TextMeshProUGUI unlockPrice2Text;
    [SerializeField] private int secondSlotPrice = 1000;
    // And the variables for the unlocked slot's display
    [SerializeField] private Image equippedIcon_3;
    [SerializeField] private TextMeshProUGUI equippedName_3;
    [SerializeField] private Button unequipButton_3;
    [Header("Owned Items Display")]
    [SerializeField] private Transform ownedItemsContainer;
    [SerializeField] private GameObject inventoryItemPrefab;
    [SerializeField] private Button unequipSkinButton;
    [SerializeField] private Sprite defaultCatSprite;
    [SerializeField] private Sprite defaultManSprite;

    private void Awake()
    {
        unequipSkinButton.onClick.AddListener(OnUnequipSkin);
        powerUpsTabButton.onClick.AddListener(ShowPowerUpsCategory);
        skinsTabButton.onClick.AddListener(ShowSkinsCategory);
        selectCatButton.onClick.AddListener(ViewCatSkins);
        selectManButton.onClick.AddListener(ViewManSkins);
        selectBatButton_Weapons.onClick.AddListener(() => SelectWeaponToDisplay(0));
        selectBowButton_Weapons.onClick.AddListener(() => SelectWeaponToDisplay(1));
        weaponsTabButton.onClick.AddListener(ShowWeaponsCategory);
        selectLauncherButton_Weapons.onClick.AddListener(() => SelectWeaponToDisplay(2));
        upgradeWeaponButton.onClick.AddListener(OnUpgradeWeaponClicked);
        selectPistolButton_Weapons.onClick.AddListener(() => SelectWeaponToDisplay(3));
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
        }
    }
    public void SelectWeaponToDisplay(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= allWeapons.Count) return;
        currentWeaponIndex = weaponIndex;
        RefreshWeaponDisplay();
    }
    public void OnUnequipSlot1()
    {
        // 1. Tell the manager to perform the unequip logic
        InventoryManager.Instance.UnequipPowerUp(0);

        // 2. Immediately refresh all the visuals
        RefreshAllDisplays();
    }
    public void ShowWeaponsCategory()
    {
        currentCategory = InventoryCategory.Weapons;
        powerUpsContent.SetActive(false);
        skinsContent.SetActive(false);
        weaponsContent.SetActive(true);

        // We'll add more logic here later to select which weapon to show.
        // For now, let's just refresh the display for the bat.
        RefreshWeaponDisplay();
    }
    private void Start()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        // Hook up the unequip buttons
        unequipButton_1.onClick.AddListener(() => {
            InventoryManager.Instance.UnequipPowerUp(0);
            RefreshAllDisplays(); // <-- ADD THIS LINE
        });

        unequipButton_2.onClick.AddListener(() => {
            InventoryManager.Instance.UnequipPowerUp(1);
            RefreshAllDisplays(); // <-- ADD THIS LINE
        });

        unequipButton_3.onClick.AddListener(() => {
            InventoryManager.Instance.UnequipPowerUp(2);
            RefreshAllDisplays(); // <-- ADD THIS LINE
        });

        // Hook up the unlock button
        unlockSlot2Button.onClick.AddListener(OnUnlockSlot2Clicked);
        unlockSlotButton.onClick.AddListener(OnUnlockSlotClicked);
    }
    public void ShowPowerUpsCategory()
    {
        currentCategory = InventoryCategory.PowerUps;
        powerUpsContent.SetActive(true);
        skinsContent.SetActive(false);
        weaponsContent.SetActive(false); // <-- ADD THIS
        RefreshAllDisplays();
    }
    private void OnUnlockSlot2Clicked()
    {
        if (WalletManager.Instance.SpendCoins(secondSlotPrice))
        {
            InventoryManager.Instance.UnlockSecondSlot();
            RefreshAllDisplays();
        }
        else
        {
            Debug.Log("Not enough coins to unlock the second slot!");
        }
    }
    private void OnUnlockSlotClicked()
    {
        // Try to spend the coins
        if (WalletManager.Instance.SpendCoins(thirdSlotPrice))
        {
            // If successful, tell the InventoryManager to unlock the slot
            InventoryManager.Instance.UnlockThirdSlot();
            // Refresh the entire display to show the newly unlocked slot
            RefreshAllDisplays();
        }
        else
        {
            // Not enough coins! You can add a sound effect or visual feedback here.
            Debug.Log("Not enough coins to unlock the third slot!");
        }
    }
    public void ShowSkinsCategory()
    {
        currentCategory = InventoryCategory.Skins;
        powerUpsContent.SetActive(false);
        skinsContent.SetActive(true);
        weaponsContent.SetActive(false); // <-- ADD THIS
        ViewCatSkins();
    }

    private void ViewCatSkins()
    {
        currentCharacterView = CharacterType.Cat;
        RefreshOwnedItemsDisplay(); // This will now refresh the skin grid
        RefreshEquippedDisplay();   // This will now refresh the equipped skin icon
    }

    private void ViewManSkins()
    {
        currentCharacterView = CharacterType.Man;
        RefreshOwnedItemsDisplay();
        RefreshEquippedDisplay();
    }
   
    public void OnUnequipSlot2()
    {
        // 1. Tell the manager to perform the unequip logic
        InventoryManager.Instance.UnequipPowerUp(1);

        // 2. Immediately refresh all the visuals
        RefreshAllDisplays();
    }
    public void OpenInventory()
    {
        if (inventoryPanel != null)
        {
            // Stop any previous animations and start the new one.
            StopAllCoroutines();
            StartCoroutine(AnimatePanel(true));
            RefreshAllDisplays();
        }
    }

    public void CloseInventory()
    {
        if (inventoryPanel != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimatePanel(false));
        }
    }
    private void RefreshWeaponDisplay()
    {
        WeaponData currentWeapon = allWeapons[currentWeaponIndex];
        int currentLevel = InventoryManager.Instance.GetWeaponLevel(currentWeapon.name);

        weaponNameText.text = currentWeapon.weaponName;
        weaponIconImage.sprite = currentWeapon.weaponIcon;
        levelSlider.value = currentLevel;
        levelText.text = "Level: " + currentLevel + " / 5";
        levelSlider.interactable = false;
        if (currentLevel >= 5)
        {
            upgradeWeaponButton.interactable = false;
            upgradeCostText.text = "MAX LEVEL";
        }
        else
        {
            upgradeWeaponButton.interactable = true;
            WeaponUpgradeData nextLevelData = currentWeapon.upgradeLevels[currentLevel];
            upgradeCostText.text = "Upgrade (" + nextLevelData.upgradeCost + " Coins)";
        }
    }
    public void OnItemClicked(PowerUpData item)
    {
        // Slot 1 is always available
        if (InventoryManager.Instance.equippedPowerUps[0] == null)
        {
            InventoryManager.Instance.EquipPowerUp(item, 0);
        }
        // Check slot 2 ONLY if it's unlocked and empty
        else if (InventoryManager.Instance.isSecondSlotUnlocked && InventoryManager.Instance.equippedPowerUps[1] == null)
        {
            InventoryManager.Instance.EquipPowerUp(item, 1);
        }
        // Check slot 3 ONLY if it's unlocked and empty
        else if (InventoryManager.Instance.isThirdSlotUnlocked && InventoryManager.Instance.equippedPowerUps[2] == null)
        {
            InventoryManager.Instance.EquipPowerUp(item, 2);
        }
        else
        {
            // If all available slots are full, replace the first one
            InventoryManager.Instance.EquipPowerUp(item, 0);
        }
        RefreshAllDisplays();
    }
    public void OnUpgradeWeaponClicked()
    {
        WeaponData currentWeapon = allWeapons[currentWeaponIndex];
        int currentLevel = InventoryManager.Instance.GetWeaponLevel(currentWeapon.name);

        if (currentLevel < 5)
        {
            int upgradeCost = currentWeapon.upgradeLevels[currentLevel].upgradeCost;
            if (WalletManager.Instance.SpendCoins(upgradeCost))
            {
                InventoryManager.Instance.UpgradeWeapon(currentWeapon.name);
                RefreshWeaponDisplay();
            }
            else
            {
                Debug.Log("Not enough coins to upgrade!");
            }
        }
    }
    private void RefreshAllDisplays()
    {
        RefreshEquippedDisplay();
        RefreshOwnedItemsDisplay();
    }

    private void RefreshEquippedDisplay()
    {
        bool isPowerUpView = (currentCategory == InventoryCategory.PowerUps);

        // Get the parent GameObjects for the main displays
        GameObject powerUpSlot1Parent = equippedIcon_1.transform.parent.gameObject;
        GameObject skinSlotParent = equippedSkinIcon.transform.parent.gameObject;

        // --- STEP 1: Set the master visibility for all displays ---
        powerUpSlot1Parent.SetActive(isPowerUpView);
        slot2_LockedGroup.SetActive(isPowerUpView);
        slot2_UnlockedGroup.SetActive(isPowerUpView);
        slot3_LockedGroup.SetActive(isPowerUpView);
        slot3_UnlockedGroup.SetActive(isPowerUpView);
        skinSlotParent.SetActive(!isPowerUpView);

        // --- STEP 2: Update the logic ONLY for the visible category ---
        if (isPowerUpView)
        {
            // --- POWER-UP VIEW LOGIC ---

            // Slot 1 is always unlocked
            UpdateSlotDisplay(0, equippedIcon_1, equippedName_1, unequipButton_1);

            // Logic for Slot 2
            if (InventoryManager.Instance.isSecondSlotUnlocked)
            {
                slot2_UnlockedGroup.SetActive(true);
                slot2_LockedGroup.SetActive(false);
                UpdateSlotDisplay(1, equippedIcon_2, equippedName_2, unequipButton_2);
            }
            else
            {
                slot2_UnlockedGroup.SetActive(false);
                slot2_LockedGroup.SetActive(true);
                unlockPrice2Text.text = secondSlotPrice.ToString();
            }

            // --- NEW, INDEPENDENT LOGIC FOR SLOT 3 ---
            if (InventoryManager.Instance.isThirdSlotUnlocked)
            {
                slot3_UnlockedGroup.SetActive(true);
                slot3_LockedGroup.SetActive(false);
                UpdateSlotDisplay(2, equippedIcon_3, equippedName_3, unequipButton_3);
            }
            else
            {
                slot3_UnlockedGroup.SetActive(false);
                slot3_LockedGroup.SetActive(true);
                unlockPriceText.text = thirdSlotPrice.ToString();
            }
            // ----------------------------------------
        }
        else // It's the Skin view
        {
            // --- SKIN VIEW LOGIC ---
            UpdateEquippedSkinDisplay();
        }
    }
    public void OnUnequipSkin()
    {
        // Tell the manager to unequip the skin for the currently viewed character
        InventoryManager.Instance.UnequipSkin(currentCharacterView);

        // Refresh the display to show "Default"
        RefreshAllDisplays();
    }
    private void UpdateEquippedSkinDisplay()
    {
        string equippedSkinKey = "EquippedSkin_" + currentCharacterView.ToString();
        string equippedSkinLabel = PlayerPrefs.GetString(equippedSkinKey, "Default");

        if (equippedSkinLabel != "Default")
        {
            // A special skin is equipped
            unequipSkinButton.gameObject.SetActive(true);

            SkinData skinData = FindSkinData(currentCharacterView, equippedSkinLabel);
            if (skinData != null)
            {
                equippedSkinIcon.sprite = skinData.icon;
                equippedSkinName.text = skinData.skinName;
            }
        }
        else
        {
            // The DEFAULT skin is equipped
            unequipSkinButton.gameObject.SetActive(false);
            equippedSkinName.text = "Default";

            // --- THIS IS THE NEW LOGIC ---
            // Check which character we are viewing and set the correct default sprite
            if (currentCharacterView == CharacterType.Cat)
            {
                equippedSkinIcon.sprite = defaultCatSprite;
            }
            else if (currentCharacterView == CharacterType.Man)
            {
                equippedSkinIcon.sprite = defaultManSprite;
            }
            // -----------------------------
        }
    }
    private SkinData FindSkinDataFromUniqueID(string uniqueID)
    {
        var allSkins = Resources.LoadAll<SkinData>("Skins");
        foreach (var skin in allSkins)
        {
            if (skin.GetUniqueID() == uniqueID)
            {
                return skin;
            }
        }
        return null;
    }
    private void UpdateSlotDisplay(int slotIndex, Image icon, TextMeshProUGUI nameText, Button unequipButton)
    {
        PowerUpData itemInSlot = InventoryManager.Instance.equippedPowerUps[slotIndex];
        if (itemInSlot != null)
        {
            icon.sprite = itemInSlot.icon;
            icon.enabled = true;
            nameText.text = itemInSlot.powerUpName;
            unequipButton.gameObject.SetActive(true);
        }
        else
        {
            icon.sprite = emptySlotSprite;
            icon.enabled = (emptySlotSprite != null);
            nameText.text = "Empty";
            unequipButton.gameObject.SetActive(false);
        }
    }
    private IEnumerator AnimatePanel(bool opening)
    {
        // Define start and end values based on whether we are opening or closing
        float startAlpha = opening ? 0f : 1f;
        float endAlpha = opening ? 1f : 0f;
        Vector3 startScale = opening ? new Vector3(0.8f, 0.8f, 1f) : Vector3.one;
        Vector3 endScale = opening ? Vector3.one : new Vector3(0.8f, 0.8f, 1f);

        // If opening, first activate the panel and set initial state
        if (opening)
        {
            inventoryPanel.SetActive(true);
            panelCanvasGroup.interactable = false; // Disable clicks during animation
        }

        float timer = 0f;
        while (timer < animationDuration)
        {
            // Calculate the progress of the animation (a value from 0 to 1)
            float progress = timer / animationDuration;

            // Interpolate (Lerp) the alpha and scale based on progress
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            inventoryPanel.transform.localScale = Vector3.Lerp(startScale, endScale, progress);

            timer += Time.unscaledDeltaTime; // Use unscaled time for UI
            yield return null; // Wait for the next frame
        }

        // Ensure final values are set perfectly
        panelCanvasGroup.alpha = endAlpha;
        inventoryPanel.transform.localScale = endScale;

        // If closing, deactivate the panel at the end
        if (!opening)
        {
            inventoryPanel.SetActive(false);
        }

        panelCanvasGroup.interactable = opening; // Re-enable clicks only if panel is open
    }
    private SkinData FindSkinData(CharacterType character, string label)
    {
        // This requires you to have all your SkinData assets in a "Resources/Skins" folder
        var allSkins = Resources.LoadAll<SkinData>("Skins");
        foreach (var skin in allSkins)
        {
            if (skin.character == character && skin.spriteLibraryLabel == label)
            {
                return skin;
            }
        }
        return null; // Return null if no matching skin is found
    }
    public void OnItemClicked(SkinData item)
    {
        InventoryManager.Instance.EquipSkin(item);
        RefreshAllDisplays();
    }
    // REPLACE your entire RefreshOwnedItemsDisplay function with this one
    private void RefreshOwnedItemsDisplay()
    {
        if (currentCategory == InventoryCategory.PowerUps)
        {
            // --- THIS IS THE SAFER WAY TO CLEAR THE GRID ---
            List<GameObject> childrenToDestroy = new List<GameObject>();
            foreach (Transform child in ownedItemsContainer)
            {
                childrenToDestroy.Add(child.gameObject);
            }
            foreach (GameObject child in childrenToDestroy)
            {
                Destroy(child);
            }
            // ---------------------------------------------

            // Repopulate with owned power-ups
            foreach (PowerUpData item in InventoryManager.Instance.ownedPowerUps)
            {
                GameObject itemObject = Instantiate(inventoryItemPrefab, ownedItemsContainer);
                itemObject.GetComponent<InventoryItemButton>().Setup(item);
            }
        }
        else // It's the Skins category
        {
            // --- APPLY THE SAME SAFE METHOD HERE ---
            List<GameObject> childrenToDestroy = new List<GameObject>();
            foreach (Transform child in ownedSkinsContainer)
            {
                childrenToDestroy.Add(child.gameObject);
            }
            foreach (GameObject child in childrenToDestroy)
            {
                Destroy(child);
            }
            // ---------------------------------------

            // Repopulate with owned skins
            foreach (string ownedSkinID in InventoryManager.Instance.ownedSkins)
            {
                SkinData skinData = FindSkinDataFromUniqueID(ownedSkinID);
                if (skinData != null && skinData.character == currentCharacterView)
                {
                    GameObject itemObject = Instantiate(inventoryItemPrefab, ownedSkinsContainer);
                    itemObject.GetComponent<InventoryItemButton>().Setup(skinData);
                }
            }
        }
    }

}
