using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Main Panel")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("Equipped Slots Display")]
    [SerializeField] private Image equippedIcon_1;
    [SerializeField] private TextMeshProUGUI equippedName_1;
    [SerializeField] private Button unequipButton_1;
    [SerializeField] private Image equippedIcon_2;
    [SerializeField] private TextMeshProUGUI equippedName_2;
    [SerializeField] private Button unequipButton_2;
    [SerializeField] private Sprite emptySlotSprite;

    [Header("Owned Items Display")]
    [SerializeField] private Transform ownedItemsContainer;
    [SerializeField] private GameObject inventoryItemPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
            inventoryPanel.SetActive(true);
            RefreshAllDisplays();
        }
    }

    public void CloseInventory()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
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
        UpdateSlotDisplay(0, equippedIcon_1, equippedName_1, unequipButton_1);
        UpdateSlotDisplay(1, equippedIcon_2, equippedName_2, unequipButton_2);
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

    private void RefreshOwnedItemsDisplay()
    {
        foreach (Transform child in ownedItemsContainer) Destroy(child.gameObject);

        foreach (PowerUpData item in InventoryManager.Instance.ownedPowerUps)
        {
            GameObject itemObject = Instantiate(inventoryItemPrefab, ownedItemsContainer);
            itemObject.GetComponent<InventoryItemButton>().Setup(item);
        }
    }
}
