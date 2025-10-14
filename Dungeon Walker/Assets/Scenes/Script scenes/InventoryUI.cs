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
    private enum InventoryCategory { PowerUps, Skins }
    private InventoryCategory currentCategory;
    private CharacterType currentCharacterView;
    [Header("Skin Inventory Specifics")]
    [SerializeField] private Button selectCatButton;
    [SerializeField] private Button selectManButton;
    [SerializeField] private Image equippedSkinIcon;
    [SerializeField] private TextMeshProUGUI equippedSkinName;
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
    [Header("Owned Items Display")]
    [SerializeField] private Transform ownedItemsContainer;
    [SerializeField] private GameObject inventoryItemPrefab;
    [SerializeField] private Button unequipSkinButton;
    private void Awake()
    {
        unequipSkinButton.onClick.AddListener(OnUnequipSkin);
        powerUpsTabButton.onClick.AddListener(ShowPowerUpsCategory);
        skinsTabButton.onClick.AddListener(ShowSkinsCategory);
        selectCatButton.onClick.AddListener(ViewCatSkins);
        selectManButton.onClick.AddListener(ViewManSkins);
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
        }
    }
    public void OnUnequipSlot1()
    {
        // 1. Tell the manager to perform the unequip logic
        InventoryManager.Instance.UnequipPowerUp(0);

        // 2. Immediately refresh all the visuals
        RefreshAllDisplays();
    }
    private void Start()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        // Hook up the unequip buttons
        unequipButton_1.onClick.AddListener(() => InventoryManager.Instance.UnequipPowerUp(0));
        unequipButton_2.onClick.AddListener(() => InventoryManager.Instance.UnequipPowerUp(1));
    }
    public void ShowPowerUpsCategory()
    {
        currentCategory = InventoryCategory.PowerUps;

        // --- THE FIX ---
        // Only show/hide the CONTENT GRIDS. Leave the equipped displays alone.
        powerUpsContent.SetActive(true);
        skinsContent.SetActive(false);
        // ---------------

        // Animate the tabs
        // AnimateTab(powerUpsTabButton, true);
        // AnimateTab(skinsTabButton, false);

        // Now, refresh everything. RefreshEquippedDisplay will handle showing/hiding the correct equipped display.
        RefreshAllDisplays();
    }

    public void ShowSkinsCategory()
    {
        currentCategory = InventoryCategory.Skins;

        // --- THE FIX ---
        // Only show/hide the CONTENT GRIDS.
        powerUpsContent.SetActive(false);
        skinsContent.SetActive(true);
        // ---------------

        // Animate the tabs
        // AnimateTab(powerUpsTabButton, false);
        // AnimateTab(skinsTabButton, true);

        ViewCatSkins(); // This will call RefreshAllDisplays internally
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

    public void OnItemClicked(PowerUpData item)
    {
        if (InventoryManager.Instance.equippedPowerUps[0] == null)
        {
            InventoryManager.Instance.EquipPowerUp(item, 0);
        }
        else if (InventoryManager.Instance.equippedPowerUps[1] == null)
        {
            InventoryManager.Instance.EquipPowerUp(item, 1);
        }
        else
        {
            InventoryManager.Instance.EquipPowerUp(item, 0); // Default to replacing slot 1
        }
        RefreshAllDisplays();
    }

    private void RefreshAllDisplays()
    {
        RefreshEquippedDisplay();
        RefreshOwnedItemsDisplay();
    }

    private void RefreshEquippedDisplay()
    {
        // --- Determine which UI should be active ---
        bool isPowerUpView = (currentCategory == InventoryCategory.PowerUps);

        // Show/Hide the PARENT objects of the equipped displays
        // NOTE: You must create parent GameObjects for these in your hierarchy
        // and drag them into new slots in the Inspector.
        // For now, we will show/hide the individual elements.

        // Power-up slots
        equippedIcon_1.gameObject.SetActive(isPowerUpView);
        equippedName_1.gameObject.SetActive(isPowerUpView);
        unequipButton_1.gameObject.SetActive(isPowerUpView);
        equippedIcon_2.gameObject.SetActive(isPowerUpView);
        equippedName_2.gameObject.SetActive(isPowerUpView);
        unequipButton_2.gameObject.SetActive(isPowerUpView);

        // Skin slot
        if (equippedSkinIcon != null) equippedSkinIcon.gameObject.SetActive(!isPowerUpView);
        if (equippedSkinName != null) equippedSkinName.gameObject.SetActive(!isPowerUpView);


        // --- Now, ONLY update the one that is VISIBLE ---
        if (isPowerUpView)
        {
            // This is the power-up view, so only update the power-up slots
            UpdateSlotDisplay(0, equippedIcon_1, equippedName_1, unequipButton_1);
            UpdateSlotDisplay(1, equippedIcon_2, equippedName_2, unequipButton_2);
        }
        else // This is the skin view
        {
            // Only update the skin slot
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
            // Skin is equipped, show the unequip button
            unequipSkinButton.gameObject.SetActive(true); // <-- ADD THIS

            SkinData skinData = FindSkinData(currentCharacterView, equippedSkinLabel);
            if (skinData != null)
            {
                equippedSkinIcon.sprite = skinData.icon;
                equippedSkinName.text = skinData.skinName;
            }
        }
        else
        {
            // Default skin is equipped, hide the unequip button
            unequipSkinButton.gameObject.SetActive(false); // <-- ADD THIS

            equippedSkinIcon.sprite = emptySlotSprite;
            equippedSkinName.text = "Default";
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
